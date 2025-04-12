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

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
public class GiftLockModule(IModEvents modEvents, IMonitor logger, ConfigManager configManager)
  : BaseModule(modEvents, logger, configManager), IConfigurable, IPatchable
{
  public void Patch(Harmony harmony)
  {
    harmony.Patch(
      AccessTools.DeclaredMethod(typeof(SocialPage), nameof(SocialPage.drawNPCSlot)),
      postfix: new HarmonyMethod(typeof(GiftLockModule), nameof(SocialPage_drawNPCSlot_Postfix))
    );
  }

  public override bool ShouldEnable()
  {
    return Config.ShowLockAfterNpcGift;
  }

  public override void OnEnable() { }

  public override void OnDisable() { }

  [SuppressMessage("ReSharper", "InconsistentNaming")]
  private static void SocialPage_drawNPCSlot_Postfix(SpriteBatch b, int i, SocialPage __instance)
  {
    var module = ModEntry.GetSingleton<GiftLockModule>();
    SocialPage.SocialEntry? socialEntry = __instance.GetSocialEntry(i);
    if (!module.Enabled || socialEntry == null)
    {
      return;
    }

    // Positions ripped from SocialMenu
    float xPosition = __instance.xPositionOnScreen + 384 + 296;
    float yPosition = __instance.sprites[i].bounds.Y + 32 + 20;
    Friendship? friendship = socialEntry.Friendship;
    if (friendship is not null && friendship.GiftsToday != 0 && friendship.GiftsThisWeek < 2)
    {
      b.Draw(
        Game1.mouseCursors,
        new Vector2(xPosition + 4, yPosition + 6),
        new Rectangle(106, 442, 9, 9),
        Color.LightGray,
        0.0f,
        Vector2.Zero,
        3f,
        SpriteEffects.None,
        0.22f
      );
    }
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
      name: I18n.Gmcm_Modules_Menus_GiftLock_Enable,
      tooltip: I18n.Gmcm_Modules_Menus_GiftLock_Enable_Tooltip,
      getValue: () => Config.ShowLockAfterNpcGift,
      setValue: value => Config.ShowLockAfterNpcGift = value
    );
  }
#endregion
}
