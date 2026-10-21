// RAM Danışmanı — işlemci adından bellek denetleyicisi sınırları.
//
// Yapı: (1) Tani(): işlemci adını çözümleyip bir "Tanim" (aile anahtarı, nesil,
// segment, model no...) çıkarır; (2) Tablo: veri satırları; (3) Esle(): Tanim'e
// uyan İLK satırı bulur. Yeni bir aile eklemek için çoğu zaman sadece TabloyuDoldur()
// içine bir Ekle(...) satırı yazmak yeterlidir (özelden genele doğru sıralayın).
//
// Değerler Intel ARK / AMD ürün sayfalarındaki aile bazındaki resmi değerlerdir.
// Emin olunmayan rakamlar 0 bırakılmış ve satır "Tahmini" işaretlenmiştir.
// Tür adları: DDR3, DDR3L, DDR4, DDR5, LPDDR3, LPDDR4, LPDDR4X, LPDDR5, LPDDR5X, DDR2.
// SMBIOS DDR3L'yi "DDR3", LPDDR4X'i "LPDDR4", LPDDR5X'i "LPDDR5" diye bildirir;
// karşılaştırma için TurDestekleniyor() / AzamiHiz() yardımcılarını kullanın.
//
// C# 5: $"", ?. , => üye, nameof, out var YOK. Metin işlemleri kültürden bağımsızdır
// (tr-TR'de "i".ToUpper() = "İ" tuzağı): ToUpperInvariant + Ordinal karşılaştırma.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RamDanismani
{
    public static class Platform
    {
        public const string KesinlikResmi = "Resmi (aile bazında)";
        public const string KesinlikTahmini = "Tahmini";

        // ---- Ortak notlar ------------------------------------------------------

        const string ND = "Dizüstünde gerçek sınırı anakart ve bilgisayar üreticisi belirler; bellek kısmen ya da tamamen lehimli olabilir. Satın almadan önce cihazın üretici belgesinden doğrulayın.";
        const string NM = "Anakartın desteklediği bellek türü, hızı ve azami kapasitesi de geçerlidir (anakart belgesi / QVL listesi).";
        const string NL = "Bellek lehimli (LPDDR) ya da işlemci paketinin üzerindedir; sonradan yükseltilemez.";
        const string NC = "Bu dönemde bellek denetleyicisi işlemcide değil anakarttaki yonga setindedir; bellek türünü ve sınırları anakart belirler.";
        const string NS = "Sunucu / iş istasyonu platformu: genelde ECC, çoğu zaman Registered (RDIMM) bellek kullanılır; anakart ya da sunucu belgesine bakın.";
        const string NH = " Hız: daha hızlı modül takılabilir ama işlemcinin/anakartın izin verdiği hızda çalışır.";
        const string N45 = " Anakart ya DDR4 ya DDR5 kullanır; ikisi aynı anda kullanılamaz ve yuvaları birbirine uymaz.";
        const string NE = " ECC (UDIMM) desteği anakarta bağlıdır.";
        const string N3L = " 1,35 V DDR3L modül alın (1,5 V DDR3 çalışmayabilir).";
        const string NK4 = " Dört kanallı: en iyi performans için modüller dörderli takılır.";

        // ---- Veri tipleri ----------------------------------------------------

        sealed class Satir
        {
            public string Anahtar = "";
            public int NesilMin;
            public int NesilMax;
            public string Segmentler = "*";   // boşlukla ayrılmış; "*" = hepsi
            public string Ad = "";            // "{N}" nesil ile değiştirilir
            public string Turler = "";        // "DDR5:4800 DDR4:3200" — hız 0 = bilinmiyor
            public int KapasiteGB;
            public int Kanal;
            public bool Ecc;
            public bool Kesin;
            public string Not = "";
            public int ModelMin;              // 0/0 = tüm modeller
            public int ModelMax;
            public string Siniflar = "";      // "79" = i7 ve i9; "" = hepsi
            public string Sonekler = "";      // "K KF"; "" = hepsi
            public string Altlar = "";        // "40 45"; "" = hepsi
            public string Modeller = "";      // "N4020 J4125 N2*"; "" = hepsi
        }

        sealed class Tanim
        {
            public string Anahtar = "";
            public int Nesil;
            public string Segment = "";
            public int Model;
            public string ModelKod = "";
            public int Sinif;
            public string Sonek = "";
            public string Alt = "";
            public bool Pro;
            public bool Tahmini;
            public string AileOzel = "";
        }

        static readonly List<Satir> Tablo = new List<Satir>();
        const RegexOptions RO = RegexOptions.CultureInvariant;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        static Platform()
        {
            TabloyuDoldur();
        }

        // ---- Genel API ---------------------------------------------------------

        /// <summary>İşlemci adından bellek denetleyicisi sınırları. Asla istisna fırlatmaz.</summary>
        public static PlatformBilgisi Bul(string islemciAdi)
        {
            PlatformBilgisi p = new PlatformBilgisi();
            try
            {
                if (string.IsNullOrEmpty(islemciAdi) || islemciAdi.Trim().Length == 0)
                {
                    p.Not = "İşlemci adı okunamadı.";
                    return p;
                }
                string n = Normallestir(islemciAdi);
                Tanim t = Tani(n);
                Satir s = t == null ? null : Esle(t);
                if (s == null)
                {
                    p = new PlatformBilgisi();
                    if (n.Contains("XEON")) p.Not = "Xeon modeli tabloda yok. " + NS;
                    else if (n.Contains("APPLE")) p.Not = "Apple işlemcilerde bellek işlemci paketindedir; yükseltilemez.";
                    else p.Not = "İşlemci modeli bellek tablosunda bulunamadı; işlemci üreticisinin ürün sayfasına bakın.";
                    return p;
                }
                Doldur(p, s, t);
            }
            catch (Exception ex)
            {
                p = new PlatformBilgisi();
                p.Not = "İşlemci bilgisi çözümlenemedi: " + ex.Message;
            }
            return p;
        }

        /// <summary>
        /// Bellek türü platformda destekleniyor mu? SMBIOS'un ayırt edemediği eşdeğerleri
        /// kabul eder: DDR3 ~ DDR3L, LPDDR4 ~ LPDDR4X, LPDDR5 ~ LPDDR5X. Büyük/küçük harf duyarsız.
        /// </summary>
        public static bool TurDestekleniyor(PlatformBilgisi p, string tur)
        {
            if (p == null || p.DesteklenenTurler == null || string.IsNullOrEmpty(tur)) return false;
            string a = TurTaban(tur);
            foreach (string d in p.DesteklenenTurler)
                if (TurTaban(d) == a) return true;
            return false;
        }

        /// <summary>Tür için resmi azami hız (MT/s), 0 = bilinmiyor. Eşdeğer türleri de dener.</summary>
        public static int AzamiHiz(PlatformBilgisi p, string tur)
        {
            if (p == null || p.MaxHizMTs == null || string.IsNullOrEmpty(tur)) return 0;
            string t = tur.Trim().ToUpperInvariant();
            int enIyi = 0;
            foreach (KeyValuePair<string, int> kv in p.MaxHizMTs)
                if (string.Equals(kv.Key, t, StringComparison.OrdinalIgnoreCase)) return kv.Value;
            string a = TurTaban(t);
            foreach (KeyValuePair<string, int> kv in p.MaxHizMTs)
                if (TurTaban(kv.Key) == a && kv.Value > enIyi) enIyi = kv.Value;
            return enIyi;
        }

        static string TurTaban(string tur)
        {
            string t = (tur ?? "").Trim().ToUpperInvariant().Replace("-", "").Replace(" ", "");
            if (t == "DDR3L") return "DDR3";
            if (t.StartsWith("LPDDR", StringComparison.Ordinal) && t.EndsWith("X", StringComparison.Ordinal)) return t.Substring(0, t.Length - 1);
            return t;
        }

        // ---- Çözümleme -----------------------------------------------------------

        static string Normallestir(string ad)
        {
            string n = ad.ToUpperInvariant();
            n = n.Replace("(R)", " ").Replace("(TM)", " ").Replace("(C)", " ").Replace("®", " ").Replace("™", " ");
            n = Regex.Replace(n, @"\s+", " ", RO).Trim();
            return n;
        }

        static int Sayi(string s)
        {
            int v;
            if (int.TryParse(s, NumberStyles.None, Inv, out v)) return v;
            return 0;
        }

        static string Harfler(string sonek)
        {
            int i = 0;
            while (i < sonek.Length && sonek[i] >= 'A' && sonek[i] <= 'Z') i++;
            return sonek.Substring(0, i);
        }

        static bool Bas(string s, string onek)
        {
            return s.StartsWith(onek, StringComparison.Ordinal);
        }

        // Intel
        static readonly Regex rUltra = new Regex(@"\bULTRA ?(X?[3579]) (\d{3})([A-Z0-9]*)", RO);
        static readonly Regex rCoreI = new Regex(@"\bI([3579])-(\d{3,5})([A-Z0-9]*)", RO);
        static readonly Regex rCoreIEski = new Regex(@"\bI([357]) CPU (?:([A-Z]{1,2}) )?(\d{3})\b", RO);
        static readonly Regex rCoreIY = new Regex(@"\bI([357])-(\d)Y(\d{2})", RO);
        static readonly Regex rCoreMY = new Regex(@"\bM([357])?[- ]?(\d)Y(\d{2})", RO);
        static readonly Regex rCoreM8 = new Regex(@"\bM([357])-(\d{4})Y", RO);
        static readonly Regex rCoreN = new Regex(@"\bCORE ([357]) (\d{3})([A-Z]*)\b", RO);
        static readonly Regex rIntelN = new Regex(@"\bN(50|95|97|100|150|200|250|300|305|350|355)\b", RO);
        static readonly Regex rAtom = new Regex(@"\b([NJ])(\d{4})[A-Z]?\b", RO);
        static readonly Regex rPentiumG = new Regex(@"\bG(\d{3,4})[A-Z]*\b", RO);
        static readonly Regex rCore2 = new Regex(@"\bCORE ?2 ?(DUO|QUAD|EXTREME|SOLO)\b", RO);
        static readonly Regex rPentiumEski = new Regex(@"\b[ET]\d{4}\b", RO);
        static readonly Regex rPentiumMobil = new Regex(@"\b(\d{4})([UY]?)\b", RO);
        static readonly Regex rXeonWYeni = new Regex(@"\bW([3579])-(\d{4})", RO);
        static readonly Regex rXeonW = new Regex(@"\bW-(\d{4})", RO);
        static readonly Regex rXeonE3 = new Regex(@"\bE3-(\d{4})[A-Z]?(?: ?V(\d))?", RO);
        static readonly Regex rXeonE5 = new Regex(@"\bE[57]-(\d{4})[A-Z]?(?: ?V(\d))?", RO);
        static readonly Regex rXeonE = new Regex(@"\bE-(\d{4})", RO);
        static readonly Regex rXeonSP = new Regex(@"\b(BRONZE|SILVER|GOLD|PLATINUM) (\d{4})", RO);
        // AMD
        static readonly Regex rRyzen = new Regex(@"\bRYZEN (?:([3579]) )?(?:(PRO) )?(\d{4})([A-Z0-9]*)", RO);
        static readonly Regex rRyzen3Hane = new Regex(@"\bRYZEN (?:[3579] )?(?:PRO )?(\d{3})\b", RO);
        static readonly Regex rUcHane = new Regex(@"\b(\d{3})\b", RO);
        static readonly Regex rPro = new Regex(@"\bPRO\b", RO);
        static readonly Regex rThreadripper = new Regex(@"\bTHREADRIPPER (?:(PRO) )?(\d{4})([A-Z0-9]*)", RO);
        static readonly Regex rRyzenZ = new Regex(@"\bRYZEN Z\d", RO);
        static readonly Regex rAthlon = new Regex(@"\bATHLON(?: (?:SILVER|GOLD|PRO))? (\d{3,4})([A-Z]*)", RO);
        static readonly Regex rFx = new Regex(@"\bFX ?-(\d{4})", RO);   // "FX(tm)-8350" -> "FX -8350"
        static readonly Regex rAmdA = new Regex(@"\bA(?:4|6|8|9|10|12)-(\d{4})", RO);
        // Qualcomm
        static readonly Regex rSnapdragonX1 = new Regex(@"\bX1[EP]\b|\bX1[EP]?-?\d{2}-?\d{3}\b|\bX1[EP]-|\bX (ELITE|PLUS)\b", RO);

        static Tanim Tani(string n)
        {
            bool amd = n.Contains("AMD") || n.Contains("RYZEN") || n.Contains("ATHLON");
            if (n.Contains("SNAPDRAGON") || n.Contains("QUALCOMM") || n.Contains("ORYON"))
                return SnapdragonTani(n);
            if (amd)
            {
                Tanim a = AmdTani(n);
                if (a != null) return a;
            }
            return IntelTani(n);
        }

        static Tanim IntelTani(string n)
        {
            Match m;
            if (n.Contains("XEON")) return XeonTani(n);

            // Core Ultra (Meteor / Lunar / Arrow / Panther Lake)
            m = rUltra.Match(n);
            if (m.Success)
            {
                int model = Sayi(m.Groups[2].Value);
                string h = Harfler(m.Groups[3].Value);
                string seg;
                if (h == "V") seg = "V";
                else if (Bas(h, "HX")) seg = "HX";
                else if (Bas(h, "H")) seg = "H";
                else if (Bas(h, "U")) seg = "U";
                else seg = "MASAUSTU";
                Tanim t = new Tanim();
                t.Anahtar = "ULTRA"; t.Nesil = model / 100; t.Segment = seg; t.Model = model;
                t.ModelKod = model.ToString(Inv); t.Sinif = Sayi(m.Groups[1].Value.Replace("X", "")); t.Sonek = h;
                return t;
            }

            // Core i3/i5/i7/i9 — yeni adlandırma (i5-12450HX, i7-1065G7, i5-3470, i7-920)
            m = rCoreI.Match(n);
            if (m.Success)
            {
                string kod = m.Groups[2].Value;
                int model = Sayi(kod);
                int nesil;
                if (kod.Length == 3) nesil = 1;
                else if (kod.Length == 4) nesil = kod[0] == '1' ? Sayi(kod.Substring(0, 2)) : kod[0] - '0';
                else nesil = Sayi(kod.Substring(0, 2));
                string h = Harfler(m.Groups[3].Value);
                Tanim t = new Tanim();
                t.Anahtar = "ICORE"; t.Nesil = nesil; t.Model = model; t.ModelKod = kod;
                t.Sinif = Sayi(m.Groups[1].Value); t.Sonek = h;
                t.Segment = IntelSegment(nesil, model, h);
                return t;
            }

            // Core i 1. nesil eski adlandırma: "Core(TM) i7 CPU 920", "Core(TM) i5 CPU M 520"
            m = rCoreIEski.Match(n);
            if (m.Success)
            {
                int model = Sayi(m.Groups[3].Value);
                string harf = m.Groups[2].Success ? m.Groups[2].Value : "";
                Tanim t = new Tanim();
                t.Anahtar = "ICORE"; t.Nesil = 1; t.Model = model; t.ModelKod = m.Groups[3].Value;
                t.Sinif = Sayi(m.Groups[1].Value); t.Sonek = harf;
                if (harf == "" || (harf == "X" && model >= 970)) t.Segment = "MASAUSTU";
                else t.Segment = "MOBIL";
                return t;
            }

            // Y serisi: i5-7Y54, m3-6Y30, Core M-5Y10c, m3-8100Y
            m = rCoreIY.Match(n);
            if (!m.Success) m = rCoreMY.Match(n);
            if (m.Success)
            {
                Tanim t = new Tanim();
                t.Anahtar = "ICORE"; t.Nesil = Sayi(m.Groups[2].Value); t.Segment = "Y";
                t.Sinif = m.Groups[1].Success ? Sayi(m.Groups[1].Value) : 0; t.Sonek = "Y";
                t.ModelKod = m.Groups[2].Value + "Y" + m.Groups[3].Value;
                return t;
            }
            m = rCoreM8.Match(n);
            if (m.Success)
            {
                string kod = m.Groups[2].Value;
                Tanim t = new Tanim();
                t.Anahtar = "ICORE"; t.Nesil = kod[0] - '0'; t.Segment = "Y"; t.Model = Sayi(kod);
                t.ModelKod = kod; t.Sinif = Sayi(m.Groups[1].Value); t.Sonek = "Y";
                return t;
            }

            // Intel N serisi (Alder Lake-N / Twin Lake): N100, N200, i3-N305, Core 3 N355
            m = rIntelN.Match(n);
            if (m.Success && (n.Contains("INTEL") || n.Contains("CORE")))
            {
                Tanim t = new Tanim();
                t.Anahtar = "INTEL_N"; t.ModelKod = "N" + m.Groups[1].Value; t.Model = Sayi(m.Groups[1].Value);
                return t;
            }

            // Core 3/5/7 (Ultra olmayan, 2024+): "Core 7 150U", "Core 5 210H"
            m = rCoreN.Match(n);
            if (m.Success)
            {
                int model = Sayi(m.Groups[2].Value);
                string h = Harfler(m.Groups[3].Value);
                Tanim t = new Tanim();
                t.Anahtar = "CORE_N"; t.Nesil = model / 100; t.Model = model; t.ModelKod = m.Groups[2].Value;
                t.Sinif = Sayi(m.Groups[1].Value); t.Sonek = h;
                if (Bas(h, "U")) t.Segment = "U";
                else if (Bas(h, "H")) t.Segment = "H";
                else t.Segment = "MASAUSTU";
                return t;
            }

            bool pc = n.Contains("CELERON") || n.Contains("PENTIUM");

            // Celeron / Pentium Silver: N4020, N5030, J4125 ...
            m = rAtom.Match(n);
            if (m.Success && (pc || n.Contains("INTEL")))
            {
                Tanim t = new Tanim();
                t.Anahtar = "ATOM"; t.ModelKod = m.Groups[1].Value + m.Groups[2].Value; t.Model = Sayi(m.Groups[2].Value);
                return t;
            }

            // Core 2 dönemi (bellek denetleyicisi yonga setinde)
            if (rCore2.IsMatch(n) || n.Contains("CORE2") || (pc && rPentiumEski.IsMatch(n)))
            {
                Tanim t = new Tanim();
                t.Anahtar = "CORE2";
                return t;
            }

            if (pc)
            {
                // Masaüstü Pentium / Celeron G serisi -> aynı nesil Core i3 platformu
                m = rPentiumG.Match(n);
                if (m.Success)
                {
                    int g = Sayi(m.Groups[1].Value);
                    int nesil = PentiumGNesli(g);
                    if (nesil > 0)
                    {
                        Tanim t = new Tanim();
                        t.Anahtar = "ICORE"; t.Nesil = nesil; t.Segment = "MASAUSTU"; t.Sinif = 3;
                        t.ModelKod = "G" + m.Groups[1].Value; t.Tahmini = true;
                        t.AileOzel = "Intel Pentium/Celeron G" + m.Groups[1].Value + " (" + nesil + ". nesil Core ile aynı platform)";
                        return t;
                    }
                }
                // Mobil Pentium Gold / Celeron: 4415U, 5405U, 7505 ...
                m = rPentiumMobil.Match(n);
                if (m.Success)
                {
                    Tanim t = new Tanim();
                    t.Anahtar = "PC_MOBIL"; t.ModelKod = m.Groups[1].Value; t.Model = Sayi(m.Groups[1].Value);
                    t.Tahmini = true;
                    return t;
                }
            }
            return null;
        }

        static int PentiumGNesli(int g)
        {
            if (g < 1000) return 2;                                   // G530, G640, G860
            if ((g >= 1600 && g <= 1699) || (g >= 2000 && g <= 2199)) return 3;
            if ((g >= 1800 && g <= 1899) || (g >= 3200 && g <= 3499)) return 4;
            if ((g >= 3900 && g <= 3929) || (g >= 4400 && g <= 4529)) return 6;
            if ((g >= 3930 && g <= 3999) || (g >= 4530 && g <= 4699)) return 7;
            if ((g >= 4900 && g <= 4999) || (g >= 5400 && g <= 5699)) return 8;
            if ((g >= 5900 && g <= 5999) || (g >= 6400 && g <= 6699)) return 10;
            if ((g >= 6900 && g <= 6999) || (g >= 7400 && g <= 7499)) return 12;
            return 0;
        }

        static string IntelSegment(int nesil, int model, string h)
        {
            if (Bas(h, "HX")) return "HX";
            if (Bas(h, "H")) return "H";        // H, HK, HQ, HS
            if (Bas(h, "U")) return "U";
            if (Bas(h, "Y")) return "Y";
            if (Bas(h, "NG")) return "G";       // 1030NG7, 1068NG7 (Ice Lake, Apple)
            if (Bas(h, "G")) return "G";        // 1065G7, 1165G7, 8809G
            if ((h == "P" || h == "PE") && nesil >= 12) return "P";   // PE: gömülü Alder/Raptor Lake-P
            switch (h)
            {
                case "M": case "QM": case "XM": case "MQ": case "MX": case "LM": case "UM":
                case "QE": case "ME": case "EQ": case "LE": case "UE":
                    return "MOBIL";
            }
            bool hedtSonek = h == "" || h == "K" || h == "X" || h == "XE";
            if (hedtSonek && ((nesil == 3 && model >= 3800 && model <= 3999) || (nesil == 4 && model >= 4800 && model <= 4999)
                || (nesil == 5 && model >= 5800 && model <= 5999) || (nesil == 6 && model >= 6800 && model <= 6999)))
                return "HEDT";
            if ((h == "X" || h == "XE") && ((nesil == 7 && model >= 7640) || (nesil == 9 && model >= 9800) || (nesil == 10 && model >= 10900)))
                return "HEDT";
            return "MASAUSTU";
        }

        static Tanim XeonTani(string n)
        {
            Tanim t = new Tanim();
            Match m = rXeonWYeni.Match(n);
            if (m.Success) { t.Anahtar = "XEON_WX"; t.Model = Sayi(m.Groups[2].Value); t.ModelKod = m.Groups[2].Value; return t; }
            m = rXeonW.Match(n);
            if (m.Success) { t.Anahtar = "XEON_W"; t.Model = Sayi(m.Groups[1].Value); t.ModelKod = m.Groups[1].Value; return t; }
            m = rXeonE3.Match(n);
            if (m.Success)
            {
                t.Anahtar = "XEON_E3"; t.Model = Sayi(m.Groups[1].Value); t.ModelKod = m.Groups[1].Value;
                t.Nesil = m.Groups[2].Success ? Sayi(m.Groups[2].Value) : 1;
                return t;
            }
            m = rXeonE5.Match(n);
            if (m.Success)
            {
                t.Anahtar = "XEON_E5"; t.Model = Sayi(m.Groups[1].Value); t.ModelKod = m.Groups[1].Value;
                t.Nesil = m.Groups[2].Success ? Sayi(m.Groups[2].Value) : 1;
                return t;
            }
            m = rXeonE.Match(n);
            if (m.Success) { t.Anahtar = "XEON_E"; t.Model = Sayi(m.Groups[1].Value); t.ModelKod = m.Groups[1].Value; return t; }
            m = rXeonSP.Match(n);
            if (m.Success)
            {
                string kod = m.Groups[2].Value;
                t.Anahtar = "XEON_SP"; t.Model = Sayi(kod); t.ModelKod = kod; t.Nesil = kod[1] - '0';
                return t;
            }
            return null;
        }

        static Tanim AmdTani(string n)
        {
            Tanim t = new Tanim();
            Match m;

            int ai = n.IndexOf("RYZEN AI", StringComparison.Ordinal);
            if (ai >= 0)
            {
                string kalan = n.Substring(ai + 8);
                m = rUcHane.Match(kalan);
                if (!m.Success) return null;
                t.Anahtar = "RYZEN_AI"; t.Model = Sayi(m.Groups[1].Value); t.ModelKod = m.Groups[1].Value;
                t.Nesil = t.Model / 100; t.Pro = rPro.IsMatch(kalan);
                t.Segment = kalan.Contains("MAX") ? "MAX" : "MOBIL";
                return t;
            }

            m = rThreadripper.Match(n);
            if (m.Success)
            {
                t.Anahtar = "TR"; t.Model = Sayi(m.Groups[2].Value); t.ModelKod = m.Groups[2].Value;
                t.Nesil = t.Model / 1000; t.Pro = m.Groups[1].Success;
                t.Sonek = Harfler(m.Groups[3].Value);
                t.Segment = Bas(t.Sonek, "WX") ? "WX" : "HEDT";
                return t;
            }

            m = rRyzen.Match(n);
            if (m.Success)
            {
                int model = Sayi(m.Groups[3].Value);
                string h = Harfler(m.Groups[4].Value);
                t.Anahtar = "RYZEN"; t.Model = model; t.ModelKod = m.Groups[3].Value; t.Nesil = model / 1000;
                t.Sinif = m.Groups[1].Success ? Sayi(m.Groups[1].Value) : 0; t.Pro = m.Groups[2].Success; t.Sonek = h;
                t.Alt = (((model / 10) % 10) * 10 + model % 10).ToString("00", Inv);
                if (Bas(h, "U") || Bas(h, "H") || Bas(h, "C")) t.Segment = "MOBIL";
                else if (Bas(h, "G")) t.Segment = "APU";
                else t.Segment = "MASAUSTU";
                return t;
            }

            m = rRyzen3Hane.Match(n);
            if (m.Success)
            {
                t.Anahtar = "RYZEN_200"; t.Model = Sayi(m.Groups[1].Value); t.ModelKod = m.Groups[1].Value;
                t.Nesil = t.Model / 100; t.Segment = "MOBIL";
                return t;
            }

            if (rRyzenZ.IsMatch(n)) { t.Anahtar = "RYZEN_Z"; return t; }

            m = rAthlon.Match(n);
            if (m.Success)
            {
                string kod = m.Groups[1].Value;
                int model = Sayi(kod);
                string h = Harfler(m.Groups[2].Value);
                t.Anahtar = "ATHLON"; t.Model = model; t.ModelKod = kod; t.Sonek = h;
                if (kod.Length == 4)
                {
                    t.Nesil = model / 1000;
                    t.Alt = (((model / 10) % 10) * 10 + model % 10).ToString("00", Inv);
                }
                if (Bas(h, "U") || Bas(h, "E") || Bas(h, "C")) t.Segment = "MOBIL";
                else if (Bas(h, "G")) t.Segment = "APU";
                else t.Segment = "MASAUSTU";
                return t;
            }

            m = rFx.Match(n);
            if (m.Success) { t.Anahtar = "FX"; t.Model = Sayi(m.Groups[1].Value); t.ModelKod = m.Groups[1].Value; return t; }

            m = rAmdA.Match(n);
            if (m.Success)
            {
                t.Anahtar = "AMD_A"; t.Model = Sayi(m.Groups[1].Value); t.ModelKod = m.Groups[1].Value;
                t.Nesil = m.Groups[1].Value[0] - '0';
                return t;
            }
            return null;
        }

        static Tanim SnapdragonTani(string n)
        {
            Tanim t = new Tanim();
            t.Anahtar = "SNAPDRAGON";
            if (rSnapdragonX1.IsMatch(n) && !n.Contains("X2")) { t.Nesil = 1; return t; }
            if (n.Contains("X2")) { t.Nesil = 2; return t; }
            return null;
        }

        // ---- Eşleme ----------------------------------------------------------------

        static bool Listede(string liste, string deger)
        {
            if (string.IsNullOrEmpty(liste) || liste == "*") return true;
            foreach (string p in liste.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (p.EndsWith("*", StringComparison.Ordinal))
                {
                    if (deger.StartsWith(p.Substring(0, p.Length - 1), StringComparison.Ordinal)) return true;
                }
                else if (p == deger) return true;
            }
            return false;
        }

        static Satir Esle(Tanim t)
        {
            foreach (Satir s in Tablo)
            {
                if (s.Anahtar != t.Anahtar) continue;
                if (t.Nesil < s.NesilMin || t.Nesil > s.NesilMax) continue;
                if (!Listede(s.Segmentler, t.Segment)) continue;
                if ((s.ModelMin != 0 || s.ModelMax != 0) && (t.Model < s.ModelMin || t.Model > s.ModelMax)) continue;
                if (s.Siniflar.Length > 0 && (t.Sinif <= 0 || s.Siniflar.IndexOf((char)('0' + t.Sinif)) < 0)) continue;
                if (!Listede(s.Sonekler, t.Sonek)) continue;
                if (!Listede(s.Altlar, t.Alt)) continue;
                if (!Listede(s.Modeller, t.ModelKod)) continue;
                return s;
            }
            return null;
        }

        static void Doldur(PlatformBilgisi p, Satir s, Tanim t)
        {
            p.Bulundu = true;
            p.Aile = t.AileOzel.Length > 0 ? t.AileOzel : s.Ad.Replace("{N}", t.Nesil.ToString(Inv));
            if (t.Pro && p.Aile.IndexOf("PRO", StringComparison.Ordinal) < 0) p.Aile += " — PRO";
            p.MaxHizMTs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            p.DesteklenenTurler = new List<string>();
            foreach (string parca in s.Turler.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] ik = parca.Split(':');
                string tur = ik[0];
                if (!p.DesteklenenTurler.Contains(tur)) p.DesteklenenTurler.Add(tur);
                int hiz = ik.Length > 1 ? Sayi(ik[1]) : 0;
                if (hiz > 0) p.MaxHizMTs[tur] = hiz;
            }
            p.MaxKapasiteGB = s.KapasiteGB;
            p.KanalSayisi = s.Kanal;
            p.EccDestekli = s.Ecc;
            string not = s.Not;
            // Ryzen PRO masaüstü işlemciler ECC'yi resmi olarak destekler.
            if (t.Pro && (t.Anahtar == "RYZEN" || t.Anahtar == "TR") && (t.Segment == "MASAUSTU" || t.Segment == "APU" || t.Segment == "WX" || t.Segment == "HEDT"))
            {
                if (!p.EccDestekli) not += NE;
                p.EccDestekli = true;
            }
            p.Kesinlik = (s.Kesin && !t.Tahmini) ? KesinlikResmi : KesinlikTahmini;
            if (p.DesteklenenTurler.Count > 0 && !(p.DesteklenenTurler.Count == 1 && Bas(p.DesteklenenTurler[0], "LP")) && not.IndexOf("yükseltilemez", StringComparison.Ordinal) < 0)
                not += NH;
            p.Not = not;
        }

        // ---- Tablo -------------------------------------------------------------------

        static void Ekle(string anahtar, int nesilMin, int nesilMax, string segmentler, string ad, string turler,
            int kapasiteGB, int kanal, bool kesin, string not,
            bool ecc = false, int modelMin = 0, int modelMax = 0, string siniflar = "", string sonekler = "",
            string altlar = "", string modeller = "")
        {
            Satir s = new Satir();
            s.Anahtar = anahtar; s.NesilMin = nesilMin; s.NesilMax = nesilMax; s.Segmentler = segmentler;
            s.Ad = ad; s.Turler = turler; s.KapasiteGB = kapasiteGB; s.Kanal = kanal; s.Kesin = kesin;
            s.Not = not ?? ""; s.Ecc = ecc; s.ModelMin = modelMin; s.ModelMax = modelMax;
            s.Siniflar = siniflar; s.Sonekler = sonekler; s.Altlar = altlar; s.Modeller = modeller;
            Tablo.Add(s);
        }

        const int HEP = 99; // nesil üst sınırı "hepsi"

        static void TabloyuDoldur()
        {
            // ===== Intel Core i (anahtar ICORE; nesil = model numarasından) ==========
            // 1. nesil (Nehalem / Westmere)
            Ekle("ICORE", 1, 1, "MASAUSTU", "Intel Core i7 1. nesil (Bloomfield/Gulftown, LGA1366)", "DDR3:1066", 24, 3, true,
                NM + " Üç kanallı: modüller üçerli takılır.", modelMin: 900, modelMax: 999);
            Ekle("ICORE", 1, 1, "MASAUSTU", "Intel Core 1. nesil (Lynnfield/Clarkdale, LGA1156)", "DDR3:1333", 16, 2, true, NM);
            Ekle("ICORE", 1, 1, "MOBIL H U", "Intel Core 1. nesil (Arrandale/Clarksfield) — mobil", "DDR3:1066", 8, 2, false,
                ND + " QM/XM modellerde 1333 MT/s.");
            // 2. nesil (Sandy Bridge)
            Ekle("ICORE", 2, 2, "MASAUSTU", "Intel Core 2. nesil (Sandy Bridge)", "DDR3:1333", 32, 2, true, NM);
            Ekle("ICORE", 2, 2, "MOBIL H U", "Intel Core 2. nesil (Sandy Bridge) — mobil", "DDR3:1333", 0, 2, false,
                ND + " Bu nesil dizüstüler pratikte genelde 16 GB'a kadar destekler; QM/XM modellerde 1600 MT/s.");
            // 3. nesil (Ivy Bridge) + Sandy Bridge-E HEDT (i7-38xx/39xx)
            Ekle("ICORE", 3, 3, "HEDT", "Intel Core i7 HEDT (Sandy Bridge-E, LGA2011)", "DDR3:1600", 64, 4, true, NM + NK4);
            Ekle("ICORE", 3, 3, "MASAUSTU", "Intel Core 3. nesil (Ivy Bridge)", "DDR3:1600", 32, 2, true, NM);
            Ekle("ICORE", 3, 3, "MOBIL H U Y", "Intel Core 3. nesil (Ivy Bridge) — mobil", "DDR3:1600 DDR3L:1600", 0, 2, false,
                ND + " Pratikte çoğu dizüstü 16 GB'a kadar destekler.");
            // 4. nesil (Haswell) + Ivy Bridge-E HEDT (i7-48xx/49xx)
            Ekle("ICORE", 4, 4, "HEDT", "Intel Core i7 HEDT (Ivy Bridge-E, LGA2011)", "DDR3:1866", 64, 4, true, NM + NK4);
            Ekle("ICORE", 4, 4, "MASAUSTU", "Intel Core 4. nesil (Haswell)", "DDR3:1600 DDR3L:1600", 32, 2, true, NM);
            Ekle("ICORE", 4, 4, "MOBIL H", "Intel Core 4. nesil (Haswell) — M/H mobil", "DDR3L:1600", 32, 2, true, ND + N3L);
            Ekle("ICORE", 4, 4, "U Y", "Intel Core 4. nesil (Haswell) — U/Y ince mobil", "DDR3L:1600 LPDDR3:1600", 16, 2, true, ND + N3L);
            // 5. nesil (Broadwell) + Haswell-E HEDT (i7-58xx/59xx)
            Ekle("ICORE", 5, 5, "HEDT", "Intel Core i7 HEDT (Haswell-E, LGA2011-3)", "DDR4:2133", 64, 4, true, NM + NK4);
            Ekle("ICORE", 5, 5, "MASAUSTU", "Intel Core 5. nesil (Broadwell)", "DDR3L:1600", 32, 2, true, NM + N3L);
            Ekle("ICORE", 5, 5, "U Y", "Intel Core 5. nesil (Broadwell) — U/Y ince mobil", "DDR3L:1600 LPDDR3:1600", 16, 2, true, ND + N3L);
            Ekle("ICORE", 5, 5, "MOBIL H", "Intel Core 5. nesil (Broadwell) — H mobil", "DDR3L:1600", 32, 2, false, ND + N3L);
            // 6. nesil (Skylake) + Broadwell-E HEDT (i7-68xx/69xx)
            Ekle("ICORE", 6, 6, "HEDT", "Intel Core i7 HEDT (Broadwell-E, LGA2011-3)", "DDR4:2400", 128, 4, true, NM + NK4);
            Ekle("ICORE", 6, 6, "MASAUSTU", "Intel Core 6. nesil (Skylake)", "DDR4:2133 DDR3L:1600", 64, 2, true, NM);
            Ekle("ICORE", 6, 6, "U", "Intel Core 6. nesil (Skylake) — U ince mobil", "DDR4:2133 LPDDR3:1866 DDR3L:1600", 32, 2, true, ND);
            Ekle("ICORE", 6, 6, "H MOBIL", "Intel Core 6. nesil (Skylake) — H mobil", "DDR4:2133 LPDDR3:1866 DDR3L:1600", 64, 2, true, ND);
            Ekle("ICORE", 6, 6, "Y", "Intel Core m / 6. nesil (Skylake) — Y", "LPDDR3:1866 DDR3L:1600", 16, 2, true, ND);
            // 7. nesil (Kaby Lake) + Kaby Lake-X / Skylake-X HEDT (LGA2066)
            Ekle("ICORE", 7, 7, "HEDT", "Intel Core 7. nesil HEDT (Kaby Lake-X, LGA2066)", "DDR4:2666", 64, 2, true, NM, modelMin: 7600, modelMax: 7799);
            Ekle("ICORE", 7, 7, "HEDT", "Intel Core i7-7800X (Skylake-X, LGA2066)", "DDR4:2400", 128, 4, true, NM + NK4, modelMin: 7800, modelMax: 7800);
            Ekle("ICORE", 7, 7, "HEDT", "Intel Core 7. nesil HEDT (Skylake-X, LGA2066)", "DDR4:2666", 128, 4, true, NM + NK4);
            Ekle("ICORE", 7, 7, "MASAUSTU", "Intel Core 7. nesil (Kaby Lake)", "DDR4:2400 DDR3L:1600", 64, 2, true, NM);
            Ekle("ICORE", 7, 7, "U", "Intel Core 7. nesil (Kaby Lake) — U ince mobil", "DDR4:2133 LPDDR3:1866 DDR3L:1600", 32, 2, true, ND);
            Ekle("ICORE", 7, 7, "H MOBIL", "Intel Core 7. nesil (Kaby Lake) — H mobil", "DDR4:2400 LPDDR3:2133 DDR3L:1600", 64, 2, true, ND);
            Ekle("ICORE", 7, 7, "Y", "Intel Core m / 7. nesil (Kaby Lake) — Y", "LPDDR3:1866 DDR3L:1600", 16, 2, true, ND);
            // 8. nesil (Coffee Lake / Kaby Lake R / Whiskey Lake / Amber Lake)
            Ekle("ICORE", 8, 8, "MASAUSTU", "Intel Core i3 8. nesil (Coffee Lake)", "DDR4:2400", 0, 2, false,
                NM + " Azami kapasite BIOS sürümüne göre 64–128 GB.", siniflar: "3");
            Ekle("ICORE", 8, 8, "MASAUSTU", "Intel Core 8. nesil (Coffee Lake)", "DDR4:2666", 128, 2, true,
                NM + " Eski BIOS'larda 64 GB sınırı olabilir (32 GB'lık modüller güncel BIOS ister).");
            Ekle("ICORE", 8, 8, "U", "Intel Core 8. nesil (Whiskey Lake) — U ince mobil", "DDR4:2400 LPDDR3:2133", 64, 2, true, ND,
                modeller: "8145 8265 8365 8565 8665");
            Ekle("ICORE", 8, 8, "U", "Intel Core 8. nesil (Kaby Lake R / Coffee Lake U) — U ince mobil", "DDR4:2400 LPDDR3:2133", 32, 2, true, ND);
            Ekle("ICORE", 8, 8, "H MOBIL", "Intel Core 8. nesil (Coffee Lake-H) — H mobil", "DDR4:2666 LPDDR3:2133", 64, 2, true, ND);
            Ekle("ICORE", 8, 8, "Y", "Intel Core 8. nesil (Amber Lake) — Y", "LPDDR3:1866", 16, 2, false, NL);
            Ekle("ICORE", 8, 8, "G", "Intel Core 8. nesil (Kaby Lake-G) — mobil", "DDR4:2400", 0, 2, false, ND);
            // 9. nesil (Coffee Lake Refresh) + Skylake-X Refresh HEDT
            Ekle("ICORE", 9, 9, "HEDT", "Intel Core 9. nesil HEDT (Skylake-X Refresh, LGA2066)", "DDR4:2666", 128, 4, true, NM + NK4);
            Ekle("ICORE", 9, 9, "MASAUSTU", "Intel Core i3 9. nesil (Coffee Lake Refresh)", "DDR4:2400", 0, 2, false,
                NM + " Azami kapasite BIOS sürümüne göre 64–128 GB.", siniflar: "3");
            Ekle("ICORE", 9, 9, "MASAUSTU", "Intel Core 9. nesil (Coffee Lake Refresh)", "DDR4:2666", 128, 2, true, NM);
            Ekle("ICORE", 9, 9, "H MOBIL", "Intel Core 9. nesil (Coffee Lake-H Refresh) — H mobil", "DDR4:2666 LPDDR3:2133", 128, 2, true, ND);
            // 10. nesil (Comet Lake / Ice Lake) + Cascade Lake-X HEDT
            Ekle("ICORE", 10, 10, "HEDT", "Intel Core i9 10. nesil HEDT (Cascade Lake-X, LGA2066)", "DDR4:2933", 256, 4, true, NM + NK4);
            Ekle("ICORE", 10, 10, "MASAUSTU", "Intel Core 10. nesil (Comet Lake)", "DDR4:2933", 128, 2, true, NM, siniflar: "79");
            Ekle("ICORE", 10, 10, "MASAUSTU", "Intel Core 10. nesil (Comet Lake)", "DDR4:2666", 128, 2, true, NM);
            Ekle("ICORE", 10, 10, "U", "Intel Core 10. nesil (Comet Lake) — U ince mobil", "DDR4:2666 LPDDR3:2133 LPDDR4:2933", 64, 2, true, ND);
            Ekle("ICORE", 10, 10, "G", "Intel Core 10. nesil (Ice Lake) — ince mobil", "DDR4:3200 LPDDR4X:3733", 64, 2, true, ND);
            Ekle("ICORE", 10, 10, "Y", "Intel Core 10. nesil (Comet Lake / Amber Lake) — Y", "LPDDR3:2133", 0, 2, false, NL);
            Ekle("ICORE", 10, 10, "H MOBIL", "Intel Core 10. nesil (Comet Lake-H) — H mobil", "DDR4:2933", 128, 2, true, ND);
            // 11. nesil (Rocket Lake / Tiger Lake)
            Ekle("ICORE", 11, 11, "MASAUSTU", "Intel Core 11. nesil (Rocket Lake)", "DDR4:3200", 128, 2, true, NM);
            Ekle("ICORE", 11, 11, "G U Y", "Intel Core 11. nesil (Tiger Lake) — ince mobil", "DDR4:3200 LPDDR4X:4267", 64, 2, true, ND);
            Ekle("ICORE", 11, 11, "H", "Intel Core 11. nesil (Tiger Lake H35) — mobil", "DDR4:3200 LPDDR4X:4267", 64, 2, true, ND,
                modelMin: 11300, modelMax: 11399);
            Ekle("ICORE", 11, 11, "H MOBIL", "Intel Core 11. nesil (Tiger Lake-H) — H mobil", "DDR4:3200", 128, 2, true, ND);
            // 12. nesil (Alder Lake)
            Ekle("ICORE", 12, 12, "MASAUSTU", "Intel Core 12. nesil (Alder Lake)", "DDR5:4800 DDR4:3200", 128, 2, true, NM + N45);
            Ekle("ICORE", 12, 12, "U P", "Intel Core 12. nesil (Alder Lake) — U/P ince mobil", "DDR5:4800 DDR4:3200 LPDDR5:5200 LPDDR4X:4267", 64, 2, true, ND);
            Ekle("ICORE", 12, 12, "H", "Intel Core 12. nesil (Alder Lake) — H mobil", "DDR5:4800 DDR4:3200 LPDDR5:5200 LPDDR4X:4267", 64, 2, true, ND);
            Ekle("ICORE", 12, 12, "HX", "Intel Core 12. nesil (Alder Lake) — HX mobil", "DDR5:4800 DDR4:3200", 128, 2, true, ND + N45);
            // 13. / 14. nesil (Raptor Lake / Raptor Lake Refresh)
            for (int g = 13; g <= 14; g++)
            {
                string ad = g == 13 ? "Intel Core 13. nesil (Raptor Lake)" : "Intel Core 14. nesil (Raptor Lake Refresh)";
                Ekle("ICORE", g, g, "MASAUSTU", ad, "DDR5:5600 DDR4:3200", 192, 2, true, NM + N45, siniflar: "79");
                Ekle("ICORE", g, g, "MASAUSTU", ad, "DDR5:5600 DDR4:3200", 192, 2, true, NM + N45, siniflar: "5", sonekler: "K KF KS");
                // i5-14600 / 14600T (K olmayan) DDR5-5600 destekler (Intel ARK); 13600 non-K ise 4800'dür.
                if (g == 14) Ekle("ICORE", g, g, "MASAUSTU", ad, "DDR5:5600 DDR4:3200", 192, 2, true, NM + N45, siniflar: "5", modeller: "14600");
                Ekle("ICORE", g, g, "MASAUSTU", ad, "DDR5:4800 DDR4:3200", 192, 2, false,
                    NM + N45 + " Bazı i5 modellerinde DDR5 azami 5600 MT/s'dir.");
            }
            Ekle("ICORE", 13, 13, "U P H", "Intel Core 13. nesil (Raptor Lake) — mobil", "DDR5:5200 DDR4:3200 LPDDR5X:6400 LPDDR4X:4267", 0, 2, false,
                ND + " Azami kapasite modele göre 64–96 GB.");
            // 13. nesil HX: yalnız i7-13850HX ve i9'lar DDR5-5600; diğer i5/i7'ler DDR5-4800 (Intel ARK, ör. i5-13450HX).
            Ekle("ICORE", 13, 13, "HX", "Intel Core 13. nesil (Raptor Lake) — HX mobil", "DDR5:5600 DDR4:3200", 0, 2, false,
                ND + N45 + " Azami kapasite 128–192 GB (BIOS'a bağlı).", modeller: "13850 13900 13950 13980");
            Ekle("ICORE", 13, 13, "HX", "Intel Core 13. nesil (Raptor Lake) — HX mobil", "DDR5:4800 DDR4:3200", 0, 2, false,
                ND + N45 + " Azami kapasite 128–192 GB (BIOS'a bağlı).");
            Ekle("ICORE", 14, 14, "HX", "Intel Core 14. nesil (Raptor Lake Refresh) — HX mobil", "DDR5:5600 DDR4:3200", 192, 2, true, ND + N45);

            // ===== Intel Core 3/5/7 (Ultra olmayan, 2024+) ============================
            Ekle("CORE_N", 1, 2, "U", "Intel Core {N}. seri (Raptor Lake-U Refresh) — U ince mobil", "DDR5:5200 DDR4:3200 LPDDR5X:6400 LPDDR4X:4267", 0, 2, false, ND);
            Ekle("CORE_N", 1, 2, "H", "Intel Core {N}. seri (Raptor Lake-H Refresh) — H mobil", "DDR5:5200 DDR4:3200 LPDDR5X:6400", 0, 2, false, ND);
            Ekle("CORE_N", 1, 2, "MASAUSTU", "Intel Core {N}. seri masaüstü (Raptor Lake)", "DDR5:4800 DDR4:3200", 0, 2, false, NM + N45);

            // ===== Intel Core Ultra ==================================================
            Ekle("ULTRA", 1, 1, "H U", "Intel Core Ultra 1. seri (Meteor Lake) — mobil", "DDR5:5600 LPDDR5X:7467", 96, 2, true, ND);
            Ekle("ULTRA", 2, 2, "V", "Intel Core Ultra 2. seri (Lunar Lake) — bellek işlemci paketinde", "LPDDR5X:8533", 16, 0, true, NL,
                modeller: "226 236 256 266");
            Ekle("ULTRA", 2, 2, "V", "Intel Core Ultra 2. seri (Lunar Lake) — bellek işlemci paketinde", "LPDDR5X:8533", 32, 0, true, NL);
            Ekle("ULTRA", 2, 2, "MASAUSTU", "Intel Core Ultra 2. seri (Arrow Lake-S) — masaüstü", "DDR5:6400", 192, 2, true,
                NM + " DDR4 desteklemez; CUDIMM modüller de desteklenir. ECC yalnızca W880 yonga setli iş istasyonu anakartlarında.");
            Ekle("ULTRA", 2, 2, "HX", "Intel Core Ultra 2. seri (Arrow Lake-HX) — HX mobil", "DDR5:6400", 0, 2, false, ND);
            Ekle("ULTRA", 2, 2, "H", "Intel Core Ultra 2. seri (Arrow Lake-H) — H mobil", "DDR5:6400 LPDDR5X:8400", 0, 2, false, ND);
            Ekle("ULTRA", 2, 2, "U", "Intel Core Ultra 2. seri (Arrow Lake-U) — U mobil", "DDR5:5600 LPDDR5X:7467", 0, 2, false, ND);
            Ekle("ULTRA", 3, 3, "*", "Intel Core Ultra 3. seri (Panther Lake) — mobil", "LPDDR5X:9600 DDR5:7200", 0, 0, false,
                ND + " Bazı modeller yalnızca lehimli LPDDR5X destekler.");

            // ===== Intel düşük güç / eski ==============================================
            Ekle("INTEL_N", 0, HEP, "*", "Intel N serisi (Alder Lake-N / Twin Lake)", "DDR5:4800 DDR4:3200 LPDDR5:4800", 16, 1, true,
                "Tek kanallı bellek denetleyicisi: ikinci modül hız kazandırmaz. Bu cihazlarda genelde tek yuva vardır ya da bellek lehimlidir.");
            Ekle("ATOM", 0, HEP, "*", "Intel Celeron/Pentium Silver (Jasper Lake)", "DDR4:2933 LPDDR4X:2933", 16, 2, true, ND,
                modeller: "N4500 N4505 N5095 N5100 N5105 N6000 N6005");
            Ekle("ATOM", 0, HEP, "*", "Intel Celeron/Pentium Silver (Gemini Lake)", "DDR4:2400 LPDDR4:2400", 8, 2, true, ND,
                modeller: "N4000 N4020 N4100 N4120 N5000 N5030 J4005 J4025 J4105 J4125 J5005 J5040");
            Ekle("ATOM", 0, HEP, "*", "Intel Celeron/Pentium (Apollo Lake)", "DDR3L:1866 LPDDR3:1866 LPDDR4:2400", 8, 2, true, ND,
                modeller: "N3350 N3450 N4200 J3355 J3455 J4205");
            Ekle("ATOM", 0, HEP, "*", "Intel Celeron/Pentium (Braswell)", "DDR3L:1600", 8, 2, true, ND + N3L,
                modeller: "N3000 N3010 N3050 N3060 N3150 N3160 N3700 N3710 J3060 J3160 J3710");
            Ekle("ATOM", 0, HEP, "*", "Intel Celeron/Pentium (Bay Trail)", "DDR3L:1333", 8, 0, false, ND + N3L,
                modeller: "N2* J1* J2*");
            Ekle("PC_MOBIL", 0, HEP, "*", "Intel Celeron/Pentium (Skylake/Kaby Lake-U) — mobil", "DDR4:2133 LPDDR3:1866 DDR3L:1600", 32, 2, false, ND,
                modeller: "3855 3955 3865 3867 4415 4417");
            Ekle("PC_MOBIL", 0, HEP, "*", "Intel Celeron/Pentium Gold (Whiskey Lake-U) — mobil", "DDR4:2400 LPDDR3:2133", 0, 2, false, ND,
                modeller: "4205 4305 5405");
            Ekle("PC_MOBIL", 0, HEP, "*", "Intel Celeron/Pentium Gold (Comet Lake-U) — mobil", "DDR4:0 LPDDR3:0", 0, 2, false, ND,
                modeller: "5205 6405");
            Ekle("PC_MOBIL", 0, HEP, "*", "Intel Celeron/Pentium Gold (Tiger Lake) — mobil", "DDR4:3200 LPDDR4X:0", 0, 2, false, ND,
                modeller: "6305 7505");
            Ekle("PC_MOBIL", 0, HEP, "*", "Intel Celeron/Pentium Gold (Alder Lake-U) — mobil", "DDR5:4800 DDR4:3200 LPDDR5:5200 LPDDR4X:4267", 0, 2, false, ND,
                modeller: "7305 8505");
            Ekle("CORE2", 0, HEP, "*", "Intel Core 2 / Pentium Dual-Core dönemi", "DDR2:0 DDR3:0", 0, 2, false,
                NC + " Bu dönemin anakartları çoğunlukla DDR2, sonrakiler DDR3 kullanır; genelde 4–8 GB ile sınırlıdır.");

            // ===== Intel Xeon ==========================================================
            Ekle("XEON_W", 0, HEP, "*", "Intel Xeon W-1200 (Comet Lake, LGA1200)", "DDR4:2933", 128, 2, false, NS + " W-1250 modellerinde 2666 MT/s.",
                ecc: true, modelMin: 1200, modelMax: 1299);
            Ekle("XEON_W", 0, HEP, "*", "Intel Xeon W-1300 (Rocket Lake, LGA1200)", "DDR4:3200", 128, 2, true, NS, ecc: true, modelMin: 1300, modelMax: 1399);
            Ekle("XEON_W", 0, HEP, "*", "Intel Xeon W-2100 (Skylake-W, LGA2066)", "DDR4:2666", 512, 4, true, NS + NK4, ecc: true, modelMin: 2100, modelMax: 2199);
            Ekle("XEON_W", 0, HEP, "*", "Intel Xeon W-2200 (Cascade Lake-W, LGA2066)", "DDR4:2933", 1024, 4, true, NS + NK4, ecc: true, modelMin: 2200, modelMax: 2299);
            Ekle("XEON_W", 0, HEP, "*", "Intel Xeon W-3100/3200 (LGA3647)", "DDR4:0", 0, 6, false, NS, ecc: true, modelMin: 3100, modelMax: 3299);
            Ekle("XEON_WX", 0, HEP, "*", "Intel Xeon W-2400 (Sapphire Rapids, LGA4677)", "DDR5:4800", 2048, 4, true, NS + NK4, ecc: true, modelMin: 2400, modelMax: 2499);
            Ekle("XEON_WX", 0, HEP, "*", "Intel Xeon W-3400 (Sapphire Rapids, LGA4677)", "DDR5:4800", 4096, 8, true, NS, ecc: true, modelMin: 3400, modelMax: 3499);
            Ekle("XEON_WX", 0, HEP, "*", "Intel Xeon W (LGA4677)", "DDR5:0", 0, 0, false, NS, ecc: true);
            Ekle("XEON_E3", 1, 1, "*", "Intel Xeon E3-1200 (Sandy Bridge)", "DDR3:1333", 32, 2, true, NS, ecc: true);
            Ekle("XEON_E3", 2, 2, "*", "Intel Xeon E3-1200 v2 (Ivy Bridge)", "DDR3:1600", 32, 2, true, NS, ecc: true);
            Ekle("XEON_E3", 3, 3, "*", "Intel Xeon E3-1200 v3 (Haswell)", "DDR3:1600 DDR3L:1600", 32, 2, true, NS, ecc: true);
            Ekle("XEON_E3", 4, 4, "*", "Intel Xeon E3-1200 v4 (Broadwell)", "DDR3L:0", 32, 2, false, NS, ecc: true);
            Ekle("XEON_E3", 5, 5, "*", "Intel Xeon E3-1200 v5 (Skylake)", "DDR4:2133 DDR3L:1600", 64, 2, true, NS, ecc: true);
            Ekle("XEON_E3", 6, 6, "*", "Intel Xeon E3-1200 v6 (Kaby Lake)", "DDR4:2400 DDR3L:0", 64, 2, false, NS, ecc: true);
            Ekle("XEON_E", 0, HEP, "*", "Intel Xeon E-2100 (Coffee Lake)", "DDR4:2666", 0, 2, false, NS, ecc: true, modelMin: 2100, modelMax: 2199);
            Ekle("XEON_E", 0, HEP, "*", "Intel Xeon E-2200 (Coffee Lake Refresh)", "DDR4:2666", 128, 2, true, NS, ecc: true, modelMin: 2200, modelMax: 2299);
            Ekle("XEON_E", 0, HEP, "*", "Intel Xeon E-2300 (Rocket Lake)", "DDR4:3200", 128, 2, true, NS, ecc: true, modelMin: 2300, modelMax: 2399);
            Ekle("XEON_E", 0, HEP, "*", "Intel Xeon E-2400 (Raptor Lake)", "DDR5:4800", 0, 2, false, NS, ecc: true, modelMin: 2400, modelMax: 2499);
            Ekle("XEON_E5", 1, 2, "*", "Intel Xeon E5/E7 v1–v2 (Sandy/Ivy Bridge-EP)", "DDR3:0", 0, 4, false, NS + NK4, ecc: true);
            Ekle("XEON_E5", 3, 4, "*", "Intel Xeon E5/E7 v3–v4 (Haswell/Broadwell-EP)", "DDR4:0", 0, 4, false, NS + NK4, ecc: true);
            Ekle("XEON_SP", 1, 2, "*", "Intel Xeon Scalable 1.–2. nesil (Skylake/Cascade Lake-SP)", "DDR4:0", 0, 6, false, NS, ecc: true);
            Ekle("XEON_SP", 3, 3, "*", "Intel Xeon Scalable 3. nesil (Ice/Cooper Lake-SP)", "DDR4:0", 0, 8, false, NS, ecc: true);
            Ekle("XEON_SP", 4, 5, "*", "Intel Xeon Scalable 4.–5. nesil (Sapphire/Emerald Rapids)", "DDR5:0", 0, 8, false, NS, ecc: true);

            // ===== AMD Ryzen (anahtar RYZEN; nesil = model/1000) =======================
            Ekle("RYZEN", 1, 1, "MASAUSTU", "AMD Ryzen 1000 masaüstü (Zen, AM4)", "DDR4:2666", 64, 2, true, NM + NE, ecc: true);
            Ekle("RYZEN", 2, 2, "MASAUSTU", "AMD Ryzen 2000 masaüstü (Zen+, AM4)", "DDR4:2933", 64, 2, true, NM + NE, ecc: true);
            Ekle("RYZEN", 2, 2, "APU", "AMD Ryzen 2000G masaüstü APU (Raven Ridge, AM4)", "DDR4:2933", 64, 2, true, NM);
            Ekle("RYZEN", 2, 2, "MOBIL", "AMD Ryzen 2000 mobil (Raven Ridge)", "DDR4:2400", 0, 2, false, ND);
            Ekle("RYZEN", 3, 3, "MASAUSTU", "AMD Ryzen 3000 masaüstü (Zen 2, AM4)", "DDR4:3200", 128, 2, true, NM + NE, ecc: true);
            Ekle("RYZEN", 3, 3, "APU", "AMD Ryzen 3000G masaüstü APU (Picasso, AM4)", "DDR4:2933", 0, 2, false, NM);
            Ekle("RYZEN", 3, 3, "MOBIL", "AMD Ryzen 3000 mobil (Picasso/Dali)", "DDR4:2400", 0, 2, false, ND);
            Ekle("RYZEN", 4, 4, "MASAUSTU APU", "AMD Ryzen 4000 masaüstü (Renoir, AM4)", "DDR4:3200", 0, 2, false, NM);
            Ekle("RYZEN", 4, 4, "MOBIL", "AMD Ryzen 4000 mobil (Renoir, Zen 2)", "DDR4:3200 LPDDR4X:4266", 0, 2, false, ND);
            Ekle("RYZEN", 5, 5, "MASAUSTU", "AMD Ryzen 5000 masaüstü (Zen 3, AM4)", "DDR4:3200", 128, 2, true, NM + NE, ecc: true);
            Ekle("RYZEN", 5, 5, "APU", "AMD Ryzen 5000G masaüstü APU (Cezanne, AM4)", "DDR4:3200", 0, 2, false, NM);
            Ekle("RYZEN", 5, 5, "MOBIL", "AMD Ryzen 5000 mobil (Lucienne/Cezanne/Barcelo)", "DDR4:3200 LPDDR4X:4266", 0, 2, false, ND);
            Ekle("RYZEN", 6, 6, "MOBIL", "AMD Ryzen 6000 mobil (Rembrandt, Zen 3+)", "DDR5:4800 LPDDR5:6400", 0, 2, false, ND);
            Ekle("RYZEN", 7, 7, "MASAUSTU", "AMD Ryzen 7000 masaüstü (Zen 4, AM5)", "DDR5:5200", 128, 2, true,
                NM + " DDR4 desteklemez. Güncel BIOS ile 48 GB'lık modüller kullanılarak daha yükseğe çıkılabilir." + NE, ecc: true);
            Ekle("RYZEN", 7, 7, "MOBIL", "AMD Ryzen 7020 mobil (Mendocino, Zen 2)", "LPDDR5:5500", 0, 0, false, NL, altlar: "20 25");
            Ekle("RYZEN", 7, 7, "MOBIL", "AMD Ryzen 7030 mobil (Barcelo-R, Zen 3)", "DDR4:3200 LPDDR4X:4266", 0, 2, false, ND, altlar: "30");
            Ekle("RYZEN", 7, 7, "MOBIL", "AMD Ryzen 7035 mobil (Rembrandt-R, Zen 3+)", "DDR5:4800 LPDDR5:6400", 0, 2, false, ND, altlar: "35");
            Ekle("RYZEN", 7, 7, "MOBIL", "AMD Ryzen 7045 HX mobil (Dragon Range, Zen 4)", "DDR5:5200", 0, 2, false, ND, altlar: "45", sonekler: "HX");
            Ekle("RYZEN", 7, 7, "MOBIL", "AMD Ryzen 7040 mobil (Phoenix, Zen 4)", "DDR5:5600 LPDDR5X:7500", 0, 2, false, ND, altlar: "40 45");
            Ekle("RYZEN", 8, 8, "MASAUSTU APU", "AMD Ryzen 8000 masaüstü (Phoenix, AM5)", "DDR5:5200", 0, 2, false, NM + " DDR4 desteklemez.");
            Ekle("RYZEN", 8, 8, "MOBIL", "AMD Ryzen 8000 HX mobil (Dragon Range Refresh, Zen 4)", "DDR5:5200", 0, 2, false, ND, sonekler: "HX");
            Ekle("RYZEN", 8, 8, "MOBIL", "AMD Ryzen 8040 mobil (Hawk Point, Zen 4)", "DDR5:5600 LPDDR5X:7500", 0, 2, false, ND);
            Ekle("RYZEN", 9, 9, "MASAUSTU", "AMD Ryzen 9000 masaüstü (Zen 5, AM5)", "DDR5:5600", 192, 2, true, NM + " DDR4 desteklemez." + NE, ecc: true);
            Ekle("RYZEN", 9, 9, "MOBIL", "AMD Ryzen 9000 HX mobil (Fire Range, Zen 5)", "DDR5:5600", 0, 2, false, ND);
            Ekle("RYZEN_200", 2, 2, "*", "AMD Ryzen 200 serisi mobil (Hawk Point)", "DDR5:5600 LPDDR5X:7500", 0, 2, false, ND);
            Ekle("RYZEN_AI", 0, HEP, "MAX", "AMD Ryzen AI Max (Strix Halo)", "LPDDR5X:8000", 128, 0, true, NL);
            Ekle("RYZEN_AI", 3, 3, "MOBIL", "AMD Ryzen AI 300 (Krackan Point, Zen 5)", "DDR5:5600 LPDDR5X:8000", 0, 2, false, ND,
                modeller: "330 340 350");
            Ekle("RYZEN_AI", 3, 3, "MOBIL", "AMD Ryzen AI 300 (Strix Point, Zen 5)", "DDR5:5600 LPDDR5X:7500", 0, 2, false, ND);
            Ekle("RYZEN_AI", 4, 4, "MOBIL", "AMD Ryzen AI 400 (Gorgon Point, Zen 5)", "DDR5:5600 LPDDR5X:8533", 0, 2, false, ND);
            Ekle("RYZEN_AI", 2, 2, "MOBIL", "AMD Ryzen AI 200 (Hawk Point)", "DDR5:5600 LPDDR5X:7500", 0, 2, false, ND);
            Ekle("RYZEN_Z", 0, HEP, "*", "AMD Ryzen Z (el konsolu)", "LPDDR5:0 LPDDR5X:0", 0, 0, false, NL);
            Ekle("TR", 1, 1, "HEDT", "AMD Ryzen Threadripper 1000 (TR4)", "DDR4:2666", 0, 4, false, NS + NK4, ecc: true);
            Ekle("TR", 2, 2, "HEDT WX", "AMD Ryzen Threadripper 2000 (TR4)", "DDR4:2933", 0, 4, false, NS + NK4, ecc: true);
            Ekle("TR", 3, 3, "HEDT", "AMD Ryzen Threadripper 3000 (sTRX4)", "DDR4:3200", 0, 4, false, NS + NK4, ecc: true);
            Ekle("TR", 3, 5, "WX", "AMD Ryzen Threadripper PRO 3000WX/5000WX (sWRX8)", "DDR4:3200", 0, 8, false, NS, ecc: true);
            Ekle("TR", 7, 7, "HEDT", "AMD Ryzen Threadripper 7000 (sTR5)", "DDR5:5200", 0, 4, false, NS + " Registered (RDIMM) DDR5 gerekir." + NK4, ecc: true);
            Ekle("TR", 7, 7, "WX", "AMD Ryzen Threadripper PRO 7000WX (sTR5)", "DDR5:5200", 0, 8, false, NS + " Registered (RDIMM) DDR5 gerekir.", ecc: true);
            Ekle("TR", 9, 9, "HEDT WX", "AMD Ryzen Threadripper 9000 (sTR5)", "DDR5:6400", 0, 0, false, NS + " Registered (RDIMM) DDR5 gerekir.", ecc: true);
            Ekle("ATHLON", 0, HEP, "APU MASAUSTU", "AMD Athlon masaüstü (AM4)", "DDR4:2666", 0, 2, false, NM);
            Ekle("ATHLON", 7, 7, "MOBIL", "AMD Athlon mobil (Mendocino)", "LPDDR5:5500", 0, 0, false, NL, altlar: "20");
            Ekle("ATHLON", 0, HEP, "MOBIL", "AMD Athlon mobil", "DDR4:2400", 0, 2, false, ND);
            Ekle("FX", 0, HEP, "*", "AMD FX (AM3+)", "DDR3:1866", 0, 2, false, NM);
            Ekle("AMD_A", 9, 9, "*", "AMD A serisi 7. nesil (Bristol/Stoney Ridge)", "DDR4:0", 0, 2, false, NM);
            Ekle("AMD_A", 0, 8, "*", "AMD A serisi (DDR3 dönemi)", "DDR3:0", 0, 2, false, NM);

            // ===== Qualcomm (Windows on ARM) ===========================================
            Ekle("SNAPDRAGON", 1, 1, "*", "Qualcomm Snapdragon X (Oryon)", "LPDDR5X:8448", 64, 0, true, NL);
            Ekle("SNAPDRAGON", 2, 2, "*", "Qualcomm Snapdragon X2 (Oryon)", "LPDDR5X:0", 0, 0, false, NL);
        }
    }
}
