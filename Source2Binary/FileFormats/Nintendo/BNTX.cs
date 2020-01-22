using System;
using System.Collections.Generic;
using System.Text;
using Syroot.NintenTools.NSW.Bntx;
using Syroot.NintenTools.NSW.Bntx.GFX;
using System.Linq;
using Source2Binary.Dds;

namespace Source2Binary
{
    public class BNTX : IConvertableBinary
    {
        public void GenerateBinary(System.IO.Stream stream, string[] args)
        {
            List<DDS> Textures = new List<DDS>();
            for (int i = 0; i < args.Length; i++)
                Textures.Add(new DDS(args[i]));

            BntxFile bntx = new BntxFile();
            bntx.Target = new char[] { 'N', 'X', ' ', ' ' };
            bntx.Name = "textures";
            bntx.Alignment = 0xC;
            bntx.TargetAddressSize = 0x40;
            bntx.VersionMajor = 0;
            bntx.VersionMajor2 = 4;
            bntx.VersionMinor = 0;
            bntx.VersionMinor2 = 0;
            bntx.Textures = new List<Texture>();
            bntx.TextureDict = new ResDict();
            bntx.RelocationTable = new RelocationTable();
            bntx.Flag = 0;

            foreach (var file in Textures)
                bntx.Textures.Add(FromDDS(file));

            bntx.Save(stream);
        }

        private Texture FromDDS(DDS dds)
        {
            TextureConfig config = new TextureConfig();
            config.Width = dds.MainHeader.Width;
            config.Height = dds.MainHeader.Height;
            config.Format = FormatConverter[dds.ToGenericFormat()];
            config.MipCount = dds.MainHeader.MipCount;
            config.Name = System.IO.Path.GetFileNameWithoutExtension(dds.FileName);

            Console.WriteLine("SurfaceFormat " + config.Format);

            return FromBitMap(dds.GetImageData(), config);
        }

        public Dictionary<SBTexFormat, SurfaceFormat> FormatConverter = new Dictionary<SBTexFormat, SurfaceFormat>()
        {
            { SBTexFormat.BC1_Unorm, SurfaceFormat.BC1_UNORM },
            { SBTexFormat.BC1_Srgb, SurfaceFormat.BC1_SRGB },
            { SBTexFormat.BC2_Unorm, SurfaceFormat.BC2_UNORM },
            { SBTexFormat.BC2_Srgb, SurfaceFormat.BC2_SRGB },
            { SBTexFormat.BC3_Unorm, SurfaceFormat.BC3_UNORM },
            { SBTexFormat.BC3_Srgb, SurfaceFormat.BC3_SRGB },
            { SBTexFormat.BC4_Unorm, SurfaceFormat.BC4_UNORM },
            { SBTexFormat.BC4_Snorm, SurfaceFormat.BC4_SNORM },
            { SBTexFormat.BC5_Unorm, SurfaceFormat.BC5_UNORM },
            { SBTexFormat.BC5_Snorm, SurfaceFormat.BC5_SNORM },
            { SBTexFormat.RGBA8_Srgb, SurfaceFormat.R8_G8_B8_A8_SRGB },
            { SBTexFormat.RGBA8_Unorm, SurfaceFormat.R8_G8_B8_A8_UNORM },
        };

        public class TextureConfig
        {
            public string Name;
            public uint AccessFlags = 0x20;
            public uint MipCount;
            public uint Depth = 1;
            public uint Width;
            public uint Height;
            public uint Flags;
            public uint Swizzle;
            public uint SampleCount = 1;
            public uint Pitch = 32;
            public uint[] Regs;

            public uint TextureLayout;
            public uint TextureLayout2 = 0x010007;

            public int sparseResidency = 0; //false
            public int sparseBinding = 0; //false

            public bool IsSRGB = true;

            public SurfaceFormat Format;

            public SurfaceDim SurfaceDim = SurfaceDim.Dim2D;
            public TileMode TileMode = TileMode.Default;
            public Dim Dim = Dim.Dim2D;
            public ChannelType RedComp = ChannelType.Red;
            public ChannelType GreenComp = ChannelType.Green;
            public ChannelType BlueComp = ChannelType.Blue;
            public ChannelType AlphaComp = ChannelType.Alpha;
        }

