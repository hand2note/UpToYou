using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using NUnit.Framework;
using UpToYou.Backend.Tests;
using UpToYou.Core;
using UpToYou.Core.Tests;

namespace UpToYou.Backend.Runner.Tests {
public static class PushUpdateTests {
    
    [Test]
    public static void 
    PushUpdate01() {
        var host = new InMemoryHost();
        var testDirectory = nameof(PushUpdate01);
        testDirectory.ClearDirectoryIfExists();
        "[//]: #1.0.0.0\nTest package update notes".WriteAllTextIntoFile("updateNotes.txt.en.md");
       "Test.exe".CreateTestFile().SetFileVersion("1.0.0.0");
      var packageId = PushUpdateHelper.PushUpdate(
           workingDirectory:testDirectory.AppendPath("_work"),
           sourceDirectory: testDirectory,
           packageSpecs: new PackageSpecs(
               packageName: "TestPackage",
               files: "Test.exe".ToRelativeGlob().ToSingleImmutableList(),
               excludedFiles: ImmutableList<RelativeGlob>.Empty, 
               versionProvider:"Test.exe".ToRelativePath(),
               customProperties: ImmutableDictionary<string, string>.Empty),
           projectionSpecs: null,
           updateNotesFiles: testDirectory.AppendPath("updateNotes.txt.en.md").ToSingleImmutableList(),
           allowEmptyNotes: true,
           host: host
           );
       
       host.AssertFileExists($"packages/{packageId}.package.proto.xz");
       host.AssertFileExists($"projections/{packageId}.projection.proto.xz");
       host.AssertFileExists(".updates.proto.xz");
       host.AssertFileExists("notes/TestPackage.UpdateNotes.en.md.xz");
       host.Files.Count.Assert(5);
    }
    
    public static void 
    WriteAllTextIntoFile(this string content, string file, [CallerMemberName] string directory = null) =>
        File.WriteAllText(Environment.CurrentDirectory.AppendPath(directory!).AppendPath(file).CreateParentDirectoryIfAbsent(), content);

    public static string 
    SetFileVersion(this string filePath, string version) {
        File.AppendAllLines(filePath, $"version: {version}".ToSingleEnumerable());
        return filePath;
    } 
    
    public static string 
    CreateTestFile(this string fileName, [CallerMemberName] string directory = null) {
        var path = Environment.CurrentDirectory.AppendPath(directory!).AppendPath(fileName).CreateParentDirectoryIfAbsent();
        path.CreateFile().Dispose();
        return path;
    }
    
    
}
}