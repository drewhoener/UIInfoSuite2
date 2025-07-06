using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Interfaces;
using UIInfoSuite2.Infrastructure.Modules.Base;

namespace UIInfoSuite2.Infrastructure.Modules.MenuAdditions;

internal class SocialPageFilterModule : BaseModule, IPatchable, IConfigurable
{
  private readonly PerScreen<string?> _filter = new(() => null);

  // mapping from original entry -> filtered index
  private readonly PerScreen<Dictionary<int, int>> _socialEntryMapping = new(() => new Dictionary<int, int>());

  private readonly Lazy<TextBox> _textBox = new(() => new TextBox(null, null, Game1.dialogueFont, Game1.textColor)
    {
      X = 20, Y = 20, Width = 256, Height = 192
    }
  );

  private readonly TextBoxEvent _textBoxEvent;
  private readonly PerScreen<bool> _useFilteredIndex = new(() => false);

  public SocialPageFilterModule(IModEvents modEvents, IMonitor logger, ConfigManager configManager) : base(
    modEvents,
    logger,
    configManager
  )
  {
    _textBoxEvent = OnTextBoxEvt;
  }

  public void Patch(Harmony harmony)
  {
    harmony.Patch(
      AccessTools.DeclaredMethod(typeof(SocialPage), nameof(SocialPage.draw)),
      transpiler: new HarmonyMethod(typeof(SocialPageFilterModule), nameof(SocialPage_draw_Transpiler)),
      postfix: new HarmonyMethod(typeof(SocialPageFilterModule), nameof(SocialPage_draw_Postfix))
    );
    harmony.Patch(
      AccessTools.DeclaredMethod(typeof(SocialPage), nameof(SocialPage.receiveLeftClick)),
      transpiler: new HarmonyMethod(typeof(SocialPageFilterModule), nameof(SocialPage_receiveLeftClick_Transpiler))
    );
    harmony.Patch(
      AccessTools.DeclaredMethod(typeof(SocialPage), nameof(SocialPage.isCharacterSlotClickable)),
      postfix: new HarmonyMethod(typeof(SocialPageFilterModule), nameof(SocialPage_isCharacterSlotClickable_Postfix))
    );
  }

  public override bool ShouldEnable()
  {
    return Config.ShowHeartFills;
  }

  public override void OnEnable()
  {
    _textBox.Value.OnEnterPressed += _textBoxEvent;
    ModEvents.Input.ButtonPressed += OnClick;
  }

  public override void OnDisable()
  {
    _textBox.Value.OnEnterPressed -= _textBoxEvent;
    ModEvents.Input.ButtonPressed -= OnClick;
  }

  private void OnTextBoxEvt(TextBox textBox)
  {
    Logger.Log(textBox.Text);
  }

  private void OnClick(object? sender, ButtonPressedEventArgs e)
  {
    if (e.Button == SButton.MouseLeft)
    {
      _textBox.Value.Update();
    }

    if (_textBox.Value.Selected)
    {
      _filter.Value = _textBox.Value.Text;
    }
  }

  public List<SocialPage.SocialEntry> GetFilteredNpcs(SocialPage socialPage)
  {
    string? filter = _filter.Value;
    if (string.IsNullOrEmpty(filter))
    {
      return socialPage.SocialEntries;
    }

    List<SocialPage.SocialEntry> entries = socialPage.SocialEntries
      .Where(entry => entry.DisplayName.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
      .ToList();
    for (var i = 0; i < entries.Count; i++)
    {
      SocialPage.SocialEntry entry = entries[i];
      int originalIndex = socialPage.SocialEntries.IndexOf(entry);
      _socialEntryMapping.Value[originalIndex] = i;
    }

    return entries;
  }

  [SuppressMessage("ReSharper", "InconsistentNaming")]
  private static IEnumerable<CodeInstruction> SocialPage_draw_Transpiler(
    IEnumerable<CodeInstruction> instructions,
    ILGenerator generator
  )
  {
    CodeMatcher matcher = new(instructions, generator);

    /*
     * IL_00ec: ldloc.3      // slotPosition
       IL_00ed: ldarg.0      // this
       IL_00ee: ldfld        class [System.Collections]System.Collections.Generic.List`1<class StardewValley.Menus.ClickableTextureComponent> StardewValley.Menus.SocialPage::sprites
       IL_00f3: callvirt     instance int32 class [System.Collections]System.Collections.Generic.List`1<class StardewValley.Menus.ClickableTextureComponent>::get_Count()
       IL_00f8: blt.s        IL_00b5
     */
    matcher.MatchEndForward(
        new CodeMatch(OpCodes.Ldloc_3),
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(i => i.opcode == OpCodes.Ldfld),
        new CodeMatch(i => i.opcode == OpCodes.Callvirt),
        new CodeMatch(OpCodes.Blt_S)
      )
      .ThrowIfNotMatch("Unable to find end of socialpage draw loop");
    int endInstruction = matcher.Pos;

    /*
     * IL_00ac: ldarg.0      // this
       IL_00ad: ldfld        int32 StardewValley.Menus.SocialPage::slotPosition
       IL_00b2: stloc.3      // slotPosition
     */
    matcher.Start()
      .MatchStartForward(
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(i => i.opcode == OpCodes.Ldfld),
        new CodeMatch(OpCodes.Stloc_3)
      )
      .ThrowIfNotMatch("Unable to find start of socialpage draw loop");
    int startInstruction = matcher.Pos;

    matcher.RemoveInstructionsInRange(startInstruction, endInstruction);
    matcher.Insert(
      new CodeInstruction(OpCodes.Ldarg_0),
      new CodeInstruction(OpCodes.Ldarg_1),
      CodeInstruction.Call(typeof(SocialPageFilterModule), nameof(DrawFilteredEntries))
    );

    return matcher.InstructionEnumeration();
  }

  private static IEnumerable<CodeInstruction> SocialPage_receiveLeftClick_Transpiler(
    IEnumerable<CodeInstruction> instructions,
    ILGenerator generator
  )
  {
    CodeMatcher matcher = new(instructions, generator);

    /*
     * IL_012d: ldarg.0      // this
       IL_012e: ldloc.1      // index
       IL_012f: call         instance class StardewValley.Menus.SocialPage/SocialEntry StardewValley.Menus.SocialPage::GetSocialEntry(int32)
       IL_0134: stloc.2      // socialEntry
     */

    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(OpCodes.Ldloc_1),
        new CodeMatch(i => i.opcode == OpCodes.Call),
        new CodeMatch(OpCodes.Stloc_2)
      )
      .ThrowIfNotMatch("Couldn't find insertion point for left click matcher")
      .Advance(2)
      .SetInstruction(CodeInstruction.Call(typeof(SocialPageFilterModule), nameof(GetFilteredSocialEntry)));

    return matcher.InstructionEnumeration();
  }

