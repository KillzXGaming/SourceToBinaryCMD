using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Source2Binary
{
    public static class BitmapExtension
    {
        /// <summary>
        /// Converts a bitmap's image data into a byte array.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static byte[] ImageToByte(Bitmap bitmap)
        {
            BitmapData bmpdata = null;

            try
            {
                bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                int numbytes = bmpdata.Stride * bitmap.Height;
                byte[] bytedata = new byte[numbytes];
                IntPtr ptr = bmpdata.Scan0;

                Marshal.Copy(ptr, bytedata, 0, numbytes);

                return bytedata;
            }
            finally
            {
                if (bmpdata != null)
                    bitmap.UnlockBits(bmpdata);
            }
        }

        /// <summary>
        /// Swaps the blue and red channels from a byte array of image data
        /// The image given must have 4 bytes used per pixel.
        /// </summary>
        /// <param name="bytes"></param>
        public static void ConvertBgraToRgba(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i += 4)
            {
                var temp = bytes[i];
                bytes[i] = bytes[i + 2];
                bytes[i + 2] = temp;
            }
        }
    }
}
