﻿using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Source2Binary.IO
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Magic
    {
        int value;
        public static implicit operator string(Magic magic) => Encoding.ASCII.GetString(BitConverter.GetBytes(magic.value));
        public static implicit operator Magic(string s) => new Magic { value = BitConverter.ToInt32(Encoding.ASCII.GetBytes(s), 0) };
    }
}
