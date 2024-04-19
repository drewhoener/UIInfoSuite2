using System;

namespace UIInfoSuite2.Infrastructure;

// https://www.johndcook.com/blog/standard_deviation/
public class MovingAverage
{
  private double _mean;

  private double _oldMean;
  private double _oldStdDev;
  private double _stdDev;

  public int Count { get; private set; }
  public double Mean => Count > 0 ? _mean : 0.0;

  public double Variance => Count > 1 ? _stdDev / (Count - 1) : 0.0;
  public double StdDev => Math.Sqrt(Variance);

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
    Count++;
    if (Count == 1)
    {
      _mean = value;
      _oldMean = _mean;
    }
    else
    {
      _mean = _oldMean + (value - _oldMean) / Count;
      _stdDev = _oldStdDev + (value - _oldMean) * (value - _mean);

      // set up for next iteration
      _oldMean = _mean;
      _oldStdDev = _stdDev;
    }
  }

  public void Reset()
  {
    ResetTo(0, 0);
  }

  public void ResetTo(double avg, int count = 1)
  {
    _mean = avg;
    _oldMean = avg;
    _stdDev = 0.0;
    _oldStdDev = 0.0;
    Count = count;
  }
}
