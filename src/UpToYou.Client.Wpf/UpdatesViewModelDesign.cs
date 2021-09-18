using System;
using System.Collections.Generic;
using System.Linq;
using UpToYou.Core;

namespace UpToYou.Client.Wpf {

internal class UpdatesViewModelDesign : UpdatesViewModel {

    static readonly List<(Update update, string notes)> _updates = new List<(string packageName, string version, string notes)>() {
        ("Hand2Note", "3.2.6.22", @"
Added:
* ``Short Deck:`` game type is split onto two new game types based on the rules: Trips Beats Straight, Straight Beats Trips.
* ``Asian rooms:`` WePoker, PotatoPoker are supported through converters working with Hand2Note Api (if implemented on the converters' side).
Fixes:
* ``Short Deck: `` Invalid calculation of hand strength and hand results.
* ``Replayer:`` pocket cards are not shown right after opening a replayer's window.
* ``Configuration:`` Crash opening the  window on Windows 32-bit.
* ``Stats Editor:`` hand strength condition _Turn Board 4-straight with 1 gap_ includes paired boards.
* ``Stats Editor:`` hand strength conditions _Ace High, K-2 High_ don't work.
* ``Hand View:`` wrong hand results are shown for [Run It Twice](https://google.com) hands."),


        ("Connective", "1.0.2.3", @"
Fixes:
* ``Connective:`` Chico errors (critical)
* ``Connective:`` CPU Load reduced to 2-3%"),


        ("Hand2Note", "3.2.6.12", @"
#Short Deck Support

![](http://hand2note.com/Images/Home/sessions.jpg?v=ajMiMLG1D534RH9sWX__EnHzTA5uDBvUUS4CPxf31EI)

Hot Fixes:
* ``Replayer:`` crash in a hand with Run It Twice from Preflop.
* ``Replayer:`` only single pocket card is shown in Texas Hold'em.
* ``Asian rooms: `` the HUD sometimes is not shown for the hands without straddle.")

    }.Select(x => (new Update(
            new PackageMetadata(UniqueId.NewUniqueId(), x.packageName, Version.Parse(x.version), DateTime.Now, versionProviderFile:new PackageFile(id: string.Empty, "hand2note.exe".ToRelativePath(), 0, String.Empty,new Version()),null), 
            new UpdatePolicy(false,  false, new UpdateRing(), false, false, null), 
            null), x.notes)).ToList();

    static readonly ApplicationUpdateContext _ctx;

    static UpdatesViewModelDesign() {
            _ctx = ApplicationUpdateContext.Create(
                 new DownloadFileHostClient("https://uptoyoutest.blob.core.windows.net/uptoyou"),
                 null,
                null,
                null,
                null);
        }


public UpdatesViewModelDesign() : base(_ctx, "en") {
            Updates = _updates.Select(x => new UpdateViewModel(x.update, new UpdateNotes(x.update.PackageMetadata.Name, x.update.PackageMetadata.Version, x.notes), _ctx)).ToList();
            IsLoading = false;
            //_ctx.OnPackageInstallStarted(_ctx.NewUpdates.First().PackageMetadata.Id);
            //    //_ctx.InstallingUpdateState.OnOperationChanged("Downloading upate files...", new Progress(
            //    //    value:1_350_000,
            //    //    speed:new SpeedInterval(1_350_000, TimeSpan.FromSeconds(5)),
            //    //    targetValue:5_520_000));
            //    _ctx.InstallingUpdateState.OnOperationChanged("Installing update...",null);
            //    base.Updates.ForEach(x => x.InstallProgress?.Refresh());
        }
}
}
