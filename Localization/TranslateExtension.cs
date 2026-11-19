using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Chronify.Localization;

// Прив'язка, на відміну від готового рядка, оновлює текст у відкритому вікні.
public sealed class TranslateExtension(string key) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding(nameof(LocalizationService.LanguageCode))
        {
            Source = LocalizationService.Current,
            Mode = BindingMode.OneWay,
            Converter = new FuncValueConverter<string, string>(_ => Texts.Get(key))
        };
    }
}
