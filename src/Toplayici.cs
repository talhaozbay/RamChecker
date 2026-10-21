// RAM Danışmanı — donanım bilgisi toplayıcı.
// SMBIOS (Smbios.cs) birincil kaynaktır; WMI eksikleri tamamlar ya da SMBIOS
// okunamazsa yedek olur. Canlı bellek kullanımı (KullanimOku) da buradadır.
// C# 5: $"" , ?. , => üye, nameof, out var YOK.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace RamDanismani
{
    public static class Toplayici
    {
        const long MB = 1024L * 1024L;

        // ==== Genel API ======================================================

        /// <summary>Canlı makineden tam envanter: SMBIOS + WMI + işletim sistemi + kullanım.</summary>
        public static Envanter Topla()
        {
            byte[] tablo = null;
            int major = 0, minor = 0;
            string hata = "";
            bool okundu = false;
            try
            {
                okundu = Smbios.HamOku(out tablo, out major, out minor);
            }
            catch (Exception ex)
            {
                okundu = false;
                hata = ex.GetType().Name + ": " + ex.Message;
            }
            if (!okundu) tablo = null;

            Envanter e = Topla(tablo, major, minor, true);
            if (!okundu)
            {
                string m = "SMBIOS tablosu okunamadı (GetSystemFirmwareTable)" + (hata.Length > 0 ? " — " + hata : "") +
                           "; bellek bilgisi WMI'dan alındı. WMI yalnız dolu yuvaları listeler, boş yuva bilgisi eksik olabilir.";
                e.ToplamaNotlari.Insert(0, m);
            }
            return e;
        }

        /// <summary>
        /// Verilen SMBIOS tablosundan envanter kurar. canliVeri=false: WMI/işletim sistemi SORGULANMAZ
        /// (testler ve --smbios dosyası için deterministik). true: SMBIOS verisi tablodan, gerisi canlı.
        /// </summary>
        public static Envanter Topla(byte[] smbiosTablo, int major, int minor, bool canliVeri)
        {
            Envanter e = new Envanter();
            e.Zaman = DateTime.Now;
            try
            {
                SmbiosUygula(e, smbiosTablo, major, minor);

                int wmiKasaKodu = 0, pcSistemTuru = 0;
                bool pilVar = false;
                string ekVmMetni = "";

                if (canliVeri)
                {
                    // WMI ayrı iş parçacığında, KENDİ envanter kopyasında çalışır (aynı SMBIOS tablosundan kurulur).
                    // Süre aşılırsa terk edilen iş parçacığı yalnız kendi kopyasına yazar; rapor/CSV SMBIOS'la üretilir.
                    WmiIsi isi = new WmiIsi();
                    byte[] tabloKopya = smbiosTablo;
                    DateTime zaman = e.Zaman;
                    Thread th = new Thread(delegate()
                    {
                        try
                        {
                            Envanter g = new Envanter();
                            g.Zaman = zaman;
                            SmbiosUygula(g, tabloKopya, major, minor);
                            WmiBellekTamamla(g);
                            WmiSistemTamamla(g, out isi.KasaKodu, out isi.PcSistemTuru, out isi.PilVar, out isi.EkVmMetni);
                            IsletimSistemiOku(g);
                            try { g.Kullanim = KullanimOku(); }
                            catch (Exception ex) { Not(g, "Bellek kullanımı okunamadı: " + ex.Message); }
                            isi.Sonuc = g;
                        }
                        catch (Exception ex) { isi.Hata = ex; }
                    });
                    th.IsBackground = true;
                    th.Start();
                    if (th.Join(TimeSpan.FromSeconds(WmiAzamiSureSn)) && isi.Sonuc != null)
                    {
                        e = isi.Sonuc;
                        wmiKasaKodu = isi.KasaKodu; pcSistemTuru = isi.PcSistemTuru; pilVar = isi.PilVar; ekVmMetni = isi.EkVmMetni ?? "";
                    }
                    else if (isi.Hata != null)
                        Not(e, "WMI toplama hatası: " + isi.Hata.GetType().Name + ": " + isi.Hata.Message + "; bilgiler yalnız SMBIOS'tan alındı.");
                    else
                        Not(e, "WMI " + WmiAzamiSureSn + " saniyede yanıt vermedi; bilgiler yalnız SMBIOS'tan alındı " +
                               "(kullanım, işletim sistemi ve takılı RAM bilgisi yok). WMI deposu bozuk olabilir.");
                }
                else
                {
                    Not(e, "Dosyadan okundu: yalnız SMBIOS tablosu kullanıldı; canlı WMI/işletim sistemi sorgusu yapılmadı " +
                           "(kullanım, işletim sistemi ve takılı RAM bilgisi yok).");
                }

                KasaBelirle(e.Sistem, wmiKasaKodu, pcSistemTuru, pilVar);
                SanalMakineBelirle(e.Sistem, ekVmMetni);
                if (e.Sistem.SanalMakine) Not(e, e.Sistem.SanalMakineNotu);
            }
            catch (Exception ex)
            {
                Not(e, "Toplama sırasında beklenmeyen hata: " + ex.GetType().Name + ": " + ex.Message);
            }

            Zenginlestir(e);
            return e;
        }

        /// <summary>
        /// Saf zenginleştirme (WMI/OS çağırmaz): üretici normalleştirme, parça numarası çözümü,
        /// eksik tür/form tamamlama, lehimli sezgisi ve platform (CPU) bilgisi.
        /// </summary>
        public static void Zenginlestir(Envanter e)
        {
            if (e == null) return;
            try
            {
                if (e.Sistem == null) e.Sistem = new SistemBilgisi();
                if (e.Yuvalar == null) e.Yuvalar = new List<BellekYuvasi>();
                if (e.Diziler == null) e.Diziler = new List<BellekDizisi>();
                if (e.ToplamaNotlari == null) e.ToplamaNotlari = new List<string>();
                if (e.Kullanim == null) e.Kullanim = new KullanimBilgisi();
                e.Yuvalar.RemoveAll(delegate(BellekYuvasi y) { return y == null; });

                foreach (BellekYuvasi y in e.Yuvalar)
                {
                    try { YuvaZenginlestir(e, y); }
                    catch (Exception ex) { Not(e, "Yuva '" + N(y.Yuva) + "' zenginleştirilemedi: " + ex.Message); }
                }

                foreach (BellekDizisi d in e.Diziler)
                {
                    if (d == null) continue;
                    if (string.IsNullOrEmpty(d.Kullanim) && d.KullanimKodu > 0) d.Kullanim = DiziKullanimAdi(d.KullanimKodu);
                    if (string.IsNullOrEmpty(d.EccTuru) && d.EccKodu > 0) d.EccTuru = EccAdi(d.EccKodu);
                }

                if (string.IsNullOrEmpty(e.Sistem.ModelAdi) || Doldurmalik(e.Sistem.ModelAdi))
                {
                    if (!Doldurmalik(e.Sistem.Model)) e.Sistem.ModelAdi = e.Sistem.Model.Trim();
                }

                PlatformBilgisi p = null;
                try { p = Platform.Bul(N(e.Sistem.IslemciAdi)); }
                catch (Exception ex) { Not(e, "İşlemci platform bilgisi bulunamadı: " + ex.Message); }
                e.Platform = p ?? new PlatformBilgisi();
            }
            catch (Exception ex)
            {
                Not(e, "Zenginleştirme hatası: " + ex.Message);
            }
        }

        // ==== SMBIOS =========================================================

        static void SmbiosUygula(Envanter e, byte[] tablo, int major, int minor)
        {
            if (tablo == null || tablo.Length == 0)
            {
                Not(e, "SMBIOS tablosu yok.");
                return;
            }
            SmbiosSonucu s = null;
            try { s = Smbios.Coz(tablo, major, minor); }
            catch (Exception ex) { Not(e, "SMBIOS çözümlenemedi: " + ex.Message); }
            if (s == null) return;

            if (s.Sistem != null) e.Sistem = s.Sistem;
            if (s.Diziler != null) e.Diziler.AddRange(s.Diziler.Where(delegate(BellekDizisi d) { return d != null; }));
            if (s.Yuvalar != null)
            {
                foreach (BellekYuvasi y in s.Yuvalar)
                {
                    if (y == null) continue;
                    if (string.IsNullOrEmpty(y.Kaynak)) y.Kaynak = "SMBIOS";
                    if (string.IsNullOrEmpty(y.UreticiHam) && !string.IsNullOrEmpty(y.Uretici)) y.UreticiHam = y.Uretici;
                    e.Yuvalar.Add(y);
                }
            }
            if (s.Notlar != null)
                foreach (string n in s.Notlar) Not(e, "SMBIOS: " + n);

            if (string.IsNullOrEmpty(e.Sistem.SmbiosSurum) && (s.Major > 0 || major > 0))
            {
                int ma = s.Major > 0 ? s.Major : major;
                int mi = s.Major > 0 ? s.Minor : minor;
                e.Sistem.SmbiosSurum = ma + "." + mi;
            }

            e.SmbiosOkundu = e.Yuvalar.Count > 0 || e.Diziler.Count > 0 ||
                             !string.IsNullOrEmpty(e.Sistem.Uretici) || !string.IsNullOrEmpty(e.Sistem.IslemciAdi);
            if (!e.SmbiosOkundu) Not(e, "SMBIOS tablosunda kullanılabilir yapı bulunamadı.");
            else if (e.Yuvalar.Count == 0) Not(e, "SMBIOS tablosunda bellek yuvası (tip 17) yok.");
        }

        // ==== WMI: bellek ====================================================

        static void WmiBellekTamamla(Envanter e)
        {
            List<BellekYuvasi> wmiYuvalar = new List<BellekYuvasi>();
            WmiSorgula("Win32_PhysicalMemory", e, delegate(ManagementBaseObject mo)
            {
                wmiYuvalar.Add(WmiYuvasi(mo));
            });

            List<BellekDizisi> wmiDiziler = new List<BellekDizisi>();
            WmiSorgula("Win32_PhysicalMemoryArray", e, delegate(ManagementBaseObject mo)
            {
                BellekDizisi d = new BellekDizisi();
                long kb = Sayi(mo, "MaxCapacityEx");
                if (kb <= 0) kb = Sayi(mo, "MaxCapacity");
                d.MaxKapasiteMB = kb > 0 ? kb / 1024 : 0;
                d.YuvaSayisi = (int)Sayi(mo, "MemoryDevices");
                d.EccKodu = (int)Sayi(mo, "MemoryErrorCorrection");
                d.EccTuru = d.EccKodu > 0 ? EccAdi(d.EccKodu) : "";
                d.KullanimKodu = (int)Sayi(mo, "Use");
                d.Kullanim = d.KullanimKodu > 0 ? DiziKullanimAdi(d.KullanimKodu) : "";
                int konum = (int)Sayi(mo, "Location");
                d.Konum = konum > 0 ? Smbios.DiziKonumuAdi(konum) : "";
                wmiDiziler.Add(d);
            });

            if (e.Diziler.Count == 0 && wmiDiziler.Count > 0)
            {
                e.Diziler.AddRange(wmiDiziler);
                Not(e, "Bellek dizisi bilgisi (azami kapasite, yuva sayısı) WMI'dan alındı.");
            }

            int smbiosDolu = e.Yuvalar.Count(delegate(BellekYuvasi y) { return y.Dolu; });
            int wmiDolu = wmiYuvalar.Count(delegate(BellekYuvasi y) { return y.Dolu; });

            if (e.Yuvalar.Count == 0 || (smbiosDolu == 0 && wmiDolu > 0))
            {
                // WMI yedeği
                if (e.Yuvalar.Count > 0)
                    Not(e, "SMBIOS'ta dolu yuva görünmüyor ama WMI'da " + wmiDolu + " modül var; WMI bilgisi kullanıldı.");
                e.Yuvalar.Clear();
                e.Yuvalar.AddRange(wmiYuvalar);
                if (wmiYuvalar.Count > 0)
                    Not(e, "Bellek modülleri WMI'dan (Win32_PhysicalMemory) okundu.");

                int bildirilenYuva = 0;
                foreach (BellekDizisi d in e.Diziler)
                    if (d.KullanimKodu == 3 || d.KullanimKodu == 0 || d.KullanimKodu == 2) bildirilenYuva += Math.Max(0, d.YuvaSayisi);
                int eksik = bildirilenYuva - e.Yuvalar.Count;
                for (int i = 1; i <= eksik && i <= 64; i++)
                {
                    BellekYuvasi b = new BellekYuvasi();
                    b.Dolu = false;
                    b.Yuva = "Bilinmeyen boş yuva " + i;
                    b.Kaynak = "WMI";
                    b.DiziHandle = 0xFFFE;
                    e.Yuvalar.Add(b);
                }
                if (eksik > 0)
                    Not(e, "BIOS " + bildirilenYuva + " yuva bildiriyor, WMI " + wmiYuvalar.Count +
                           " modül listeledi; " + eksik + " boş yuva eklendi (yuva adları bilinmiyor).");
                return;
            }

            // SMBIOS başarılı: boş kalan alanları WMI ile doldur (DeviceLocator ile eşle).
            foreach (BellekYuvasi w in wmiYuvalar)
            {
                if (!w.Dolu) continue;
                BellekYuvasi s = Esle(e.Yuvalar, w);
                if (s != null && s.BoyutMB <= 0 && w.BoyutMB > 0)
                {
                    s.BoyutMB = w.BoyutMB;
                    s.Dolu = true;
                    Not(e, "Yuva '" + s.Yuva + "': boyut SMBIOS'ta bilinmiyordu, WMI'dan tamamlandı.");
                }
                if (s == null || !s.Dolu) continue;
                List<string> doldurulan = new List<string>();
                if (Doldurmalik(s.UreticiHam) && !Doldurmalik(w.UreticiHam)) { s.UreticiHam = w.UreticiHam; s.Uretici = w.UreticiHam; doldurulan.Add("üretici"); }
                if (Doldurmalik(s.ParcaNo) && !Doldurmalik(w.ParcaNo)) { s.ParcaNo = w.ParcaNo; doldurulan.Add("parça no"); }
                if (Doldurmalik(s.SeriNo) && !Doldurmalik(w.SeriNo)) { s.SeriNo = w.SeriNo; doldurulan.Add("seri no"); }
                if (s.HizMTs <= 0 && w.HizMTs > 0) { s.HizMTs = w.HizMTs; doldurulan.Add("hız"); }
                if (s.YapilandirilmisHizMTs <= 0 && w.YapilandirilmisHizMTs > 0) { s.YapilandirilmisHizMTs = w.YapilandirilmisHizMTs; doldurulan.Add("çalışma hızı"); }
                if (s.YapilandirilmisVoltajMv <= 0 && w.YapilandirilmisVoltajMv > 0) { s.YapilandirilmisVoltajMv = w.YapilandirilmisVoltajMv; doldurulan.Add("voltaj"); }
                if (s.MinVoltajMv <= 0 && w.MinVoltajMv > 0) s.MinVoltajMv = w.MinVoltajMv;
                if (s.MaxVoltajMv <= 0 && w.MaxVoltajMv > 0) s.MaxVoltajMv = w.MaxVoltajMv;
                if (s.VeriGenisligi <= 0 && w.VeriGenisligi > 0) s.VeriGenisligi = w.VeriGenisligi;
                if (s.ToplamGenislik <= 0 && w.ToplamGenislik > 0) s.ToplamGenislik = w.ToplamGenislik;
                if (s.Rank <= 0 && w.Rank > 0) { s.Rank = w.Rank; doldurulan.Add("rank"); }
                if (TurBilinmiyor(s) && w.TurKodu > 2) { s.TurKodu = w.TurKodu; s.Tur = TurAdi(w.TurKodu); doldurulan.Add("tür"); }
                if ((s.FormFaktorKodu == 0 || s.FormFaktorKodu == 2) && w.FormFaktorKodu > 2) { s.FormFaktorKodu = w.FormFaktorKodu; s.FormFaktor = FormFaktorAdi(w.FormFaktorKodu); doldurulan.Add("form faktörü"); }
                if (s.TurDetayBayraklari == 0 && w.TurDetayBayraklari != 0) { s.TurDetayBayraklari = w.TurDetayBayraklari; s.TurDetay.Clear(); }
                if (doldurulan.Count > 0)
                    Not(e, "Yuva '" + s.Yuva + "': " + string.Join(", ", doldurulan.ToArray()) + " SMBIOS'ta boştu, WMI'dan tamamlandı.");
            }
        }

        static BellekYuvasi Esle(List<BellekYuvasi> liste, BellekYuvasi w)
        {
            string yer = N(w.Yuva).Trim();
            if (yer.Length == 0) return null;
            List<BellekYuvasi> aday = liste.FindAll(delegate(BellekYuvasi y)
            {
                return string.Equals(N(y.Yuva).Trim(), yer, StringComparison.OrdinalIgnoreCase);
            });
            if (aday.Count == 1) return aday[0];
            if (aday.Count == 0) return null;
            string banka = N(w.Banka).Trim();
            List<BellekYuvasi> tam = aday.FindAll(delegate(BellekYuvasi y)
            {
                return string.Equals(N(y.Banka).Trim(), banka, StringComparison.OrdinalIgnoreCase);
            });
            return tam.Count == 1 ? tam[0] : null;
        }

        static BellekYuvasi WmiYuvasi(ManagementBaseObject mo)
        {
            BellekYuvasi y = new BellekYuvasi();
            y.Kaynak = "WMI";
            y.DiziHandle = 0xFFFE;
            long bayt = Sayi(mo, "Capacity");
            y.BoyutMB = bayt > 0 ? bayt / MB : 0;
            y.Dolu = y.BoyutMB > 0;
            y.Yuva = Yazi(mo, "DeviceLocator");
            y.Banka = Yazi(mo, "BankLabel");

            int tur = (int)Sayi(mo, "SMBIOSMemoryType");   // Windows 10+
            if (tur <= 2)
            {
                int eski = WmiBellekTurunuSmbiosaCevir((int)Sayi(mo, "MemoryType"));
                if (eski > 2 || tur == 0) tur = eski;
            }
            y.TurKodu = tur;
            y.Tur = tur > 0 ? TurAdi(tur) : "";
            y.FormFaktorKodu = WmiFormFaktorunuSmbiosaCevir((int)Sayi(mo, "FormFactor"));
            y.FormFaktor = FormFaktorAdi(y.FormFaktorKodu);
            y.TurDetayBayraklari = (int)Sayi(mo, "TypeDetail");
            y.HizMTs = (int)Sayi(mo, "Speed");
            y.YapilandirilmisHizMTs = (int)Sayi(mo, "ConfiguredClockSpeed");
            y.UreticiHam = Yazi(mo, "Manufacturer");
            y.Uretici = y.UreticiHam;
            y.ParcaNo = Yazi(mo, "PartNumber");
            y.SeriNo = Yazi(mo, "SerialNumber");
            y.YapilandirilmisVoltajMv = (int)Sayi(mo, "ConfiguredVoltage");
            y.MinVoltajMv = (int)Sayi(mo, "MinVoltage");
            y.MaxVoltajMv = (int)Sayi(mo, "MaxVoltage");
            y.VeriGenisligi = (int)Sayi(mo, "DataWidth");
            y.ToplamGenislik = (int)Sayi(mo, "TotalWidth");
            y.Rank = (int)(Sayi(mo, "Attributes") & 0x0F);
            if (y.VeriGenisligi == 0xFFFF) y.VeriGenisligi = 0;
            if (y.ToplamGenislik == 0xFFFF) y.ToplamGenislik = 0;
            return y;
        }

        /// <summary>WMI (CIM) FormFactor kodu → SMBIOS tip 17 form faktörü kodu. İki kodlama FARKLIDIR.</summary>
        internal static int WmiFormFaktorunuSmbiosaCevir(int wmi)
        {
            switch (wmi)
            {
                case 0: return 0x02;   // Unknown
                case 1: return 0x01;   // Other
                case 2: return 0x04;   // SIP
                case 3: return 0x06;   // DIP
                case 4: return 0x07;   // ZIP
                case 5: return 0x05;   // SOJ → Chip
                case 6: return 0x08;   // Proprietary → Proprietary Card
                case 7: return 0x03;   // SIMM
                case 8: return 0x09;   // DIMM
                case 9: return 0x0A;   // TSOP
                case 10: return 0x05;  // PGA → Chip
                case 11: return 0x0C;  // RIMM
                case 12: return 0x0D;  // SODIMM
                case 13: return 0x0E;  // SRIMM
                case 14: case 15: case 16: case 17: case 18: case 19: case 20: case 21: case 22: case 23:
                    return 0x05;       // SMD, SSMP, QFP, TQFP, SOIC, LCC, PLCC, BGA, FPBGA, LGA → çip (lehimli)
                default: return 0x02;
            }
        }

        /// <summary>Eski WMI MemoryType kodu → SMBIOS bellek türü kodu (SMBIOSMemoryType yoksa).</summary>
        internal static int WmiBellekTurunuSmbiosaCevir(int wmi)
        {
            switch (wmi)
            {
                case 0: return 0x02;
                case 1: return 0x01;
                case 2: return 0x03;   // DRAM
                case 3: return 0x0F;   // Synchronous DRAM
                case 6: return 0x04;   // EDRAM
                case 7: return 0x05;   // VRAM
                case 8: return 0x06;   // SRAM
                case 9: return 0x07;   // RAM
                case 10: return 0x08;  // ROM
                case 11: return 0x09;  // Flash
                case 12: return 0x0A;  // EEPROM
                case 13: return 0x0B;  // FEPROM
                case 14: return 0x0C;  // EPROM
                case 15: return 0x0D;  // CDRAM
                case 16: return 0x0E;  // 3DRAM
                case 17: return 0x0F;  // SDRAM
                case 18: return 0x10;  // SGRAM
                case 19: return 0x11;  // RDRAM
                case 20: return 0x12;  // DDR
                case 21: return 0x13;  // DDR2
                case 22: return 0x14;  // DDR2 FB-DIMM
                case 24: return 0x18;  // DDR3
                case 25: return 0x19;  // FBD2
                case 26: return 0x1A;  // DDR4 (bazı sürümler)
                default: return 0x01;
            }
        }

        // ==== WMI: sistem ====================================================

        static void WmiSistemTamamla(Envanter e, out int kasaKodu, out int pcSistemTuru, out bool pilVar, out string ekVmMetni)
        {
            SistemBilgisi s = e.Sistem;
            int kk = 0, pc = 0;
            bool pil = false;
            StringBuilder ek = new StringBuilder();

            WmiSorgula("Win32_ComputerSystem", e, delegate(ManagementBaseObject mo)
            {
                Tamamla(ref s.Uretici, Yazi(mo, "Manufacturer"));
                Tamamla(ref s.Model, Yazi(mo, "Model"));
                pc = (int)Sayi(mo, "PCSystemType");
                ek.Append(' ').Append(Yazi(mo, "Manufacturer")).Append(' ').Append(Yazi(mo, "Model"));
            });
            WmiSorgula("Win32_ComputerSystemProduct", e, delegate(ManagementBaseObject mo)
            {
                string surum = Yazi(mo, "Version");
                // Lenovo'da Version alanı kullanıcı dostu model adıdır ("LOQ 15IAX9E", "ThinkPad T14 Gen 3").
                if ((Doldurmalik(s.ModelAdi) || s.ModelAdi == s.Model) && !Doldurmalik(surum) &&
                    s.Uretici.IndexOf("LENOVO", StringComparison.OrdinalIgnoreCase) >= 0)
                    s.ModelAdi = surum;
                Tamamla(ref s.SeriNo, Yazi(mo, "IdentifyingNumber"));
                ek.Append(' ').Append(Yazi(mo, "Name")).Append(' ').Append(Yazi(mo, "Vendor"));
            });
            WmiSorgula("Win32_BIOS", e, delegate(ManagementBaseObject mo)
            {
                Tamamla(ref s.BiosSurum, Yazi(mo, "SMBIOSBIOSVersion"));
                Tamamla(ref s.BiosTarih, CimTarihi(Yazi(mo, "ReleaseDate")));
                if (string.IsNullOrEmpty(s.SmbiosSurum))
                {
                    long ma = Sayi(mo, "SMBIOSMajorVersion"), mi = Sayi(mo, "SMBIOSMinorVersion");
                    if (ma > 0) s.SmbiosSurum = ma + "." + mi;
                }
                ek.Append(' ').Append(Yazi(mo, "Manufacturer")).Append(' ').Append(Yazi(mo, "Version"))
                  .Append(' ').Append(Yazi(mo, "SMBIOSBIOSVersion"));
            });
            WmiSorgula("Win32_BaseBoard", e, delegate(ManagementBaseObject mo)
            {
                Tamamla(ref s.AnakartUretici, Yazi(mo, "Manufacturer"));
                Tamamla(ref s.AnakartModel, Yazi(mo, "Product"));
            });

            string cpuAdi = "";
            int cekirdek = 0, isParcacigi = 0;
            WmiSorgula("Win32_Processor", e, delegate(ManagementBaseObject mo)
            {
                if (cpuAdi.Length == 0) cpuAdi = TekBosluk(Yazi(mo, "Name"));
                cekirdek += (int)Sayi(mo, "NumberOfCores");
                isParcacigi += (int)Sayi(mo, "NumberOfLogicalProcessors");
            });
            if (Doldurmalik(s.IslemciAdi) && cpuAdi.Length > 0) s.IslemciAdi = cpuAdi;
            if (cekirdek > s.CekirdekSayisi) s.CekirdekSayisi = cekirdek;          // çok soketli: WMI toplamı
            if (isParcacigi > s.IsParcacigiSayisi) s.IsParcacigiSayisi = isParcacigi;

            WmiSorgula("Win32_SystemEnclosure", e, delegate(ManagementBaseObject mo)
            {
                object v = Ozellik(mo, "ChassisTypes");
                Array dizi = v as Array;
                if (dizi != null && kk == 0)
                {
                    foreach (object o in dizi)
                    {
                        int kod = 0;
                        try { kod = Convert.ToInt32(o, CultureInfo.InvariantCulture); } catch { }
                        if (kod > 2) { kk = kod; break; }
                    }
                }
            });
            WmiSorgula("Win32_Battery", e, delegate(ManagementBaseObject mo) { pil = true; });

            kasaKodu = kk;
            pcSistemTuru = pc;
            pilVar = pil;
            ekVmMetni = ek.ToString();
        }

        static void IsletimSistemiOku(Envanter e)
        {
            SistemBilgisi s = e.Sistem;
            try { s.Isletim64Bit = Environment.Is64BitOperatingSystem; } catch { }

            WmiSorgula("Win32_OperatingSystem", e, delegate(ManagementBaseObject mo)
            {
                string ad = Yazi(mo, "Caption"), surum = Yazi(mo, "Version");
                s.IsletimSistemi = (ad + " " + surum).Trim();
            });
            if (s.IsletimSistemi.Length == 0)
            {
                try { s.IsletimSistemi = Environment.OSVersion.VersionString; } catch { }
            }

            try
            {
                ulong kb;
                if (GetPhysicallyInstalledSystemMemory(out kb) && kb > 0) s.TakiliRamMB = (long)(kb / 1024UL);
                else Not(e, "Takılı RAM miktarı (GetPhysicallyInstalledSystemMemory) okunamadı.");
            }
            catch (Exception ex) { Not(e, "Takılı RAM miktarı okunamadı: " + ex.Message); }

            try
            {
                BellekDurumu d = new BellekDurumu();
                d.Boyut = (uint)Marshal.SizeOf(typeof(BellekDurumu));
                if (GlobalMemoryStatusEx(ref d)) s.KullanilabilirRamMB = (long)(d.ToplamFiziksel / (ulong)MB);
            }
            catch (Exception ex) { Not(e, "Kullanılabilir RAM okunamadı: " + ex.Message); }
        }

        // ==== Kasa ve sanal makine ===========================================

        /// <summary>SMBIOS tip 3 kasa kodu → KasaTipi. Üst bit (kilit) yok sayılır.</summary>
        internal static KasaTipi KasaKodundan(int kod)
        {
            return Smbios.KasaTipiBul(kod);   // tek tablo: Smbios.cs
        }

        /// <summary>Win32_ComputerSystem.PCSystemType → KasaTipi.</summary>
        internal static KasaTipi PcSistemTuruKasa(int tur)
        {
            switch (tur)
            {
                case 1: case 3: return KasaTipi.Masaustu;
                case 2: return KasaTipi.Dizustu;
                case 4: case 5: return KasaTipi.Sunucu;
                case 6: return KasaTipi.MiniPc;
                case 8: return KasaTipi.Tablet;
                default: return KasaTipi.Bilinmiyor;
            }
        }

        /// <summary>SMBIOS kasa kodu öncelikli; bilinmiyorsa WMI kasa kodu, PCSystemType ve pil varlığı.</summary>
        internal static void KasaBelirle(SistemBilgisi s, int wmiKasaKodu, int pcSistemTuru, bool pilVar)
        {
            if (s == null) return;
            KasaTipi k = KasaKodundan(s.KasaTuruKodu);
            if (k == KasaTipi.Bilinmiyor && s.Kasa != KasaTipi.SanalMakine) k = s.Kasa;
            if (k == KasaTipi.Bilinmiyor && wmiKasaKodu > 0)
            {
                k = KasaKodundan(wmiKasaKodu);
                if (k != KasaTipi.Bilinmiyor && (s.KasaTuruKodu & 0x7F) <= 2) s.KasaTuruKodu = wmiKasaKodu;
            }
            if (k == KasaTipi.Bilinmiyor) k = PcSistemTuruKasa(pcSistemTuru);
            if (k == KasaTipi.Bilinmiyor && pilVar) k = KasaTipi.Dizustu;
            s.Kasa = k;
        }

        /// <summary>Üretici/model/BIOS metinlerinden sanal makine tespiti. ek: WMI'dan ek metin (BIOS üreticisi vb.).</summary>
        internal static void SanalMakineBelirle(SistemBilgisi s, string ek)
        {
            if (s == null) return;
            string metin = (N(s.Uretici) + " " + N(s.Model) + " " + N(s.ModelAdi) + " " + N(s.BiosSurum) + " " +
                            N(s.AnakartUretici) + " " + N(s.AnakartModel) + " " + N(ek)).ToUpperInvariant();
            HashSet<string> sozcukler = new HashSet<string>(Regex.Split(metin, "[^A-Z0-9]+"), StringComparer.Ordinal);

            string ad = "";
            if (metin.Contains("VMWARE")) ad = "VMware";
            else if (metin.Contains("VIRTUALBOX") || metin.Contains("INNOTEK")) ad = "VirtualBox";
            else if (metin.Contains("PARALLELS")) ad = "Parallels";
            else if (sozcukler.Contains("QEMU")) ad = "QEMU";
            else if (sozcukler.Contains("KVM")) ad = "KVM";
            else if (sozcukler.Contains("XEN") || metin.Contains("HVM DOMU")) ad = "Xen";
            else if (sozcukler.Contains("BOCHS")) ad = "Bochs";
            else if (metin.Contains("AMAZON EC2")) ad = "Amazon EC2";
            else if (metin.Contains("GOOGLE COMPUTE")) ad = "Google Compute Engine";
            else if (metin.Contains("VIRTUAL MACHINE")) ad = metin.Contains("MICROSOFT") ? "Microsoft Hyper-V" : "Sanal makine";

            if (ad.Length == 0) return;
            s.SanalMakine = true;
            s.Kasa = KasaTipi.SanalMakine;
            s.SanalMakineNotu = "Bu bilgisayar bir sanal makine (" + ad + "). RAM fiziksel modülle değil, sanal makine " +
                                "ayarlarından (hipervizör) belirlenir; RAM satın almak yerine sanal makineye ayrılan belleği artırın.";
        }

        // ==== Zenginleştirme ayrıntıları =====================================

        static void YuvaZenginlestir(Envanter e, BellekYuvasi y)
        {
            y.Yuva = N(y.Yuva).Trim();
            y.Banka = N(y.Banka).Trim();
            y.ParcaNo = N(y.ParcaNo).Trim();
            y.SeriNo = N(y.SeriNo).Trim();
            y.VarlikEtiketi = N(y.VarlikEtiketi).Trim();
            y.Kaynak = N(y.Kaynak);
            if (Doldurmalik(y.ParcaNo)) y.ParcaNo = "";
            if (Doldurmalik(y.SeriNo)) y.SeriNo = "";
            if (Doldurmalik(y.VarlikEtiketi)) y.VarlikEtiketi = "";
            if (y.TurDetay == null) y.TurDetay = new List<string>();

            if (string.IsNullOrEmpty(y.Tur) && y.TurKodu > 0) y.Tur = TurAdi(y.TurKodu);
            if (y.TurKodu == 0 && !string.IsNullOrEmpty(y.Tur)) y.TurKodu = TurKoduAdindan(y.Tur);
            if (string.IsNullOrEmpty(y.FormFaktor) && y.FormFaktorKodu > 0) y.FormFaktor = FormFaktorAdi(y.FormFaktorKodu);
            if (y.FormFaktorKodu == 0 && !string.IsNullOrEmpty(y.FormFaktor)) y.FormFaktorKodu = FormFaktorKoduAdindan(y.FormFaktor);
            if (string.IsNullOrEmpty(y.Teknoloji) && y.TeknolojiKodu > 0) y.Teknoloji = TeknolojiAdi(y.TeknolojiKodu);
            if (y.TurDetay.Count == 0 && y.TurDetayBayraklari != 0) y.TurDetay.AddRange(TurDetayAdlari(y.TurDetayBayraklari));

            // Üretici
            string ham = N(y.UreticiHam).Trim();
            if (ham.Length == 0) { ham = N(y.Uretici).Trim(); y.UreticiHam = ham; }
            string norm = null;
            try { norm = Ureticiler.Normallestir(ham, y.ModulUreticiId); }
            catch (Exception ex) { Not(e, "Üretici adı normalleştirilemedi (" + ham + "): " + ex.Message); norm = ham; }
            y.Uretici = norm ?? "";

            // Parça numarası
            y.ParcaBilgisi = null;
            if (y.Dolu && y.ParcaNo.Length > 0)
            {
                try { y.ParcaBilgisi = Ureticiler.ParcaNoCoz(y.ParcaNo, y.Uretici); }
                catch (Exception ex) { Not(e, "Parça numarası çözülemedi (" + y.ParcaNo + "): " + ex.Message); }
            }

            ParcaCozumu pb = y.ParcaBilgisi;
            if (y.Dolu && pb != null && pb.Bulundu)
            {
                string yer = YuvaEtiketi(y);
                if (y.Uretici.Length == 0 && !string.IsNullOrEmpty(pb.Marka))
                {
                    y.Uretici = pb.Marka;
                    Not(e, yer + ": üretici BIOS'ta yok; parça numarasından " + pb.Marka + " olarak belirlendi.");
                }
                if (TurBilinmiyor(y) && !string.IsNullOrEmpty(pb.Tur))
                {
                    y.Tur = pb.Tur;
                    int kod = TurKoduAdindan(pb.Tur);
                    if (kod > 0) y.TurKodu = kod;
                    Not(e, yer + ": bellek türü BIOS'ta bilinmiyor; parça numarasından " + pb.Tur + " olarak tamamlandı.");
                }
                if ((y.FormFaktorKodu == 0 || y.FormFaktorKodu == 2) && !string.IsNullOrEmpty(pb.FormFaktor))
                {
                    int kod = FormFaktorKoduAdindan(pb.FormFaktor);
                    if (kod > 0) y.FormFaktorKodu = kod;
                    y.FormFaktor = pb.FormFaktor;
                    Not(e, yer + ": form faktörü BIOS'ta bilinmiyor; parça numarasından " + pb.FormFaktor + " olarak tamamlandı.");
                }
                if (y.HizMTs <= 0 && pb.HizMTs > 0)
                {
                    y.HizMTs = pb.HizMTs;
                    Not(e, yer + ": modül hızı BIOS'ta yok; parça numarasından " + pb.HizMTs + " MT/s olarak tamamlandı.");
                }
                if (y.BoyutMB <= 0 && pb.KapasiteGB > 0)
                {
                    y.BoyutMB = pb.KapasiteGB * 1024L;
                    Not(e, yer + ": boyut BIOS'ta bilinmiyor; parça numarasından " + pb.KapasiteGB + " GB olarak tamamlandı (doğrulayın).");
                }
            }

            if (y.Dolu) HizBirimiDuzelt(e, y);

            LehimliBelirle(y);
        }

        /// <summary>
        /// Eski SMBIOS'larda (2.7–3.0) ofset 0x20 "Configured Memory Clock Speed" MHz cinsindendir; bazı BIOS'lar
        /// hâlâ saat frekansını (DDR4-2666 için 1333) yazar. Türün JEDEC alt sınırının altındaki değer kesinlikle
        /// MHz'dir ve iki katına çevrilir. Alt sınırın üstündeki tam yarı değerlere (DDR4 3200/1600) dokunulmaz.
        /// </summary>
        internal static void HizBirimiDuzelt(Envanter e, BellekYuvasi y)
        {
            int min = 0;
            switch (y.TurKodu)
            {
                case 0x13: min = 400; break;    // DDR2
                case 0x18: min = 800; break;    // DDR3
                case 0x1A: min = 1600; break;   // DDR4
                case 0x22: min = 3200; break;   // DDR5
            }
            if (min <= 0) return;
            string yer = YuvaEtiketi(y);
            if (y.HizMTs > 0 && y.HizMTs < min && y.HizMTs * 2 >= min)
            {
                y.HizMTs *= 2;
                Not(e, yer + ": BIOS modül hızını MHz cinsinden bildiriyor; MT/s'ye çevrildi (" + y.HizMTs + " MT/s).");
            }
            if (y.YapilandirilmisHizMTs > 0 && y.YapilandirilmisHizMTs < min &&
                y.YapilandirilmisHizMTs * 2 <= Math.Max(y.HizMTs, min * 2) * 1.02)
            {
                y.YapilandirilmisHizMTs *= 2;
                Not(e, yer + ": BIOS çalışma hızını MHz cinsinden bildiriyor; MT/s'ye çevrildi (" + y.YapilandirilmisHizMTs + " MT/s).");
            }
        }

        static readonly string[] LehimliAnahtarlari =
        {
            "ONBOARD", "ON BOARD", "ON-BOARD", "SOLDER", "BUILT-IN", "BUILT IN", "BUILTIN", "MEMORY DOWN", "MEMORYDOWN", "MEM DOWN"
        };

        static void LehimliBelirle(BellekYuvasi y)
        {
            y.Lehimli = false;
            y.LehimliSupheli = false;
            y.LehimliNedeni = "";
            if (!y.Dolu) return;

            int ff = y.FormFaktorKodu;
            string tur = N(y.Tur);
            bool lp = tur.StartsWith("LPDDR", StringComparison.OrdinalIgnoreCase) || LpTurKodu(y.TurKodu);

            if (ff == 0x11)   // CAMM (LPCAMM2 dahil) — vidalı, sökülebilir modül
                return;

            string konum = (y.Yuva + " " + y.Banka).ToUpperInvariant();
            string anahtar = null;
            foreach (string a in LehimliAnahtarlari)
                if (konum.IndexOf(a, StringComparison.Ordinal) >= 0) { anahtar = a; break; }

            if (lp)
            {
                y.Lehimli = true;
                y.LehimliNedeni = (tur.Length > 0 ? tur : "LPDDR") + " bellek anakarta lehimlidir; sökülüp değiştirilemez " +
                                  "(yalnız LPCAMM2 modülleri istisnadır).";
                if (anahtar == null && (ff == 0 || ff == 0x01 || ff == 0x02 || ff == 0x08))
                {
                    y.LehimliSupheli = true;
                    y.LehimliNedeni += " Form faktörü BIOS'ta belirsiz (" + (ff == 0 ? "boş" : FormFaktorAdi(ff)) +
                                       "); cihaz LPCAMM2 modül kullanıyor olabilir, üretici belgesinden doğrulayın.";
                }
                return;
            }
            if (anahtar != null)
            {
                y.Lehimli = true;
                y.LehimliNedeni = "Yuva/banka adı \"" + (y.Yuva + " " + y.Banka).Trim() + "\" anakarta gömülü (onboard) belleği gösteriyor.";
                return;
            }
            if (ff == 0x0B || ff == 0x05 || ff == 0x10 || ff == 0x0A)
            {
                y.Lehimli = true;
                y.LehimliNedeni = "Form faktörü \"" + (string.IsNullOrEmpty(y.FormFaktor) ? FormFaktorAdi(ff) : y.FormFaktor) +
                                  "\": bellek çipleri doğrudan anakart üzerinde, takılıp çıkarılan bir modül değil.";
                return;
            }
            // DIMM / SODIMM / FB-DIMM ve diğerleri: sökülebilir kabul edilir.
        }

        static bool LpTurKodu(int kod)
        {
            return kod == 0x1B || kod == 0x1C || kod == 0x1D || kod == 0x1E || kod == 0x23;
        }

        static bool TurBilinmiyor(BellekYuvasi y)
        {
            if (y.TurKodu > 2) return false;
            string t = N(y.Tur).Trim();
            return t.Length == 0 || string.Equals(t, "Unknown", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(t, "Other", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(t, "Bilinmiyor", StringComparison.OrdinalIgnoreCase);
        }

        static string YuvaEtiketi(BellekYuvasi y)
        {
            string ad = N(y.Yuva);
            if (ad.Length == 0) ad = N(y.Banka);
            return "Yuva '" + (ad.Length > 0 ? ad : "?") + "'";
        }

        // ==== Kod tabloları (SMBIOS 3.x) =====================================

        internal static string TurAdi(int kod)
        {
            switch (kod)
            {
                case 0x01: return "Other";
                case 0x02: return "Unknown";
                case 0x03: return "DRAM";
                case 0x04: return "EDRAM";
                case 0x05: return "VRAM";
                case 0x06: return "SRAM";
                case 0x07: return "RAM";
                case 0x08: return "ROM";
                case 0x09: return "FLASH";
                case 0x0A: return "EEPROM";
                case 0x0B: return "FEPROM";
                case 0x0C: return "EPROM";
                case 0x0D: return "CDRAM";
                case 0x0E: return "3DRAM";
                case 0x0F: return "SDRAM";
                case 0x10: return "SGRAM";
                case 0x11: return "RDRAM";
                case 0x12: return "DDR";
                case 0x13: return "DDR2";
                case 0x14: return "DDR2 FB-DIMM";
                case 0x18: return "DDR3";
                case 0x19: return "FBD2";
                case 0x1A: return "DDR4";
                case 0x1B: return "LPDDR";
                case 0x1C: return "LPDDR2";
                case 0x1D: return "LPDDR3";
                case 0x1E: return "LPDDR4";
                case 0x1F: return "Logical non-volatile device";
                case 0x20: return "HBM";
                case 0x21: return "HBM2";
                case 0x22: return "DDR5";
                case 0x23: return "LPDDR5";
                case 0x24: return "HBM3";
                default: return kod > 0 ? "Kod 0x" + kod.ToString("X2") : "";
            }
        }

        internal static int TurKoduAdindan(string ad)
        {
            string a = N(ad).Trim().ToUpperInvariant().Replace("-", "").Replace(" ", "");
            if (a.Length == 0) return 0;
            if (a == "LPDDR4X") return 0x1E;
            if (a == "LPDDR5X") return 0x23;
            for (int k = 1; k <= 0x24; k++)
            {
                string t = TurAdi(k).ToUpperInvariant().Replace("-", "").Replace(" ", "");
                if (t == a) return k;
            }
            return 0;
        }

        internal static string FormFaktorAdi(int kod)
        {
            switch (kod)
            {
                case 0x01: return "Other";
                case 0x02: return "Unknown";
                case 0x03: return "SIMM";
                case 0x04: return "SIP";
                case 0x05: return "Chip";
                case 0x06: return "DIP";
                case 0x07: return "ZIP";
                case 0x08: return "Proprietary Card";
                case 0x09: return "DIMM";
                case 0x0A: return "TSOP";
                case 0x0B: return "Row of chips";
                case 0x0C: return "RIMM";
                case 0x0D: return "SODIMM";
                case 0x0E: return "SRIMM";
                case 0x0F: return "FB-DIMM";
                case 0x10: return "Die";
                case 0x11: return "CAMM";
                default: return kod > 0 ? "Kod 0x" + kod.ToString("X2") : "";
            }
        }

        internal static int FormFaktorKoduAdindan(string ad)
        {
            string a = N(ad).Trim().ToUpperInvariant().Replace("-", "").Replace(" ", "");
            if (a.Length == 0) return 0;
            if (a == "SODIMM" || a == "SO DIMM" || a == "SODIMM") return 0x0D;
            if (a == "UDIMM" || a == "RDIMM" || a == "LRDIMM") return 0x09;
            if (a.StartsWith("LPCAMM", StringComparison.Ordinal) || a.StartsWith("CAMM", StringComparison.Ordinal)) return 0x11;
            for (int k = 1; k <= 0x11; k++)
            {
                string t = FormFaktorAdi(k).ToUpperInvariant().Replace("-", "").Replace(" ", "");
                if (t == a) return k;
            }
            return 0;
        }

        static string TeknolojiAdi(int kod)
        {
            switch (kod)
            {
                case 1: return "Other";
                case 2: return "Unknown";
                case 3: return "DRAM";
                case 4: return "NVDIMM-N";
                case 5: return "NVDIMM-F";
                case 6: return "NVDIMM-P";
                case 7: return "Intel Optane persistent memory";
                default: return "";
            }
        }

        static List<string> TurDetayAdlari(int bayrak)
        {
            string[] adlar =
            {
                "", "Other", "Unknown", "Fast-paged", "Static column", "Pseudo-static", "RAMBUS", "Synchronous",
                "CMOS", "EDO", "Window DRAM", "Cache DRAM", "Non-volatile", "Registered (Buffered)",
                "Unbuffered (Unregistered)", "LRDIMM"
            };
            List<string> l = new List<string>();
            for (int i = 1; i < adlar.Length; i++)
                if ((bayrak & (1 << i)) != 0) l.Add(adlar[i]);
            return l;
        }

        static string EccAdi(int kod)
        {
            switch (kod)
            {
                case 1: return "Other";
                case 2: return "Unknown";
                case 3: return "None";
                case 4: return "Parity";
                case 5: return "Single-bit ECC";
                case 6: return "Multi-bit ECC";
                case 7: return "CRC";
                default: return "";
            }
        }

        static string DiziKullanimAdi(int kod)
        {
            switch (kod)
            {
                case 1: return "Other";
                case 2: return "Unknown";
                case 3: return "System memory";
                case 4: return "Video memory";
                case 5: return "Flash memory";
                case 6: return "Non-volatile RAM";
                case 7: return "Cache memory";
                default: return "";
            }
        }

        // ==== Canlı kullanım =================================================

        static readonly object sayfaKilit = new object();
        static bool sayfaOkundu;
        static bool sayfaOkunuyor;
        static DateTime sayfaZamani = DateTime.MinValue;
        static long sayfaToplamMB, sayfaKullanilanMB;

        /// <summary>
        /// Canlı bellek kullanımı (2 sn'de bir çağrılabilir, hızlıdır). Sayfa dosyası bilgisi WMI'dan
        /// ilk çağrıda okunur, sonra önbellekten verilir ve arka planda seyrek tazelenir.
        /// </summary>
        public static KullanimBilgisi KullanimOku()
        {
            KullanimBilgisi k = new KullanimBilgisi();
            k.Zaman = DateTime.Now;
            ulong sayfaToplamBayt = 0, sayfaBosBayt = 0;
            try
            {
                BellekDurumu d = new BellekDurumu();
                d.Boyut = (uint)Marshal.SizeOf(typeof(BellekDurumu));
                if (GlobalMemoryStatusEx(ref d))
                {
                    k.ToplamMB = (long)(d.ToplamFiziksel / (ulong)MB);
                    k.BosMB = (long)(d.BosFiziksel / (ulong)MB);
                    k.KullanilanMB = Math.Max(0, k.ToplamMB - k.BosMB);
                    k.YuzdeKullanim = (int)d.YukYuzdesi;
                    sayfaToplamBayt = d.ToplamSayfaDosyasi;
                    sayfaBosBayt = d.BosSayfaDosyasi;
                    k.TaahhutSiniriMB = (long)(sayfaToplamBayt / (ulong)MB);
                    k.TaahhutMB = (long)((sayfaToplamBayt - Math.Min(sayfaToplamBayt, sayfaBosBayt)) / (ulong)MB);
                }
            }
            catch { }

            try
            {
                PerformansBilgisi p;
                if (PerformansOku(out p))
                {
                    ulong sayfa = p.SayfaBoyutu.ToUInt64();
                    if (sayfa == 0) sayfa = 4096;
                    k.TaahhutMB = (long)(p.TaahhutToplam.ToUInt64() * sayfa / (ulong)MB);
                    k.TaahhutSiniriMB = (long)(p.TaahhutSinir.ToUInt64() * sayfa / (ulong)MB);
                    k.OnbellekMB = (long)(p.SistemOnbellek.ToUInt64() * sayfa / (ulong)MB);
                }
            }
            catch { }

            SayfaDosyasiGuncelle();
            lock (sayfaKilit)
            {
                k.SayfaDosyasiMB = sayfaToplamMB;
                k.SayfaDosyasiKullanilanMB = sayfaKullanilanMB;
            }
            if (k.SayfaDosyasiMB <= 0 && k.TaahhutSiniriMB > k.ToplamMB)
                k.SayfaDosyasiMB = k.TaahhutSiniriMB - k.ToplamMB;   // yaklaşık: sınır = RAM + sayfa dosyası

            SurecleriOku(k);
            if (k.YuzdeKullanim <= 0 && k.ToplamMB > 0)
                k.YuzdeKullanim = (int)Math.Round(100.0 * k.KullanilanMB / k.ToplamMB);
            return k;
        }

        static bool PerformansOku(out PerformansBilgisi p)
        {
            p = new PerformansBilgisi();
            uint boyut = (uint)Marshal.SizeOf(typeof(PerformansBilgisi));
            p.Boyut = boyut;
            try { return K32GetPerformanceInfo(out p, boyut); }
            catch (EntryPointNotFoundException) { }
            catch (DllNotFoundException) { }
            try
            {
                p = new PerformansBilgisi();
                p.Boyut = boyut;
                return GetPerformanceInfo(out p, boyut);
            }
            catch { return false; }
        }

        static void SayfaDosyasiGuncelle()
        {
            bool ilk;
            lock (sayfaKilit)
            {
                if (sayfaOkunuyor) return;
                ilk = !sayfaOkundu;
                if (!ilk && (DateTime.UtcNow - sayfaZamani).TotalSeconds < 30) return;
                sayfaOkunuyor = true;
            }
            if (ilk)
            {
                SayfaDosyasiOku();
            }
            else
            {
                try { ThreadPool.QueueUserWorkItem(delegate { SayfaDosyasiOku(); }); }
                catch { lock (sayfaKilit) { sayfaOkunuyor = false; } }
            }
        }

        static void SayfaDosyasiOku()
        {
            long toplam = 0, kullanilan = 0;
            bool basarili = false;
            try
            {
                WmiSorgula("Win32_PageFileUsage", null, delegate(ManagementBaseObject mo)
                {
                    toplam += Sayi(mo, "AllocatedBaseSize");   // MB
                    kullanilan += Sayi(mo, "CurrentUsage");     // MB
                    basarili = true;
                }, 5);
                basarili = true;   // sayfa dosyası olmayabilir (0 kayıt) — bu da geçerli bir sonuç
            }
            catch { }
            lock (sayfaKilit)
            {
                if (basarili)
                {
                    sayfaToplamMB = toplam;
                    sayfaKullanilanMB = kullanilan;
                }
                sayfaOkundu = true;
                sayfaZamani = DateTime.UtcNow;
                sayfaOkunuyor = false;
            }
        }

        static void SurecleriOku(KullanimBilgisi k)
        {
            Process[] surecler = null;
            try { surecler = Process.GetProcesses(); }
            catch { return; }

            Dictionary<string, SurecBellek> gruplar = new Dictionary<string, SurecBellek>(StringComparer.OrdinalIgnoreCase);
            long ozelBayt, kumeBayt;
            foreach (Process p in surecler)
            {
                try
                {
                    int id = p.Id;
                    if (id == 0) continue;   // Idle
                    string ad = p.ProcessName;
                    ozelBayt = p.PrivateMemorySize64;
                    kumeBayt = p.WorkingSet64;
                    if (string.Equals(ad, "Memory Compression", StringComparison.OrdinalIgnoreCase))
                        k.SikistirilmisMB += kumeBayt / MB;

                    SurecBellek g;
                    if (!gruplar.TryGetValue(ad, out g))
                    {
                        g = new SurecBellek();
                        g.Ad = ad;
                        g.Adet = 0;
                        gruplar[ad] = g;
                    }
                    g.Adet++;
                    g.OzelMB += ozelBayt;          // önce bayt topla, sonra MB'ye çevir
                    g.CalismaKumesiMB += kumeBayt;
                }
                catch { }
                finally
                {
                    try { p.Dispose(); } catch { }
                }
            }

            List<SurecBellek> liste = new List<SurecBellek>(gruplar.Values);
            foreach (SurecBellek g in liste)
            {
                g.OzelMB = g.OzelMB / MB;
                g.CalismaKumesiMB = g.CalismaKumesiMB / MB;
            }
            k.EnCokKullananlar = liste
                .OrderByDescending(delegate(SurecBellek s) { return s.OzelMB; })
                .ThenByDescending(delegate(SurecBellek s) { return s.CalismaKumesiMB; })
                .Take(15)
                .ToList();
        }

        // ==== WMI yardımcıları ===============================================

        /// <summary>Canlı toplamada WMI bölümünün kesin üst süre sınırı (saniye).</summary>
        const int WmiAzamiSureSn = 30;

        sealed class WmiIsi
        {
            public Envanter Sonuc;
            public Exception Hata;
            public int KasaKodu, PcSistemTuru;
            public bool PilVar;
            public string EkVmMetni;
        }

        static void WmiSorgula(string sinif, Envanter e, Action<ManagementBaseObject> isle)
        {
            WmiSorgula(sinif, e, isle, 15);
        }

        static void WmiSorgula(string sinif, Envanter e, Action<ManagementBaseObject> isle, int zamanAsimiSn)
        {
            try
            {
                EnumerationOptions sec = new EnumerationOptions();
                sec.Timeout = TimeSpan.FromSeconds(zamanAsimiSn);
                sec.ReturnImmediately = true;
                sec.Rewindable = false;
                ConnectionOptions baglanti = new ConnectionOptions();
                baglanti.Timeout = TimeSpan.FromSeconds(10);
                ManagementScope kapsam = new ManagementScope(@"root\cimv2", baglanti);
                using (ManagementObjectSearcher ara = new ManagementObjectSearcher(kapsam, new ObjectQuery("SELECT * FROM " + sinif), sec))
                using (ManagementObjectCollection sonuc = ara.Get())
                {
                    foreach (ManagementBaseObject mo in sonuc)
                    {
                        try { isle(mo); }
                        catch (Exception ex) { Not(e, "WMI " + sinif + " kaydı işlenemedi: " + ex.Message); }
                        finally { try { mo.Dispose(); } catch { } }
                    }
                }
            }
            catch (Exception ex)
            {
                Not(e, "WMI " + sinif + " okunamadı: " + ex.Message);
            }
        }

        static object Ozellik(ManagementBaseObject mo, string ad)
        {
            try { return mo[ad]; }
            catch { return null; }   // eski Windows'ta olmayan özellik (ör. SMBIOSMemoryType)
        }

        static string Yazi(ManagementBaseObject mo, string ad)
        {
            object v = Ozellik(mo, ad);
            if (v == null) return "";
            try { return Convert.ToString(v, CultureInfo.InvariantCulture).Trim(); }
            catch { return ""; }
        }

        static long Sayi(ManagementBaseObject mo, string ad)
        {
            object v = Ozellik(mo, ad);
            if (v == null) return 0;
            try
            {
                if (v is ulong)
                {
                    ulong u = (ulong)v;
                    return u > (ulong)long.MaxValue ? 0 : (long)u;
                }
                return Convert.ToInt64(v, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }

        /// <summary>CIM tarih "20250123000000.000000+000" → "01/23/2025" (SMBIOS'taki biçim).</summary>
        static string CimTarihi(string cim)
        {
            if (string.IsNullOrEmpty(cim) || cim.Length < 8) return "";
            string yil = cim.Substring(0, 4), ay = cim.Substring(4, 2), gun = cim.Substring(6, 2);
            int dummy;
            if (!int.TryParse(yil + ay + gun, NumberStyles.None, CultureInfo.InvariantCulture, out dummy)) return "";
            return ay + "/" + gun + "/" + yil;
        }

        // ==== Genel yardımcılar ==============================================

        static string N(string s) { return s ?? ""; }

        static string TekBosluk(string s)
        {
            return Regex.Replace(N(s), @"\s+", " ").Trim();
        }

        static void Tamamla(ref string hedef, string kaynak)
        {
            if (Doldurmalik(hedef) && !Doldurmalik(kaynak)) hedef = kaynak.Trim();
            else if (hedef == null) hedef = "";
        }

        static void Not(Envanter e, string mesaj)
        {
            if (e == null || string.IsNullOrEmpty(mesaj)) return;
            if (e.ToplamaNotlari == null) e.ToplamaNotlari = new List<string>();
            if (!e.ToplamaNotlari.Contains(mesaj)) e.ToplamaNotlari.Add(mesaj);
        }

        static readonly string[] DoldurmalikDegerler =
        {
            "TO BE FILLED BY O.E.M.", "TO BE FILLED BY OEM", "DEFAULT STRING", "NOT SPECIFIED", "NOT APPLICABLE",
            "NOT AVAILABLE", "NONE", "N/A", "NA", "UNKNOWN", "UNDEFINED", "SYSTEM PRODUCT NAME",
            "SYSTEM MANUFACTURER", "SYSTEM VERSION", "SYSTEM SERIAL NUMBER", "O.E.M.", "OEM", "NULL", "EMPTY",
            "DEFAULT", "BASE BOARD PRODUCT NAME", "BASEBOARD PRODUCT NAME", "9876543210", "0123456789", "1234567890"
        };

        static readonly Regex DoldurmalikKalip = new Regex(
            @"^(0+|F+|X+|\?+|-+|\.+|(PARTNUM|SERNUM|ASSETTAGNUM|ASSET ?TAG|PART ?NUMBER|SERIAL ?NUMBER|MODULE ?PARTNUMBER|MODULE ?SERIALNO|ARRAY\d+_(PARTNUMBER|SERIALNUMBER|ASSETTAG)|DIMM ?PART ?NUMBER)\s*\d*)$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>Boş ya da BIOS doldurmalık değeri mi ("To Be Filled By O.E.M.", "Unknown", "00000000", "PartNum0" ...).</summary>
        internal static bool Doldurmalik(string s)
        {
            if (s == null) return true;
            string t = s.Trim().Trim('\0').Trim();
            if (t.Length == 0) return true;
            string u = t.ToUpperInvariant();
            foreach (string d in DoldurmalikDegerler)
                if (u == d) return true;
            return DoldurmalikKalip.IsMatch(u);
        }

        // ==== Win32 ==========================================================

        [StructLayout(LayoutKind.Sequential)]
        struct BellekDurumu   // MEMORYSTATUSEX
        {
            public uint Boyut;                  // dwLength
            public uint YukYuzdesi;             // dwMemoryLoad
            public ulong ToplamFiziksel;        // ullTotalPhys
            public ulong BosFiziksel;           // ullAvailPhys
            public ulong ToplamSayfaDosyasi;    // ullTotalPageFile (= taahhüt sınırı)
            public ulong BosSayfaDosyasi;       // ullAvailPageFile
            public ulong ToplamSanal;           // ullTotalVirtual
            public ulong BosSanal;              // ullAvailVirtual
            public ulong BosGenisletilmisSanal; // ullAvailExtendedVirtual
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PerformansBilgisi   // PERFORMANCE_INFORMATION (sayfa cinsinden)
        {
            public uint Boyut;                  // cb
            public UIntPtr TaahhutToplam;       // CommitTotal
            public UIntPtr TaahhutSinir;        // CommitLimit
            public UIntPtr TaahhutZirve;        // CommitPeak
            public UIntPtr FizikselToplam;      // PhysicalTotal
            public UIntPtr FizikselBos;         // PhysicalAvailable
            public UIntPtr SistemOnbellek;      // SystemCache
            public UIntPtr CekirdekToplam;      // KernelTotal
            public UIntPtr CekirdekSayfali;     // KernelPaged
            public UIntPtr CekirdekSayfasiz;    // KernelNonpaged
            public UIntPtr SayfaBoyutu;         // PageSize
            public uint TutamacSayisi;          // HandleCount
            public uint SurecSayisi;            // ProcessCount
            public uint IsParcacigiSayisi;      // ThreadCount
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalMemoryStatusEx(ref BellekDurumu durum);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out ulong kilobayt);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool K32GetPerformanceInfo(out PerformansBilgisi bilgi, uint boyut);

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPerformanceInfo(out PerformansBilgisi bilgi, uint boyut);
    }
}
