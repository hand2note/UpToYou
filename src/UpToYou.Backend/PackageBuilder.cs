using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    public ImmutableDictionary<string, string> CustomProperties { get; }
    public PackageBuilder(string sourceDirectory, string outputDirectory, PackageSpecs specs, ImmutableDictionary<string, string> customProperties) {
        SourceDirectory = sourceDirectory;
        OutputDirectory = outputDirectory;
        Specs = specs;
        CustomProperties = customProperties;
    }
}

internal class 
ProjectionBuilder {
    public string SourceDirectory { get; }
    public string OutputDirectory{ get; }
    public Package Package{ get; }
    public PackageProjectionSpecs ProjectionSpecs{ get; }
    public IHost Host {get;}
    public string HostRootUrl{ get; }
    public ILogger Logger { get; }
    public ProjectionBuilder(string sourceDirectory, string outputDirectory, Package package, PackageProjectionSpecs projectionSpecs, IHost host, string hostRootUrl, ILogger logger) {
        SourceDirectory = sourceDirectory;
        OutputDirectory = outputDirectory;
        Package = package;
        ProjectionSpecs = projectionSpecs;
        Host = host;
        HostRootUrl = hostRootUrl;
        Logger = logger;
    }
}
}
