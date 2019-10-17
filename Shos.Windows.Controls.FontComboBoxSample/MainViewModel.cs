#nullable enable

using Prism.Mvvm;

namespace Shos.Windows.Sample
{
    public enum FontSizeKind
    {
        Small  = 12,
        Medium = 16,
        Large  = 20         
    }

    class MainViewModel : BindableBase
    {
        FontSizeKind fontSizeKind = FontSizeKind.Medium;

        public int FontSize => (int)fontSizeKind;

        public FontSizeKind FontSizeKind {
            get => fontSizeKind;
            set {
                SetProperty(ref fontSizeKind, value);
                RaisePropertyChanged(nameof(FontSize));
            }
        }

        string selectedFontFamilyName;

        public string SelectedFontFamilyName {
            get => selectedFontFamilyName;
            set => SetProperty(ref selectedFontFamilyName, value);
        }

        string selectedLocalFontFamilyName;

        public string SelectedLocalFontFamilyName {
            get => selectedLocalFontFamilyName;
            set => SetProperty(ref selectedLocalFontFamilyName, value);
        }

        public MainViewModel()
            => selectedFontFamilyName = selectedLocalFontFamilyName = "Arial";
    }
}
