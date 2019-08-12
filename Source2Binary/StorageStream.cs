using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Source2Binary
{
    public class StorageStream : Stream
    {
        Stream baseStream;
        readonly long length;
        readonly long baseOffset;

        public StorageStream(Stream baseStream, long offset=0, long length = 0)
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            baseStream.Position = baseOffset + offset + Position;
            int read = baseStream.Read(buffer, offset, (int)Math.Min(count, length - Position));
            Position += read;
            return read;
        }

        public override long Length => length;
        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override bool CanSeek => true;
        public override long Position { get; set; }
        public override void Flush() => baseStream.Flush();

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin: return Position = offset;
                case SeekOrigin.Current: return Position += offset;
                case SeekOrigin.End: return Position = length + offset;
            }
            throw new ArgumentException("origin is invalid");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
