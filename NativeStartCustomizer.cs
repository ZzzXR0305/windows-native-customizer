using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Automation;
using Microsoft.Win32;

[assembly: System.Reflection.AssemblyTitle("原生开始菜单定制器")]
[assembly: System.Reflection.AssemblyDescription("分别定制 Windows 11 原生开始菜单、快捷设置与透明任务栏自动对比；搜索面板保持系统原生")]
[assembly: System.Reflection.AssemblyCompany("Native Start Customizer")]
[assembly: System.Reflection.AssemblyProduct("原生开始菜单定制器")]
[assembly: System.Reflection.AssemblyVersion("3.6.2.0")]
[assembly: System.Reflection.AssemblyFileVersion("3.6.2.0")]

namespace NativeStartCustomizer
{
    internal static class Paths
    {
        internal const string ModId = "windows-11-start-menu-styler";
        internal const string ModDll = "windows-11-start-menu-styler_1.6_64.dll";
        internal const string ModRegistry = @"SOFTWARE\Windhawk\Engine\Mods\windows-11-start-menu-styler";
        internal const string NotificationModId = "windows-11-notification-center-styler";
        internal const string NotificationModDll = "windows-11-notification-center-styler_1.6_64.dll";
        internal const string NotificationModRegistry = @"SOFTWARE\Windhawk\Engine\Mods\windows-11-notification-center-styler";
        internal const string TaskbarModId = "native-taskbar-adaptive-contrast";
        internal const string TaskbarModDll = "native-taskbar-adaptive-contrast_1.2_64.dll";
        internal const string PreviousTaskbarModDll = "native-taskbar-adaptive-contrast_1.0_64.dll";
        internal const string TaskbarModRegistry = @"SOFTWARE\Windhawk\Engine\Mods\native-taskbar-adaptive-contrast";
        internal const string LegacyTaskbarModRegistry = @"SOFTWARE\Windhawk\Engine\Mods\windows-11-taskbar-styler";
        internal const string EngineSettingsRegistry = @"SOFTWARE\Windhawk\Engine\Settings";
        internal static readonly string AppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NativeStartCustomizer");
        internal static readonly string UserSettings = Path.Combine(AppData, "settings.ini");
        internal static readonly string SearchSettings = Path.Combine(AppData, "search.ini");
        internal static readonly string QuickSettings = Path.Combine(AppData, "quick-settings.ini");
        internal static readonly string TaskbarSettings = Path.Combine(AppData, "taskbar-contrast.ini");
        internal static readonly string Backup = Path.Combine(AppData, "windhawk-before-first-apply.reg");
        internal static readonly string BackupAbsent = Path.Combine(AppData, "windhawk-before-first-apply.absent");
        internal static readonly string PackageLocalState = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Packages\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy\LocalState\StartCanvasNative");
        internal static readonly string ProcessedImage = Path.Combine(PackageLocalState, "background.png");
        internal static readonly string CbsPackageLocalState = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Packages\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\LocalState\NativeShellCustomizer");
        internal static readonly string SearchProcessedImage = Path.Combine(CbsPackageLocalState, "search-background.png");
        internal static readonly string QuickProcessedImage = Path.Combine(CbsPackageLocalState, "quick-background.png");
        internal static readonly string WindhawkModDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Windhawk\Engine\Mods\64");
        internal static readonly string WindhawkModule = Path.Combine(WindhawkModDir, ModDll);
        internal static readonly string NotificationWindhawkModule = Path.Combine(WindhawkModDir, NotificationModDll);
        internal static readonly string BundledNotificationModule = Path.Combine(Application.StartupPath, @"运行组件\Mod", NotificationModDll);
        internal static readonly string TaskbarWindhawkModule = Path.Combine(WindhawkModDir, TaskbarModDll);
        internal static readonly string CompatibilityTaskbarWindhawkModule = Path.Combine(WindhawkModDir, PreviousTaskbarModDll);
        internal static readonly string BundledTaskbarModule = Path.Combine(Application.StartupPath, @"运行组件\Mod", TaskbarModDll);
        internal static readonly string TaskbarGuard = Path.Combine(AppData, "taskbar-windhawk-guard.ini");
        internal static readonly string TaskbarStatus = Path.Combine(AppData, "taskbar-adaptive-status.ini");
        internal static readonly string TaskbarRegistryRollback = Path.Combine(AppData, "taskbar-registry-before-v1.1.reg");
        internal static readonly string TaskbarWatcher = Path.Combine(Application.StartupPath, @"运行组件\TaskbarContrastWatcher.exe");
        internal static readonly string DiagnosticLog = Path.Combine(AppData, "last-apply-diagnostic.txt");
        internal static readonly string OperationStatus = Path.Combine(AppData, "last-operation.txt");
        internal static readonly string SafetyLock = Path.Combine(AppData, "windhawk-safety-lock.txt");
        internal static readonly string ServiceBackup = Path.Combine(AppData, "windhawk-service-before-standalone.txt");
    }

    internal sealed class TaskbarContrastSettings
    {
        internal bool Enabled;

        internal static TaskbarContrastSettings Load()
        {
            var settings = new TaskbarContrastSettings();
            try
            {
                if (!File.Exists(Paths.TaskbarSettings)) return settings;
                foreach (string line in File.ReadAllLines(Paths.TaskbarSettings, Encoding.UTF8))
                    if (String.Equals(line.Trim(), "Enabled=1", StringComparison.OrdinalIgnoreCase)) settings.Enabled = true;
            }
            catch { }
            return settings;
        }

        internal void Save()
        {
            Directory.CreateDirectory(Paths.AppData);
            File.WriteAllText(Paths.TaskbarSettings, "Enabled=" + (Enabled ? "1" : "0") + Environment.NewLine, new UTF8Encoding(true));
        }
    }

    internal sealed class DesignSettings
    {
        internal string Mode = "image";
        internal string ImagePath = "";
        internal int Blur = 16;
        internal int TintOpacity = 28;
        internal int CornerRadius = 14;
        internal Color Tint = Color.FromArgb(246, 248, 252);
        internal string Fit = "UniformToFill";
        internal string Layout = "default";
        internal string TextContrast = "auto";
        internal string Tone = "custom";
        internal string SizePreset = "auto";
        internal int MenuWidth = 900;
        internal int MenuHeight = 720;
        internal bool HideRecommended;
        internal bool HideSearch;
        internal bool AlignToStartButton = true;
        internal bool Enabled = true;

        internal static DesignSettings Load(string file)
        {
            var s = new DesignSettings();
            if (!File.Exists(file)) return s;
            try
            {
                foreach (var raw in File.ReadAllLines(file, Encoding.UTF8))
                {
                    int p = raw.IndexOf('=');
                    if (p <= 0) continue;
                    string key = raw.Substring(0, p), value = raw.Substring(p + 1);
                    int n;
                    if (key == "Mode") s.Mode = value;
                    else if (key == "ImagePath") s.ImagePath = Decode(value);
                    else if (key == "Blur" && int.TryParse(value, out n)) s.Blur = Math.Max(0, Math.Min(50, n));
                    else if (key == "TintOpacity" && int.TryParse(value, out n)) s.TintOpacity = Math.Max(0, Math.Min(90, n));
                    else if (key == "CornerRadius" && int.TryParse(value, out n)) s.CornerRadius = Math.Max(0, Math.Min(32, n));
                    else if (key == "Tint" && int.TryParse(value, out n)) s.Tint = Color.FromArgb(255, Color.FromArgb(n));
                    else if (key == "Fit") s.Fit = value;
                    else if (key == "Layout") s.Layout = value;
                    else if (key == "TextContrast") s.TextContrast = value;
                    else if (key == "Tone") s.Tone = value;
                    else if (key == "SizePreset") s.SizePreset = value;
                    else if (key == "MenuWidth" && int.TryParse(value, out n)) s.MenuWidth = Math.Max(680, Math.Min(1400, n));
                    else if (key == "MenuHeight" && int.TryParse(value, out n)) s.MenuHeight = Math.Max(620, Math.Min(1000, n));
                    else if (key == "HideRecommended") s.HideRecommended = value == "1";
                    else if (key == "HideSearch") s.HideSearch = value == "1";
                    else if (key == "AlignToStartButton") s.AlignToStartButton = value == "1";
                    else if (key == "Enabled") s.Enabled = value == "1";
                }
            }
            catch { }
            return s;
        }

        internal void Save(string file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllLines(file, new[] {
                "Mode=" + Mode,
                "ImagePath=" + Encode(ImagePath ?? ""),
                "Blur=" + Blur,
                "TintOpacity=" + TintOpacity,
                "CornerRadius=" + CornerRadius,
                "Tint=" + Tint.ToArgb(),
                "Fit=" + Fit,
                "Layout=" + Layout,
                "TextContrast=" + TextContrast,
                "Tone=" + Tone,
                "SizePreset=" + SizePreset,
                "MenuWidth=" + MenuWidth,
                "MenuHeight=" + MenuHeight,
                "HideRecommended=" + (HideRecommended ? "1" : "0"),
                "HideSearch=" + (HideSearch ? "1" : "0"),
                "AlignToStartButton=" + (AlignToStartButton ? "1" : "0"),
                "Enabled=" + (Enabled ? "1" : "0")
            }, new UTF8Encoding(true));
        }

