// RAM Danışmanı — ortak veri modeli.
// Tüm modüller bu tiplerle konuşur. C# 5 (Windows'taki yerleşik csc.exe) ile
// derlenir: $"" , ?. , => üye, nameof, out var, otomatik özellik başlatıcı YOK.
using System;
using System.Collections.Generic;
using System.Text;

namespace RamDanismani
{
    // ---- Ham donanım bilgisi -------------------------------------------------

    /// <summary>SMBIOS tip 17 (Memory Device) — dolu ya da boş tek bir yuva.</summary>
    public class BellekYuvasi
    {
        public int Handle;
        public int DiziHandle;              // bağlı olduğu tip 16 dizisinin handle'ı (0xFFFE = yok)
        public bool Dolu;                   // boyut 0 ise boş yuva
        public long BoyutMB;                // 0 = boş
        public string Yuva = "";            // Device Locator  (ör. "Controller0-ChannelA-DIMM0", "DIMM_A2")
        public string Banka = "";           // Bank Locator    (ör. "BANK 0", "P0 CHANNEL A")
        public int FormFaktorKodu;          // SMBIOS ham kod
        public string FormFaktor = "";      // "DIMM", "SODIMM", "Row of chips", "CAMM" ...
        public int TurKodu;                 // SMBIOS ham kod (0x1A=DDR4, 0x22=DDR5 ...)
        public string Tur = "";             // "DDR4", "DDR5", "LPDDR5" ...
        public int TurDetayBayraklari;      // ham bit alanı
        public List<string> TurDetay = new List<string>(); // "Synchronous", "Unbuffered (Unregistered)", "Registered (Buffered)" ...
        public int HizMTs;                  // modülün azami (etiket) hızı, MT/s
        public int YapilandirilmisHizMTs;   // şu an çalıştığı hız, MT/s
        public string Uretici = "";         // normalleştirilmiş marka adı ("Samsung", "SK hynix" ...)
        public string UreticiHam = "";      // SMBIOS/WMI'daki ham metin ("80CE", "Micron Technology" ...)
        public string ParcaNo = "";
        public string SeriNo = "";
        public string VarlikEtiketi = "";
        public int Rank;                    // 0 = bilinmiyor
        public int VeriGenisligi;           // bit, 0 = bilinmiyor
        public int ToplamGenislik;          // bit, 0 = bilinmiyor; > veri genişliği ise ECC bitleri var
        public int MinVoltajMv;             // 0 = bilinmiyor
        public int MaxVoltajMv;
        public int YapilandirilmisVoltajMv;
        public int TeknolojiKodu;           // SMBIOS 3.2 Memory Technology (3 = DRAM)
        public string Teknoloji = "";
        public int ModulUreticiId;          // SMBIOS 3.2 JEDEC kimliği (0 = yok)
        public int ModulUrunId;
        public bool Lehimli;                // anakarta lehimli (sökülemez) olduğu sonucu
        public string LehimliNedeni = "";   // neden lehimli sayıldığı (insan okuyacak)
        public bool LehimliSupheli;         // LPDDR ama form faktörü belirsiz: LPCAMM2 olabilir
        public string Kaynak = "";          // "SMBIOS" ya da "WMI"
        public ParcaCozumu ParcaBilgisi;    // parça numarası çözümlemesi (null olabilir)

        public bool Ecc
        {
            get { return VeriGenisligi > 0 && ToplamGenislik > VeriGenisligi; }
        }

        public bool Kayitli // Registered / Buffered (sunucu belleği)
        {
            get
            {
                foreach (string d in TurDetay)
                    if (d.IndexOf("Registered", StringComparison.OrdinalIgnoreCase) >= 0 && d.IndexOf("Unregistered", StringComparison.OrdinalIgnoreCase) < 0)
                        return true;
                return false;
            }
        }
    }

    /// <summary>SMBIOS tip 16 (Physical Memory Array).</summary>
    public class BellekDizisi
    {
        public int Handle;
        public string Konum = "";           // "System board or motherboard" ...
        public string Kullanim = "";        // "System memory", "Video memory" ...
        public int KullanimKodu;            // 3 = System memory
        public string EccTuru = "";         // "None", "Single-bit ECC", "Multi-bit ECC" ...
        public int EccKodu;
        public long MaxKapasiteMB;          // BIOS'un bildirdiği azami kapasite (0 = bilinmiyor). GÜVENİLMEZ olabilir.
        public int YuvaSayisi;              // BIOS'un bildirdiği yuva sayısı (0 = bilinmiyor). GÜVENİLMEZ olabilir.
    }

    public enum KasaTipi { Bilinmiyor, Masaustu, Dizustu, HepsiBirArada, MiniPc, Sunucu, Tablet, SanalMakine }

