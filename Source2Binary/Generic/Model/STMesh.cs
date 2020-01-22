using System;
using System.Collections.Generic;
using System.Text;

namespace Source2Binary
{
    public class STGenericMesh
    {
        /// <summary>
        /// Gets the total amount of faces in all polygon groups
        /// </summary>
        public List<uint> Faces
        {
            get
            {
                List<uint> faces = new List<uint>();
                for (int i = 0; i < PolygonGroups.Count; i++)
                    faces.AddRange(PolygonGroups[i].Faces);
                return faces;
            }
        }

        public List<STPolygonGroup> PolygonGroups = new List<STPolygonGroup>();
        public List<STVertex> Vertices = new List<STVertex>();

        public string Name { get; set; }
    }
}
