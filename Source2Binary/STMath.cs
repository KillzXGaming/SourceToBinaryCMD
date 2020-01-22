using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace Source2Binary
{
    public class STMath
    {
        //From https://github.com/Ploaj/SSBHLib/blob/e37b0d83cd088090f7802be19b1d05ec998f2b6a/CrossMod/Tools/CrossMath.cs#L42
        //Seems to give good results
        public static Vector3 ToEulerAngles(double X, double Y, double Z, double W)
        {
            return ToEulerAngles(new Quaternion((float)X, (float)Y, (float)Z, (float)W));
        }

        public static Vector3 ToEulerAngles(float X, float Y, float Z, float W)
        {
            return ToEulerAngles(new Quaternion(X, Y, Z, W));
        }

        public static Quaternion FromEulerAngles(Vector3 rotation)
        {
            Quaternion xRotation = Quaternion.FromAxisAngle(Vector3.UnitX, rotation.X);
            Quaternion yRotation = Quaternion.FromAxisAngle(Vector3.UnitY, rotation.Y);
            Quaternion zRotation = Quaternion.FromAxisAngle(Vector3.UnitZ, rotation.Z);
            Quaternion q = (zRotation * yRotation * xRotation);

            if (q.W < 0)
                q *= -1;

            return q;
        }

        public static Vector3 ToEulerAngles(Quaternion q)
        {
            Matrix4 mat = Matrix4.CreateFromQuaternion(q);
            float x, y, z;
            y = (float)Math.Asin(Clamp(mat.M13, -1, 1));

            if (Math.Abs(mat.M13) < 0.99999)
            {
                x = (float)Math.Atan2(-mat.M23, mat.M33);
                z = (float)Math.Atan2(-mat.M12, mat.M11);
            }
            else
            {
                x = (float)Math.Atan2(mat.M32, mat.M22);
                z = 0;
            }
            return new Vector3(x, y, z) * -1;
        }

        public static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
