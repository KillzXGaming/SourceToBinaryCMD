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
 
            BNTX bntx = new BNTX();
            using (var fileStream = new FileStream("test.bntx", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                bntx.GenerateBinary(fileStream, args);
            }

        }
    }
}
