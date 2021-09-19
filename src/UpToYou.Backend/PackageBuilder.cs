using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UpToYou.Core;

namespace UpToYou.Backend {

internal class
PackageBuilder {
    public string SourceDirectory { get; }
    public string OutputDirectory{ get; }
    public PackageSpecs Specs { get; }
    public Dictionary<string, string>? CustomProperties { get; }

    public PackageBuilder(string sourceDirectory, string outputDirectory, PackageSpecs specs,   Dictionary<string, string>? customProperties= null) =>
        (SourceDirectory, OutputDirectory, Specs, CustomProperties) = (sourceDirectory, outputDirectory, specs, customProperties);
}

internal class 
ProjectionBuilder {
    public string SourceDirectory { get; }
    public string OutputDirectory{ get; }
    public Package Package{ get; }
    public PackageProjectionSpecs ProjectionSpecs{ get; }
    public IHost Host {get;}
    public string HostRootUrl{ get; }
    public ILogger Log { get; }
    public ProjectionBuilder(string sourceDirectory, string outputDirectory, Package package, PackageProjectionSpecs projectionSpecs, IHost host, string hostRootUrl, ILogger log) {
        SourceDirectory = sourceDirectory;
        OutputDirectory = outputDirectory;
        Package = package;
        ProjectionSpecs = projectionSpecs;
        Host = host;
        HostRootUrl = hostRootUrl;
        Log = log;
    }
}
}
