using HarmonyLib;

namespace UIInfoSuite2.Infrastructure.Interfaces;

public interface IPatchable
{
  public void Patch(Harmony harmony);
}
