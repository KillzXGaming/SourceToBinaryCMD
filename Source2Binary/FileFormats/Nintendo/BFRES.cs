using System;
using System.Collections.Generic;
using System.Text;
using Syroot.NintenTools.NSW.Bfres.Helpers;
using Syroot.NintenTools.NSW.Bfres;
using Syroot.NintenTools.NSW.Bfres.GFX;
using System.IO;
using Syroot.Maths;
using System.Linq;

namespace Source2Binary
{
    public class BFRES : IConvertableBinary
    {
        public void GenerateBinary(System.IO.Stream stream, string[] args)
        {
            ResFile resFile = new ResFile();
            string output = "";
            string input = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-i" || args[i] == "--input")
                {
                    input = args[i + 1];
                }
                if (args[i] == "-o" || args[i] == "--output")
                {
                    output = args[i + 1];
                }
            }

            if (input == string.Empty)
            {
                Console.WriteLine("Must have an input (-i) file path");
                return;
            }
            if (output == string.Empty)
            {
                Console.WriteLine("Must have an output (-o) file path");
                return;
            }

            STGenericScene scene = new STGenericScene();

            string ext = Path.GetExtension(input);
            switch (ext)
            {
                case ".dae":
                    scene = Collada.ColladaReader.Read(input);
                    break;
                case ".fbx":
                    break;
            }

            ConvertScene(resFile, scene);
            resFile.Save(output);
        }

