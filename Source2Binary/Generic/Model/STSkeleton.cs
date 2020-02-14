using System;
using System.Collections.Generic;
using System.Text;

namespace Source2Binary
{
    /// <summary>
    /// Represents a skeleton which stores multiple <see cref="STBone"/>.
    /// This is used for rendering, editing and exporting a skeleton with its bones.
    /// </summary>
    public class STSkeleton
    {
        /// <summary>
        /// A list of bones attatched to the skeleton.
        /// </summary>
        public List<STBone> Bones = new List<STBone>();
    }
}
