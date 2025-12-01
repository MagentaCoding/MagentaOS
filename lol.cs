using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace BSDAPP
{
    public partial class Form1 : Form
    {
        private PrivateFontCollection _fonts = new PrivateFontCollection();
        private Font _mainFont = SystemFonts.DefaultFont;
        private Font _countdownFont = SystemFonts.DefaultFont;
        private System.Windows.Forms.Timer? countdownTimer;
        private int remainingSeconds = 0;

        public Form1()
        {
            InitializeComponent();
            LoadFonts();
            ApplyFonts();
            // Countdown wird erst gestartet, wenn Set Time geklickt wird
        }

        private void LoadFonts()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            _mainFont = LoadFontSafe(asm, "BSDAPP.Fonts.main.ttf", 16f);
            _countdownFont = LoadFontSafe(asm, "BSDAPP.Fonts.countdown.ttf", 64f);
        }

        private Font LoadFontSafe(Assembly asm, string resourceName, float size)
        {
            using Stream fontStream = asm.GetManifestResourceStream(resourceName)
                ?? throw new Exception($"Font {resourceName} nicht gefunden");
            byte[] fontData = ReadFully(fontStream);

            GCHandle handle = GCHandle.Alloc(fontData, GCHandleType.Pinned);
            try
            {
                _fonts.AddMemoryFont(handle.AddrOfPinnedObject(), fontData.Length);
            }
            finally
            {
                handle.Free();
            }

            return new Font(_fonts.Families[_fonts.Families.Length - 1], size);
        }

        private static byte[] ReadFully(Stream input)
        {
            using MemoryStream ms = new MemoryStream();
            input.CopyTo(ms);
            return ms.ToArray();
        }

        private void ApplyFonts()
        {
            lblCountdown.Font = _countdownFont;
            btnAbort.Font = _mainFont;
            btnClose.Font = _mainFont;
            txtTimeInput.Font = _mainFont;
            btnSetTime.Font = _mainFont;
        }

        private void StartShutdown(int seconds)
        {
            remainingSeconds = seconds;

            countdownTimer?.Stop();
            countdownTimer = new System.Windows.Forms.Timer();
            countdownTimer.Interval = 1000;
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            if (remainingSeconds <= 0)
            {
                countdownTimer?.Stop();
                lblCountdown.Text = "Self destruct sequence initiated...";
                System.Diagnostics.Process.Start("shutdown", "/sg /t 0");
                return;
            }

            remainingSeconds--;
            TimeSpan ts = TimeSpan.FromSeconds(remainingSeconds);
            lblCountdown.Text = $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        private async void BtnAbort_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("shutdown", "/a");
                countdownTimer?.Stop();
                lblCountdown.Text = "Abgebrochen";

                await Task.Delay(5000);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Abbrechen des Shutdowns: {ex.Message}");
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private int ParseTimeInput(string input)
        {
            input = input.Trim().ToLower();

            if (int.TryParse(input, out int sec))
                return sec;

            int totalSeconds = 0;
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part.EndsWith("h") && int.TryParse(part[..^1], out int h))
                    totalSeconds += h * 3600;
                else if (part.EndsWith("m") && int.TryParse(part[..^1], out int m))
                    totalSeconds += m * 60;
                else if (part.EndsWith("s") && int.TryParse(part[..^1], out int s))
                    totalSeconds += s;
            }
            if (totalSeconds > 0) return totalSeconds;

            if (TimeSpan.TryParse(input, out TimeSpan ts))
            {
                DateTime target = DateTime.Today + ts;
                int diff = (int)(target - DateTime.Now).TotalSeconds;
                return Math.Max(diff, 0);
            }

            if (DateTime.TryParseExact(input, "dd.MM.yyyy HH:mm", null,
                System.Globalization.DateTimeStyles.None, out DateTime dt))
            {
                int diff = (int)(dt - DateTime.Now).TotalSeconds;
                return Math.Max(diff, 0);
            }

            throw new Exception("Ung�ltiges Zeitformat!");
        }

        private void BtnSetTime_Click(object sender, EventArgs e)
        {
            try
            {
                int seconds = ParseTimeInput(txtTimeInput.Text);
                StartShutdown(seconds);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lblCountdown_Click(object sender, EventArgs e)
        {

        }
    }
}
