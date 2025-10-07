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
        private ToolStripStatusLabel statusLabelChars = null!;
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
        private DateTime currentParagraphMinute = DateTime.MinValue;
        private DateTime currentFileParagraphMinute = DateTime.MinValue;
        private bool writePlainMarkdown = true; // Düz metin (.md) yaz
        private int charsSinceLastAutoEmail = 0;
        private int autoEmailThreshold = 50;
        private bool autoEmailEnabled = true;
        private bool autoEmailSending = false;
        private long sessionPrintableCharCount = 0;

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
            logPath = Path.Combine(logDir, "safe_type_log.md");

            btnStartStop.Click += BtnStartStop_Click;
            btnOpenLog.Click += BtnOpenLog_Click;

            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            this.KeyPress += Form1_KeyPress;

            UpdateStatusBar();
            EnsureFirstRunWizard();
            ApplyThemeAndFont();
        }

        private void InitializeMenu()
        {
            menuStrip = new MenuStrip();

            var fileMenu = new ToolStripMenuItem("Dosya");
            var miOpen = new ToolStripMenuItem("Logu Aç", null, (s, e) => BtnOpenLog_Click(s, e)) { ShortcutKeys = Keys.Control | Keys.L };
            var miClear = new ToolStripMenuItem("Logu Temizle", null, (s, e) => ClearLog()) { ShortcutKeys = Keys.Control | Keys.D };
            var miSend = new ToolStripMenuItem("E-postaya Gönder", null, (s, e) => SendLogByEmail()) { ShortcutKeys = Keys.Control | Keys.E };
            var miExport = new ToolStripMenuItem("Ayarları Dışa Aktar (maskeli)", null, (s, e) => ExportSettingsMasked()) { ShortcutKeys = Keys.Control | Keys.S };
            var miReset = new ToolStripMenuItem("Ayarları Sıfırla", null, (s, e) => ResetSettingsToDefaults()) { ShortcutKeys = Keys.Control | Keys.R };
            var miExit = new ToolStripMenuItem("Çıkış", null, (s, e) => Close()) { ShortcutKeys = Keys.Alt | Keys.F4 };
            fileMenu.DropDownItems.Add(miOpen);
            fileMenu.DropDownItems.Add(miClear);
            fileMenu.DropDownItems.Add(miSend);
            fileMenu.DropDownItems.Add(miExport);
            fileMenu.DropDownItems.Add(miReset);
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
            statusLabelChars = new ToolStripStatusLabel();
            statusStrip.Items.Add(statusLabelRecording);
            statusStrip.Items.Add(new ToolStripStatusLabel("|"));
            statusStrip.Items.Add(statusLabelLines);
            statusStrip.Items.Add(new ToolStripStatusLabel("|"));
            statusStrip.Items.Add(statusLabelSize);
            statusStrip.Items.Add(new ToolStripStatusLabel("|"));
            statusStrip.Items.Add(statusLabelChars);
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
            statusLabelChars.Text = $"Karakter: {sessionPrintableCharCount}";
        }

        private void BtnStartStop_Click(object sender, EventArgs e)
        {
            if (!recording)
            {
                var consent = MessageBox.Show(
                    "Bu uygulama yalnızca bu pencere odaktayken yapılan tuş vuruşlarını kaydedecektir.\n" +
                    "Hassas bilgileri (parolalar, kart numaraları vb.) GİRMEYİN.\n" +
                    "Not: 50 karaktere ulaşıldığında log dosyası otomatik olarak e‑posta ile gönderilir.\n\n" +
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
                sessionPrintableCharCount = 0;
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
            // Ekranda göstermiyoruz. Dosyaya yaz: yalnızca şifreli modda.
            if (!writePlainMarkdown)
                AppendLogEntry(entry, displayNewLine: false, displayText: false);
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!recording) return;
            if (ShouldSkipCurrentControl()) return;

            string printable = e.KeyChar.ToString();
            if (char.IsControl(e.KeyChar))
                printable = $"\\u{(int)e.KeyChar:x4}";

            // Görünümü dakika başına paragraf olarak güncelle
            AppendDisplayFromKeyPress(e);

            if (writePlainMarkdown)
            {
                AppendMarkdownFromKeyPressToFile(e);
            }
            else
            {
                // Dosyaya ayrıntılı olay olarak yazmaya devam et (ekranda göstermeden)
                string entry = $"{DateTime.Now:HH:mm:ss} KeyPress '{printable}'";
                AppendLogEntry(entry, displayNewLine: false, displayText: false);
            }
        }

        private void AppendDisplayFromKeyPress(KeyPressEventArgs e)
        {
            var now = DateTime.Now;
            var minute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

            if (currentParagraphMinute != minute)
            {
                // Önceki paragrafı satırla kapat ve yeni dakika başlığı yaz
                if (txtDisplay.TextLength > 0)
                {
                    if (!txtDisplay.Text.EndsWith(Environment.NewLine))
                        txtDisplay.AppendText(Environment.NewLine);
                }
                txtDisplay.AppendText($"[{now:HH:mm}] ");
                currentParagraphMinute = minute;
            }

            char c = e.KeyChar;
            if (c == '\r' || c == '\n') { txtDisplay.AppendText(Environment.NewLine); return; }

            if (char.IsControl(c))
            {
                // Diğer kontrol karakterlerini görselde yoksay
                return;
            }

            txtDisplay.AppendText(c.ToString());
        }

        private void AppendMarkdownFromKeyPressToFile(KeyPressEventArgs e)
        {
            try
            {
                EnsureLogRotation();
                var now = DateTime.Now;
                var minute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

                if (currentFileParagraphMinute != minute)
                {
                    var prefix = (File.Exists(logPath) && new FileInfo(logPath).Length > 0) ? Environment.NewLine : string.Empty;
                    AppendToLogFile(prefix + $"[{now:HH:mm}] ");
                    currentFileParagraphMinute = minute;
                }

                char c = e.KeyChar;
                if (c == '\r' || c == '\n')
                {
                    AppendToLogFile(Environment.NewLine);
                    lineCount++;
                    UpdateStatusBar();
                    return;
                }
                if (char.IsControl(c)) return;

                AppendToLogFile(c.ToString());
                // Otomatik e-posta tetikleyici: yalnızca yazdırılabilir karakterlerde say
                if (autoEmailEnabled)
                {
                    charsSinceLastAutoEmail++;
                    TryTriggerAutoEmail();
                }
                sessionPrintableCharCount++;
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                AppendVisibleLine("[Hata] MD yazılamadı: " + ex.Message);
            }
        }

        private void AppendToLogFile(string text)
        {
            const int maxAttempts = 4;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var data = Encoding.UTF8.GetBytes(text);
                    using (var fs = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    {
                        fs.Write(data, 0, data.Length);
                    }
                    return;
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
            // Son çare: StreamWriter ile bir kez daha dene (aynı paylaşım modu)
            using (var fs = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var sw = new StreamWriter(fs, Encoding.UTF8))
            {
                sw.Write(text);
            }
        }

        private void TryTriggerAutoEmail()
        {
            if (autoEmailSending) return;
            if (charsSinceLastAutoEmail >= autoEmailThreshold)
            {
                SendLogByEmailAuto();
            }
        }

        private void SendLogByEmailAuto()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(settings.SmtpHost) || string.IsNullOrWhiteSpace(settings.RecipientEmail))
                {
                    return; // SMTP yapılandırılmadıysa sessizce çık
                }
                autoEmailSending = true;
                var fileToSend = logPath;
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        using var client = new System.Net.Mail.SmtpClient(settings.SmtpHost, settings.SmtpPort)
                        {
                            EnableSsl = settings.SmtpUseSsl,
                            Credentials = new System.Net.NetworkCredential(settings.SmtpUser, settings.GetSmtpPasswordOrEmpty())
                        };

                        var from = string.IsNullOrWhiteSpace(settings.FromEmail) ? settings.SmtpUser : settings.FromEmail;
                        using var mail = new System.Net.Mail.MailMessage(from, settings.RecipientEmail)
                        {
                            Subject = $"SafeType Auto Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                            Body = $"Otomatik gönderim (karakter eşiği). Oturum: {sessionId}. Dosya: {Path.GetFileName(fileToSend)}"
                        };
                        // Dosya kilitlenmesini önlemek için geçici kopya ile gönder
                        var tempCopy = Path.Combine(Path.GetTempPath(), $"SafeType_{Guid.NewGuid():N}.md");
                        try
                        {
                            File.Copy(fileToSend, tempCopy, true);
                            mail.Attachments.Add(new System.Net.Mail.Attachment(tempCopy));
                        }
                        catch { mail.Attachments.Add(new System.Net.Mail.Attachment(fileToSend)); }

                        client.Send(mail);
                        try { this.BeginInvoke(new Action(() => AppendVisibleLine("[Bilgi] Otomatik e-posta gönderildi."))); } catch { }
                    }
                    catch (Exception ex)
                    {
                        try { this.BeginInvoke(new Action(() => AppendVisibleLine("[Hata] Otomatik e-posta: " + ex.Message))); } catch { }
                    }
                    finally
                    {
                        // Geçici dosyayı temizle
                        try
                        {
                            foreach (var att in new System.Net.Mail.Attachment[] { }) { }
                        }
                        catch { }
                        charsSinceLastAutoEmail = 0;
                        autoEmailSending = false;
                    }
                });
            }
            catch { autoEmailSending = false; }
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

        private void AppendLogEntry(string entry, bool displayNewLine = true, bool displayText = true)
        {
            // Görünüm: istenirse yaz
            if (displayText)
            {
                if (displayNewLine)
                {
                    AppendVisibleLine(entry);
                }
                else
                {
                    txtDisplay.AppendText(entry);
                }
            }

            try
            {
                EnsureLogRotation();
                var plain = Encoding.UTF8.GetBytes(entry);
                var encrypted = ProtectedData.Protect(plain, optionalEntropy, DataProtectionScope.CurrentUser);
                var b64 = Convert.ToBase64String(encrypted);

                // Dosya formatını satır-bazlı tut: her giriş yeni satır
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
                    logPath = Path.Combine(logDir, "safe_type_log.md");
                    UpdateStatusBar();
                    ApplyThemeAndFont();
                }
            }
        }

        private void ResetSettingsToDefaults()
        {
            try
            {
                settings = new AppSettings();
                settings.Save();
                var logDir = Directory.Exists(settings.LogDirectory) ? settings.LogDirectory : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                logPath = Path.Combine(logDir, "safe_type_log.md");
                sessionPrintableCharCount = 0;
                ApplyThemeAndFont();
                UpdateStatusBar();
                AppendVisibleLine("[Bilgi] Ayarlar varsayılanlara sıfırlandı.");
            }
            catch (Exception ex)
            {
                AppendVisibleLine("[Hata] Ayarlar sıfırlanamadı: " + ex.Message);
            }
        }

        private void ExportSettingsMasked()
        {
            try
            {
                var s = AppSettings.Load();
                var export = new
                {
                    s.LogDirectory,
                    s.MaxLogSizeBytes,
                    s.ArchiveDirectory,
                    s.Theme,
                    s.FontSize,
                    s.LastConsentIso,
                    SmtpHost = string.IsNullOrWhiteSpace(s.SmtpHost) ? "" : MaskMiddle(s.SmtpHost),
                    SmtpPort = s.SmtpPort,
                    SmtpUseSsl = s.SmtpUseSsl,
                    SmtpUser = string.IsNullOrWhiteSpace(s.SmtpUser) ? "" : MaskMiddle(s.SmtpUser),
                    RecipientEmail = string.IsNullOrWhiteSpace(s.RecipientEmail) ? "" : MaskMiddle(s.RecipientEmail),
                    FromEmail = string.IsNullOrWhiteSpace(s.FromEmail) ? "" : MaskMiddle(s.FromEmail),
                };

                using (var sfd = new SaveFileDialog())
                {
                    sfd.Title = "Ayarları dışa aktar";
                    sfd.Filter = "JSON (*.json)|*.json|Tümü (*.*)|*.*";
                    sfd.FileName = "settings_masked.json";
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(export, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(sfd.FileName, json, Encoding.UTF8);
                        AppendVisibleLine("[Bilgi] Ayarlar maskeli olarak dışa aktarıldı: " + sfd.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                AppendVisibleLine("[Hata] Ayarlar dışa aktarılamadı: " + ex.Message);
            }
        }

        private static string MaskMiddle(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            int keep = Math.Min(2, value.Length / 3);
            var start = value.Substring(0, keep);
            var end = value.Substring(value.Length - keep, keep);
            return start + new string('*', Math.Max(3, value.Length - keep * 2)) + end;
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

        private void EnsureFirstRunWizard()
        {
            try
            {
                if (!settings.FirstRunCompleted)
                {
                    var result = MessageBox.Show(
                        "Varsayılan log klasörü Belgelerim. Değiştirmek ister misiniz?",
                        "İlk Kurulum",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        using (var fbd = new FolderBrowserDialog())
                        {
                            fbd.Description = "Log klasörünü seçin";
                            if (fbd.ShowDialog(this) == DialogResult.OK)
                            {
                                settings.LogDirectory = fbd.SelectedPath;
                            }
                        }
                    }
                    settings.FirstRunCompleted = true;
                    settings.Save();
                    var logDir = Directory.Exists(settings.LogDirectory) ? settings.LogDirectory : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    logPath = Path.Combine(logDir, "safe_type_log.md");
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
                    ofd.Filter = "Log (*.md;*.txt;*.dat)|*.md;*.txt;*.dat|Tümü (*.*)|*.*";
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