        public Texture FromBitMap(List<byte[]> arrayFaces, TextureConfig config)
        {
            Texture tex = new Texture();
            tex.Name = config.Name;
            tex.Format = config.Format;
            tex.Width = config.Width;
            tex.Height = config.Height;
            tex.ChannelRed = config.RedComp;
            tex.ChannelGreen = config.GreenComp;
            tex.ChannelBlue = config.BlueComp;
            tex.ChannelAlpha = config.AlphaComp;
            tex.sparseBinding = config.sparseBinding;
            tex.sparseResidency = config.sparseResidency;
            tex.AccessFlags = config.AccessFlags;
            tex.MipCount = config.MipCount;
            tex.Depth = config.Depth;
            tex.Dim = config.Dim;
            tex.Flags = config.Flags;
            tex.TileMode = config.TileMode;
            tex.textureLayout = config.TextureLayout;
            tex.textureLayout2 = config.TextureLayout2;
            tex.Swizzle = config.Swizzle;
            tex.SurfaceDim = config.SurfaceDim;
            tex.SampleCount = config.SampleCount;
            tex.Regs = config.Regs;
            tex.Pitch = config.Pitch;
            tex.MipOffsets = new long[tex.MipCount];
            tex.TextureData = new List<List<byte[]>>();

            for (int i = 0; i < arrayFaces.Count; i++)
            {
                List<byte[]> mipmaps = SwizzleSurfaceMipMaps(tex, arrayFaces[i], tex.MipCount);
                tex.TextureData.Add(mipmaps);

                //Combine mip map data
                byte[] combinedMips = ByteUtils.CombineArray(mipmaps.ToArray());
                tex.TextureData[i][0] = combinedMips;
            }

            return tex;
        }

        public static List<byte[]> SwizzleSurfaceMipMaps(Texture tex, byte[] data, uint MipCount)
        {
            int blockHeightShift = 0;
            int target = 1;
            uint Pitch = 0;
            uint SurfaceSize = 0;
            uint blockHeight = 0;

            uint blk_dim = Formats.blk_dims((uint)((int)tex.Format >> 8));
            uint blkWidth = blk_dim >> 4;
            uint blkHeight = blk_dim & 0xF;

            uint bpp = Formats.bpps((uint)((int)tex.Format >> 8));

            uint linesPerBlockHeight = 0;

            if (tex.TileMode == TileMode.LinearAligned)
            {
                blockHeight = 1;
                tex.BlockHeightLog2 = 0;
                tex.Alignment = 1;

                linesPerBlockHeight = 1;
                tex.ReadTextureLayout = 0;
            }
            else
            {
                blockHeight = TegraX1Swizzle.GetBlockHeight(TegraX1Swizzle.DIV_ROUND_UP(tex.Height, blkHeight));
                tex.BlockHeightLog2 = (uint)Convert.ToString(blockHeight, 2).Length - 1;
                tex.Alignment = 512;
                tex.ReadTextureLayout = 1;

                linesPerBlockHeight = blockHeight * 8;

            }

            List<byte[]> mipmaps = new List<byte[]>();
            for (int mipLevel = 0; mipLevel < MipCount; mipLevel++)
            {
                var result = TextureHelper.GetCurrentMipSize(tex.Width, tex.Height, blkWidth, blkHeight, bpp, mipLevel);
                uint offset = result.Item1;
                uint size = result.Item2;


                byte[] data_ = ByteUtils.SubArray(data, offset, size);

                uint width_ = Math.Max(1, tex.Width >> mipLevel);
                uint height_ = Math.Max(1, tex.Height >> mipLevel);
                uint depth_ = Math.Max(1, tex.Depth >> mipLevel);

                uint width__ = TegraX1Swizzle.DIV_ROUND_UP(width_, blkWidth);
                uint height__ = TegraX1Swizzle.DIV_ROUND_UP(height_, blkHeight);

                byte[] AlignedData = new byte[(TegraX1Swizzle.round_up(SurfaceSize, (uint)tex.Alignment) - SurfaceSize)];
                SurfaceSize += (uint)AlignedData.Length;

                //  Console.WriteLine("SurfaceSize Aligned " + AlignedData);

                Console.WriteLine("MipOffsets " + SurfaceSize);
                Console.WriteLine("size " + size);

                tex.MipOffsets[mipLevel] = SurfaceSize;
                if (tex.TileMode == TileMode.LinearAligned)
                {
                    Pitch = width__ * bpp;

                    if (target == 1)
                        Pitch = TegraX1Swizzle.round_up(width__ * bpp, 32);

                    SurfaceSize += Pitch * height__;
                }
                else
                {
                    if (TegraX1Swizzle.pow2_round_up(height__) < linesPerBlockHeight)
                        blockHeightShift += 1;

                    Pitch = TegraX1Swizzle.round_up(width__ * bpp, 64);
                    SurfaceSize += Pitch * TegraX1Swizzle.round_up(height__, Math.Max(1, blockHeight >> blockHeightShift) * 8);
                }

                byte[] SwizzledData = TegraX1Swizzle.swizzle(width_, height_, depth_, blkWidth, blkHeight, 1, target, bpp, (uint)tex.TileMode, (int)Math.Max(0, tex.BlockHeightLog2 - blockHeightShift), data_);
                mipmaps.Add(AlignedData.Concat(SwizzledData).ToArray());
            }
            tex.ImageSize = SurfaceSize;

            return mipmaps;
        }

