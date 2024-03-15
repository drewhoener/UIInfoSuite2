using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using UIInfoSuite2.Options;

namespace UIInfoSuite2.UIElements.Base
{
    internal abstract class HudIconComponent : UIComponentBase
    {
        private readonly PerScreen<List<AbstractHudIcon>> _icons = new();

        protected HudIconComponent(IModHelper helper, ModOptions options) : base(helper, options)
        {
        }

        protected abstract List<AbstractHudIcon> GenerateIcons();

        private void RegenIcons()
        {
            _icons.Value = GenerateIcons();
        }

        protected override void Init()
        {
            RegenIcons();
        }

        protected sealed override void _RegisterInternalEvents()
        {
            foreach (AbstractHudIcon abstractHudIcon in _icons.Value)
            {
                abstractHudIcon.RegisterEvents(Helper);
            }
        }

        protected sealed override void _UnregisterInternalEvents()
        {
            foreach (AbstractHudIcon abstractHudIcon in _icons.Value)
            {
                abstractHudIcon.UnregisterEvents(Helper);
            }
        }
    }
}