        private void ConvertScene(ResFile resFile, STGenericScene scene)
        {
            foreach (var model in scene.Models)
            {
                Model fmdl = new Model();
                fmdl.Name = model.Name;
                foreach (var mat in model.Materials)
                {
                    Material fmat = new Material();
                    fmat.Name = mat.Name;
                    fmdl.Materials.Add(fmat);
                    fmdl.MaterialDict.Add(fmat.Name);
                }

                fmdl.Skeleton = new Skeleton();
                foreach (var bone in model.Skeleton.Bones)
                {
                    fmdl.Skeleton.BoneDict.Add(bone.Name);
                    fmdl.Skeleton.Bones.Add(new Bone()
                    {
                        FlagsRotation = BoneFlagsRotation.EulerXYZ,
                        FlagsTransform = SetBoneFlags(bone),
                        Name = bone.Name,
                        RigidMatrixIndex = -1,  //Gets calculated after
                        SmoothMatrixIndex = -1, //Gets calculated after
                        ParentIndex = (short)bone.ParentIndex,
                        Position = new Vector3F(
                            bone.Position.X,
                            bone.Position.Y,
                            bone.Position.Z),
                        Scale = new Vector3F(
                            bone.Scale.X,
                            bone.Scale.Y,
                            bone.Scale.Z),
                        Rotation = new Vector4F(
                            bone.EulerRotation.X,
                            bone.EulerRotation.Y,
                            bone.EulerRotation.Z,
                            1.0f),
                        Visible = true,
                    });
                }

                List<int> smoothSkinningIndices = new List<int>();
                List<int> rigidSkinningIndices = new List<int>();

                //Determine the rigid and smooth bone skinning
                foreach (var mesh in model.Meshes)
                {
                    Console.WriteLine("mesh " + mesh.Name);

                    var numSkinning = CalculateSkinCount(mesh.Vertices);
                    foreach (var vertex in mesh.Vertices)
                    {
                        foreach (var bone in vertex.BoneWeights)
                        {
                            var bn = fmdl.Skeleton.Bones.Where(x => x.Name == bone.Bone).FirstOrDefault();
                            if (bn != null) {
                                int index = fmdl.Skeleton.Bones.IndexOf(bn);

                                //Rigid skinning
                                if (numSkinning == 1)
                                {
                                    bn.RigidMatrixIndex = (short)index;
                                    if (!rigidSkinningIndices.Contains(index))
                                        rigidSkinningIndices.Add(index);
                                }
                                else
                                {
                                    bn.SmoothMatrixIndex = (short)index;
                                    if (!smoothSkinningIndices.Contains(index))
                                        smoothSkinningIndices.Add(index);
                                }
                            }
                        }
                    }
                }

                List<int> skinningIndices = new List<int>();
                skinningIndices.AddRange(smoothSkinningIndices);
                skinningIndices.AddRange(rigidSkinningIndices);

                fmdl.Skeleton.MatrixToBoneList = new List<ushort>();
                for (int i = 0; i < skinningIndices.Count; i++)
                    fmdl.Skeleton.MatrixToBoneList.Add((ushort)skinningIndices[i]); 

                foreach (var mesh in model.Meshes)
                {
                    var settings = new MeshSettings()
                    {
                        UseBoneIndices = true,
                        UseBoneWeights = true,
                        UseNormal = true,
                        UseTexCoord = new bool[5] { true, true, true, true, true, },
                        UseColor    = new bool[5] { true, true, true, true, true, },
                    };

                    var names = fmdl.Shapes.Select(x => x.Name).ToList();

                    Shape fshp = new Shape();
                    fshp.Name = Utility.RenameDuplicateString(names, mesh.Name, 0, 2);
                //    fshp.MaterialIndex = (ushort)mesh.PolygonGroups[0].MaterialIndex;
                    fshp.MaterialIndex = 0;

                    var boundingBox = CalculateBoundingBox(mesh.Vertices);
                    fshp.SubMeshBoundings.Add(boundingBox);
                    fshp.SubMeshBoundings.Add(boundingBox);
                    fshp.SubMeshBoundingIndices.Add(0);
                    fshp.RadiusArray.Add((float)(boundingBox.Center.Length + boundingBox.Extent.Length));
                    fshp.SubMeshBoundingNodes.Add(new BoundingNode()
                    {
                        LeftChildIndex = 0,
                        RightChildIndex = 0,
                        NextSibling = 0,
                        SubMeshIndex = 0,
                        Unknown = 0,
                        SubMeshCount = 1,
                    });

                    try
                    {
                        VertexBuffer buffer = GenerateVertexBuffer(mesh, settings, fmdl.Skeleton);
                        fshp.VertexBufferIndex = (ushort)fmdl.VertexBuffers.Count;
                        fshp.VertexSkinCount = (byte)buffer.VertexSkinCount;
                        fmdl.VertexBuffers.Add(buffer);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Failed to generate vertex buffer! \n " + ex.ToString());
                    }


                    Mesh bMesh = new Mesh();
                    bMesh.PrimitiveType = PrimitiveType.Triangles;
                    bMesh.SetIndices(mesh.Faces);
                    bMesh.SubMeshes.Add(new SubMesh()
                    {
                        Offset = 0,
                        Count = (uint)mesh.Faces.Count,
                    });
                    fshp.Meshes.Add(bMesh);

                    fmdl.Shapes.Add(fshp);
                    fmdl.ShapeDict.Add(fshp.Name);
                }

                resFile.Models.Add(fmdl);
            }
        }

        private static BoneFlagsTransform SetBoneFlags(STBone bn)
        {
            BoneFlagsTransform flags = BoneFlagsTransform.None;
            if (bn.Position == OpenTK.Vector3.Zero)
                flags |= BoneFlagsTransform.TranslateZero;
            if (bn.EulerRotation == OpenTK.Vector3.Zero)
                flags |= BoneFlagsTransform.RotateZero;
            if (bn.Scale == OpenTK.Vector3.One)
                flags |= BoneFlagsTransform.ScaleOne;
            return flags;
        }

        private static uint CalculateSkinCount(List<STVertex> vertices)
        {
            uint numSkinning = 0;
            for (int v = 0; v < vertices.Count; v++)
                numSkinning = Math.Max(numSkinning, (uint)vertices[v].BoneWeights.Count);
            return numSkinning;
        }

