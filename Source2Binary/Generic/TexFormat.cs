using System;
using System.Collections.Generic;
using System.Text;

namespace Source2Binary
{
    /// <summary>
    /// Generic formats used across the tool 
    /// for cross conerting of multiple image formats.
    /// </summary>
    public enum SBTexFormat
    {
        RGBA8_Srgb,
        RGBA8_Unorm,
        RGB565,
        RGBA4441,
        RGB8,
        BC1_Unorm,
        BC1_Srgb,
        BC2_Unorm,
        BC2_Srgb,
        BC3_Unorm,
        BC3_Srgb,
        BC4_Unorm,
        BC4_Snorm,
        BC5_Unorm,
        BC5_Snorm,
    }
}
