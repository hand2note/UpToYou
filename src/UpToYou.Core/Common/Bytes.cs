using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpToYou.Core{
internal static class
Bytes {
    public static double 
    BytesToMegabytes(this long bytes, int roundingDecimals = 2) => Math.Round(bytes / 1_000_000d, roundingDecimals);

    public static double 
    BytesToMegabytes(this int bytes, int roundingDecimals = 2) => Math.Round(bytes / 1_000_000d, roundingDecimals);
}
}
