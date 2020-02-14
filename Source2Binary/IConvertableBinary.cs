using System;
using System.Collections.Generic;
using System.Text;

namespace Source2Binary
{
    public interface IConvertableBinary
    {
        string CommandActivate { get; }
        void GenerateBinary(FileSettings settings, string[] args);
    }
}
