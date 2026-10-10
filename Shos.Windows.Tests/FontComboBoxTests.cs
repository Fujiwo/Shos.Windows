using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Shos.Windows.Controls;

namespace Shos.Windows.Tests;

public class FontComboBoxTests
{
    class Source : INotifyPropertyChanged
    {
        string fontFamilyName = "";

        public string FontFamilyName {
            get => fontFamilyName;
            set {
                fontFamilyName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FontFamilyName)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    static void RunOnSta(Action action)
    {
        ExceptionDispatchInfo? exception = null;
        var thread = new Thread(() => {
            try {
                action();
            } catch (Exception e) {
                exception = ExceptionDispatchInfo.Capture(e);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        exception?.Throw();
    }

    static FontComboBoxItemViewModel? SelectedItemOf(FontComboBox fontComboBox)
        => fontComboBox.SelectedItem as FontComboBoxItemViewModel;

    [Fact]
    public void SelectedItem_IsSynchronizedWithDefaultValuesInitially()
        => RunOnSta(() => {
            var fontComboBox = new FontComboBox();
            Assert.Equal(fontComboBox.SelectedFontFamilyName     , SelectedItemOf(fontComboBox)?.FontFamilyName     );
            Assert.Equal(fontComboBox.SelectedLocalFontFamilyName, SelectedItemOf(fontComboBox)?.LocalFontFamilyName);
        });

    [Fact]
    public void SelectedFontFamily_AcceptsNewFontFamilyInstance()
        => RunOnSta(() => {
            var fontComboBox = new FontComboBox { SelectedFontFamily = new FontFamily("Arial") };
            Assert.Equal("Arial", SelectedItemOf(fontComboBox)?.FontFamilyName);
            Assert.Equal("Arial", fontComboBox.SelectedFontFamilyName);
        });

    [Fact]
    public void SelectedFontFamilyName_IsCaseInsensitive()
        => RunOnSta(() => {
            var fontComboBox = new FontComboBox { SelectedFontFamilyName = "arial" };
            Assert.Equal("Arial", SelectedItemOf(fontComboBox)?.FontFamilyName);
        });

    [Fact]
    public void SelectedFontFamilyName_SelectsTheFontItselfEvenIfSymbolFontsExist()
        => RunOnSta(() => {
            var fontComboBox = new FontComboBox { SelectedFontFamilyName = "Arial" };
            fontComboBox.SelectedFontFamilyName = SystemFonts.MenuFontFamily.Source;
            Assert.Equal(SystemFonts.MenuFontFamily.Source, SelectedItemOf(fontComboBox)?.FontFamily.Source);
        });

    [Fact]
    public void SymbolFonts_KeepTheirOwnName()
        => RunOnSta(() => {
            var fontComboBox = new FontComboBox();
            var symbolItems  = fontComboBox.Items.Cast<FontComboBoxItemViewModel>()
                                           .Where(item => !Equals(item.DisplayFontFamily, item.FontFamily));
            Assert.All(symbolItems, item => Assert.Equal(item.FontFamily.Source, item.FontFamilyName));
        });

    [Fact]
    public void UnknownFontFamilyName_ClearsSelection()
        => RunOnSta(() => {
            var fontComboBox = new FontComboBox { SelectedFontFamilyName = "Arial" };
            fontComboBox.SelectedFontFamilyName = "No Such Font";
            Assert.Null(fontComboBox.SelectedItem);
            Assert.Null(fontComboBox.SelectedFontFamily);
            Assert.Equal("", fontComboBox.SelectedLocalFontFamilyName);
            Assert.Equal("No Such Font", fontComboBox.SelectedFontFamilyName);
        });

    [Fact]
    public void Selection_UpdatesBindingSourceByDefault()
        => RunOnSta(() => {
            var source       = new Source { FontFamilyName = "Arial" };
            var fontComboBox = new FontComboBox();
            fontComboBox.SetBinding(FontComboBox.SelectedFontFamilyNameProperty, new Binding(nameof(Source.FontFamilyName)) { Source = source });
            Assert.Equal("Arial", SelectedItemOf(fontComboBox)?.FontFamilyName);

            fontComboBox.SelectedItem = fontComboBox.Items.Cast<FontComboBoxItemViewModel>().First(item => item.FontFamilyName != "Arial");
            Assert.Equal(SelectedItemOf(fontComboBox)?.FontFamilyName, source.FontFamilyName);
        });

    [Fact]
    public void Selection_KeepsOneWayBinding()
        => RunOnSta(() => {
            var source       = new Source { FontFamilyName = "Arial" };
            var fontComboBox = new FontComboBox();
            fontComboBox.SetBinding(FontComboBox.SelectedFontFamilyNameProperty, new Binding(nameof(Source.FontFamilyName)) { Source = source, Mode = BindingMode.OneWay });

            fontComboBox.SelectedItem = fontComboBox.Items.Cast<FontComboBoxItemViewModel>().First(item => item.FontFamilyName != "Arial");
            source.FontFamilyName = "Arial";
            Assert.Equal("Arial", SelectedItemOf(fontComboBox)?.FontFamilyName);
        });
}
