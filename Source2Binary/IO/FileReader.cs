using System;
using System.Collections.Generic;
using System.Text;
using Syroot.BinaryData;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Linq;

namespace Source2Binary.IO
{
    public class FileReader : BinaryDataReader
    {
        public FileReader(Stream stream, bool leaveOpen = false)
            : base(stream, Encoding.ASCII, leaveOpen)
        {
            this.Position = 0;
        }

        public FileReader(string fileName)
             : this(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            this.Position = 0;
        }

        public FileReader(byte[] data)
             : this(new MemoryStream(data))
        {
            this.Position = 0;
        }

        //From kuriimu https://github.com/IcySon55/Kuriimu/blob/master/src/Kontract/IO/BinaryReaderX.cs#L40
        public T ReadStruct<T>() => ReadBytes(Marshal.SizeOf<T>()).BytesToStruct<T>(ByteOrder == ByteOrder.BigEndian);
        public List<T> ReadMultipleStructs<T>(int count) => Enumerable.Range(0, count).Select(_ => ReadStruct<T>()).ToList();

        public void SeekBegin(uint Offset) { Seek(Offset, SeekOrigin.Begin); }
        public void SeekBegin(int Offset) { Seek(Offset, SeekOrigin.Begin); }
        public void SeekBegin(long Offset) { Seek(Offset, SeekOrigin.Begin); }

        public bool CheckSignature(uint Identifier, long position = 0)
        {
            if (Position + 4 >= BaseStream.Length || position < 0 || position + 4 >= BaseStream.Length)
                return false;

            Position = position;
            uint signature = ReadUInt32();

            //Reset position
            Position = 0;

            return signature == Identifier;
        }
    }
}
