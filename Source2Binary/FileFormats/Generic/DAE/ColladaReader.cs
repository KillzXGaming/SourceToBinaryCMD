using System;
using System.Collections.Generic;
using System.Text;
using Collada141;
using OpenTK;
using System.Linq;

namespace Source2Binary.Collada
{
    public class ColladaReader
    {
        public static STGenericScene Read(string fileName, DAE.ImportSettings settings = null)
        {
            if (settings == null) settings = new DAE.ImportSettings();

             STGenericScene Scene = new STGenericScene();

            COLLADA collada = COLLADA.Load(fileName);
            ColladaScene colladaScene = new ColladaScene(collada, settings);

            //Usually there is only one scene, but it can be possible some tools use multiple per model
            //Each one contains node hiearchies for bones and meshes
            foreach (var scene in colladaScene.scenes.visual_scene)
            {
                var model = new STGenericModel(scene.name);
                Node Root = LoadScene(scene, model, colladaScene);
                Scene.Models.Add(model);

                if (colladaScene.materials != null) {
                    foreach (var mat in colladaScene.materials.material)
                        model.Materials.Add(LoadMaterial(mat));
                }
                else
                    model.Materials.Add(new STGenericMaterial() { Name = "Dummy" });

                if (model.Skeleton.Bones.Count == 0)
                    model.Skeleton.Bones.Add(new STBone(model.Skeleton, "root"));

                if (settings.FixDuplicateNames) {
                    //Adjust duplicate names
                /*    foreach (var mesh in model.Meshes)
                    {
                        var names = model.Meshes.Select(x => x.Name).ToList();
                        mesh.Name = Utility.RenameDuplicateString(names, mesh.Name, 0, 2);
                    }

                    foreach (var mat in model.Materials)
                    {
                        var names = model.Materials.Select(x => x.Name).ToList();
                        mat.Name = Utility.RenameDuplicateString(names, mat.Name, 0, 2);
                    }

                    foreach (var bone in model.Skeleton.Bones)
                    {
                        var names = model.Skeleton.Bones.Select(x => x.Name).ToList();
                        bone.Name = Utility.RenameDuplicateString(names, bone.Name, 0, 2);
                    }*/
                }
            }

            return Scene;
        }

        public class ColladaScene
        {
            public DAE.ImportSettings Settings;

            public library_geometries geometries;
            public library_images images;
            public library_visual_scenes scenes;
            public library_effects effects;
            public library_controllers controllers;
            public library_materials materials;

            public UpAxisType UpAxisType;
            public assetUnit UintSize;

            public ColladaScene(COLLADA collada, DAE.ImportSettings settings)
            {
                Settings    = settings;
                geometries  = FindLibraryItem<library_geometries>(collada.Items);
                images      = FindLibraryItem<library_images>(collada.Items);
                scenes      = FindLibraryItem<library_visual_scenes>(collada.Items);
                effects     = FindLibraryItem<library_effects>(collada.Items);
                controllers = FindLibraryItem<library_controllers>(collada.Items);
                materials   = FindLibraryItem<library_materials>(collada.Items);

                if (collada.asset != null) {
                    UpAxisType = collada.asset.up_axis;
                    UintSize = collada.asset.unit;
                }
            }

            private static T FindLibraryItem<T>(object[] items) {
                var item = Array.Find(items, x => x.GetType() == typeof(T));
                return (T)item;
            }
        }

        public static STGenericMaterial LoadMaterial(material daeMat)
        {
            STGenericMaterial mat = new STGenericMaterial();
            mat.Name = daeMat.id;
            return mat;
        }

        public static Node LoadScene(visual_scene visualScene,
            STGenericModel model, ColladaScene colladaScene)
        {
            Node node = new Node(null);
            node.Name = visualScene.name;

            foreach (node child in visualScene.node)
                node.Children.Add(LoadHiearchy(node, child, model, colladaScene));
            return node;
        }

