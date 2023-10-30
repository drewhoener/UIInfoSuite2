using System;

namespace UIInfoSuite2.Options
{
    public abstract class ModOptionsValueElement<T> : ModOptionsElement, IOptionValue<T>
    {
        protected readonly Action<string, T>? OnValueUpdate;

        protected ModOptionsValueElement(Action<string, T>? onValueUpdate, OptionStringWrapper label, int whichOption = -1, ModOptionsElement parent = null) : base(label, whichOption, parent)
        {
            OnValueUpdate = onValueUpdate;
        }

        public abstract T GetValue();
        public abstract void SetValue(T newValue);
    }
}
