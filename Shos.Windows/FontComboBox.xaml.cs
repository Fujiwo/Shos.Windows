using System;
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
        {
            try {
                var typeface = @this.GetTypefaces().FirstOrDefault();
                return typeface is not null && typeface.TryGetGlyphTypeface(out var glyphTypeface) && glyphTypeface.Symbol;
            } catch (Exception) { // A broken font file must not break the whole control.
                return false;
            }
        }
    }

    class FontComboBoxItemViewModel
    {
        public static XmlLanguage Language { get; } = XmlLanguage.GetLanguage(Thread.CurrentThread.CurrentCulture.Name);

        public static FontFamily DefaultFontFamily => SystemFonts.MenuFontFamily;

        public FontFamily FontFamily          { get; }
        public FontFamily DisplayFontFamily   { get; }
        public string     FontFamilyName      { get; }
        public string     LocalFontFamilyName { get; }

        public FontComboBoxItemViewModel(FontFamily fontFamily)
        {
            FontFamily          = fontFamily;
            DisplayFontFamily   = fontFamily.IsSymbol() ? DefaultFontFamily : fontFamily;
            FontFamilyName      = fontFamily.Source;
            LocalFontFamilyName = ToLocalFontFamilyName(fontFamily);
        }

        public override string ToString() => LocalFontFamilyName;

        public static string ToLocalFontFamilyName(FontFamily fontFamily)
            => fontFamily.FamilyNames.TryGetValue(Language, out var localFontFamilyName) ? localFontFamilyName : fontFamily.Source;
    }

    /// <summary>
    /// A <see cref="ComboBox"/> to select one of the system font families.
    /// Each item is drawn with its own font (symbol fonts are drawn with the default font).
    /// </summary>
    /// <remarks>
    /// <see cref="SelectedFontFamily"/>, <see cref="SelectedFontFamilyName"/>, <see cref="SelectedLocalFontFamilyName"/> and
    /// <see cref="System.Windows.Controls.Primitives.Selector.SelectedItem"/> are kept synchronized and bind two-way by default.
    /// When a value that matches no system font is set, the selection is cleared.
    /// </remarks>
    public partial class FontComboBox : ComboBox
    {
        static readonly Lazy<IReadOnlyList<FontComboBoxItemViewModel>> items
            = new(() => Fonts.SystemFontFamilies
                             .Select(fontFamily => new FontComboBoxItemViewModel(fontFamily))
                             .OrderBy(fontComboBoxItemViewModel => fontComboBoxItemViewModel.LocalFontFamilyName)
                             .ToList());

        static FontFamily DefaultFontFamily => FontComboBoxItemViewModel.DefaultFontFamily;

        /// <summary>Identifies the <see cref="SelectedFontFamily"/> dependency property.</summary>
        public static readonly DependencyProperty SelectedFontFamilyProperty
            = DependencyProperty.Register(
                  nameof(SelectedFontFamily),
                  typeof(FontFamily),
                  typeof(FontComboBox),
                  new FrameworkPropertyMetadata(
                      DefaultFontFamily,
                      FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                      (d, e) => ((FontComboBox)d).Select(FindItem(e.NewValue is FontFamily fontFamily ? fontFamily.Source : null, item => item.FontFamilyName), e.Property)
                  )
              );

        /// <summary>Gets or sets the selected font family.</summary>
        public FontFamily? SelectedFontFamily {
            get => (FontFamily?)GetValue(SelectedFontFamilyProperty);
            set => SetValue(SelectedFontFamilyProperty, value);
        }

        /// <summary>Identifies the <see cref="SelectedFontFamilyName"/> dependency property.</summary>
        public static readonly DependencyProperty SelectedFontFamilyNameProperty
            = DependencyProperty.Register(
                  nameof(SelectedFontFamilyName),
                  typeof(string),
                  typeof(FontComboBox),
                  new FrameworkPropertyMetadata(
                      DefaultFontFamily.Source,
                      FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                      (d, e) => ((FontComboBox)d).Select(FindItem(e.NewValue as string, item => item.FontFamilyName), e.Property)
                  )
              );

        /// <summary>Gets or sets the name (<see cref="FontFamily.Source"/>) of the selected font family. e.g. "Yu Gothic UI"</summary>
        public string SelectedFontFamilyName {
            get => (string)GetValue(SelectedFontFamilyNameProperty);
            set => SetValue(SelectedFontFamilyNameProperty, value);
        }

        /// <summary>Identifies the <see cref="SelectedLocalFontFamilyName"/> dependency property.</summary>
        public static readonly DependencyProperty SelectedLocalFontFamilyNameProperty
            = DependencyProperty.Register(
                  nameof(SelectedLocalFontFamilyName),
                  typeof(string),
                  typeof(FontComboBox),
                  new FrameworkPropertyMetadata(
                      FontComboBoxItemViewModel.ToLocalFontFamilyName(DefaultFontFamily),
                      FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                      (d, e) => ((FontComboBox)d).Select(FindItem(e.NewValue as string, item => item.LocalFontFamilyName), e.Property)
                  )
              );

        /// <summary>Gets or sets the localized name (for the current culture) of the selected font family. e.g. "游ゴシック UI"</summary>
        public string SelectedLocalFontFamilyName {
            get => (string)GetValue(SelectedLocalFontFamilyNameProperty);
            set => SetValue(SelectedLocalFontFamilyNameProperty, value);
        }

        bool isSynchronizing;

        /// <summary>Initializes a new instance of the <see cref="FontComboBox"/> class.</summary>
        public FontComboBox()
        {
            InitializeComponent();

            Language    = FontComboBoxItemViewModel.Language;
            ItemsSource = items.Value;
            Select(FindItem(SelectedFontFamilyName, item => item.FontFamilyName), SelectedFontFamilyNameProperty);
        }

        /// <inheritdoc/>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            Select(SelectedItem as FontComboBoxItemViewModel, SelectedItemProperty);
        }

        static FontComboBoxItemViewModel? FindItem(string? name, Func<FontComboBoxItemViewModel, string> nameOf)
            => string.IsNullOrEmpty(name) ? null : items.Value.FirstOrDefault(item => string.Equals(nameOf(item), name, StringComparison.OrdinalIgnoreCase));

        // Synchronizes SelectedItem and the Selected... properties except the one that has been changed (origin).
        // SetCurrentValue is used so that bindings on these properties are kept.
        void Select(FontComboBoxItemViewModel? item, DependencyProperty origin)
        {
            if (isSynchronizing)
                return;
            isSynchronizing = true;
            try {
                if (origin != SelectedItemProperty)
                    SetCurrentValue(SelectedItemProperty, item);
                if (origin != SelectedFontFamilyProperty)
                    SetCurrentValue(SelectedFontFamilyProperty, item?.FontFamily);
                if (origin != SelectedFontFamilyNameProperty)
                    SetCurrentValue(SelectedFontFamilyNameProperty, item?.FontFamilyName ?? "");
                if (origin != SelectedLocalFontFamilyNameProperty)
                    SetCurrentValue(SelectedLocalFontFamilyNameProperty, item?.LocalFontFamilyName ?? "");
            } finally {
                isSynchronizing = false;
            }
        }
    }
}
