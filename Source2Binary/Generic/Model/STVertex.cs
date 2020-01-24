﻿using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
namespace Source2Binary
{
    public class STVertex
    {
        public Vector3 Position { get; set; }
        public Vector3 Normal { get; set; }
        public Vector2[] TexCoords { get; set; }
        public Vector4[] Colors { get; set; }
        public Vector4 Tangent { get; set; }
        public Vector4 Bitangent { get; set; }

        public List<BoneWeight> BoneWeights = new List<BoneWeight>();

        public float DistanceTo(STVertex vertex)
        {
            float deltaX = vertex.Position.X - Position.X;
            float deltaY = vertex.Position.Y - Position.Y;
            float deltaZ = vertex.Position.Z - Position.Z;

            return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        }
    }

    public class BoneWeight
    {
        public string Bone { get; set; }
        public int Index { get; set; }
        public float Weight { get; set; }
    }
}
