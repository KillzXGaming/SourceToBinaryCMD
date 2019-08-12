using System;
using System.Collections.Generic;
using System.Text;

namespace Source2Binary
{
    public interface IConvertableBinary
    {
        void GenerateBinary(System.IO.Stream stream, List<string> sourceFies);
    }
}
