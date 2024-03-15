using System;
using System.Collections.Generic;
using StardewModdingAPI;
using UIInfoSuite2.Options;

namespace UIInfoSuite2.UIElements.Base
{
    internal abstract class UIComponentBase : IDisposable
    {
       protected readonly IModHelper Helper;
        protected readonly List<ModOptionsElement> OptionElements = new();
        protected readonly ModOptions Options;

        private bool _enabled;
        protected static IMonitor Logger => ModEntry.MonitorObject;

        #region Lifecycle

        protected UIComponentBase(IModHelper helper, ModOptions options)
        {
            Helper = helper;
            Options = options;
            _Init();
        }

        private void _Init()
        {
            Init();
            SetUpOptions();
            Reload();
        }

        protected virtual void Init()
        {
        }

        public virtual void Dispose()
        {
            Disable();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region UI Element Enable / Disable

        protected abstract bool ShouldEnable();

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }

        public void Enable()
        {
            if (_enabled)
            {
                return;
            }

            OnEnable();
            RegisterEvents();
            _RegisterInternalEvents();
            _enabled = true;
        }

        public void Disable()
        {
            if (!_enabled)
            {
                return;
            }

            OnDisable();
            _UnregisterInternalEvents();
            UnregisterEvents();
            _enabled = false;
        }

        public void Reload()
        {
            Disable();
            if (ShouldEnable())
            {
                Enable();
            }
        }

        #endregion

        #region Options

        protected abstract void SetUpOptions();

        protected virtual void OnOptionsChanged(string? _, dynamic? __)
        {
            Reload();
        }

        public IEnumerable<ModOptionsElement> GetOptionsList()
        {
            return OptionElements.AsReadOnly();
        }

        #endregion

        #region Events

        protected abstract void RegisterEvents();

        protected abstract void UnregisterEvents();

        protected virtual void _RegisterInternalEvents()
        {
        }

        protected virtual void _UnregisterInternalEvents()
        {
        }

        #endregion
    }
}
