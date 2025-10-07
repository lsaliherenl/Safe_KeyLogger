using System;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace KeyLogger
{
	public class AppSettings
	{
		public string LogDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		public long MaxLogSizeBytes { get; set; } = 2 * 1024 * 1024; // 2 MB
		public string ArchiveDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SafeTypeRecorder_Archive");
		public string Theme { get; set; } = "Light"; // Light | Dark
		public float FontSize { get; set; } = 9.0f;
		public string LastConsentIso { get; set; } = string.Empty; // Son rıza zaman damgası
		public bool FirstRunCompleted { get; set; } = false;

		// Email / SMTP settings
		public string SmtpHost { get; set; } = string.Empty;
		public int SmtpPort { get; set; } = 587;
		public bool SmtpUseSsl { get; set; } = true;
		public string SmtpUser { get; set; } = string.Empty;
		public string? SmtpPasswordProtected { get; set; } = null; // DPAPI ile korunmuş Base64
		public string RecipientEmail { get; set; } = string.Empty;
		public string FromEmail { get; set; } = string.Empty;

		private static string GetSettingsPath()
		{
			var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SafeTypeRecorder");
			Directory.CreateDirectory(dir);
			return Path.Combine(dir, "settings.json");
		}

		public void Save()
		{
			var path = GetSettingsPath();
			var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(path, json);
		}

		public static AppSettings Load()
		{
			try
			{
				var path = GetSettingsPath();
				if (!File.Exists(path)) return new AppSettings();
				var json = File.ReadAllText(path);
				var s = JsonSerializer.Deserialize<AppSettings>(json);
				return s ?? new AppSettings();
			}
			catch
			{
				return new AppSettings();
			}
		}

		public void SetSmtpPassword(string plainText)
		{
			if (string.IsNullOrEmpty(plainText))
			{
				SmtpPasswordProtected = null;
				return;
			}
			var data = Encoding.UTF8.GetBytes(plainText);
			var enc = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
			SmtpPasswordProtected = Convert.ToBase64String(enc);
		}

		public string GetSmtpPasswordOrEmpty()
		{
			try
			{
				if (string.IsNullOrEmpty(SmtpPasswordProtected)) return string.Empty;
				var enc = Convert.FromBase64String(SmtpPasswordProtected);
				var dec = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
				return Encoding.UTF8.GetString(dec);
			}
			catch
			{
				return string.Empty;
			}
		}
	}
}