        public static Node LoadHiearchy(Node parent, node daeNode,
            STGenericModel model, ColladaScene colladaScene)
        {
            Node node = new Node(parent);
            node.Name = daeNode.name;
            node.Type = daeNode.type;
            node.Transform = DaeUtility.GetMatrix(daeNode.Items) * parent.Transform;

            try
            {
                if (daeNode.instance_geometry != null)
                {
                    geometry geom = DaeUtility.FindGeoemertyFromNode(daeNode, colladaScene.geometries);
                    model.Meshes.Add(LoadMeshData(colladaScene, node, geom, colladaScene.materials));
                }
                if (daeNode.instance_controller != null)
                {
                    controller controller = DaeUtility.FindControllerFromNode(daeNode, colladaScene.controllers);
                    geometry geom = DaeUtility.FindGeoemertyFromController(controller, colladaScene.geometries);
                    model.Meshes.Add(LoadMeshData(colladaScene, node, geom, colladaScene.materials, controller));
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to convert mesh {daeNode.name} \n {ex.ToString()}");
            }

            //Find the root bone
            if (node.Type == NodeType.JOINT) {
                LoadBoneHiearchy(daeNode, model, null, parent);
            }
            else if (daeNode.node1 != null) {
                foreach (node child in daeNode.node1)
                    node.Children.Add(LoadHiearchy(node, child, model, colladaScene));
            }

            return node;
        }

        private static STBone LoadBoneHiearchy(node daeNode, STGenericModel model,
            STBone boneParent, Node parent)
        {
            STBone bone = new STBone(model.Skeleton, daeNode.name);
            model.Skeleton.Bones.Add(bone);

            var transform = DaeUtility.GetMatrix(daeNode.Items) * parent.Transform;

            bone.Scale = transform.ExtractScale();
            bone.Rotation = transform.ExtractRotation();
            bone.Position = transform.ExtractTranslation();
            bone.Parent = boneParent;

            if (daeNode.node1 != null)
            {
                foreach (node child in daeNode.node1)
                    bone.Children.Add(LoadBoneHiearchy(child, model, bone, parent));
            }
            return bone;
        }

        private static STGenericMesh LoadMeshData(ColladaScene scene, Node node,
            geometry geom, library_materials materials, controller controller = null)
        {
            mesh daeMesh = geom.Item as mesh;

            STGenericMesh mesh = new STGenericMesh();
            mesh.Vertices = new List<STVertex>();
            mesh.Name = geom.name;

            var boneWeights = ParseWeightController(controller);
            int numVertex = GetTotalVertexCount(daeMesh.Items);

            //Create a current list of all the vertices
            //Use a list to expand duplicate indices
           // for (int i = 0; i < numVertex; i++)
            //    vertices.Add(new Vertex(vertices.Count, new List<int>()));

            foreach (var item in daeMesh.Items) {
                if (item is polylist)
                {
                    var poly = item as polylist;
                    ConvertPolygon(scene, mesh, daeMesh, poly.input,
                        boneWeights, materials, poly.material, poly.p);
                }
                else if (item is triangles)
                {
                    var triangle = item as triangles;
                    ConvertPolygon(scene, mesh, daeMesh, triangle.input,
                       boneWeights, materials, triangle.material, triangle.p);
                }
            }

            return mesh;
        }

        private static int GetTotalVertexCount(object[] items)
        {
            int numVertex = 0;
            foreach (var poly in items)
            {
                string[] indices = new string[0];
                if (poly is polylist)
                    indices = ((polylist)poly).p.Trim(' ').Split(' ');
                else if (poly is triangles)
                    indices = ((triangles)poly).p.Trim(' ').Split(' ');

                for (int i = 0; i < indices.Length; i++)
                {
                    int index = Convert.ToInt32(indices[i]);
                    numVertex = Math.Max(numVertex, index + 1);
                }
            }
            return numVertex;
        }

        private static void ConvertPolygon(ColladaScene scene,STGenericMesh mesh, mesh daeMesh,
            InputLocalOffset[] inputs, List<BoneWeight[]> boneWeights, 
            library_materials materials, string material, string polys)
        {
            List<uint> faces = new List<uint>();

            STPolygonGroup group = new STPolygonGroup();
            mesh.PolygonGroups.Add(group);
            group.MaterialIndex = DaeUtility.FindMaterialIndex(materials, material);

            string[] indices = polys.Trim(' ').Split(' ');
            int stride = inputs.Length;

            List<Vertex> vertices = new List<Vertex>();
            var vertexSource = DaeUtility.FindSourceFromInput(daeMesh.vertices.input[0], daeMesh.source);
            var floatArr = vertexSource.Item as float_array;
            for (int v = 0; v < (int)floatArr.count / 3; v++) {
                vertices.Add(new Vertex(vertices.Count, new List<int>()));
            }

            for (int i = 0; i < indices.Length ; i += stride)
            {
                List<int> semanticIndices = new List<int>();
                for (int j = 0; j < stride; j++)
                {
                    var input = inputs[j];
                    int index = Convert.ToInt32(indices[i  + (int)input.offset]);
                    semanticIndices.Add(index);
                }

                BoneWeight[] boneWeightData = new BoneWeight[0];
                if (boneWeights?.Count > semanticIndices[0])
                    boneWeightData = boneWeights[semanticIndices[0]];

                 VertexLoader.LoadVertex(ref faces, ref vertices, semanticIndices, boneWeightData);
            }

            group.Faces = faces;

            int numTexCoordChannels = 0;
            int numColorChannels = 0;

            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i].semantic == "TEXCOORD")
                    numTexCoordChannels++;
                if (inputs[i].semantic == "COLOR")
                    numColorChannels++;
            }

