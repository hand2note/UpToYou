using System;
using System.IO;

namespace UpToYou.Core {

internal class ParsingException : Exception {
    public string? ParsedData { get; }
    public ParsingException(string msg, string? parsedData = null) : base(msg) => ParsedData = parsedData;
    public ParsingException(string msg, Exception innerException, string? parsedData = null) : base(msg, innerException) => ParsedData = parsedData;

    public override string ToString() => Message + "\n" + ParsedData;
}

internal class 
SourceFileNotFoundException : FileNotFoundException { }

public class InvalidPackageDataException : Exception
{
    public InvalidPackageDataException(string message) : base(message)
    {
    }
}

public class
InvalidRemoteDataException : Exception {
    public InvalidRemoteDataException(string msg) : base(msg) { }
    public InvalidRemoteDataException(string msg, Exception inner) : base(msg, inner) { }
}

}