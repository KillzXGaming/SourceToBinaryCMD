using System;
using System.Collections.Generic;
using System.IO;
using Source2Binary.IO;
using System.Runtime.InteropServices;

namespace Source2Binary.Dds
{
    public class DDS
    {
        #region Constants

        public const uint FOURCC_DXT1 = 0x31545844;
        public const uint FOURCC_DXT2 = 0x32545844;
        public const uint FOURCC_DXT3 = 0x33545844;
        public const uint FOURCC_DXT4 = 0x34545844;
        public const uint FOURCC_DXT5 = 0x35545844;
        public const uint FOURCC_ATI1 = 0x31495441;
        public const uint FOURCC_BC4U = 0x55344342;
        public const uint FOURCC_BC4S = 0x53344342;
        public const uint FOURCC_BC5U = 0x55354342;
        public const uint FOURCC_BC5S = 0x53354342;
        public const uint FOURCC_DX10 = 0x30315844;

        public const uint FOURCC_ATI2 = 0x32495441;
        public const uint FOURCC_RXGB = 0x42475852;

        // RGBA Masks
        private static int[] A1R5G5B5_MASKS = { 0x7C00, 0x03E0, 0x001F, 0x8000 };
        private static int[] X1R5G5B5_MASKS = { 0x7C00, 0x03E0, 0x001F, 0x0000 };
        private static int[] A4R4G4B4_MASKS = { 0x0F00, 0x00F0, 0x000F, 0xF000 };
        private static int[] X4R4G4B4_MASKS = { 0x0F00, 0x00F0, 0x000F, 0x0000 };
        private static int[] R5G6B5_MASKS = { 0xF800, 0x07E0, 0x001F, 0x0000 };
        private static int[] R8G8B8_MASKS = { 0xFF0000, 0x00FF00, 0x0000FF, 0x000000 };
        private static uint[] A8B8G8R8_MASKS = { 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000 };
        private static int[] X8B8G8R8_MASKS = { 0x000000FF, 0x0000FF00, 0x00FF0000, 0x00000000 };
        private static uint[] A8R8G8B8_MASKS = { 0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000 };
        private static int[] X8R8G8B8_MASKS = { 0x00FF0000, 0x0000FF00, 0x000000FF, 0x00000000 };

        private static int[] L8_MASKS = { 0x000000FF, 0x0000, };
        private static int[] A8L8_MASKS = { 0x000000FF, 0x0F00, };

        #endregion

        #region enums

        public enum CubemapFace
        {
            PosX,
            NegX,
            PosY,
            NegY,
            PosZ,
            NegZ
        }

        [Flags]
        public enum DDSD : uint
        {
            CAPS = 0x00000001,
            HEIGHT = 0x00000002,
            WIDTH = 0x00000004,
            PITCH = 0x00000008,
            PIXELFORMAT = 0x00001000,
            MIPMAPCOUNT = 0x00020000,
            LINEARSIZE = 0x00080000,
            DEPTH = 0x00800000
        }
        [Flags]
        public enum DDPF : uint
        {
            ALPHAPIXELS = 0x00000001,
            ALPHA = 0x00000002,
            FOURCC = 0x00000004,
            RGB = 0x00000040,
            YUV = 0x00000200,
            LUMINANCE = 0x00020000,
        }
        [Flags]
        public enum DDSCAPS : uint
        {
            COMPLEX = 0x00000008,
            TEXTURE = 0x00001000,
            MIPMAP = 0x00400000,
        }
        [Flags]
        public enum DDSCAPS2 : uint
        {
            CUBEMAP = 0x00000200,
            CUBEMAP_POSITIVEX = 0x00000400 | CUBEMAP,
            CUBEMAP_NEGATIVEX = 0x00000800 | CUBEMAP,
            CUBEMAP_POSITIVEY = 0x00001000 | CUBEMAP,
            CUBEMAP_NEGATIVEY = 0x00002000 | CUBEMAP,
            CUBEMAP_POSITIVEZ = 0x00004000 | CUBEMAP,
            CUBEMAP_NEGATIVEZ = 0x00008000 | CUBEMAP,
            CUBEMAP_ALLFACES = (CUBEMAP_POSITIVEX | CUBEMAP_NEGATIVEX |
                                  CUBEMAP_POSITIVEY | CUBEMAP_NEGATIVEY |
                                  CUBEMAP_POSITIVEZ | CUBEMAP_NEGATIVEZ),
            VOLUME = 0x00200000
        }

