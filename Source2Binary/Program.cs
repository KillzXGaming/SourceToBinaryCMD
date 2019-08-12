using System;
using System.IO;
using System.Collections.Generic;
using Source2Binary.Dds;
using System.Linq;

namespace Source2Binary
{
    class Program
    {
        static void Main(string[] args)
        {
            args = new string[1] { "test.dds" };

            if (args.Length == 0)
                Console.WriteLine("Drag some DDS files to test this out! Note this will be able to convert many formats, current bntx is only supported at this time!");

            BNTX bntx = new BNTX();
            using (var fileStream = new FileStream("test.bntx", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                bntx.GenerateBinary(fileStream, args.ToList());
            }

            Console.Read();
        }
    }
}
