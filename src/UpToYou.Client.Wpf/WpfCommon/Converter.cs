using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace UpToYou.Client.Wpf {
[MarkupExtensionReturnType(typeof(IValueConverter))]
internal abstract class MarkupConverter : MarkupExtension, IValueConverter {
    public MarkupConverter() { }
    public override object ProvideValue(IServiceProvider serviceProvider) => this;
    public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

internal class BoolToVisibilityConverter: MarkupConverter {
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) => 
        value != null && (bool) value ? Visibility.Visible : Visibility.Collapsed;
}

internal class BoolToVisibilityInverseConverter: MarkupConverter {
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) => 
        value != null && (bool) value ? Visibility.Collapsed : Visibility.Visible ;
}

internal class NullToVisibilityConverter: MarkupConverter {
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) => 
       value == null? Visibility.Collapsed : Visibility.Visible;
}

internal class BoolToInverseBoolConverter: MarkupConverter {
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) => 
        value != null && !((bool)value);
}

internal class AllTrueToVisibilityMultiConverter: IMultiValueConverter {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
        foreach (var value in values)
            if (!(value is bool b && b))
                return Visibility.Collapsed;

        return Visibility.Visible;
    }
        

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

}
