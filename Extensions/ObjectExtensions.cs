using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreFramework.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// If null, returns empty string, else returns object.ToString
        /// </summary>
        public static string ForceToString(this object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }
            else
            {
                return obj.ToString();
            }
        }
    }
}
