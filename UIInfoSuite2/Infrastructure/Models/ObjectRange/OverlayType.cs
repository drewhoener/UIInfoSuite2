namespace UIInfoSuite2.Infrastructure.Models.ObjectRange;

internal class OverlayType
{
  public const string JunimoHut = "JunimoHut";
  public const string Scarecrow = "Scarecrow";
  public const string Sprinkler = "Sprinkler";

  public static int GetLayer(string layer)
  {
    switch (layer)
    {
      case JunimoHut:
        return 0;
      case Scarecrow:
        return 1;
      case Sprinkler:
        return 2;
      default:
        return 3;
    }
  }
}
