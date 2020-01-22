using System.Text;
using Syroot.BinaryData;
using System.IO;

namespace Source2Binary.IO
{
    public class FileWriter : BinaryDataWriter
    {
        public FileWriter(Stream stream, bool leaveOpen = false)
        : base(stream, Encoding.ASCII, leaveOpen)
        {
        }

        public FileWriter(string fileName)
             : this(new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
        }

        public FileWriter(byte[] data)
             : this(new MemoryStream(data))
        {
        }

        public void SeekBegin(uint Offset) { Seek(Offset, SeekOrigin.Begin); }
        public void SeekBegin(int Offset) { Seek(Offset, SeekOrigin.Begin); }
        public void SeekBegin(long Offset) { Seek(Offset, SeekOrigin.Begin); }

        public void WriteStruct<T>(T item) => Write(item.StructToBytes(ByteOrder == ByteOrder.BigEndian));
    }
}
