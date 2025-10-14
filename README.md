Okul Projesi Notu
------------------
Bu yazılım, eğitim amaçlı bir okul projesidir. Kötüye kullanım için tasarlanmamıştır; yalnızca açık rıza ile ve uygulama odaklı kayıt gerçekleştirir.

YouTube Video Linki: https://youtu.be/Qtq68xnvv4A

Safe Type Recorder

Uygulama odaklı, açık rızaya dayalı, güvenli yazı kaydedici. Sadece bu uygulama penceresi odaktayken yapılan tuş vuruşlarını kaydeder; hassas alanları (şifre kutuları) atlar, kayıtları yerel olarak kullanıcı bağlamında şifreler (DPAPI).

Özellikler

- Uygulama odaklı kayıt: Sadece bu pencere odaktayken KeyDown/KeyPress olayları kaydedilir.
- Açık rıza akışı: Kayıt başlatılırken onay penceresi gösterilir; rıza zaman damgası saklanır.
- Hassas alanlardan kaçınma: TextBox.UseSystemPasswordChar veya PasswordChar setli alanlarda kayıt yapılmaz.
- Loglama: Varsayılan olarak sade Markdown (.md) olarak dakika başına paragraf halinde yazılır. İsteğe bağlı şifreli .dat moduna alınabilir.
- Oturumlar: Her kayıt başlat/durdur’da benzersiz sessionId ve zaman damgalı başlık satırları eklenir.
- Log görüntüleyici: Dosyadaki satırlar çözülüp ayrı bir pencerede okunabilir şekilde gösterilir.
- Menü/Toolbar: Sık kullanılan işlemler için menü ve araç çubuğu.
- StatusStrip: Kayıt durumu, toplam satır sayısı ve log dosyası boyutu.
- Tema ve yazı tipi: Açık/Koyu tema ve font boyutu ayarları tüm UI’ya uygulanır.
- Log rotasyonu: Boyut limiti aşıldığında log, zaman damgalı adla arşiv klasörüne taşınır.
- E‑posta ile gönderim: Log dosyasını seçerek SMTP üzerinden alıcıya gönderme; şifre DPAPI ile korunur.
  - Otomatik gönderim: Yazılabilir karakter sayısı 50’ye ulaştığında mevcut log dosyası otomatik olarak e‑posta ekiyle gönderilir.
- Ayarlar ekranı: Tüm yapılandırmalar için kullanıcı dostu arayüz.

Kurulum ve Derleme

Gereksinimler:
- .NET 8 SDK
- Windows 10/11

Komutlar (PowerShell):
cd "C:\DD proje\KeyLogger"
dotnet restore ".\KeyLogger\KeyLogger.sln"
dotnet build -c Debug ".\KeyLogger\KeyLogger.sln"

Çalıştırma:
dotnet run -c Debug --project ".\KeyLogger\KeyLogger\KeyLogger.csproj"

Kullanım

1) Uygulamayı başlatın.
2) “Kaydı Başlat” butonuna tıklayın, çıkan rıza penceresinde onay verin (not: 50 karaktere ulaşıldığında log otomatik e‑posta ile gönderilir).
3) Pencere odaktayken yazdıklarınız (şifre alanları hariç) kayıt altına alınır.
4) “Logu Aç” ile şifreli log dosyasındaki girdiler çözülerek okunabilir şekilde görüntülenir.
5) “Kaydı Durdur” ile oturumu sonlandırın; kapanış başlığı eklenir.

Kısayollar:
- Ctrl+L: Logu Aç
- Ctrl+D: Logu Temizle
- Ctrl+E: E‑postaya Gönder
- Ctrl+,: Ayarlar
- Alt+F4: Çıkış

Ayarlar

Ayarlar uygulama içinden “Görünüm > Ayarlar” ile açılır, şu konuma JSON olarak kaydedilir:
- %AppData%\SafeTypeRecorder\settings.json

Genel:
- Log Klasörü: .dat log’un tutulacağı klasör.
- Arşiv Klasörü: Rotasyon sonrası taşınacak klasör.
- Maks. Boyut (MB): Bu boyut aşıldığında log döndürülür (time‑stamp ile yeni dosyaya taşınır).
- Tema: Light/Dark.
- Font Boyutu: Tüm arayüzde uygulanır.

E‑posta (SMTP):
- Sunucu (Host), Port, SSL
- Kullanıcı, Şifre (DPAPI ile korunur)
- Gönderen (From), Alıcı (Recipient)

E‑posta ile Gönderim

Dosya > “E‑postaya Gönder” veya araç çubuğundaki “Gönder” butonuyla:
1) Varsayılan olarak mevcut safe_type_log.dat seçili gelir, dilerseniz farklı .dat seçebilirsiniz.
2) SMTP ayarları kullanılarak dosya ekli e‑posta gönderilir.

Notlar:
- Gmail ve Office 365 gibi sağlayıcılarda genellikle “Uygulama Parolası” gerekir (normal şifre çalışmayabilir).
- “Şifre” alanı kullanıcı bağlamında DPAPI ile şifrelenerek settings.json içinde saklanır.

