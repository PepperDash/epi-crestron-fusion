using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace DynFusion
{
    public static partial class Extensions
    {
        public static string NullIfEmpty(this string @this)
        {
            return @this == "" ? null : @this;
        }
    }
}