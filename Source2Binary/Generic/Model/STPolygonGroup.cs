using System;
using System.Collections.Generic;
using System.Text;

namespace Source2Binary
{
    public class STPolygonGroup
    {
        public int MaterialIndex { get; set; }

        public STGenericMaterial Material { get; set; }

        public List<uint> Faces = new List<uint>();
    }
}
