using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using IWshRuntimeLibrary;
using System.Security.Principal;
using System.Reflection;

namespace GoodbyeDPI_Launcher
{
    public partial class Form1 : Form
    {
        private readonly string[] resourceFiles = { "goodbyedpi.exe", "WinDivert.dll", "WinDivert64.sys" };
        private readonly string tempDir = Path.Combine(Path.GetTempPath(), "GoodbyeDPI");
        private NotifyIcon trayIcon;

        public Form1()
        {
            InitializeComponent();
            SetupTrayIcon();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            string[] args = Environment.GetCommandLineArgs();

            if (args.Contains("-autostart"))
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Visible = false;
                StartDpiProcess(true);
            }
        }

        private void SetupTrayIcon()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "GoodbyeDPI Launcher";
            trayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Göster", null, (s, e) => ShowForm());
            contextMenu.Items.Add("Kapat", null, (s, e) => {
                btnDurdur_Click(null, null);
                Application.Exit();
            });

            trayIcon.ContextMenuStrip = contextMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => ShowForm();
        }

        private void ShowForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.BringToFront();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.gg/Xbk6GYyxX8",
                UseShellExecute = true
            });
        }

        private void btnBaslat_Click(object sender, EventArgs e)
        {
            StartDpiProcess(false);
        }

        private void StartDpiProcess(bool silentMode)
        {
            try
            {
                Process[] runningProcesses = Process.GetProcessesByName("goodbyedpi");
                foreach (Process process in runningProcesses)
                {
                    if (process.Id != Process.GetCurrentProcess().Id)
                    {
                        if (!silentMode) MessageBox.Show("GoodbyeDPI zaten çalışıyor!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                if (!ExtractResources())
                {
                    if (!silentMode) MessageBox.Show("Gerekli dosyalar kaynaklardan çıkarılamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string exePath = Path.Combine(tempDir, "goodbyedpi.exe");

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "-5 --dns-addr 77.88.8.8 --dns-port 1253 --dnsv6-addr 2a02:6b8::feed:0ff --dnsv6-port 1253",
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = tempDir
                };

                Process.Start(psi);
                RegisterTask();

                if (!silentMode)
                    MessageBox.Show("GoodbyeDPI başlatıldı.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    trayIcon.ShowBalloonTip(3000, "GoodbyeDPI", "Arka planda otomatik başlatıldı.", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                if (!silentMode) MessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RegisterTask()
        {
            try
            {
                string taskName = "GoodbyeDPILauncherAutoStart";
                string exePath = Application.ExecutablePath;
                string args = "-autostart";

                string xmlCommand = $"/Create /TN \"{taskName}\" /TR \"'{exePath}' {args}\" /SC ONLOGON /RL HIGHEST /F";

                ProcessStartInfo psi = new ProcessStartInfo("schtasks.exe", xmlCommand)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch { }
        }

        private bool ExtractResources()
        {
            try
            {
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                Assembly assembly = Assembly.GetExecutingAssembly();
                string[] allResourceNames = assembly.GetManifestResourceNames();

                foreach (string fileName in resourceFiles)
                {
                    string targetPath = Path.Combine(tempDir, fileName);
                    if (System.IO.File.Exists(targetPath) && new FileInfo(targetPath).Length > 0) continue;
                    string resourceName = allResourceNames.FirstOrDefault(r => r.EndsWith(fileName, StringComparison.InvariantCultureIgnoreCase));
                    if (string.IsNullOrEmpty(resourceName)) return false;

                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null) return false;
                        using (FileStream fileStream = System.IO.File.Create(targetPath))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }
                return true;
            }
            catch { return false; }
        }

        private void btnDurdur_Click(object sender, EventArgs e)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("goodbyedpi");
                bool stopped = false;
                foreach (Process p in processes)
                {
                    p.Kill();
                    p.WaitForExit();
                    stopped = true;
                }

                if (sender != null)
                {
                    if (stopped) MessageBox.Show("GoodbyeDPI durduruldu.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else MessageBox.Show("Çalışan GoodbyeDPI bulunamadı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                if (sender != null) MessageBox.Show($"Durdurma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnKaldir_Click(object sender, EventArgs e)
        {
            if (!IsAdministrator())
            {
                MessageBox.Show("Yönetici olarak çalıştırmalısınız!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Sistem temizlenecek, onaylıyor musunuz?", "Onay", MessageBoxButtons.YesNo) == DialogResult.No) return;

            try
            {
                Process.Start(new ProcessStartInfo("schtasks.exe", "/Delete /TN \"GoodbyeDPILauncherAutoStart\" /F") { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });

                StopAndDeleteService("GoodbyeDPI");
                StopAndDeleteService("WinDivert");
                foreach (var p in Process.GetProcessesByName("goodbyedpi")) { try { p.Kill(); } catch { } }

                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                if (trayIcon != null) trayIcon.Visible = false;

                MessageBox.Show("Sistem temizlendi.");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void StopAndDeleteService(string serviceName)
        {
            try
            {
                Process.Start(new ProcessStartInfo("sc.exe", $"stop \"{serviceName}\"") { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden })?.WaitForExit();
                Process.Start(new ProcessStartInfo("sc.exe", $"delete \"{serviceName}\"") { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden })?.WaitForExit();
            }
            catch { }
        }

        private bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("GoodbyeDPI Launcher\nSürüm: v1.0.0", "Bilgi");
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = "https://github.com/Salim6996/GoodbyeDPI-Launcher", UseShellExecute = true });
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}