using System;
using System.Collections.Generic;
using System.Linq;

namespace UpToYou.Core {

public readonly struct 
SpeedInterval {
    public long Progress { get; }
    public TimeSpan Span { get; }

    public SpeedInterval(long progress, TimeSpan timeSpan) => (Progress, Span) = (progress, timeSpan);
    
    public double 
    ProgressPerSec => Span > TimeSpan.Zero ? Progress / Span.TotalSeconds : 0;

    public static SpeedInterval
    operator +(SpeedInterval si1, SpeedInterval si2) =>
        new SpeedInterval(si1.Progress + si2.Progress, si1.Span + si2.Span);

    public static SpeedInterval
    operator /(SpeedInterval si, TimeSpan span) {
        if (span <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(span), "TimeSpan should be greater zero");
        return new SpeedInterval(si.Progress * span.Ticks / si.Span.Ticks, span);
    }
}

public readonly struct 
Progress {
    public long Value { get; }
    public SpeedInterval Speed{ get; }
    public long? TargetValue { get; }
    
    public Progress(long value, SpeedInterval speed, long? targetValue) => (Value, Speed, TargetValue) = (value, speed, targetValue);

    public double?
    Percentage => TargetValue.HasValue? (double) Value / TargetValue * 100 : null;

    public static Progress
    operator +(Progress p1, Progress p2) {
        var targetValue = (p1.TargetValue??0) + (p2.TargetValue??0);
        return new Progress(p1.Value + p2.Value, p1.Speed + p2.Speed, targetValue != 0? (long?) targetValue : null);
    }

    public Progress
    WithTargetValue(long? targetValue) => new Progress(Value, Speed, targetValue);

}

public static class 
SpeedIntervalEx {
    public static void
    AddProgressIncrement(this IList<SpeedInterval> speedIntervals, TimeSpan span, long increment) => 
        speedIntervals.Add(new SpeedInterval(increment, span));

    private static IEnumerable<SpeedInterval>
    GetRelevantSpeedIntervals(this IList<SpeedInterval> speedIntervals, TimeSpan timeSpan) {
        if (timeSpan < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeSpan), "TimeSpan can't be negative");

        if (timeSpan > TimeSpan.Zero) {
            var span = new TimeSpan();
            foreach(var si in speedIntervals.Reversed())
                if (si.Span + span < timeSpan) {
                    yield return si;
                    span += si.Span;
                }
                else {
                    yield return si / (timeSpan - span);
                    yield break;
                }
        }
    }
    
    public static SpeedInterval
    Sum(this IEnumerable<SpeedInterval> speedIntervals) => 
        speedIntervals.Aggregate<SpeedInterval, SpeedInterval>(new SpeedInterval(), (s, x) => s +x);

    public static SpeedInterval
    CalcRelevantSpeed(this IList<SpeedInterval> speedIntervals, TimeSpan relevantSpan) =>
        speedIntervals.GetRelevantSpeedIntervals(relevantSpan).Sum();
}

public interface
IProgressObserver {
    void OnProgressChanged(Progress progress);
}

public class
ProgressContext {
    private readonly IProgressObserver? _observer;
    private readonly TimeSpan _relevantSpeedInterval;
    public Progress Progress { get; private set; }
    private readonly List<SpeedInterval> _speedIntervals = new List<SpeedInterval>();
    public ProgressContext(IProgressObserver? observer, TimeSpan relevantSpeedInterval) => (_observer, _relevantSpeedInterval) = (observer, relevantSpeedInterval);
    private ProgressSpanIncrement? _progressSpanIncrement;
    private readonly object _lockObject = new object();
    public void
    OnExtraTargetValue(long targetValue) {
        lock (_lockObject) {
            if (targetValue != 0)
                Progress = Progress.WithTargetValue(targetValue);
        }
    }

    public void 
    OnProgressStarted(DateTime time) {
        lock(_lockObject)
            if (_progressSpanIncrement == null)
                _progressSpanIncrement = new ProgressSpanIncrement(this, time);
    }

    public void 
    OnIncrement(DateTime time, long increment) {
        lock (_lockObject) {
            if (_progressSpanIncrement == null)
                throw new InvalidOperationException("ProgressIncrement is null. Call OnProgressStarted first");
            _progressSpanIncrement.OnProgressIncrement(time, increment);
        }
    }

    public void
    OnIncrement(TimeSpan timeSpan, long increment) {
        lock (_lockObject) {
            _speedIntervals.AddProgressIncrement(timeSpan, increment);
            Progress = new Progress(Progress.Value + increment, _speedIntervals.CalcRelevantSpeed(_relevantSpeedInterval), Progress.TargetValue);
            _observer?.OnProgressChanged(Progress);
        }
    }
}

public class
ProgressSpanIncrement {
    private readonly ProgressContext _context;
    private DateTime _lastIncrementTime;

    public ProgressSpanIncrement(ProgressContext context, DateTime timeStarted)=> (_context, _lastIncrementTime) = (context, timeStarted);

    public void
    OnProgressIncrement(DateTime time, long increment) {
        var span = time - _lastIncrementTime;
        _lastIncrementTime = time;
        _context.OnIncrement(span, increment);
    }
}

public class
ProgressValueIncrement {
    private long _lastTotalProgressValue;
    public long 
    OnProgressValueUpdate(long totalProgressValue) {
        var result = totalProgressValue - _lastTotalProgressValue;
        _lastTotalProgressValue = totalProgressValue;
        return result;  
    }
}

public struct 
ProgressOperation {
    public string? Operation { get; }
    public Progress? Progress { get; }

    public ProgressOperation(string? operation, Progress? progress) => (Operation, Progress) = (operation, progress);
}

public interface 
IProgressOperationObserver: IProgressObserver {
    void OnOperationChanged(string operation, Progress? progress = null);
}

}
