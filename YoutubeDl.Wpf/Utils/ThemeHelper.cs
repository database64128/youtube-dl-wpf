using MaterialDesignThemes.Wpf;

namespace YoutubeDl.Wpf.Utils;

public static class ThemeHelper
{
    public static void SetCustomizedBaseTheme(this Theme theme, BaseTheme baseTheme)
    {
        switch (baseTheme)
        {
            case BaseTheme.Light:
                theme.SetLightTheme();
                break;
            case BaseTheme.Dark:
                theme.SetCustomizedDarkTheme();
                break;
            default:
                switch (Theme.GetSystemTheme())
                {
                    case BaseTheme.Dark:
                        theme.SetCustomizedDarkTheme();
                        break;
                    default:
                        theme.SetLightTheme();
                        break;
                }
                break;
        }
    }

    public static void SetCustomizedDarkTheme(this Theme theme)
    {
        theme.SetDarkTheme();
        theme.Chips.Background = theme.Chips.OutlineBorder;
    }
}
