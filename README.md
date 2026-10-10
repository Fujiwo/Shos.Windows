# Shos.Windows

WPF (.NET) Library for .NET 8.0 or later.

- [FontComboBox](#fontcombobox): a combo box to select a font family
- [EnumToBooleanConverter](#enumtobooleanconverter): for multiple radio buttons binding to a single enum

Install as a NuGet Package: [NuGet Gallery | Shos.Windows](https://www.nuget.org/packages/Shos.Windows "NuGet Gallery | Shos.Windows")

```
dotnet add package Shos.Windows
```

## Projects

| Project | Description |
|---|---|
| Shos.Windows | WPF (.NET) Library (namespaces: `Shos.Windows.Controls`, `Shos.Windows.Data`) |
| Shos.Windows.Sample | WPF (.NET) Sample for Shos.Windows |
| Shos.Windows.Tests | Unit tests (xUnit) |

## Controls

### FontComboBox

Namespace: `Shos.Windows.Controls`

Derives from [System.Windows.Controls.ComboBox](https://learn.microsoft.com/dotnet/api/system.windows.controls.combobox?view=windowsdesktop-8.0).

A combo box to select one of the system font families.
Items are drawn with each font (symbol fonts are drawn with the default font).

#### Properties

The following properties are kept synchronized with each other and with `SelectedItem`, and bind two-way by default.
When a value that matches no system font is set, the selection is cleared.

| Property | Type | Description |
|---|---|---|
| SelectedFontFamily | FontFamily | The selected font family. |
| SelectedFontFamilyName | string | The name (`FontFamily.Source`) of the selected font family. e.g. "Yu Gothic UI" (case-insensitive) |
| SelectedLocalFontFamilyName | string | The localized name (for the current culture) of the selected font family. e.g. "游ゴシック UI" (case-insensitive) |

#### Example

```xml
<Window xmlns:shoswindowscontrols="clr-namespace:Shos.Windows.Controls;assembly=Shos.Windows" ...>
    <StackPanel>
        <shoswindowscontrols:FontComboBox SelectedFontFamilyName="{Binding SelectedFontFamilyName}" />
        <TextBox FontFamily="{Binding SelectedFontFamilyName}" />
    </StackPanel>
</Window>
```

## Converters

### EnumToBooleanConverter

Namespace: `Shos.Windows.Data`

Converts an enum value to `true` when it equals the converter parameter, and converts `true` back to the parameter value.
Used to bind multiple radio buttons to a single enum property.
The converter parameter can be the name of an enum member (e.g. `ConverterParameter=Small`) or the enum value itself (e.g. `ConverterParameter={x:Static local:FontSizeKind.Small}`).

#### Example

```csharp
public enum FontSizeKind { Small, Medium, Large }
```

```xml
<Window xmlns:shoswindowsdata="clr-namespace:Shos.Windows.Data;assembly=Shos.Windows" ...>
    <Window.Resources>
        <shoswindowsdata:EnumToBooleanConverter x:Key="enumToBooleanConverter" />
    </Window.Resources>
    <StackPanel>
        <RadioButton Content="Small"  IsChecked="{Binding FontSizeKind, Converter={StaticResource enumToBooleanConverter}, ConverterParameter={x:Static local:FontSizeKind.Small}}" />
        <RadioButton Content="Medium" IsChecked="{Binding FontSizeKind, Converter={StaticResource enumToBooleanConverter}, ConverterParameter={x:Static local:FontSizeKind.Medium}}" />
        <RadioButton Content="Large"  IsChecked="{Binding FontSizeKind, Converter={StaticResource enumToBooleanConverter}, ConverterParameter={x:Static local:FontSizeKind.Large}}" />
    </StackPanel>
</Window>
```

## Author Info

Fujio Kojima: a software developer in Japan
* Microsoft MVP for Development Tools - Visual C# (Jul. 2005 - Dec. 2014)
* Microsoft MVP for .NET (Jan. 2015 - Oct. 2015)
* Microsoft MVP for Visual Studio and Development Technologies (Nov. 2015 - Jun. 2018)
* Microsoft MVP for Developer Technologies (Nov. 2018 - Jun. 2027)
* [MVP Profile](https://mvp.microsoft.com/en-us/PublicProfile/21482 "MVP Profile")
* [Blog (Japanese)](https://wp.shos.info "Blog (Japanese)")
* [Web Site (Japanese)](https://www.shos.info "Web Site (Japanese)")

## License

This library is under the MIT License.
