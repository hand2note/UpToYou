using System;
using System.Collections.Generic;
using UpToYou.Backend;

namespace UpToYou.Tests.UpdateTestCases {

internal interface IProjectionTestCase { 
    Version Version { get; }
    TestFilesSet PackageFiles { get; }
    PackageSpecs PackageSpecs { get; }
    PackageProjectionSpecs ProjectionSpecs { get; }
    List<IProjectionTestCase>? PreviousProjections { get; }
    string VersionProvider { get; }
}


}