  private static void SocialPage_isCharacterSlotClickable_Postfix(int i, ref bool __result, SocialPage __instance)
  {
    var module = ModEntry.GetSingleton<SocialPageFilterModule>();
    if (!module._useFilteredIndex.Value || module._socialEntryMapping.Value.IsEmpty())
    {
      return;
    }

    // i is the originalIndex, find its visual position
    if (module._socialEntryMapping.Value.TryGetValue(i, out int visualIndex))
    {
      SocialPage.SocialEntry? socialEntry = GetFilteredSocialEntry(__instance, visualIndex);
      __result = socialEntry is { IsPlayer: false, IsChild: false, IsMet: true };
    }
    else
    {
      __result = false; // Not visible in current filtered view
    }
  }

  public static SocialPage.SocialEntry? GetFilteredSocialEntry(SocialPage socialPage, int visualIndex)
  {
    var module = ModEntry.GetSingleton<SocialPageFilterModule>();

    List<SocialPage.SocialEntry> filteredEntries = module.GetFilteredNpcs(socialPage);

    // Convert visual index to filtered entry
    int adjustedIndex = visualIndex - socialPage.slotPosition;
    if (adjustedIndex >= 0 && adjustedIndex < filteredEntries.Count)
    {
      return filteredEntries[adjustedIndex];
    }

    return null;
  }

  public static void DrawFilteredEntries(SocialPage socialPage, SpriteBatch b)
  {
    var module = ModEntry.GetSingleton<SocialPageFilterModule>();

    List<SocialPage.SocialEntry> filteredEntries = module.GetFilteredNpcs(socialPage);
    int startIndex = Math.Max(0, socialPage.slotPosition);

    for (var i = 0; i < 5 && startIndex + i < filteredEntries.Count; i++)
    {
      SocialPage.SocialEntry entry = filteredEntries[startIndex + i];
      int originalIndex = socialPage.SocialEntries.IndexOf(entry);
      Rectangle originalSlotBounds = socialPage.characterSlots[originalIndex].bounds;

      // Use the SAME calculation as updateSlots()
      int correctY = socialPage.yPositionOnScreen + IClickableMenu.borderWidth + 32 + 112 * i + 32;

      // Temporarily update the sprite's Y position
      module._useFilteredIndex.Value = true;
      int originalY = socialPage.sprites[originalIndex].bounds.Y;
      socialPage.sprites[originalIndex].bounds.Y = correctY;
      socialPage.characterSlots[originalIndex].bounds = new Rectangle(
        socialPage.xPositionOnScreen + IClickableMenu.borderWidth,
        correctY - 4, // Match the visual positioning
        socialPage.width - IClickableMenu.borderWidth * 2,
        socialPage.characterSlots[originalIndex].bounds.Height
      );

      // Draw with original methods
      if (entry.IsPlayer)
      {
        socialPage.drawFarmerSlot(b, originalIndex);
      }
      else
      {
        socialPage.drawNPCSlot(b, originalIndex);
      }

      // Restore original position
      socialPage.sprites[originalIndex].bounds.Y = originalY;
      socialPage.characterSlots[originalIndex].bounds = originalSlotBounds;
      module._useFilteredIndex.Value = false;
    }
  }

  [SuppressMessage("ReSharper", "InconsistentNaming")]
  private static void SocialPage_draw_Postfix(SpriteBatch b, SocialPage __instance)
  {
    var module = ModEntry.GetSingleton<SocialPageFilterModule>();
    module._textBox.Value.Draw(b);
  }

#region Configuration Setup
  public string GetConfigPage()
  {
    return ConfigPageNames.MenuFeatures;
  }

  public string GetConfigSection()
  {
    return ConfigSectionNames.EmptySection;
  }

  public string GetSubHeader()
  {
    return I18n.Gmcm_Group_SocialFeatures();
  }

  public void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest) { }
#endregion
}
