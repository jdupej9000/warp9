// The themes are copied with namespaces modified from
// https://github.com/AngryCarrot789/WPFDarkTheme
namespace Warp9.Themes
{
    public enum ThemeType
    {
        SoftDark = 0,      
        LightTheme
    }

    public static class ThemeTypeExtension
    {
        public static string GetName(this ThemeType type)
        {
            switch (type)
            {
                case ThemeType.SoftDark: return "SoftDark";               
                case ThemeType.LightTheme: return "LightTheme";
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}