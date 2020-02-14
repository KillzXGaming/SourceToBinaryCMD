using System;
using System.Collections.Generic;
using System.IO;
using Syroot.NintenTools.Bfres;
using Toolbox.Library;
using Syroot.NintenTools.Bfres.GX2;
using System.Drawing;

namespace Source2Binary
{
    public class BFRESU : IConvertableBinary
    {
        public string CommandActivate => "-bfresU";

        public void GenerateBinary(FileSettings settings, string[] args)
        {

        }

        public static void BatchCreateTextures(string filePath)
        {
            Console.WriteLine("filePath " + filePath);

            ResFile resFile = new ResFile();

            Bitmap image = new Bitmap(filePath);
            var data = BitmapExtension.ImageToByte(image);
            BitmapExtension.ConvertBgraToRgba(data);

            string name = Path.GetFileNameWithoutExtension(filePath);
            resFile.Name = $"{name}.sbitemico";
            var tex = AddTexture(name, data, image.Width, image.Height, 1, GX2SurfaceFormat.TCS_R8_G8_B8_A8_SRGB);
            resFile.Textures.Add(tex.Name, tex);
            resFile.Alignment = 2048;
            resFile.VersionMajor = 4;
            resFile.VersionMajor2 = 5;
            resFile.VersionMinor = 0;
            resFile.VersionMinor2 = 3;

            if (!Directory.Exists("output"))
                Directory.CreateDirectory("output");

            var mem = new MemoryStream();
            resFile.Save(mem);
            var comp = EveryFileExplorer.YAZ0.Compress(mem.ToArray());
            File.WriteAllBytes($"output/{name}.sbitemico", comp);
        }

        public static Texture AddTexture(string name, byte[] image, int width, int height, int mipCount, GX2SurfaceFormat format) {
            return AddTexture(name, new List<byte[]>() { image }, width, height, mipCount, format);
        }

        public static Texture AddTexture(string name, List<byte[]> images, int width, int height, int mipCount, GX2SurfaceFormat format)
        {
            Texture tex = null;

            //Go through each texture as an array or depth surface
            List<byte[]> imageData = new List<byte[]>();
            List<byte[]> mipData = new List<byte[]>();
            for (int i = 0; i < images.Count; i++)
            {
                //Swizzle and create surface
                var surface = GX2.CreateGx2Texture(images[i], name,
                    (uint)GX2TileMode.Mode2dTiledThin1,
                    (uint)GX2AAMode.Mode1X,
                    (uint)width,
                    (uint)height,
                    (uint)1,
                    (uint)format,
                    (uint)0,
                    (uint)GX2SurfaceDim.Dim2D,
                    (uint)mipCount
                    );

                //Apply first surface
                if (i == 0)
                    tex = FromGx2Surface(surface, name);

                imageData.Add(surface.data);
                mipData.Add(surface.mipData);
            }

            tex.Data = Utils.CombineByteArray(imageData.ToArray());
            tex.MipData = Utils.CombineByteArray(mipData.ToArray());
            tex.ArrayLength = (uint)images.Count;
            imageData.Clear();
            mipData.Clear();
            return tex;
        }

        public static Texture FromGx2Surface(GX2.GX2Surface surf, string Name)
        {
            Texture tex = new Texture();
            tex.Name = Name;
            tex.Path = "";
            tex.AAMode = (GX2AAMode)surf.aa;
            tex.Alignment = (uint)surf.alignment;
            tex.ArrayLength = 1;
            tex.Data = surf.data;
            tex.MipData = surf.mipData;
            tex.Format = (GX2SurfaceFormat)surf.format;
            tex.Dim = (GX2SurfaceDim)surf.dim;
            tex.Use = (GX2SurfaceUse)surf.use;
            tex.TileMode = (GX2TileMode)surf.tileMode;
            tex.Swizzle = surf.swizzle;
            tex.Pitch = surf.pitch;
            tex.Depth = surf.depth;
            tex.MipCount = surf.numMips;
            tex.ViewMipCount = surf.numMips;
            tex.ViewSliceCount = 1;

            tex.MipOffsets = new uint[13];
            for (int i = 0; i < 13; i++)
            {
                if (i < surf.mipOffset.Length)
                    tex.MipOffsets[i] = surf.mipOffset[i];
            }
            tex.Height = surf.height;
            tex.Width = surf.width;
            tex.ArrayLength = 1;

            tex.Regs = new uint[5];

            if (surf.compSel == null || surf.compSel.Length != 4)
                surf.compSel = new byte[] { 0, 1, 2, 3 };

            //Set channels for settings and format
            tex.CompSelR = (GX2CompSel)surf.compSel[0];
            tex.CompSelG = (GX2CompSel)surf.compSel[1];
            tex.CompSelB = (GX2CompSel)surf.compSel[2];
            tex.CompSelA = (GX2CompSel)surf.compSel[3];
            SetChannelsByFormat((GX2SurfaceFormat)surf.format, tex);

            tex.UserData = new ResDict<UserData>();
            return tex;
        }

        public static void SetChannelsByFormat(GX2SurfaceFormat Format, Texture tex)
        {
            switch (Format)
            {
                case GX2SurfaceFormat.T_BC5_UNorm:
                    tex.CompSelR = GX2CompSel.ChannelR;
                    tex.CompSelG = GX2CompSel.ChannelG;
                    tex.CompSelB = GX2CompSel.Always1; //This is important for botw
                    tex.CompSelA = GX2CompSel.Always1;
                    break;
                case GX2SurfaceFormat.T_BC5_SNorm:
                    tex.CompSelR = GX2CompSel.ChannelR;
                    tex.CompSelG = GX2CompSel.ChannelG;
                    tex.CompSelB = GX2CompSel.Always0;
                    tex.CompSelA = GX2CompSel.Always1;
                    break;
                case GX2SurfaceFormat.T_BC4_SNorm:
                case GX2SurfaceFormat.T_BC4_UNorm:
                    tex.CompSelR = GX2CompSel.ChannelR;
                    tex.CompSelG = GX2CompSel.ChannelR;
                    tex.CompSelB = GX2CompSel.ChannelR;
                    tex.CompSelA = GX2CompSel.ChannelR;
                    break;
            }
        }
    }
}
