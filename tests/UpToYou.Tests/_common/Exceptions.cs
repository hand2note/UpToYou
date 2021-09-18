using System;

namespace UpToYou.Tests
{

public class InvalidTestDataException : Exception {
    public InvalidTestDataException(string msg) : base(msg) { }
}
}
