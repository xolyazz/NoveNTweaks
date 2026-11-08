using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace NoveNTweaks;

// Splash window - uses the exact same XAML as the original PowerShell app,
// including the Braille-dot spinner animation.
public partial class SplashWindow : Window
{
    // Braille-dot spinner characters (same sequence as original .ps1).
    public static readonly char[] SpinChars = {
        (char)0x280B, (char)0x2819, (char)0x2839, (char)0x2838,
        (char)0x283C, (char)0x2834, (char)0x2826, (char)0x2827,
        (char)0x2807, (char)0x280F,
    };
    int _spinIdx = 0;
    DispatcherTimer _spinTimer;

    public SplashWindow()
    {
        InitializeComponent();

        bool anims = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AnimationsEnabled") ?? 1) == 1;
        if (!anims)
        {
            Opacity = 1.0;
            if (RootScale != null)
            {
                RootScale.ScaleX = 1.0;
                RootScale.ScaleY = 1.0;
            }
            if (SplashContentTranslate != null)
            {
                SplashContentTranslate.Y = 0;
            }
            if (SplashLogoScale != null)
            {
                SplashLogoScale.ScaleX = 1.0;
                SplashLogoScale.ScaleY = 1.0;
            }
            if (SplashLogoRotate != null)
            {
                SplashLogoRotate.Angle = 0;
            }
            if (AppLabel != null) AppLabel.Opacity = 1.0;
            if (SubLabel != null) SubLabel.Opacity = 1.0;
        }
        else
        {
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            // Fade-in.
            var fa = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(550)) { EasingFunction = ease };
            BeginAnimation(OpacityProperty, fa);

            // Root scale 0.7 -> 1.0 with spring (BackEase).
            if (RootScale != null)
            {
                var be = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
                var sx = new DoubleAnimation(0.7, 1.0, TimeSpan.FromMilliseconds(650)) { EasingFunction = be };
                RootScale.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
                RootScale.BeginAnimation(ScaleTransform.ScaleYProperty, sx);
            }

            // Staggered slide up for the inner content
            if (SplashContentTranslate != null)
            {
                var cTranslate = new DoubleAnimation(25.0, 0.0, TimeSpan.FromMilliseconds(750)) { EasingFunction = ease };
                SplashContentTranslate.BeginAnimation(TranslateTransform.YProperty, cTranslate);
            }

            // Logo roll-in and scale spring
            if (SplashLogoScale != null && SplashLogoRotate != null)
            {
                var lScaleBe = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 };
                var lScale = new DoubleAnimation(0.4, 1.0, TimeSpan.FromMilliseconds(800)) { EasingFunction = lScaleBe };
                SplashLogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, lScale);
                SplashLogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, lScale);

                var lRotate = new DoubleAnimation(-35.0, 0.0, TimeSpan.FromMilliseconds(900)) { EasingFunction = ease };
                SplashLogoRotate.BeginAnimation(RotateTransform.AngleProperty, lRotate);
            }

            // AppLabel + SubLabel fade-in.
            if (AppLabel != null)
            {
                var lf = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(600))
                { BeginTime = TimeSpan.FromMilliseconds(200), EasingFunction = ease };
                AppLabel.BeginAnimation(OpacityProperty, lf);
            }
            if (SubLabel != null)
            {
                var sf = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(500))
                { BeginTime = TimeSpan.FromMilliseconds(400), EasingFunction = ease };
                SubLabel.BeginAnimation(OpacityProperty, sf);
            }
        }

        // Braille-dot spinner.
        _spinTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
        _spinTimer.Tick += (_, _) =>
        {
            if (SpinnerLabel != null)
            {
                SpinnerLabel.Text = SpinChars[_spinIdx].ToString();
                _spinIdx = (_spinIdx + 1) % SpinChars.Length;
            }
        };
        _spinTimer.Start();
    }

    public void CloseGracefully()
    {
        _spinTimer?.Stop();

        bool anims = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AnimationsEnabled") ?? 1) == 1;
        if (!anims)
        {
            Close();
            return;
        }

        var fe = new CubicEase { EasingMode = EasingMode.EaseInOut };
        var fo = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(380)) { EasingFunction = fe };
        fo.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fo);
    }

    // Localise the subtitle ("Loading...") after the splash is shown.
    public void SetLoadingText(string text)
    {
        if (SubLabel != null) SubLabel.Text = text ?? "Loading...";
    }
}
