#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace Shos.Windows.Controls
{
    static class FontFamilyExtension
    {
        public static bool IsSymbol(this FontFamily @this)
            => @this.GetTypefaces().First().TryGetGlyphTypeface(out var glyphTypeface) && glyphTypeface != null ? glyphTypeface.Symbol : false;
    }

    class FontComboBoxItemViewModel
    {
        static readonly XmlLanguage language = XmlLanguage.GetLanguage(Thread.CurrentThread.CurrentCulture.Name);

        public static FontFamily DefaultFontFamily => SystemFonts.MenuFontFamily;

        public FontFamily? FontFamily          { get; private set; }
        public string      FontFamilyName      { get; private set; }
        public string      LocalFontFamilyName { get; private set; }

        public FontComboBoxItemViewModel(FontFamily fontFamily)
        {
            FontFamily          = fontFamily;
            FontFamilyName      = fontFamily.IsSymbol() ? DefaultFontFamily.Source : fontFamily?.Source ?? "";
            LocalFontFamilyName = ToLocalFontFamilyName(fontFamily);
        }

        string ToLocalFontFamilyName(FontFamily? fontFamily)
                => fontFamily == null ? "" : fontFamily.FamilyNames.FirstOrDefault(eachFontFamily => eachFontFamily.Key == language).Value ?? fontFamily.Source;
    }

    public partial class FontComboBox : ComboBox
    {
        static readonly IEnumerable<FontComboBoxItemViewModel> itemsSource
            = Fonts.SystemFontFamilies
                   .Select(fontFamily => new FontComboBoxItemViewModel(fontFamily))
                   .OrderBy(fontComboBoxItemViewModel => fontComboBoxItemViewModel.LocalFontFamilyName)
                   .ToList();

        static FontFamily DefaultFontFamily => FontComboBoxItemViewModel.DefaultFontFamily;

        static string DefaultLocalFontFamilyName
            => itemsSource.FirstOrDefault(fontComboBoxItemViewModel => fontComboBoxItemViewModel.FontFamilyName == DefaultFontFamily.Source)?.LocalFontFamilyName ?? "";

        public static readonly DependencyProperty SelectedFontFamilyProperty
            = DependencyProperty.Register(
                  "SelectedFontFamily",
                  typeof(FontFamily),
                  typeof(FontComboBox),
                  new PropertyMetadata(
                      DefaultFontFamily,
                      SelectedFontFamilyPropertyChanged
                  )
              );

        public FontFamily? SelectedFontFamily {
            get => (FontFamily)GetValue(SelectedFontFamilyProperty);
            set => SetValue(SelectedFontFamilyProperty, value);
        }

        static void SelectedFontFamilyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var fontFamily                        = e.NewValue as FontFamily;
            var selectedFontComboBoxItemViewModel = itemsSource.FirstOrDefault(fontComboBoxItemViewModel => fontComboBoxItemViewModel.FontFamily == fontFamily);
            if (selectedFontComboBoxItemViewModel != null) {
                d.SetValue(SelectedItemProperty               , selectedFontComboBoxItemViewModel                           );
                d.SetValue(SelectedFontFamilyNameProperty     , selectedFontComboBoxItemViewModel?.FontFamilyName      ?? "");
                d.SetValue(SelectedLocalFontFamilyNameProperty, selectedFontComboBoxItemViewModel?.LocalFontFamilyName ?? "");
            }
        }

        public static readonly DependencyProperty SelectedFontFamilyNameProperty
            = DependencyProperty.Register(
                  "SelectedFontFamilyName",
                  typeof(string),
                  typeof(FontComboBox),
                  new PropertyMetadata(
                      DefaultFontFamily.Source,
                      SelectedFontFamilyNamePropertyChanged
                  )
              );

        public string SelectedFontFamilyName {
            get => (string)GetValue(SelectedFontFamilyNameProperty);
            set => SetValue(SelectedFontFamilyNameProperty, value);
        }

        static void SelectedFontFamilyNamePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var fontFamilyName = e.NewValue as string;
            var selectedFontComboBoxItemViewModel = itemsSource.FirstOrDefault(fontComboBoxItemViewModel => fontComboBoxItemViewModel.FontFamilyName == fontFamilyName);
            if (selectedFontComboBoxItemViewModel != null) {
                d.SetValue(SelectedItemProperty, selectedFontComboBoxItemViewModel);
                d.SetValue(SelectedFontFamilyProperty, selectedFontComboBoxItemViewModel?.FontFamily);
                d.SetValue(SelectedLocalFontFamilyNameProperty, selectedFontComboBoxItemViewModel?.LocalFontFamilyName ?? "");
            }
        }

        public static readonly DependencyProperty SelectedLocalFontFamilyNameProperty
            = DependencyProperty.Register(
                  "SelectedLocalFontFamilyName",
                  typeof(string),
                  typeof(FontComboBox),
                  new PropertyMetadata(
                      DefaultLocalFontFamilyName,
                      SelectedLocalFontFamilyNamePropertyChanged
                  )
              );

        public string SelectedLocalFontFamilyName {
            get => (string)GetValue(SelectedLocalFontFamilyNameProperty);
            set => SetValue(SelectedLocalFontFamilyNameProperty, value);
        }

        static void SelectedLocalFontFamilyNamePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var localFontFamilyName               = e.NewValue as string;
            var selectedFontComboBoxItemViewModel = itemsSource.FirstOrDefault(fontComboBoxItemViewModel => fontComboBoxItemViewModel.LocalFontFamilyName == localFontFamilyName);
            if (selectedFontComboBoxItemViewModel != null) {
                d.SetValue(SelectedItemProperty          , selectedFontComboBoxItemViewModel                );
                d.SetValue(SelectedFontFamilyProperty    , selectedFontComboBoxItemViewModel?.FontFamily    );
                d.SetValue(SelectedFontFamilyNameProperty, selectedFontComboBoxItemViewModel?.FontFamilyName);
            }
        }

        public FontComboBox()
        {
            InitializeComponent();

            Language    = XmlLanguage.GetLanguage(Thread.CurrentThread.CurrentCulture.Name);
            ItemsSource = itemsSource;

            SelectionChanged += (sender, e) => {
                var fontComboBoxItemViewModel = SelectedItem as FontComboBoxItemViewModel;
                SelectedFontFamily          = fontComboBoxItemViewModel?.FontFamily               ;
                SelectedFontFamilyName      = fontComboBoxItemViewModel?.FontFamilyName      ?? "";
                SelectedLocalFontFamilyName = fontComboBoxItemViewModel?.LocalFontFamilyName ?? "";
            };
        }
    }
}
