using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Options;
using UIInfoSuite2.UIElements.Base;

namespace UIInfoSuite2.UIElements
{
    #region Helper Classes

    internal class BirthdayIconComponent
    {
        private const float IconScale = 2.9f;

        public BirthdayIconComponent(NPC npc)
        {
            NPC = npc;
            Icon = GenerateIcon();
            HoverText = string.Format(ModEntry.GetTranslated(LanguageKeys.NpcBirthday), NPC.displayName);
        }

        internal NPC NPC { get; }
        private ClickableTextureComponent Icon { get; }
        private string HoverText { get; }

        private ClickableTextureComponent GenerateIcon()
        {
            var headShot = NPC.GetHeadShot();
            return new ClickableTextureComponent(
                NPC.Name,
                new Rectangle(
                    0,
                    0,
                    (int)(16.0 * IconScale),
                    (int)(16.0 * IconScale)),
                null,
                NPC.Name,
                NPC.Sprite.Texture,
                headShot,
                2f
            );
        }

        public void DrawIcon()
        {
            var iconPosition = IconHandler.Handler.GetNewIconPosition();

            Game1.spriteBatch.Draw(
                Game1.mouseCursors,
                new Vector2(iconPosition.X, iconPosition.Y),
                new Rectangle(228, 409, 16, 16),
                Color.White,
                0.0f,
                Vector2.Zero,
                IconScale,
                SpriteEffects.None,
                1f
            );

            Icon.setPosition(
                iconPosition.X - 7,
                iconPosition.Y - 2
            );

            Icon.draw(Game1.spriteBatch);
        }

        public void DrawHoverText()
        {
            if (Icon.IsHoveredOver())
            {
                IClickableMenu.drawHoverText(
                    Game1.spriteBatch,
                    HoverText,
                    Game1.dialogueFont);
            }
        }
    }

    #endregion


    // Inherit from base because we don't need the icon that UIHudElement provides
    internal class ShowBirthdayIcon : UIElementBase
    {
        #region Properties

        private readonly PerScreen<List<BirthdayIconComponent>> _birthdayNPCs =
            new(() => new List<BirthdayIconComponent>());

        #endregion


        #region Life cycle

        public ShowBirthdayIcon(IModHelper helper, ModOptions options) : base(helper, options)
        {
        }

        protected override bool ShouldEnable()
        {
            return Options.ShowBirthdayIcon;
        }

        protected override void OnEnable()
        {
            CheckForBirthday();
        }

        protected override void RegisterEvents()
        {
            Helper.Events.GameLoop.DayStarted += OnDayStarted;
            Helper.Events.Display.RenderingHud += OnRenderingHud;
            Helper.Events.Display.RenderedHud += OnRenderedHud;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        protected override void UnregisterEvents()
        {
            Helper.Events.GameLoop.DayStarted -= OnDayStarted;
            Helper.Events.Display.RenderingHud -= OnRenderingHud;
            Helper.Events.Display.RenderedHud -= OnRenderedHud;
            Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        }

        protected override void SetUpOptions()
        {
            var enabledOption = new ModOptionsCheckbox(
                new OptionStringWrapper(nameof(Options.ShowBirthdayIcon)),
                1,
                (s, b) => OnOptionsChanged(s, b),
                () => Options.ShowBirthdayIcon,
                b => Options.ShowBirthdayIcon = b
            );
            OptionElements.Add(enabledOption);
            OptionElements.Add(new ModOptionsCheckbox(
                new OptionStringWrapper(nameof(Options.HideBirthdayIfFullFriendShip)),
                1,
                (_, _) => { CheckForBirthday(); }, // Refresh Birthday List on option toggle
                () => Options.HideBirthdayIfFullFriendShip,
                b => Options.HideBirthdayIfFullFriendShip = b,
                enabledOption
            ));
        }

        #endregion


        #region Event subscriptions

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (e.IsOneSecond)
            {
                CheckForGiftGiven();
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            CheckForBirthday();
        }

        private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
        {
            if (Game1.eventUp)
                return;

            // Draw Birthday Icons
            foreach (var npcIcon in _birthdayNPCs.Value)
            {
                npcIcon.DrawIcon();
            }
        }


        private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (Game1.eventUp)
                return;

            // Draw Icon Hover
            foreach (var npcIcon in _birthdayNPCs.Value)
            {
                npcIcon.DrawHoverText();
            }
        }

        #endregion


        #region Logic

        private void CheckForGiftGiven()
        {
            _birthdayNPCs.Value.RemoveAll(pair =>
            {
                var friendship = GetFriendshipWithNPC(pair.NPC.Name);
                return friendship is { GiftsToday: > 0 };
            });
        }

        private void CheckForBirthday()
        {
            _birthdayNPCs.Value.Clear();
            foreach (var character in Tools.GetAllCharacters())
            {
                if (!character.isBirthday(Game1.currentSeason, Game1.dayOfMonth))
                    continue; // Early escape if it doesn't match

                var friendship = GetFriendshipWithNPC(character.Name);
                if (friendship == null)
                    continue;

                var hasMaxFriendship = friendship.Points >=
                                       Utility.GetMaximumHeartsForCharacter(character) *
                                       NPC.friendshipPointsPerHeartLevel;
                if (Options.HideBirthdayIfFullFriendShip && hasMaxFriendship)
                    continue;

                _birthdayNPCs.Value.Add(new BirthdayIconComponent(character));
            }
        }

        private static Friendship? GetFriendshipWithNPC(string name)
        {
            try
            {
                return Game1.player.friendshipData.TryGetValue(name, out var friendship) ? friendship : null;
            }
            catch (Exception ex)
            {
                ModEntry.MonitorObject.LogOnce("Error while getting information about the birthday of " + name,
                    LogLevel.Error);
                ModEntry.MonitorObject.Log(ex.ToString());
            }

            return null;
        }

        #endregion
    }
}
