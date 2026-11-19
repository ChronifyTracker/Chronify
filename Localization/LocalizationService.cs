using System.ComponentModel;
using System.Globalization;

namespace Chronify.Localization;

public sealed class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Current { get; } = new();

    private IReadOnlyDictionary<string, string> _dictionary = RussianTexts.Values;
    private readonly List<WeakReference<ILocalizable>> _viewModels = [];

    public string LanguageCode { get; private set; } = "ru";
    public CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("ru-RU");

    public string this[string key]
    {
        get
        {
            return _dictionary.TryGetValue(key, out string? value) ? value : key;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Register(ILocalizable viewModel)
    {
        // Закриті вікна не повинні залишатися в пам'яті через сервіс перекладів.
        _viewModels.Add(new WeakReference<ILocalizable>(viewModel));
    }

    public static bool IsSupported(string code)
    {
        return code is "ru" or "uk" or "en";
    }

    public void ChangeLanguage(string code)
    {
        if (!IsSupported(code))
        {
            throw new ArgumentException(Texts.Get("Errors.Language"));
        }

        bool changed = LanguageCode != code;
        _dictionary = code switch
        {
            "uk" => UkrainianTexts.Values,
            "en" => EnglishTexts.Values,
            _ => RussianTexts.Values
        };
        LanguageCode = code;
        Culture = CultureInfo.GetCultureInfo(code switch
        {
            "uk" => "uk-UA",
            "en" => "en-GB",
            _ => "ru-RU"
        });

        // Нові вікна та фонові операції також використовують обрану культуру.
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
        if (!changed)
        {
            return;
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LanguageCode)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Culture)));
        foreach (var reference in _viewModels.ToArray())
        {
            if (reference.TryGetTarget(out var viewModel))
            {
                viewModel.RefreshLanguage();
            }
        }
        _viewModels.RemoveAll(reference => !reference.TryGetTarget(out _));
    }
}
