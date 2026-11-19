namespace Chronify.Localization;

// Об'єкт залишається тим самим під час перекладу, тому ComboBox зберігає вибір.
public sealed class LocalizedOption(string key) : LocalizedObservableObject
{
    public string Label => Texts.Get(key);

    public override string ToString()
    {
        return Label;
    }
}
