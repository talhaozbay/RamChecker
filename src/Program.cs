// RAM Danışmanı — giriş noktası, komut satırı ve konsol çıktısı.
// Exe /target:winexe derlenir; komut satırı seçenekleri verilirse pencere açılmadan
// çalışır ve çıktıyı (yönlendirilmişse) boruya/dosyaya ya da çağıran konsola yazar.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;

[assembly: AssemblyTitle("RAM Danışmanı")]
[assembly: AssemblyDescription("Bilgisayardaki RAM modüllerini okur, bu bilgisayara uygun RAM yükseltmesini anlatır.")]
[assembly: AssemblyProduct("RAM Danışmanı")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: ComVisible(false)]

namespace RamDanismani
{
    public static class Program
    {
        public const string Surum = "1.0";
        public const string ProgramAdi = "RAM Danışmanı";

        /// <summary>--ekran modunda true: hiçbir MessageBox açılmaz (otomasyonu kilitlemesin).</summary>
        public static bool SessizMod;

        /// <summary>Komut satırından açıldığında: çağıran konsola bağlandık mı.</summary>
        static bool konsolaBagli;
        static bool ciktiYonlendirildi;

        class Secenekler
        {
            public bool Metin;
            public bool Test;
            public bool Yardim;
            public string Html;
            public string Csv;
            public string Smbios;
            public string SmbiosKaydet;
            public string Ekran;
            public string Hata;
        }

        [STAThread]
        public static void Main(string[] args)
        {
            int kod;
            try
            {
                AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
                {
                    HataKaydet(e.ExceptionObject as Exception, "AppDomain.UnhandledException");
                };
                kod = Calistir(args ?? new string[0]);
            }
            catch (Exception ex)
            {
                HataKaydet(ex, "Main");
                try { Console.Error.WriteLine("HATA: " + ex.Message + " (ayrıntı: " + HataDosyasi + ")"); }
                catch { }
                kod = 1;
            }
            try { Console.Out.Flush(); Console.Error.Flush(); }
            catch { }
            Environment.Exit(kod);
        }

        static int Calistir(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Secenekler s = Ayristir(args);
            bool konsolIsi = s.Hata != null || s.Yardim || s.Metin || s.Test
                || s.Html != null || s.Csv != null || s.SmbiosKaydet != null;
            if (konsolIsi)
            {
                KonsoluHazirla();
                return KonsolCalistir(s);
            }
            return ArayuzCalistir(s);
        }

        // ---- Komut satırı ayrıştırma ---------------------------------------

