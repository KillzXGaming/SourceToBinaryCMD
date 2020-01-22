using System;
using System.Collections.Generic;
using System.Text;

namespace Source2Binary.Collada
{
    public class DAE
    {
        public STGenericScene Scene = new STGenericScene();

        public class ExportSettings
        {
            public bool SuppressConfirmDialog = false;
            public bool OptmizeZeroWeights = true;
            public bool UseOldExporter = false;
            public bool FlipTexCoordsVertical = true;
            public bool ExportTextures = true;

            public Version FileVersion = new Version();

            public ProgramPreset Preset = ProgramPreset.NONE;

            public string ImageExtension = ".png";
            public string ImageFolder = "";
        }

        public class ImportSettings
        {
            public bool FixDuplicateNames { get; set; } = true;
        }

        public class Version
        {
            public int Major = 1;
            public int Minor = 4;
            public int Micro = 1;
        }
    }
}
