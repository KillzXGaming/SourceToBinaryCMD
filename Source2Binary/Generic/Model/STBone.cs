using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace Source2Binary
{
    public class STBone
    {
        private STSkeleton Skeleton;

        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }

        public Quaternion Rotation { get; set; }

        public Vector3 EulerRotation 
        {
            get { return  STMath.ToEulerAngles(Rotation); }
            set { Rotation = STMath.FromEulerAngles(value); }
        }

        public string Name { get; set; }

        public STBone Parent;

        public List<STBone> Children = new List<STBone>();

        public int ParentIndex
        {
            set
            {
                if (Parent != null) Parent.Children.Remove(this);
                if (value > -1 && value < Skeleton.Bones.Count)
                {
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
