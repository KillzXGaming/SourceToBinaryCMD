using System;
using System.IO;
using System.Collections.Generic;
using Source2Binary.Dds;
using System.Linq;

namespace Source2Binary
{
    public class FileSettings
    {
        public List<string> inputFiles = new List<string>();
        public List<string> outputFiles = new List<string>();

        public string inputDir;

        private string outputdir;
        public string outputDir
        {
            get
            {
                if (outputdir == string.Empty)
                    return Path.GetDirectoryName(outputFiles[0]);
                return outputdir;
            }
            set
            {
                outputdir = value;
            }
        }

        private string ext;
        public string OutputExtension
        {
            get 
            {
                if (ext == string.Empty)
                    return Path.GetExtension(outputFiles[0]);
                return ext; 
            }
            set
            {
                ext = value;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            foreach (var arg in args)
             BFRESU.BatchCreateTextures(arg);
            return;

            FileSettings settings = new FileSettings();
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-h":
                    case "--help":
                    case "-help":
                        Console.WriteLine(
                            "How to use:\n" +
                            "-i --input (Selects input file)\n" +
                            "-id --input-dir (Selects input directory)\n" +
                            "-o --output (Selects output file)\n" +
                            "-od --output-dir (Selects output directory)\n");
                        return;
                    case "-i":
                    case "--input":
                        settings.inputFiles.Add(args[i + 1]);
                        break;
                    case "-o":
                    case "--output":
                        settings.outputFiles.Add(args[i + 1]);
                        break;
                    case "-id":
                    case "--input-dir":
                        settings.inputDir = args[i + 1];
                        break;
                    case "-od":
                    case "--output-dir":
                        settings.outputDir = args[i + 1];
                        break;
                    case "-e":
                    case "--output-ext":
                        settings.OutputExtension = args[i + 1];
                        break;
                }
            }

            if (settings.inputFiles.Count == 0 && 
                settings.inputDir == string.Empty) {
                Console.WriteLine("You must select a valid input file or directory (-i/-id)");
                return;
            }

            if (settings.outputFiles.Count == 0 &&
                settings.outputDir == string.Empty) {
                Console.WriteLine("You must select a valid output file or directory (-o/-od)");
                return;
            }

            var formats = FindFormats();
            bool executed = false;
            foreach (IConvertableBinary conv in formats)
            {
                if (args.Contains(conv.CommandActivate)) {
                    conv.GenerateBinary(settings, args);
                    executed = true;
                }
            }

            if (!executed)
            {
                Console.WriteLine("No formats selected. Choose one of the supported formats.");
                foreach (IConvertableBinary conv in formats)
                    Console.WriteLine(conv.CommandActivate);
            }
            //   foreach (var arg in args)
            //      BFRESU.BatchCreateTextures(arg);
            return;

            args = new string[4]
            {
                "-i", "cube.dae", "-o", "cube.bfres"
            };

            BFRES bfres = new BFRES();
            bfres.GenerateBinary(new FileSettings(), args);

            Console.Read();
            return;
            /* if (args.Length == 0)
             {
                 Console.WriteLine("Drag some DDS files to test this out! Note this will be able to convert many formats, current bntx is only supported at this time!");
                 Console.Read();
                 return;
             }*/
        }

        static IEnumerable<IConvertableBinary> FindFormats()
        {
            var type = typeof(IConvertableBinary);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p));

            var convertables = new List<IConvertableBinary>();
            foreach (Type t in types)
            {
                Type[] interfaces_array = t.GetInterfaces();
                for (int i = 0; i < interfaces_array.Length; i++)
                {
                    if (interfaces_array[i] == typeof(IConvertableBinary))
                        convertables.Add((IConvertableBinary)Activator.CreateInstance(t));
                }
            }

            return convertables;
        }
    }
}
