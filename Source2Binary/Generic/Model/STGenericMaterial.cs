using System;
using System.Collections.Generic;
using System.Text;

namespace Source2Binary
{
    public class STGenericMaterial
    {
        public string Name { get; set; }

        public List<STGenericTextureMap> TextureMaps = new List<STGenericTextureMap>();
    }
}
