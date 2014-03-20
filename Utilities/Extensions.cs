using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NowReadable.Utilities
{
    public static class Extensions
    {
        public static void FuckingAdd(this IsolatedStorageSettings isss, string key, object value)
        {
            if (isss.Contains(key))
            {
                isss.Remove(key);
            }
            isss.Add(key, value);
        }
    }
}
