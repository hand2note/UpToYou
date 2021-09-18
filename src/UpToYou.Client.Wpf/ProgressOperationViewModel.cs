using System;
using System.ComponentModel;
using UpToYou.Core;

namespace UpToYou.Client.Wpf {

public class ProgressOperationViewModel: INotifyPropertyChanged, IDisposable {
    private readonly IHasProgressOperation _provider;

    private readonly System.Timers.Timer _refreshTimer;

    public ProgressOperationViewModel(IHasProgressOperation provider, int refreshRateMs = 200) {
        _provider = provider;
        _refreshTimer = new System.Timers.Timer(refreshRateMs);
        _refreshTimer.Elapsed += (s,e) => Refresh();
        _refreshTimer.Start();
    }

    public void Refresh() {
        Operation = _provider.ProgressOperation?.Operation;
        IsProgressLessOperation = !( 
            _provider.ProgressOperation.HasValue && 
            _provider.ProgressOperation.Value.Progress.HasValue &&
            _provider.ProgressOperation.Value.Progress.Value.Value > 0);
        Progress = _provider.ProgressOperation?.Progress??new Progress();
    }

    private string _Operation;
    public string Operation { get => _Operation; set { _Operation = value; RaisePropertyChanged(nameof(Operation)); } }

    private bool _IsProgressLessOperation;
    public bool IsProgressLessOperation { get => _IsProgressLessOperation; set { _IsProgressLessOperation = value; RaisePropertyChanged(nameof(IsProgressLessOperation)); } }

    private Progress _Progress;
    public Progress Progress { get => _Progress; set { _Progress = value; RaisePropertyChanged(nameof(Progress)); } }

    public void Dispose() => _refreshTimer?.Dispose();

    public event PropertyChangedEventHandler PropertyChanged;

    public void RaisePropertyChanged(string propertyName) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

}