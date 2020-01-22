using System;
using System.Collections.Generic;
using System.Text;

namespace Source2Binary
{
    public class Utility
    {
        public static string RenameDuplicateString(List<string> strings, string oldString, int index = 0, int numDigits = 1)
        {
            if (strings.Contains(oldString))
            {
                string key = $"{index++}";
                if (numDigits == 2)
                    key = string.Format("{0:00}", key);

                string NewString = $"{oldString}_{key}";
                if (strings.Contains(NewString))
                    return RenameDuplicateString(strings, oldString, index, numDigits);
                else
                    return NewString;
            }

            return oldString;
        }
    }
}