    public class SistemBilgisi
    {
        public string Uretici = "";         // "LENOVO"
        public string Model = "";           // "83LK"
        public string ModelAdi = "";        // "LOQ 15IAX9E" (Lenovo'da Version alanı; yoksa Model)
        public string SeriNo = "";
        public string AnakartUretici = "";
        public string AnakartModel = "";
        public string BiosSurum = "";
        public string BiosTarih = "";
        public string SmbiosSurum = "";     // "3.4"
        public int KasaTuruKodu;            // SMBIOS tip 3 ham kodu
        public KasaTipi Kasa = KasaTipi.Bilinmiyor;
        public string IslemciAdi = "";      // "12th Gen Intel(R) Core(TM) i5-12450HX"
        public int CekirdekSayisi;
        public int IsParcacigiSayisi;
        public string IsletimSistemi = "";  // "Microsoft Windows 11 Pro 10.0.26200"
        public bool Isletim64Bit;
        public long TakiliRamMB;            // GetPhysicallyInstalledSystemMemory (donanımda takılı)
        public long KullanilabilirRamMB;    // Windows'un gördüğü (donanıma ayrılan düşülmüş)
        public bool SanalMakine;
        public string SanalMakineNotu = "";
    }

    public class SurecBellek
    {
        public string Ad = "";
        public long OzelMB;                 // Private bytes
        public long CalismaKumesiMB;        // Working set
        public int Adet = 1;                // aynı adlı süreçler birleştirilirse kaç tane
    }

    public class KullanimBilgisi
    {
        public long ToplamMB;
        public long KullanilanMB;
        public long BosMB;                  // kullanılabilir (Available)
        public int YuzdeKullanim;
        public long TaahhutMB;              // Commit charge
        public long TaahhutSiniriMB;        // Commit limit (RAM + sayfa dosyası)
        public long SayfaDosyasiMB;         // toplam sayfa dosyası boyutu
        public long SayfaDosyasiKullanilanMB;
        public long OnbellekMB;             // System cache / standby
        public long SikistirilmisMB;        // bellek sıkıştırma (biliniyorsa, 0 = bilinmiyor)
        public List<SurecBellek> EnCokKullananlar = new List<SurecBellek>();
        public DateTime Zaman;
    }

    /// <summary>İşlemcinin bellek denetleyicisi sınırları (CPU adına göre tablo).</summary>
    public class PlatformBilgisi
    {
        public bool Bulundu;
        public string Aile = "";            // "Intel Core 12. nesil (Alder Lake) — HX mobil"
        public List<string> DesteklenenTurler = new List<string>();      // "DDR4", "DDR5", "LPDDR5" ...
        public Dictionary<string, int> MaxHizMTs = new Dictionary<string, int>(); // tür -> resmi azami hız
        public int MaxKapasiteGB;           // 0 = bilinmiyor
        public int KanalSayisi;             // 0 = bilinmiyor
        public bool EccDestekli;
        public string Not = "";
        public string Kesinlik = "";        // "Üretici belgesi" / "Tahmini (aile bazında)"
    }

    /// <summary>Parça numarasından çıkarılan bilgiler.</summary>
    public class ParcaCozumu
    {
        public bool Bulundu;
        public string Marka = "";
        public string Seri = "";            // "ValueRAM", "FURY Beast", "Crucial"...
        public int KapasiteGB;              // 0 = çözülemedi
        public string Tur = "";
        public int HizMTs;
        public string FormFaktor = "";      // "DIMM" / "SODIMM"
        public int CL;                      // 0 = çözülemedi
        public bool? Ecc;
        public string Aciklama = "";        // insan okuyacak tek satır
    }

    public class Envanter
    {
        public SistemBilgisi Sistem = new SistemBilgisi();
        public List<BellekDizisi> Diziler = new List<BellekDizisi>();
        public List<BellekYuvasi> Yuvalar = new List<BellekYuvasi>();   // boş yuvalar dahil
        public KullanimBilgisi Kullanim = new KullanimBilgisi();
        public PlatformBilgisi Platform = new PlatformBilgisi();
        public bool SmbiosOkundu;
        public List<string> ToplamaNotlari = new List<string>();      // toplama sırasında oluşan uyarılar/hatalar
        public DateTime Zaman;

        public List<BellekYuvasi> DoluYuvalar
        {
            get { return Yuvalar.FindAll(delegate(BellekYuvasi y) { return y.Dolu; }); }
        }

        public long ToplamTakiliMB
        {
            get { long t = 0; foreach (BellekYuvasi y in Yuvalar) if (y.Dolu) t += y.BoyutMB; return t; }
        }
    }

    // ---- Danışman çıktısı ----------------------------------------------------

    public enum Seviye { Bilgi, Iyi, Uyari, Kritik }

    public class Bulgu
    {
        public Seviye Seviye;
        public string Baslik = "";
        public string Aciklama = "";        // düz metin (HTML'e çevrilirken kaçırılır); satır sonu \n
        public Bulgu() { }
        public Bulgu(Seviye s, string baslik, string aciklama) { Seviye = s; Baslik = baslik; Aciklama = aciklama; }
    }

    public enum YukseltmeDurumu { Bilinmiyor, Evet, Kismen, Hayir }

