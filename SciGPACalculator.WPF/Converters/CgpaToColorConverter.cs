using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GpaCalculator.WPF.Converters
{
    /// <summary>
    /// بيحوّل رقم الـ CGPA للون مناسب، بدل ما تفضل تقرا الرقم بالتفصيل كل مرة.
    /// العتبات (3.5 / 2.5) اختيار تقديري بسيط، سهل تغييره هنا لو حبيت.
    /// </summary>
    public class CgpaToColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush SuccessBrush = new(Color.FromRgb(0x27, 0xAE, 0x60));
        private static readonly SolidColorBrush WarningBrush = new(Color.FromRgb(0xE6, 0x7E, 0x22));
        private static readonly SolidColorBrush DangerBrush = new(Color.FromRgb(0xC0, 0x39, 0x2B));

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not decimal cgpa)
                return SystemColors.ControlDarkBrush; // مفيش بيانات بعد - لون محايد

            if (cgpa >= 3.5m) return SuccessBrush;
            if (cgpa >= 2.5m) return WarningBrush;
            return DangerBrush;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
