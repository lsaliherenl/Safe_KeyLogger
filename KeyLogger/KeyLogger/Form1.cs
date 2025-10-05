using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace KeyLogger
{
    public partial class Form1 : Form
    {
        private MenuStrip menuStrip = null!;
        private ToolStrip toolStrip = null!;
        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel statusLabelRecording = null!;
        private ToolStripStatusLabel statusLabelLines = null!;
        private ToolStripStatusLabel statusLabelSize = null!;
        private ToolStripButton tsBtnStartStop = null!;
        private ToolStripButton tsBtnOpenLog = null!;
        private ToolStripButton tsBtnSettings = null!;
        private ToolStripButton tsBtnClear = null!;
        private Button btnStartStop;
        private Button btnOpenLog;
        private TextBox txtDisplay;
        private bool recording = false;
        private string logPath;
        private byte[] optionalEntropy = null;
        private long lineCount = 0;
        private AppSettings settings;
        private string sessionId = Guid.NewGuid().ToString("N");

        public Form1()
        {
            Text = "Safe Type Recorder (Consent Required)";
            Width = 800;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;

            LoadSettings();

            InitializeMenu();
            InitializeToolBar();
            InitializeStatusBar();

            btnStartStop = new Button { Left = 10, Top = 10, Width = 120, Text = "Kaydı Başlat" };
            btnOpenLog = new Button { Left = 140, Top = 10, Width = 120, Text = "Logu Aç" };
            txtDisplay = new TextBox { Left = 10, Top = 50, Width = 760, Height = 500, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };

            Controls.Add(btnStartStop);
            Controls.Add(btnOpenLog);
            Controls.Add(txtDisplay);

            var logDir = Directory.Exists(settings.LogDirectory) ? settings.LogDirectory : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            logPath = Path.Combine(logDir, "safe_type_log.dat");

            btnStartStop.Click += BtnStartStop_Click;
            btnOpenLog.Click += BtnOpenLog_Click;

            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            this.KeyPress += Form1_KeyPress;

            UpdateStatusBar();
            ApplyThemeAndFont();
        }

        private void InitializeMenu()
        {
            menuStrip = new MenuStrip();

            var fileMenu = new ToolStripMenuItem("Dosya");
            var miOpen = new ToolStripMenuItem("Logu Aç", null, (s, e) => BtnOpenLog_Click(s, e)) { ShortcutKeys = Keys.Control | Keys.L };
            var miClear = new ToolStripMenuItem("Logu Temizle", null, (s, e) => ClearLog()) { ShortcutKeys = Keys.Control | Keys.D };
            var miSend = new ToolStripMenuItem("E-postaya Gönder", null, (s, e) => SendLogByEmail()) { ShortcutKeys = Keys.Control | Keys.E };
            var miExit = new ToolStripMenuItem("Çıkış", null, (s, e) => Close()) { ShortcutKeys = Keys.Alt | Keys.F4 };
            fileMenu.DropDownItems.Add(miOpen);
            fileMenu.DropDownItems.Add(miClear);
            fileMenu.DropDownItems.Add(miSend);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(miExit);

            var viewMenu = new ToolStripMenuItem("Görünüm");
            var miSettings = new ToolStripMenuItem("Ayarlar", null, (s, e) => OpenSettings()) { ShortcutKeys = Keys.Control | Keys.Oemcomma };
            viewMenu.DropDownItems.Add(miSettings);

            var helpMenu = new ToolStripMenuItem("Yardım");
            var miAbout = new ToolStripMenuItem("Hakkında", null, (s, e) => MessageBox.Show("Rızaya dayalı, uygulama odaklı yazı kaydedici.", "Hakkında"));
            helpMenu.DropDownItems.Add(miAbout);

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(viewMenu);
            menuStrip.Items.Add(helpMenu);

            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
        }

        private void InitializeToolBar()
        {
            toolStrip = new ToolStrip();
            tsBtnStartStop = new ToolStripButton("Başlat/Durdur", null, (s, e) => BtnStartStop_Click(s, e));
            tsBtnOpenLog = new ToolStripButton("Logu Aç", null, (s, e) => BtnOpenLog_Click(s, e));
            tsBtnSettings = new ToolStripButton("Ayarlar", null, (s, e) => OpenSettings());
            tsBtnClear = new ToolStripButton("Temizle", null, (s, e) => ClearLog());
            var tsBtnSend = new ToolStripButton("Gönder", null, (s, e) => SendLogByEmail());

            toolStrip.Items.Add(tsBtnStartStop);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(tsBtnOpenLog);
            toolStrip.Items.Add(tsBtnClear);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(tsBtnSend);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(tsBtnSettings);

            toolStrip.Top = menuStrip.Height;
            Controls.Add(toolStrip);
        }

        private void InitializeStatusBar()
        {
            statusStrip = new StatusStrip();
            statusLabelRecording = new ToolStripStatusLabel();
            statusLabelLines = new ToolStripStatusLabel();
            statusLabelSize = new ToolStripStatusLabel();
            statusStrip.Items.Add(statusLabelRecording);
            statusStrip.Items.Add(new ToolStripStatusLabel("|"));
            statusStrip.Items.Add(statusLabelLines);
            statusStrip.Items.Add(new ToolStripStatusLabel("|"));
            statusStrip.Items.Add(statusLabelSize);
            statusStrip.Dock = DockStyle.Bottom;
            Controls.Add(statusStrip);
        }

        private void UpdateStatusBar()
        {
            statusLabelRecording.Text = recording ? "Kayıt: Açık" : "Kayıt: Kapalı";
            statusLabelLines.Text = $"Satır: {lineCount}";
            try
            {
                var size = File.Exists(logPath) ? new FileInfo(logPath).Length : 0;
                statusLabelSize.Text = $"Boyut: {FormatBytes(size)}";
            }
            catch
            {
                statusLabelSize.Text = "Boyut: -";
            }
        }

        private void BtnStartStop_Click(object sender, EventArgs e)
        {
            if (!recording)
            {
                var consent = MessageBox.Show(
                    "Bu uygulama yalnızca bu pencere odaktayken yapılan tuş vuruşlarını kaydedecektir.\n" +
                    "Hassas bilgileri (parolalar, kart numaraları vb.) GİRMEYİN.\n\n" +
                    "Kayıt başlatılsın mı? (Evet = Başlat, Hayır = İptal)",
                    "Rıza / Consent",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (consent != DialogResult.Yes)
                {
                    return;
                }

                recording = true;
                btnStartStop.Text = "Kaydı Durdur";
                settings.LastConsentIso = DateTime.UtcNow.ToString("o");
                settings.Save();
                sessionId = Guid.NewGuid().ToString("N");
                AppendVisibleLine($"-- {DateTime.Now:HH:mm:ss} Session {sessionId} START -- ConsentUTC:{settings.LastConsentIso}");
                UpdateStatusBar();
            }
            else
            {
                recording = false;
                btnStartStop.Text = "Kaydı Başlat";
                AppendVisibleLine($"-- {DateTime.Now:HH:mm:ss} Session {sessionId} STOP --");
                UpdateStatusBar();
            }
        }

        private void BtnOpenLog_Click(object sender, EventArgs e)
        {
            if (!File.Exists(logPath))
            {
                MessageBox.Show("Log dosyası bulunamadı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var allLines = File.ReadAllLines(logPath);
                var sb = new StringBuilder();
                foreach (var line in allLines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var encrypted = Convert.FromBase64String(line);
                        var decrypted = ProtectedData.Unprotect(encrypted, optionalEntropy, DataProtectionScope.CurrentUser);
                        sb.AppendLine(Encoding.UTF8.GetString(decrypted));
                    }
                    catch
                    {
                        sb.AppendLine("[!!] Bir satır çözülemedi veya bozuk: " + line);
                    }
                }

                var viewer = new Form { Text = "Log Görüntüleyici", Width = 700, Height = 500, StartPosition = FormStartPosition.CenterParent };
                var tv = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, Text = sb.ToString() };
                viewer.Controls.Add(tv);
                viewer.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Log açılırken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!recording) return;
            if (ShouldSkipCurrentControl()) return;

            string entry = $"{DateTime.Now:HH:mm:ss} KeyDown {e.KeyCode} Mod:{(e.Modifiers == Keys.None ? "-" : e.Modifiers.ToString())}";
            AppendLogEntry(entry);
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!recording) return;
            if (ShouldSkipCurrentControl()) return;

            string printable = e.KeyChar.ToString();
            if (char.IsControl(e.KeyChar))
                printable = $"\\u{(int)e.KeyChar:x4}";

            string entry = $"{DateTime.Now:HH:mm:ss} KeyPress '{printable}'";
            AppendLogEntry(entry);
        }

        private bool ShouldSkipCurrentControl()
        {
            try
            {
                var ctl = this.ActiveControl;
                if (ctl is TextBox tb)
                {
                    if (tb.UseSystemPasswordChar) return true;
                    if (tb.PasswordChar != '\0') return true;
                }
            }
            catch { }

            return false;
        }

        private void AppendVisibleLine(string text)
        {
            txtDisplay.AppendText(text + Environment.NewLine);
        }

        private void AppendLogEntry(string entry)
        {
            AppendVisibleLine(entry);

            try
            {
                EnsureLogRotation();
                var plain = Encoding.UTF8.GetBytes(entry);
                var encrypted = ProtectedData.Protect(plain, optionalEntropy, DataProtectionScope.CurrentUser);
                var b64 = Convert.ToBase64String(encrypted);

                File.AppendAllText(logPath, b64 + Environment.NewLine);
                lineCount++;
            }
            catch (Exception ex)
            {
                AppendVisibleLine("[Hata] Log yazılamadı: " + ex.Message);
            }
            UpdateStatusBar();
        }

        private void EnsureLogRotation()
        {
            try
            {
                if (!File.Exists(logPath)) return;
                var fi = new FileInfo(logPath);
                if (fi.Length < settings.MaxLogSizeBytes) return;

                var dir = Path.GetDirectoryName(logPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var name = Path.GetFileNameWithoutExtension(logPath);
                var ext = Path.GetExtension(logPath);
                var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var archiveRoot = Directory.Exists(settings.ArchiveDirectory) ? settings.ArchiveDirectory : dir;
                Directory.CreateDirectory(archiveRoot);
                var archive = Path.Combine(archiveRoot, $"{name}_{ts}{ext}");
                File.Move(logPath, archive, true);
                lineCount = 0;
            }
            catch (Exception ex)
            {
                AppendVisibleLine("[Hata] Log rotasyonu yapılamadı: " + ex.Message);
            }
        }

        private void ClearLog()
        {
            try
            {
                if (File.Exists(logPath)) File.Delete(logPath);
                lineCount = 0;
                AppendVisibleLine("[Bilgi] Log temizlendi.");
            }
            catch (Exception ex)
            {
                AppendVisibleLine("[Hata] Log temizlenemedi: " + ex.Message);
            }
            UpdateStatusBar();
        }

        private void LoadSettings()
        {
            settings = AppSettings.Load();
        }

        private void OpenSettings()
        {
            using (var f = new SettingsForm(settings))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    settings = f.ResultSettings;
                    settings.Save();
                    var logDir = Directory.Exists(settings.LogDirectory) ? settings.LogDirectory : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    logPath = Path.Combine(logDir, "safe_type_log.dat");
                    UpdateStatusBar();
                    ApplyThemeAndFont();
                }
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }

        private void ApplyThemeAndFont()
        {
            try
            {
                var font = new System.Drawing.Font(SystemFonts.MessageBoxFont.FontFamily, settings.FontSize);
                this.Font = font;
                txtDisplay.Font = font;

                bool dark = string.Equals(settings.Theme, "Dark", StringComparison.OrdinalIgnoreCase);
                var back = dark ? System.Drawing.Color.FromArgb(32, 32, 32) : System.Drawing.SystemColors.Control;
                var fore = dark ? System.Drawing.Color.Gainsboro : System.Drawing.SystemColors.ControlText;
                var textBack = dark ? System.Drawing.Color.FromArgb(24, 24, 24) : System.Drawing.Color.White;

                this.BackColor = back;
                this.ForeColor = fore;
                foreach (Control c in this.Controls)
                {
                    c.ForeColor = fore;
                    if (c is TextBox)
                    {
                        c.BackColor = textBack;
                    }
                    else
                    {
                        c.BackColor = back;
                    }
                }
                if (menuStrip != null)
                {
                    menuStrip.BackColor = back;
                    menuStrip.ForeColor = fore;
                }
                if (toolStrip != null)
                {
                    toolStrip.BackColor = back;
                    toolStrip.ForeColor = fore;
                }
                if (statusStrip != null)
                {
                    statusStrip.BackColor = back;
                    statusStrip.ForeColor = fore;
                }
            }
            catch { }
        }

        private void SendLogByEmail()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(settings.SmtpHost) || string.IsNullOrWhiteSpace(settings.RecipientEmail))
                {
                    MessageBox.Show("Lütfen Ayarlar > E-posta (SMTP) bilgilerini doldurun.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Dosya seçtir, varsayılan olarak mevcut log
                string selectedFile = logPath;
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Title = "Gönderilecek log dosyasını seçin";
                    ofd.InitialDirectory = Path.GetDirectoryName(logPath);
                    ofd.Filter = "Log (*.dat)|*.dat|Tümü (*.*)|*.*";
                    ofd.FileName = Path.GetFileName(logPath);
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        selectedFile = ofd.FileName;
                    }
                    else
                    {
                        return;
                    }
                }

                using var client = new System.Net.Mail.SmtpClient(settings.SmtpHost, settings.SmtpPort)
                {
                    EnableSsl = settings.SmtpUseSsl,
                    Credentials = new System.Net.NetworkCredential(settings.SmtpUser, settings.GetSmtpPasswordOrEmpty())
                };

                var from = string.IsNullOrWhiteSpace(settings.FromEmail) ? settings.SmtpUser : settings.FromEmail;
                using var mail = new System.Net.Mail.MailMessage(from, settings.RecipientEmail)
                {
                    Subject = $"SafeType Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    Body = $"Otomatik gönderim. Oturum: {sessionId}. Dosya: {Path.GetFileName(selectedFile)}"
                };
                mail.Attachments.Add(new System.Net.Mail.Attachment(selectedFile));

                client.Send(mail);
                MessageBox.Show("E-posta gönderildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("E-posta gönderilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
