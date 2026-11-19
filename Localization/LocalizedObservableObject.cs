using CommunityToolkit.Mvvm.ComponentModel;

namespace Chronify.Localization;

public abstract class LocalizedObservableObject : ObservableObject, ILocalizable
{
    protected LocalizedObservableObject()
    {
        LocalizationService.Current.Register(this);
    }

    public virtual void RefreshLanguage()
    {
        OnPropertyChanged(string.Empty);
    }
}
