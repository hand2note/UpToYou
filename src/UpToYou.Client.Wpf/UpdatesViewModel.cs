using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Markdig;
using UpToYou.Core;

namespace UpToYou.Client.Wpf {

public class 
UpdatesViewModel: INotifyPropertyChanged, IDisposable {
    private readonly ApplicationUpdateContext _ctx;
    private readonly string _locale;
    private readonly Update _highlightedUpdate;

    public UpdatesViewModel(ApplicationUpdateContext ctx, string locale, Update highlightedUpdate = null) {
        _ctx = ctx;
        _locale = locale;
        _highlightedUpdate = highlightedUpdate;
        Refresh();    
    }

    private bool _IsLoading;
    public bool IsLoading { get => _IsLoading; set { _IsLoading = value; RaisePropertyChanged(nameof(IsLoading)); } }

    private List<UpdateViewModel> _Updates;
    public List<UpdateViewModel> Updates { get => _Updates; set { _Updates = value; RaisePropertyChanged(nameof(Updates)); } }

    public void Dispose() => Updates?.ForEach(x => x.Dispose());

    private async void Refresh() {
        IsLoading =true;
        var allUpdateNotes = await _ctx.GetUpdateNotesAsync(_locale);
        Updates = _ctx.AllUpdates.Select(x => new UpdateViewModel(x, allUpdateNotes.FirstOrDefault(y => y.Hits(x.PackageMetadata.Name, x.PackageMetadata.Version)), _ctx, isHighlighted:x == _highlightedUpdate)).ToList();
        IsLoading = false;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void RaisePropertyChanged(string propertyName) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class UpdateViewModel: INotifyPropertyChanged, IDisposable {
    protected readonly ApplicationUpdateContext _ctx;

    public UpdateViewModel(Update update, UpdateNotes updateNotes, ApplicationUpdateContext ctx, bool isHighlighted = false) {
        _ctx = ctx;
        Model = update;
        IsHighlighted = isHighlighted;
        IsAutoLazyUpdate = update?.UpdatePolicy != null && (update.UpdatePolicy.IsAuto || update.UpdatePolicy.IsRequired) && update.UpdatePolicy.IsLazy;
        UpdateInstallProgress();
        ctx.PackageInstallCompleted += PackageInstallCompletedHandler;
        ctx.PackageInstallStarted += PackageInstallStartedHandler;
        if (updateNotes?.Notes != null)
            (Header, UpdateNotes) = updateNotes.Notes.SplitUpdateNotesHeader();
        Refresh();
        Pipeline = new MarkdownPipelineBuilder()
           .UseAdvancedExtensions()
           .UseEmphasisExtras()
           .UseAutoLinks()
           .UseEmojiAndSmiley()
           .Build();
    }

    public Update Model { get; }

    public string Header { get; }
    public string UpdateNotes { get; }

    public MarkdownPipeline Pipeline { get; }

    public bool IsHighlighted { get; }

    private bool _IsInstalled;
    public bool IsInstalled { get => _IsInstalled; set { _IsInstalled = value; RaisePropertyChanged(nameof(IsInstalled)); } }

    private bool _IsNew;
    public bool IsNew { get => _IsNew; set { _IsNew = value; RaisePropertyChanged(nameof(IsNew)); } }

    private bool _IsInstalling;
    public bool IsInstalling { get => _IsInstalling; set { _IsInstalling = value; RaisePropertyChanged(nameof(IsInstalling)); } }

    private bool _IsLastUpdate;
    public bool IsLastUpdate { get => _IsLastUpdate; set { _IsLastUpdate = value; RaisePropertyChanged(nameof(IsLastUpdate)); } }

    private bool _IsAutoLazyUpdate;
    public bool IsAutoLazyUpdate { get { return _IsAutoLazyUpdate; } set { _IsAutoLazyUpdate = value; RaisePropertyChanged(nameof(IsAutoLazyUpdate)); } }

    private ProgressOperationViewModel _InstallProgress;
    public ProgressOperationViewModel InstallProgress { get => _InstallProgress; set { _InstallProgress = value; RaisePropertyChanged(nameof(InstallProgress)); } }

    private bool _IsPreviouslyInstalled;
    public bool IsPreviouslyInstalled { get { return _IsPreviouslyInstalled; } set { _IsPreviouslyInstalled = value; RaisePropertyChanged(nameof(IsPreviouslyInstalled)); } }

    private string _Error;
    public string Error { get { return _Error; } set { _Error = value; RaisePropertyChanged(nameof(Error)); } }

    public ICommand InstallCommand=> new RelayCommand(InstallExecute, () => !IsInstalled && !_ctx.IsInstalling);
    public ICommand CancelCommand => new RelayCommand(
        () => _ctx. InstallingUpdateState?.CancellationTokenSource.Cancel(),
        () => _ctx.InstallingUpdateState?.PackageId == Model.PackageMetadata.Id);

    private async void InstallExecute() {
        var requiredDependencies = FindRequiredDependencies().ToList();
        if (requiredDependencies.Count > 0) {
            MessageBox.Show($"{Properties.Resources.InstallDependency}\n{string.Join("\n", requiredDependencies.Select(x => $"{x.PackageName} {x.MinVersion}"))}");
            return;
        }
        var result = await Model.DownloadAndInstallAsync(_ctx);
        if (result.IsError)
            Error = result.ErrorMessage;
        UpdateInstallProgress();
        if (result.IsRunnerExecutionRequired)
            Runner.RunUpdater(_ctx.UpdateDirectory.AppendPath(InstallModule.RunnerSourcesSubDirectory), _ctx.GetUpdateBackupDirectory(Model));
        if (result.IsApplicationShutDownRequired)
            _ctx.ShutDown();
    }

    private IEnumerable<PackageDependency> FindRequiredDependencies() {
        if (Model.HasDependencies)
            foreach (var dependency in Model.Dependencies) {
                if (!dependency.IsInstalled(_ctx))
                    yield return dependency;
            }
    }


    private void UpdateInstallProgress() {
        if (_ctx.InstallingUpdateState?.PackageId == Model.PackageMetadata.Id && _ctx.InstallingUpdateState != null) {
            IsInstalling = true;
            InstallProgress = new ProgressOperationViewModel(_ctx.InstallingUpdateState);
        }
    }

    private void PackageInstallCompletedHandler(string packageId) {
        Refresh();
    }

    private void Refresh() {
        IsInstalled = Model.PackageMetadata.IsInstalled(_ctx.ProgramDirectory);
        IsNew = !IsInstalled && !Model.PackageMetadata.IsHigherVersionInstalled(_ctx.ProgramDirectory);
        IsInstalling = _ctx.IsInstalling && _ctx.InstallingUpdateState?.PackageId == Model.PackageMetadata.Id;
        IsLastUpdate = _ctx.AllUpdates.FirstOrDefault(x => x.PackageMetadata.Name == Model.PackageMetadata.Name)?.PackageMetadata.Id == Model.PackageMetadata.Id;
        IsPreviouslyInstalled = !IsNew;
    }

    private void PackageInstallStartedHandler(string packageId) => UpdateInstallProgress();

    public void Dispose() {
        _ctx.PackageInstallStarted -= PackageInstallStartedHandler;
        _ctx.PackageInstallCompleted -= PackageInstallCompletedHandler;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void RaisePropertyChanged(string propertyName) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
}
