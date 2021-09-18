using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UpToYou.Client;
using UpToYou.Client.Wpf;
using UpToYou.Core;

namespace Updater.Client.Wpf.Debug
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IApplicationShutDown
    {

        class UpdateRingPolicy: IUpdateRingPolicy {
            public int GetPercentage() => 0;
        }

        public MainWindow() {
            var updateCtx = ApplicationUpdateContext.Create( new DownloadFileHostClientTest("https://uptoyoutest.blob.core.windows.net/uptoyou"), this, new UpdateRingPolicy(), 
                updatesFilter:x => x.PackageMetadata.FindCustomProperty("Architecture") != "x86");
            var lastHand2NoteUpdate = updateCtx.AllUpdates.FirstOrDefault(x => x.PackageMetadata.Name == "Hand2Note");
            var highlightedUpdate = lastHand2NoteUpdate!= null && !lastHand2NoteUpdate.IsInstalled(updateCtx) ? lastHand2NoteUpdate:null;
            DataContext = new UpdatesViewModel(updateCtx, "en", highlightedUpdate );
        }

        public void ShutDown() {
            Dispatcher.Invoke(() => App.Current.Shutdown());
        }

       
    }

public class DownloadFileHostClientTest: IFilesHostClient {
    private readonly string _rootUrl;

    public DownloadFileHostClientTest(string rootUrl) => _rootUrl = rootUrl;

    public string DownloadFile(ProgressContext? pCtx, RelativePath path, string outFile) {
        Task.Delay(1000).Wait();
        return path.ToAbsolute(_rootUrl).DownloadFile(pCtx, outFile);
    }

    public byte[] DownloadData(ProgressContext? pCtx, RelativePath path) {
        Task.Delay(1000).Wait();
        return path.ToAbsolute(_rootUrl).DownloadData(pCtx);
    }
}


}
