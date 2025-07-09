using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Warp9.ProjectExplorer
{
    public class ProjectItemTypeToIconConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProjectItemKind kind)
            {
                return Application.Current.FindResource(kind switch
                {
                    ProjectItemKind.Folder => "Folder",
                    ProjectItemKind.Gallery => "Camera",
                    ProjectItemKind.Viewer => "Scene",
                    ProjectItemKind.Table => "Table",
                    _ => "File"
                });
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
