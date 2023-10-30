namespace UIInfoSuite2.Options
{
    public interface IOptionValue<T>
    {
        T GetValue();
        void SetValue(T newValue);
    }
}
