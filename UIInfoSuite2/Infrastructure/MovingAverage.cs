using System;

namespace UIInfoSuite2.Infrastructure;

public class MovingAverage
{
  public int Count { get; private set; }
  public double Avg { get; private set; }

  public double Variance { get; private set; }
  public double StdDev { get; private set; }

  public void AddValue(int value)
  {
    AddValue((double)value);
  }

  public void AddValue(float value)
  {
    AddValue((double)value);
  }

  public void AddValue(double value)
  {
    double oldAvg = Avg;
    Count++;
    Avg += (value - Avg) / Count;
    Variance += (value - oldAvg) * (value - Avg);
    StdDev = Math.Sqrt(Variance);
  }

  public void Reset()
  {
    ResetTo(0, 0);
  }

  public void ResetTo(double avg, int count = 1)
  {
    Avg = avg;
    Count = count;
  }
}
