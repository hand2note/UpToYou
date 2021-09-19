using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProtoBuf;

namespace UpToYou.Core {

public interface 
IHostClient {
    void DownloadFile(RelativePath path, IProgress<long> progress, CancellationToken cancellationToken, Stream outStream);
}

public class 
NullProgress : IProgress<long> {
    public static NullProgress Instance = new();
    public void Report(long value) {  }
}

}
