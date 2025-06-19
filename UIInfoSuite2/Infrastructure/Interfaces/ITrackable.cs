namespace UIInfoSuite2.Infrastructure.Interfaces;

internal interface ITrackable
{
  public bool IsDirty { get; }
  public void ResetDirty();
}
