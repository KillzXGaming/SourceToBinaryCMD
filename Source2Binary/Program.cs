using System;
using System.IO;

namespace Source2Binary
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            using (var fileStream = new FileStream("test.bntx", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                BNTX bntx = new BNTX();
                bntx.GenerateBinary(fileStream);
                Console.WriteLine("Generated BNTX successfully!");
            }

            Console.Read();
        }
    }
}
