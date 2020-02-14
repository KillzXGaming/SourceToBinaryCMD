using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace Source2Binary
{
    /// <summary>
    /// Represents a bone storing information to allow rendering, editing, and exporting from a skeleton
    /// </summary>
    public class STBone
    {
        /// <summary>
        /// Gets or sets the name of the bone.
        /// </summary>
        public string Name { get; set; }

        private STSkeleton Skeleton;

        private Matrix4 transform;

        /// <summary>
        /// Gets or sets the transformation of the bone.
        /// Setting this will adjust the 
        /// <see cref="Scale"/>, 
        /// <see cref="Rotation"/>, and 
        /// <see cref="Position"/> properties.
        /// </summary>
        public Matrix4 Transform
        {
            set
            {
                Scale = value.ExtractScale();
                Rotation = value.ExtractRotation();
                Position = value.ExtractTranslation();
                transform = value;
            }
            get
            {
                return transform;
            }
        }

        /// <summary>
        /// Gets or sets the position of the bone in world space.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Gets or sets the scale of the bone in world space.
        /// </summary>
        public Vector3 Scale { get; set; }

        /// <summary>
        /// Gets or sets the rotation of the bone in world space.
        /// </summary>
        public Quaternion Rotation { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rotation"/> using euler method. 
        /// </summary>
        public Vector3 EulerRotation 
        {
            get { return  STMath.ToEulerAngles(Rotation); }
            set { Rotation = STMath.FromEulerAngles(value); }
        }

        /// <summary>
        /// Gets or sets the parent bone. Returns null if unused.
        /// </summary>
        public STBone Parent;

        /// <summary>
        /// The list of children this bone is parenting to.
        /// </summary>
        public List<STBone> Children = new List<STBone>();

        /// <summary>
        /// Gets or sets the parent bone index.
        /// </summary>
        public int ParentIndex
        {
            set
            {
                if (Parent != null) Parent.Children.Remove(this);
                if (value > -1 && value < Skeleton.Bones.Count) {
                    Skeleton.Bones[value].Children.Add(this);
                }
            }

            get
            {
                if (Parent == null || !(Parent is STBone))
                    return -1;

                return Skeleton.Bones.IndexOf((STBone)Parent);
            }
        }

        public STBone(STSkeleton parentSkeleton, string name) {
            Skeleton = parentSkeleton;
            Name = name;
            Scale = new Vector3(1,1,1);
            Position = new Vector3(0,0,0);
            EulerRotation = new Vector3(0,0,0);
        }
    }
}
