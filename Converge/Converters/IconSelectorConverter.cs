using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Converge.Converters
{
    public class IconSelectorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var uri = value == null
                ? "avares://Converge/Assets/folder.png"
                : "avares://Converge/Assets/connection.png";

            return new Bitmap(AssetLoader.Open(new Uri(uri)));
        }


        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

}

