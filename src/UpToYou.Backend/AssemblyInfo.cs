using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UpToYou.Tests")]
[assembly: InternalsVisibleTo("UpToYou.Backend")]
[assembly: InternalsVisibleTo("UpToYou")]
[assembly: InternalsVisibleTo("UpToYou.Backend.Tests")]
[assembly: InternalsVisibleTo("UpToYou.Backend.Runner.Tests")]
[assembly: InternalsVisibleTo("Deploy")]
[assembly: InternalsVisibleTo("Deploy.UpToYou")]

namespace UpToYou.Backend {
    public class AssemblyInfo { }
}