        static Secenekler Ayristir(string[] args)
        {
            Secenekler s = new Secenekler();
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i] == null ? "" : args[i].Trim();
                if (a.Length == 0) continue;
                string ad = SecenekAdi(a);
                if (ad == null)
                {
                    s.Hata = "Tanınmayan bağımsız değişken: " + a;
                    return s;
                }
                switch (ad)
                {
                    case "metin":
                    case "text":
                        s.Metin = true;
                        break;
                    case "test":
                        s.Test = true;
                        break;
                    case "yardim":
                    case "yardım":
                    case "help":
                    case "h":
                    case "?":
                        s.Yardim = true;
                        break;
                    case "html":
                    case "csv":
                    case "smbios":
                    case "smbios-kaydet":
                    case "ekran":
                        {
                            string deger = i + 1 < args.Length ? args[i + 1] : null;
                            if (string.IsNullOrEmpty(deger) || deger.Trim().Length == 0 || deger.StartsWith("--", StringComparison.Ordinal))
                            {
                                s.Hata = "--" + ad + " seçeneğinden sonra bir " + (ad == "ekran" ? "klasör" : "dosya") + " yolu yazılmalı.";
                                return s;
                            }
                            i++;
                            deger = deger.Trim().Trim('"');
                            if (ad == "html") s.Html = deger;
                            else if (ad == "csv") s.Csv = deger;
                            else if (ad == "smbios") s.Smbios = deger;
                            else if (ad == "smbios-kaydet") s.SmbiosKaydet = deger;
                            else s.Ekran = deger;
                        }
                        break;
                    default:
                        s.Hata = "Tanınmayan seçenek: " + a;
                        return s;
                }
            }
            if (s.Hata == null && s.Ekran != null && (s.Metin || s.Test || s.Yardim || s.Html != null || s.Csv != null || s.SmbiosKaydet != null))
                s.Hata = "--ekran yalnızca tek başına ya da --smbios ile birlikte kullanılabilir.";
            return s;
        }

        static string SecenekAdi(string a)
        {
            if (a.StartsWith("--", StringComparison.Ordinal)) a = a.Substring(2);
            else if (a.StartsWith("-", StringComparison.Ordinal) || a.StartsWith("/", StringComparison.Ordinal)) a = a.Substring(1);
            else return null;
            return a.ToLowerInvariant();
        }

        public static string KullanimMetni()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(ProgramAdi + " " + Surum + " — RamChecker.exe");
            sb.AppendLine("Bilgisayardaki RAM'leri gösterir ve bu bilgisayara nasıl RAM alınacağını anlatır.");
            sb.AppendLine();
            sb.AppendLine("Kullanım:");
            sb.AppendLine("  RamChecker.exe                         Pencereyi açar");
            sb.AppendLine("  RamChecker.exe --metin                 Raporu düz metin olarak yazar");
            sb.AppendLine("  RamChecker.exe --html <dosya>          Tam raporu HTML dosyasına kaydeder");
            sb.AppendLine("  RamChecker.exe --csv <dosya>           CSV dosyasına bu bilgisayar için bir satır ekler");
            sb.AppendLine("                                         (dosya yoksa oluşturur; ağ paylaşımında toplu envanter için)");
            sb.AppendLine("  RamChecker.exe --smbios <dosya>        Canlı okuma yerine kayıtlı SMBIOS dosyasını kullanır");
            sb.AppendLine("                                         (diğer seçeneklerle ve pencereyle birlikte kullanılabilir)");
            sb.AppendLine("  RamChecker.exe --smbios-kaydet <dosya> Bu bilgisayarın SMBIOS tablosunu dosyaya kaydeder (destek için)");
            sb.AppendLine("  RamChecker.exe --test                  Gömülü testleri çalıştırır (çıkış kodu 0 = hepsi geçti)");
            sb.AppendLine("  RamChecker.exe --ekran <klasör>        Pencereyi açar, her sekmenin görüntüsünü PNG kaydeder, kapanır");
            sb.AppendLine("                                         (yalnız --smbios ile birleştirilebilir)");
            sb.AppendLine("  RamChecker.exe --yardim                Bu yardım");
            sb.AppendLine();
            sb.AppendLine("Örnekler:");
            sb.AppendLine("  RamChecker.exe --html \"%USERPROFILE%\\Desktop\\ram-raporu.html\"");
            sb.AppendLine("  RamChecker.exe --csv \\\\sunucu\\paylasim\\ram-envanteri.csv      (oturum açma betiğinde)");
            sb.AppendLine("  PowerShell:  & .\\RamChecker.exe --metin | Out-String");
            sb.AppendLine();
            sb.AppendLine("Not: Bu bir pencere programıdır; PowerShell ve cmd istemi çıktı boruya bağlanmadıkça bitmesini BEKLEMEZ.");
            sb.AppendLine("  PowerShell'de bekleme ve çıkış kodu için:  & .\\RamChecker.exe --csv <dosya> | Out-Null; $LASTEXITCODE");
            sb.AppendLine("     ya da:  (Start-Process .\\RamChecker.exe -ArgumentList '--csv','<dosya>' -Wait -PassThru).ExitCode");
            sb.AppendLine("  cmd isteminde:  start /wait RamChecker.exe ...   (.bat dosyası içinde gerekmez)");
            sb.AppendLine();
            sb.AppendLine("Çıkış kodları (yukarıdaki biçimde çağrıldığında): 0 başarılı, 1 hata, 2 hatalı kullanım.");
            sb.AppendLine("Kurulum ve yönetici izni gerektirmez; internete bağlanmaz, hiçbir veri göndermez.");
            return sb.ToString();
        }

        // ---- Konsol modu -----------------------------------------------------

        static int KonsolCalistir(Secenekler s)
        {
            if (s.Hata != null)
            {
                Console.Error.WriteLine("HATA: " + s.Hata);
                Console.Error.WriteLine();
                Console.Error.Write(KullanimMetni());
                if (!konsolaBagli && !ciktiYonlendirildi)
                    MesajGoster(s.Hata + "\n\n" + KullanimMetni(), MessageBoxIcon.Warning);
                return 2;
            }
            if (s.Yardim)
            {
                Console.Out.Write(KullanimMetni());
                if (!konsolaBagli && !ciktiYonlendirildi)
                    MesajGoster(KullanimMetni(), MessageBoxIcon.Information);
                return 0;
            }

            // --metin ile birlikte verilen durum iletileri metni bozmasın diye stderr'e gider.
            TextWriter durum = s.Metin ? Console.Error : Console.Out;
            int sonuc = 0;

            if (s.SmbiosKaydet != null)
            {
                bool ok = false;
                string yol = s.SmbiosKaydet;
                try { yol = Path.GetFullPath(s.SmbiosKaydet); ok = Smbios.DosyayaKaydet(yol); }
                catch (Exception ex) { HataKaydet(ex, "--smbios-kaydet"); }
                if (ok) durum.WriteLine("SMBIOS tablosu kaydedildi: " + yol);
                else { Console.Error.WriteLine("HATA: SMBIOS tablosu okunamadı ya da dosyaya yazılamadı: " + yol); sonuc = 1; }
            }

            if (s.Test)
            {
                int t;
                try { t = Testler.Calistir(); }
                catch (Exception ex)
                {
                    HataKaydet(ex, "--test");
                    Console.Error.WriteLine("HATA: Testler çalıştırılamadı: " + ex.Message);
                    t = 1;
                }
                if (t != 0) sonuc = t;
            }

            if (s.Metin || s.Html != null || s.Csv != null)
            {
                byte[] tablo = null;
                int major = 0, minor = 0;
                if (s.Smbios != null)
                {
                    string hata;
                    if (!SmbiosDosyasiOku(s.Smbios, out tablo, out major, out minor, out hata))
                    {
                        Console.Error.WriteLine("HATA: " + hata);
                        return 1;
                    }
                }

                Envanter e;
                DanismanSonucu d;
                try
                {
                    e = EnvanterTopla(tablo, major, minor);
                    d = Danisman.Degerlendir(e);
                }
                catch (Exception ex)
                {
                    HataKaydet(ex, "veri toplama");
                    Console.Error.WriteLine("HATA: Bellek bilgileri okunamadı: " + ex.Message);
                    return 1;
                }
                if (d == null) d = new DanismanSonucu();

                if (s.Metin)
                {
                    try
                    {
                        string metin = Rapor.Metin(e, d) ?? "";
                        Console.Out.Write(metin);
                        if (!metin.EndsWith("\n", StringComparison.Ordinal)) Console.Out.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        HataKaydet(ex, "--metin");
                        Console.Error.WriteLine("HATA: Metin rapor oluşturulamadı: " + ex.Message);
                        sonuc = 1;
                    }
                }

                if (s.Html != null)
                {
                    try
                    {
                        string yol = Path.GetFullPath(s.Html);
                        string klasor = Path.GetDirectoryName(yol);
                        if (!string.IsNullOrEmpty(klasor) && !Directory.Exists(klasor)) Directory.CreateDirectory(klasor);
                        File.WriteAllText(yol, Rapor.TamRaporHtml(e, d), new UTF8Encoding(true));
                        durum.WriteLine("HTML rapor kaydedildi: " + yol);
                    }
                    catch (Exception ex)
                    {
                        HataKaydet(ex, "--html");
                        Console.Error.WriteLine("HATA: HTML rapor kaydedilemedi: " + ex.Message);
                        sonuc = 1;
                    }
                }

                if (s.Csv != null)
                {
                    string hata = null;
                    bool ok = false;
                    string yol = s.Csv;
                    try
                    {
                        yol = Path.GetFullPath(s.Csv);
                        ok = CsvEkle(yol, Rapor.CsvBaslik(), Rapor.CsvSatir(e, d), out hata);
                    }
                    catch (Exception ex)
                    {
                        HataKaydet(ex, "--csv");
                        hata = ex.Message;
                    }
                    if (ok) durum.WriteLine("CSV dosyasına satır eklendi: " + yol);
                    else { Console.Error.WriteLine("HATA: CSV dosyasına yazılamadı (" + yol + "): " + hata); sonuc = 1; }
                }
            }
            return sonuc;
        }

        /// <summary>
        /// CSV'ye tek satır ekler. Dosya yoksa UTF-8 BOM + başlık satırıyla oluşturur.
        /// Aynı paylaşımdaki dosyaya başka bilgisayar o anda yazıyorsa (kilitli) 10 kez yeniden dener.
        /// </summary>
        public static bool CsvEkle(string yol, string baslik, string satir, out string hata)
        {
            hata = null;
            try
            {
                string klasor = Path.GetDirectoryName(Path.GetFullPath(yol));
                if (!string.IsNullOrEmpty(klasor) && !Directory.Exists(klasor)) Directory.CreateDirectory(klasor);
            }
            catch (Exception ex) { hata = ex.Message; return false; }

            Random rastgele = new Random(Environment.TickCount ^ Environment.MachineName.GetHashCode());
            UTF8Encoding utf8 = new UTF8Encoding(false);
            for (int deneme = 1; deneme <= 10; deneme++)
            {
                try
                {
                    using (FileStream fs = new FileStream(yol, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                    {
                        StringBuilder sb = new StringBuilder();
                        if (fs.Length == 0)
                        {
                            byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
                            fs.Write(bom, 0, bom.Length);
                            sb.Append(SatirSonu(baslik));
                        }
                        else
                        {
                            fs.Seek(-1, SeekOrigin.End);
                            int son = fs.ReadByte();
                            fs.Seek(0, SeekOrigin.End);
                            if (son != '\n') sb.Append("\r\n");
                        }
                        sb.Append(SatirSonu(satir));
                        byte[] veri = utf8.GetBytes(sb.ToString());
                        fs.Write(veri, 0, veri.Length);
                        fs.Flush();
                    }
                    try
                    {
                        // Önceki bir kilit hatasında bırakılan bekleyen satır artık geçersiz (yeni satır yazıldı).
                        string bekleyen = BekleyenYolu(yol);
                        if (File.Exists(bekleyen)) File.Delete(bekleyen);
                    }
                    catch { }
                    return true;
                }
                catch (DirectoryNotFoundException ex) { hata = ex.Message; return false; }
                catch (UnauthorizedAccessException ex) { hata = "Erişim izni yok: " + ex.Message; return false; }
                catch (IOException ex)
                {
                    // Büyük olasılıkla başka bir bilgisayar o anda yazıyor (dosya kilitli).
                    hata = ex.Message + " (" + deneme + " deneme)";
                    if (deneme < 10) Thread.Sleep(rastgele.Next(250, 1200));
                }
            }
            // Kilit hiç açılmadı (genelde dosya Excel'de açık): satır kaybolmasın, makineye özel yedek dosyaya yaz.
            hata = "Dosya başka bir programda (ör. Excel) açık olabilir; kapatıp tekrar deneyin. " + hata;
            try
            {
                string bekleyen = BekleyenYolu(yol);
                File.WriteAllText(bekleyen, new string((char)0xFEFF, 1) + SatirSonu(baslik) + SatirSonu(satir), utf8);
                hata += " — satır şu dosyaya yazıldı: " + bekleyen;
            }
            catch { }
            return false;
        }

        static string BekleyenYolu(string yol)
        {
            string tam = Path.GetFullPath(yol);
            string klasor = Path.GetDirectoryName(tam) ?? "";
            string makine = System.Text.RegularExpressions.Regex.Replace(Environment.MachineName ?? "PC", @"[^A-Za-z0-9_-]", "_");
            return Path.Combine(klasor, Path.GetFileNameWithoutExtension(tam) + ".bekleyen-" + makine + ".csv");
        }

        static string SatirSonu(string s)
        {
            return (s ?? "").TrimEnd('\r', '\n') + "\r\n";
        }

        // ---- Ortak veri toplama ---------------------------------------------

        /// <summary>smbiosTablo null ise canlı makine; değilse dosyadan gelen tablo (WMI/OS sorgulanmaz).</summary>
        public static Envanter EnvanterTopla(byte[] smbiosTablo, int major, int minor)
        {
            Envanter e = smbiosTablo != null ? Toplayici.Topla(smbiosTablo, major, minor, false) : Toplayici.Topla();
            return e ?? new Envanter();
        }

        public static bool SmbiosDosyasiOku(string yol, out byte[] tablo, out int major, out int minor, out string hata)
        {
            tablo = null; major = 0; minor = 0; hata = null;
            try
            {
                string tam = Path.GetFullPath(yol);
                if (!File.Exists(tam))
                {
                    hata = "SMBIOS dosyası bulunamadı: " + tam;
                    return false;
                }
                tablo = Smbios.DosyadanOku(tam, out major, out minor);
                if (tablo == null || tablo.Length == 0)
                {
                    hata = "SMBIOS dosyası boş ya da okunamadı: " + tam;
                    tablo = null;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                HataKaydet(ex, "--smbios");
                hata = "SMBIOS dosyası okunamadı (" + yol + "): " + ex.Message;
                tablo = null;
                return false;
            }
        }

        // ---- Pencere modu -----------------------------------------------------

        static int ArayuzCalistir(Secenekler s)
        {
            string ekran = null;
            if (s.Ekran != null)
            {
                SessizMod = true;
                try
                {
                    ekran = Path.GetFullPath(s.Ekran);
                    Directory.CreateDirectory(ekran);
                }
                catch (Exception ex)
                {
                    HataKaydet(ex, "--ekran klasörü");
                    return 1;
                }
            }

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object o, ThreadExceptionEventArgs e)
            {
                HataKaydet(e.Exception, "Application.ThreadException");
                MesajGoster("Beklenmeyen bir hata oluştu, ancak program çalışmaya devam ediyor.\n\n"
                    + (e.Exception == null ? "" : e.Exception.Message)
                    + "\n\nAyrıntılar şu dosyaya yazıldı:\n" + HataDosyasi, MessageBoxIcon.Error);
            };

            byte[] tablo = null;
            int major = 0, minor = 0;
            if (s.Smbios != null)
            {
                string hata;
                if (!SmbiosDosyasiOku(s.Smbios, out tablo, out major, out minor, out hata))
                {
                    MesajGoster(hata, MessageBoxIcon.Error);
                    return 1;
                }
            }

            using (AnaForm f = new AnaForm(tablo, major, minor, s.Smbios, ekran))
            {
                Application.Run(f);
                return f.CikisKodu;
            }
        }

        // ---- Hata kaydı ve iletiler -----------------------------------------

        public static string HataDosyasi
        {
            get
            {
                try { return Path.Combine(Path.GetTempPath(), "RamChecker-hata.txt"); }
                catch { return "RamChecker-hata.txt"; }
            }
        }

        /// <summary>Hata ayrıntısını %TEMP%\RamChecker-hata.txt dosyasına ekler. Asla istisna fırlatmaz.</summary>
        public static void HataKaydet(Exception ex, string yer)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("==== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + " — " + ProgramAdi + " " + Surum + " — " + yer);
                sb.AppendLine("Windows: " + Environment.OSVersion + (Environment.Is64BitOperatingSystem ? " (64 bit)" : " (32 bit)")
                    + ", .NET " + Environment.Version + ", süreç " + (Environment.Is64BitProcess ? "64" : "32") + " bit");
                sb.AppendLine("Komut satırı: " + Environment.CommandLine);
                sb.AppendLine(ex == null ? "(ayrıntı yok)" : ex.ToString());
                sb.AppendLine();
                File.AppendAllText(HataDosyasi, sb.ToString(), new UTF8Encoding(true));
            }
            catch { }
        }

        public static void MesajGoster(string metin, MessageBoxIcon simge)
        {
            MesajGoster(null, metin, simge);
        }

        public static void MesajGoster(IWin32Window sahip, string metin, MessageBoxIcon simge)
        {
            if (SessizMod) return;
            try
            {
                string baslik = simge == MessageBoxIcon.Error ? ProgramAdi + " — Hata"
                    : simge == MessageBoxIcon.Warning ? ProgramAdi + " — Uyarı" : ProgramAdi;
                if (sahip != null) MessageBox.Show(sahip, metin, baslik, MessageBoxButtons.OK, simge);
                else MessageBox.Show(metin, baslik, MessageBoxButtons.OK, simge);
            }
            catch { }
        }

        // ---- Konsol hazırlığı -------------------------------------------------

        const int STD_OUTPUT_HANDLE = -11;
        const int STD_ERROR_HANDLE = -12;
        const uint FILE_TYPE_DISK = 1;
        const uint FILE_TYPE_PIPE = 3;

        /// <summary>
        /// winexe olduğumuz için kendi konsolumuz yok. Önce çağıranın konsoluna bağlanmayı deneriz
        /// (AttachConsole) ve onun çıktı kod sayfasını öğreniriz. Çıktı yönlendirilmişse:
        ///  - dosyaya (cmd "> rapor.txt"): UTF-8
        ///  - boruya (PowerShell "| Out-String", cmd "| findstr"): okuyan tarafın çözdüğü kod sayfası,
        ///    yani konsolun çıktı kod sayfası ([Console]::OutputEncoding; Türkçe Windows'ta varsayılan 857,
        ///    kullanıcı UTF-8 yaptıysa 65001). Konsol yoksa UTF-8.
        /// Yönlendirilmemişse konsola WriteConsoleW ile Unicode yazarız (kod sayfasından bağımsız).
        /// </summary>
        static void KonsoluHazirla()
        {
            try
            {
                // Yönlendirme tutamaçlarını konsola bağlanmadan ÖNCE al (bağlanma onları değiştirebilir).
                uint outTur, errTur;
                IntPtr outH = StdTutamac(STD_OUTPUT_HANDLE, out outTur);
                IntPtr errH = StdTutamac(STD_ERROR_HANDLE, out errTur);

                bool bagli = YerelYontemler.AttachConsole(-1);
                int konsolKodSayfasi = 0;
                if (bagli)
                {
                    try { konsolKodSayfasi = (int)YerelYontemler.GetConsoleOutputCP(); }
                    catch { konsolKodSayfasi = 0; }
                }

                bool outYon = AkisiYonlendir(outH, outTur, true, konsolKodSayfasi);
                bool errYon = AkisiYonlendir(errH, errTur, false, konsolKodSayfasi);
                ciktiYonlendirildi = outYon;

                if (bagli && (!outYon || !errYon))
                {
                    KonsolYazici kw = new KonsolYazici();
                    if (kw.Gecerli)
                    {
                        konsolaBagli = true;
                        if (!outYon)
                        {
                            kw.Write("\r\n"); // istemi (prompt) ezmemek için
                            Console.SetOut(kw);
                        }
                        if (!errYon) Console.SetError(kw);
                    }
                }
                else if (bagli)
                {
                    YerelYontemler.FreeConsole(); // her iki akış da yönlendirilmiş: konsola gerek yok
                }
            }
            catch (Exception ex)
            {
                HataKaydet(ex, "konsol hazırlığı");
            }
        }

        static IntPtr StdTutamac(int std, out uint tur)
        {
            tur = 0;
            IntPtr h = YerelYontemler.GetStdHandle(std);
            if (h == IntPtr.Zero || h == new IntPtr(-1)) return IntPtr.Zero;
            tur = YerelYontemler.GetFileType(h);
            return h;
        }

        static bool AkisiYonlendir(IntPtr h, uint tur, bool cikti, int konsolKodSayfasi)
        {
            if (h == IntPtr.Zero) return false;
            if (tur != FILE_TYPE_DISK && tur != FILE_TYPE_PIPE) return false;

            Encoding kodlama = new UTF8Encoding(false);
            if (tur == FILE_TYPE_PIPE && konsolKodSayfasi > 0 && konsolKodSayfasi != 65001)
            {
                try { kodlama = Encoding.GetEncoding(konsolKodSayfasi); }
                catch { kodlama = new UTF8Encoding(false); }
            }
            Stream akis = new FileStream(new SafeFileHandle(h, false), FileAccess.Write, 1);
            StreamWriter w = new StreamWriter(akis, kodlama);
            w.AutoFlush = true;
            TextWriter yazici = kodlama.CodePage == 65001 ? (TextWriter)w : new KarakterDuzeltenYazici(w);
            if (cikti) Console.SetOut(yazici);
            else Console.SetError(yazici);
            return true;
        }
    }

    /// <summary>Çağıran konsola WriteConsoleW ile Unicode yazar (kod sayfasından bağımsız).</summary>
    class KonsolYazici : TextWriter
    {
        readonly SafeFileHandle tutamac;
        public readonly bool Gecerli;

        public KonsolYazici()
        {
            tutamac = YerelYontemler.CreateFile("CONOUT$", 0xC0000000u, 3u, IntPtr.Zero, 3u, 0u, IntPtr.Zero);
            Gecerli = tutamac != null && !tutamac.IsInvalid;
        }

        public override Encoding Encoding { get { return Encoding.Unicode; } }

        public override void Write(char value) { Write(new string(value, 1)); }

        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer == null || count <= 0) return;
            Write(new string(buffer, index, count));
        }

        public override void Write(string value)
        {
            if (string.IsNullOrEmpty(value) || !Gecerli) return;
            int i = 0;
            while (i < value.Length)
            {
                int n = Math.Min(8000, value.Length - i);
                uint yazilan;
                if (!YerelYontemler.WriteConsoleW(tutamac, value.Substring(i, n), (uint)n, out yazilan, IntPtr.Zero)) return;
                i += n;
            }
        }
    }

    /// <summary>OEM kod sayfasında olmayan tipografik karakterleri ASCII eşdeğerine çevirir (— → -, … → ...).</summary>
    class KarakterDuzeltenYazici : TextWriter
    {
        readonly TextWriter ic;
        public KarakterDuzeltenYazici(TextWriter ic) { this.ic = ic; }
        public override Encoding Encoding { get { return ic.Encoding; } }

        public override void Write(char value) { ic.Write(Duzelt(new string(value, 1))); }
        public override void Write(string value) { ic.Write(Duzelt(value)); }
        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer == null || count <= 0) return;
            ic.Write(Duzelt(new string(buffer, index, count)));
        }
        public override void Flush() { ic.Flush(); }

        static string Duzelt(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            StringBuilder sb = null;
            for (int i = 0; i < s.Length; i++)
            {
                string yerine = null;
                switch (s[i])
                {
                    case '—': case '–': case '−': yerine = "-"; break;
                    case '…': yerine = "..."; break;
                    case '“': case '”': case '„': yerine = "\""; break;
                    case '‘': case '’': yerine = "'"; break;
                    case '•': case '·': yerine = "*"; break;
                    case '→': yerine = "->"; break;
                    case '←': yerine = "<-"; break;
                    case '≥': yerine = ">="; break;
                    case '≤': yerine = "<="; break;
                    case '≈': yerine = "~"; break;
                    case '×': yerine = "x"; break;
                    case '✓': case '✔': yerine = "+"; break;
                    case '✗': case '✘': yerine = "x"; break;
                    case ' ': case ' ': yerine = " "; break;
                }
                if (yerine != null)
                {
                    if (sb == null) { sb = new StringBuilder(s.Length + 8); sb.Append(s, 0, i); }
                    sb.Append(yerine);
                }
                else if (sb != null) sb.Append(s[i]);
            }
            return sb == null ? s : sb.ToString();
        }
    }

    static class YerelYontemler
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        public static extern uint GetFileType(IntPtr hFile);

        [DllImport("kernel32.dll")]
        public static extern uint GetConsoleOutputCP();

        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
            IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool WriteConsoleW(SafeFileHandle hConsoleOutput, string lpBuffer, uint nNumberOfCharsToWrite,
            out uint lpNumberOfCharsWritten, IntPtr lpReserved);

        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out YerelDikdortgen pvAttribute, int cbAttribute);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct YerelDikdortgen
    {
        public int Sol, Ust, Sag, Alt;
    }
}
