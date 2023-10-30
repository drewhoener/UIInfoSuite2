namespace UIInfoSuite2.Options
{
    public class OptionStringWrapper
    {
        public string Str { get; private set; }
        private string? _translated;
        public OptionStringWrapper(string str, bool translateAutomatically = true)
        {
            Str = str;
            if (translateAutomatically)
            {
                Translated();
            }
        }

        public string Translated()
        {
            return _translated ??= ModEntry.GetTranslated(Str);
        }

        public override string ToString()
        {
            return _translated ?? Str;
        }
    }
}
