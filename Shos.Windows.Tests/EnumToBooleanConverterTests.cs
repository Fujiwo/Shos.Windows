using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Shos.Windows.Data;

namespace Shos.Windows.Tests;

public class EnumToBooleanConverterTests
{
    enum Size { Small, Medium, Large }

    enum ByteSize : byte { Small = 1, Large = 2 }

    readonly EnumToBooleanConverter converter = new();

    object Convert(object? value, object? parameter)
        => converter.Convert(value, typeof(bool?), parameter, CultureInfo.InvariantCulture);

    object ConvertBack(object? value, Type targetType, object? parameter)
        => converter.ConvertBack(value, targetType, parameter, CultureInfo.InvariantCulture);

    [Fact]
    public void Convert_ReturnsWhetherValueEqualsParameterName()
    {
        Assert.Equal(true , Convert(Size.Medium, "Medium"));
        Assert.Equal(false, Convert(Size.Medium, "Small" ));
    }

    [Fact]
    public void Convert_AcceptsEnumValueAsParameter()
    {
        Assert.Equal(true , Convert(Size.Large, Size.Large));
        Assert.Equal(false, Convert(Size.Large, Size.Small));
    }

    [Fact]
    public void Convert_SupportsNonInt32UnderlyingType()
        => Assert.Equal(true, Convert(ByteSize.Large, "Large"));

    [Theory]
    [InlineData(null    , "Small"  )]
    [InlineData("Small" , "Small"  )]
    [InlineData(1       , "Small"  )]
    public void Convert_ReturnsUnsetValueForNonEnumValue(object? value, string parameter)
        => Assert.Same(DependencyProperty.UnsetValue, Convert(value, parameter));

    [Theory]
    [InlineData(null    )]
    [InlineData(""      )]
    [InlineData("Smal"  )]
    public void Convert_ReturnsUnsetValueForInvalidParameter(string? parameter)
        => Assert.Same(DependencyProperty.UnsetValue, Convert(Size.Small, parameter));

    [Fact]
    public void ConvertBack_ReturnsParameterValueWhenChecked()
    {
        Assert.Equal(Size.Large, ConvertBack(true, typeof(Size ), "Large"   ));
        Assert.Equal(Size.Large, ConvertBack(true, typeof(Size?), "Large"   ));
        Assert.Equal(Size.Large, ConvertBack(true, typeof(Size ), Size.Large));
    }

    [Theory]
    [InlineData(false, "Large")]
    [InlineData(null , "Large")]
    [InlineData(true , "Huge" )]
    [InlineData(true , null   )]
    public void ConvertBack_ReturnsDoNothingOtherwise(object? value, string? parameter)
        => Assert.Same(Binding.DoNothing, ConvertBack(value, typeof(Size), parameter));
}
