using System;
using System.Collections.Generic;
using System.Text;

namespace Source2Binary
{
    public class STGenericModel
    {
        public string Name { get; set; }

        public List<STGenericMesh> Meshes = new List<STGenericMesh>();

        public List<STGenericMaterial> Materials = new List<STGenericMaterial>();

        public STSkeleton Skeleton = new STSkeleton();

        public STGenericModel(string name) {
            Name = name;
        }
    }
}