            foreach (var daeVertex in vertices)
            {
                if (daeVertex.semanticIndices.Count == 0)
                    continue;

                STVertex vertex = new STVertex();
                vertex.TexCoords = new Vector2[numTexCoordChannels];
                vertex.Colors = new Vector4[numColorChannels];
                vertex.BoneWeights = daeVertex.BoneWeights.ToList();
                mesh.Vertices.Add(vertex);

                for (int i = 0; i < inputs.Length; i++)
                {
                    var input = inputs[i];
                    source source = null;
                    if (input.semantic == "VERTEX")
                    {
                        for (int j = 0; j < daeMesh.vertices.input.Length; j++)
                        {
                            var vertexInput = daeMesh.vertices.input[j];
                            if (vertexInput.semantic == "POSITION")
                                source = DaeUtility.FindSourceFromInput(vertexInput, daeMesh.source);
                        }
                    }
                    else
                        source = DaeUtility.FindSourceFromInput(input, daeMesh.source);

                    int dataStride = (int)source.technique_common.accessor.stride;
                    int index = daeVertex.semanticIndices[i] * dataStride;
                    int set = (int)input.set;

                    float_array array = source.Item as float_array;
                    switch (inputs[i].semantic)
                    {
                        case "VERTEX":
                            vertex.Position = new Vector3(
                                (float)array.Values[index + 0],
                                (float)array.Values[index + 1],
                                (float)array.Values[index + 2]);
                            break;
                        case "NORMAL":
                            vertex.Normal = new Vector3(
                                (float)array.Values[index + 0],
                                (float)array.Values[index + 1],
                                (float)array.Values[index + 2]);
                            break;
                        case "TEXCOORD":
                            vertex.TexCoords[set] = new Vector2(
                                (float)array.Values[index + 0],
                                (float)array.Values[index + 1]);
                            break;
                        case "COLOR":
                            float R = 1, G = 1, B = 1, A = 1;
                            if (stride >= 1) R = (float)array.Values[index + 0];
                            if (stride >= 2) G = (float)array.Values[index + 1];
                            if (stride >= 3) B = (float)array.Values[index + 2];
                            if (stride >= 4) A = (float)array.Values[index + 3];
                            vertex.Colors[set] = new Vector4(R,G,B,A);
                            break;
                    }

                    switch (scene.UpAxisType)
                    {
                        case UpAxisType.X_UP:
                            break;
                        case UpAxisType.Y_UP:
                            break;
                        case UpAxisType.Z_UP:
                            break;
                    }
                }
            }
        }

        private static List<BoneWeight[]> ParseWeightController(controller controller)
        {
            if (controller == null) return new List<BoneWeight[]>();

            List<BoneWeight[]> boneWeights = new List<BoneWeight[]>();
            skin skin = controller.Item as skin;
            string[] skinningCounts = skin.vertex_weights.vcount.Trim(' ').Split(' ');
            string[] indices = skin.vertex_weights.v.Trim(' ').Split(' ');

            int stride = skin.vertex_weights.input.Length;
            int indexOffset = 0;
            for (int v = 0; v < skinningCounts.Length; v++)
            {
                int numSkinning = Convert.ToInt32(skinningCounts[v]);

                BoneWeight[] boneWeightsArr = new BoneWeight[numSkinning];
                for (int j = 0; j < numSkinning; j++) {
                    boneWeightsArr[j] = new BoneWeight();
                    foreach (var input in skin.vertex_weights.input)
                    {
                        int offset = (int)input.offset;
                        var source = DaeUtility.FindSourceFromInput(input, skin.source);
                        int index = Convert.ToInt32(indices[indexOffset + offset]);
                        if (input.semantic == "WEIGHT")
                        {
                            var weights = source.Item as float_array;
                            boneWeightsArr[j].Weight = (float)weights.Values[index];
                        }
                        if (input.semantic == "JOINT")
                        {
                            var bones = source.Item as Name_array;
                            boneWeightsArr[j].Bone = bones.Values[index];
                        }
                    }
                    indexOffset += stride;
                }

                boneWeightsArr = RemoveZeroWeights(boneWeightsArr);
                boneWeights.Add(boneWeightsArr);
            }

            return boneWeights;
        }

        private static BoneWeight[] RemoveZeroWeights(BoneWeight[] boneWeights)
        {
            float[] weights = new float[4];

            int MaxWeight = 255;
            for (int j = 0; j < 4; j++)
            {
                if (boneWeights.Length < j + 1)
                {
                    weights[j] = 0;
                    MaxWeight = 0;
                }
                else
                {
                    int weight = (int)(boneWeights[j].Weight * 255);
                    if (boneWeights.Length == j + 1)
                        weight = MaxWeight;

                    if (weight >= MaxWeight)
                    {
                        weight = MaxWeight;
                        MaxWeight = 0;
                    }
                    else
                        MaxWeight -= weight;

                    weights[j] = weight / 255f;
                }
            }

            for (int i = 0; i < boneWeights.Length; i++)
                boneWeights[i].Weight = weights[i];

            return boneWeights;
        }

        public class Node
        {
            public Node Parent;

            public string Name { get; set; }
            public Matrix4 Transform { get; set; }

            public NodeType Type = NodeType.NODE;

            public List<Node> Children = new List<Node>();

            public Node(Node parent) {
                Parent = parent;
                Transform = Matrix4.Identity;
            }
        }

        public class Vertex
        {
            public BoneWeight[] BoneWeights;

            public List<int> semanticIndices = new List<int>();

            public Vertex DuplicateVertex;
            public bool IsSet => semanticIndices.Count > 0;
            public int Index { get; private set; }

            public Vertex(int index, List<int> indices)
            {
                Index = index;
                semanticIndices = indices;
                BoneWeights = new BoneWeight[0];
            }

            public bool IsMatch(List<int> indices)
            {
                for (int i = 0; i < indices.Count; i++)
                    if (indices[i] != semanticIndices[i])
                        return false;
                return true;
            }
        }
    }
}