        private static string Encode(string value) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(value)); }
        private static string Decode(string value)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
            catch { return ""; }
        }
    }

    internal sealed class PanelSettings
    {
        internal string Kind;
        internal bool Enabled;
        internal string Mode = "blur";
        internal string ImagePath = "";
        internal int Blur = 18;
        internal int TintOpacity = 34;
        internal int CornerRadius = 14;
        internal Color Tint = Color.FromArgb(238, 242, 248);
        internal string Fit = "UniformToFill";
        internal string TextContrast = "auto";
        internal int Width;
        internal int Height;
        internal bool SeparateMedia;
        internal Color MediaTint = Color.FromArgb(238, 242, 248);
        internal int MediaOpacity = 34;
        internal int MediaRadius = 14;
        internal bool HideSearchAds = true;

        internal static PanelSettings Load(string file, string kind)
        {
            var s = new PanelSettings { Kind = kind };
            if (kind == "search") { s.Width = 1040; s.Height = 760; }
            else { s.Width = 0; s.Height = 0; }
            if (!File.Exists(file)) return s;
            try
            {
                foreach (string raw in File.ReadAllLines(file, Encoding.UTF8))
                {
                    int p = raw.IndexOf('=');
                    if (p <= 0) continue;
                    string key = raw.Substring(0, p), value = raw.Substring(p + 1);
                    int n;
                    if (key == "Enabled") s.Enabled = value == "1";
                    else if (key == "Mode") s.Mode = value;
                    else if (key == "ImagePath") s.ImagePath = Decode(value);
                    else if (key == "Blur" && int.TryParse(value, out n)) s.Blur = Clamp(n, 0, 50);
                    else if (key == "TintOpacity" && int.TryParse(value, out n)) s.TintOpacity = Clamp(n, 0, 90);
                    else if (key == "CornerRadius" && int.TryParse(value, out n)) s.CornerRadius = Clamp(n, 0, 32);
                    else if (key == "Tint" && int.TryParse(value, out n)) s.Tint = Color.FromArgb(255, Color.FromArgb(n));
                    else if (key == "Fit") s.Fit = value;
                    else if (key == "TextContrast") s.TextContrast = value;
                    else if (key == "Width" && int.TryParse(value, out n)) s.Width = kind == "search" ? Clamp(n, 760, 1500) : 0;
                    else if (key == "Height" && int.TryParse(value, out n)) s.Height = Clamp(n, 0, 1100);
                    else if (key == "SeparateMedia") s.SeparateMedia = value == "1";
                    else if (key == "MediaTint" && int.TryParse(value, out n)) s.MediaTint = Color.FromArgb(255, Color.FromArgb(n));
                    else if (key == "MediaOpacity" && int.TryParse(value, out n)) s.MediaOpacity = Clamp(n, 0, 90);
                    else if (key == "MediaRadius" && int.TryParse(value, out n)) s.MediaRadius = Clamp(n, 0, 32);
                    else if (key == "HideSearchAds") s.HideSearchAds = value == "1";
                }
            }
            catch { }
            return s;
        }

        internal void Save(string file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllLines(file, new[] {
                "Enabled=" + (Enabled ? "1" : "0"), "Mode=" + Mode,
                "ImagePath=" + Encode(ImagePath ?? ""), "Blur=" + Blur,
                "TintOpacity=" + TintOpacity, "CornerRadius=" + CornerRadius,
                "Tint=" + Tint.ToArgb(), "Fit=" + Fit, "TextContrast=" + TextContrast,
                "Width=" + Width, "Height=" + Height,
                "SeparateMedia=" + (SeparateMedia ? "1" : "0"),
                "MediaTint=" + MediaTint.ToArgb(), "MediaOpacity=" + MediaOpacity,
                "MediaRadius=" + MediaRadius,
                "HideSearchAds=" + (HideSearchAds ? "1" : "0")
            }, new UTF8Encoding(true));
        }

        internal bool UseDarkTextTheme()
        {
            if (TextContrast == "white") return true;
            if (TextContrast == "black") return false;
            Color c = Tint;
            double luminance = (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
            return luminance < 0.52;
        }

        private static int Clamp(int n, int min, int max) { return Math.Max(min, Math.Min(max, n)); }
        private static string Encode(string value) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(value)); }
        private static string Decode(string value) { try { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); } catch { return ""; } }
    }

    internal static class ColorTools
    {
        private static string cachedPath;
        private static DateTime cachedWriteTime;
        private static Color[] cachedSamples = new[] { Color.FromArgb(128, 128, 128) };

        internal static Color EffectiveTint(DesignSettings settings)
        {
            if (settings.Tone == "dark") return Color.FromArgb(22, 25, 31);
            if (settings.Tone == "light") return Color.FromArgb(246, 248, 252);
            return settings.Tint;
        }

        internal static bool UseDarkTheme(DesignSettings settings)
        {
            if (settings.TextContrast == "white") return true;
            if (settings.TextContrast == "black") return false;

            Color tint = EffectiveTint(settings);
            Color[] backgrounds = new[] { tint };
            if ((settings.Mode == "image" || settings.Mode == "blur") && File.Exists(settings.ImagePath))
                backgrounds = SampleImage(settings.ImagePath);

            double opacity = settings.Mode == "color" ? 1.0 : Math.Max(0, Math.Min(1, settings.TintOpacity / 100.0));
            var effectiveBackgrounds = new Color[backgrounds.Length];
            for (int i = 0; i < backgrounds.Length; i++)
                effectiveBackgrounds[i] = Blend(backgrounds[i], tint, opacity);

            // Compare actual WCAG contrast ratios instead of using a fixed
            // brightness threshold. The lower quartile protects readability
            // on mixed images, while the median avoids a few extreme pixels
            // flipping the whole menu theme.
            double whiteScore = ContrastScore(effectiveBackgrounds, Color.White);
            double darkScore = ContrastScore(effectiveBackgrounds, Color.FromArgb(24, 27, 33));
            return whiteScore > darkScore;
        }

        internal static Color ReadableText(DesignSettings settings)
        {
            return UseDarkTheme(settings) ? Color.White : Color.FromArgb(24, 27, 33);
        }

        private static Color[] SampleImage(string path)
        {
            try
            {
                DateTime time = File.GetLastWriteTimeUtc(path);
                if (String.Equals(cachedPath, path, StringComparison.OrdinalIgnoreCase) && time == cachedWriteTime) return cachedSamples;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var image = Image.FromStream(stream))
                using (var small = new Bitmap(36, 36))
                {
                    using (var g = Graphics.FromImage(small))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        // Text is concentrated away from the extreme image
                        // edges, so sample the central 80% more heavily.
                        int sourceX = image.Width / 10;
                        int sourceY = image.Height / 10;
                        var source = new Rectangle(sourceX, sourceY,
                            Math.Max(1, image.Width - sourceX * 2),
                            Math.Max(1, image.Height - sourceY * 2));
                        g.DrawImage(image, new Rectangle(0, 0, small.Width, small.Height), source, GraphicsUnit.Pixel);
                    }
                    var samples = new Color[small.Width * small.Height];
                    int index = 0;
                    for (int y = 0; y < small.Height; y++) for (int x = 0; x < small.Width; x++)
                        samples[index++] = small.GetPixel(x, y);
                    cachedSamples = samples;
                    cachedPath = path; cachedWriteTime = time;
                    return cachedSamples;
                }
            }
            catch { return new[] { Color.FromArgb(128, 128, 128) }; }
        }

        private static Color Blend(Color source, Color tint, double opacity)
        {
            return Color.FromArgb(
                (int)Math.Round(source.R * (1 - opacity) + tint.R * opacity),
                (int)Math.Round(source.G * (1 - opacity) + tint.G * opacity),
                (int)Math.Round(source.B * (1 - opacity) + tint.B * opacity));
        }

        private static double ContrastScore(Color[] backgrounds, Color text)
        {
            double textLuminance = RelativeLuminance(text);
            var ratios = new double[backgrounds.Length];
            for (int i = 0; i < backgrounds.Length; i++)
            {
                double backgroundLuminance = RelativeLuminance(backgrounds[i]);
                double lighter = Math.Max(backgroundLuminance, textLuminance);
                double darker = Math.Min(backgroundLuminance, textLuminance);
                ratios[i] = (lighter + 0.05) / (darker + 0.05);
            }
            Array.Sort(ratios);
            int lowerQuartile = Math.Min(ratios.Length - 1, ratios.Length / 4);
            int median = Math.Min(ratios.Length - 1, ratios.Length / 2);
            return ratios[lowerQuartile] * 0.7 + ratios[median] * 0.3;
        }

        private static double RelativeLuminance(Color color)
        {
            return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
        }

        private static double Linear(int value)
        {
            double c = value / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length > 0 && args[0] == "--apply")
            {
                Worker.Apply(args.Length > 1 ? args[1] : Paths.UserSettings);
                return;
            }
            if (args.Length > 0 && args[0] == "--restore")
            {
                Worker.Restore();
                return;
            }
            if (args.Length > 0 && args[0] == "--apply-quick")
            {
                Worker.ApplyQuickSettings();
                return;
            }
            if (args.Length > 0 && args[0] == "--restore-quick")
            {
                Worker.RestoreQuickSettings();
                return;
            }
            if (args.Length > 0 && args[0] == "--apply-taskbar-contrast")
            {
                Worker.ApplyTaskbarContrast();
                return;
            }
            if (args.Length > 0 && args[0] == "--restore-taskbar-contrast")
            {
                Worker.RestoreTaskbarContrast();
                return;
            }
            if (args.Length > 0 && args[0] == "--activate-taskbar-compat")
            {
                Worker.ActivateTaskbarCompatibilityNoElevation();
                return;
            }
            if (args.Length > 0 && args[0] == "--diagnose")
            {
                Console.WriteLine(Worker.DiagnosticText());
                return;
            }
            if (args.Length > 1 && args[0] == "--alignment-diagnostic")
            {
                File.WriteAllText(args[1], Worker.AlignmentDiagnostic(), new UTF8Encoding(true));
                return;
            }
            if (args.Length > 1 && (args[0] == "--render" || args[0] == "--render-scrolled"))
            {
                using (var form = new MainForm())
                {
                    form.Show();
                    Application.DoEvents();
                    if (args[0] == "--render-scrolled")
                    {
                        var scrollPanel = form.Controls.OfType<Panel>().FirstOrDefault(p => p.AutoScroll);
                        if (scrollPanel != null) scrollPanel.AutoScrollPosition = new Point(0, 230);
                        Application.DoEvents();
                    }
                    using (var bitmap = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                        bitmap.Save(args[1], ImageFormat.Png);
                    }
                    form.Close();
                }
                return;
            }
            if (args.Length > 1 && args[0] == "--render-advanced")
            {
                using (var form = new AdvancedForm(DesignSettings.Load(Paths.UserSettings)))
                {
                    form.Show();
                    Application.DoEvents();
                    using (var bitmap = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                        bitmap.Save(args[1], ImageFormat.Png);
                    }
                    form.Close();
                }
                return;
            }
            if (args.Length > 1 && args[0] == "--render-quick")
            {
                using (var form = new PanelCustomizerForm("quick"))
                {
                    form.Show();
                    Application.DoEvents();
                    using (var bitmap = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                        bitmap.Save(args[1], ImageFormat.Png);
                    }
                    form.Close();
                }
                return;
            }
            if (args.Length > 1 && args[0] == "--render-taskbar")
            {
                using (var form = new TaskbarContrastForm())
                {
                    form.Show();
                    Application.DoEvents();
                    using (var bitmap = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                        bitmap.Save(args[1], ImageFormat.Png);
                    }
                    form.Close();
                }
                return;
            }
            if (args.Length > 2 && args[0] == "--self-test-image")
            {
                ImageProcessor.CreateStartBackground(args[1], args[2], 22, "UniformToFill");
                return;
            }
            // The legacy standalone watcher/overlay is permanently retired.
            // Version 3.6 keeps the user's Enabled choice; the new adaptive
            // foreground runs inside the isolated Explorer taskbar module and
            // never paints a backdrop or launches a resident helper process.
            try
            {
                TaskbarContrastRuntime.ConfigureStartup(false);
                TaskbarContrastRuntime.StopWatcher();
            }
            catch { }
            Application.Run(args.Length > 0 && args[0] == "--taskbar" ? (Form)new TaskbarContrastForm() : new MainForm());
        }
    }

    internal static class TaskbarContrastRuntime
    {
        internal const string StopEventName = @"Local\NativeStartCustomizer.TaskbarContrast.Stop.v1";
        private const string RunValueName = "NativeStartCustomizerTaskbarContrast";

        internal static void ConfigureStartup(bool enabled)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (enabled)
                    key.SetValue(RunValueName, "\"" + Paths.TaskbarWatcher + "\"", RegistryValueKind.String);
                else
                    key.DeleteValue(RunValueName, false);
            }
        }

        internal static void StartWatcher()
        {
            if (!File.Exists(Paths.TaskbarWatcher)) return;
            try
            {
                Process.Start(new ProcessStartInfo(Paths.TaskbarWatcher)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Path.GetDirectoryName(Paths.TaskbarWatcher)
                });
            }
            catch { }
        }

        internal static void StopWatcher()
        {
            try
            {
                using (EventWaitHandle stop = EventWaitHandle.OpenExisting(StopEventName)) stop.Set();
            }
            catch { }
        }

        internal static bool IsWatcherRunning()
        {
            try { return Process.GetProcessesByName("TaskbarContrastWatcher").Length > 0; }
            catch { return false; }
        }
    }

    internal static class Worker
    {
        internal static bool IsAdministrator()
        {
            try
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        internal static string DiagnosticText()
        {
            try
            {
                if (File.Exists(Paths.OperationStatus))
                {
                    string lastOperation = File.ReadAllText(Paths.OperationStatus, Encoding.UTF8).Trim();
                    if (!String.IsNullOrEmpty(lastOperation)) return lastOperation;
                }
            }
            catch { }
            string startVersion = "未找到";
            string startExe = @"C:\Windows\SystemApps\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy\StartMenuExperienceHost.exe";
            try { startVersion = FileVersionInfo.GetVersionInfo(startExe).FileVersion; } catch { }
            bool registered = false, enabled = false;
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(Paths.ModRegistry))
                {
                    registered = key != null && !String.IsNullOrEmpty(key.GetValue("LibraryFileName") as string);
                    enabled = registered && Convert.ToInt32(key.GetValue("Disabled", 0)) == 0;
                }
            }
            catch { }
            bool serviceRunning = IsWindhawkServiceRunning();
            bool engineLoaded = IsModuleLoaded("windhawk.dll");
            bool modLoaded = IsModuleLoaded(Paths.ModDll);
            string state = modLoaded ? "模组已加载" : (!enabled ? (registered ? "模组已停用" : "尚未应用") : (engineLoaded ? "模组未加载" : "Windhawk 未注入"));
            return String.Format("开始菜单 {0} · 运行组件{1} · {2}", startVersion,
                serviceRunning ? "服务运行中" : "服务未运行", state);
        }

        internal static string TaskbarContrastStatus()
        {
            TaskbarContrastSettings settings = TaskbarContrastSettings.Load();
            if (!settings.Enabled)
                return "当前：任务栏保持完全透明；自动前景色未启用。";
            if (File.Exists(Paths.TaskbarGuard))
            {
                try
                {
                    if (DateTime.UtcNow - File.GetLastWriteTimeUtc(Paths.TaskbarGuard) < TimeSpan.FromMinutes(2))
                    {
                        int guardedPid = 0;
                        foreach (string line in File.ReadAllLines(Paths.TaskbarGuard, Encoding.UTF8))
                            if (line.StartsWith("Pid=", StringComparison.OrdinalIgnoreCase))
                                Int32.TryParse(line.Substring(4).Trim(), out guardedPid);
                        bool currentExplorer = Process.GetProcessesByName("explorer").Any(p =>
                        {
                            try { return p.Id == guardedPid; }
                            finally { p.Dispose(); }
                        });
                        if (!currentExplorer)
                            return "安全保护已触发：本次 Explorer 会话保持原生前景色，避免循环崩溃。";
                        return "当前：安全组件正在完成 30 秒稳定性观察；任务栏背景仍完全透明。";
                    }
                }
                catch { }
            }
            int shellPid = GetTaskbarExplorerPid();
            string validation;
            bool loaded = shellPid > 0 && IsModuleLoadedInProcess(shellPid, Paths.TaskbarModDll);
            bool healthy = loaded && ValidateTaskbarStatus(shellPid, out validation);
            string compatibilityDetails;
            bool compatibilityHealthy = shellPid > 0 && File.Exists(Paths.BundledTaskbarModule) &&
                IsHealthyCompatibilityTaskbarModule(shellPid, out compatibilityDetails);
            if (healthy || compatibilityHealthy)
                return "当前：完全透明；直接采样托盘背后的物理 96×12 像素，右侧原生文字与单色图标正在自动切换黑/白。";
            if (loaded)
                return "当前：修正版组件已加载，正在等待托盘叶级前景校验完成。";
            return "当前：自动对比已启用，等待 Explorer 加载修正版隔离组件。";
        }

        internal static bool TryGetApplyBlockReason(out string reason)
        {
            reason = "";
            if (!IsEngineIsolationConfigured())
            {
                reason = "安全隔离未就绪：运行组件白名单包含了未授权进程，当前已拒绝应用。";
                return true;
            }
            try
            {
                int startMode = -1;
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Windhawk"))
                    if (key != null) startMode = Convert.ToInt32(key.GetValue("Start", -1));
                if (startMode != 4) return false;

                string runtimeExe = Path.Combine(Application.StartupPath, @"运行组件\Windhawk\windhawk.exe");
                bool quarantined = !File.Exists(runtimeExe) && File.Exists(runtimeExe + ".disabled");
                reason = quarantined
                    ? "运行组件正被另一个系统修复任务隔离，当前不能应用；本程序不会自动恢复或弹窗。"
                    : "运行组件服务已被禁用，当前不能应用；请先完成系统修复。";
                return true;
            }
            catch { return false; }
        }

        internal static void Apply(string settingsFile)
        {
            try
            {
                if (!IsEngineIsolationConfigured())
                    throw new InvalidOperationException("运行组件未通过单进程隔离检查，已拒绝应用。");
                DesignSettings s = DesignSettings.Load(settingsFile);
                s.Enabled = true;
                s.Save(settingsFile);
                if (s.Mode == "image")
                {
                    if (String.IsNullOrWhiteSpace(s.ImagePath) || !File.Exists(s.ImagePath))
                        throw new FileNotFoundException("请选择一张有效的背景图片。", s.ImagePath);
                    ImageProcessor.CreateStartBackground(s.ImagePath, Paths.ProcessedImage, s.Blur, s.Fit);
                }

                if (!File.Exists(Paths.WindhawkModule))
                    throw new FileNotFoundException("没有找到已经安装的开始菜单样式组件。请保留完整的程序文件夹并重新运行首次配置。", Paths.WindhawkModule);
                if (!IsWindhawkServiceRunning())
                    throw new InvalidOperationException("运行组件没有启动；程序不会为你自动启动注入服务。");
                CreateFirstBackup();
                WriteWindhawkSettings(s);
                if (WriteEngineWhitelistForEnabledModules()) RestartWindhawkService();
                bool modLoaded = WaitForModLoaded(Paths.ModDll, new[] { "StartMenuExperienceHost" }, 8);
                if (!modLoaded)
                {
                    string details = BuildDiagnosticReport();
                    Directory.CreateDirectory(Paths.AppData);
                    File.WriteAllText(Paths.DiagnosticLog, details, new UTF8Encoding(true));
                    throw new InvalidOperationException("设置已经写入，但 Windhawk 没有把模组载入开始菜单。\n\n诊断报告：" + Paths.DiagnosticLog + "\n\n" + details);
                }
                try { if (File.Exists(Paths.DiagnosticLog)) File.Delete(Paths.DiagnosticLog); } catch { }
                try { if (File.Exists(Paths.OperationStatus)) File.Delete(Paths.OperationStatus); } catch { }
                // Successful applies are intentionally silent. The elevated
                // helper exits immediately so it doesn't interrupt the user.
            }
            catch (Exception ex)
            {
                try
                {
                    Directory.CreateDirectory(Paths.AppData);
                    File.WriteAllText(Paths.OperationStatus, "应用失败：" + ex.Message.Replace("\r", " ").Replace("\n", " "), new UTF8Encoding(true));
                }
                catch { }
            }
        }

        internal static void Restore()
        {
            try
            {
                if (!IsAdministrator()) throw new InvalidOperationException("恢复原生菜单需要管理员权限。请从主界面点击“恢复 Windows 原生样式”。");
                DesignSettings start = DesignSettings.Load(Paths.UserSettings);
                start.Enabled = false;
                start.Save(Paths.UserSettings);
                WriteWindhawkSettings(start);
                if (WriteEngineWhitelistForEnabledModules()) RestartWindhawkService();
                try { if (File.Exists(Paths.OperationStatus)) File.Delete(Paths.OperationStatus); } catch { }
            }
            catch (Exception ex)
            {
                try
                {
                    Directory.CreateDirectory(Paths.AppData);
                    File.WriteAllText(Paths.OperationStatus, "恢复失败：" + ex.Message.Replace("\r", " ").Replace("\n", " "), new UTF8Encoding(true));
                }
                catch { }
            }
        }

        internal static void ApplyQuickSettings()
        {
            RunSilently(delegate
            {
                RequireSafeEngine();
                PanelSettings quick = PanelSettings.Load(Paths.QuickSettings, "quick");
                ValidateAndPreparePanelImage(quick, Paths.QuickProcessedImage, "快捷设置");
                quick.Enabled = true;
                quick.Save(Paths.QuickSettings);
                EnsureNotificationModuleAvailable();
                WriteNotificationSettings(quick);
                if (WriteEngineWhitelistForEnabledModules()) RestartWindhawkService();
                RestartShellHostForStyleRefresh();
                if (!WaitForModLoaded(Paths.NotificationModDll, new[] { "ShellHost" }, 8))
                    throw new InvalidOperationException("设置已写入，但样式组件尚未载入 ShellHost.exe。重新打开快捷设置后会自动重试。");
            }, "快捷设置");
        }

        internal static void RestoreQuickSettings()
        {
            RunSilently(delegate
            {
                RequireSafeEngine();
                PanelSettings quick = PanelSettings.Load(Paths.QuickSettings, "quick");
                quick.Enabled = false;
                quick.Save(Paths.QuickSettings);
                using (var key = Registry.LocalMachine.CreateSubKey(Paths.NotificationModRegistry, true))
                {
                    key.SetValue("Disabled", 1, RegistryValueKind.DWord);
                    key.SetValue("SettingsChangeTime", UnixTime(), RegistryValueKind.DWord);
                }
                if (WriteEngineWhitelistForEnabledModules()) RestartWindhawkService();
            }, "快捷设置恢复");
        }

        internal static void ApplyTaskbarContrast()
        {
            RunSilently(delegate
            {
                RequireSafeEngine();
                TaskbarContrastRuntime.StopWatcher();
                TaskbarContrastRuntime.ConfigureStartup(false);
                EnsureTaskbarModuleAvailable();

                TaskbarContrastSettings settings = TaskbarContrastSettings.Load();
                settings.Enabled = true;
                settings.Save();
                if (IsHealthyTaskbarModuleAlreadyRunning()) return;

                ExportTaskbarRegistrySnapshot();
                int oldShellPid = GetTaskbarExplorerPid();
                if (oldShellPid <= 0)
                    throw new InvalidOperationException("没有找到拥有任务栏的 Explorer.exe，未执行升级。");

                try
                {
                    try { if (File.Exists(Paths.TaskbarGuard)) File.Delete(Paths.TaskbarGuard); } catch { }
                    try { if (File.Exists(Paths.TaskbarStatus)) File.Delete(Paths.TaskbarStatus); } catch { }

                    // Stage the complete versioned module and style registry.
                    // No SettingsChangeTime is written: the old Explorer is
                    // never asked to unload or hot-reload XAML customizations.
                    DisableLegacyTaskbarStyler();
                    WriteTaskbarWindhawkSettings(true, false);
                    WriteEngineWhitelistForEnabledModules();

                    int newShellPid = RestartTaskbarExplorerOnce(oldShellPid);
                    string validation;
                    if (!WaitForHealthyTaskbarUpgrade(newShellPid, 25, out validation))
                        throw new InvalidOperationException("新组件校验失败：" + validation);

                    CleanupPreviousTaskbarModule();
                }
                catch
                {
                    // Roll back the registration for the next Explorer session,
                    // but never hot-unload the module and never restart Explorer
                    // a second time. If the shell is absent, start it once.
                    try { RestoreTaskbarRegistrySnapshot(); } catch { }
                    EnsureTaskbarExplorerRunning();
                    throw;
                }
            }, "任务栏自动对比");
        }

        internal static void ActivateTaskbarCompatibilityNoElevation()
        {
            RunSilently(delegate
            {
                if (!IsEngineIsolationConfigured())
                    throw new InvalidOperationException("Windhawk 进程白名单未通过隔离检查。");
                if (!IsWindhawkServiceRunning())
                    throw new InvalidOperationException("Windhawk 服务没有运行。");

                using (var key = Registry.LocalMachine.OpenSubKey(Paths.TaskbarModRegistry))
                {
                    if (key == null || Convert.ToInt32(key.GetValue("Disabled", 1)) != 0 ||
                        !String.Equals(Convert.ToString(key.GetValue("LibraryFileName", "")),
                            Paths.PreviousTaskbarModDll, StringComparison.OrdinalIgnoreCase) ||
                        Convert.ToString(key.GetValue("Include", "")).IndexOf("explorer.exe", StringComparison.OrdinalIgnoreCase) < 0)
                        throw new InvalidOperationException("现有任务栏模块登记不适合无弹窗兼容迁移，系统配置未改动。");
                }

                TaskbarContrastRuntime.StopWatcher();
                TaskbarContrastRuntime.ConfigureStartup(false);
                TaskbarContrastSettings settings = TaskbarContrastSettings.Load();
                settings.Enabled = true;
                settings.Save();

                if (!File.Exists(Paths.BundledTaskbarModule))
                    throw new FileNotFoundException("没有找到 v1.2 修正版任务栏组件。", Paths.BundledTaskbarModule);

                bool stagedNewBytes = StageCompatibilityTaskbarModule();
                int oldShellPid = GetTaskbarExplorerPid();
                if (oldShellPid <= 0)
                    throw new InvalidOperationException("没有找到拥有任务栏的 Explorer.exe。");

                string currentDetails;
                // Once the backing DLL was replaced, hashing its path only
                // proves the on-disk bytes. The running Explorer still has the
                // previous image mapped in memory, so it must be restarted once.
                if (!stagedNewBytes &&
                    IsHealthyCompatibilityTaskbarModule(oldShellPid, out currentDetails)) return;

                try { if (File.Exists(Paths.TaskbarGuard)) File.Delete(Paths.TaskbarGuard); } catch { }
                try { if (File.Exists(Paths.TaskbarStatus)) File.Delete(Paths.TaskbarStatus); } catch { }
                int newShellPid = RestartTaskbarExplorerOnce(oldShellPid);
                string validation;
                if (!WaitForHealthyCompatibilityTaskbarUpgrade(newShellPid, 45, out validation))
                    throw new InvalidOperationException("无弹窗升级校验失败：" + validation);
            }, "任务栏无弹窗安全升级");
        }

        private static bool IsHealthyTaskbarModuleAlreadyRunning()
        {
            int shellPid = GetTaskbarExplorerPid();
            if (shellPid <= 0 || !IsModuleLoadedInProcess(shellPid, Paths.TaskbarModDll)) return false;

            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(Paths.TaskbarModRegistry))
                {
                    if (key == null || Convert.ToInt32(key.GetValue("Disabled", 1)) != 0)
                        return false;
                    string include = Convert.ToString(key.GetValue("Include", ""));
                    if (include.IndexOf("explorer.exe", StringComparison.OrdinalIgnoreCase) < 0)
                        return false;
                }
            }
            catch { return false; }

            string validation;
            return ValidateTaskbarStatus(shellPid, out validation);
        }

        internal static void RestoreTaskbarContrast()
        {
            RunSilently(delegate
            {
                RequireSafeEngine();
                var settings = TaskbarContrastSettings.Load();
                settings.Enabled = false;
                settings.Save();
                TaskbarContrastRuntime.StopWatcher();
                TaskbarContrastRuntime.ConfigureStartup(false);
                ExportTaskbarRegistrySnapshot();
                int oldShellPid = GetTaskbarExplorerPid();
                try
                {
                    WriteTaskbarWindhawkSettings(false, false);
                    WriteEngineWhitelistForEnabledModules();
                    if (oldShellPid > 0) RestartTaskbarExplorerOnce(oldShellPid);
                    else EnsureTaskbarExplorerRunning();
                    try { if (File.Exists(Paths.TaskbarGuard)) File.Delete(Paths.TaskbarGuard); } catch { }
                    try { if (File.Exists(Paths.TaskbarStatus)) File.Delete(Paths.TaskbarStatus); } catch { }
                }
                catch
                {
                    try { RestoreTaskbarRegistrySnapshot(); } catch { }
                    EnsureTaskbarExplorerRunning();
                    throw;
                }
            }, "任务栏自动对比恢复");
        }

        private static void RunSilently(Action action, string operation)
        {
            try
            {
                action();
                try { if (File.Exists(Paths.OperationStatus)) File.Delete(Paths.OperationStatus); } catch { }
            }
            catch (Exception ex)
            {
                try
                {
                    Directory.CreateDirectory(Paths.AppData);
                    File.WriteAllText(Paths.OperationStatus, operation + "失败：" + ex.Message.Replace("\r", " ").Replace("\n", " "), new UTF8Encoding(true));
                }
                catch { }
            }
        }

        private static void RequireSafeEngine()
        {
            if (!IsAdministrator()) throw new InvalidOperationException("需要管理员权限写入原生界面样式。");
            if (!IsEngineIsolationConfigured()) throw new InvalidOperationException("运行组件白名单不安全，已拒绝应用。");
            if (!IsWindhawkServiceRunning()) throw new InvalidOperationException("运行组件服务没有启动。");
        }

        private static void ValidateAndPreparePanelImage(PanelSettings settings, string output, string displayName)
        {
            if (settings.Mode != "image") return;
            if (String.IsNullOrWhiteSpace(settings.ImagePath) || !File.Exists(settings.ImagePath))
                throw new FileNotFoundException("请为" + displayName + "选择有效的背景图片。", settings.ImagePath);
            ImageProcessor.CreateStartBackground(settings.ImagePath, output, settings.Blur, settings.Fit);
        }

        private static void SetSearchWebSuggestionsPolicy(bool hide)
        {
            const string policyPath = @"Software\Policies\Microsoft\Windows\Explorer";
            const string statePath = @"Software\NativeStartCustomizer";
            const string valueName = "DisableSearchBoxSuggestions";
            const string statePresent = "SearchPolicyWasPresent";
            const string stateValue = "SearchPolicyPreviousValue";

            using (var state = Registry.CurrentUser.CreateSubKey(statePath, true))
            {
                if (hide)
                {
                    if (state.GetValue(statePresent, null) == null)
                    {
                        object previous = null;
                        using (var existing = Registry.CurrentUser.OpenSubKey(policyPath, false))
                            if (existing != null) previous = existing.GetValue(valueName, null);
                        state.SetValue(statePresent, previous == null ? 0 : 1, RegistryValueKind.DWord);
                        if (previous != null) state.SetValue(stateValue, Convert.ToInt32(previous), RegistryValueKind.DWord);
                    }
                    using (var policy = Registry.CurrentUser.CreateSubKey(policyPath, true))
                        policy.SetValue(valueName, 1, RegistryValueKind.DWord);
                    return;
                }

                object presentMarker = state.GetValue(statePresent, null);
                if (presentMarker == null) return;
                using (var policy = Registry.CurrentUser.CreateSubKey(policyPath, true))
                {
                    if (Convert.ToInt32(presentMarker) == 1)
                        policy.SetValue(valueName, Convert.ToInt32(state.GetValue(stateValue, 0)), RegistryValueKind.DWord);
                    else
                        policy.DeleteValue(valueName, false);
                }
                state.DeleteValue(statePresent, false);
                state.DeleteValue(stateValue, false);
            }
        }

        private static void RestartSearchHostForInjection()
        {
            const string expectedPrefix = @"C:\Windows\SystemApps\MicrosoftWindows.Client.CBS_";
            foreach (var process in Process.GetProcessesByName("SearchHost"))
            {
                try
                {
                    string path = process.MainModule == null ? "" : process.MainModule.FileName;
                    if (!String.IsNullOrEmpty(path) && path.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
                        process.Kill();
                }
                catch { }
                finally { process.Dispose(); }
            }

            Thread.Sleep(700);
            var remaining = Process.GetProcessesByName("SearchHost");
            bool relaunched = remaining.Length > 0;
            foreach (var process in remaining) process.Dispose();
            if (relaunched) return;

            var info = new ProcessStartInfo("explorer.exe", @"shell:AppsFolder\MicrosoftWindows.Client.CBS_cw5n1h2txyewy!CortanaUI")
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };
            using (var activation = Process.Start(info)) { }
        }

        private static void RestartShellHostForStyleRefresh()
        {
            const string expectedPath = @"C:\Windows\System32\ShellHost.exe";
            foreach (var process in Process.GetProcessesByName("ShellHost"))
            {
                try
                {
                    string path = process.MainModule == null ? "" : process.MainModule.FileName;
                    if (String.Equals(path, expectedPath, StringComparison.OrdinalIgnoreCase)) process.Kill();
                }
                catch { }
                finally { process.Dispose(); }
            }
            Thread.Sleep(700);
        }

        private static void EnsureStartModuleAvailable()
        {
            if (!File.Exists(Paths.WindhawkModule))
                throw new FileNotFoundException("没有找到开始菜单/搜索面板样式组件。", Paths.WindhawkModule);
        }

        private static void EnsureNotificationModuleAvailable()
        {
            Directory.CreateDirectory(Paths.WindhawkModDir);
            // The module is loaded by ShellHost while the style is enabled, so
            // overwriting an existing copy fails with a sharing violation.
            // Reuse the verified installed module and copy only when missing.
            if (!File.Exists(Paths.NotificationWindhawkModule) && File.Exists(Paths.BundledNotificationModule))
                File.Copy(Paths.BundledNotificationModule, Paths.NotificationWindhawkModule, false);
            if (!File.Exists(Paths.NotificationWindhawkModule))
                throw new FileNotFoundException("没有找到快捷设置样式组件。", Paths.BundledNotificationModule);
        }

        private static void EnsureTaskbarModuleAvailable()
        {
            Directory.CreateDirectory(Paths.WindhawkModDir);
            if (!File.Exists(Paths.BundledTaskbarModule))
                throw new FileNotFoundException("没有找到安全任务栏组件。请保留完整的运行组件文件夹。", Paths.BundledTaskbarModule);
            string bundledHash = ComputeSha256(Paths.BundledTaskbarModule);
            string installedHash = File.Exists(Paths.TaskbarWindhawkModule)
                ? ComputeSha256(Paths.TaskbarWindhawkModule) : "";
            if (!String.Equals(bundledHash, installedHash, StringComparison.OrdinalIgnoreCase))
                File.Copy(Paths.BundledTaskbarModule, Paths.TaskbarWindhawkModule, true);
            if (!File.Exists(Paths.TaskbarWindhawkModule))
                throw new FileNotFoundException("无法安装安全任务栏组件。", Paths.TaskbarWindhawkModule);
            installedHash = ComputeSha256(Paths.TaskbarWindhawkModule);
            if (!String.Equals(bundledHash, installedHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("任务栏组件复制后的 SHA-256 校验失败，已停止升级。");
        }

        private static void DisableLegacyTaskbarStyler()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(Paths.LegacyTaskbarModRegistry, true))
            {
                if (key == null) return;
                key.SetValue("Disabled", 1, RegistryValueKind.DWord);
                key.SetValue("Include", "", RegistryValueKind.String);
            }
        }

        private static void WriteTaskbarWindhawkSettings(bool enabled, bool notifyLiveEngine)
        {
            using (var key = Registry.LocalMachine.CreateSubKey(Paths.TaskbarModRegistry, true))
            {
                // Keep the module disabled until its complete fixed style set
                // has been written. Runtime contrast changes never touch these
                // values and therefore never invoke an XAML hot reload.
                key.SetValue("LibraryFileName", Paths.TaskbarModDll, RegistryValueKind.String);
                key.SetValue("Disabled", 1, RegistryValueKind.DWord);
                key.SetValue("LoggingEnabled", 0, RegistryValueKind.DWord);
                key.SetValue("DebugLoggingEnabled", 0, RegistryValueKind.DWord);
                key.SetValue("Include", enabled ? "explorer.exe" : "", RegistryValueKind.String);
                key.SetValue("Exclude", "", RegistryValueKind.String);
                key.SetValue("IncludeCustom", "", RegistryValueKind.String);
                key.SetValue("ExcludeCustom", "", RegistryValueKind.String);
                key.SetValue("IncludeExcludeCustomOnly", 0, RegistryValueKind.DWord);
                key.SetValue("PatternsMatchCriticalSystemProcesses", 0, RegistryValueKind.DWord);
                key.SetValue("Architecture", "x86-64", RegistryValueKind.String);
                key.SetValue("Version", "1.2", RegistryValueKind.String);
                try { key.DeleteSubKeyTree("Settings", false); } catch (ArgumentException) { }

                using (var st = key.CreateSubKey("Settings", true))
                {
                    st.SetValue("theme", "", RegistryValueKind.String);
                    st.SetValue("clickThroughTaskbar", 0, RegistryValueKind.DWord);
                    // Avoid the Windhawk Toolkit warning popup; the module's
                    // process whitelist is the safety boundary.
                    st.SetValue("xamlDiagnosticsHandling", "allow", RegistryValueKind.String);
                    int index = 0;
                    if (enabled) WriteTaskbarAdaptiveStyles(st, ref index);
                }

                key.SetValue("Disabled", enabled ? 0 : 1, RegistryValueKind.DWord);
                if (notifyLiveEngine)
                    key.SetValue("SettingsChangeTime", UnixTime(), RegistryValueKind.DWord);
            }
        }

        private static void WriteTaskbarAdaptiveStyles(RegistryKey st, ref int index)
        {
            const string right = "Foreground={ThemeResource NativeStartAdaptiveTaskbarRightForeground}";
            const string accent = "Foreground={ThemeResource NativeStartAdaptiveTaskbarAccentForeground}";

            // Only leaf text/glyph elements receive a foreground brush. Parent
            // controls keep their native theme, opacity, font metrics, layout
            // and pointer visual states.
            WriteControlStyle(st, ref index, "TextBlock#TimeInnerTextBlock", right);
            WriteControlStyle(st, ref index, "TextBlock#DateInnerTextBlock", right);
            WriteControlStyle(st, ref index, "SystemTray.AdaptiveTextBlock#LanguageInnerTextBlock > TextBlock#InnerTextBlock", right);
            WriteControlStyle(st, ref index, "SystemTray.AdaptiveTextBlock#Base > TextBlock#InnerTextBlock", right);
            WriteControlStyle(st, ref index, "SystemTray.AdaptiveTextBlock#Underlay > TextBlock#InnerTextBlock", right);
            WriteControlStyle(st, ref index, "SystemTray.TextIconContent > Grid#ContainerGrid > SystemTray.AdaptiveTextBlock > TextBlock", right);
            WriteControlStyle(st, ref index, "SystemTray.TextIconContent > Grid#ContainerGrid > SystemTray.AdaptiveTextBlock#Base > TextBlock#InnerTextBlock", right);
            WriteControlStyle(st, ref index, "SystemTray.BatteryIconContent > Grid#ContainerGrid > StackPanel > Grid > TextBlock", right);
            WriteControlStyle(st, ref index, "SystemTray.BatteryIconContent > Grid#ContainerGrid > StackPanel > Grid > TextBlock[1]", right);
            WriteControlStyle(st, ref index, "TextBlock#BatteryTextBlock", right);

            // Windows exposes active privacy/unread state as a separate leaf
            // overlay. Keep that overlay pale blue; the ordinary base glyph
            // continues to follow the local black/white contrast brush.
            WriteControlStyle(st, ref index, "SystemTray.AdaptiveTextBlock#AccentOverlay > TextBlock#InnerTextBlock", accent);
            WriteControlStyle(st, ref index, "TextBlock[Text=\uE361]", accent);
            WriteControlStyle(st, ref index, "TextBlock[Text=\uE720]", accent);
            WriteControlStyle(st, ref index, "TextBlock[Text=\uEC71]", accent);
            WriteControlStyle(st, ref index, "TextBlock[Text=\uF12E]", accent);
            WriteControlStyle(st, ref index, "TextBlock[Text=\uF47F]", accent);
            WriteControlStyle(st, ref index, "TextBlock[Text=\uE7E7]", accent);
            WriteControlStyle(st, ref index, "TextBlock[Text=\uEE7A]", accent);
            WriteControlStyle(st, ref index, "TextBlock[Text=\uF2A5]", accent);

            // Keep the hidden-tray overflow flyout native, rounded and white.
            // This deliberately does not add a square base behind the flyout.
            WriteControlStyle(st, ref index, "Grid#OverflowRootGrid", "RequestedTheme=1");
            WriteControlStyle(st, ref index, "SystemTray.NotificationAreaOverflow > Windows.UI.Xaml.Controls.Grid#OverflowRootGrid > Windows.UI.Xaml.Controls.Border#OverflowFlyoutBackgroundBorder",
                "RequestedTheme=1", "Background:=<SolidColorBrush Color=\"#FFF7F7F7\" />");

            // High-contrast hover hints remain obvious on any wallpaper.
            WriteControlStyle(st, ref index, "ToolTip", "Background=#F21C2027", "Foreground=White",
                "BorderBrush=#B8FFFFFF", "BorderThickness=1", "CornerRadius=8", "RequestedTheme=2", "Opacity=1");
            WriteControlStyle(st, ref index, "ToolTip > ContentPresenter#LayoutRoot", "Background=#F21C2027", "Foreground=White",
                "BorderBrush=#B8FFFFFF", "BorderThickness=1", "CornerRadius=8", "Padding=11,7,11,7", "Opacity=1");
            WriteControlStyle(st, ref index, "ToolTip > ContentPresenter#LayoutRoot > TextBlock", "Foreground=White", "FontWeight=SemiBold", "FontSize=13");
            WriteControlStyle(st, ref index, "ToolTip > ContentPresenter#LayoutRoot > StackPanel > TextBlock", "Foreground=White", "FontWeight=SemiBold", "FontSize=13");
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }

        private static void ExportTaskbarRegistrySnapshot()
        {
            Directory.CreateDirectory(Paths.AppData);
            string absent = Paths.TaskbarRegistryRollback + ".absent";
            try { if (File.Exists(Paths.TaskbarRegistryRollback)) File.Delete(Paths.TaskbarRegistryRollback); } catch { }
            try { if (File.Exists(absent)) File.Delete(absent); } catch { }
            using (var key = Registry.LocalMachine.OpenSubKey(Paths.TaskbarModRegistry))
            {
                if (key == null)
                {
                    File.WriteAllText(absent, "absent", Encoding.ASCII);
                    return;
                }
            }
            RunRegistryCommand("export \"HKLM\\" + Paths.TaskbarModRegistry + "\" \"" + Paths.TaskbarRegistryRollback + "\" /y");
            if (!File.Exists(Paths.TaskbarRegistryRollback))
                throw new InvalidOperationException("无法创建任务栏组件注册配置回滚副本。");
        }

        private static void RestoreTaskbarRegistrySnapshot()
        {
            string absent = Paths.TaskbarRegistryRollback + ".absent";
            if (File.Exists(absent))
            {
                try { Registry.LocalMachine.DeleteSubKeyTree(Paths.TaskbarModRegistry, false); } catch { }
                return;
            }
            if (!File.Exists(Paths.TaskbarRegistryRollback)) return;
            RunRegistryCommand("import \"" + Paths.TaskbarRegistryRollback + "\"");
        }

        private static void RunRegistryCommand(string arguments)
        {
            using (var process = Process.Start(new ProcessStartInfo("reg.exe", arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            }))
            {
                if (process == null || !process.WaitForExit(15000) || process.ExitCode != 0)
                    throw new InvalidOperationException("注册配置备份/恢复失败。");
            }
        }

        private static int GetTaskbarExplorerPid()
        {
            IntPtr taskbar = FindWindow("Shell_TrayWnd", null);
            if (taskbar == IntPtr.Zero) return 0;
            uint pid;
            GetWindowThreadProcessId(taskbar, out pid);
            return unchecked((int)pid);
        }

        private static int RestartTaskbarExplorerOnce(int oldShellPid)
        {
            if (oldShellPid <= 0 || GetTaskbarExplorerPid() != oldShellPid)
                throw new InvalidOperationException("任务栏 Explorer PID 已变化，未执行重复重启。");

            using (var shell = Process.GetProcessById(oldShellPid))
            {
                shell.Kill();
                shell.WaitForExit(10000);
            }

            int newPid = WaitForTaskbarExplorer(oldShellPid, 10);
            if (newPid > 0) return newPid;

            StartExplorerShell();
            newPid = WaitForTaskbarExplorer(oldShellPid, 15);
            if (newPid <= 0)
                throw new InvalidOperationException("Explorer 已退出，但新的任务栏没有在限定时间内启动。");
            return newPid;
        }

        private static int WaitForTaskbarExplorer(int rejectedPid, int seconds)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(seconds);
            do
            {
                int pid = GetTaskbarExplorerPid();
                if (pid > 0 && pid != rejectedPid) return pid;
                Thread.Sleep(250);
            } while (DateTime.UtcNow < deadline);
            return 0;
        }

        private static void StartExplorerShell()
        {
            string explorer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            Process.Start(new ProcessStartInfo(explorer)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows)
            });
        }

        private static void EnsureTaskbarExplorerRunning()
        {
            if (GetTaskbarExplorerPid() > 0) return;
            try { StartExplorerShell(); } catch { }
            WaitForTaskbarExplorer(0, 15);
        }

        private static bool IsModuleLoadedInProcess(int processId, string moduleName)
        {
            if (processId <= 0) return false;
            try
            {
                using (var process = Process.GetProcessById(processId))
                    foreach (ProcessModule module in process.Modules)
                        if (String.Equals(module.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            catch { }
            return false;
        }

        private static string GetLoadedModulePath(int processId, string moduleName)
        {
            if (processId <= 0) return "";
            try
            {
                using (var process = Process.GetProcessById(processId))
                    foreach (ProcessModule module in process.Modules)
                        if (String.Equals(module.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase))
                            return module.FileName ?? "";
            }
            catch { }
            return "";
        }

        private static bool StageCompatibilityTaskbarModule()
        {
            string sourceHash = ComputeSha256(Paths.BundledTaskbarModule);
            string target = Paths.CompatibilityTaskbarWindhawkModule;
            if (File.Exists(target) && String.Equals(sourceHash, ComputeSha256(target), StringComparison.OrdinalIgnoreCase))
                return false;

            string renamed = target + ".v3.6-passive-backup";
            for (int suffix = 2; File.Exists(renamed); suffix++)
                renamed = target + ".v3.6-passive-backup-" + suffix.ToString();

            bool moved = false;
            try
            {
                if (File.Exists(target))
                {
                    File.Move(target, renamed);
                    moved = true;
                }
                File.Copy(Paths.BundledTaskbarModule, target, false);
                if (!String.Equals(sourceHash, ComputeSha256(target), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("兼容入口复制后的 SHA-256 校验失败。");
                return true;
            }
            catch
            {
                try { if (File.Exists(target)) File.Delete(target); } catch { }
                try { if (moved && File.Exists(renamed)) File.Move(renamed, target); } catch { }
                throw;
            }
        }

        private static bool IsHealthyCompatibilityTaskbarModule(int shellPid, out string details)
        {
            details = "修正版尚未载入任务栏 Explorer。";
            string loadedPath = GetLoadedModulePath(shellPid, Paths.PreviousTaskbarModDll);
            if (String.IsNullOrEmpty(loadedPath) || !File.Exists(loadedPath)) return false;
            string expectedHash = ComputeSha256(Paths.BundledTaskbarModule);
            string loadedHash;
            try { loadedHash = ComputeSha256(loadedPath); }
            catch { return false; }
            if (!String.Equals(expectedHash, loadedHash, StringComparison.OrdinalIgnoreCase))
            {
                details = "Explorer 仍映射旧哈希。";
                return false;
            }
            return ValidateTaskbarStatus(shellPid, out details);
        }

        private static Dictionary<string, string> ReadTaskbarStatus()
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(Paths.TaskbarStatus)) return values;
                foreach (string raw in File.ReadAllLines(Paths.TaskbarStatus, Encoding.Unicode))
                {
                    string line = raw.Trim();
                    int equals = line.IndexOf('=');
                    if (equals > 0) values[line.Substring(0, equals).Trim()] = line.Substring(equals + 1).Trim();
                }
                if (values.Count == 0)
                {
                    foreach (string raw in File.ReadAllLines(Paths.TaskbarStatus, Encoding.Default))
                    {
                        string line = raw.Trim();
                        int equals = line.IndexOf('=');
                        if (equals > 0) values[line.Substring(0, equals).Trim()] = line.Substring(equals + 1).Trim();
                    }
                }
            }
            catch { }
            return values;
        }

        private static bool ValidateTaskbarStatus(int shellPid, out string details)
        {
            Dictionary<string, string> status = ReadTaskbarStatus();
            int pid, brushes, elements, dispatched;
            Int32.TryParse(status.ContainsKey("Pid") ? status["Pid"] : "", out pid);
            Int32.TryParse(status.ContainsKey("UpdatedBrushes") ? status["UpdatedBrushes"] : "", out brushes);
            Int32.TryParse(status.ContainsKey("UpdatedElements") ? status["UpdatedElements"] : "", out elements);
            Int32.TryParse(status.ContainsKey("Dispatched") ? status["Dispatched"] : "", out dispatched);
            details = String.Format("ShellPid={0}, StatusPid={1}, UpdatedBrushes={2}, UpdatedElements={3}, Dispatched={4}",
                shellPid, pid, brushes, elements, dispatched);
            return pid == shellPid && brushes == 3 && elements > 0 && dispatched == 1;
        }

        private static bool WaitForHealthyTaskbarUpgrade(int expectedShellPid, int seconds, out string details)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(seconds);
            details = "尚未读取到新组件状态。";
            do
            {
                int currentShellPid = GetTaskbarExplorerPid();
                if (currentShellPid > 0 && currentShellPid != expectedShellPid)
                {
                    details = "Explorer 在校验期内再次重启，已停止且不会循环重试。";
                    return false;
                }
                if (currentShellPid == expectedShellPid &&
                    IsModuleLoadedInProcess(expectedShellPid, Paths.TaskbarModDll) &&
                    ValidateTaskbarStatus(expectedShellPid, out details))
                    return true;
                Thread.Sleep(250);
            } while (DateTime.UtcNow < deadline);
            return false;
        }

        private static bool WaitForHealthyCompatibilityTaskbarUpgrade(int expectedShellPid, int seconds, out string details)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(seconds);
            details = "尚未读取到修正版状态。";
            do
            {
                int currentShellPid = GetTaskbarExplorerPid();
                if (currentShellPid > 0 && currentShellPid != expectedShellPid)
                {
                    details = "Explorer 在校验期内再次重启，已停止且不会循环重试。";
                    return false;
                }
                if (currentShellPid == expectedShellPid &&
                    IsHealthyCompatibilityTaskbarModule(expectedShellPid, out details))
                    return true;
                Thread.Sleep(250);
            } while (DateTime.UtcNow < deadline);
            return false;
        }

        private static void CleanupPreviousTaskbarModule()
        {
            string installedOld = Path.Combine(Paths.WindhawkModDir, Paths.PreviousTaskbarModDll);
            string bundledOld = Path.Combine(Application.StartupPath, @"运行组件\Mod", Paths.PreviousTaskbarModDll);
            try { if (File.Exists(installedOld)) File.Delete(installedOld); } catch { }
            try { if (File.Exists(bundledOld)) File.Delete(bundledOld); } catch { }
        }

        private static void CreateFirstBackup()
        {
            Directory.CreateDirectory(Paths.AppData);
            if (File.Exists(Paths.Backup) || File.Exists(Paths.BackupAbsent)) return;
            using (var key = Registry.LocalMachine.OpenSubKey(Paths.ModRegistry))
            {
                if (key == null)
                {
                    File.WriteAllText(Paths.BackupAbsent, "The Windhawk mod registry key did not exist before the first apply.", Encoding.UTF8);
                    return;
                }
            }
            var psi = new ProcessStartInfo("reg.exe", "export \"HKLM\\" + Paths.ModRegistry + "\" \"" + Paths.Backup + "\" /y")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using (var p = Process.Start(psi))
            {
                if (!p.WaitForExit(5000)) throw new System.TimeoutException("备份现有 Windhawk 设置超时。已停止应用。");
                if (p.ExitCode != 0) throw new InvalidOperationException("无法备份现有 Windhawk 模组设置。为避免覆盖你的配置，已停止应用。");
            }
        }

        private static void WriteWindhawkSettings(DesignSettings s)
        {
            var include = new List<string>();
            if (s.Enabled) include.Add("StartMenuExperienceHost.exe");
            using (var key = Registry.LocalMachine.CreateSubKey(Paths.ModRegistry, true))
            {
                key.SetValue("LibraryFileName", Paths.ModDll, RegistryValueKind.String);
                key.SetValue("Disabled", 1, RegistryValueKind.DWord);
                key.SetValue("LoggingEnabled", 0, RegistryValueKind.DWord);
                key.SetValue("DebugLoggingEnabled", 0, RegistryValueKind.DWord);
                key.SetValue("Include", String.Join("|", include), RegistryValueKind.String);
                key.SetValue("Exclude", "", RegistryValueKind.String);
                key.SetValue("IncludeCustom", "", RegistryValueKind.String);
                key.SetValue("ExcludeCustom", "", RegistryValueKind.String);
                key.SetValue("IncludeExcludeCustomOnly", 0, RegistryValueKind.DWord);
                key.SetValue("PatternsMatchCriticalSystemProcesses", 0, RegistryValueKind.DWord);
                key.SetValue("Architecture", "x86-64", RegistryValueKind.String);
                key.SetValue("Version", "1.6", RegistryValueKind.String);
                try { key.DeleteSubKeyTree("Settings", false); } catch (ArgumentException) { }
                using (var st = key.CreateSubKey("Settings", true))
                {
                    st.SetValue("theme", "", RegistryValueKind.String);
                    st.SetValue("disableNewStartMenuLayout", s.Layout == "newLayoutSideBySide" ? "newLayoutSideBySide" : "default", RegistryValueKind.String);
                    int styleIndex = 0;
                    if (s.Enabled) WriteStartStyles(st, ref styleIndex, s);
                }
                key.SetValue("Disabled", include.Count == 0 ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("SettingsChangeTime", UnixTime(), RegistryValueKind.DWord);
            }
        }

        private static void WriteStartStyles(RegistryKey st, ref int styleIndex, DesignSettings s)
        {
            Color tintColor = ColorTools.EffectiveTint(s);
            WriteControlStyle(st, ref styleIndex, "Border#AcrylicBorder", BuildStartBrush(s, tintColor),
                "BorderThickness=0", "BorderBrush=Transparent", "CornerRadius=" + s.CornerRadius);
            if (s.Mode == "image")
                WriteControlStyle(st, ref styleIndex, "Border#AcrylicOverlay", "Visibility=Visible",
                    "Background=#" + Alpha(s.TintOpacity).ToString("X2") + tintColor.R.ToString("X2") + tintColor.G.ToString("X2") + tintColor.B.ToString("X2"),
                    "CornerRadius=" + s.CornerRadius);
            else
                WriteControlStyle(st, ref styleIndex, "Border#AcrylicOverlay", "Visibility=Collapsed");

            foreach (string target in new[] { "Border#StartDropShadow", "Border#DropShadow", "Border#RootGridDropShadow", "Border#RightCompanionDropShadow" })
                WriteControlStyle(st, ref styleIndex, target, "Visibility=Collapsed");
            foreach (string target in new[] { "Border#DropShadowDismissTarget", "Border#RightCompanionDropShadowDismissTarget", "Border#LeftCompanionDropShadowDismissTarget" })
                WriteControlStyle(st, ref styleIndex, target, "Background=Transparent", "BorderThickness=0", "BorderBrush=Transparent");

            int menuWidth, menuHeight;
            GetMenuSize(s, out menuWidth, out menuHeight);
            var mainStyles = new List<string> { "RequestedTheme=" + (ColorTools.UseDarkTheme(s) ? "2" : "1"), "Background=Transparent" };
            if (menuWidth > 0) mainStyles.AddRange(new[] { "Width=" + menuWidth, "MinWidth=" + menuWidth, "MaxWidth=" + menuWidth });
            WriteControlStyle(st, ref styleIndex, "Grid#MainMenu", mainStyles.ToArray());
            if (menuWidth > 0 && menuHeight > 0)
            {
                WriteControlStyle(st, ref styleIndex, "StartMenu.StartBlendedFlexFrame > Grid#FrameRoot",
                    "Width=" + menuWidth, "Height=" + menuHeight, "MinWidth=" + menuWidth, "MaxWidth=" + menuWidth,
                    "MinHeight=" + menuHeight, "MaxHeight=" + menuHeight, "Margin=0", "Padding=0", "Background=Transparent");
                WriteControlStyle(st, ref styleIndex, "Border#AcrylicBorder", "Width=" + menuWidth, "Height=" + menuHeight);
            }
            if (s.AlignToStartButton)
            {
                string details;
                int translateX = GetStartButtonAlignmentOffset(menuWidth > 0 ? menuWidth : 900, out details);
                if (translateX != 0) WriteControlStyle(st, ref styleIndex, "StartMenu.StartBlendedFlexFrame",
                    "RenderTransform:=<TranslateTransform X=\"" + translateX + "\" />");
            }
            if (s.HideSearch) WriteControlStyle(st, ref styleIndex, "StartMenu.SearchBoxToggleButton", "Visibility=Collapsed", "Height=0", "Margin=0");
            if (s.HideRecommended)
            {
                foreach (string target in new[] { "Grid#SuggestionsParentContainer", "Grid#TopLevelSuggestionsRoot" })
                    WriteControlStyle(st, ref styleIndex, target, "Visibility=Collapsed", "Height=0", "MinHeight=0", "MaxHeight=0", "Margin=0");
                foreach (string target in new[] { "Windows.UI.Xaml.Controls.Grid#TopLevelSuggestionsListHeader", "Windows.UI.Xaml.Controls.Grid#NoTopLevelSuggestionsText", "Windows.UI.Xaml.Controls.Grid#TopLevelSuggestionsContainer", "Windows.UI.Xaml.Controls.Grid#ShowMoreSuggestions", "Windows.UI.Xaml.Controls.GridView#RecommendedList" })
                    WriteControlStyle(st, ref styleIndex, target, "Visibility=Collapsed", "Height=0", "Margin=0");
                WriteControlStyle(st, ref styleIndex, "StartMenu.PinnedList", "Margin=0");
            }
        }

        private static string BuildStartBrush(DesignSettings s, Color tintColor)
        {
            if (s.Mode == "image") return "Background:=<ImageBrush Stretch=\"" + FitValue(s.Fit) + "\" ImageSource=\"ms-appdata:///local/StartCanvasNative/background.png\" />";
            return BuildBrush(s.Mode, s.Blur, s.TintOpacity, tintColor, null, s.Fit);
        }

        private static string BuildBrush(string mode, int blur, int opacity, Color tintColor, string imageUri, string fit)
        {
            if (mode == "image" && !String.IsNullOrEmpty(imageUri))
                return "Background:=<ImageBrush Stretch=\"" + FitValue(fit) + "\" ImageSource=\"" + imageUri + "\" />";
            if (mode == "color")
                return "Background=#" + Alpha(opacity).ToString("X2") + tintColor.R.ToString("X2") + tintColor.G.ToString("X2") + tintColor.B.ToString("X2");
            return String.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Background:=<WindhawkBlur BlurAmount=\"{0}\" TintColor=\"#{1:X2}{2:X2}{3:X2}\" TintOpacity=\"{4:0.00}\" TintSaturation=\"1.0\" NoiseOpacity=\"0.02\" FallbackColor=\"#{1:X2}{2:X2}{3:X2}\" />",
                blur, tintColor.R, tintColor.G, tintColor.B, opacity / 100.0);
        }

        private static void WriteSearchStyles(RegistryKey st, ref int styleIndex, PanelSettings s)
        {
            string theme = s.UseDarkTextTheme() ? "2" : "1";
            string radius = s.CornerRadius.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string backgroundBrush = BuildBrush(s.Mode, s.Blur, s.TintOpacity, s.Tint,
                "ms-appdata:///local/NativeShellCustomizer/search-background.png", s.Fit);

            // TaskbarSearchPage is hosted inside a native 12-DIP inset on every side.
            // The user setting represents the complete flyout, so the XAML content must
            // be 24 DIPs smaller. Without this conversion an 800x710 page is clipped to
            // 776x686 and only its top-left rounded corner remains inside the host.
            int contentWidth = s.Width > 0 ? Math.Max(320, s.Width - 24) : 0;
            int contentHeight = s.Height > 0 ? Math.Max(320, s.Height - 24) : 0;

            // The search host owns a work-area-sized surface. Keep the resized native page
            // attached to the taskbar instead of leaving it aligned to the top of that surface.
            var pageStyles = new List<string> {
                "Background=Transparent", "RequestedTheme=" + theme,
                "HorizontalAlignment=Center", "VerticalAlignment=Bottom", "Margin=0"
            };
            // The page owns the complete requested flyout size. Only its child content
            // is inset by Windows, so don't subtract the 24 DIPs at this level.
            if (s.Width > 0) pageStyles.AddRange(new[] { "Width=" + s.Width, "MinWidth=" + s.Width, "MaxWidth=" + s.Width });
            if (s.Height > 0) pageStyles.AddRange(new[] { "Height=" + s.Height, "MinHeight=" + s.Height, "MaxHeight=" + s.Height });
            WriteControlStyle(st, ref styleIndex, "Cortana.UI.Views.TaskbarSearchPage", pageStyles.ToArray());
            WriteControlStyle(st, ref styleIndex, "SearchUx.SearchUI", pageStyles.ToArray());

            // Sizing AppBorder alone only rounds the painted brush. Size and clip the complete
            // root so web content and the other native background layers cannot cover the corners.
            var rootStyles = new List<string> {
                "Background=Transparent", "RequestedTheme=" + theme,
                "HorizontalAlignment=Stretch", "VerticalAlignment=Stretch",
                "CornerRadius=" + radius, "BorderThickness=0"
            };
            if (contentWidth > 0) rootStyles.AddRange(new[] { "Width=" + contentWidth, "MinWidth=" + contentWidth, "MaxWidth=" + contentWidth });
            if (contentHeight > 0) rootStyles.AddRange(new[] { "Height=" + contentHeight, "MinHeight=" + contentHeight, "MaxHeight=" + contentHeight });
            WriteControlStyle(st, ref styleIndex, "Cortana.UI.Views.TaskbarSearchPage > Grid#RootGrid", rootStyles.ToArray());
            WriteControlStyle(st, ref styleIndex, "SearchUx.SearchUI > Grid#RootGrid", rootStyles.ToArray());

            // OuterBorderGrid is a Grid, not the visual border that owns the flyout corners.
            // Painting the brush here leaves a square rectangle visible behind AppBorder and
            // makes the top-right/bottom corners look unrounded. Keep it transparent and let
            // the native AppBorder below be the only element that paints the panel background.
            var outerStyles = new List<string>(rootStyles);
            WriteControlStyle(st, ref styleIndex, "Cortana.UI.Views.TaskbarSearchPage > Grid#RootGrid > Grid#OuterBorderGrid", outerStyles.ToArray());

            var frameStyles = new[] {
                "Background=Transparent", "BorderBrush=Transparent", "BorderThickness=0",
                "CornerRadius=" + radius
            };
            var borderFrameStyles = new[] {
                "Background=Transparent", "BorderBrush=Transparent", "BorderThickness=0", "CornerRadius=" + radius
            };
            WriteControlStyle(st, ref styleIndex, "Cortana.UI.Views.TaskbarSearchPage > Grid#RootGrid > Grid#OuterBorderGrid > Grid#BorderGrid", frameStyles);
            WriteControlStyle(st, ref styleIndex, "Cortana.UI.Views.TaskbarSearchPage > Grid#RootGrid > Grid#OuterBorderGrid > Grid#BorderGrid > Border", borderFrameStyles);
            WriteControlStyle(st, ref styleIndex, "Border#LayerBorder", frameStyles);
            WriteControlStyle(st, ref styleIndex, "Border#AccentLayerBorder", frameStyles);
            WriteControlStyle(st, ref styleIndex, "Border#AccentAppBorder", frameStyles);
            WriteControlStyle(st, ref styleIndex, "Border#dropshadow", new[] { "Background=Transparent", "Opacity=0", "CornerRadius=" + radius });

            // AppBorder is a child of the broad Border rule above, so write it last. This keeps
            // every covering layer transparent while restoring the selected brush on the one
            // border whose live bounds provide all four rounded corners.
            var appBorderStyles = new[] { backgroundBrush, "BorderThickness=0",
                "BorderBrush=Transparent", "CornerRadius=" + radius,
                "BackgroundSizing=InnerBorderEdge", "RequestedTheme=" + theme };
            WriteControlStyle(st, ref styleIndex, "Border#AppBorder", appBorderStyles);

            string text = s.UseDarkTextTheme() ? "#FFFFFF" : "#111111";
            WriteWebStyle(st, 0, "html, body, #root", "background: transparent !important", "background-color: transparent !important", "color: " + text + " !important");
            WriteWebStyle(st, 1, ".suggContainer, .previewContainer, .curatedSettingsGroup, .scope-tile > div, .groupContainer, .suggsList, .suggsListContainer, .suggestion, .suggDetailsContainer, .topResults, #qfContainer, #qfPreviewPane, [class*='card' i], [class*='tile' i]", "background: transparent !important", "background-color: transparent !important", "box-shadow: none !important", "border-color: transparent !important", "color: " + text + " !important");
            WriteWebStyle(st, 2, "h1, h2, h3, h4, .groupTitle, .title, .details, .secondaryText, .menu-item_details", "color: " + text + " !important");
            if (s.HideSearchAds)
            {
                WriteWebStyle(st, 3, "#qfPreviewPane, #temporaryMessages, #chatButtonRight, #gr11, #gr36, #gr43, #pp_Share, #pp_Review, div[data-region='TopApps'], #root.zeroInput19H1 [data-region*='Trending' i], #root.zeroInput19H1 [data-region*='Game' i], #root.zeroInput19H1 [data-region*='Quiz' i], #root.zeroInput19H1 [data-region*='ImageOfTheDay' i], #root.zeroInput19H1 [data-region*='QuoteOfTheDay' i], #root.zeroInput19H1 [data-region*='WordOfTheDay' i], #root.zeroInput19H1 [class*='trending' i], #root.zeroInput19H1 [class*='game' i], #root.zeroInput19H1 [class*='quiz' i], #root.zeroInput19H1 [class*='imageOfTheDay' i], #root.zeroInput19H1 [class*='quoteOfTheDay' i], #root.zeroInput19H1 [class*='wordOfTheDay' i]", "display: none !important", "visibility: hidden !important");
            }
        }

        private static void WriteWebStyle(RegistryKey st, int index, string target, params string[] styles)
        {
            st.SetValue("webContentStyles[" + index + "].target", target, RegistryValueKind.String);
            for (int i = 0; i < styles.Length; i++) st.SetValue("webContentStyles[" + index + "].styles[" + i + "]", styles[i], RegistryValueKind.String);
        }

        private static void WriteNotificationSettings(PanelSettings s)
        {
            using (var key = Registry.LocalMachine.CreateSubKey(Paths.NotificationModRegistry, true))
            {
                key.SetValue("LibraryFileName", Paths.NotificationModDll, RegistryValueKind.String);
                key.SetValue("Disabled", 1, RegistryValueKind.DWord);
                key.SetValue("LoggingEnabled", 0, RegistryValueKind.DWord);
                key.SetValue("DebugLoggingEnabled", 0, RegistryValueKind.DWord);
                key.SetValue("Include", "ShellHost.exe", RegistryValueKind.String);
                key.SetValue("Exclude", "", RegistryValueKind.String);
                key.SetValue("IncludeCustom", "", RegistryValueKind.String);
                key.SetValue("ExcludeCustom", "", RegistryValueKind.String);
                key.SetValue("IncludeExcludeCustomOnly", 0, RegistryValueKind.DWord);
                key.SetValue("PatternsMatchCriticalSystemProcesses", 0, RegistryValueKind.DWord);
                key.SetValue("Architecture", "x86-64", RegistryValueKind.String);
                key.SetValue("Version", "1.6", RegistryValueKind.String);
                try { key.DeleteSubKeyTree("Settings", false); } catch (ArgumentException) { }
                using (var st = key.CreateSubKey("Settings", true))
                {
                    st.SetValue("theme", "", RegistryValueKind.String);
                    int index = 0;
                    var quickStyles = new List<string> { BuildBrush(s.Mode, s.Blur, s.TintOpacity, s.Tint,
                        "ms-appdata:///local/NativeShellCustomizer/quick-background.png", s.Fit),
                        "CornerRadius=" + s.CornerRadius, "BorderThickness=0", "Shadow:=" };
                    WriteControlStyle(st, ref index, "Grid#ControlCenterRegion", quickStyles.ToArray());
                    Color mediaColor = s.SeparateMedia ? s.MediaTint : s.Tint;
                    int mediaOpacity = s.SeparateMedia ? s.MediaOpacity : s.TintOpacity;
                    int mediaRadius = s.SeparateMedia ? s.MediaRadius : s.CornerRadius;
                    string mediaBrush = s.SeparateMedia
                        ? BuildBrush("color", s.Blur, mediaOpacity, mediaColor, null, s.Fit)
                        : BuildBrush(s.Mode, s.Blur, mediaOpacity, mediaColor, "ms-appdata:///local/NativeShellCustomizer/quick-background.png", s.Fit);
                    WriteControlStyle(st, ref index, "Grid#MediaTransportControlsRegion", mediaBrush,
                        "CornerRadius=" + mediaRadius, "BorderThickness=0", "Shadow:=");
                    WriteControlStyle(st, ref index, "ControlCenter.ControlCenterPage > Grid#RootGrid", "RequestedTheme=" + (s.UseDarkTextTheme() ? "2" : "1"));
                }
                key.SetValue("Disabled", s.Enabled ? 0 : 1, RegistryValueKind.DWord);
                key.SetValue("SettingsChangeTime", UnixTime(), RegistryValueKind.DWord);
            }
        }

        private static void WriteControlStyle(RegistryKey settingsKey, ref int index, string target, params string[] styles)
        {
            settingsKey.SetValue("controlStyles[" + index + "].target", target, RegistryValueKind.String);
            for (int i = 0; i < styles.Length; i++)
                settingsKey.SetValue("controlStyles[" + index + "].styles[" + i + "]", styles[i], RegistryValueKind.String);
            index++;
        }

        private static void GetMenuSize(DesignSettings settings, out int width, out int height)
        {
            width = height = 0;
            if (settings.SizePreset == "compact") { width = 680; height = 620; }
            else if (settings.SizePreset == "wide") { width = 1080; height = 760; }
            else if (settings.SizePreset == "fullscreen")
            {
                Rectangle area = Screen.PrimaryScreen.WorkingArea;
                width = Math.Max(680, area.Width - 24);
                height = Math.Max(620, area.Height - 24);
            }
            else if (settings.SizePreset == "custom")
            {
                Rectangle area = Screen.PrimaryScreen.WorkingArea;
                int maxWidth = Math.Max(680, area.Width - 24);
                int maxHeight = Math.Max(620, area.Height - 24);
                width = Math.Min(maxWidth, Math.Max(680, settings.MenuWidth));
                height = Math.Min(maxHeight, Math.Max(620, settings.MenuHeight));
            }
        }

        internal static string AlignmentDiagnostic()
        {
            string details;
            int offset = GetStartButtonAlignmentOffset(900, out details);
            return details + Environment.NewLine + "TranslateX=" + offset;
        }

        private static int GetStartButtonAlignmentOffset(int menuWidth, out string details)
        {
            details = "Start button not found; centered fallback will be used.";
            try
            {
                IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
                if (taskbarHandle == IntPtr.Zero) return 0;
                AutomationElement taskbar = AutomationElement.FromHandle(taskbarHandle);
                if (taskbar == null) return 0;

                Condition condition = new OrCondition(
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "StartButton"),
                    new AndCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                        new OrCondition(
                            new PropertyCondition(AutomationElement.NameProperty, "开始"),
                            new PropertyCondition(AutomationElement.NameProperty, "Start"))));
                AutomationElement startButton = taskbar.FindFirst(TreeScope.Descendants, condition);
                if (startButton == null) return 0;

                System.Windows.Rect buttonBounds = startButton.Current.BoundingRectangle;
                Screen screen = Screen.FromHandle(taskbarHandle);
                uint dpi = GetDpiForWindow(taskbarHandle);
                double scale = dpi > 0 ? dpi / 96.0 : 1.0;
                double screenLeftDip = screen.Bounds.Left / scale;
                double screenWidthDip = screen.Bounds.Width / scale;
                double buttonLeftDip = buttonBounds.Left / scale;
                double centeredMenuLeftDip = screenLeftDip + (screenWidthDip - menuWidth) / 2.0;
                int offset = (int)Math.Round(buttonLeftDip - centeredMenuLeftDip);
                details = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Screen={0:0}..{1:0} DIP; StartLeft={2:0.0} DIP; MenuWidth={3}; CenteredLeft={4:0.0} DIP",
                    screenLeftDip, screenLeftDip + screenWidthDip, buttonLeftDip, menuWidth, centeredMenuLeftDip);
                return offset;
            }
            catch (Exception ex)
            {
                details = "Alignment fallback: " + ex.GetType().Name;
                return 0;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr window);

        private static int Alpha(int percent) { return Math.Max(0, Math.Min(255, (int)Math.Round(percent * 2.55))); }
        private static string FitValue(string fit)
        {
            return fit == "Uniform" || fit == "Fill" || fit == "None" ? fit : "UniformToFill";
        }
        private static int UnixTime()
        {
            long value = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            return (int)(value & 0x7fffffff);
        }
        private static bool IsWindhawkServiceRunning()
        {
            return IsServiceRunning("Windhawk");
        }

        private static bool IsServiceRunning(string serviceName)
        {
            try
            {
                using (var service = new ServiceController(serviceName))
                    return service.Status == ServiceControllerStatus.Running;
            }
            catch { return false; }
        }

        internal static bool IsEngineIsolationConfigured()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(Paths.EngineSettingsRegistry))
                {
                    if (key == null) return false;
                    string include = (key.GetValue("Include", "") as string ?? "").Trim();
                    string exclude = (key.GetValue("Exclude", "") as string ?? "").Trim();
                    var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                        "StartMenuExperienceHost.exe", "ShellHost.exe", "explorer.exe"
                    };
                    string[] entries = include.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    if (entries.Any(entry => !allowed.Contains(entry.Trim()))) return false;
                    if (!String.Equals(exclude, "*", StringComparison.Ordinal)) return false;
                    if (Convert.ToInt32(key.GetValue("InjectIntoCriticalProcesses", 0)) != 0) return false;
                    if (Convert.ToInt32(key.GetValue("InjectIntoIncompatiblePrograms", 0)) != 0) return false;
                    if (Convert.ToInt32(key.GetValue("InjectIntoGames", 0)) != 0) return false;
                    return true;
                }
            }
            catch { return false; }
        }

        private static bool WriteEngineWhitelistForEnabledModules()
        {
            var processes = new List<string>();
            DesignSettings start = DesignSettings.Load(Paths.UserSettings);
            PanelSettings quick = PanelSettings.Load(Paths.QuickSettings, "quick");
            TaskbarContrastSettings taskbar = TaskbarContrastSettings.Load();
            if (start.Enabled) processes.Add("StartMenuExperienceHost.exe");
            if (quick.Enabled) processes.Add("ShellHost.exe");
            if (taskbar.Enabled) processes.Add("explorer.exe");
            // The global exclusion remains "*". Only these exact shell hosts
            // are admitted; ACE, games, svchost and other processes remain
            // outside the injection boundary.
            string desired = String.Join("|", processes.Distinct(StringComparer.OrdinalIgnoreCase));
            bool changed = true;
            using (var read = Registry.LocalMachine.OpenSubKey(Paths.EngineSettingsRegistry))
            {
                if (read != null)
                {
                    string current = read.GetValue("Include", "") as string ?? "";
                    changed = !String.Equals(current, desired, StringComparison.OrdinalIgnoreCase);
                }
            }
            using (var key = Registry.LocalMachine.CreateSubKey(Paths.EngineSettingsRegistry, true))
            {
                key.SetValue("Include", desired, RegistryValueKind.String);
                key.SetValue("Exclude", "*", RegistryValueKind.String);
                key.SetValue("InjectIntoCriticalProcesses", 0, RegistryValueKind.DWord);
                key.SetValue("InjectIntoIncompatiblePrograms", 0, RegistryValueKind.DWord);
                key.SetValue("InjectIntoGames", 0, RegistryValueKind.DWord);
            }
            return changed;
        }

        private static void RestartWindhawkService()
        {
            // Windhawk's own restart entry point also rebuilds the per-user daemon and
            // global injection session. Restarting only the Windows service can leave
            // the old session metadata behind, causing a newly created SearchHost to
            // run without windhawk.dll even though the service reports "Running".
            string windhawkExe = GetWindhawkServiceExecutable();
            if (!String.IsNullOrEmpty(windhawkExe))
            {
                var psi = new ProcessStartInfo(windhawkExe, "-restart-bg -timeout 30000")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (var process = Process.Start(psi))
                {
                    if (process == null || !process.WaitForExit(40000))
                        throw new System.TimeoutException("Windhawk 官方后台重启超时。");
                    if (process.ExitCode != 0)
                        throw new InvalidOperationException("Windhawk 官方后台重启失败，退出代码：" + process.ExitCode);
                }
                using (var service = new ServiceController("Windhawk"))
                {
                    service.Refresh();
                    if (service.Status != ServiceControllerStatus.Running)
                        service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(20));
                }
                return;
            }

            // Compatibility fallback for installations whose service ImagePath can't
            // be resolved. This path is only used when the official executable is absent.
            using (var service = new ServiceController("Windhawk"))
            {
                service.Refresh();
                if (service.Status != ServiceControllerStatus.Stopped && service.Status != ServiceControllerStatus.StopPending)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20));
                }
                else if (service.Status == ServiceControllerStatus.StopPending)
                {
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20));
                }
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(20));
            }
        }

        private static string GetWindhawkServiceExecutable()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Windhawk"))
                {
                    string imagePath = key == null ? "" : key.GetValue("ImagePath", "") as string;
                    imagePath = Environment.ExpandEnvironmentVariables((imagePath ?? "").Trim());
                    string candidate = "";
                    if (imagePath.StartsWith("\"", StringComparison.Ordinal))
                    {
                        int end = imagePath.IndexOf('"', 1);
                        if (end > 1) candidate = imagePath.Substring(1, end - 1);
                    }
                    else
                    {
                        int end = imagePath.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                        if (end >= 0) candidate = imagePath.Substring(0, end + 4).Trim();
                    }
                    return File.Exists(candidate) ? candidate : "";
                }
            }
            catch { return ""; }
        }

        private static void EnsureWindhawkServiceRunning()
        {
            try
            {
                using (var service = new ServiceController("Windhawk"))
                {
                    service.Refresh();
                    if (service.Status == ServiceControllerStatus.Running) return;
                    if (service.Status == ServiceControllerStatus.StartPending)
                    {
                        service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
                        return;
                    }
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
                }
            }
            catch (Exception ex)
            {
                int startMode = -1;
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Windhawk"))
                        if (key != null) startMode = Convert.ToInt32(key.GetValue("Start", -1));
                }
                catch { }
                if (startMode == 4)
                    throw new InvalidOperationException("运行组件服务已被系统禁用，因此透明/模糊设置无法载入。请先恢复 Windhawk 服务后再应用。", ex);
                throw new InvalidOperationException("运行组件服务没有启动，设置暂时无法载入。请确认 Windhawk 服务可用后重试。", ex);
            }
        }

        private static bool WaitForModLoaded(string moduleName, string[] processNames, int seconds)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(seconds);
            do
            {
                if (IsModuleLoaded(moduleName, processNames)) return true;
                Thread.Sleep(500);
            } while (DateTime.UtcNow < deadline);
            return false;
        }

        private static bool IsModuleLoaded(string moduleName)
        {
            return IsModuleLoaded(moduleName, new[] { "StartMenuExperienceHost", "ShellHost" });
        }

        private static bool IsModuleLoaded(string moduleName, string[] processNames)
        {
            foreach (string processName in processNames)
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    try
                    {
                        foreach (ProcessModule module in process.Modules)
                            if (String.Equals(module.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase)) return true;
                    }
                    catch { }
                    finally { process.Dispose(); }
                }
            }
            return false;
        }

        private static string BuildDiagnosticReport()
        {
            int disabled = -1;
            string library = "";
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(Paths.ModRegistry))
                {
                    if (key != null)
                    {
                        disabled = Convert.ToInt32(key.GetValue("Disabled", -1));
                        library = key.GetValue("LibraryFileName", "") as string;
                    }
                }
            }
            catch { }
            return String.Format("时间：{0}\r\nWindhawk 服务：{1}\r\nwindhawk.dll：{2}\r\n模组 DLL：{3}\r\nDisabled：{4}\r\nLibraryFileName：{5}\r\n模组文件存在：{6}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), IsWindhawkServiceRunning(), IsModuleLoaded("windhawk.dll"),
                IsModuleLoaded(Paths.ModDll), disabled, library, File.Exists(Paths.WindhawkModule));
        }
    }

    internal static class ImageProcessor
    {
        internal static void CreateStartBackground(string source, string output, int blur, string fit)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using (var stream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var loaded = Image.FromStream(stream))
            using (var canvas = new Bitmap(1400, 1450, PixelFormat.Format32bppPArgb))
            {
                using (var g = Graphics.FromImage(canvas))
                {
                    g.Clear(Color.Black);
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    DrawFit(g, loaded, new Rectangle(0, 0, canvas.Width, canvas.Height), fit);
                }
                Bitmap final = null;
                try
                {
                    final = blur <= 0 ? new Bitmap(canvas) : SoftBlur(canvas, blur);
                    string temp = output + ".new";
                    if (File.Exists(temp)) File.Delete(temp);
                    final.Save(temp, ImageFormat.Png);
                    if (File.Exists(output)) File.Delete(output);
                    File.Move(temp, output);
                }
                finally { if (final != null) final.Dispose(); }
            }
        }

        internal static Bitmap SoftBlur(Bitmap src, int strength)
        {
            int divisor = Math.Max(1, 1 + strength / 3);
            int w = Math.Max(70, src.Width / divisor), h = Math.Max(70, src.Height / divisor);
            using (var small = new Bitmap(w, h, PixelFormat.Format32bppPArgb))
            {
                using (var g = Graphics.FromImage(small))
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(src, new Rectangle(0, 0, w, h));
                }
                var result = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppPArgb);
                using (var g = Graphics.FromImage(result))
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(small, new Rectangle(0, 0, result.Width, result.Height));
                }
                return result;
            }
        }

        internal static void DrawFit(Graphics g, Image image, Rectangle box, string fit)
        {
            if (fit == "Fill") { g.DrawImage(image, box); return; }
            double sx = (double)box.Width / image.Width, sy = (double)box.Height / image.Height;
            double scale = fit == "Uniform" ? Math.Min(sx, sy) : Math.Max(sx, sy);
            int w = (int)Math.Ceiling(image.Width * scale), h = (int)Math.Ceiling(image.Height * scale);
            var dest = new Rectangle(box.X + (box.Width - w) / 2, box.Y + (box.Height - h) / 2, w, h);
            g.DrawImage(image, dest);
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly DesignSettings settings;
        private readonly PreviewPanel preview = new PreviewPanel();
        private readonly ComboBox mode = new ComboBox();
        private readonly ComboBox fit = new ComboBox();
        private readonly ComboBox layout = new ComboBox();
        private readonly TextBox imagePath = new TextBox();
        private readonly TrackBar blur = new TrackBar();
        private readonly TrackBar tintOpacity = new TrackBar();
        private readonly TrackBar radius = new TrackBar();
        private readonly Label blurValue = ValueLabel();
        private readonly Label tintValue = ValueLabel();
        private readonly Label radiusValue = ValueLabel();
        private readonly Button tintButton = new Button();
        private readonly Label status = new Label();
        private bool initializing;

        internal MainForm()
        {
            settings = DesignSettings.Load(Paths.UserSettings);
            Text = "原生开始菜单定制器";
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(245, 247, 251);
            ForeColor = Color.FromArgb(27, 32, 43);
            MinimumSize = new Size(960, 740);
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1080, 790);
            BuildUi();
            Populate();
            Activated += delegate { status.Text = Worker.DiagnosticText(); };
        }

        private void BuildUi()
        {
            var title = new Label { Text = "原生开始菜单定制器", Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold), AutoSize = true, Location = new Point(34, 25) };
            var subtitle = new Label { Text = "定制 Windows 原生开始菜单与快捷设置；透明任务栏支持安全自动对比，搜索面板保持 Windows 原生", ForeColor = Color.FromArgb(92, 100, 116), AutoSize = true, Location = new Point(38, 67) };
            Controls.Add(title); Controls.Add(subtitle);

            var taskbarModule = FlatButton("任务栏自动对比…", Color.FromArgb(235, 240, 248), Color.FromArgb(31, 42, 61));
            taskbarModule.Location = new Point(746, 31); taskbarModule.Size = new Size(156, 34);
            taskbarModule.Click += delegate { using (var form = new TaskbarContrastForm()) form.ShowDialog(this); };
            Controls.Add(taskbarModule);

            var quickModule = FlatButton("快捷设置…", Color.FromArgb(235, 240, 248), Color.FromArgb(31, 42, 61));
            quickModule.Location = new Point(910, 31); quickModule.Size = new Size(112, 34);
            quickModule.Click += delegate { using (var form = new PanelCustomizerForm("quick")) form.ShowDialog(this); };
            Controls.Add(quickModule);

            status.AutoSize = false; status.Location = new Point(39, 98); status.Size = new Size(995, 36);
            status.BackColor = Color.FromArgb(232, 242, 255); status.ForeColor = Color.FromArgb(25, 91, 159);
            status.Padding = new Padding(12, 9, 8, 7); status.Text = Worker.DiagnosticText();
            Controls.Add(status);

            preview.Location = new Point(39, 153); preview.Size = new Size(474, 570); preview.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
            Controls.Add(preview);

            var panel = new Panel
            {
                Location = new Point(541, 151),
                Size = new Size(493, 572),
                Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoScroll = true,
                TabStop = true,
                BackColor = Color.White
            };
            // Keep the settings as a real vertical list. The inner panel is
            // intentionally taller than the viewport so Windows always gives
            // it a usable vertical scrollbar, including at 125%/150% DPI.
            var list = new Panel { Location = Point.Empty, Size = new Size(465, 850), BackColor = Color.White };
            panel.Controls.Add(list);
            panel.AutoScrollMinSize = new Size(0, list.Height);
            panel.MouseEnter += delegate { panel.Focus(); };
            Controls.Add(panel);
            int y = 20;
            AddLabel(list, "背景模式", 22, y); y += 28;
            mode.DropDownStyle = ComboBoxStyle.DropDownList; mode.Items.AddRange(new object[] { "自选图片", "实时桌面模糊", "纯色背景" }); mode.Location = new Point(22, y); mode.Width = 425;
            mode.SelectedIndexChanged += delegate { if (!initializing) ModeChanged(); }; list.Controls.Add(mode); y += 52;

            AddLabel(list, "背景图片", 22, y); y += 27;
            imagePath.Location = new Point(22, y); imagePath.Width = 310; imagePath.ReadOnly = true; list.Controls.Add(imagePath);
            var choose = FlatButton("选择图片…", Color.FromArgb(235, 240, 248), Color.FromArgb(31, 42, 61)); choose.Location = new Point(340, y - 1); choose.Size = new Size(107, 29); choose.Click += ChooseImage; list.Controls.Add(choose); y += 43;
            fit.DropDownStyle = ComboBoxStyle.DropDownList; fit.Items.AddRange(new object[] { "图片：裁切铺满", "图片：完整显示", "图片：拉伸铺满" }); fit.Location = new Point(22, y); fit.Width = 425; fit.SelectedIndexChanged += delegate { if (!initializing) SyncSettings(); }; list.Controls.Add(fit); y += 43;
            layout.DropDownStyle = ComboBoxStyle.DropDownList; layout.Items.AddRange(new object[] { "布局：保持原生", "布局：新版左右并排" }); layout.Location = new Point(22, y); layout.Width = 425; layout.SelectedIndexChanged += delegate { if (!initializing) SyncSettings(); }; list.Controls.Add(layout); y += 54;

            AddSlider(list, "模糊度", blur, blurValue, 0, 50, y, delegate { if (initializing) return; settings.Blur = blur.Value; blurValue.Text = blur.Value.ToString(); RefreshPreview(); }); y += 90;
            AddSlider(list, "颜色覆盖", tintOpacity, tintValue, 0, 90, y, delegate { if (initializing) return; settings.TintOpacity = tintOpacity.Value; tintValue.Text = tintOpacity.Value + "%"; RefreshPreview(); }); y += 90;
            AddSlider(list, "圆角", radius, radiusValue, 0, 32, y, delegate { if (initializing) return; settings.CornerRadius = radius.Value; radiusValue.Text = radius.Value.ToString(); RefreshPreview(); }); y += 90;

            AddLabel(list, "颜色与更多设置", 22, y); y += 29;
            var advanced = FlatButton("字体、布局与尺寸…", Color.FromArgb(235, 240, 248), Color.FromArgb(31, 42, 61)); advanced.Location = new Point(22, y); advanced.Size = new Size(310, 34); advanced.Click += OpenAdvanced; list.Controls.Add(advanced);
            tintButton.Text = "选择颜色"; tintButton.Location = new Point(340, y); tintButton.Size = new Size(107, 34); tintButton.FlatStyle = FlatStyle.Flat; tintButton.FlatAppearance.BorderColor = Color.FromArgb(210, 216, 226); tintButton.Click += ChooseTint; list.Controls.Add(tintButton); y += 50;

            var apply = FlatButton("应用到原生开始菜单", Color.FromArgb(42, 111, 232), Color.White); apply.Font = new Font(Font, FontStyle.Bold); apply.Location = new Point(22, y); apply.Size = new Size(425, 44); apply.Click += ApplyClicked; list.Controls.Add(apply); y += 52;
            var restore = FlatButton("恢复 Windows 原生样式", Color.FromArgb(238, 241, 247), Color.FromArgb(45, 53, 68)); restore.Location = new Point(22, y); restore.Size = new Size(425, 40); restore.Click += RestoreClicked; list.Controls.Add(restore); y += 52;

            var note = new Label { Text = "应用会请求管理员确认；配置器关闭后占用 0 MB。\r\n滚动鼠标滚轮可以查看全部设置。", AutoSize = false, Size = new Size(425, 48), ForeColor = Color.FromArgb(105, 113, 128), Location = new Point(22, y) };
            list.Controls.Add(note);
        }

        private static Label ValueLabel() { return new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleRight, ForeColor = Color.FromArgb(82, 91, 108) }; }
        private static void AddLabel(Control parent, string text, int x, int y) { parent.Controls.Add(new Label { Text = text, AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold), Location = new Point(x, y) }); }
        private static Button FlatButton(string text, Color back, Color fore)
        {
            return new Button { Text = text, BackColor = back, ForeColor = fore, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        }
        private static void AddSlider(Control parent, string text, TrackBar slider, Label value, int min, int max, int y, EventHandler changed)
        {
            AddLabel(parent, text, 22, y);
            value.Location = new Point(405, y - 2); value.Size = new Size(57, 24); parent.Controls.Add(value);
            slider.Minimum = min; slider.Maximum = max; slider.TickStyle = TickStyle.None;
            slider.AutoSize = false; slider.Location = new Point(18, y + 29); slider.Size = new Size(429, 34);
            slider.ValueChanged += changed; parent.Controls.Add(slider);
        }

        private void Populate()
        {
            initializing = true;
            string wallpaper = "";
            try { using (var k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop")) wallpaper = k.GetValue("WallPaper", "") as string; } catch { }
            if (String.IsNullOrEmpty(settings.ImagePath) && !String.IsNullOrEmpty(wallpaper) && File.Exists(wallpaper)) settings.ImagePath = wallpaper;
            mode.SelectedIndex = settings.Mode == "blur" ? 1 : settings.Mode == "color" ? 2 : 0;
            imagePath.Text = settings.ImagePath;
            blur.Value = Math.Max(blur.Minimum, Math.Min(blur.Maximum, settings.Blur));
            tintOpacity.Value = Math.Max(tintOpacity.Minimum, Math.Min(tintOpacity.Maximum, settings.TintOpacity));
            radius.Value = Math.Max(radius.Minimum, Math.Min(radius.Maximum, settings.CornerRadius));
            fit.SelectedIndex = settings.Fit == "Uniform" ? 1 : settings.Fit == "Fill" ? 2 : 0;
            layout.SelectedIndex = settings.Layout == "newLayoutSideBySide" ? 1 : 0;
            tintButton.BackColor = ColorTools.EffectiveTint(settings);
            blurValue.Text = settings.Blur.ToString(); tintValue.Text = settings.TintOpacity + "%"; radiusValue.Text = settings.CornerRadius.ToString();
            initializing = false;
            RefreshPreview();
        }

        private void ModeChanged()
        {
            settings.Mode = mode.SelectedIndex == 1 ? "blur" : mode.SelectedIndex == 2 ? "color" : "image";
            bool image = settings.Mode == "image";
            imagePath.Enabled = image; fit.Enabled = image;
            RefreshPreview();
        }

        private void SyncSettings()
        {
            settings.Fit = fit.SelectedIndex == 1 ? "Uniform" : fit.SelectedIndex == 2 ? "Fill" : "UniformToFill";
            settings.Layout = layout.SelectedIndex == 1 ? "newLayoutSideBySide" : "default";
            RefreshPreview();
        }

        private void ChooseImage(object sender, EventArgs e)
        {
            using (var d = new OpenFileDialog { Title = "选择开始菜单背景图片", Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp|所有文件|*.*" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                settings.ImagePath = d.FileName; imagePath.Text = d.FileName; mode.SelectedIndex = 0; RefreshPreview();
            }
        }

        private void ChooseTint(object sender, EventArgs e)
        {
            using (var d = new ColorDialog { Color = settings.Tint, FullOpen = true })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                settings.Tint = d.Color; settings.Tone = "custom"; tintButton.BackColor = d.Color; RefreshPreview();
            }
        }

        private void OpenAdvanced(object sender, EventArgs e)
        {
            using (var form = new AdvancedForm(settings))
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;
                tintButton.BackColor = ColorTools.EffectiveTint(settings);
                RefreshPreview();
            }
        }

        private void ApplyClicked(object sender, EventArgs e)
        {
            SyncSettings(); settings.Save(Paths.UserSettings);
            if (settings.Mode == "image" && (!File.Exists(settings.ImagePath)))
            {
                status.Text = "请先选择一张有效的背景图片。"; return;
            }
            string blockReason;
            if (Worker.TryGetApplyBlockReason(out blockReason))
            {
                status.Text = blockReason;
                return;
            }
            StartApplyWorker();
        }

        private void StartApplyWorker()
        {
            try
            {
                var info = new ProcessStartInfo(Application.ExecutablePath, "--apply \"" + Paths.UserSettings + "\"");
                if (Worker.IsAdministrator())
                {
                    info.UseShellExecute = false; info.CreateNoWindow = true; info.WindowStyle = ProcessWindowStyle.Hidden;
                }
                else
                {
                    info.UseShellExecute = true; info.Verb = "runas"; info.WindowStyle = ProcessWindowStyle.Hidden;
                }
                var process = Process.Start(info);
                status.Text = "正在后台应用，请稍候…";
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try { process.WaitForExit(); }
                    catch { }
                    finally { process.Dispose(); }
                    try
                    {
                        if (!IsDisposed && IsHandleCreated)
                            BeginInvoke((MethodInvoker)delegate { status.Text = Worker.DiagnosticText(); });
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                status.Text = "无法启动后台应用：" + ex.Message;
            }
        }

        private void RestoreClicked(object sender, EventArgs e)
        {
            RelaunchElevated("--restore");
        }

        private bool RelaunchElevated(string args)
        {
            try
            {
                using (var process = Process.Start(new ProcessStartInfo(Application.ExecutablePath, args) { UseShellExecute = true, Verb = "runas" })) { }
                return true;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                status.Text = ex.NativeErrorCode == 1223 ? "已取消管理员确认，设置没有更改。" : "无法启动应用组件：" + ex.Message;
                return false;
            }
        }

        private void RefreshPreview()
        {
            preview.Settings = settings;
            // Blur and solid-color previews don't use the selected wallpaper.
            // Releasing it here avoids keeping a needless bitmap/GDI object.
            preview.LoadImage(settings.Mode == "image" ? settings.ImagePath : null);
            preview.Invalidate();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { SyncSettings(); settings.Save(Paths.UserSettings); } catch { }
            base.OnFormClosing(e);
        }
    }

    internal sealed class TaskbarContrastForm : Form
    {
        private readonly Label status = new Label();
        private readonly Button enable = new Button();
        private readonly Button disable = new Button();

        internal TaskbarContrastForm()
        {
            Text = "透明任务栏自动对比";
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(245, 247, 251);
            ForeColor = Color.FromArgb(27, 32, 43);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(610, 445);

            Controls.Add(new Label
            {
                Text = "透明任务栏自动对比",
                Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(28, 24)
            });
            Controls.Add(new Label
            {
                Text = "背景继续完全透明，只按任务栏旁边的一小条画面自动切换黑/白前景。\r\n不改 Windows 深浅色模式，也不修改 TranslucentTB 的配置。",
                AutoSize = false,
                Location = new Point(31, 72),
                Size = new Size(548, 44),
                ForeColor = Color.FromArgb(83, 92, 108)
            });

            var facts = new Label
            {
                Text = "• 直接采样右侧托盘背后的物理 96×12 像素，不缩放整屏\r\n" +
                       "• 连续两次确认后切换，约 0.5 秒内更新黑/白前景\r\n" +
                       "• 仅在 Explorer 中加载固定样式；不进入 ACE、游戏或系统服务\r\n" +
                       "• 变色只更新画刷，不卸载或重建任务栏 XAML\r\n" +
                       "• 带 30 秒崩溃熔断；异常重启时自动退回原生前景色",
                AutoSize = false,
                Location = new Point(31, 120),
                Size = new Size(548, 154),
                ForeColor = Color.FromArgb(48, 57, 73)
            };
            Controls.Add(facts);

            status.Location = new Point(30, 302); status.Size = new Size(550, 46);
            status.BackColor = Color.FromArgb(232, 242, 255); status.ForeColor = Color.FromArgb(25, 91, 159);
            status.Padding = new Padding(10, 9, 8, 6); status.Text = Worker.TaskbarContrastStatus();
            Controls.Add(status);

            enable.Text = "启用自动对比"; enable.Location = new Point(30, 370); enable.Size = new Size(270, 42);
            enable.BackColor = Color.FromArgb(42, 111, 232); enable.ForeColor = Color.White; enable.FlatStyle = FlatStyle.Flat;
            enable.Click += delegate { StartChange(true); }; Controls.Add(enable);

            disable.Text = "恢复 Windows 原生前景色"; disable.Location = new Point(310, 370); disable.Size = new Size(270, 42);
            disable.BackColor = Color.FromArgb(235, 240, 248); disable.ForeColor = Color.FromArgb(31, 42, 61); disable.FlatStyle = FlatStyle.Flat;
            disable.Click += delegate { StartChange(false); }; Controls.Add(disable);
        }

        private void StartChange(bool enabled)
        {
            var local = TaskbarContrastSettings.Load();
            local.Enabled = enabled;
            local.Save();
            if (!enabled)
            {
                TaskbarContrastRuntime.ConfigureStartup(false);
                TaskbarContrastRuntime.StopWatcher();
            }

            Process process = null;
            try
            {
                bool administrator = Worker.IsAdministrator();
                string command = enabled
                    ? (administrator ? "--apply-taskbar-contrast" : "--activate-taskbar-compat")
                    : "--restore-taskbar-contrast";
                var info = new ProcessStartInfo(Application.ExecutablePath, command);
                if (administrator || enabled)
                {
                    info.UseShellExecute = false; info.CreateNoWindow = true; info.WindowStyle = ProcessWindowStyle.Hidden;
                }
                else
                {
                    info.UseShellExecute = true; info.Verb = "runas"; info.WindowStyle = ProcessWindowStyle.Hidden;
                }
                process = Process.Start(info);
                enable.Enabled = disable.Enabled = false;
                status.Text = enabled ? "正在启用任务栏自动对比…" : "正在恢复任务栏原生前景色…";
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try { if (process != null) process.WaitForExit(); }
                    catch { }
                    finally { if (process != null) process.Dispose(); }
                    bool failed = false;
                    try
                    {
                        if (File.Exists(Paths.OperationStatus))
                        {
                            string text = File.ReadAllText(Paths.OperationStatus, Encoding.UTF8);
                            failed = text.StartsWith("任务栏自动对比失败", StringComparison.Ordinal) ||
                                text.StartsWith("任务栏自动对比恢复失败", StringComparison.Ordinal) ||
                                text.StartsWith("任务栏无弹窗安全升级失败", StringComparison.Ordinal);
                        }
                    }
                    catch { }
                    if (failed && enabled)
                    {
                        try
                        {
                            var rollback = TaskbarContrastSettings.Load();
                            rollback.Enabled = false;
                            rollback.Save();
                            TaskbarContrastRuntime.ConfigureStartup(false);
                            TaskbarContrastRuntime.StopWatcher();
                        }
                        catch { }
                    }
                    try
                    {
                        if (!IsDisposed && IsHandleCreated) BeginInvoke((MethodInvoker)delegate
                        {
                            enable.Enabled = disable.Enabled = true;
                            status.Text = failed ? ReadOperationStatus() : Worker.TaskbarContrastStatus();
                        });
                    }
                    catch { }
                });
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                local.Enabled = !enabled;
                local.Save();
                status.Text = "无法启动安全组件：" + ex.Message;
            }
            catch (Exception ex)
            {
                status.Text = "无法应用：" + ex.Message;
            }
        }

        private static string ReadOperationStatus()
        {
            try { return File.ReadAllText(Paths.OperationStatus, Encoding.UTF8).Trim(); }
            catch { return "操作失败，请重试。"; }
        }
    }

    internal sealed class PanelCustomizerForm : Form
    {
        private readonly string kind;
        private readonly string settingsFile;
        private readonly PanelSettings settings;
        private readonly ComboBox mode = new ComboBox();
        private readonly ComboBox fit = new ComboBox();
        private readonly ComboBox textContrast = new ComboBox();
        private readonly TextBox imagePath = new TextBox();
        private readonly TrackBar blur = new TrackBar();
        private readonly TrackBar opacity = new TrackBar();
        private readonly TrackBar radius = new TrackBar();
        private readonly Label blurValue = new Label();
        private readonly Label opacityValue = new Label();
        private readonly Label radiusValue = new Label();
        private readonly Button tintButton = new Button();
        private readonly NumericUpDown width = new NumericUpDown();
        private readonly NumericUpDown height = new NumericUpDown();
        private readonly CheckBox separateMedia = new CheckBox();
        private readonly Button mediaTintButton = new Button();
        private readonly TrackBar mediaOpacity = new TrackBar();
        private readonly TrackBar mediaRadius = new TrackBar();
        private readonly Label mediaOpacityValue = new Label();
        private readonly Label mediaRadiusValue = new Label();
        private readonly CheckBox hideSearchAds = new CheckBox();
        private readonly Label status = new Label();
        private bool initializing;

        internal PanelCustomizerForm(string kind)
        {
            this.kind = kind;
            settingsFile = kind == "search" ? Paths.SearchSettings : Paths.QuickSettings;
            settings = PanelSettings.Load(settingsFile, kind);
            Text = kind == "search" ? "原生搜索面板定制" : "原生快捷设置定制";
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(245, 247, 251);
            ForeColor = Color.FromArgb(27, 32, 43);
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(720, 650);
            Size = new Size(760, 760);
            BuildUi();
            Populate();
        }

        private void BuildUi()
        {
            string display = kind == "search" ? "Windows 原生搜索面板" : "Windows 原生快捷设置";
            Controls.Add(new Label { Text = display, Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold), AutoSize = true, Location = new Point(30, 22) });
            Controls.Add(new Label { Text = "只在启用后载入对应的 Windows 用户界面宿主，可随时单独恢复。", AutoSize = true, ForeColor = Color.FromArgb(92, 100, 116), Location = new Point(34, 61) });
            status.Location = new Point(30, 90); status.Size = new Size(680, 34); status.Padding = new Padding(10, 8, 6, 6);
            status.BackColor = Color.FromArgb(232, 242, 255); status.ForeColor = Color.FromArgb(25, 91, 159);
            status.Text = settings.Enabled ? "当前：已启用" : "当前：未启用（保持 Windows 原生样式）";
            Controls.Add(status);

            var footer = new Panel { Location = new Point(30, ClientSize.Height - 68), Size = new Size(ClientSize.Width - 60, 54), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom, BackColor = Color.White };
            var viewport = new Panel { Location = new Point(30, 140), Size = new Size(680, footer.Top - 150), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, AutoScroll = true, BackColor = Color.White };
            var list = new Panel { Location = Point.Empty, Size = new Size(650, 720), BackColor = Color.White };
            viewport.Controls.Add(list); viewport.MouseEnter += delegate { viewport.Focus(); };
            Controls.Add(viewport);
            Controls.Add(footer);

            int y = 20;
            AddHeading(list, "背景模式", y); y += 29;
            mode.DropDownStyle = ComboBoxStyle.DropDownList; mode.Items.AddRange(new object[] { "自选图片", "实时桌面模糊", "纯色背景" });
            mode.Location = new Point(22, y); mode.Width = 600; mode.SelectedIndexChanged += delegate { if (!initializing) ModeChanged(); }; list.Controls.Add(mode); y += 48;

            AddHeading(list, "背景图片", y); y += 28;
            imagePath.Location = new Point(22, y); imagePath.Width = 465; imagePath.ReadOnly = true; list.Controls.Add(imagePath);
            var choose = ButtonOf("选择图片…", Color.FromArgb(235, 240, 248), Color.FromArgb(31, 42, 61)); choose.Location = new Point(497, y - 1); choose.Size = new Size(125, 30); choose.Click += ChooseImage; list.Controls.Add(choose); y += 42;
            fit.DropDownStyle = ComboBoxStyle.DropDownList; fit.Items.AddRange(new object[] { "裁切铺满", "完整显示", "拉伸铺满" }); fit.Location = new Point(22, y); fit.Width = 600; list.Controls.Add(fit); y += 52;

            AddSlider(list, "模糊度", blur, blurValue, 0, 50, y); y += 80;
            AddSlider(list, "颜色覆盖", opacity, opacityValue, 0, 90, y); y += 80;
            AddSlider(list, "圆角", radius, radiusValue, 0, 32, y); y += 80;

            AddHeading(list, "背景颜色", y); tintButton.Text = "选择颜色"; tintButton.Location = new Point(497, y - 6); tintButton.Size = new Size(125, 32); tintButton.FlatStyle = FlatStyle.Flat; tintButton.Click += ChooseTint; list.Controls.Add(tintButton); y += 47;

            AddHeading(list, "面板尺寸", y); y += 28;
            if (kind == "search")
            {
                list.Controls.Add(new Label { Text = "宽", AutoSize = true, Location = new Point(22, y + 5) });
                width.Location = new Point(58, y); width.Width = 150; width.Increment = 10; width.Minimum = 760; width.Maximum = 1500; list.Controls.Add(width);
                list.Controls.Add(new Label { Text = "高", AutoSize = true, Location = new Point(250, y + 5) });
                height.Location = new Point(286, y); height.Width = 150; height.Increment = 10; height.Minimum = 560; height.Maximum = 1100; list.Controls.Add(height);
            }
            else
                list.Controls.Add(new Label { Text = "跟随 Windows 原生布局（防止按钮裁切和错位）", AutoSize = true, ForeColor = Color.FromArgb(92, 100, 116), Location = new Point(22, y + 4) });
            y += 49;

            AddHeading(list, "字体对比度", y); y += 28;
            textContrast.DropDownStyle = ComboBoxStyle.DropDownList; textContrast.Items.AddRange(new object[] { "自动识别背景", "强制白色字体", "强制黑色字体" }); textContrast.Location = new Point(22, y); textContrast.Width = 600; list.Controls.Add(textContrast); y += 52;

            if (kind == "search")
            {
                hideSearchAds.Text = "隐藏广告、热搜和游戏推荐（保留本地搜索结果）";
                hideSearchAds.AutoSize = true; hideSearchAds.Location = new Point(22, y); list.Controls.Add(hideSearchAds); y += 43;
            }

            if (kind == "quick")
            {
                separateMedia.Text = "媒体卡片单独设置颜色和圆角"; separateMedia.AutoSize = true; separateMedia.Location = new Point(22, y); separateMedia.CheckedChanged += delegate { UpdateMediaControls(); }; list.Controls.Add(separateMedia); y += 42;
                AddHeading(list, "媒体卡片颜色", y); mediaTintButton.Text = "选择颜色"; mediaTintButton.Location = new Point(497, y - 6); mediaTintButton.Size = new Size(125, 32); mediaTintButton.FlatStyle = FlatStyle.Flat; mediaTintButton.Click += ChooseMediaTint; list.Controls.Add(mediaTintButton); y += 50;
                AddSlider(list, "媒体颜色覆盖", mediaOpacity, mediaOpacityValue, 0, 90, y); y += 80;
                AddSlider(list, "媒体卡片圆角", mediaRadius, mediaRadiusValue, 0, 32, y); y += 80;
            }

            list.Height = y + 24;
            viewport.AutoScrollMinSize = new Size(0, list.Height);

            var apply = ButtonOf("应用到" + display, Color.FromArgb(42, 111, 232), Color.White); apply.Font = new Font(Font, FontStyle.Bold); apply.Location = new Point(10, 7); apply.Size = new Size(430, 40); apply.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top; apply.Click += ApplyClicked; footer.Controls.Add(apply);
            var restore = ButtonOf("仅恢复这个面板", Color.FromArgb(238, 241, 247), Color.FromArgb(45, 53, 68)); restore.Location = new Point(450, 7); restore.Size = new Size(190, 40); restore.Anchor = AnchorStyles.Right | AnchorStyles.Top; restore.Click += RestoreClicked; footer.Controls.Add(restore);
        }

        private static void AddHeading(Control parent, string text, int y) { parent.Controls.Add(new Label { Text = text, AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold), Location = new Point(22, y) }); }
        private static Button ButtonOf(string text, Color back, Color fore) { return new Button { Text = text, BackColor = back, ForeColor = fore, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand }; }
        private static void AddSlider(Control parent, string title, TrackBar slider, Label value, int min, int max, int y)
        {
            AddHeading(parent, title, y); value.Location = new Point(555, y - 2); value.Size = new Size(67, 24); value.TextAlign = ContentAlignment.MiddleRight; parent.Controls.Add(value);
            slider.Minimum = min; slider.Maximum = max; slider.TickStyle = TickStyle.None; slider.AutoSize = false; slider.Location = new Point(18, y + 27); slider.Size = new Size(604, 34); parent.Controls.Add(slider);
        }

        private void Populate()
        {
            initializing = true;
            mode.SelectedIndex = settings.Mode == "blur" ? 1 : settings.Mode == "color" ? 2 : 0;
            fit.SelectedIndex = settings.Fit == "Uniform" ? 1 : settings.Fit == "Fill" ? 2 : 0;
            textContrast.SelectedIndex = settings.TextContrast == "white" ? 1 : settings.TextContrast == "black" ? 2 : 0;
            imagePath.Text = settings.ImagePath;
            blur.Value = settings.Blur; opacity.Value = settings.TintOpacity; radius.Value = settings.CornerRadius;
            blurValue.Text = settings.Blur.ToString(); opacityValue.Text = settings.TintOpacity + "%"; radiusValue.Text = settings.CornerRadius.ToString();
            tintButton.BackColor = settings.Tint;
            if (kind == "search")
            {
                width.Value = Math.Max((int)width.Minimum, Math.Min((int)width.Maximum, settings.Width));
                height.Value = Math.Max((int)height.Minimum, Math.Min((int)height.Maximum, settings.Height));
            }
            hideSearchAds.Checked = settings.HideSearchAds;
            if (kind == "quick")
            {
                separateMedia.Checked = settings.SeparateMedia; mediaTintButton.BackColor = settings.MediaTint;
                mediaOpacity.Value = settings.MediaOpacity; mediaRadius.Value = settings.MediaRadius;
                mediaOpacityValue.Text = settings.MediaOpacity + "%"; mediaRadiusValue.Text = settings.MediaRadius.ToString();
            }
            blur.ValueChanged += ValuesChanged; opacity.ValueChanged += ValuesChanged; radius.ValueChanged += ValuesChanged;
            mediaOpacity.ValueChanged += ValuesChanged; mediaRadius.ValueChanged += ValuesChanged;
            initializing = false; ModeChanged(); UpdateMediaControls();
        }

        private void ValuesChanged(object sender, EventArgs e)
        {
            blurValue.Text = blur.Value.ToString(); opacityValue.Text = opacity.Value + "%"; radiusValue.Text = radius.Value.ToString();
            mediaOpacityValue.Text = mediaOpacity.Value + "%"; mediaRadiusValue.Text = mediaRadius.Value.ToString();
        }

        private void ModeChanged() { bool image = mode.SelectedIndex == 0; imagePath.Enabled = image; fit.Enabled = image; }
        private void UpdateMediaControls() { bool enabled = kind == "quick" && separateMedia.Checked; mediaTintButton.Enabled = enabled; mediaOpacity.Enabled = enabled; mediaRadius.Enabled = enabled; }

        private void ChooseImage(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog { Title = "选择原生面板背景图片", Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp|所有文件|*.*" })
                if (dialog.ShowDialog(this) == DialogResult.OK) { imagePath.Text = dialog.FileName; mode.SelectedIndex = 0; }
        }
        private void ChooseTint(object sender, EventArgs e) { using (var d = new ColorDialog { Color = tintButton.BackColor, FullOpen = true }) if (d.ShowDialog(this) == DialogResult.OK) tintButton.BackColor = d.Color; }
        private void ChooseMediaTint(object sender, EventArgs e) { using (var d = new ColorDialog { Color = mediaTintButton.BackColor, FullOpen = true }) if (d.ShowDialog(this) == DialogResult.OK) mediaTintButton.BackColor = d.Color; }

        private void Sync()
        {
            settings.Mode = mode.SelectedIndex == 1 ? "blur" : mode.SelectedIndex == 2 ? "color" : "image";
            settings.ImagePath = imagePath.Text; settings.Fit = fit.SelectedIndex == 1 ? "Uniform" : fit.SelectedIndex == 2 ? "Fill" : "UniformToFill";
            settings.Blur = blur.Value; settings.TintOpacity = opacity.Value; settings.CornerRadius = radius.Value; settings.Tint = tintButton.BackColor;
            settings.TextContrast = textContrast.SelectedIndex == 1 ? "white" : textContrast.SelectedIndex == 2 ? "black" : "auto";
            settings.Width = kind == "search" ? (int)width.Value : 0; if (kind == "search") settings.Height = (int)height.Value;
            if (kind == "search") settings.HideSearchAds = hideSearchAds.Checked;
            if (kind == "quick") { settings.SeparateMedia = separateMedia.Checked; settings.MediaTint = mediaTintButton.BackColor; settings.MediaOpacity = mediaOpacity.Value; settings.MediaRadius = mediaRadius.Value; }
        }

        private void ApplyClicked(object sender, EventArgs e)
        {
            Sync();
            if (settings.Mode == "image" && !File.Exists(settings.ImagePath)) { status.Text = "请先选择有效的背景图片。"; return; }
            settings.Enabled = true; settings.Save(settingsFile);
            StartWorker(kind == "search" ? "--apply-search" : "--apply-quick");
        }

        private void RestoreClicked(object sender, EventArgs e)
        {
            Sync(); settings.Enabled = false; settings.Save(settingsFile);
            StartWorker(kind == "search" ? "--restore-search" : "--restore-quick");
        }

        private void StartWorker(string argument)
        {
            try
            {
                var info = new ProcessStartInfo(Application.ExecutablePath, argument);
                if (Worker.IsAdministrator()) { info.UseShellExecute = false; info.CreateNoWindow = true; info.WindowStyle = ProcessWindowStyle.Hidden; }
                else { info.UseShellExecute = true; info.Verb = "runas"; info.WindowStyle = ProcessWindowStyle.Hidden; }
                var process = Process.Start(info); status.Text = "正在后台应用，请稍候…";
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try { process.WaitForExit(); } catch { } finally { process.Dispose(); }
                    try { if (!IsDisposed && IsHandleCreated) BeginInvoke((MethodInvoker)delegate { status.Text = Worker.DiagnosticText(); }); } catch { }
                });
            }
            catch (System.ComponentModel.Win32Exception ex) { status.Text = ex.NativeErrorCode == 1223 ? "已取消管理员确认，设置没有更改。" : "无法应用：" + ex.Message; }
            catch (Exception ex) { status.Text = "无法应用：" + ex.Message; }
        }

        protected override void OnFormClosing(FormClosingEventArgs e) { try { Sync(); settings.Save(settingsFile); } catch { } base.OnFormClosing(e); }
    }

    internal sealed class AdvancedForm : Form
    {
        private readonly DesignSettings settings;
        private readonly ComboBox contrast = new ComboBox();
        private readonly ComboBox tone = new ComboBox();
        private readonly ComboBox sizePreset = new ComboBox();
        private readonly NumericUpDown menuWidth = new NumericUpDown();
        private readonly NumericUpDown menuHeight = new NumericUpDown();
        private readonly CheckBox hideRecommended = new CheckBox();
        private readonly CheckBox hideSearch = new CheckBox();
        private readonly CheckBox alignToStartButton = new CheckBox();

        internal AdvancedForm(DesignSettings settings)
        {
            this.settings = settings;
            Text = "字体、布局与尺寸";
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(247, 249, 252);
            ForeColor = Color.FromArgb(27, 32, 43);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 540);
            Build();
            Populate();
        }

        private void Build()
        {
            Controls.Add(new Label { Text = "可读性与背景", Font = new Font(Font, FontStyle.Bold), AutoSize = true, Location = new Point(28, 24) });
            AddLabel("字体颜色", 28, 62);
            SetupCombo(contrast, new[] { "自动分析背景亮度", "始终使用白色字体", "始终使用黑色字体" }, 155, 57);
            AddLabel("背景倾向", 28, 105);
            SetupCombo(tone, new[] { "自定义颜色", "偏黑玻璃", "偏白玻璃" }, 155, 100);
            Controls.Add(new Label { Text = "自动模式会分析图片或壁纸亮度，并自动切换黑字或白字。\r\n自定义尺寸最低为 680 × 620，避免开始菜单内容被裁切。", AutoSize = false, Size = new Size(455, 42), Location = new Point(28, 143), ForeColor = Color.FromArgb(98, 106, 121) });

            Controls.Add(new Label { Text = "菜单大小", Font = new Font(Font, FontStyle.Bold), AutoSize = true, Location = new Point(28, 192) });
            AddLabel("尺寸预设", 28, 230);
            SetupCombo(sizePreset, new[] { "跟随 Windows", "紧凑（680 × 620）", "宽屏（1080 × 760）", "全屏", "自定义" }, 155, 225);
            sizePreset.SelectedIndexChanged += delegate { UpdateSizeInputs(); };
            AddLabel("自定义宽度", 28, 273); SetupNumber(menuWidth, 155, 268, 680, 1400);
            AddLabel("自定义高度", 28, 316); SetupNumber(menuHeight, 155, 311, 620, 1000);

            Controls.Add(new Label { Text = "内容排版", Font = new Font(Font, FontStyle.Bold), AutoSize = true, Location = new Point(28, 366) });
            hideSearch.Text = "隐藏搜索框"; hideSearch.AutoSize = true; hideSearch.Location = new Point(31, 402); Controls.Add(hideSearch);
            hideRecommended.Text = "隐藏推荐项目区"; hideRecommended.AutoSize = true; hideRecommended.Location = new Point(180, 402); Controls.Add(hideRecommended);
            alignToStartButton.Text = "菜单左边缘与任务栏 Windows 按钮左边缘对齐"; alignToStartButton.AutoSize = true; alignToStartButton.Location = new Point(31, 434); Controls.Add(alignToStartButton);

            var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, BackColor = Color.FromArgb(42, 111, 232), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Location = new Point(304, 482), Size = new Size(88, 36) };
            ok.Click += delegate { Save(); };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(402, 482), Size = new Size(88, 36) };
            Controls.Add(ok); Controls.Add(cancel); AcceptButton = ok; CancelButton = cancel;
        }

        private void Populate()
        {
            contrast.SelectedIndex = settings.TextContrast == "white" ? 1 : settings.TextContrast == "black" ? 2 : 0;
            tone.SelectedIndex = settings.Tone == "dark" ? 1 : settings.Tone == "light" ? 2 : 0;
            sizePreset.SelectedIndex = settings.SizePreset == "compact" ? 1 : settings.SizePreset == "wide" ? 2 : settings.SizePreset == "fullscreen" ? 3 : settings.SizePreset == "custom" ? 4 : 0;
            menuWidth.Value = Math.Max(menuWidth.Minimum, Math.Min(menuWidth.Maximum, settings.MenuWidth));
            menuHeight.Value = Math.Max(menuHeight.Minimum, Math.Min(menuHeight.Maximum, settings.MenuHeight));
            hideRecommended.Checked = settings.HideRecommended; hideSearch.Checked = settings.HideSearch;
            alignToStartButton.Checked = settings.AlignToStartButton;
            UpdateSizeInputs();
        }

        private void Save()
        {
            settings.TextContrast = contrast.SelectedIndex == 1 ? "white" : contrast.SelectedIndex == 2 ? "black" : "auto";
            settings.Tone = tone.SelectedIndex == 1 ? "dark" : tone.SelectedIndex == 2 ? "light" : "custom";
            string[] presets = { "auto", "compact", "wide", "fullscreen", "custom" };
            settings.SizePreset = presets[Math.Max(0, sizePreset.SelectedIndex)];
            settings.MenuWidth = (int)menuWidth.Value; settings.MenuHeight = (int)menuHeight.Value;
            settings.HideRecommended = hideRecommended.Checked; settings.HideSearch = hideSearch.Checked;
            settings.AlignToStartButton = alignToStartButton.Checked;
        }

        private void UpdateSizeInputs()
        {
            bool custom = sizePreset.SelectedIndex == 4; menuWidth.Enabled = custom; menuHeight.Enabled = custom;
        }

        private void AddLabel(string text, int x, int y) { Controls.Add(new Label { Text = text, AutoSize = true, Location = new Point(x, y) }); }
        private void SetupCombo(ComboBox box, string[] items, int x, int y) { box.DropDownStyle = ComboBoxStyle.DropDownList; box.Items.AddRange(items); box.Location = new Point(x, y); box.Size = new Size(335, 28); Controls.Add(box); }
        private void SetupNumber(NumericUpDown box, int x, int y, int min, int max) { box.Minimum = min; box.Maximum = max; box.Increment = 10; box.Location = new Point(x, y); box.Size = new Size(160, 28); Controls.Add(box); }
    }

    internal sealed class PreviewPanel : Panel
    {
        internal DesignSettings Settings;
        private Bitmap source;
        private string loadedPath;
        internal PreviewPanel() { DoubleBuffered = true; BackColor = Color.FromArgb(224, 229, 238); }

        internal void LoadImage(string path)
        {
            if (path == loadedPath) return;
            loadedPath = path;
            if (source != null) { source.Dispose(); source = null; }
            if (String.IsNullOrEmpty(path) || !File.Exists(path)) return;
            try
            {
                using (var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var i = Image.FromStream(s))
                {
                    // The preview panel is under 500 px wide. A 640 px cache
                    // is enough for high-DPI rendering and uses about half the
                    // bitmap memory of the former 900 px cache.
                    double scale = Math.Min(1.0, 640.0 / Math.Max(i.Width, i.Height));
                    int w = Math.Max(1, (int)Math.Round(i.Width * scale));
                    int h = Math.Max(1, (int)Math.Round(i.Height * scale));
                    source = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
                    using (var g = Graphics.FromImage(source))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(i, new Rectangle(0, 0, w, h));
                    }
                }
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && source != null)
            {
                source.Dispose();
                source = null;
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Settings == null) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var outer = new Rectangle(20, 12, Width - 40, Height - 50);
            using (var path = Rounded(outer, Settings.CornerRadius + 8))
            {
                e.Graphics.SetClip(path);
                if (Settings.Mode == "image" && source != null)
                {
                    if (Settings.Blur > 0)
                    {
                        using (var low = new Bitmap(Math.Max(35, outer.Width / (1 + Settings.Blur / 4)), Math.Max(35, outer.Height / (1 + Settings.Blur / 4))))
                        {
                            using (var lg = Graphics.FromImage(low)) { lg.InterpolationMode = InterpolationMode.HighQualityBicubic; ImageProcessor.DrawFit(lg, source, new Rectangle(0, 0, low.Width, low.Height), Settings.Fit); }
                            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic; e.Graphics.DrawImage(low, outer);
                        }
                    }
                    else ImageProcessor.DrawFit(e.Graphics, source, outer, Settings.Fit);
                }
                else if (Settings.Mode == "blur")
                {
                    using (var b = new LinearGradientBrush(outer, Color.FromArgb(128, 153, 221), Color.FromArgb(223, 177, 206), 35F)) e.Graphics.FillRectangle(b, outer);
                    for (int i = 0; i < 10; i++) using (var b = new SolidBrush(Color.FromArgb(10, 255, 255, 255))) e.Graphics.FillEllipse(b, outer.X - 100 + i * 55, outer.Y + i * 31, 240, 190);
                }
                else using (var b = new SolidBrush(ColorTools.EffectiveTint(Settings))) e.Graphics.FillRectangle(b, outer);
                if (Settings.Mode == "image" || Settings.Mode == "blur") using (var b = new SolidBrush(Color.FromArgb((int)(Settings.TintOpacity * 2.55), ColorTools.EffectiveTint(Settings)))) e.Graphics.FillRectangle(b, outer);
                e.Graphics.ResetClip();
                using (var pen = new Pen(Color.FromArgb(90, 255, 255, 255), 1)) e.Graphics.DrawPath(pen, path);
            }
            DrawStartContent(e.Graphics, outer);
            using (var b = new SolidBrush(Color.FromArgb(112, 122, 139))) e.Graphics.DrawString("预览（原生菜单内容会保留）", Font, b, 28, Height - 29);
        }

        private void DrawStartContent(Graphics g, Rectangle r)
        {
            Color readable = ColorTools.ReadableText(Settings);
            Color readableSoft = Color.FromArgb(175, readable);
            var search = new Rectangle(r.X + 24, r.Y + 20, r.Width - 48, 36);
            using (var p = Rounded(search, 18)) using (var b = new SolidBrush(Color.FromArgb(216, 255, 255, 255))) g.FillPath(b, p);
            using (var pen = new Pen(Color.FromArgb(48, 65, 82), 2)) { g.DrawEllipse(pen, search.X + 14, search.Y + 10, 13, 13); g.DrawLine(pen, search.X + 24, search.Y + 22, search.X + 30, search.Y + 28); }
            using (var b = new SolidBrush(Color.FromArgb(80, 85, 95))) g.DrawString("搜索应用、设置和文档", Font, b, search.X + 40, search.Y + 9);
            using (var b = new SolidBrush(Color.FromArgb(225, readable)))
            {
                g.DrawString("已固定", new Font(Font, FontStyle.Bold), b, r.X + 28, r.Y + 78);
                g.DrawString("推荐的项目", new Font(Font, FontStyle.Bold), b, r.X + 28, r.Y + 245);
            }
            Color[] colors = { Color.FromArgb(35, 109, 225), Color.FromArgb(35, 166, 91), Color.FromArgb(240, 101, 55), Color.FromArgb(116, 82, 218), Color.FromArgb(0, 145, 215), Color.FromArgb(244, 171, 38) };
            for (int row = 0; row < 2; row++) for (int col = 0; col < 6; col++)
            {
                int x = r.X + 31 + col * ((r.Width - 64) / 6), y = r.Y + 111 + row * 72;
                using (var b = new SolidBrush(colors[(row * 6 + col) % colors.Length])) g.FillRoundedRectangle(b, new Rectangle(x, y, 27, 27), 6);
                using (var b = new SolidBrush(readableSoft)) g.FillRectangle(b, x - 4, y + 35, 35, 3);
            }
            for (int i = 0; i < 3; i++)
            {
                int y = r.Y + 280 + i * 45;
                using (var b = new SolidBrush(Color.FromArgb(180, 255, 255, 255))) g.FillRoundedRectangle(b, new Rectangle(r.X + 28, y, 34, 34), 6);
                using (var b = new SolidBrush(readableSoft)) { g.FillRectangle(b, r.X + 75, y + 7, r.Width - 125, 5); g.FillRectangle(b, r.X + 75, y + 19, r.Width - 190, 4); }
            }
            using (var b = new SolidBrush(Color.FromArgb(70, 255, 255, 255))) g.FillRectangle(b, r.X, r.Bottom - 51, r.Width, 51);
            using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, r.X + 25, r.Bottom - 39, 26, 26);
            using (var b = new SolidBrush(Color.FromArgb(225, readable))) { g.DrawString(Environment.UserName, Font, b, r.X + 62, r.Bottom - 34); g.DrawString("⚙    ⏻", new Font("Segoe UI Symbol", 13F), b, r.Right - 86, r.Bottom - 38); }
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            radius = Math.Max(1, Math.Min(Math.Min(r.Width, r.Height) / 2, radius)); int d = radius * 2;
            var p = new GraphicsPath(); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p;
        }
    }

    internal static class GraphicsExtensions
    {
        internal static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle r, int radius)
        {
            using (var p = new GraphicsPath())
            {
                int d = radius * 2; p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); g.FillPath(brush, p);
            }
        }
    }
}