    public class AlinacakModul
    {
        public int Adet;
        public int ModulGB;
        public string Tur = "";             // "DDR5"
        public string FormFaktor = "";      // "SODIMM"
        public int HizMTs;                  // önerilen hız (mevcutla eş ya da platform azamisi)
        public int VoltajMv;                // 0 = belirtme
        public bool Ecc;
        public bool Kayitli;
        public string Aciklama = "";
    }

    public class YukseltmeSenaryosu
    {
        public string Ad = "";              // "Boş yuvaya ekle", "Hepsini değiştir (kit)"
        public string Aciklama = "";
        public List<AlinacakModul> Alinacaklar = new List<AlinacakModul>();
        public List<string> Cikarilacaklar = new List<string>(); // çıkarılacak yuva adları
        public int SonucToplamGB;
        public bool CiftKanal;
        public bool Flex;                   // kısmi çift kanal (flex mode): eşleşmeyen kısım tek kanal
        public bool Sunucu;                 // sunucu/iş istasyonu: kanal yerleşimi kılavuza göre
        public bool Tavsiye;                // önerilen senaryo mu
        public List<string> Notlar = new List<string>();
    }

    public class SatinAlmaOzeti
    {
        public bool Gecerli;                // false ise satın alma önerilemiyor (lehimli, bilinmiyor...)
        public string Tur = "";
        public string FormFaktor = "";
        public int HizMTs;
        public int VoltajMv;
        public bool Ecc;
        public bool Kayitli;
        public int ModulGB;
        public int Adet;
        public string AramaMetni = "";      // mağazada aratılacak metin: "16GB DDR5 5600 SODIMM"
        public string OnerilenParcaNo = ""; // birebir eşi (mevcut modülün parça numarası)
        public List<string> Ozellikler = new List<string>(); // madde madde teknik şartname
        public string Gerekce = "";
    }

    public class DanismanSonucu
    {
        public YukseltmeDurumu Durum = YukseltmeDurumu.Bilinmiyor;
        public string OzetCumle = "";       // en üstte görünen tek cümlelik cevap
        public int MevcutToplamGB;
        public int ToplamYuva;              // bilinen/tahmin edilen
        public int BosYuva;
        public bool YuvaBilgisiGuvenilir;
        public int AzamiGB;                 // önerilen azami (BIOS + platform birlikte değerlendirilmiş), 0 = bilinmiyor
        public string AzamiGBKaynagi = "";
        public int PratikAzamiGB;           // yuva x güvenli modül boyutu (AzamiGB'den küçükse), 0 = yok
        public string PratikAzamiMetni = "";// "2 yuva x 32 GB"
        public int SupheliEkYuva;           // BIOS'un gizlediği düşünülen muhtemel yuva sayısı (harita için)
        public string KanalDurumu = "";     // "Çift kanal (eş modüller)", "Tek kanal" ...
        public List<Bulgu> Bulgular = new List<Bulgu>();          // donanım tespitleri / uyarılar
        public List<Bulgu> IhtiyacAnalizi = new List<Bulgu>();    // kullanım verisine göre gerçekten gerekiyor mu
        public List<YukseltmeSenaryosu> Senaryolar = new List<YukseltmeSenaryosu>();
        public SatinAlmaOzeti Alinacak = new SatinAlmaOzeti();
        public List<string> KontrolListesi = new List<string>(); // satın almadan önce son kontrol
        public List<string> TakmaAdimlari = new List<string>();  // bu cihaza özel takma talimatı
        public List<Baglanti> Baglantilar = new List<Baglanti>(); // üretici belgesi araması vb.
    }

    public class Baglanti
    {
        public string Metin = "";
        public string Url = "";
        public Baglanti() { }
        public Baglanti(string metin, string url) { Metin = metin; Url = url; }
    }

    // ---- Yardımcılar ---------------------------------------------------------

    public static class Html
    {
        /// <summary>HTML kaçırma. Kullanıcı/donanım kaynaklı her metin buradan geçmeli.</summary>
        public static string Ka(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            StringBuilder sb = new StringBuilder(s.Length + 16);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '\'': sb.Append("&#39;"); break;
                    case '\n': sb.Append("<br>"); break;
                    case '\r': break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
    }

    public static class Bicim
    {
        /// <summary>MB → "16 GB" / "512 MB" / "1,5 GB".</summary>
        public static string Boyut(long mb)
        {
            if (mb <= 0) return "—";
            if (mb < 1024) return mb + " MB";
            double gb = mb / 1024.0;
            if (Math.Abs(gb - Math.Round(gb)) < 0.05) return ((long)Math.Round(gb)) + " GB";
            return gb.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")) + " GB";
        }

        public static string Voltaj(int mv)
        {
            if (mv <= 0) return "—";
            return (mv / 1000.0).ToString("0.00#", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")) + " V";
        }

        public static string Hiz(int mts)
        {
            if (mts <= 0) return "—";
            return mts + " MT/s";
        }
    }
}