        private static Bounding CalculateBoundingBox(List<STVertex> vertices)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;
            for (int i = 0; i < vertices.Count; i++)
            {
                minX = Math.Min(minX, vertices[i].Position.X);
                minY = Math.Min(minY, vertices[i].Position.Y);
                minZ = Math.Min(minZ, vertices[i].Position.Z);
                maxX = Math.Max(maxX, vertices[i].Position.X);
                maxY = Math.Max(maxY, vertices[i].Position.Y);
                maxZ = Math.Max(maxZ, vertices[i].Position.Z);
            }

            return CalculateBoundingBox(
                new Vector3F(minX, minY, minZ),
                new Vector3F(maxX, maxY, maxZ));
        }

        private static Bounding CalculateBoundingBox(Vector3F min, Vector3F max)
        {
            Vector3F center = max + min;

            float xxMax = GetExtent(max.X, min.X);
            float yyMax = GetExtent(max.Y, min.Y);
            float zzMax = GetExtent(max.Z, min.Z);

            Vector3F extend = new Vector3F(xxMax, yyMax, zzMax);

            return new Bounding()
            {
                Center = new Vector3F(center.X, center.Y, center.Z),
                Extent = new Vector3F(extend.X, extend.Y, extend.Z),
            };
        }

        private static float GetExtent(float max, float min)
        {
            return (float)Math.Max(Math.Sqrt(max * max), Math.Sqrt(min * min));
        }

        private VertexBuffer GenerateVertexBuffer(STGenericMesh mesh, MeshSettings settings,
           Skeleton skeleton)
        {
            VertexBufferHelper vertexBufferHelper = new VertexBufferHelper();

            List<Vector4F> Positions = new List<Vector4F>();
            List<Vector4F> Normals = new List<Vector4F>();
            List<Vector4F> BoneWeights = new List<Vector4F>();
            List<Vector4F> BoneIndices = new List<Vector4F>();
            List<Vector4F> Tangents = new List<Vector4F>();
            List<Vector4F> Bitangents = new List<Vector4F>();

            int numTexCoords = mesh.Vertices.FirstOrDefault().TexCoords.Length;
            int numColors    = mesh.Vertices.FirstOrDefault().Colors.Length;

            Vector4F[][] TexCoords = new Vector4F[numTexCoords][];
            Vector4F[][] Colors = new Vector4F[numColors][];

            for (int c = 0; c < numColors; c++)
                Colors[c] = new Vector4F[mesh.Vertices.Count];
            for (int c = 0; c < numTexCoords; c++)
                TexCoords[c] = new Vector4F[mesh.Vertices.Count];

            for (int v = 0; v < mesh.Vertices.Count; v++)
            {
                var vertex = mesh.Vertices[v];

                Positions.Add(new Vector4F(
                    vertex.Position.X,
                    vertex.Position.Y, 
                    vertex.Position.Z, 0));

                Normals.Add(new Vector4F(
                    vertex.Normal.X,
                    vertex.Normal.Y,
                    vertex.Normal.Z, 0));

                for (int i = 0; i < vertex.TexCoords?.Length; i++)
                {
                    TexCoords[i][v] = new Vector4F(
                        vertex.TexCoords[i].X,
                        vertex.TexCoords[i].Y,
                        0,0);
                }

                for (int i = 0; i < vertex.Colors?.Length; i++)
                {
                    Colors[i][v] = new Vector4F(
                        vertex.Colors[i].X,
                        vertex.Colors[i].Y,
                        vertex.Colors[i].Z,
                        vertex.Colors[i].W);
                }

                int[] indices = new int[4];
                float[] weights = new float[4];
                for (int j = 0; j < vertex.BoneWeights?.Count; j++)
                {
                    int index = Array.FindIndex(skeleton.Bones.ToArray(), x => x.Name == vertex.BoneWeights[j].Bone);
                    if (index != -1) {
                        indices[j] = skeleton.MatrixToBoneList.IndexOf((ushort)index);
                        weights[j] = vertex.BoneWeights[j].Weight;
                    }
                }

                if (vertex.BoneWeights?.Count > 0 && settings.UseBoneIndices)
                {
                    BoneWeights.Add(new Vector4F(weights[0], weights[1], weights[2], weights[3]));
                    BoneIndices.Add(new Vector4F(indices[0], indices[1], indices[2], indices[3]));
                }
            }

            List<VertexBufferHelperAttrib> attributes = new List<VertexBufferHelperAttrib>();

            attributes.Add(new VertexBufferHelperAttrib()
            {
                Name = "_p0",
                Data = Positions.ToArray(),
                Format = settings.PositionFormat,
            });

            if (Normals.Count > 0) {
                attributes.Add(new VertexBufferHelperAttrib()
                {
                    Name = "_n0",
                    Data = Normals.ToArray(),
                    Format = settings.NormalFormat,
                });
            }


            if (Tangents.Count > 0) {
                attributes.Add(new VertexBufferHelperAttrib()
                {
                    Name = "_t0",
                    Data = Tangents.ToArray(),
                    Format = settings.TangentFormat,
                });
            }

            if (Bitangents.Count > 0) {
                attributes.Add(new VertexBufferHelperAttrib()
                {
                    Name = "_b0",
                    Data = Bitangents.ToArray(),
                    Format = settings.BitangentFormat,
                });
            }

            for (int i = 0; i < TexCoords.Length; i++) {
                if (settings.UseTexCoord[i]) {
                    attributes.Add(new VertexBufferHelperAttrib()
                    {
                        Name = $"_u{i}",
                        Data = TexCoords[i],
                        Format = settings.TexCoordFormat,
                    });
                }
            }

            for (int i = 0; i < Colors.Length; i++)
            {
                if (settings.UseColor[i]) {
                    attributes.Add(new VertexBufferHelperAttrib()
                    {
                        Name = $"_c{i}",
                        Data = Colors[i],
                        Format = settings.ColorFormat,
                    });
                }
            }

            if (BoneIndices.Count > 0) {
                attributes.Add(new VertexBufferHelperAttrib()
                {
                    Name = "_i0",
                    Data = BoneIndices.ToArray(),
                    Format = settings.BoneIndicesFormat,
                });
            }

            if (BoneWeights.Count > 0) {
                attributes.Add(new VertexBufferHelperAttrib()
                {
                    Name = "_w0",
                    Data = BoneWeights.ToArray(),
                    Format = settings.BoneWeightsFormat,
                });
            }

            vertexBufferHelper.Attributes = attributes;
            var buffer = vertexBufferHelper.ToVertexBuffer();

            if (settings.UseBoneIndices)
                buffer.VertexSkinCount = CalculateSkinCount(mesh.Vertices);
            else
                buffer.VertexSkinCount = 0;

            return buffer;
        }

        public class MeshSettings
        {
            public bool UseNormal { get; set; }
            public bool[] UseTexCoord { get; set; }
            public bool[] UseColor { get; set; }
            public bool UseBoneWeights { get; set; }
            public bool UseBoneIndices { get; set; }

            public AttribFormat PositionFormat = AttribFormat.Format_32_32_32_Single;
            public AttribFormat NormalFormat = AttribFormat.Format_10_10_10_2_SNorm;
            public AttribFormat TexCoordFormat = AttribFormat.Format_16_16_Single;
            public AttribFormat ColorFormat = AttribFormat.Format_16_16_16_16_Single;
            public AttribFormat TangentFormat = AttribFormat.Format_8_8_8_8_SNorm;
            public AttribFormat BitangentFormat = AttribFormat.Format_8_8_8_8_SNorm;

            public AttribFormat BoneIndicesFormat = AttribFormat.Format_8_8_8_8_UInt;
            public AttribFormat BoneWeightsFormat = AttribFormat.Format_8_8_8_8_UNorm;

        }
    }
}
