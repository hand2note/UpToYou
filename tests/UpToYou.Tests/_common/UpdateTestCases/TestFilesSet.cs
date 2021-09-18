using System.Collections.Generic;
using System.Linq;
using UpToYou.Core;

namespace UpToYou.Tests.UpdateTestCases {

public class 
    TestFilesSet {

    public TestFilesSet(string root, IEnumerable<string> files) => (Root, Files) =(root, files.ToList());
    public TestFilesSet(string root, params string[] files) => (Root, Files) =(root, files.ToList());

    public TestFilesSet(string root, TestFilesSet other) : this(root, other.Files.ToArray()) { }

    public string Root { get; }
    public List<string> Files { get; }
    public List<string> AbsoluteFiles => Files.Select(x => x.ToAbsoluteFilePath(Root)).ToList();
    public List<RelativePath> RelativeFiles => Files.ToRelativePathsList();
}

}