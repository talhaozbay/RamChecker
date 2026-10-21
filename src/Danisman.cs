// RAM Danışmanı — karar motoru.
// "Bu bilgisayara nasıl RAM almalıyım, nasıl yükseltirim?" sorusunu
// toplanan Envanter'e göre bu makineye özel cevaplar.
// Kurallar: asla istisna fırlatmaz; tüm metinler düz metindir (HTML kaçırmayı Rapor yapar);
// donanım metinleri kültürden bağımsız (ToUpperInvariant / OrdinalIgnoreCase) işlenir.
// C# 5: $"", ?. , => üye, nameof, out var YOK.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RamDanismani
{
    public static class Danisman
    {
        // İhtiyaç sonuçları
        const int IhtiyacBilinmiyor = 0, IhtiyacYeterli = 1, IhtiyacSinirda = 2, IhtiyacYetersiz = 3;

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>Değerlendirme sırasında taşınan ara bilgiler.</summary>
        class Baglam
        {
            public Envanter E;
            public DanismanSonucu D;
            public SistemBilgisi S;
            public PlatformBilgisi P;
            public KullanimBilgisi K;
            public KasaTipi Kasa;
            public bool Mobil;              // SODIMM sınıfı (dizüstü, AIO, mini PC)
            public bool Sunucu;
            public bool Kanal2;             // platform 2+ kanallı (bilinmiyorsa tüketici için varsayılır)
            public List<BellekDizisi> Diziler = new List<BellekDizisi>();
            public List<BellekYuvasi> Yuvalar = new List<BellekYuvasi>();
            public List<BellekYuvasi> Dolu = new List<BellekYuvasi>();
            public List<BellekYuvasi> Bos = new List<BellekYuvasi>();
            public List<BellekYuvasi> Lehimli = new List<BellekYuvasi>();
            public List<BellekYuvasi> Sokulebilir = new List<BellekYuvasi>();
            public long ToplamMB;
            public int ToplamGB, LehimliGB, SokulebilirGB;
            public string Tur = "";         // DDR4, DDR5, LPDDR5 ...
            public bool Ddr3L;
            public string AlimTuru = "";    // DDR3L / DDR4 / DDR5 ...
            public string Form = "";        // alınacak modülün formu: DIMM / SODIMM / CAMM ("" = bilinmiyor)
            public bool FormTahmini;
            public bool AlinabilirTur;
            public int PlatformHiz;
            public bool YuvaSupheli;
            public string SupheliYuvaAdi = "";
            public long BiosMaxMB;
            public bool BiosMaxGuvenilir;
            public bool TekKanal, CiftKanal, AyniKanal, Flex;
            public bool KanalKesin;         // çift kanal yuva adlarından doğrulandı ("muhtemelen" değil)
            public bool LehimliYuvaSupheli; // lehimli bellek + BIOS ek SODIMM yuvasını gizliyor olabilir
            public bool LehimliSupheli;     // tüm lehimli LPDDR modüllerin form faktörü belirsiz (LPCAMM2 olabilir)
            public int BoyutBilinmeyen;     // dolu ama boyutu bilinmeyen modül sayısı
            public int CalisanHiz;
            public bool Ecc, Kayitli, Lrdimm;
            public int ModulBasiGuvenli, ModulBasiTeorik;
            public int Ihtiyac;
            public double TaahhutOrani;
            public bool Isletim32;
            public BellekYuvasi Ornek;
            public int OrnekGB;
            public bool HepsiEs;
            public int HedefGB;
            public bool Gerekmiyor;
            public int EkleAdet;
            public YukseltmeSenaryosu SenEkle, SenBuyukEkle, SenKit, SenTekBuyuk, SenLehimliEs, SenLehimliBuyuk, SenYuvaBuyut, SenKucukDegistir;
        }

        // =====================================================================
        //  Giriş noktası
        // =====================================================================

        public static DanismanSonucu Degerlendir(Envanter e)
        {
            DanismanSonucu d = new DanismanSonucu();
            Baglam b = new Baglam();
            b.D = d;
            try
            {
                Hazirla(b, e);
            }
            catch (Exception ex)
            {
                IcHata(b, "veri hazırlığı", ex);
                d.Durum = YukseltmeDurumu.Bilinmiyor;
                d.OzetCumle = "Bellek bilgisi işlenemedi; Görev Yöneticisi > Performans > Bellek ekranından yuva ve hız bilgisine bakın.";
                d.Alinacak.Gecerli = false;
                return d;
            }

            try
            {
                if (b.S.SanalMakine || b.Kasa == KasaTipi.SanalMakine)
                {
                    Adim(b, "sanal makine", SanalMakine);
                    Adim(b, "kullanım analizi", IhtiyacAnalizi);
                    Adim(b, "işletim sistemi", IsletimSistemiKontrol);
                    return d;
                }

                Adim(b, "kullanım analizi", IhtiyacAnalizi);
                Adim(b, "işletim sistemi", IsletimSistemiKontrol);

                if (b.Dolu.Count == 0)
                {
                    Adim(b, "veri yok", VeriYok);
                    Adim(b, "kontrol listesi", KontrolListesi);
                    Adim(b, "takma adımları", TakmaAdimlari);
                    Adim(b, "bağlantılar", Baglantilar);
                    return d;
                }

                Adim(b, "yuva analizi", YuvaAnalizi);
                Adim(b, "lehimli bellek", LehimliAnalizi);
                Adim(b, "azami kapasite", KapasiteAnalizi);
                Adim(b, "kanal analizi", KanalAnalizi);
                Adim(b, "hız analizi", HizAnalizi);
                Adim(b, "modül karışıklığı", KarisiklikAnalizi);
                Adim(b, "tür ve voltaj", TurVeVoltajAnalizi);
                Adim(b, "senaryolar", Senaryolar);
                Adim(b, "satın alma özeti", SatinAlma);
                Adim(b, "durum", DurumBelirle);
                Adim(b, "özet cümle", OzetCumle);
                Adim(b, "kontrol listesi", KontrolListesi);
                Adim(b, "takma adımları", TakmaAdimlari);
                Adim(b, "bağlantılar", Baglantilar);
            }
            catch (Exception ex)
            {
                IcHata(b, "değerlendirme", ex);
            }

            if (string.IsNullOrEmpty(d.OzetCumle))
                d.OzetCumle = "Bellek bilgisi kısmen değerlendirilebildi; ayrıntılar için bulgulara ve üretici belgesine bakın.";
            if (d.Alinacak == null) d.Alinacak = new SatinAlmaOzeti();
            return d;
        }

        static void Adim(Baglam b, string ad, Action<Baglam> is_)
        {
            try { is_(b); }
            catch (Exception ex) { IcHata(b, ad, ex); }
        }

        static void IcHata(Baglam b, string ad, Exception ex)
        {
            try
            {
                b.D.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Değerlendirmenin bir bölümü tamamlanamadı (" + ad + ")",
                    "Program bu bölümde beklenmeyen bir veriyle karşılaştı: " + ex.GetType().Name + ": " + ex.Message +
                    "\nDiğer sonuçlar yine geçerlidir; şüphede kalırsanız üretici belgesinden doğrulayın."));
            }
            catch { }
        }

        // =====================================================================
        //  Hazırlık: filtreleme ve normalleştirme
        // =====================================================================

        static void Hazirla(Baglam b, Envanter e)
        {
            if (e == null) e = new Envanter();
            b.E = e;
            b.S = e.Sistem ?? new SistemBilgisi();
            b.P = e.Platform ?? new PlatformBilgisi();
            b.K = e.Kullanim ?? new KullanimBilgisi();
            if (b.P.MaxHizMTs == null) b.P.MaxHizMTs = new Dictionary<string, int>();
            if (b.P.DesteklenenTurler == null) b.P.DesteklenenTurler = new List<string>();
            b.Kasa = b.S.Kasa;

            List<BellekDizisi> diziler = (e.Diziler ?? new List<BellekDizisi>()).Where(x => x != null).ToList();
            List<BellekYuvasi> yuvalar = (e.Yuvalar ?? new List<BellekYuvasi>()).Where(x => x != null).ToList();

            // 1) Sistem belleği dizileri (video / önbellek / flash dizileri sayılmaz)
            HashSet<int> disarida = new HashSet<int>();
            foreach (BellekDizisi dz in diziler)
            {
                if (SistemDizisi(dz)) b.Diziler.Add(dz);
                else disarida.Add(dz.Handle);
            }
            if (b.Diziler.Count == 0 && diziler.Count > 0)
            {
                // Hiç sistem dizisi yoksa BIOS kodları anlamsızdır: hepsini kullan.
                b.Diziler.AddRange(diziler);
                disarida.Clear();
            }
            foreach (BellekYuvasi y in yuvalar)
                if (!disarida.Contains(y.DiziHandle)) b.Yuvalar.Add(y);
            if (!b.Yuvalar.Any(y => y.Dolu) && yuvalar.Any(y => y.Dolu))
                b.Yuvalar = new List<BellekYuvasi>(yuvalar); // filtre her şeyi sildiyse güvenme

            foreach (BellekYuvasi y in b.Yuvalar)
            {
                if (y.Dolu) b.Dolu.Add(y); else b.Bos.Add(y);
            }
            foreach (BellekYuvasi y in b.Dolu)
            {
                if (LehimliMi(y)) b.Lehimli.Add(y); else b.Sokulebilir.Add(y);
                b.ToplamMB += Math.Max(0, y.BoyutMB);
                if (y.BoyutMB <= 0) b.BoyutBilinmeyen++;
            }
            b.LehimliSupheli = b.Lehimli.Count > 0 && b.Lehimli.All(y => y.LehimliSupheli);
            b.ToplamGB = GB(b.ToplamMB);
            b.LehimliGB = GB(b.Lehimli.Sum(y => Math.Max(0, y.BoyutMB)));
            b.SokulebilirGB = GB(b.Sokulebilir.Sum(y => Math.Max(0, y.BoyutMB)));
            b.D.MevcutToplamGB = b.ToplamGB;

            // Tür (en sık görülen)
            b.Tur = EnSik(b.Dolu.Select(y => TurAdi(y)).Where(t => t != ""));
            if (b.Tur == "DDR3L") { b.Tur = "DDR3"; b.Ddr3L = true; }
            if (b.Tur == "" && b.P.Bulundu)
            {
                List<string> masaustuTurleri = b.P.DesteklenenTurler.Where(t => t != null && !t.StartsWith("LP", StringComparison.OrdinalIgnoreCase)).ToList();
                if (masaustuTurleri.Count == 1) b.Tur = masaustuTurleri[0].Trim().ToUpperInvariant();
            }
            if (b.Tur == "DDR3")
            {
                foreach (BellekYuvasi y in b.Dolu)
                    if (Ddr3LVoltaji(y.MinVoltajMv) || Ddr3LVoltaji(y.YapilandirilmisVoltajMv) || TurAdi(y) == "DDR3L") b.Ddr3L = true;
                bool ddr3Var = b.P.DesteklenenTurler.Any(t => string.Equals((t ?? "").Trim(), "DDR3", StringComparison.OrdinalIgnoreCase));
                bool ddr3LVar = b.P.DesteklenenTurler.Any(t => string.Equals((t ?? "").Trim(), "DDR3L", StringComparison.OrdinalIgnoreCase));
                if (ddr3LVar && !ddr3Var) b.Ddr3L = true;
            }
            b.AlimTuru = b.Ddr3L ? "DDR3L" : b.Tur;

            // Kasa / mobil
            b.Sunucu = b.Kasa == KasaTipi.Sunucu;
            string doluForm = EnSik(b.Sokulebilir.Select(y => FormAdi(y)).Where(f => f != "" && f != "LEHIMLI"));
            string bosForm = EnSik(b.Bos.Select(y => FormAdi(y)).Where(f => f != "" && f != "LEHIMLI"));
            b.Form = doluForm != "" ? doluForm : bosForm;
            if (b.Form == "")
            {
                if (b.Kasa == KasaTipi.Dizustu || b.Kasa == KasaTipi.Tablet || b.Kasa == KasaTipi.MiniPc || b.Kasa == KasaTipi.HepsiBirArada)
                { b.Form = "SODIMM"; b.FormTahmini = true; }
                else if (b.Kasa == KasaTipi.Masaustu || b.Kasa == KasaTipi.Sunucu)
                { b.Form = "DIMM"; b.FormTahmini = true; }
            }
            b.Mobil = b.Kasa == KasaTipi.Dizustu || b.Kasa == KasaTipi.Tablet ||
                      (b.Form == "SODIMM" && b.Kasa != KasaTipi.Masaustu && b.Kasa != KasaTipi.Sunucu);

            // ECC / Registered
            foreach (BellekYuvasi y in b.Dolu)
            {
                if (y.Ecc) b.Ecc = true;
                if (KayitliMi(y)) b.Kayitli = true;
                if (y.TurDetay != null)
                    foreach (string td in y.TurDetay)
                        if (td != null && (td.IndexOf("LRDIMM", StringComparison.OrdinalIgnoreCase) >= 0 || td.IndexOf("Load Reduced", StringComparison.OrdinalIgnoreCase) >= 0))
                        { b.Lrdimm = true; b.Kayitli = true; }
            }
            if (b.Kayitli) b.Sunucu = true;

            b.AlinabilirTur = (b.Tur == "DDR2" || b.Tur == "DDR3" || b.Tur == "DDR4" || b.Tur == "DDR5") &&
                              (b.Form == "DIMM" || b.Form == "SODIMM");

            // Platform
            b.PlatformHiz = PlatformHizi(b.P, b.Ddr3L ? "DDR3L" : b.Tur);
            if (b.P.Bulundu && b.P.KanalSayisi > 0) b.Kanal2 = b.P.KanalSayisi >= 2;
            else b.Kanal2 = !b.Sunucu;

            // Modül başına sınırlar
            ModulSinirlari(b);

            // Örnek (temsilci) modül: en sık boyuttaki sökülebilir modül
            if (b.Sokulebilir.Count > 0)
            {
                long sikBoyut = b.Sokulebilir.GroupBy(y => y.BoyutMB).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).First().Key;
                b.Ornek = b.Sokulebilir.First(y => y.BoyutMB == sikBoyut);
                b.OrnekGB = GB(b.Ornek.BoyutMB);
                b.HepsiEs = b.Sokulebilir.All(y => GB(y.BoyutMB) == b.OrnekGB && TurAdi(y) == TurAdi(b.Ornek)) &&
                            b.Sokulebilir.Select(y => EtiketHizi(y)).Where(h => h > 0).Distinct().Count() <= 1;
            }
            else if (b.Lehimli.Count > 0)
            {
                b.Ornek = b.Lehimli[0];
                b.OrnekGB = GB(b.Ornek.BoyutMB);
            }

            List<int> calisan = b.Dolu.Select(y => y.YapilandirilmisHizMTs).Where(h => h > 0).ToList();
            b.CalisanHiz = calisan.Count > 0 ? calisan.Min() : 0;
        }

        static bool SistemDizisi(BellekDizisi dz)
        {
            // SMBIOS: 1 Other, 2 Unknown, 3 System memory, 4 Video, 5 Flash, 6 NVRAM, 7 Cache
            if (dz.KullanimKodu == 0 || dz.KullanimKodu == 1 || dz.KullanimKodu == 2 || dz.KullanimKodu == 3)
            {
                string k = (dz.Kullanim ?? "").ToUpperInvariant();
                if (k.Contains("VIDEO") || k.Contains("CACHE") || k.Contains("FLASH")) return false;
                return true;
            }
            return false;
        }

        static void ModulSinirlari(Baglam b)
        {
            switch (b.Tur)
            {
                case "DDR2": b.ModulBasiGuvenli = 2; b.ModulBasiTeorik = b.Form == "SODIMM" ? 2 : 4; break;
                case "DDR3": b.ModulBasiGuvenli = 8; b.ModulBasiTeorik = 8; break;
                case "DDR4": b.ModulBasiGuvenli = 32; b.ModulBasiTeorik = 32; break;
                case "DDR5": b.ModulBasiGuvenli = 32; b.ModulBasiTeorik = 64; break;
                default: b.ModulBasiGuvenli = 0; b.ModulBasiTeorik = 0; break;
            }
            if (b.Kayitli)
            {
                if (b.Tur == "DDR3") { b.ModulBasiGuvenli = 16; b.ModulBasiTeorik = 32; }
                else if (b.Tur == "DDR4") { b.ModulBasiGuvenli = 64; b.ModulBasiTeorik = 128; }
                else if (b.Tur == "DDR5") { b.ModulBasiGuvenli = 64; b.ModulBasiTeorik = 128; }
            }
        }

        // =====================================================================
        //  Sanal makine / veri yok
        // =====================================================================

        static void SanalMakine(Baglam b)
        {
            DanismanSonucu d = b.D;
            d.Durum = YukseltmeDurumu.Evet;
            int gb = b.ToplamGB > 0 ? b.ToplamGB : GB(b.S.TakiliRamMB > 0 ? b.S.TakiliRamMB : b.S.KullanilabilirRamMB);
            d.MevcutToplamGB = gb;
            d.ToplamYuva = b.Yuvalar.Count;
            d.BosYuva = b.Bos.Count;
            d.YuvaBilgisiGuvenilir = false;
            d.KanalDurumu = "Sanal makine (sanal bellek)";
            string not = string.IsNullOrEmpty(b.S.SanalMakineNotu) ? "" : " (" + b.S.SanalMakineNotu.Trim() + ")";
            d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Sanal makinedesin" + not,
                "Bu Windows bir sanal makinede çalışıyor; gördüğün \"RAM modülleri\" hipervizörün taklit ettiği sanal aygıtlardır." +
                "\nRAM, hipervizör ayarından artırılır (Hyper-V: Ayarlar > Bellek; VMware: VM Settings > Memory; VirtualBox: Ayarlar > Sistem > Ana Bellek; " +
                "bulutta: daha büyük makine boyutu). Fiziksel RAM satın almaya gerek yok." +
                "\nBelleği artırmadan önce ana makinede (host) yeterli boş RAM olduğundan emin ol; ana makinenin RAM'i yetmiyorsa yükseltilecek olan odur."));
            d.OzetCumle = "Bu bir sanal makine" + (gb > 0 ? " (" + gb + " GB ayrılmış)" : "") +
                ": RAM, hipervizörün (Hyper-V, VMware, VirtualBox ya da bulut) ayarlarından artırılır; fiziksel RAM almaya gerek yok.";
            d.Alinacak.Gecerli = false;
            d.Alinacak.Gerekce = "Sanal makine: bellek hipervizör ayarından artırılır, fiziksel modül alınmaz.";
            d.KontrolListesi.Add("Ana makinede (host) sanal makineye verilecek kadar boş RAM var mı? Yoksa önce ana makinenin RAM'i yükseltilmeli.");
            d.KontrolListesi.Add("Dinamik bellek (Hyper-V Dynamic Memory) açıksa en az/en çok değerlerini birlikte ayarla.");
            d.KontrolListesi.Add("Bulut sanal makinesinde boyut değişikliği maliyeti artırır; ihtiyacı Kullanım sekmesindeki zirve değerle doğrula.");
            d.TakmaAdimlari.Add("Sanal makineyi kapat (kaydet/askıya al değil, tamamen kapat).");
            d.TakmaAdimlari.Add("Hipervizörde bellek ayarını artır (Hyper-V: Ayarlar > Bellek; VMware: Settings > Memory; VirtualBox: Ayarlar > Sistem > Ana Bellek).");
            d.TakmaAdimlari.Add("Sanal makineyi başlat ve bu programla yeni miktarı doğrula.");
        }

        static void VeriYok(Baglam b)
        {
            DanismanSonucu d = b.D;
            d.Durum = YukseltmeDurumu.Bilinmiyor;
            d.YuvaBilgisiGuvenilir = false;
            d.ToplamYuva = Math.Max(b.Yuvalar.Count, b.Diziler.Where(x => x.YuvaSayisi > 0).Sum(x => x.YuvaSayisi));
            d.BosYuva = d.ToplamYuva;
            d.KanalDurumu = "Bilinmiyor";
            long takili = b.S.TakiliRamMB > 0 ? b.S.TakiliRamMB : b.S.KullanilabilirRamMB;
            if (takili > 0) d.MevcutToplamGB = GB(takili);
            string miktar = takili > 0 ? " Windows " + Bicim.Boyut(takili) + " RAM görüyor ama modül ayrıntısı yok." : "";
            d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Bellek modülü bilgisi okunamadı",
                "BIOS/SMBIOS ve WMI bu bilgisayardaki bellek yuvalarını bildirmedi." + miktar +
                "\nElle bakmak için: Görev Yöneticisi (Ctrl+Shift+Esc) > Performans > Bellek. Sağ altta \"Kullanılan yuvalar\" (ör. 1 / 2), " +
                "\"Form faktörü\" (SODIMM = dizüstü, DIMM = masaüstü) ve \"Hız\" yazar. Türü (DDR4/DDR5) için üretici belgesine bak." +
                "\nSonra üreticinin destek sayfasında modeli arayıp azami kapasiteyi ve yuva sayısını doğrula."));
            d.OzetCumle = "Bu bilgisayarın bellek yuvaları okunamadı" + (takili > 0 ? " (Windows " + Bicim.Boyut(takili) + " görüyor)" : "") +
                "; Görev Yöneticisi > Performans > Bellek ekranındaki \"Kullanılan yuvalar\", \"Form faktörü\" ve \"Hız\" bilgisine bakıp üretici belgesinden doğrulayın.";
            d.Alinacak.Gecerli = false;
            d.Alinacak.Gerekce = "Bellek türü ve yuva bilgisi okunamadığı için güvenli bir satın alma önerisi yapılamıyor.";
        }

        // =====================================================================
        //  İşletim sistemi (32-bit, donanıma ayrılan)
        // =====================================================================

        static void IsletimSistemiKontrol(Baglam b)
        {
            DanismanSonucu d = b.D;
            if (!string.IsNullOrEmpty(b.S.IsletimSistemi) && !b.S.Isletim64Bit)
            {
                b.Isletim32 = true;
                long takili = b.S.TakiliRamMB > 0 ? b.S.TakiliRamMB : b.ToplamMB;
                string ek = takili > 4096
                    ? "Bu bilgisayarda " + Bicim.Boyut(takili) + " RAM takılı ama 32-bit Windows bunun yalnızca yaklaşık 3–4 GB'ını kullanabiliyor; gerisi boşa duruyor."
                    : "32-bit Windows en fazla yaklaşık 3–4 GB RAM kullanabilir; 4 GB üstüne yapılacak her yükseltme boşa gider.";
                d.Bulgular.Add(new Bulgu(Seviye.Kritik, "32-bit Windows: önce 64-bit Windows'a geç",
                    ek + "\nRAM almadan önce 64-bit Windows'a geçilmeli (temiz kurulum gerekir; 32-bit'ten 64-bit'e yerinde yükseltme yoktur, verileri yedekle). " +
                    "İşlemci 64-bit destekliyorsa (2008 sonrası neredeyse hepsi) sorun olmaz."));
            }

            if (!b.Isletim32 && b.S.TakiliRamMB > 0 && b.S.KullanilabilirRamMB > 0)
            {
                long ayrilan = b.S.TakiliRamMB - b.S.KullanilabilirRamMB;
                if (ayrilan > 1024)
                {
                    bool cok = ayrilan > 2048 && ayrilan * 100 / b.S.TakiliRamMB >= 15;
                    d.Bulgular.Add(new Bulgu(cok ? Seviye.Uyari : Seviye.Bilgi, "Donanıma ayrılan bellek: " + Bicim.Boyut(ayrilan),
                        "Takılı " + Bicim.Boyut(b.S.TakiliRamMB) + " RAM'in " + Bicim.Boyut(ayrilan) + " kadarını Windows kullanamıyor. " +
                        "Genelde dahili grafiğin (iGPU) payıdır; BIOS'ta \"UMA Frame Buffer / iGPU Memory\" ayarı küçültülebilir." +
                        (cok ? "\nBu oran yüksek: BIOS'ta \"Memory Remap\" kapalı olabilir ya da bir modül tam tanınmıyor olabilir; modüllerin oturduğunu kontrol et." : "")));
                }
            }

            if (b.S.TakiliRamMB > 0 && b.ToplamMB > 0 && b.S.TakiliRamMB > b.ToplamMB + 1024)
            {
                d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Listelenmeyen bellek var",
                    "Windows " + Bicim.Boyut(b.S.TakiliRamMB) + " takılı RAM görüyor ama BIOS yalnızca " + Bicim.Boyut(b.ToplamMB) +
                    " değerinde modül listeliyor. BIOS bazı modülleri (ör. lehimli belleği ya da ikinci yuvayı) bildirmiyor olabilir; yuva bilgisini üretici belgesinden doğrula."));
            }
        }

        // =====================================================================
        //  Yuva sayımı ve güvenilirliği
        // =====================================================================

        static void YuvaAnalizi(Baglam b)
        {
            DanismanSonucu d = b.D;
            int listelenen = b.Yuvalar.Count;
            int bios = b.Diziler.Where(x => x.YuvaSayisi > 0).Sum(x => x.YuvaSayisi);
            d.ToplamYuva = Math.Max(listelenen, bios);
            d.BosYuva = Math.Max(0, d.ToplamYuva - b.Dolu.Count);
            d.YuvaBilgisiGuvenilir = true;

            bool kanalIpucu = false;
            string ilkAd = "";
            foreach (BellekYuvasi y in b.Dolu)
            {
                bool tahmini;
                string k = KanalAnahtari(y, out tahmini);
                if (k != "" && !tahmini) kanalIpucu = true;
                if (ilkAd == "") ilkAd = YuvaAdi(y);
            }

            bool uygunKasa = b.Kasa == KasaTipi.Dizustu || b.Kasa == KasaTipi.Masaustu || b.Kasa == KasaTipi.HepsiBirArada ||
                             b.Kasa == KasaTipi.MiniPc || b.Kasa == KasaTipi.Bilinmiyor;
            bool tekKanalliPlatform = b.P.Bulundu && b.P.KanalSayisi == 1;

            if (b.BoyutBilinmeyen > 0)
            {
                d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Bir modülün boyutu BIOS'ta bilinmiyor",
                    b.BoyutBilinmeyen + " dolu modülün boyutu BIOS'ta 'bilinmiyor' olarak geçiyor ve parça numarasından da çıkarılamadı. " +
                    "Toplam ve yükseltme önerisi bu yüzden güvenilmez; modül etiketinden (ya da Görev Yöneticisi > Bellek'ten) doğrulayın."));
            }

            // Lehimli bellek + BIOS'un gizlediği olası SODIMM yuvası (Lenovo vb. boş yuvayı hiç listelemeyebilir)
            if (b.Lehimli.Count > 0 && b.Sokulebilir.Count == 0 && d.ToplamYuva == b.Lehimli.Count &&
                (b.Tur == "DDR4" || b.Tur == "DDR5") &&
                (b.Kasa == KasaTipi.Dizustu || b.Kasa == KasaTipi.MiniPc || b.Kasa == KasaTipi.Bilinmiyor) &&
                !b.Sunucu && b.P.Bulundu && b.P.KanalSayisi >= 2)
            {
                b.LehimliYuvaSupheli = true;
                d.YuvaBilgisiGuvenilir = false;
                d.SupheliEkYuva = 1;
                d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Ek SODIMM yuvası olup olmadığı doğrulanmalı",
                    "BIOS yalnızca lehimli belleği listeliyor. Birçok BIOS boş yuvayı hiç göstermez; bu modelde lehimli belleğin yanında " +
                    "boş bir SODIMM yuvası olabilir. Üretici belgesinden (Lenovo PSREF, Dell/HP servis kılavuzu) ya da kapağı açıp bakarak doğrula."));
            }

            if (d.ToplamYuva == 1 && b.Dolu.Count == 1 && b.Lehimli.Count == 0 && !b.Sunucu && uygunKasa && !tekKanalliPlatform)
            {
                b.YuvaSupheli = true;
                b.SupheliYuvaAdi = ilkAd;
                d.YuvaBilgisiGuvenilir = false;
                d.SupheliEkYuva = 1;
                StringBuilder sb = new StringBuilder();
                sb.Append("BIOS bu cihazda yalnızca 1 bellek yuvası bildiriyor ve o yuva dolu.");
                if (kanalIpucu && ilkAd != "")
                    sb.Append(" Yuva adı (\"" + ilkAd + "\") bir kanal adı içeriyor; bu, en az bir kanalın daha olabileceğini düşündürür.");
                if (b.P.Bulundu && b.P.KanalSayisi >= 2)
                    sb.Append(" İşlemci " + b.P.KanalSayisi + " kanallı bellek denetleyicisine sahip.");
                sb.Append("\nBirçok BIOS boş yuvayı hiç listelemez; bu yüzden bu model 2. yuvaya sahip olabilir. ");
                sb.Append("Üretici belgesinden (Lenovo PSREF, Dell/HP servis kılavuzu ya da anakart kılavuzu) veya kapağı açıp bakarak doğrula. ");
                sb.Append("Bazı ince modellerde belleğin bir kısmı lehimlidir ve gerçekten tek yuva olabilir.");
                if (b.S.TakiliRamMB > 0 && Math.Abs(b.S.TakiliRamMB - b.ToplamMB) <= 512)
                    sb.Append("\nWindows'un gördüğü toplam (" + Bicim.Boyut(b.S.TakiliRamMB) + ") bu modülle aynı: şu an başka takılı modül yok.");
                d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Yuva sayısı doğrulanmalı: BIOS yalnızca takılı modülü listeliyor olabilir", sb.ToString()));
                return;
            }

            if (bios > 0 && listelenen < bios)
            {
                d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "BIOS " + bios + " yuva bildiriyor, " + listelenen + " tanesini listeliyor",
                    "Listelenmeyen yuvalar büyük olasılıkla boştur. Toplam yuva sayısı " + d.ToplamYuva + " kabul edildi."));
            }
            else if (bios > 0 && listelenen > bios)
            {
                d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Yuva sayısında tutarsızlık",
                    "BIOS dizi bilgisinde " + bios + " yuva yazıyor ama " + listelenen + " yuva listeleniyor. Listelenen sayı esas alındı; üretici belgesinden doğrulamak iyi olur."));
            }

            if (tekKanalliPlatform && d.ToplamYuva == 1)
            {
                d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Tek yuva (tek kanallı işlemci)",
                    "Bu işlemcinin bellek denetleyicisi tek kanallı; tek yuvalı tasarım bu sınıfta normaldir. Kapasite artırmak için modül daha büyüğüyle değiştirilir."));
            }

            if (d.BosYuva > 0)
            {
                d.Bulgular.Add(new Bulgu(Seviye.Iyi, "Boş yuva var: " + d.ToplamYuva + " yuvadan " + d.BosYuva + " tanesi boş",
                    "Mevcut modülleri çıkarmadan RAM eklenebilir."));
            }
            else if (b.Lehimli.Count < b.Dolu.Count)
            {
                d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Tüm yuvalar dolu (" + b.Dolu.Count + " / " + d.ToplamYuva + ")",
                    "Kapasite artırmak için mevcut modüller daha büyükleriyle değiştirilmeli (eş kit önerilir)."));
            }
        }

        // =====================================================================
        //  Lehimli bellek
        // =====================================================================

        static void LehimliAnalizi(Baglam b)
        {
            DanismanSonucu d = b.D;
            if (b.Lehimli.Count == 0) return;
            string turMetni = b.Tur != "" ? " " + b.Tur : "";
            if (b.Sokulebilir.Count == 0 && d.BosYuva == 0 && b.LehimliYuvaSupheli)
            {
                d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Lehimli bellek; ek yuva olup olmadığı doğrulanmalı",
                    "Bu cihazdaki " + b.LehimliGB + " GB" + turMetni + " bellek anakarta lehimli. BIOS boş yuva listelemiyor ama bu üretici ailesinde boş SODIMM yuvası gizlenebiliyor." +
                    "\nÜretici belgesinden (Lenovo PSREF vb.) ek yuva olup olmadığını doğrula. Yuva varsa lehimliyle eş " + b.LehimliGB + " GB " + b.Tur +
                    " SODIMM simetrik çift kanal sağlar; yuva yoksa RAM artırılamaz."));
                return;
            }
            if (b.Sokulebilir.Count == 0 && d.BosYuva == 0 && b.LehimliSupheli)
            {
                d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Muhtemelen lehimli; LPCAMM2 olabilir",
                    "Bu cihazdaki " + b.LehimliGB + " GB" + turMetni + " bellek büyük olasılıkla anakarta lehimli, ancak BIOS form faktörünü belirsiz bildiriyor. " +
                    "Bazı yeni iş istasyonları değiştirilebilir LPCAMM2 modülü kullanır." +
                    "\nRAM almadan önce üretici belgesinden LPCAMM2 olup olmadığını doğrula; değilse RAM artırılamaz."));
                return;
            }
            if (b.Sokulebilir.Count == 0 && d.BosYuva == 0)
            {
                d.Bulgular.Add(new Bulgu(Seviye.Uyari, "RAM anakarta lehimli: yükseltilemez",
                    "Bu cihazdaki " + b.LehimliGB + " GB" + turMetni + " bellek anakarta lehimli ve boş yuva yok. RAM eklenemez ve değiştirilemez." +
                    "\nDaha fazla RAM gerekiyorsa çözüm cihaz değişimidir. Yeni cihaz alırken: en az " + Math.Max(16, SonrakiKademe(b.ToplamGB)) +
                    " GB seç, mümkünse yuvalı (SODIMM) model tercih et; lehimli modellerde sonradan artıramayacağın için ilk alımda yüksek kapasite al." +
                    "\nSayfa dosyası ve tarayıcı sekmelerini azaltmak gibi yazılım önlemleri geçici rahatlama sağlar."));
                return;
            }
            if (b.Tur.StartsWith("LP", StringComparison.Ordinal))
            {
                d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Lehimli " + b.Tur + " ile birlikte yuva bildirildi",
                    b.Tur + " bellek ile normal SODIMM yuvası aynı cihazda bulunmaz; BIOS bilgisi hatalı olabilir. Satın almadan önce üretici belgesinden yuva olup olmadığını doğrula."));
                return;
            }
            d.Bulgular.Add(new Bulgu(Seviye.Bilgi, b.LehimliGB + " GB lehimli bellek + " + (d.ToplamYuva - b.Lehimli.Count) + " yuva",
                "Belleğin " + b.LehimliGB + " GB'ı anakarta lehimli (sökülemez), ayrıca takılıp çıkarılabilen " +
                (b.Form != "" ? b.Form + " " : "") + "yuvası var." +
                "\nYuvaya lehimli bellekle eş boyutta (" + b.LehimliGB + " GB) modül takmak simetrik çift kanal sağlar; daha büyüğü de çalışır ama fazlası tek kanalda (flex mode) çalışır."));
        }

        // =====================================================================
        //  Azami kapasite
        // =====================================================================

        static void KapasiteAnalizi(Baglam b)
        {
            DanismanSonucu d = b.D;
            b.BiosMaxMB = b.Diziler.Where(x => x.MaxKapasiteMB > 0).Sum(x => x.MaxKapasiteMB);
            b.BiosMaxGuvenilir = b.BiosMaxMB > 0;
            string neden = "";
            if (b.BiosMaxMB > 0)
            {
                long biosGB = GB(b.BiosMaxMB);
                if (b.BiosMaxMB <= b.ToplamMB)
                {
                    b.BiosMaxGuvenilir = false;
                    neden = "BIOS'un bildirdiği azami kapasite (" + Bicim.Boyut(b.BiosMaxMB) + ") takılı miktara eşit ya da ondan küçük. " +
                            "Birçok BIOS bu alanı yalnızca takılı belleğe ya da eski bir değere göre doldurur; bu değer güvenilmez sayıldı.";
                }
                else if (b.BiosMaxMB < 2048 || biosGB > 16384)
                {
                    b.BiosMaxGuvenilir = false;
                    neden = "BIOS'un bildirdiği azami kapasite (" + Bicim.Boyut(b.BiosMaxMB) + ") anlamsız görünüyor; güvenilmez sayıldı.";
                }
                else
                {
                    int makul = b.Tur == "DDR5" ? 32 : (b.Tur == "DDR4" ? 16 : 0);
                    if (makul > 0 && biosGB < makul && d.ToplamYuva >= 2 && !b.Mobil)
                    {
                        b.BiosMaxGuvenilir = false;
                        neden = "BIOS'un bildirdiği azami kapasite (" + biosGB + " GB) " + b.Tur + " bir sistem için alışılmadık derecede düşük; eski/yanlış bir değer olabilir.";
                    }
                }
                if ((b.YuvaSupheli || b.LehimliYuvaSupheli) && b.BiosMaxGuvenilir)
                {
                    b.BiosMaxGuvenilir = false;
                    neden = "BIOS boş yuvayı listelemediği için aynı tablodaki azami kapasite (" + Bicim.Boyut(b.BiosMaxMB) + ") de eksik olabilir; güvenilmez sayıldı.";
                }
            }

            int azami = 0;
            string kaynak = "";
            if (b.BiosMaxGuvenilir)
            {
                azami = (int)Math.Min(int.MaxValue, GB(b.BiosMaxMB));
                kaynak = "Üretici BIOS'u";
            }
            if (b.P.Bulundu && b.P.MaxKapasiteGB > 0 && (azami == 0 || b.P.MaxKapasiteGB < azami))
            {
                azami = b.P.MaxKapasiteGB;
                bool tahmini = (b.P.Kesinlik ?? "").IndexOf("Tahmin", StringComparison.OrdinalIgnoreCase) >= 0;
                kaynak = tahmini ? "İşlemci sınırı (aile bazında tahmini)" : "İşlemci sınırı (resmi)";
            }
            int yuvaSay = b.YuvaSupheli ? 2 : (b.LehimliYuvaSupheli ? b.Lehimli.Count + 1 : d.ToplamYuva);
            int sokulebilirYuva = Math.Max(0, yuvaSay - b.Lehimli.Count);
            int pratik = (b.ModulBasiGuvenli > 0 && yuvaSay > 0) ? b.LehimliGB + sokulebilirYuva * b.ModulBasiGuvenli : 0;
            if (azami == 0 && pratik > 0)
            {
                azami = pratik;
                kaynak = "Tahmini";
            }
            d.AzamiGB = azami;
            d.AzamiGBKaynagi = kaynak;
            if (pratik > 0 && pratik < azami && !b.Sunucu)
            {
                d.PratikAzamiGB = pratik;
                d.PratikAzamiMetni = (b.Lehimli.Count > 0 ? b.LehimliGB + " GB lehimli + " : "") + sokulebilirYuva + " yuva x " + b.ModulBasiGuvenli + " GB" +
                                     (b.YuvaSupheli ? ", 2 yuva varsayımı" : (b.LehimliYuvaSupheli ? ", yuva varsa" : ""));
            }

            if (neden != "")
            {
                bool etkiler = b.P.Bulundu && b.P.MaxKapasiteGB > 0;
                d.Bulgular.Add(new Bulgu(etkiler ? Seviye.Bilgi : Seviye.Uyari, "BIOS'un azami kapasite bilgisi güvenilmez",
                    neden + (etkiler ? "\nBunun yerine işlemcinin resmi sınırı (" + b.P.MaxKapasiteGB + " GB) kullanıldı; gerçek sınır için üretici belgesine bak." :
                                       "\nGerçek sınır için üretici belgesine bak.")));
            }

            if (azami > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Desteklenen azami bellek: " + azami + " GB (kaynak: " + kaynak + ").");
                if (pratik > 0 && pratik < azami && !b.Sunucu)
                {
                    sb.Append("\nPratikte: " + (b.Lehimli.Count > 0 ? b.LehimliGB + " GB lehimli + " : "") + sokulebilirYuva + " yuva x " + b.ModulBasiGuvenli +
                              " GB = " + pratik + " GB" + (b.YuvaSupheli ? " (2 yuva varsayımıyla)" : "") + ".");
                    if (b.Tur == "DDR5")
                        sb.Append(" DDR5'te 48/64 GB'lık modüller de var ama yeni ve pahalı; desteği BIOS sürümüne bağlı. 32 GB modül güvenli seçimdir.");
                }
                if (b.ToplamGB >= azami)
                    d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Azami kapasitedesin (" + b.ToplamGB + " GB)", sb.ToString() + "\nBu bilgisayara daha fazla RAM eklenemez."));
                else
                    d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Azami kapasite: " + azami + " GB", sb.ToString()));
            }
        }

        // =====================================================================
        //  Kanal analizi
        // =====================================================================

        /// <summary>
        /// Yuva/banka adından kanal anahtarı çıkarır ("C0A", "B", "P0A" ...). Bulamazsa "".
        /// tahmini=true: yalnızca "BANK n" gibi zayıf bir ipucundan çıkarıldı.
        /// </summary>
        internal static string KanalAnahtari(BellekYuvasi y, out bool tahmini)
        {
            tahmini = false;
            if (y == null) return "";
            string yuva = (y.Yuva ?? "").ToUpperInvariant();
            string banka = (y.Banka ?? "").ToUpperInvariant();
            string m = yuva + " | " + banka;

            string ctrl = "";
            Match mc = Regex.Match(m, @"CONTROLLER[\s_\-]*(\d+)", RegexOptions.CultureInvariant);
            if (mc.Success) ctrl = "C" + mc.Groups[1].Value;

            string soket = "";
            Match ms = Regex.Match(m, @"(?<![A-Z0-9])(?:CPU|NODE|P)[\s_]*(\d)(?!\d)", RegexOptions.CultureInvariant);
            if (ms.Success) soket = "P" + ms.Groups[1].Value;

            string kanal = "";
            Match mk = Regex.Match(m, @"(?<![A-Z])CH(?:ANNEL)?[\s_\-]*([A-H]|\d{1,2})(?![A-Z0-9])", RegexOptions.CultureInvariant);
            if (mk.Success) kanal = mk.Groups[1].Value;
            if (kanal == "")
            {
                Match md = Regex.Match(m, @"DIMM[\s_\-]*([A-H])(?![A-Z])", RegexOptions.CultureInvariant);
                if (md.Success) kanal = md.Groups[1].Value;
            }
            if (kanal == "")
            {
                Match mb = Regex.Match(yuva, @"^\s*(?:SLOT[\s_\-]*)?([A-H])[\s_\-]?\d\s*$", RegexOptions.CultureInvariant);
                if (mb.Success) kanal = mb.Groups[1].Value;
            }

            if (kanal != "") return soket + ctrl + kanal;
            if (ctrl != "") return soket + ctrl;

            Match mbank = Regex.Match(banka, @"BANK[\s_\-]*(\d+)", RegexOptions.CultureInvariant);
            if (mbank.Success)
            {
                tahmini = true;
                return "BANK" + mbank.Groups[1].Value;
            }
            return "";
        }

        static void KanalAnalizi(Baglam b)
        {
            DanismanSonucu d = b.D;
            int n = b.Dolu.Count;

            if (b.Sunucu)
            {
                d.KanalDurumu = "Sunucu/iş istasyonu: " + n + " modül (kanal yerleşimi üretici kılavuzuna göre)";
                d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Sunucu bellek yerleşimi",
                    "Sunucu ve iş istasyonlarında her kanala eşit sayıda, eş modül takılmalıdır (kanal sayısı genelde 4–8). " +
                    "Hangi yuvalara takılacağını üreticinin \"memory population guidelines\" belgesinden kontrol et."));
                return;
            }
            if (b.Lehimli.Count == n && b.Tur.StartsWith("LP", StringComparison.Ordinal))
            {
                d.KanalDurumu = "Lehimli " + b.Tur + " (kanal düzeni üretici tasarımı, değiştirilemez)";
                return;
            }
            if (b.P.Bulundu && b.P.KanalSayisi == 1)
            {
                b.TekKanal = true;
                d.KanalDurumu = "Tek kanal (işlemci tek kanallı)";
                return;
            }

            List<long> boyutlar = b.Dolu.Select(y => y.BoyutMB).Distinct().ToList();
            if (n == 1)
            {
                b.TekKanal = true;
                d.KanalDurumu = "Tek kanal (1 modül)";
            }
            else
            {
                List<string> anahtarlar = new List<string>();
                bool hepsiKesin = true;
                foreach (BellekYuvasi y in b.Dolu)
                {
                    bool t;
                    string k = KanalAnahtari(y, out t);
                    if (k == "" || t) hepsiKesin = false;
                    anahtarlar.Add(k);
                }
                int farkli = anahtarlar.Where(k => k != "").Distinct().Count();
                // Aynı fiziksel konum adı iki kez gelirse (ör. iki modül de "ChannelA-DIMM0") bu BIOS etiket hatasıdır.
                bool konumlarFarkli = b.Dolu.Select(y => ((y.Yuva ?? "") + "|" + (y.Banka ?? "")).Trim().ToUpperInvariant()).Distinct().Count() == n;
                bool kesinKanal = hepsiKesin || farkli >= 2 || (n == 2 && d.ToplamYuva == 2);
                if (hepsiKesin && farkli == 1 && d.BosYuva > 0 && konumlarFarkli)
                {
                    b.AyniKanal = true;
                    b.TekKanal = true;
                    d.KanalDurumu = "Tek kanal (modüller aynı kanalda)";
                }
                else if (hepsiKesin && farkli == 1)
                {
                    b.CiftKanal = b.Kanal2;
                    b.Flex = boyutlar.Count > 1 && b.BoyutBilinmeyen == 0;
                    b.KanalKesin = false;
                    d.KanalDurumu = "Doğrulanamadı (BIOS yuva adları tutarsız)";
                    d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Yuva adları tutarsız: kanal durumu doğrulanamadı",
                        "BIOS dolu yuvaların hepsini aynı kanalda bildiriyor ama taşınacak boş yuva yok ya da aynı yuva adı birden fazla kez geçiyor; " +
                        "bu genelde BIOS etiket hatasıdır. Gerçek kanal durumunu CPU-Z (Memory > Channel) ya da Görev Yöneticisi ile kontrol et."));
                }
                else
                {
                    b.CiftKanal = b.Kanal2;
                    b.Flex = boyutlar.Count > 1 && b.BoyutBilinmeyen == 0;
                    b.KanalKesin = kesinKanal;
                    if (!b.Kanal2) d.KanalDurumu = "Tek kanal";
                    else if (b.Flex) d.KanalDurumu = "Çift kanal — flex mode (farklı boyutlar)";
                    else if (kesinKanal) d.KanalDurumu = "Çift kanal (eş modüller)";
                    else d.KanalDurumu = "Muhtemelen çift kanal (yuva adlarından doğrulanamadı)";
                }
            }

            if (b.TekKanal && b.Kanal2)
            {
                if (b.AyniKanal)
                {
                    string adlar = string.Join(", ", b.Dolu.Select(y => YuvaAdi(y)).Where(s => s != "").ToArray());
                    d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Modüller aynı kanalda: tek kanal çalışıyor",
                        "Modüller (" + adlar + ") aynı bellek kanalına takılı görünüyor; bu durumda bellek tek kanalda çalışır ve bant genişliğinin yarısı kullanılmaz." +
                        "\nÜcretsiz çözüm: modüllerden birini diğer kanalın yuvasına taşı (masaüstü anakartlarda genelde A2 + B2; anakart kılavuzuna bak)."));
                }
                else
                {
                    string kosul = b.YuvaSupheli ? " (2. yuva varsa)" : "";
                    string iGpu = "Dahili grafik (iGPU) kullanıyorsan oyun ve video performansı belirgin düşer; harici ekran kartlı sistemlerde de işlemci ağırlıklı işlerde fark hissedilir.";
                    if (b.Lehimli.Count > 0 && d.BosYuva > 0)
                        d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Tek kanal: bellek bant genişliğinin yarısı kullanılıyor",
                            "Yalnızca lehimli bellek çalışıyor; boş yuvaya modül takılmadığı için işlemcinin 2 kanallı bellek yolunun yarısı boşta. " + iGpu +
                            "\nBoş yuvaya lehimliyle eş boyutta bir modül takmak teorik bant genişliğini iki katına çıkarır; gerçek kazanç işe göre değişir."));
                    else if (b.Lehimli.Count > 0 && b.LehimliYuvaSupheli)
                        d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Tek kanal: bellek bant genişliğinin yarısı kullanılıyor",
                            "Yalnızca lehimli bellek çalışıyor; işlemcinin 2 kanallı bellek yolunun yarısı boşta. " + iGpu +
                            "\nCihazda boş bir SODIMM yuvası varsa (BIOS listelemiyor olabilir), oraya lehimliyle eş boyutta bir modül takmak teorik bant genişliğini iki katına çıkarır."));
                    else if (b.Lehimli.Count > 0)
                        d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Tek kanal (tasarım gereği)",
                            "Bu tasarımda bellek tek kanalda çalışır; lehimli bellek değiştirilemez ve ek yuva yok."));
                    else
                        d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Tek kanal: bellek bant genişliğinin yarısı kullanılıyor",
                            "Tek modül takılı olduğu için bellek tek kanalda çalışıyor; işlemcinin 2 kanallı bellek yolunun yarısı boşta. " + iGpu +
                            "\nAynı modülden ikinci bir tane takmak" + kosul + " teorik bant genişliğini iki katına çıkarır; gerçek kazanç işe göre değişir."));
                }
                if (b.Tur == "DDR5")
                {
                    d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "DDR5 alt kanalları çift kanal değildir",
                        "Her DDR5 modülünün içinde iki adet 32-bit alt kanal vardır; bazı araçlar bunu \"2 x 32-bit\" ya da \"Dual\" diye gösterebilir. " +
                        "Bu gerçek çift kanal değildir: tek modül yine toplam 64 bit genişliğinde tek kanaldır. Çift kanal için yine 2 modül gerekir."));
                }
            }
            else if (b.CiftKanal)
            {
                if (b.Flex)
                {
                    List<int> gbler = b.Dolu.Select(y => GB(y.BoyutMB)).OrderBy(x => x).ToList();
                    int kucuk = gbler[0];
                    int fazla = b.ToplamGB - kucuk * 2;
                    string liste = string.Join(" + ", gbler.Select(x => x.ToString(Inv)).ToArray()) + " GB";
                    d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Farklı boyutlu modüller: flex mode",
                        liste + " takılı. Bu durumda ilk 2 x " + kucuk + " GB = " + (kucuk * 2) + " GB çift kanal, kalan " + Math.Max(0, fazla) +
                        " GB tek kanal çalışır (Intel Flex Memory; AMD'de benzer). Çalışır, sorun değildir; en iyi performans için eş boyutlu modüller tercih edilir."));
                }
                else if (b.KanalKesin)
                {
                    d.Bulgular.Add(new Bulgu(Seviye.Iyi, "Çift kanal çalışıyor",
                        n + " modül iki kanala dağılmış; bellek bant genişliğinin tamamı kullanılıyor."));
                }
                else if (!d.KanalDurumu.StartsWith("Doğrulanamadı", StringComparison.Ordinal))
                {
                    d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Çift kanal muhtemel ama doğrulanamadı",
                        "Yuva adları kanalı göstermiyor; anakart kılavuzundaki yuva-kanal eşleşmesinden ya da CPU-Z (Memory > Channel) ile doğrula. " +
                        "Bazı 4 yuvalı kartlarda yan yana iki yuva aynı kanaldadır."));
                }
            }
        }

        // =====================================================================
        //  Hız analizi
        // =====================================================================

        static void HizAnalizi(Baglam b)
        {
            DanismanSonucu d = b.D;
            int calisan = b.CalisanHiz;
            List<int> etiketler = b.Dolu.Select(y => EtiketHizi(y)).Where(h => h > 0).ToList();
            if (calisan <= 0 || etiketler.Count == 0) return;
            int etiketMin = etiketler.Min(), etiketMax = etiketler.Max();
            bool karisik = etiketler.Distinct().Count() > 1;
            int plat = b.PlatformHiz;
            string islemci = b.P.Bulundu && !string.IsNullOrEmpty(b.P.Aile) ? " (" + b.P.Aile + ")" : "";

            if (etiketMax > calisan)
            {
                if (plat > 0 && plat <= calisan)
                {
                    d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Hız: modül " + etiketMax + ", çalışan " + calisan + " MT/s — Normal (işlemci sınırı)",
                        "Modülün etiket hızı " + etiketMax + " MT/s, şu an " + calisan + " MT/s'de çalışıyor. Bu normal: işlemci" + islemci +
                        " resmi olarak en fazla " + plat + " MT/s " + b.Tur + " destekliyor. Daha hızlı RAM almak bu bilgisayarda hız kazandırmaz; " +
                        "yeni modül " + etiketMax + " MT/s de olsa " + calisan + " MT/s'de çalışır."));
                }
                else if (karisik && calisan <= etiketMin)
                {
                    d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Farklı hızlarda modüller: hepsi en yavaşa iner",
                        "Takılı modüllerin etiket hızları farklı (" + string.Join(", ", etiketler.Distinct().OrderBy(x => x).Select(x => x.ToString(Inv)).ToArray()) +
                        " MT/s). Bellek denetleyicisi tüm modülleri en yavaşın hızında (" + calisan + " MT/s) çalıştırır." +
                        (plat > calisan ? " İşlemci " + plat + " MT/s'ye kadar destekliyor; eş ve hızlı modüllerle bu hız kazanılabilir." : "") +
                        "\nYeni alımda mevcutla aynı hızda (ya da hepsini eş kitle) almak en temizidir."));
                }
                else if (!b.Mobil && !b.Sunucu)
                {
                    d.Bulgular.Add(new Bulgu(Seviye.Uyari, "XMP/EXPO kapalı olabilir: bellek etiket hızının altında",
                        "Modüller " + etiketMax + " MT/s için etiketli ama " + calisan + " MT/s'de çalışıyor. Masaüstünde bu genelde BIOS'ta XMP (Intel) / EXPO ya da DOCP (AMD) " +
                        "profilinin kapalı olmasından kaynaklanır. BIOS'a girip profili açarsan bellek etiket hızında çalışır." +
                        (plat > 0 ? " (İşlemcinin resmi azamisi: " + plat + " MT/s; üstü hız aşırtma sayılır ama XMP kitlerinde yaygındır.)" : "") +
                        (b.Dolu.Count >= 4 ? "\n4 modül takılıyken yüksek XMP hızları her zaman kararlı olmayabilir; kararsızlıkta bir kademe düşür." : "")));
                }
                else
                {
                    d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Hız: modül " + etiketMax + ", çalışan " + calisan + " MT/s",
                        "Modül daha yüksek hız için etiketli ama " + calisan + " MT/s'de çalışıyor. Dizüstü ve kompakt cihazların BIOS'u genelde hız ayarına izin vermez; " +
                        "hızı işlemci ya da üretici belirler." +
                        (plat > 0 ? " İşlemci tablosuna göre azami " + plat + " MT/s; üretici bu cihazda daha düşük bir hız seçmiş olabilir." : " İşlemci sınırı olabilir.") +
                        " Bu bir arıza değildir."));
                }
            }
            else
            {
                if (plat > 0 && calisan < plat)
                {
                    d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Modüller işlemcinin desteklediğinden yavaş",
                        "Modüller " + calisan + " MT/s'de (etiket hızlarında) çalışıyor; işlemci " + plat + " MT/s'ye kadar destekliyor. " +
                        "Daha hızlı RAM sınırlı fark yaratır (günlük işte birkaç yüzde); kapasite yetiyorsa yalnızca hız için RAM değiştirmeye değmez. " +
                        "Hepsini değiştireceksen yeni kiti " + plat + " MT/s seçebilirsin."));
                }
                else
                {
                    d.Bulgular.Add(new Bulgu(Seviye.Iyi, "Bellek etiket hızında çalışıyor (" + calisan + " MT/s)",
                        "Modüller tam hızında çalışıyor; hız açısından yapılacak bir şey yok."));
                }
            }

            if (b.Tur == "DDR5" && b.Dolu.Count >= 4 && !b.Sunucu)
            {
                d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "4 modül DDR5: hız düşer",
                    "DDR5'te kanal başına iki modül (4 modül) takıldığında bellek denetleyicisi hızı belirgin düşürür (ör. 5600 → 4400–5000 MT/s). " +
                    "Yüksek kapasite için 2 büyük modül genelde daha hızlıdır."));
            }
        }

        // =====================================================================
        //  Modül karışıklığı
        // =====================================================================

        static void KarisiklikAnalizi(Baglam b)
        {
            DanismanSonucu d = b.D;
            if (b.Dolu.Count < 2) return;

            List<string> turler = b.Dolu.Select(y => TurAdi(y)).Where(t => t != "").Distinct().ToList();
            if (turler.Count > 1)
                d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Farklı bellek türleri bildirildi (" + string.Join(", ", turler.ToArray()) + ")",
                    "Farklı nesil bellekler (ör. DDR4 ve DDR5) aynı anda takılamaz; bu büyük olasılıkla BIOS'un hatalı bildirimidir. Satın almadan önce modül etiketine bak."));

            List<BellekYuvasi> s = b.Sokulebilir;
            if (s.Count >= 2)
            {
                List<string> ureticiler = s.Select(y => (y.Uretici ?? "").Trim()).Where(u => u != "").Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                List<string> parcalar = s.Select(y => ParcaNoTemiz(y.ParcaNo)).Where(p => p != "").Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (ureticiler.Count > 1)
                    d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Farklı marka modüller (" + string.Join(", ", ureticiler.ToArray()) + ")",
                        "Farklı markalar aynı tür/hız/voltajda genelde sorunsuz çalışır; nadiren kararsızlık olur. Sorun yaşarsan önce modülleri tek tek dene."));
                else if (parcalar.Count > 1)
                    d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Aynı marka, farklı parça numaraları",
                        "Modüller aynı markadan ama farklı parça numaralı (" + string.Join(", ", parcalar.ToArray()) + "). Genelde sorun olmaz; en garantilisi birebir eş modüllerdir."));

                List<int> voltajlar = s.Select(y => y.YapilandirilmisVoltajMv).Where(v => v > 0).Distinct().ToList();
                if (voltajlar.Count > 1)
                    d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Farklı voltajlarda modüller",
                        "Modüllerin çalışma voltajları farklı (" + string.Join(", ", voltajlar.OrderBy(x => x).Select(x => VoltajMetni(x)).ToArray()) +
                        "). Hepsi tek voltajda çalışmak zorunda; bu kararsızlığa yol açabilir. Eş voltajlı modül kullan."));

                List<int> ranklar = s.Select(y => y.Rank).Where(r => r > 0).Distinct().ToList();
                if (ranklar.Count > 1)
                    d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Farklı rank sayısında modüller",
                        "Modüllerin rank (sıra) sayıları farklı (" + string.Join(", ", ranklar.OrderBy(x => x).Select(x => x + "R").ToArray()) +
                        "). Genelde çalışır ama eş rank daha kararlıdır ve bazı sistemlerde hızı düşürebilir."));

                int eccli = s.Count(y => y.Ecc);
                if (eccli > 0 && eccli < s.Count)
                    d.Bulgular.Add(new Bulgu(Seviye.Uyari, "ECC ve ECC'siz modüller karışık",
                        "Bazı modüller ECC'li, bazıları değil. Bu durumda ECC devre dışı kalır ya da sistem açılmayabilir. Hepsini aynı tipte kullan."));

                int kayitli = s.Count(y => KayitliMi(y));
                if (kayitli > 0 && kayitli < s.Count)
                    d.Bulgular.Add(new Bulgu(Seviye.Kritik, "Registered ve unbuffered modüller karışık",
                        "Registered (RDIMM) ve normal (unbuffered) modüller birlikte çalışmaz. Hepsi aynı tipte olmalı."));

                bool birebir = b.HepsiEs && parcalar.Count == 1 && s.All(y => ParcaNoTemiz(y.ParcaNo) != "");
                if (birebir && turler.Count <= 1)
                    d.Bulgular.Add(new Bulgu(Seviye.Iyi, "Modüller birebir eş",
                        s.Count + " modül aynı boyut, hız ve parça numarasına (" + parcalar[0] + ") sahip. Bu en kararlı yapılandırmadır."));
            }
        }

        // =====================================================================
        //  Tür, voltaj, ECC, eski platform
        // =====================================================================

        static void TurVeVoltajAnalizi(Baglam b)
        {
            DanismanSonucu d = b.D;
            if (b.Tur == "")
            {
                d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Bellek türü okunamadı",
                    "BIOS modüllerin türünü (DDR4/DDR5 ...) bildirmedi. Satın almadan önce modül etiketinden ya da üretici belgesinden türü öğren; farklı nesiller birbirine takılmaz."));
            }
            if (b.Ddr3L)
            {
                d.Bulgular.Add(new Bulgu(Seviye.Kritik, "DDR3L (1,35 V) almalısın, 1,5 V DDR3 takma",
                    "Bu bilgisayardaki bellek 1,35 V'ta çalışan DDR3L. Dizüstü ve yeni nesil (Intel 6. nesil gibi) platformlar normal 1,5 V DDR3'ü desteklemez; " +
                    "takılırsa açılmayabilir, kararsız çalışabilir ya da uzun vadede bellek denetleyicisine zarar verebilir." +
                    "\nMağazada \"DDR3L\" ya da \"1.35V\" yazan modül seç (çift voltajlı 1,35/1,5 V modüller de uygundur)."));
            }
            if (b.Kayitli)
            {
                d.Bulgular.Add(new Bulgu(Seviye.Kritik, "ECC Registered bellek: alacağın modül de Registered olmalı",
                    "Bu sistem " + (b.Lrdimm ? "LRDIMM (Load Reduced)" : "RDIMM (Registered)") + (b.Ecc ? " ECC" : "") +
                    " bellek kullanıyor. Normal masaüstü (unbuffered) RAM bu sistemde çalışmaz. Aynı tip (" + (b.Lrdimm ? "LRDIMM" : "RDIMM") +
                    "), aynı rank ve mümkünse aynı parça numarası al."));
            }
            else if (b.Ecc)
            {
                d.Bulgular.Add(new Bulgu(Seviye.Kritik, "ECC bellek: alacağın modül de ECC olmalı",
                    "Bu sistemde ECC'li (hata düzeltmeli) unbuffered bellek var. ECC'siz modül eklemek ECC'yi devre dışı bırakır; " +
                    "Registered (RDIMM) modül ise hiç çalışmaz. \"ECC Unbuffered (UDIMM)\" modül al."));
            }
            if (b.Tur != "" && b.P.Bulundu && b.P.DesteklenenTurler.Count > 0)
            {
                bool listede = Platform.TurDestekleniyor(b.P, b.Tur);   // DDR3~DDR3L, LPDDRn~LPDDRnX eşdeğer
                if (!listede)
                    d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Tür, işlemci tablosuyla uyuşmuyor",
                        "BIOS " + b.Tur + " bildiriyor ama işlemci tablosunda " + string.Join(", ", b.P.DesteklenenTurler.ToArray()) +
                        " yazıyor. İşlemci tanıması ya da BIOS bilgisi hatalı olabilir; satın almadan önce modül etiketinden doğrula."));
            }
            if (b.Tur == "DDR3" || b.Tur == "DDR2" || b.Tur == "DDR")
            {
                d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Eski platform (" + (b.Ddr3L ? "DDR3L" : b.Tur) + ")",
                    (b.Tur == "DDR3"
                        ? "DDR3 dönemi bir bilgisayar. RAM eklemek ucuzdur ve işe yarar; ancak hâlâ mekanik disk (HDD) kullanıyorsa SSD'ye geçmek çok daha büyük hız farkı yaratır. "
                        : "Çok eski bir bellek türü; yükseltme parçası bulmak zor ve fayda sınırlı. Yeni cihaz düşünmek daha mantıklı olabilir. ") +
                    "Bu tür artık yeni üretilmiyor; stok/ikinci el ürünlerde garanti süresine dikkat et."));
            }
        }

        // =====================================================================
        //  Senaryolar
        // =====================================================================

        static void Senaryolar(Baglam b)
        {
            DanismanSonucu d = b.D;
            if (!b.AlinabilirTur) return;
            if (b.BoyutBilinmeyen > 0) return;   // toplam bilinmiyor: yanlış kit/ekleme önerme

            // Hedef kapasite (ihtiyaca göre)
            int hedef = SonrakiKademe(b.ToplamGB);
            if (b.ToplamGB < 8) hedef = Math.Max(hedef, 8);
            if (b.Ihtiyac == IhtiyacYetersiz && b.TaahhutOrani > 1.5) hedef = SonrakiKademe(hedef);
            b.HedefGB = hedef;

            int cap = b.ModulBasiGuvenli;
            bool bosVar = d.BosYuva > 0 || b.YuvaSupheli;
            string ad2 = b.YuvaSupheli ? "2. yuva varsa: " : "";

            // a) Boş yuvaya aynısını ekle
            if (b.Lehimli.Count == 0 && b.Sokulebilir.Count > 0 && b.HepsiEs && bosVar && b.OrnekGB > 0)
            {
                int ekle = b.YuvaSupheli ? 1 : (b.Sokulebilir.Count == 1 ? 1 : Math.Min(d.BosYuva, b.Sokulebilir.Count));
                int sonuc = b.ToplamGB + ekle * b.OrnekGB;
                if (ekle > 0 && Sigar(b, sonuc))
                {
                    b.EkleAdet = ekle;
                    YukseltmeSenaryosu s = new YukseltmeSenaryosu();
                    s.Ad = Onek(ad2, ekle == 1 ? "Boş yuvaya aynı modülden 1 tane ekle" : "Boş yuvalara aynı modülden " + ekle + " tane ekle");
                    s.Alinacaklar.Add(Modul(b, ekle, b.OrnekGB, EkleHizi(b), "Mevcut modülün birebir eşi"));
                    s.SonucToplamGB = sonuc;
                    s.CiftKanal = b.Kanal2 && !b.Sunucu && (b.Dolu.Count + ekle) >= 2;
                    s.Aciklama = "Mevcut " + b.OrnekGB + " GB " + TurHiz(b.AlimTuru, EkleHizi(b)) + " " + b.Form + " modülün aynısından " + ekle + " tane ekle: toplam " + sonuc + " GB" +
                                 (s.CiftKanal && b.TekKanal ? ", tek kanaldan çift kanala geçiş. En iyi maliyet/fayda: hem kapasite iki katına çıkar hem bant genişliği." :
                                  (s.CiftKanal ? ", çift kanal korunur." : "."));
                    string parca = ParcaNoTemiz(b.Ornek != null ? b.Ornek.ParcaNo : "");
                    if (parca != "")
                        s.Notlar.Add("İdeal: aynı parça numarası (" + MarkaParca(b.Ornek) + "). Bulamazsan aynı boyut/tür/hız/voltajda başka marka da genelde sorunsuz çalışır.");
                    else
                        s.Notlar.Add("Aynı boyut, tür, hız ve voltajda modül seç; mümkünse aynı marka.");
                    if (b.YuvaSupheli)
                        s.Notlar.Add("Önce 2. yuvanın varlığını doğrula: üretici belgesi (teknik özellikler) ya da alt kapağı açıp bakarak.");
                    if (!b.BiosMaxGuvenilir && b.BiosMaxMB > 0 && GB(b.BiosMaxMB) < sonuc)
                        s.Notlar.Add("BIOS azami " + Bicim.Boyut(b.BiosMaxMB) + " bildiriyor; bu değer güvenilmez görünüyor ama " + sonuc + " GB desteğini üretici belgesinden doğrula.");
                    if (b.Dolu.Count + ekle >= 4 && b.Tur == "DDR5")
                        s.Notlar.Add("DDR5'te 4 modülle bellek hızı belirgin düşer; 2 büyük modüllü kit daha hızlı olabilir.");
                    else if (b.Dolu.Count + ekle >= 4)
                        s.Notlar.Add("4 modülde (kanal başına 2) hız bir kademe düşebilir; normaldir.");
                    if (!b.Mobil && b.Dolu.Count == 1)
                        s.Notlar.Add("Yeni modülü diğer kanalın yuvasına tak (genelde A2 + B2 eşleşmesi; anakart kılavuzuna bak).");
                    b.SenEkle = s;
                }
            }

            // b) Boş yuvaya daha büyük modül ekle (flex mode)
            if (b.Lehimli.Count == 0 && b.Sokulebilir.Count == 1 && bosVar && b.OrnekGB > 0 && !b.Sunucu)
            {
                int yeni = b.OrnekGB * 2;
                int sonuc = b.ToplamGB + yeni;
                if (yeni <= cap && Sigar(b, sonuc))
                {
                    YukseltmeSenaryosu s = new YukseltmeSenaryosu();
                    s.Ad = Onek(ad2, "Boş yuvaya daha büyük modül ekle (" + yeni + " GB)");
                    s.Alinacaklar.Add(Modul(b, 1, yeni, EkleHizi(b), "Daha büyük tek modül (flex mode)"));
                    s.SonucToplamGB = sonuc;
                    s.CiftKanal = false;
                    s.Flex = b.Kanal2;
                    s.Aciklama = "Boş yuvaya " + yeni + " GB modül ekle: toplam " + sonuc + " GB. İlk 2 x " + b.OrnekGB + " GB çift kanal, kalan " + (yeni - b.OrnekGB) + " GB tek kanal çalışır (flex mode).";
                    s.Notlar.Add("Kısmi çift kanal: kapasite daha çok artar ama tam simetrik değildir. Günlük işte fark küçüktür.");
                    s.Notlar.Add("Hız ve voltaj mevcut modülle aynı olmalı.");
                    if (b.YuvaSupheli)
                        s.Notlar.Add("Önce 2. yuvanın varlığını doğrula.");
                    if (!b.BiosMaxGuvenilir && b.BiosMaxMB > 0 && GB(b.BiosMaxMB) < sonuc)
                        s.Notlar.Add("BIOS azami " + Bicim.Boyut(b.BiosMaxMB) + " bildiriyor; " + sonuc + " GB desteğini üretici belgesinden doğrula.");
                    b.SenBuyukEkle = s;
                }
            }

            // b2) Farklı boyutlarda: yalnız küçük modül(ler)i büyüğün eşiyle değiştir (sağlam büyük modül atılmaz)
            if (b.Lehimli.Count == 0 && !b.Sunucu && cap > 0 && b.Sokulebilir.Count >= 2 &&
                b.Sokulebilir.Select(y => GB(y.BoyutMB)).Distinct().Count() > 1)
            {
                int buyuk = b.Sokulebilir.Max(y => GB(y.BoyutMB));
                List<BellekYuvasi> kucukler = b.Sokulebilir.Where(y => GB(y.BoyutMB) < buyuk).ToList();
                int adet = kucukler.Count;
                int kucuklerGB = kucukler.Sum(y => GB(y.BoyutMB));
                int sonuc = b.ToplamGB - kucuklerGB + adet * buyuk;
                if (adet > 0 && buyuk <= cap && Sigar(b, sonuc) && sonuc > b.ToplamGB)
                {
                    YukseltmeSenaryosu s = new YukseltmeSenaryosu();
                    s.Ad = "Küçük modül" + (adet > 1 ? "leri" : "ü") + " büyükle eş boyutta değiştir (" + adet + " x " + buyuk + " GB)";
                    s.Alinacaklar.Add(Modul(b, adet, buyuk, EkleHizi(b), "Mevcut " + buyuk + " GB modülün eşi"));
                    foreach (BellekYuvasi y in kucukler) s.Cikarilacaklar.Add(YuvaAdi(y) != "" ? YuvaAdi(y) : "Yuva");
                    s.SonucToplamGB = sonuc;
                    s.CiftKanal = b.Kanal2 && b.Sokulebilir.Count % 2 == 0;
                    s.Aciklama = "Küçük modül" + (adet > 1 ? "leri" : "ü") + " çıkarıp yerine mevcut " + buyuk + " GB modülün eşinden " + adet + " tane tak: toplam " + sonuc + " GB" +
                                 (s.CiftKanal ? ", simetrik çift kanal." : ".") + " Mevcut " + buyuk + " GB modül yerinde kalır; kit almaktan ucuzdur.";
                    BellekYuvasi buyukModul = b.Sokulebilir.First(y => GB(y.BoyutMB) == buyuk);
                    string parca = ParcaNoTemiz(buyukModul.ParcaNo);
                    s.Notlar.Add("Kalan modülle aynı tür/hız/voltaj" + (parca != "" ? "; mümkünse aynı parça no (" + MarkaParca(buyukModul) + ")." : "; mümkünse aynı marka."));
                    if (!b.BiosMaxGuvenilir && b.BiosMaxMB > 0 && GB(b.BiosMaxMB) < sonuc)
                        s.Notlar.Add("BIOS azami " + Bicim.Boyut(b.BiosMaxMB) + " bildiriyor; " + sonuc + " GB desteğini üretici belgesinden doğrula.");
                    b.SenKucukDegistir = s;
                }
            }

            // c) Hepsini eş kitle değiştir
            int yuvaSay = b.YuvaSupheli ? 2 : d.ToplamYuva;
            int sokulebilirYuva = yuvaSay - b.Lehimli.Count;
            if (b.Lehimli.Count == 0 && b.Sokulebilir.Count > 0 && sokulebilirYuva >= 1 && !b.Sunucu && cap > 0)
            {
                int n = (b.Kanal2 && sokulebilirYuva >= 2) ? 2 : 1;
                int kit = hedef;
                if (b.SenEkle != null && b.Dolu.Count + b.EkleAdet == n && kit / n == b.OrnekGB) kit = SonrakiKademe(kit);
                int modul = kit / n;
                if (modul > cap && n == 2 && sokulebilirYuva >= 4) { n = 4; modul = kit / n; }
                if (modul > cap) { modul = cap; kit = n * cap; }
                while (kit > b.ToplamGB && !Sigar(b, kit)) { kit /= 2; modul = kit / n; }
                // Aynı toplamı tek modül ekleyerek veren yol varsa kit gereksiz pahalıdır (DDR5'te 4 modül istisnası hariç).
                bool ekleAyni = b.SenEkle != null && kit <= b.SenEkle.SonucToplamGB && !(b.Tur == "DDR5" && b.Dolu.Count + b.EkleAdet >= 4);
                if (kit > b.ToplamGB && modul >= 1 && !ekleAyni)
                {
                    YukseltmeSenaryosu s = new YukseltmeSenaryosu();
                    s.Ad = Onek(ad2, n > 1 ? "Hepsini eş kitle değiştir (" + n + " x " + modul + " GB)" : "Daha büyük tek modülle değiştir (1 x " + modul + " GB)");
                    s.Alinacaklar.Add(Modul(b, n, modul, KitHizi(b), n > 1 ? n + "'li eş kit (aynı paket)" : "Tek modül"));
                    foreach (BellekYuvasi y in b.Sokulebilir) s.Cikarilacaklar.Add(YuvaAdi(y) != "" ? YuvaAdi(y) : "Yuva");
                    s.SonucToplamGB = kit;
                    s.CiftKanal = n >= 2 && b.Kanal2;
                    s.Aciklama = "Mevcut " + (b.Sokulebilir.Count > 1 ? b.Sokulebilir.Count + " modülü" : "modülü") + " çıkarıp " + n + " x " + modul + " GB " + TurHiz(b.AlimTuru, KitHizi(b)) + " " + b.Form +
                                 (n > 1 ? " eş kit tak" : " modül tak") + ": toplam " + kit + " GB" + (s.CiftKanal ? ", çift kanal." : ".");
                    if (n > 1) s.Notlar.Add("Kit (aynı paketteki modüller) al: eşleşme ve uyumluluk garantilidir.");
                    s.Notlar.Add("Çıkan modüller başka bir bilgisayarda kullanılabilir ya da yedek olarak saklanabilir.");
                    if (b.Tur == "DDR5" && modul > 32)
                        s.Notlar.Add("48/64 GB DDR5 modül desteği BIOS sürümüne bağlıdır; önce BIOS'u güncelle ve üretici belgesinden doğrula.");
                    if (n >= 4 && b.Tur == "DDR5")
                        s.Notlar.Add("DDR5'te 4 modülle hız belirgin düşer.");
                    if (b.YuvaSupheli && n >= 2)
                        s.Notlar.Add("Önce 2. yuvanın varlığını doğrula.");
                    if (!b.BiosMaxGuvenilir && b.BiosMaxMB > 0 && GB(b.BiosMaxMB) < kit)
                        s.Notlar.Add("BIOS azami " + Bicim.Boyut(b.BiosMaxMB) + " bildiriyor; " + kit + " GB desteğini üretici belgesinden doğrula.");
                    b.SenKit = s;
                }
            }

            // c2) Yuva şüpheliyse: tek yuva varsa daha büyük tek modül
            if (b.YuvaSupheli && b.Sokulebilir.Count == 1 && cap > 0)
            {
                int modul = Math.Min(cap, SonrakiKademe(b.OrnekGB));
                if (modul > b.ToplamGB && Sigar(b, modul))
                {
                    YukseltmeSenaryosu s = new YukseltmeSenaryosu();
                    s.Ad = "Tek yuva varsa: daha büyük tek modülle değiştir (1 x " + modul + " GB)";
                    s.Alinacaklar.Add(Modul(b, 1, modul, KitHizi(b), "Tek büyük modül"));
                    s.Cikarilacaklar.Add(YuvaAdi(b.Sokulebilir[0]) != "" ? YuvaAdi(b.Sokulebilir[0]) : "Yuva");
                    s.SonucToplamGB = modul;
                    s.CiftKanal = false;
                    s.Aciklama = "Cihazda gerçekten tek yuva varsa mevcut modülü " + modul + " GB'lık tek modülle değiştir: toplam " + modul + " GB, tek kanal kalır.";
                    if (b.BiosMaxMB > 0 && GB(b.BiosMaxMB) < modul)
                        s.Notlar.Add("BIOS azami " + Bicim.Boyut(b.BiosMaxMB) + " bildiriyor; " + modul + " GB'lık modülün tanınacağını üretici belgesinden doğrula.");
                    s.Notlar.Add("Tek kanal kalacağı için performans artışı yalnızca kapasiteden gelir.");
                    b.SenTekBuyuk = s;
                }
            }

            // d) Lehimli + yuva
            if (b.Lehimli.Count > 0 && !b.Tur.StartsWith("LP", StringComparison.Ordinal) && cap > 0)
            {
                if ((d.BosYuva > 0 || b.LehimliYuvaSupheli) && b.Sokulebilir.Count == 0)
                {
                    string yv = b.LehimliYuvaSupheli && d.BosYuva == 0 ? "Yuva varsa: " : "";
                    int es = KademeyeYuvarla(b.LehimliGB);
                    if (es >= 1 && es <= cap && Sigar(b, b.ToplamGB + es))
                    {
                        YukseltmeSenaryosu s = new YukseltmeSenaryosu();
                        s.Ad = Onek(yv, "Boş yuvaya lehimliyle eş boyutta modül ekle (" + es + " GB)");
                        if (yv != "") s.Notlar.Add("Önce ek SODIMM yuvasının varlığını üretici belgesinden (Lenovo PSREF vb.) ya da kapağı açıp doğrula.");
                        s.Alinacaklar.Add(Modul(b, 1, es, EkleHizi(b), "Lehimli bellekle eş boyut"));
                        s.SonucToplamGB = b.ToplamGB + es;
                        s.CiftKanal = b.Kanal2;
                        s.Aciklama = "Boş " + b.Form + " yuvasına " + es + " GB " + TurHiz(b.AlimTuru, EkleHizi(b)) + " modül ekle: toplam " + s.SonucToplamGB +
                                     " GB" + (s.CiftKanal ? ", simetrik çift kanal." : ".");
                        s.Notlar.Add("Lehimli bellekle aynı hız ve voltajda modül seç.");
                        b.SenLehimliEs = s;
                    }
                    int buyuk = Math.Min(cap, Math.Max(es, 1) * 2);
                    if (buyuk > es && Sigar(b, b.ToplamGB + buyuk))
                    {
                        YukseltmeSenaryosu s = new YukseltmeSenaryosu();
                        s.Ad = Onek(yv, "Boş yuvaya daha büyük modül ekle (" + buyuk + " GB)");
                        s.Alinacaklar.Add(Modul(b, 1, buyuk, EkleHizi(b), "Daha büyük modül (flex mode)"));
                        s.SonucToplamGB = b.ToplamGB + buyuk;
                        s.CiftKanal = false;
                        s.Flex = b.Kanal2;
                        s.Aciklama = "Boş yuvaya " + buyuk + " GB modül ekle: toplam " + s.SonucToplamGB + " GB. İlk 2 x " + b.LehimliGB + " GB çift kanal, kalanı tek kanal çalışır (flex mode).";
                        s.Notlar.Add("Daha çok kapasite, kısmi çift kanal.");
                        b.SenLehimliBuyuk = s;
                    }
                }
                else if (b.Sokulebilir.Count > 0)
                {
                    int mevcut = b.SokulebilirGB;
                    int yeni = mevcut < b.LehimliGB ? KademeyeYuvarla(b.LehimliGB) : Math.Min(cap, Math.Max(1, mevcut) * 2);
                    int sonuc = b.LehimliGB + yeni;
                    if (yeni > mevcut && Sigar(b, sonuc))
                    {
                        YukseltmeSenaryosu s = new YukseltmeSenaryosu();
                        s.Ad = "Yuvadaki modülü büyüt (" + mevcut + " GB → " + yeni + " GB)";
                        s.Alinacaklar.Add(Modul(b, 1, yeni, EkleHizi(b), "Yuvadaki modülün yerine"));
                        foreach (BellekYuvasi y in b.Sokulebilir) s.Cikarilacaklar.Add(YuvaAdi(y) != "" ? YuvaAdi(y) : "Yuva");
                        s.SonucToplamGB = sonuc;
                        s.CiftKanal = b.Kanal2 && yeni == b.LehimliGB;
                        s.Flex = b.Kanal2 && yeni != b.LehimliGB;
                        s.Aciklama = "Yuvadaki " + mevcut + " GB modülü " + yeni + " GB ile değiştir: toplam " + sonuc + " GB" +
                                     (s.CiftKanal ? ", simetrik çift kanal." :
                                      (s.Flex ? "; ilk 2 x " + Math.Min(yeni, b.LehimliGB) + " GB çift kanal, kalanı tek kanal çalışır (flex mode)." : "."));
                        b.SenYuvaBuyut = s;
                    }
                }
            }

            // Tavsiye seçimi (maliyet/fayda)
            YukseltmeSenaryosu tavsiye = null;
            if (b.SenLehimliEs != null) tavsiye = b.SenLehimliEs;
            else if (b.SenEkle != null && (b.TekKanal || b.YuvaSupheli)) tavsiye = b.SenEkle;
            else if (b.SenEkle != null && !(b.Tur == "DDR5" && b.Dolu.Count + b.EkleAdet >= 4 && b.SenKit != null)) tavsiye = b.SenEkle;
            else if (b.SenKucukDegistir != null && (b.SenKucukDegistir.CiftKanal || !b.Kanal2) &&
                     (b.SenKit == null || b.SenKucukDegistir.SonucToplamGB >= b.SenKit.SonucToplamGB)) tavsiye = b.SenKucukDegistir;
            else if (b.SenKit != null) tavsiye = b.SenKit;
            else if (b.SenYuvaBuyut != null) tavsiye = b.SenYuvaBuyut;
            else if (b.SenLehimliBuyuk != null) tavsiye = b.SenLehimliBuyuk;
            else if (b.SenBuyukEkle != null) tavsiye = b.SenBuyukEkle;
            else if (b.SenTekBuyuk != null) tavsiye = b.SenTekBuyuk;

            List<YukseltmeSenaryosu> hepsi = new List<YukseltmeSenaryosu>();
            foreach (YukseltmeSenaryosu s in new YukseltmeSenaryosu[] { b.SenLehimliEs, b.SenEkle, b.SenBuyukEkle, b.SenKucukDegistir, b.SenKit, b.SenTekBuyuk, b.SenLehimliBuyuk, b.SenYuvaBuyut })
                if (s != null) { s.Sunucu = b.Sunucu; hepsi.Add(s); }
            if (tavsiye != null)
            {
                tavsiye.Tavsiye = true;
                hepsi.Remove(tavsiye);
                hepsi.Insert(0, tavsiye);
            }
            if (b.Isletim32)
                foreach (YukseltmeSenaryosu s in hepsi)
                    if (s.SonucToplamGB > 4) s.Notlar.Insert(0, "Önce 64-bit Windows'a geç: 32-bit Windows 4 GB'tan fazlasını kullanamaz.");
            d.Senaryolar.AddRange(hepsi);

            // Yükseltme gerekmiyor mu?
            b.Gerekmiyor = b.Ihtiyac == IhtiyacYeterli && b.ToplamGB >= 16 && ((b.CiftKanal && b.KanalKesin) || (b.P.Bulundu && b.P.KanalSayisi == 1)) && !b.Isletim32;
        }

        /// <summary>Koşul öneki varsa ("2. yuva varsa: ") arkasındaki ilk harfi küçültür.</summary>
        static string Onek(string onek, string ad)
        {
            if (string.IsNullOrEmpty(onek) || string.IsNullOrEmpty(ad)) return onek + ad;
            return onek + char.ToLower(ad[0], CultureInfo.GetCultureInfo("tr-TR")) + ad.Substring(1);
        }

        static bool Sigar(Baglam b, int sonucGB)
        {
            return b.D.AzamiGB <= 0 || sonucGB <= b.D.AzamiGB;
        }

        static AlinacakModul Modul(Baglam b, int adet, int gb, int hiz, string aciklama)
        {
            AlinacakModul m = new AlinacakModul();
            m.Adet = adet;
            m.ModulGB = gb;
            m.Tur = b.AlimTuru;
            m.FormFaktor = b.Form;
            m.HizMTs = hiz;
            m.VoltajMv = StandartVoltaj(b.AlimTuru);
            m.Ecc = b.Ecc;
            m.Kayitli = b.Kayitli;
            m.Aciklama = aciklama;
            return m;
        }

        static int EkleHizi(Baglam b)
        {
            int h = b.Ornek != null ? EtiketHizi(b.Ornek) : 0;
            if (h <= 0) h = b.CalisanHiz;
            if (h <= 0) h = b.PlatformHiz;
            return h;
        }

        static int KitHizi(Baglam b)
        {
            int h = Math.Max(b.PlatformHiz, b.Ornek != null ? EtiketHizi(b.Ornek) : 0);
            if (h <= 0) h = b.CalisanHiz;
            return h;
        }

        // =====================================================================
        //  Satın alma özeti
        // =====================================================================

        static void SatinAlma(Baglam b)
        {
            DanismanSonucu d = b.D;
            SatinAlmaOzeti a = d.Alinacak;
            YukseltmeSenaryosu t = d.Senaryolar.FirstOrDefault(s => s.Tavsiye);
            if (t == null || t.Alinacaklar.Count == 0)
            {
                a.Gecerli = false;
                if (b.BoyutBilinmeyen > 0)
                    a.Gerekce = "Bir modülün boyutu BIOS'ta bilinmiyor; toplam kesin olmadığı için güvenli bir satın alma önerisi yapılamıyor. Modül etiketinden doğrula.";
                else if (b.Lehimli.Count > 0 && b.Sokulebilir.Count == 0 && d.BosYuva == 0 && b.LehimliSupheli)
                    a.Gerekce = "Bellek büyük olasılıkla lehimli; cihaz LPCAMM2 modül kullanıyorsa değiştirilebilir. Üretici belgesinden doğrulamadan RAM alma.";
                else if (b.Lehimli.Count > 0 && b.Sokulebilir.Count == 0 && d.BosYuva == 0)
                    a.Gerekce = "Bellek anakarta lehimli; satın alınıp takılabilecek modül yok.";
                else if (!b.AlinabilirTur)
                    a.Gerekce = "Bellek türü ya da form faktörü kesin bilinmediği için güvenli bir satın alma önerisi yapılamıyor; modül etiketinden ya da üretici belgesinden doğrula.";
                else
                    a.Gerekce = "Bu bilgisayar yuva ve kapasite sınırlarına ulaşmış görünüyor; anlamlı bir RAM yükseltmesi yok.";
                return;
            }
            AlinacakModul m = t.Alinacaklar[0];
            a.Gecerli = true;
            a.Tur = m.Tur;
            a.FormFaktor = m.FormFaktor;
            a.HizMTs = m.HizMTs;
            a.VoltajMv = m.VoltajMv;
            a.Ecc = m.Ecc;
            a.Kayitli = m.Kayitli;
            a.ModulGB = m.ModulGB;
            a.Adet = m.Adet;
            bool ekleme = t == b.SenEkle;
            a.OnerilenParcaNo = ekleme && b.Ornek != null ? ParcaNoTemiz(b.Ornek.ParcaNo) : "";
            a.Gerekce = t.Aciklama;

            // Mağaza arama metni (Türk mağazaları MT/s yerine MHz yazar)
            StringBuilder ara = new StringBuilder();
            if (m.Adet > 1) ara.Append((m.Adet * m.ModulGB) + "GB (" + m.Adet + "x" + m.ModulGB + "GB)");
            else ara.Append(m.ModulGB + "GB");
            ara.Append(" " + m.Tur);
            if (m.HizMTs > 0) ara.Append(" " + m.HizMTs + "MHz");
            if (m.Kayitli) ara.Append(" ECC Registered RDIMM");
            else if (m.Ecc) ara.Append(" ECC " + (m.FormFaktor == "SODIMM" ? "SODIMM" : "UDIMM"));
            else ara.Append(" " + m.FormFaktor);
            a.AramaMetni = ara.ToString();

            // Şartname
            int enAz = b.CalisanHiz > 0 ? b.CalisanHiz : b.PlatformHiz;
            a.Ozellikler.Add("Tür: " + m.Tur + " (" + TakilmazMetni(m.Tur) + ")");
            a.Ozellikler.Add("Form: " + m.FormFaktor + PinMetni(b.Tur, m.FormFaktor) + (m.FormFaktor == "SODIMM" ? " (dizüstü tipi, kısa modül)" : " (masaüstü tipi, uzun modül)") +
                             (b.FormTahmini ? " — tahmini, üretici belgesinden doğrula" : ""));
            a.Ozellikler.Add("Kapasite: " + m.Adet + " x " + m.ModulGB + " GB" + (m.Adet > 1 && t == b.SenKit ? " (kit)" : ""));
            if (m.HizMTs > 0)
                a.Ozellikler.Add("Hız: " + m.HizMTs + " MT/s" + (enAz > 0 && enAz < m.HizMTs ? " (en az " + enAz + "; daha hızlısı da takılabilir ama bu bilgisayarda " + enAz + " MT/s'de çalışır)" : "") +
                                 " — mağazada \"" + m.HizMTs + "MHz\" yazar");
            if (m.VoltajMv > 0)
            {
                if (m.Tur == "DDR3L") a.Ozellikler.Add("Voltaj: 1,35 V (DDR3L) — 1,5 V DDR3 ALMA");
                else if (m.Tur == "DDR3") a.Ozellikler.Add("Voltaj: 1,5 V (mevcut modüllerle aynı olmalı)");
                else a.Ozellikler.Add("Voltaj: " + VoltajMetni(m.VoltajMv) + " (standart)");
            }
            if (m.Kayitli) a.Ozellikler.Add("ECC: Var — " + (b.Lrdimm ? "ECC LRDIMM" : "ECC Registered (RDIMM)") + " ŞART; normal RAM çalışmaz");
            else if (m.Ecc) a.Ozellikler.Add("ECC: Var — ECC Unbuffered (UDIMM) şart");
            else a.Ozellikler.Add("ECC: Yok (non-ECC, unbuffered)");
            if (b.Ornek != null && b.Ornek.ParcaBilgisi != null && b.Ornek.ParcaBilgisi.CL > 0)
                a.Ozellikler.Add("Gecikme: CL" + b.Ornek.ParcaBilgisi.CL + " ya da daha düşük (mevcutla aynı ideal)");
            if (ekleme && b.Ornek != null && b.Ornek.Rank > 0)
                a.Ozellikler.Add("Rank: " + b.Ornek.Rank + "R (mevcut modülle aynı ideal)");
            if (a.OnerilenParcaNo != "")
                a.Ozellikler.Add("Mümkünse aynı marka/parça: " + MarkaParca(b.Ornek));
            if (b.LehimliYuvaSupheli && d.BosYuva == 0)
                a.Ozellikler.Insert(0, "ÖNCE ek SODIMM yuvası olduğunu üretici belgesinden doğrula (BIOS boş yuvayı listelemiyor)");
            if (b.Isletim32)
                a.Ozellikler.Insert(0, "ÖNCE 64-bit Windows'a geç (32-bit Windows 4 GB'tan fazlasını kullanamaz)");
        }

        // =====================================================================
        //  Durum ve özet cümle
        // =====================================================================

        static void DurumBelirle(Baglam b)
        {
            DanismanSonucu d = b.D;
            if (b.BoyutBilinmeyen > 0)
                d.Durum = YukseltmeDurumu.Bilinmiyor;
            else if (b.Lehimli.Count > 0 && b.Sokulebilir.Count == 0 && d.BosYuva == 0 && (b.LehimliYuvaSupheli || b.LehimliSupheli))
                d.Durum = YukseltmeDurumu.Bilinmiyor;
            else if (b.Lehimli.Count > 0 && b.Sokulebilir.Count == 0 && d.BosYuva == 0)
                d.Durum = YukseltmeDurumu.Hayir;
            else if (b.Lehimli.Count > 0)
                d.Durum = d.Senaryolar.Count > 0 ? YukseltmeDurumu.Kismen : (b.AlinabilirTur ? YukseltmeDurumu.Hayir : YukseltmeDurumu.Bilinmiyor);
            else if (d.Senaryolar.Count > 0)
                d.Durum = YukseltmeDurumu.Evet;
            else if (b.AlinabilirTur)
                d.Durum = YukseltmeDurumu.Hayir;
            else
                d.Durum = YukseltmeDurumu.Bilinmiyor;
        }

        static void OzetCumle(Baglam b)
        {
            DanismanSonucu d = b.D;
            string yer = Yer(b);
            string tur = b.Tur != "" ? (b.Ddr3L ? "DDR3L" : b.Tur) : "";
            string mevcut = MevcutTanim(b);
            YukseltmeSenaryosu t = d.Senaryolar.FirstOrDefault(s => s.Tavsiye);
            string cumle;

            if (b.BoyutBilinmeyen > 0)
            {
                cumle = yer + " en az " + b.ToplamGB + " GB RAM var ama bir modülün boyutu BIOS'ta bilinmiyor; toplam ve yükseltme önerisi güvenilmez. " +
                        "RAM almadan önce modül etiketlerinden (ya da Görev Yöneticisi > Performans > Bellek ekranından) boyutları doğrulayın.";
            }
            else if (b.LehimliSupheli && t == null && b.Sokulebilir.Count == 0 && d.BosYuva == 0)
            {
                cumle = yer + " " + b.ToplamGB + " GB" + (tur != "" ? " " + tur : "") + " RAM büyük olasılıkla anakarta lehimli; ancak BIOS form faktörünü belirsiz bildiriyor. " +
                        "Cihaz LPCAMM2 modül kullanıyor olabilir: RAM almadan ya da cihaz değişimine karar vermeden önce üretici belgesinden doğrula.";
            }
            else if (d.Durum == YukseltmeDurumu.Hayir && b.Lehimli.Count > 0 && b.Sokulebilir.Count == 0 && d.BosYuva == 0)
            {
                cumle = yer + " " + b.ToplamGB + " GB" + (tur != "" ? " " + tur : "") + " RAM anakarta lehimli ve boş yuva yok; RAM yükseltilemez. " +
                        "Daha fazla RAM gerekiyorsa cihazı değiştirmek gerekir (yeni cihazda en az " + Math.Max(16, SonrakiKademe(b.ToplamGB)) + " GB ve mümkünse yuvalı model seç).";
            }
            else if (t == null)
            {
                if (!b.AlinabilirTur)
                    cumle = yer + " " + mevcut + " var ama bellek türü/formu kesin okunamadı; RAM almadan önce modül etiketinden ya da üretici belgesinden türü (DDR4/DDR5) ve formu (DIMM/SODIMM) doğrulayın.";
                else if (d.AzamiGB > 0 && b.ToplamGB >= d.AzamiGB)
                    cumle = yer + " " + mevcut + " var ve desteklenen azami kapasiteye (" + d.AzamiGB + " GB) ulaşılmış; daha fazla RAM eklenemez.";
                else
                    cumle = yer + " " + mevcut + " var; yuvalar dolu ve modül başına pratik sınıra ulaşılmış, anlamlı bir RAM yükseltmesi yok.";
            }
            else
            {
                AlinacakModul m = t.Alinacaklar.Count > 0 ? t.Alinacaklar[0] : null;
                string modulTanim = m != null ? m.ModulGB + " GB " + TurHiz(m.Tur, m.HizMTs) + " " + m.FormFaktor : "";
                string eylem;
                bool tektenCifte = t.CiftKanal && b.TekKanal && !(b.AyniKanal && t == b.SenKit);
                string sonucMetni = tektenCifte ? "seni " + t.SonucToplamGB + " GB çift kanala çıkarır" : "toplamı " + t.SonucToplamGB + " GB'a çıkarır" + (t.CiftKanal ? " (çift kanal)" : "");

                if (t == b.SenEkle)
                    eylem = "aynı modülden " + (m != null && m.Adet > 1 ? m.Adet + " tane" : "bir tane") + " (" + modulTanim + ") eklemek " + sonucMetni;
                else if (t == b.SenLehimliEs || t == b.SenLehimliBuyuk || t == b.SenBuyukEkle)
                    eylem = "boş yuvaya " + modulTanim + " eklemek " + sonucMetni;
                else if (t == b.SenKucukDegistir)
                    eylem = "küçük modül" + (m != null && m.Adet > 1 ? "leri" : "ü") + " " + (m != null && m.Adet > 1 ? m.Adet + " x " : "") + modulTanim + " ile değiştirmek " + sonucMetni;
                else if (t == b.SenKit)
                    eylem = (m != null && m.Adet > 1 ? m.Adet + " x " + modulTanim + " eş kit alıp mevcut modüllerin yerine takmak" : "mevcut modülü " + modulTanim + " ile değiştirmek") + " " + sonucMetni;
                else
                    eylem = "mevcut modülü " + modulTanim + " ile değiştirmek " + sonucMetni;

                if (b.YuvaSupheli)
                {
                    cumle = yer + " " + mevcut + " var. BIOS 2. yuvayı göstermiyor ama bu sınıf cihazlarda genelde 2 yuva bulunur: üretici belgesinden doğruladıktan sonra " + eylem + ".";
                }
                else if (b.Lehimli.Count > 0 && b.LehimliYuvaSupheli && d.BosYuva == 0)
                {
                    cumle = yer + " " + b.LehimliGB + " GB RAM anakarta lehimli. BIOS ek yuva göstermiyor ama bu modelde boş bir SODIMM yuvası olabilir: " +
                            "üretici belgesinden doğrula; yuva varsa " + eylem + ". Yuva yoksa RAM artırılamaz.";
                }
                else if (b.Lehimli.Count > 0)
                {
                    cumle = yer + " " + b.LehimliGB + " GB RAM anakarta lehimli" +
                            (d.BosYuva > 0 ? ", ayrıca " + d.BosYuva + " boş " + b.Form + " yuvası var: " : ", yuvada " + b.SokulebilirGB + " GB modül var: ") + eylem + ".";
                }
                else if (t == b.SenKit && d.BosYuva == 0)
                {
                    cumle = yer + " " + mevcut + " var ve boş yuva yok: yükseltmek için " + eylem + ".";
                }
                else
                {
                    cumle = yer + " " + mevcut + " var" + (d.BosYuva > 0 ? " ve " + d.BosYuva + " boş yuva bulunuyor" : "") + ": " + eylem + ".";
                }

                if (b.Gerekmiyor)
                    cumle = "Şu anki kullanımına göre RAM yükseltmesi gerekmiyor (" + b.ToplamGB + " GB yeterli görünüyor). İleride gerekirse: " + cumle;
                else if (b.AyniKanal)
                    cumle = "Önce ücretsiz düzeltme: modüller aynı kanalda, birini diğer kanalın yuvasına taşı. " + cumle;
            }

            if (b.Isletim32)
                cumle += " Ama önce 64-bit Windows'a geçmelisin; 32-bit Windows 4 GB'tan fazlasını kullanamaz.";
            d.OzetCumle = cumle;
        }

        static string MevcutTanim(Baglam b)
        {
            DanismanSonucu d = b.D;
            string tur = b.Tur != "" ? (b.Ddr3L ? "DDR3L" : b.Tur) : "";
            string form = b.Form;
            int n = b.Dolu.Count;
            if (n == 1)
                return b.ToplamGB + " GB" + (tur != "" ? " " + tur : "") + (form != "" && b.Lehimli.Count == 0 ? " " + form : "") +
                       " tek modül" + (b.TekKanal ? " (tek kanal)" : "");
            string kanal = b.TekKanal ? "tek kanal" : (b.Flex ? "çift kanal, flex mode" : (b.CiftKanal ? (b.KanalKesin ? "çift kanal" : "muhtemelen çift kanal") : ""));
            if (b.HepsiEs && b.Lehimli.Count == 0)
                return n + " x " + b.OrnekGB + " GB" + (tur != "" ? " " + tur : "") + (form != "" ? " " + form : "") + (kanal != "" ? " (" + kanal + ")" : "");
            string liste = string.Join(" + ", b.Dolu.Select(y => GB(y.BoyutMB).ToString(Inv)).ToArray());
            return b.ToplamGB + " GB" + (tur != "" ? " " + tur : "") + " (" + liste + " GB" + (kanal != "" ? ", " + kanal : "") + ")";
        }

        static string Yer(Baglam b)
        {
            switch (b.Kasa)
            {
                case KasaTipi.Dizustu: return "Bu dizüstünde";
                case KasaTipi.Masaustu: return "Bu masaüstünde";
                case KasaTipi.HepsiBirArada: return "Bu hepsi bir arada bilgisayarda";
                case KasaTipi.MiniPc: return "Bu mini PC'de";
                case KasaTipi.Sunucu: return "Bu sunucuda";
                case KasaTipi.Tablet: return "Bu tablette";
                default: return "Bu bilgisayarda";
            }
        }

        // =====================================================================
        //  İhtiyaç analizi (canlı kullanım)
        // =====================================================================

        static void IhtiyacAnalizi(Baglam b)
        {
            DanismanSonucu d = b.D;
            KullanimBilgisi k = b.K;
            if (k == null || k.ToplamMB <= 0)
            {
                b.Ihtiyac = IhtiyacBilinmiyor;
                d.IhtiyacAnalizi.Add(new Bulgu(Seviye.Bilgi, "Kullanım ölçümü yok",
                    "Bu raporda canlı bellek kullanımı ölçülmedi. Gerçekten RAM'e ihtiyacın olup olmadığını görmek için programı en ağır işini yaparken açık tut ve " +
                    "\"Kullanım (canlı)\" sekmesindeki zirve değerine bak."));
                return;
            }

            long top = k.ToplamMB;
            int yuzde = k.YuzdeKullanim > 0 ? k.YuzdeKullanim : (int)Math.Min(100, Math.Max(0, k.KullanilanMB * 100 / top));
            double taah = k.TaahhutMB > 0 ? k.TaahhutMB / (double)top : 0;
            b.TaahhutOrani = taah;

            d.IhtiyacAnalizi.Add(new Bulgu(yuzde >= 90 ? Seviye.Uyari : (yuzde >= 75 ? Seviye.Uyari : Seviye.Bilgi),
                "Anlık kullanım: %" + yuzde + " (" + Bicim.Boyut(k.KullanilanMB) + " / " + Bicim.Boyut(top) + ")",
                "Boş (kullanılabilir): " + Bicim.Boyut(k.BosMB) + "." +
                (k.OnbellekMB > 0 ? " Önbellek: " + Bicim.Boyut(k.OnbellekMB) + " (gerektiğinde boşaltılır, sorun değildir)." : "")));

            if (k.TaahhutMB > 0)
            {
                int oran = (int)Math.Round(taah * 100);
                if (taah >= 1.0)
                    d.IhtiyacAnalizi.Add(new Bulgu(Seviye.Uyari, "Taahhüt/RAM oranı %" + oran + ": fiziksel RAM'den fazla bellek ayrılmış",
                        "Programların ayırdığı bellek (taahhüt: " + Bicim.Boyut(k.TaahhutMB) + ") fiziksel RAM'den (" + Bicim.Boyut(top) + ") büyük. " +
                        "Ayrılan belleğin hepsi kullanılmıyor olabilir; ancak yük binerse Windows sayfaları diske taşır ve yavaşlama başlar. " +
                        "Sıkıştırma ve sayfa dosyası kullanımı yüksekse daha fazla RAM gerçek fayda sağlar."));
                else if (taah > 0.9)
                    d.IhtiyacAnalizi.Add(new Bulgu(Seviye.Uyari, "Taahhüt/RAM oranı %" + oran + ": RAM sınırda",
                        "Programların ayırdığı bellek (taahhüt: " + Bicim.Boyut(k.TaahhutMB) + ") fiziksel RAM'in (" + Bicim.Boyut(top) + ") %90'ını aşıyor. " +
                        "Biraz daha yük binince Windows belleğin bir kısmını sayfa dosyasına (diske) taşımaya başlar ve yavaşlama hissedilir."));
                else
                    d.IhtiyacAnalizi.Add(new Bulgu(Seviye.Bilgi, "Taahhüt/RAM oranı %" + oran,
                        "Programların ayırdığı toplam bellek " + Bicim.Boyut(k.TaahhutMB) + " (sınır: " + Bicim.Boyut(k.TaahhutSiniriMB) + "). Fiziksel RAM'e sığıyor."));
            }

            if (k.SikistirilmisMB > 0 && k.SikistirilmisMB * 100 / top >= 5)
                d.IhtiyacAnalizi.Add(new Bulgu(k.SikistirilmisMB * 100 / top >= 15 ? Seviye.Uyari : Seviye.Bilgi, "Bellek sıkıştırma: " + Bicim.Boyut(k.SikistirilmisMB),
                    "Windows yer açmak için belleğin bir kısmını sıkıştırıyor. Bu RAM baskısının işaretidir; sıkıştırma işlemci zamanı harcar."));

            if (k.SayfaDosyasiKullanilanMB > 1024 && (k.SayfaDosyasiMB <= 0 || k.SayfaDosyasiKullanilanMB * 4 >= k.SayfaDosyasiMB))
                d.IhtiyacAnalizi.Add(new Bulgu(Seviye.Bilgi, "Sayfa dosyası kullanımı: " + Bicim.Boyut(k.SayfaDosyasiKullanilanMB),
                    "Bellek sayfaları diske yazılmış. Disk RAM'den çok daha yavaş olduğu için sık kullanılıyorsa takılmalar olur."));

            // En çok kullananlar
            List<SurecBellek> surecler = (k.EnCokKullananlar ?? new List<SurecBellek>()).Where(s => s != null).ToList();
            if (surecler.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (SurecBellek s in surecler.OrderByDescending(x => SurecMB(x)).Take(5))
                    sb.Append("• " + s.Ad + (s.Adet > 1 ? " (" + s.Adet + " süreç)" : "") + ": " + Bicim.Boyut(SurecMB(s)) + "\n");
                long tarayici = 0, teams = 0, vm = 0;
                foreach (SurecBellek s in surecler)
                {
                    string ad = SurecAdi(s.Ad);
                    if (ad == "chrome" || ad == "msedge" || ad == "firefox" || ad == "opera" || ad == "brave" || ad == "vivaldi" || ad == "msedgewebview2") tarayici += SurecMB(s);
                    else if (ad == "ms-teams" || ad == "teams" || ad == "msteams") teams += SurecMB(s);
                    else if (ad.StartsWith("vmmem", StringComparison.Ordinal) || ad == "vmwp" || ad == "virtualboxvm" || ad == "vmware-vmx") vm += SurecMB(s);
                }
                if (tarayici >= 1024) sb.Append("Tarayıcılar toplam " + Bicim.Boyut(tarayici) + " kullanıyor: açık sekme sayısı en büyük etken; uyku sekmeleri (Edge/Chrome) yardımcı olur.\n");
                if (teams >= 512) sb.Append("Teams " + Bicim.Boyut(teams) + " kullanıyor; toplantı sırasında daha da artar.\n");
                if (vm >= 1024) sb.Append("Sanal makine/WSL (" + Bicim.Boyut(vm) + ") ayrılmış bellek tutuyor; WSL için .wslconfig ile sınır konabilir.\n");
                d.IhtiyacAnalizi.Add(new Bulgu(Seviye.Bilgi, "En çok bellek kullananlar", sb.ToString().TrimEnd('\n')));
            }

            int sonuc;
            // Taahhüt > RAM tek başına sayfalama kanıtı değildir: doğrudan bir baskı göstergesi de aranır.
            bool baski = yuzde >= 85 || (k.SikistirilmisMB > 0 && k.SikistirilmisMB * 100 / top >= 10) || k.SayfaDosyasiKullanilanMB > 1024;
            if (yuzde >= 90 || (taah >= 1.0 && baski) || (taah > 0.9 && yuzde >= 80)) sonuc = IhtiyacYetersiz;
            else if (yuzde >= 75 || taah > 0.9) sonuc = IhtiyacSinirda;
            else sonuc = IhtiyacYeterli;
            b.Ihtiyac = sonuc;

            string anlik = "\nBu anlık bir ölçümdür. Gerçek ihtiyacı görmek için en ağır işini (çok sekme, Teams toplantısı, Excel/AutoCAD vb.) yaparken " +
                           "\"Kullanım (canlı)\" sekmesini açık bırak ve oturum boyunca görülen zirve değerine bak.";
            if (sonuc == IhtiyacYetersiz)
                d.IhtiyacAnalizi.Add(new Bulgu(Seviye.Uyari, "Sonuç: Şu an RAM yetersiz",
                    "Kullanım %" + yuzde + (k.TaahhutMB > 0 ? ", taahhüt/RAM oranı %" + (int)Math.Round(taah * 100) : "") + ". Daha fazla RAM bu bilgisayarı belirgin hızlandırır." + anlik));
            else if (sonuc == IhtiyacSinirda)
                d.IhtiyacAnalizi.Add(new Bulgu(Seviye.Uyari, "Sonuç: Şu an sınırda",
                    "Kullanım %" + yuzde + ". Şimdilik idare ediyor ama yük artınca yavaşlama başlar; yükseltme mantıklı." + anlik));
            else
                d.IhtiyacAnalizi.Add(new Bulgu(Seviye.Iyi, "Sonuç: Şu an yeterli",
                    "Kullanım %" + yuzde + (k.TaahhutMB > 0 ? ", taahhüt/RAM oranı %" + (int)Math.Round(taah * 100) : "") + ". Şu anki iş yükü için RAM yetiyor." + anlik));
        }

        static long SurecMB(SurecBellek s)
        {
            // Özel bellek (private bytes) esas alınır: çalışma kümesi paylaşılan sayfaları her süreçte
            // yeniden saydığı için çok süreçli uygulamalarda (svchost, chrome) toplamı şişirir.
            return s.OzelMB > 0 ? s.OzelMB : s.CalismaKumesiMB;
        }

        static string SurecAdi(string ad)
        {
            string a = (ad ?? "").Trim().ToLowerInvariant();
            if (a.EndsWith(".exe", StringComparison.Ordinal)) a = a.Substring(0, a.Length - 4);
            return a;
        }

        // =====================================================================
        //  Kontrol listesi, takma adımları, bağlantılar
        // =====================================================================

        static void KontrolListesi(Baglam b)
        {
            DanismanSonucu d = b.D;
            SatinAlmaOzeti a = d.Alinacak;
            List<string> l = d.KontrolListesi;
            string cihaz = CihazAdi(b);

            if (b.Isletim32)
                l.Add("Önce 64-bit Windows'a geç: 32-bit Windows 4 GB'tan fazla RAM kullanamaz.");

            if (a.Gecerli)
            {
                l.Add("Tür: " + a.Tur + " — " + TakilmazMetni(a.Tur) + ".");
                l.Add("Form: " + a.FormFaktor + PinMetni(b.Tur, a.FormFaktor) + (a.FormFaktor == "SODIMM" ? " — dizüstü tipi kısa modül; masaüstü (DIMM) modülü takılmaz." : " — masaüstü tipi uzun modül; dizüstü (SODIMM) modülü takılmaz."));
                if (a.HizMTs > 0)
                {
                    int enAz = b.CalisanHiz > 0 ? b.CalisanHiz : b.PlatformHiz;
                    l.Add("Hız: " + a.HizMTs + " MT/s (mağazada \"" + a.HizMTs + "MHz\")" + (enAz > 0 && enAz < a.HizMTs ? "; daha hızlısı da çalışır ama " + enAz + " MT/s'ye iner, fazlasına para verme." : "."));
                }
                if (a.Tur == "DDR3L") l.Add("Voltaj: mutlaka 1,35 V (DDR3L). Etikette \"1.5V\" yazıyorsa ALMA.");
                else if (a.VoltajMv > 0) l.Add("Voltaj: " + VoltajMetni(a.VoltajMv) + " standart modül.");
                if (a.Kayitli) l.Add("ECC Registered (RDIMM) olmalı; normal masaüstü RAM bu sistemde çalışmaz.");
                else if (a.Ecc) l.Add("ECC Unbuffered olmalı.");
                else l.Add("ECC'siz (non-ECC), unbuffered olmalı; sunucu RAM'i (ECC Registered) alma.");
                l.Add("Adet: " + a.Adet + (a.Adet > 1 ? " — aynı paketteki kit halinde alırsan eşleşme garantili." : "."));
                if (a.OnerilenParcaNo != "")
                    l.Add("En garantilisi aynı parça numarası: " + MarkaParca(b.Ornek) + ".");
            }

            if (b.YuvaSupheli || !d.YuvaBilgisiGuvenilir)
                l.Add("Yuva sayısını DOĞRULA: BIOS bu cihazda boş yuvayı göstermiyor olabilir. Üretici belgesine bak" + (cihaz != "" ? " (" + cihaz + ")" : "") + (b.Mobil ? " ya da alt kapağı açıp gözünle gör." : "."));
            else
                l.Add("Üretici belgesinden yuva sayısını ve azami kapasiteyi doğrula" + (cihaz != "" ? " (" + cihaz + ")" : "") + ".");
            if (d.AzamiGB > 0)
                l.Add("Hedef toplam, desteklenen azamiyi (" + d.AzamiGB + " GB, kaynak: " + d.AzamiGBKaynagi + ") aşmamalı" +
                      (d.PratikAzamiGB > 0 ? "; pratikte ulaşılabilecek en yüksek değer " + d.PratikAzamiGB + " GB (" + d.PratikAzamiMetni + ")." : "."));
            if (b.Mobil && b.Dolu.Count > 0)
                l.Add("Alt kapağı açınca boş yuvayı ve modülün üzerindeki metal kalkan/bant olup olmadığını gör; bazı modellerde yuva klavyenin altındadır.");
            if (b.Tur == "DDR5")
                l.Add("BIOS'u güncelle: yeni modül boyutları (24/48 GB) ve uyumluluk düzeltmeleri BIOS güncellemesiyle gelir.");
            l.Add("Garanti ve iade: satıcının iade koşulunu ve modülün garanti süresini (çoğu markada uzun/ömür boyu) kontrol et. Cihaz garantideyse kapak açmanın garantiye etkisini üretici politikasından öğren.");
            l.Add("Şirket cihazıysa demirbaş/varlık kaydına yeni RAM'i işle.");
        }

        static void TakmaAdimlari(Baglam b)
        {
            DanismanSonucu d = b.D;
            List<string> t = d.TakmaAdimlari;
            if (b.Lehimli.Count > 0 && b.Sokulebilir.Count == 0 && d.BosYuva == 0 && b.LehimliSupheli)
            {
                t.Add("Bellek büyük olasılıkla lehimli. Cihaz LPCAMM2 modül kullanıyorsa değiştirme işlemi için üreticinin servis kılavuzuna uy (vidalı modül, sıkıştırma plakası).");
                return;
            }
            if (b.Lehimli.Count > 0 && b.Sokulebilir.Count == 0 && d.BosYuva == 0 && !b.LehimliYuvaSupheli)
            {
                t.Add("Bu cihazda RAM takılabilecek yuva yok (bellek lehimli); takma işlemi yapılamaz.");
                return;
            }
            string uretici = (b.S.Uretici ?? "").ToUpperInvariant();
            bool ddr5 = b.Tur == "DDR5";

            if (b.Sunucu)
            {
                t.Add("Sunucuyu düzgünce kapat, güç kablolarını çıkar.");
                t.Add("Üreticinin servis kılavuzundaki bellek yerleşim (population) kurallarına uy: her kanala eşit, aynı rank ve aynı tip (RDIMM/LRDIMM).");
                t.Add("Antistatik bileklik kullan; modülü çentiği hizalayarak dik bastır, iki mandal kendiliğinden kapanana kadar.");
                t.Add("İlk açılışta bellek eğitimi/POST uzun sürebilir; BMC/iLO/iDRAC kayıtlarında bellek hatası olmadığını kontrol et.");
                t.Add("Windows'ta bu programla yeni miktarı doğrula; mümkünse bakım penceresinde bellek testi çalıştır.");
                return;
            }

            bool pilsizKasa = b.Kasa == KasaTipi.MiniPc || b.Kasa == KasaTipi.HepsiBirArada;
            if (b.Mobil && pilsizKasa)
            {
                // Mini PC / AIO: ana pil yoktur (anakarttaki tek pil CMOS düğme pilidir; sökülmez).
                t.Add("Hepsi bir arada ve mini PC'lerde bellek yuvasına erişim modele göre çok değişir; önce üreticinin servis kılavuzundaki söküm adımlarına bak.");
                t.Add("Açık işleri kaydet ve bilgisayarı tamamen kapat (Shift basılıyken Kapat — hızlı başlatma uykusu değil).");
                t.Add("Güç adaptörünü/kablosunu çıkar ve kalan enerjiyi boşaltmak için güç tuşuna 10–15 saniye basılı tut. (Anakarttaki düğme pile dokunma.)");
                t.Add("Servis kılavuzundaki adımlarla kasayı/arka kapağı aç.");
                t.Add("Statik elektriğe dikkat: topraklı bir metale dokun ya da antistatik bileklik kullan; modülü yalnızca kenarlarından tut.");
                t.Add("Modülün üzerinde metal kalkan ya da bant varsa nazikçe kaldır.");
                t.Add("SODIMM'i çentiği yuvadaki çıkıntıyla hizalayıp yaklaşık 30° açıyla sonuna kadar it, sonra yan klipsler \"tık\" diye oturana kadar aşağı bastır.");
                if (b.SenKit != null && d.Senaryolar.Count > 0 && d.Senaryolar[0] == b.SenKit)
                    t.Add("Eski modülü çıkarmak için iki yandaki klipsi dışa doğru it; modül açılı olarak kalkar, çekip çıkar.");
                t.Add("Kapağı kapat, güç kablosunu tak ve aç.");
            }
            else if (b.Mobil)
            {
                string modelAdi = (b.S.ModelAdi ?? "").ToUpperInvariant();
                t.Add("Açık işleri kaydet ve bilgisayarı tamamen kapat (Shift basılıyken Kapat — hızlı başlatma uykusu değil). Şarj aletini çıkar.");
                if (uretici.Contains("LENOVO") && modelAdi.Contains("THINKPAD"))
                    t.Add("Dahili pili devre dışı bırak: açılışta F1 ile BIOS'a gir > Config > Power > \"Disable Built-in Battery\" (cihaz kapanır; şarj aleti takılınca pil yeniden etkinleşir).");
                else if (uretici.Contains("LENOVO") && (modelAdi.Contains("IDEAPAD") || modelAdi.Contains("LOQ") || modelAdi.Contains("LEGION") || modelAdi.Contains("YOGA")))
                    t.Add("Dahili pili devre dışı bırak: açılışta F2 ile BIOS'a gir > Configuration > \"Disable Built-in Battery\" (cihaz kapanır; şarj aleti takılınca pil yeniden etkinleşir).");
                else if (uretici.Contains("LENOVO"))
                    t.Add("Dahili pili devre dışı bırak: BIOS'ta (ThinkPad: F1 > Config > Power; IdeaPad/LOQ/Legion: F2 > Configuration) \"Disable Built-in Battery\" seçeneğini kullan (cihaz kapanır; şarj aleti takılınca pil yeniden etkinleşir).");
                else if (uretici.Contains("DELL"))
                    t.Add("Dahili pili devre dışı bırak: Dell'de alt kapağı açınca önce pil kablosunu anakarttan çıkar (bazı modellerde BIOS'ta \"Ship mode\" var).");
                else if (uretici.Contains("HP") || uretici.Contains("HEWLETT"))
                    t.Add("Dahili pili devre dışı bırak: HP'de alt kapağı açınca önce pil konektörünü çıkar (servis kılavuzuna bak).");
                else
                    t.Add("Dahili pili devre dışı bırak: BIOS'ta \"Disable Built-in Battery\" / \"Ship mode\" seçeneği varsa kullan; yoksa kapağı açınca ilk iş pil kablosunu çıkar.");
                t.Add("Alt kapak vidalarını sök (bazıları tutucu vidadır, tamamen çıkmaz). Kapağı plastik bir kartla kenardan, klipsleri kırmadan aç.");
                t.Add("Statik elektriğe dikkat: topraklı bir metale dokun ya da antistatik bileklik kullan; modülü yalnızca kenarlarından tut.");
                t.Add("Modülün üzerinde metal kalkan ya da bant varsa nazikçe kaldır.");
                t.Add("SODIMM'i çentiği yuvadaki çıkıntıyla hizalayıp yaklaşık 30° açıyla sonuna kadar it, sonra yan klipsler \"tık\" diye oturana kadar aşağı bastır.");
                if (b.SenKit != null && d.Senaryolar.Count > 0 && d.Senaryolar[0] == b.SenKit)
                    t.Add("Eski modülü çıkarmak için iki yandaki klipsi dışa doğru it; modül açılı olarak kalkar, çekip çıkar.");
                t.Add("Kapağı kapat, şarj aletini tak ve aç" + (uretici.Contains("LENOVO") ? " (pil devre dışıysa cihaz yalnızca şarj aletiyle açılır)" : "") + ".");
            }
            else
            {
                t.Add("Bilgisayarı kapat, güç kablosunu çek ve kalan enerjiyi boşaltmak için güç tuşuna 10–15 saniye basılı tut.");
                t.Add("Statik elektriğe dikkat: kasanın boyasız metal kısmına dokun ya da antistatik bileklik kullan; modülü kenarlarından tut.");
                t.Add("Yan kapağı aç. Yuva önceliği: 2 modül için genelde A2 + B2 (işlemciden 2. ve 4. yuva); anakart kılavuzundaki tabloya bak.");
                t.Add("Yuvanın mandallarını aç, modülün çentiğini yuvadaki çıkıntıyla hizala (ters takılmaz).");
                t.Add("Modülü iki ucundan eşit kuvvetle dik bastır; mandallar \"tık\" sesiyle kendiliğinden kapanmalı. Yarım oturan modül en sık açılmama nedenidir.");
                t.Add("Kapağı kapat, güç kablosunu tak ve aç.");
                t.Add("XMP/EXPO: şirket bilgisayarlarında açmayın, bellek JEDEC hızında çalışsın (kurumsal BIOS'larda bu seçenek genelde yoktur). Yalnızca XMP/EXPO profilli kitlerde ve kişisel/oyun masaüstlerinde BIOS'tan isteğe bağlı açılabilir.");
            }
            if (ddr5)
                t.Add("DDR5'te ilk açılış 1–3 dakika siyah ekranda bekleyebilir (bellek eğitimi). Bu normaldir; kapatma.");
            else
                t.Add("İlk açılış biraz uzun sürebilir; görüntü gelmezse kapatıp modülün tam oturduğunu kontrol et.");
            t.Add("Windows açılınca bu programla (ya da Görev Yöneticisi > Performans > Bellek) yeni miktarı, yuvaları ve hızı doğrula.");
            t.Add("Bellek testi: Başlat > \"mdsched\" (Windows Bellek Tanılama) > \"Şimdi yeniden başlat ve sorunları denetle\".");
        }

        static void Baglantilar(Baglam b)
        {
            DanismanSonucu d = b.D;
            string uretici = Anlamli(b.S.Uretici);
            string modelAdi = Anlamli(b.S.ModelAdi);
            string model = Anlamli(b.S.Model);
            string seri = Anlamli(b.S.SeriNo);
            string anaUretici = Anlamli(b.S.AnakartUretici);
            string anaModel = Anlamli(b.S.AnakartModel);
            string u = uretici.ToUpperInvariant();

            List<string> parcalar = new List<string>();
            if (uretici != "") parcalar.Add(uretici);
            if (modelAdi != "") parcalar.Add(modelAdi);
            if (model != "" && !string.Equals(model, modelAdi, StringComparison.OrdinalIgnoreCase) &&
                (modelAdi == "" || modelAdi.IndexOf(model, StringComparison.OrdinalIgnoreCase) < 0)) parcalar.Add(model);
            if (parcalar.Count > (uretici != "" ? 1 : 0))
            {
                string q = string.Join(" ", parcalar.ToArray()) + " memory specifications maximum";
                d.Baglantilar.Add(new Baglanti("Üretici teknik özellikleri: bellek ve azami kapasite (Google)", Google(q)));
            }

            string ad = modelAdi != "" ? modelAdi : model;
            if (u.Contains("LENOVO") && ad != "")
                d.Baglantilar.Add(new Baglanti("Lenovo PSREF: " + ad + " teknik özellikleri", Google("site:psref.lenovo.com " + ad)));
            if (u.Contains("DELL") && seri != "" && Regex.IsMatch(seri, "^[A-Za-z0-9]{5,10}$"))
                d.Baglantilar.Add(new Baglanti("Dell destek (Service Tag " + seri + ")",
                    "https://www.dell.com/support/home/tr-tr/product-support/servicetag/" + Uri.EscapeDataString(seri) + "/overview"));
            if ((u.Contains("HP") || u.Contains("HEWLETT")) && ad != "")
                d.Baglantilar.Add(new Baglanti("HP QuickSpecs: " + ad + " bellek", Google(ad + " QuickSpecs memory")));

            bool masaustu = b.Kasa == KasaTipi.Masaustu || (!b.Mobil && b.Kasa != KasaTipi.Sunucu);
            if ((masaustu || uretici == "") && anaModel != "")
                d.Baglantilar.Add(new Baglanti("Anakart bellek uyumluluk listesi (QVL): " + (anaUretici != "" ? anaUretici + " " : "") + anaModel,
                    Google((anaUretici != "" ? anaUretici + " " : "") + anaModel + " QVL memory")));

            if (d.Alinacak != null && d.Alinacak.Gecerli && d.Alinacak.AramaMetni != "")
                d.Baglantilar.Add(new Baglanti("Mağazalarda ara: " + d.Alinacak.AramaMetni, Google(d.Alinacak.AramaMetni)));
        }

        static string Google(string q)
        {
            return "https://www.google.com/search?q=" + Uri.EscapeDataString(q);
        }

        static string CihazAdi(Baglam b)
        {
            string u = Anlamli(b.S.Uretici), m = Anlamli(b.S.ModelAdi);
            if (m == "") m = Anlamli(b.S.Model);
            return (u + " " + m).Trim();
        }

        // =====================================================================
        //  Küçük yardımcılar
        // =====================================================================

        static int GB(long mb)
        {
            if (mb <= 0) return 0;
            long g = (mb + 512) / 1024;
            if (g < 1) g = 1;
            return (int)Math.Min(int.MaxValue, g);
        }

        static int SonrakiKademe(int gb)
        {
            int[] kademe = { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096 };
            foreach (int k in kademe) if (k > gb) return k;
            return gb * 2;
        }

        static int KademeyeYuvarla(int gb)
        {
            int[] kademe = { 1, 2, 4, 8, 12, 16, 24, 32, 48, 64 };
            foreach (int k in kademe) if (k >= gb) return k;
            return gb;
        }

        static string EnSik(IEnumerable<string> liste)
        {
            List<string> l = liste.ToList();
            if (l.Count == 0) return "";
            return l.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
        }

        static bool KayitliMi(BellekYuvasi y)
        {
            return y.TurDetay != null && y.Kayitli;
        }

        static bool Ddr3LVoltaji(int mv)
        {
            return mv >= 1300 && mv <= 1400;
        }

        static bool LehimliMi(BellekYuvasi y)
        {
            if (y.Lehimli) return true;
            string f = FormAdi(y);
            if (f == "LEHIMLI") return true;
            if (TurAdi(y).StartsWith("LP", StringComparison.Ordinal) && f != "CAMM") return true;
            return false;
        }

        internal static string TurAdi(BellekYuvasi y)
        {
            string t = (y.Tur ?? "").Trim().ToUpperInvariant();
            if (t == "UNKNOWN" || t == "OTHER" || t == "BILINMIYOR" || t == "—" || t == "-") t = "";
            if (t == "") t = TurKodundan(y.TurKodu);
            if (t == "" && y.ParcaBilgisi != null && y.ParcaBilgisi.Bulundu && !string.IsNullOrEmpty(y.ParcaBilgisi.Tur))
                t = y.ParcaBilgisi.Tur.Trim().ToUpperInvariant();
            t = t.Replace(" ", "");
            if (t == "LPDDR4X") t = "LPDDR4";
            if (t == "LPDDR5X") t = "LPDDR5";
            return t;
        }

        static string TurKodundan(int kod)
        {
            switch (kod)
            {
                case 0x12: return "DDR";
                case 0x13: return "DDR2";
                case 0x18: return "DDR3";
                case 0x1A: return "DDR4";
                case 0x1B: return "LPDDR";
                case 0x1C: return "LPDDR2";
                case 0x1D: return "LPDDR3";
                case 0x1E: return "LPDDR4";
                case 0x22: return "DDR5";
                case 0x23: return "LPDDR5";
                default: return "";
            }
        }

        internal static string FormAdi(BellekYuvasi y)
        {
            string f = (y.FormFaktor ?? "").Trim().ToUpperInvariant().Replace("-", "").Replace(" ", "").Replace("_", "");
            if (f == "SODIMM" || f == "SODIMMS") return "SODIMM";
            if (f == "DIMM" || f == "UDIMM" || f == "RDIMM" || f == "LRDIMM" || f == "FBDIMM") return "DIMM";
            if (f.Contains("CAMM")) return "CAMM";
            if (f == "ROWOFCHIPS" || f == "CHIP" || f == "DIE" || f.Contains("LEHIM") || f.Contains("SOLDER") || f.Contains("ONBOARD")) return "LEHIMLI";
            switch (y.FormFaktorKodu)
            {
                case 0x09: return "DIMM";
                case 0x0F: return "DIMM";
                case 0x0D: return "SODIMM";
                case 0x05:
                case 0x0B:
                case 0x10: return "LEHIMLI";
                case 0x11: return "CAMM";
            }
            if (y.ParcaBilgisi != null && y.ParcaBilgisi.Bulundu && !string.IsNullOrEmpty(y.ParcaBilgisi.FormFaktor))
            {
                string p = y.ParcaBilgisi.FormFaktor.Trim().ToUpperInvariant().Replace("-", "");
                if (p == "SODIMM") return "SODIMM";
                if (p == "DIMM") return "DIMM";
            }
            return "";
        }

        static int EtiketHizi(BellekYuvasi y)
        {
            int h = y.HizMTs;
            if (y.ParcaBilgisi != null && y.ParcaBilgisi.Bulundu && y.ParcaBilgisi.HizMTs > h && y.ParcaBilgisi.HizMTs < 20000) h = y.ParcaBilgisi.HizMTs;
            return h;
        }

        static int PlatformHizi(PlatformBilgisi p, string tur)
        {
            return Platform.AzamiHiz(p, tur);   // eşdeğer türler (DDR3L, LPDDRnX) Platform'da
        }

        static int StandartVoltaj(string tur)
        {
            switch (tur)
            {
                case "DDR2": return 1800;
                case "DDR3": return 1500;
                case "DDR3L": return 1350;
                case "DDR4": return 1200;
                case "DDR5": return 1100;
                default: return 0;
            }
        }

        static string VoltajMetni(int mv)
        {
            return Bicim.Voltaj(mv);
        }

        static string TurHiz(string tur, int hiz)
        {
            if (string.IsNullOrEmpty(tur)) return hiz > 0 ? hiz + " MT/s" : "";
            return hiz > 0 ? tur + "-" + hiz : tur;
        }

        static string TakilmazMetni(string tur)
        {
            switch (tur)
            {
                case "DDR5": return "DDR4 TAKILMAZ";
                case "DDR4": return "DDR3 ve DDR5 TAKILMAZ";
                case "DDR3L": return "DDR4 TAKILMAZ; 1,5 V DDR3 alma";
                case "DDR3": return "DDR2 ve DDR4 TAKILMAZ";
                case "DDR2": return "DDR ve DDR3 TAKILMAZ";
                default: return "başka nesil TAKILMAZ";
            }
        }

        static string PinMetni(string tur, string form)
        {
            string t = tur == "DDR3L" ? "DDR3" : tur;
            int pin = 0;
            if (form == "SODIMM")
            {
                if (t == "DDR2") pin = 200; else if (t == "DDR3") pin = 204; else if (t == "DDR4") pin = 260; else if (t == "DDR5") pin = 262;
            }
            else if (form == "DIMM")
            {
                if (t == "DDR2" || t == "DDR3") pin = 240; else if (t == "DDR4" || t == "DDR5") pin = 288;
            }
            return pin > 0 ? " " + pin + " pin" : "";
        }

        static string ParcaNoTemiz(string p)
        {
            string s = (p ?? "").Trim();
            string u = s.ToUpperInvariant();
            if (u == "" || u == "UNKNOWN" || u == "UNDEFINED" || u == "NOT SPECIFIED" || u == "NONE" || u == "N/A" ||
                u.StartsWith("MODULEPARTNUMBER", StringComparison.Ordinal) || u.StartsWith("PARTNUM", StringComparison.Ordinal) ||
                u.Trim('0', ' ', 'F') == "") return "";
            return s;
        }

        static string MarkaParca(BellekYuvasi y)
        {
            if (y == null) return "";
            string u = (y.Uretici ?? "").Trim();
            if (u == "") u = (y.UreticiHam ?? "").Trim();
            string p = ParcaNoTemiz(y.ParcaNo);
            return (u + " " + p).Trim();
        }

        static string YuvaAdi(BellekYuvasi y)
        {
            string a = (y.Yuva ?? "").Trim();
            if (a == "") a = (y.Banka ?? "").Trim();
            return a;
        }

        static string Anlamli(string s)
        {
            string t = (s ?? "").Trim();
            string u = t.ToUpperInvariant();
            if (u == "" || u.Contains("TO BE FILLED") || u.Contains("SYSTEM PRODUCT NAME") || u.Contains("SYSTEM MANUFACTURER") ||
                u.Contains("DEFAULT STRING") || u == "O.E.M." || u == "OEM" || u == "NOT APPLICABLE" || u == "NONE" || u == "N/A" ||
                u == "UNKNOWN" || u == "SYSTEM SERIAL NUMBER" || u == "INVALID" || u == "123456789" || u == "0" || u == "BASE BOARD PRODUCT NAME" ||
                u == "SYSTEM VERSION" || u == "TYPE1PRODUCTCONFIGID" || u == "CHASSIS SERIAL NUMBER")
                return "";
            return t;
        }
    }
}
