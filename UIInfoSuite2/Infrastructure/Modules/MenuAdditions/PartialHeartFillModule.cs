using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Interfaces;
using UIInfoSuite2.Infrastructure.Modules.Base;

namespace UIInfoSuite2.Infrastructure.Modules.MenuAdditions;

internal class PartialHeartFillModule(IModEvents modEvents, IMonitor logger, ConfigManager configManager)
  : BaseModule(modEvents, logger, configManager), IPatchable, IConfigurable
{
  public void Patch(Harmony harmony)
  {
    harmony.Patch(
      AccessTools.DeclaredMethod(typeof(SocialPage), nameof(SocialPage.drawNPCSlotHeart)),
      postfix: new HarmonyMethod(typeof(PartialHeartFillModule), nameof(SocialPage_drawNPCSlotHeart_Postfix))
    );
  }

  public override bool ShouldEnable()
  {
    return Config.ShowHeartFills;
  }

  public override void OnEnable() { }

  public override void OnDisable() { }

  [SuppressMessage("ReSharper", "InconsistentNaming")]
  private static void SocialPage_drawNPCSlotHeart_Postfix(
    SpriteBatch b,
    int npcIndex,
    SocialPage.SocialEntry entry,
    int hearts,
    bool isDating,
    bool isCurrentSpouse,
    SocialPage __instance
  )
  {
    var module = ModEntry.GetSingleton<PartialHeartFillModule>();

    if (!module.Enabled ||
        entry.Friendship is null ||
        // The only heart we fill is the next partial
        hearts != entry.HeartLevel ||
        // Heart is locked until we start dating
        (entry.IsDatable && !isDating && !isCurrentSpouse && hearts >= 8))
    {
      return;
    }

    float nextHeartFillPct = entry.Friendship.Points % 255f / 255f;
    // Don't draw on exact
    if (nextHeartFillPct == 0.0)
    {
      return;
    }

    int heartsXOffset = hearts < 10 ? hearts * 32 : (hearts - 10) * 32;
    int heartsYOffset = hearts < 10 ? -28 : 0;

    var drawPosition = new Vector2(
      __instance.xPositionOnScreen + 320 - 4 + heartsXOffset,
      __instance.sprites[npcIndex].bounds.Y + 64 + heartsYOffset
    );

    b.Draw(
      Game1.mouseCursors,
      drawPosition,
      new Rectangle(211, 428, (int)(7 * nextHeartFillPct), 6),
      Color.White,
      0.0f,
      Vector2.Zero,
      4f,
      SpriteEffects.None,
      0.88f
    );
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

  public void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Menus_Hearts_Enable,
      tooltip: I18n.Gmcm_Modules_Menus_Hearts_Enable_Tooltip,
      getValue: () => Config.ShowHeartFills,
      setValue: value => Config.ShowHeartFills = value
    );
  }
#endregion
}
