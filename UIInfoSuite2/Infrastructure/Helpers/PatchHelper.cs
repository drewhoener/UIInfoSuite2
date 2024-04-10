using System.Reflection.Emit;
using HarmonyLib;

namespace UIInfoSuite2.Infrastructure.Helpers;

public static class PatchHelper
{
  public static CodeMatch CreateLocMatcher(OpCode code, int locIdx)
  {
    return new CodeMatch(
      instruction => instruction.opcode == code &&
                     instruction.operand is LocalBuilder builder &&
                     builder.LocalIndex == locIdx
    );
  }
}