        #endregion

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Header
        {
            public Magic Magic = "DDS ";
            public uint Size;
            public uint Flags;
            public uint Height;
            public uint Width;
            public uint PitchOrLinearSize;
            public uint Depth;
            public uint MipCount;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            public uint[] Reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class DDSPFHeader
        {
            public uint Size;
            public uint Flags;
            public uint FourCC;
            public uint RgbBitCount;
            public uint RBitMask;
            public uint GBitMask;
            public uint BBitMask;
            public uint ABitMask;
            public uint Caps1;
            public uint Caps2;
            public uint Caps3;
            public uint Caps4;
            public uint Reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class DX10Header
        {
            public uint DxgiFormat;
            public uint ResourceDim;
            public uint MiscFlags1;
            public uint ArrayCount;
            public uint MiscFlags2;
        }

        public string FileName { get; set; }

        public Header MainHeader;
        public DDSPFHeader PfHeader;
        public DX10Header Dx10Header;

        public byte[] ImageData;

        public DDS(string fileName) { Load(fileName); }

        public SBTexFormat ToGenericFormat()
        {
            if (Dx10Header != null)
            {
                switch((DXGI_FORMAT)Dx10Header.DxgiFormat)
                {
                    case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM: return SBTexFormat.BC1_Unorm;
                    case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB: return SBTexFormat.BC1_Srgb;
                    case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM: return SBTexFormat.BC2_Unorm;
                    case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB: return SBTexFormat.BC2_Srgb;
                    case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM: return SBTexFormat.BC3_Unorm;
                    case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB: return SBTexFormat.BC3_Srgb;
                    case DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM: return SBTexFormat.BC4_Unorm;
                    case DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM: return SBTexFormat.BC4_Snorm;
                    case DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM: return SBTexFormat.BC5_Unorm;
                    case DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM: return SBTexFormat.BC5_Snorm;
                    case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM: return SBTexFormat.RGBA8_Srgb;
                    case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB: return SBTexFormat.RGBA8_Unorm;
                    default:
                        throw new Exception("Unsupported DXGI Format! " + (DXGI_FORMAT)Dx10Header.DxgiFormat);
                }
            }
            else
            {
                Console.WriteLine(PfHeader.FourCC);
                switch (PfHeader.FourCC)
                {
                    case FOURCC_DXT1:
                        return SBTexFormat.BC1_Unorm;
                    case FOURCC_DXT2:
                    case FOURCC_DXT3:
                        return SBTexFormat.BC2_Unorm;
                    case FOURCC_DXT4:
                    case FOURCC_DXT5:
                        return SBTexFormat.BC3_Unorm;
                    case FOURCC_ATI1:
                    case FOURCC_BC4U:
                        return SBTexFormat.BC4_Unorm;
                    case FOURCC_ATI2:
                    case FOURCC_BC5U:
                        return SBTexFormat.BC5_Unorm;
                    case FOURCC_BC5S:
                        return SBTexFormat.BC5_Snorm;
                    case FOURCC_RXGB:
                        return SBTexFormat.RGBA8_Unorm;
                    default:
                        return SBTexFormat.RGBA8_Unorm;
                }
            }
        }

        public void Load(string fileName) {
            FileName = fileName;
            Load(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        public void Load(System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream))
            {
                MainHeader = reader.ReadStruct<Header>();
                PfHeader = reader.ReadStruct<DDSPFHeader>();
                if (PfHeader.FourCC == FOURCC_DX10)
                    Dx10Header = reader.ReadStruct<DX10Header>();

                int Dx10Size = Dx10Header != null ? 20 : 0;

                reader.TemporarySeek((int)(4 + MainHeader.Size + Dx10Size), SeekOrigin.Begin);
                ImageData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            }
        }

        public List<byte[]> GetImageData()
        {
            return new List<byte[]>() { ImageData };
        }
    }
}
