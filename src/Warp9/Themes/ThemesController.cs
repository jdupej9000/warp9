using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace Warp9.Themes
{
    public static class ThemesController
    {
        public static ThemeType CurrentTheme { get; set; }

        public static event EventHandler ThemeChanged;

        private static ResourceDictionary ThemeDictionary
        {
            get => Application.Current.Resources.MergedDictionaries[0];
            set => Application.Current.Resources.MergedDictionaries[0] = value;
        }

        private static ResourceDictionary ControlColours
        {
            get => Application.Current.Resources.MergedDictionaries[1];
            set => Application.Current.Resources.MergedDictionaries[1] = value;
        }

        private static void RefreshControls()
        {
            // This seems to be faster than reloading the whole file, and it also seems to work
            Collection<ResourceDictionary> merged = Application.Current.Resources.MergedDictionaries;
            ResourceDictionary dictionary = merged[2];
            merged.RemoveAt(2);
            merged.Insert(2, dictionary);

            // If the above doesn't work then fall back to this
            // Application.Current.Resources.MergedDictionaries[2] = new ResourceDictionary() { Source = new Uri("Themes/Controls.xaml", UriKind.Relative) };

            // Doesn't work
            // FieldInfo field = typeof(PropertyMetadata).GetField("_defaultValue", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField);
            // object style = merged[2][typeof(ComboBox)];
            // field.SetValue(DataGridComboBoxColumn.ElementStyleProperty.DefaultMetadata, style);
        }

        public static void SetTheme(ThemeType theme)
        {
            if(theme == CurrentTheme) 
                return;

            string themeName = theme.GetName();
            if (string.IsNullOrEmpty(themeName))
            {
                return;
            }

            CurrentTheme = theme;
            ThemeDictionary = new ResourceDictionary() { Source = new Uri($"Themes/ColourDictionaries/{themeName}.xaml", UriKind.Relative) };
            ControlColours = new ResourceDictionary() { Source = new Uri("Themes/ControlColours.xaml", UriKind.Relative) };
            RefreshControls();

            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        public static object? GetResource(object key)
        {
            return ThemeDictionary[key];
        }

        public static Brush GetBrush(string name)
        {
            object? r = GetResource(name);

            if (r is null)
                r = ControlColours[name];

            if (r is Color c)
                return new SolidColorBrush(c);
            else if (r is Brush b)
                return b;

            return new SolidColorBrush(Colors.White);
        }

        public static System.Drawing.Color GetColor(string name)
        {
            object? r = GetResource(name);

            if (r is null)
                r = ControlColours[name];

            if (r is System.Drawing.Color c)
                return c;
            else if (r is Color mc)
                return System.Drawing.Color.FromArgb(mc.A, mc.R, mc.G, mc.B);
            else if (r is SolidColorBrush b)
                return System.Drawing.Color.FromArgb(b.Color.A, b.Color.R, b.Color.G, b.Color.B);

            return System.Drawing.Color.White;
        }
    }
}