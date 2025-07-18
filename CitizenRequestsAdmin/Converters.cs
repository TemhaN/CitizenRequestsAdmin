using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace CitizenRequestsAdmin
{
    public class AlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result;
            if (value is bool boolValue)
            {
                result = boolValue;
            }
            else if (value is int intValue)
            {
                result = intValue != 0;
            }
            else
            {
                return HorizontalAlignment.Right;
            }

            return result ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class BackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result;
            if (value is bool boolValue)
            {
                result = boolValue;
            }
            else if (value is int intValue)
            {
                result = intValue != 0;
            }
            else
            {
                return new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
            }

            return result
                ? new SolidColorBrush(Color.FromRgb(0x02, 0x88, 0xD1))
                : new SolidColorBrush(Color.FromRgb(0xD3, 0x2F, 0x2F));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class AnsweredToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result;
            if (value is bool boolValue)
            {
                result = boolValue;
            }
            else if (value is int intValue)
            {
                result = intValue != 0;
            }
            else
            {
                return new SolidColorBrush(Color.FromRgb(0xD3, 0x2F, 0x2F));
            }

            return result
                ? new SolidColorBrush(Color.FromRgb(0x02, 0x88, 0xD1))
                : new SolidColorBrush(Color.FromRgb(0xD3, 0x2F, 0x2F));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class AnsweredToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = value is bool boolValue ? boolValue : (value is int intValue ? intValue != 0 : false);
            System.Diagnostics.Debug.WriteLine($"AnsweredToVisibilityConverter: IsAnswered = {result}");

            if (parameter?.ToString() == "Thickness")
            {
                return result ? new Thickness(0) : new Thickness(4); // 4 для неотвеченных, 0 для отвеченных
            }

            return result ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class DescriptionToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string description)
            {
                double width = 300 + (description.Length * 0.5);
                return Math.Min(width, 450);
            }
            return 300;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class AnsweredToBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = value is bool boolValue ? boolValue : (value is int intValue ? intValue != 0 : false);
            System.Diagnostics.Debug.WriteLine($"AnsweredToBorderConverter: IsAnswered = {result}");
            return result
                ? new SolidColorBrush(Color.FromRgb(0x02, 0x88, 0xD1)) // Синий для отвеченных
                : new SolidColorBrush(Color.FromRgb(0xD3, 0x2F, 0x2F)); // Красный для неотвеченных
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class AnsweredToShadowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = value is bool boolValue ? boolValue : (value is int intValue ? intValue != 0 : false);
            System.Diagnostics.Debug.WriteLine($"AnsweredToShadowConverter: IsAnswered = {result}");
            return result ? Elevation.Dp2 : Elevation.Dp7;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class StatusToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status)
                {
                    case "Новое":
                        return new SolidColorBrush(Color.FromRgb(0x02, 0x88, 0xD1));
                    case "В обработке":
                        return new SolidColorBrush(Color.FromRgb(0xFB, 0x8C, 0x00));
                    case "Рассмотрено":
                        return new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
                    case "Отклонено":
                        return new SolidColorBrush(Color.FromRgb(0xD3, 0x2F, 0x2F));
                    default:
                        return new SolidColorBrush(Color.FromRgb(0x42, 0x42, 0x42));
                }
            }
            return new SolidColorBrush(Color.FromRgb(0x42, 0x42, 0x42));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
    public class WidthToCardWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is double wrapPanelWidth && !double.IsNaN(wrapPanelWidth) && wrapPanelWidth > 0)
                {
                    int cardsPerRow = (int)((wrapPanelWidth + 10) / 310);
                    if (cardsPerRow < 1) cardsPerRow = 1;
                    double totalMargin = (cardsPerRow - 1) * 10;
                    double cardWidth = (wrapPanelWidth - totalMargin) / cardsPerRow;
                    double finalWidth = Math.Max(300, Math.Min(600, cardWidth));
                    System.Diagnostics.Debug.WriteLine($"WrapPanelWidth: {wrapPanelWidth}, CardsPerRow: {cardsPerRow}, CardWidth: {finalWidth}");
                    return finalWidth;
                }
                System.Diagnostics.Debug.WriteLine($"WidthToCardWidthConverter: Invalid wrapPanelWidth: {value}");
                return 300;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WidthToCardWidthConverter Exception: {ex.Message}");
                return 300;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}