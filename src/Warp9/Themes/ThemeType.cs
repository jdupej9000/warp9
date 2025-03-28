﻿// The themes are copied with namespaces modified from
// https://github.com/AngryCarrot789/WPFDarkTheme
namespace Warp9.Themes
{
    public enum ThemeType
    {
        SoftDark = 0,
        DeepDark,
        DarkGreyTheme,
        GreyTheme,
        LightTheme,
        RedBlackTheme
    }

    public static class ThemeTypeExtension
    {
        public static string GetName(this ThemeType type)
        {
            switch (type)
            {
                case ThemeType.SoftDark: return "SoftDark";
                case ThemeType.RedBlackTheme: return "RedBlackTheme";
                case ThemeType.DeepDark: return "DeepDark";
                case ThemeType.GreyTheme: return "GreyTheme";
                case ThemeType.DarkGreyTheme: return "DarkGreyTheme";
                case ThemeType.LightTheme: return "LightTheme";
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}