        public class Formats
        {
            public enum BNTXImageFormat
            {
                IMAGE_FORMAT_INVALID = 0x0,
                IMAGE_FORMAT_R8_G8_B8_A8 = 0x0b,
                IMAGE_FORMAT_R5_G6_B5 = 0x07,
                IMAGE_FORMAT_R8 = 0x02,
                IMAGE_FORMAT_R8_G8 = 0x09,
                IMAGE_FORMAT_BC1 = 0x1a,
                IMAGE_FORMAT_BC2 = 0x1b,
                IMAGE_FORMAT_BC3 = 0x1c,
                IMAGE_FORMAT_BC4 = 0x1d,
                IMAGE_FORMAT_BC5 = 0x1e,
                IMAGE_FORMAT_BC6 = 0x1f,
                IMAGE_FORMAT_BC7 = 0x20,
            };

            public enum BNTXImageTypes
            {
                UNORM = 0x01,
                SNORM = 0x02,
                SRGB = 0x06,
            };

            public static uint blk_dims(uint format)
            {
                switch (format)
                {
                    case (uint)BNTXImageFormat.IMAGE_FORMAT_BC1:
                    case (uint)BNTXImageFormat.IMAGE_FORMAT_BC2:
                    case (uint)BNTXImageFormat.IMAGE_FORMAT_BC3:
                    case (uint)BNTXImageFormat.IMAGE_FORMAT_BC4:
                    case (uint)BNTXImageFormat.IMAGE_FORMAT_BC5:
                    case (uint)BNTXImageFormat.IMAGE_FORMAT_BC6:
                    case (uint)BNTXImageFormat.IMAGE_FORMAT_BC7:
                    case 0x2d:
                        return 0x44;

                    case 0x2e: return 0x54;
                    case 0x2f: return 0x55;
                    case 0x30: return 0x65;
                    case 0x31: return 0x66;
                    case 0x32: return 0x85;
                    case 0x33: return 0x86;
                    case 0x34: return 0x88;
                    case 0x35: return 0xa5;
                    case 0x36: return 0xa6;
                    case 0x37: return 0xa8;
                    case 0x38: return 0xaa;
                    case 0x39: return 0xca;
                    case 0x3a: return 0xcc;

                    default: return 0x11;
                }
            }

            public static uint bpps(uint format)
            {
                switch (format)
                {
                    case (uint)BNTXImageFormat.IMAGE_FORMAT_R8_G8_B8_A8: return 4;
                    case (uint)BNTXImageFormat.IMAGE_FORMAT_R8: return 1;

                    case (uint)BNTXImageFormat.IMAGE_FORMAT_R5_G6_B5:
                    case (uint)BNTXImageFormat.IMAGE_FORMAT_R8_G8:
                        return 2;

                    case (uint)BNTXImageFormat.IMAGE_FORMAT_BC1:
                    case (uint)BNTXImageFormat.IMAGE_FORMAT_BC4:
                        return 8;

                    case (uint)BNTXImageFormat.IMAGE_FORMAT_BC2:
                    case (uint)BNTXImageFormat.IMAGE_FORMAT_BC3:
                    case (uint)BNTXImageFormat.IMAGE_FORMAT_BC5:
                    case (uint)BNTXImageFormat.IMAGE_FORMAT_BC6:
                    case (uint)BNTXImageFormat.IMAGE_FORMAT_BC7:
                    case 0x2e:
                    case 0x2f:
                    case 0x30:
                    case 0x31:
                    case 0x32:
                    case 0x33:
                    case 0x34:
                    case 0x35:
                    case 0x36:
                    case 0x37:
                    case 0x38:
                    case 0x39:
                    case 0x3a:
                        return 16;
                    default: return 0x00;
                }
            }
        }
    }
}
