using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using UpToYou.Core;

namespace UpToYou.Client.Wpf
{
internal class DateTimeToDateConverter: MarkupConverter {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) => 
            ((DateTime)value).ToString("d MMMM, yyyy");
    }

internal class ProgressWidthConverter: IMultiValueConverter {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
        var progress = (Progress)values[0];
        var maxWidth = (double)values[1];
        if (progress.Percentage.HasValue)
            return maxWidth * progress.Percentage.Value / 100;
        return maxWidth;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

internal class DoubleToRoundedConverter: MarkupConverter {
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) => 
        Math.Round((double)value, 0) ;
}

internal class ProgressToSpeedDescriptionConverter: MarkupConverter {
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value == null)
            return null;
        var progress = (Progress)value;
        string res = null;
        if (progress.Value == 0)
            return null;
        if (progress.TargetValue.HasValue) {
            var downloadedMb =progress.Value / 1_000_000d;
            var targetMb = progress.TargetValue.Value / 1_000_000d;
            res += $"{Math.Round(downloadedMb, 1)} of {Math.Round(targetMb,1)} mb, ";
        }

        var speed = progress.Speed.ProgressPerSec;
        if (speed < 1_000_000) {
            res += $"{Math.Round(speed / 1000)} kb/s";
        }
        else 
            res += $"{Math.Round(speed / 1_000_000,1)} mb/s";
        return res;
    }
}

//internal class UpdateVmToMenuVisibilityConverter: MarkupConverter {
//    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
//        if (value is UpdateViewModel vm) {
//            return !vm.IsInstalled && !vm.IsLastUpdate;
//        }
//    }
//}

internal class UpdateVmToInstallButtonVisibilityConverter: MarkupConverter  {
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is UpdateViewModel update) {
            return update.IsLastUpdate && !update.IsInstalled ? Visibility.Visible: Visibility.Collapsed;
        }
        return false;
    }
}

internal class UpdateVmToNewMarkerVisibilityConverter: MarkupConverter  {
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is UpdateViewModel update) {
            return update.IsNew && !update.IsLastUpdate && !update.IsInstalled && !update.IsInstalling ? Visibility.Visible: Visibility.Collapsed;
        }
        return false;
    }
}

internal class UpdateVmToMenuVisibilityVisibilityConverter: MarkupConverter  {
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is UpdateViewModel update) {
            return !update.IsNew && !update.IsLastUpdate && !update.IsInstalled && !update.IsInstalling ? Visibility.Visible: Visibility.Collapsed;
        }
        return false;
    }
}

}