Loglama Ayrıntıları

- Format (UI’de sade görüntü):
  - Olaylar: HH:mm:ss KeyDown A Mod:-, HH:mm:ss KeyPress 'a'
  - Başlıklar: -- HH:mm:ss Session <id> START -- ConsentUTC:<iso> ve -- HH:mm:ss Session <id> STOP --
- Dosya: varsayılan safe_type_log.md (Log Klasörü’nde)
- Rotasyon: Boyut limiti aşılınca SafeTypeRecorder_Archive (veya Ayarlarda seçilen klasör) altına safe_type_log_yyyyMMdd_HHmmss.dat adıyla taşınır.

Güvenlik ve Gizlilik

- Rıza ve Şeffaflık: Kayıt yalnızca kullanıcı onayı sonrası başlar ve sadece uygulama odaklıdır.
- Şifreli Depolama: Log satırları DPAPI CurrentUser kapsamında korunur; başka bir kullanıcı hesabında çözülemez.
- Hassas Alanlar: Şifre tipindeki TextBox alanları otomatik olarak atlanır.
- Telemetri Yok: İnternete otomatik veri gönderimi yoktur. E‑posta gönderimi kullanıcı onayı ile, elle başlatılır.
 - Otomatik e‑posta: Kullanıcı rızası alındıktan sonra, yerel SMTP ayarlarıyla 50 karakter eşiğinde tetiklenir.

Sorun Giderme

- Derleme hatası: PowerShell’de komutları ; ile ayırın (örn. dotnet restore; dotnet build).
- Çalışmıyor: .NET 8 kurulu olduğundan emin olun; Windows’ta Defender veya SmartScreen uyarılarına izin verin.
- SMTP hataları: Sunucu/port/SSL, kullanıcı ve uygulama parolasını kontrol edin; From adresini sağlayıcının kısıtlarına uygun seçin.
- Log görüntülenmiyor: safe_type_log.dat dosyasının varlığını ve erişim izinlerini kontrol edin.

Proje Yapısı

KeyLogger/
  KeyLogger.sln
  KeyLogger/
    KeyLogger.csproj
    Program.cs
    Form1.cs
    Form1.Designer.cs
    AppSettings.cs
    SettingsForm.cs
    bin/ Debug/
    obj/

Lisans ve Hukuki Not

Bu uygulama yalnızca açık rıza ile, uygulama odaklı metin kaydı amacıyla tasarlanmıştır. Sistem çapında veya arka plan tuş yakalama gibi kötüye kullanıma açık senaryolar hedeflenmemektedir. Kullanımınızın yerel yasal düzenlemelere uygun olduğundan emin olun.

Sık Sorulan Sorular (SSS)

- E‑posta (SMTP) alanlarına ne yazmalıyım?
  - Sunucu: SMTP sunucu adresiniz (ör. Gmail: smtp.gmail.com, Office 365: smtp.office365.com).
  - Port: Genellikle 587 (TLS/STARTTLS) veya 465 (SSL).
  - SSL: 587/465 için işaretli tutabilirsiniz; sağlayıcınıza göre değişir.
  - Kullanıcı: Çoğunlukla tam e‑posta adresiniz (ör. kullanici@alan.com).
  - Şifre: Normal hesabınızın şifresi değil; “Uygulama Parolası” kullanın.
  - Gönderen (From): Genellikle kullanıcı adresinizle aynı olmalı.
  - Alıcı (Recipient): Log’u göndermek istediğiniz e‑posta adresi.

- Gmail’de şifreyi nereden alacağım?
  - Google Hesabı > Güvenlik > 2 Adımlı Doğrulama’yı açın.
  - “Uygulama Şifreleri”nden posta için yeni şifre üretin; bu şifreyi Ayarlar’daki Şifre alanına yazın.

- Outlook/Office 365 için ne yapmalıyım?
  - Hesap/Güvenlik ayarlarında İki Aşamalı Doğrulama açık olmalı.
  - Güvenlik bilgilerine “App password” ekleyin ve oluşan parolayı kullanın.

- Gönderim başarısız olursa ne kontrol etmeliyim?
  - Sunucu, port, SSL ayarları; kullanıcı adı ve uygulama parolası.
  - “From” adresinizin sağlayıcı kısıtlarına uygun olduğundan emin olun.
  - Ağ/Firewall engelleri ve iki aşamalı doğrulama gereklilikleri.

- Log dosyası nerede, adı ne?
  - Varsayılan: Belgelerim (Documents) içinde safe_type_log.md (Ayarlar’dan değiştirilebilir).



- Log rotasyonu nasıl çalışır?
  - Maksimum boyut aşılınca mevcut dosya zaman damgalı adla Arşiv Klasörü’ne taşınır ve yeni log başlatılır.

- Tema/Font ayarı tüm arayüze uygulanıyor mu?
  - Evet. Açık/Koyu ve yazı tipi boyutu menü/toolbar/status dahil uygular.

- Hassas alanlar kaydediliyor mu?
  - Hayır. TextBox şifre alanları (UseSystemPasswordChar/PasswordChar) otomatik atlanır.

