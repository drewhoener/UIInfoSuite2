using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace UIInfoSuite2.Infrastructure;

public class Profiler
{
  private readonly Queue<long> _entries;
  private readonly int _logEvery;
  private readonly Action<string> _loggerFunc;
  private readonly string _profilerName;
  private readonly int _rollingWindowSize;
  private readonly Stopwatch _stopwatch;
  private long _entriesSinceLastLog;
  private long _totalElapsedTicks;

  public Profiler(string profilerName, int rollingWindowSize, int logEvery, Action<string> loggerFunc)
  {
    if (rollingWindowSize <= 0)
    {
      throw new ArgumentException("Rolling window size must be greater than 0");
    }

    if (logEvery <= 0)
    {
      throw new ArgumentException("Log every must be greater than 0");
    }

    _stopwatch = new Stopwatch();
    _entries = new Queue<long>();
    _profilerName = profilerName;
    _rollingWindowSize = rollingWindowSize;
    _logEvery = logEvery;
    _loggerFunc = loggerFunc ?? throw new ArgumentNullException(nameof(loggerFunc));
    _totalElapsedTicks = 0;
  }

  public void Start()
  {
    _stopwatch.Start();
  }

  public void Pause()
  {
    _stopwatch.Stop();
  }

  public void Stop()
  {
    _stopwatch.Stop();
    long elapsedTicks = _stopwatch.ElapsedTicks;
    _totalElapsedTicks += elapsedTicks;
    _entries.Enqueue(elapsedTicks);
    _stopwatch.Reset();

    _entriesSinceLastLog++;

    if (_entries.Count > _rollingWindowSize)
    {
      _totalElapsedTicks -= _entries.Dequeue();
    }

    if (_entriesSinceLastLog != _logEvery)
    {
      return;
    }

    _entriesSinceLastLog = 0;
    double avg = GetRollingAverage();
    double avgMicros = avg / Stopwatch.Frequency * 1000000;
    _loggerFunc(
      $"[Profiler ({_profilerName})]: Rolling average ({_rollingWindowSize} entries) / {avgMicros:F2} us / {avg / Stopwatch.Frequency:F2} s"
    );
  }

  private double GetRollingAverage()
  {
    return (double)_totalElapsedTicks / _entries.Count;
  }
}
