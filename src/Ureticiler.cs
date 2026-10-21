// RAM Danışmanı — bellek üreticisi adlarının normalleştirilmesi ve parça
// numarası çözümlemesi. C# 5 (yerleşik csc.exe) ile derlenir.
//
// Üretici alanı SMBIOS/WMI'da çok farklı biçimlerde gelir:
//   "Samsung", "Samsung Electronics", "80CE", "CE00000000000000", "00CE",
//   "0x80CE", "80AD000080AD", "7F7F7F7F7F9B" ...
// JEDEC JEP106 kimliği = (devam kodu sayısı, üretici kodu). Kod baytının 7. biti
// tek-parite bitidir; "banka baytı" ise 0x80 | devam sayısı biçiminde (yine
// parite bitli) ya da paritesiz gelebilir.
//
// Donanım metinleri HER ZAMAN ToUpperInvariant ile büyütülür (Türkçe kültürde
// "i".ToUpper() = "İ" olur ve karşılaştırmalar bozulur).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RamDanismani
{
    public static class Ureticiler
    {
        // ---------------------------------------------------------------------
        // JEP106 tablosu — anahtar: devamKoduSayisi * 256 + kod (parite bitli).
        // Yalnızca emin olunan bellek üreticisi kodları; tahmin yok.
        // ---------------------------------------------------------------------
        static readonly Dictionary<int, string> jedecTablosu = JedecTablosuKur();

        static Dictionary<int, string> JedecTablosuKur()
        {
            Dictionary<int, string> d = new Dictionary<int, string>();
            // Banka 1 (devam kodu yok)
            d[0 * 256 + 0x01] = "AMD";
            d[0 * 256 + 0x04] = "Fujitsu";
            d[0 * 256 + 0x07] = "Hitachi";
            d[0 * 256 + 0x10] = "NEC";
            d[0 * 256 + 0x1C] = "Mitsubishi";
            d[0 * 256 + 0x2C] = "Micron";
            d[0 * 256 + 0x89] = "Intel";
            d[0 * 256 + 0x98] = "Toshiba";
            d[0 * 256 + 0xA4] = "IBM";
            d[0 * 256 + 0xAD] = "SK hynix";
            d[0 * 256 + 0xC1] = "Infineon";
            d[0 * 256 + 0xCE] = "Samsung";
            d[0 * 256 + 0xDA] = "Winbond";
            d[0 * 256 + 0xFE] = "Elpida";
            // Banka 2 (1 devam kodu)
            d[1 * 256 + 0x4F] = "Transcend";
            d[1 * 256 + 0x7A] = "Apacer";
            d[1 * 256 + 0x94] = "Smart Modular";
            d[1 * 256 + 0x98] = "Kingston";
            // Banka 3
            d[2 * 256 + 0x9E] = "Corsair";
            // Banka 4
            d[3 * 256 + 0x0B] = "Nanya";
            // Banka 5
            d[4 * 256 + 0x43] = "Ramaxel";
            d[4 * 256 + 0xCB] = "ADATA";
            d[4 * 256 + 0xCD] = "G.Skill";
            d[4 * 256 + 0xEF] = "Team Group";
            // Banka 6
            d[5 * 256 + 0x51] = "Qimonda";
            d[5 * 256 + 0x9B] = "Crucial";
            return d;
        }

        // ---------------------------------------------------------------------
        // Metin takma adları. Sıra önemli: özel olan önce.
        // "icerir" = büyük harfli metinde geçerse; "kelime" = ayrı bir sözcük olarak geçerse
        // (HP, PNY, ATP gibi kısa adlar başka sözcüklerin içinde yanlış eşleşmesin).
        // ---------------------------------------------------------------------
        class TakmaAd
        {
            public string Ad;
            public string[] Icerir;
            public string[] Kelime;
            public TakmaAd(string ad, string[] icerir, string[] kelime) { Ad = ad; Icerir = icerir; Kelime = kelime; }
        }

        static readonly List<TakmaAd> takmaAdlar = TakmaAdlarKur();

        static List<TakmaAd> TakmaAdlarKur()
        {
            List<TakmaAd> l = new List<TakmaAd>();
            string[] bos = new string[0];
            l.Add(new TakmaAd("Crucial", new string[] { "CRUCIAL", "BALLISTIX" }, bos));
            l.Add(new TakmaAd("Micron", new string[] { "MICRON" }, bos));
            l.Add(new TakmaAd("Samsung", new string[] { "SAMSUNG" }, bos));
            l.Add(new TakmaAd("SK hynix", new string[] { "HYNIX", "HYUNDAI" }, bos));
            l.Add(new TakmaAd("Kingston", new string[] { "KINGSTON", "HYPERX" }, bos));
            l.Add(new TakmaAd("Kingmax", new string[] { "KINGMAX" }, bos));
            l.Add(new TakmaAd("KingBank", new string[] { "KINGBANK" }, bos));
            l.Add(new TakmaAd("Corsair", new string[] { "CORSAIR" }, bos));
            l.Add(new TakmaAd("G.Skill", new string[] { "G.SKILL", "GSKILL", "G SKILL", "G-SKILL" }, bos));
            l.Add(new TakmaAd("ADATA", new string[] { "ADATA", "A-DATA", "A DATA" }, new string[] { "XPG" }));
            l.Add(new TakmaAd("Team Group", new string[] { "TEAMGROUP", "TEAM GROUP", "T-FORCE", "TFORCE" }, new string[] { "TEAM" }));
            l.Add(new TakmaAd("Patriot", new string[] { "PATRIOT" }, bos));
            l.Add(new TakmaAd("Transcend", new string[] { "TRANSCEND" }, bos));
            l.Add(new TakmaAd("Nanya", new string[] { "NANYA" }, bos));
            l.Add(new TakmaAd("Ramaxel", new string[] { "RAMAXEL" }, bos));
            l.Add(new TakmaAd("Smart Modular", new string[] { "SMART MODULAR", "SMARTMODULAR", "SMART MODUL" }, bos));
            l.Add(new TakmaAd("Apacer", new string[] { "APACER" }, bos));
            l.Add(new TakmaAd("PNY", new string[] { "PNY TECH", "PNY ELEC" }, new string[] { "PNY" }));
            l.Add(new TakmaAd("Lexar", new string[] { "LEXAR" }, bos));
            l.Add(new TakmaAd("Netac", new string[] { "NETAC" }, bos));
            l.Add(new TakmaAd("Elpida", new string[] { "ELPIDA" }, bos));
            l.Add(new TakmaAd("Qimonda", new string[] { "QIMONDA" }, bos));
            l.Add(new TakmaAd("Infineon", new string[] { "INFINEON" }, bos));
            l.Add(new TakmaAd("HP", new string[] { "HEWLETT" }, new string[] { "HP", "HPE" }));
            l.Add(new TakmaAd("Dell", new string[0], new string[] { "DELL" }));
            l.Add(new TakmaAd("Lenovo", new string[] { "LENOVO" }, bos));
            l.Add(new TakmaAd("Fujitsu", new string[] { "FUJITSU" }, bos));
            l.Add(new TakmaAd("Mushkin", new string[] { "MUSHKIN" }, bos));
            l.Add(new TakmaAd("GeIL", new string[] { "GOLDEN EMPIRE" }, new string[] { "GEIL" }));
            l.Add(new TakmaAd("Silicon Power", new string[] { "SILICON POWER", "SILICON-POWER", "SILICONPOWER" }, bos));
            l.Add(new TakmaAd("Hikvision", new string[] { "HIKVISION", "HIKSEMI" }, bos));
            l.Add(new TakmaAd("OCZ", new string[0], new string[] { "OCZ" }));
            l.Add(new TakmaAd("GOODRAM", new string[] { "GOODRAM" }, new string[] { "WILK" }));
            l.Add(new TakmaAd("Unifosa", new string[] { "UNIFOSA" }, bos));
            l.Add(new TakmaAd("Avant", new string[0], new string[] { "AVANT" }));
            l.Add(new TakmaAd("Viking", new string[] { "VIKING" }, bos));
            l.Add(new TakmaAd("Netlist", new string[] { "NETLIST" }, bos));
            l.Add(new TakmaAd("Innodisk", new string[] { "INNODISK" }, bos));
            l.Add(new TakmaAd("ATP", new string[0], new string[] { "ATP" }));
            l.Add(new TakmaAd("Swissbit", new string[] { "SWISSBIT" }, bos));
            l.Add(new TakmaAd("Integral", new string[] { "INTEGRAL MEMORY" }, new string[] { "INTEGRAL" }));
            l.Add(new TakmaAd("Intel", new string[0], new string[] { "INTEL" }));
            l.Add(new TakmaAd("Toshiba", new string[] { "TOSHIBA" }, bos));
            l.Add(new TakmaAd("Winbond", new string[] { "WINBOND" }, bos));
            l.Add(new TakmaAd("CXMT", new string[] { "CXMT", "CHANGXIN" }, bos));
            l.Add(new TakmaAd("Longsys", new string[] { "LONGSYS" }, bos));
            l.Add(new TakmaAd("Gloway", new string[] { "GLOWAY" }, bos));
            l.Add(new TakmaAd("Asgard", new string[] { "ASGARD" }, bos));
            l.Add(new TakmaAd("V-Color", new string[] { "V-COLOR", "VCOLOR" }, bos));
            l.Add(new TakmaAd("Thermaltake", new string[] { "THERMALTAKE" }, bos));
            l.Add(new TakmaAd("Biwin", new string[] { "BIWIN" }, bos));
            l.Add(new TakmaAd("Gigabyte", new string[] { "GIGABYTE", "AORUS" }, bos));
            l.Add(new TakmaAd("Super Talent", new string[] { "SUPER TALENT", "SUPERTALENT" }, bos));
            l.Add(new TakmaAd("TwinMOS", new string[] { "TWINMOS" }, bos));
            l.Add(new TakmaAd("Wintec", new string[] { "WINTEC" }, bos));
            l.Add(new TakmaAd("Panram", new string[] { "PANRAM" }, bos));
            l.Add(new TakmaAd("Acer", new string[0], new string[] { "ACER" }));
            l.Add(new TakmaAd("AMD", new string[0], new string[] { "AMD" }));
            return l;
        }

        static readonly Regex rxAnlamsiz = new Regex(
            @"^(?:(?:MODULE|MEMORY|DIMM|DRAM)\s*)?(?:MANUFACTURER|MFG|MFR)\s*[#_]?\s*\d*$",
            RegexOptions.CultureInvariant);
        static readonly Regex rxSifirF = new Regex(@"^(?:0X)?(?:0+|F+)$", RegexOptions.CultureInvariant);
        static readonly Regex rxHex = new Regex(@"^[0-9A-F]+$", RegexOptions.CultureInvariant);

        static readonly string[] anlamsizMetinler = new string[] {
            "", "-", "--", "?", "NONE", "N/A", "NA", "NULL", "EMPTY", "[EMPTY]", "NO DIMM", "NODIMM",
            "NOT SPECIFIED", "NOTSPECIFIED", "NOT AVAILABLE", "NOT INSTALLED", "DEFAULT STRING",
            "TO BE FILLED BY O.E.M.", "TO BE FILLED BY OEM", "OEM", "O.E.M.", "SYSTEM MANUFACTURER",
            "SERIALNUM", "PARTNUM", "MANUFACTURER", "VENDOR", "GENERIC"
        };

        // ---------------------------------------------------------------------
        // Normallestir
        // ---------------------------------------------------------------------

        /// <summary>
        /// Üretici alanını tanınmış marka adına çevirir ("80CE" → "Samsung").
        /// Anlamsız değerler → "". Tanınmayan → Trim edilmiş ham metin.
        /// jedecId = SMBIOS 3.2 Module Manufacturer ID (düşük bayt: devam kodu sayısı
        /// + parite, yüksek bayt: üretici kodu); 0 = yok.
        /// </summary>
        public static string Normallestir(string ham, int jedecId)
        {
            try
            {
                string s = Temizle(ham);
                bool anlamsiz = Anlamsiz(s);
                if (!anlamsiz)
                {
                    string ad = MetindenBul(s);
                    if (ad != null) return ad;
                    ad = HexBul(s);
                    if (ad != null) return ad;
                }
                string j = JedecIdCoz(jedecId);
                if (j != null) return j;
                if (anlamsiz) return "";
                return s;
            }
            catch
            {
                return ham == null ? "" : ham.Trim();
            }
        }

        static string Temizle(string ham)
        {
            if (ham == null) return "";
            StringBuilder sb = new StringBuilder(ham.Length);
            bool bosluk = false;
            foreach (char c in ham)
            {
                if (c == '\0' || char.IsWhiteSpace(c) || char.IsControl(c))
                {
                    bosluk = true;
                    continue;
                }
                if (bosluk && sb.Length > 0) sb.Append(' ');
                bosluk = false;
                sb.Append(c);
            }
            return sb.ToString();
        }

        static bool Anlamsiz(string s)
        {
            string b = s.ToUpperInvariant().Trim();
            if (b.Length == 0) return true;
            foreach (string a in anlamsizMetinler)
                if (string.Equals(a, b, StringComparison.Ordinal)) return true;
            if (b.StartsWith("UNKNOWN", StringComparison.Ordinal)) return true;
            if (b.StartsWith("UNDEFINED", StringComparison.Ordinal)) return true;
            if (rxSifirF.IsMatch(b.Replace(" ", ""))) return true;
            if (rxAnlamsiz.IsMatch(b)) return true;
            return false;
        }

        static string MetindenBul(string s)
        {
            string b = s.ToUpperInvariant();
            List<string> kelimeler = new List<string>();
            foreach (string k in b.Split(new char[] { ' ', '.', ',', '-', '_', '(', ')', '/', '[', ']', ';', ':', '&' }, StringSplitOptions.RemoveEmptyEntries))
                kelimeler.Add(k);
            foreach (TakmaAd t in takmaAdlar)
            {
                foreach (string i in t.Icerir)
                    if (b.IndexOf(i, StringComparison.Ordinal) >= 0) return t.Ad;
                foreach (string k in t.Kelime)
                    if (kelimeler.Contains(k)) return t.Ad;
            }
            return null;
        }

        static int Parite(int b7) // 7 bitlik değere tek-parite bitini ekler
        {
            int x = b7 & 0x7F;
            int say = 0;
            for (int i = 0; i < 7; i++) if (((x >> i) & 1) != 0) say++;
            return (say % 2 == 0) ? (x | 0x80) : x;
        }

        /// <summary>(devam sayısı, kod baytı) → ad. Kod baytı paritesiz gelmişse onarılır,
        /// parite biti var ama yanlışsa reddedilir.</summary>
        static string JedecAra(int devam, int kod)
        {
            if (devam < 0 || devam > 15) return null;
            kod &= 0xFF;
            if (kod == 0 || kod == 0x7F || kod == 0xFF || kod == 0x80) return null;
            int duzgun = Parite(kod);
            if ((kod & 0x80) != 0 && duzgun != kod) return null;
            string ad;
            if (jedecTablosu.TryGetValue(devam * 256 + duzgun, out ad)) return ad;
            return null;
        }

        /// <summary>Banka baytı: 0x80|n (parite) ya da n. Parite yanlışsa da kabul edilir (bazı BIOS'lar).</summary>
        static int BankaBayti(int b)
        {
            int n = b & 0x7F;
            if (n > 15) return -1;
            return n;
        }

        static string HexBul(string s)
        {
            string b = s.ToUpperInvariant().Replace(" ", "");
            if (b.StartsWith("0X", StringComparison.Ordinal)) b = b.Substring(2);
            if (b.Length < 2 || b.Length > 32 || b.Length % 2 != 0 || !rxHex.IsMatch(b)) return null;
            byte[] y = new byte[b.Length / 2];
            for (int i = 0; i < y.Length; i++)
                y[i] = byte.Parse(b.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            string ad;
            // 1) SPD tarzı: 7F 7F ... kod
            int n = 0;
            while (n < y.Length && y[n] == 0x7F) n++;
            if (n > 0 && n < y.Length)
            {
                ad = JedecAra(n, y[n]);
                if (ad != null) return ad;
            }
            if (y.Length == 1) return JedecAra(0, y[0]);
            // 2) Önce banka, sonra kod: "80CE", "0198", "859B", "00CE"
            int banka = BankaBayti(y[0]);
            if (banka >= 0)
            {
                ad = JedecAra(banka, y[1]);
                if (ad != null) return ad;
            }
            // 3) Önce kod, sonra banka: "CE00000000000000", "9801", "2C00"
            banka = BankaBayti(y[1]);
            if (banka >= 0)
            {
                ad = JedecAra(banka, y[0]);
                if (ad != null) return ad;
            }
            return null;
        }

        static string JedecIdCoz(int jedecId)
        {
            if (jedecId <= 0 || jedecId > 0xFFFF) return null;
            int dusuk = jedecId & 0xFF, yuksek = (jedecId >> 8) & 0xFF;
            int banka = BankaBayti(dusuk);
            string ad = null;
            if (banka >= 0) ad = JedecAra(banka, yuksek);
            if (ad == null)
            {
                // bazı firmware'ler baytları ters yazar
                banka = BankaBayti(yuksek);
                if (banka >= 0) ad = JedecAra(banka, dusuk);
            }
            return ad;
        }

        // ---------------------------------------------------------------------
        // Parça numarası çözümleme
        // ---------------------------------------------------------------------

        class CozumEki
        {
            public int Kit = 1;
            public List<string> Notlar = new List<string>();
        }

        const RegexOptions RO = RegexOptions.CultureInvariant;

        // Kingston
        static readonly Regex rxKvr = new Regex(@"^KVR(\d{2,4})(D[234])?(L)?([NSUER])(\d{1,2})([A-Z0-9]*?)(?:K(\d))?[/-](\d+)G?", RO);
        static readonly Regex rxKf = new Regex(@"^(KF|HX)(\d)(\d{2})(L)?([CS])(\d{2})([A-Z0-9]*?)(?:K(\d))?[/-](\d+)G?", RO);
        // SPD'ye yazılan biçim: KF3200C16D4/8GX, KF3600C17D4/16GX (form faktörü belirsiz)
        static readonly Regex rxKfSpd = new Regex(@"^(KF|HX)(\d{4})C(\d{2})D([345])/(\d+)GX?", RO);
        static readonly Regex rxKhx = new Regex(@"^KHX(\d{4})C(\d{1,2})([A-Z0-9]*?)(?:K(\d))?/(\d+)G?", RO);
        static readonly Regex rxKcp = new Regex(@"^KCP(\d)(L)?(\d{2})([NSUER])([A-Z0-9]*?)(?:K(\d))?[/-](\d+)G?", RO);
        static readonly Regex rxKsm = new Regex(@"^KSM(\d{2})([A-Z]{1,2})([A-Z0-9]*?)(?:K(\d))?[/-](\d+)G?", RO);
        // Samsung / SK hynix / Micron
        static readonly Regex rxSamsung = new Regex(@"^M(471|378|391|393|386|474|425|323|321|324)([ABR])([0-9A-Z]{2})", RO);
        static readonly Regex rxHynix = new Regex(@"^HM([AT])([0-9A-Z])([0-9A-Z]{2})([SUR])(\d)", RO);
        static readonly Regex rxHynix5 = new Regex(@"^HMC[GT][0-9A-Z]{6}", RO);
        static readonly Regex rxMicron4 = new Regex(@"^MTA(\d+)A[A-Z]F(\d+)(G?)(64|72)([A-Z])[A-Z]*-(\dG\d)", RO);
        static readonly Regex rxMicron3 = new Regex(@"^MT(\d+)([JK])[A-Z]F(\d+)(G?)(64|72)([A-Z])[A-Z]*-(1G\d)", RO);
        static readonly Regex rxMicron5 = new Regex(@"^MTC(\d+)C([12])(04|08|016)(\d)S\d([SUR])C(\d{2})", RO);
        // Crucial
        static readonly Regex rxCrucial5 = new Regex(@"^C([TP])(?:(\d+)K)?(\d+)G(\d{2})C(\d{2})([SU])5", RO);
        static readonly Regex rxCrucial4 = new Regex(@"^C([TP])(?:(\d+)K)?(\d+)G4([SDWR])F([A-Z0-9]{2})(\d{2,3})", RO);
        static readonly Regex rxBallistix = new Regex(@"^BL(?:(\d+)K)?(\d+)G(\d{2})C(\d{2})([US])4", RO);
        // Corsair / G.Skill / ADATA / Team / Patriot / diğerleri
        static readonly Regex rxCorsair = new Regex(@"^CM([A-Z]{1,4}?)(\d+)GX([345])M(\d)([A-Z])(\d{4})C(\d{2})", RO);
        static readonly Regex rxGskill = new Regex(@"^F([34])-(\d{4})C(\d{1,2})([SDQO])?-(\d+)G([A-Z0-9]*)", RO);
        static readonly Regex rxGskill5 = new Regex(@"^F5-(\d{4})([A-Z])(\d{2})(\d{2})([A-Z])(\d+)G(?:X(\d))?(?:-([A-Z0-9]+))?", RO);
        static readonly Regex rxAdata = new Regex(@"^A([DX])([345])([SU])(\d{4})(?:C(\d{2}))?(\d+)G(\d{2})?", RO);
        static readonly Regex rxTeam = new Regex(@"^T([A-Z]{1,4}?)D?([345])(\d+)G(\d{4})H?C(\d{2})(.*)$", RO);
        static readonly Regex rxPatriotSig = new Regex(@"^PSD([345])(\d+)G(\d{4})(\d*)([A-Z]?)", RO);
        static readonly Regex rxPatriotViper = new Regex(@"^PV([A-Z0-9]{0,2}?)([345])(\d+)G(\d{3})C(\d{1,2})(Q)?(K)?", RO);
        static readonly Regex rxLexar = new Regex(@"^LD([45])[A-Z]([SU])0*(\d+)G-[A-Z](\d{4})", RO);
        static readonly Regex rxRamaxel = new Regex(@"^RM([SU])A\d[0-9A-Z]*-(\d{4})", RO);
        static readonly Regex rxJetram = new Regex(@"^JM(\d{4})H([SL])[A-Z]-(\d+)G", RO);

        /// <summary>
        /// Parça numarasından marka/seri/kapasite/tür/hız/form/CL çıkarır.
        /// Kit gösteriminde KapasiteGB modül başınadır, Aciklama'da "2'li kit" yazar.
        /// Çözemezse Bulundu=false, Aciklama="". Asla istisna fırlatmaz.
        /// </summary>
        public static ParcaCozumu ParcaNoCoz(string parcaNo, string uretici)
        {
            try
            {
                string marka = Normallestir(uretici, 0);
                List<string> adaylar = new List<string>();
                string p = ParcaHazirla(parcaNo);
                if (p.Length >= 4) adaylar.Add(p);
                string h = HexMetinCoz(p);
                if (h != null)
                {
                    string ph = ParcaHazirla(h);
                    if (ph.Length >= 4) adaylar.Add(ph);
                }
                foreach (string a in adaylar)
                {
                    ParcaCozumu c = new ParcaCozumu();
                    CozumEki e = new CozumEki();
                    if (Coz(a, marka, c, e))
                    {
                        c.Bulundu = true;
                        if (c.Aciklama.Length == 0) c.Aciklama = AciklamaYap(c, e);
                        return c;
                    }
                }
            }
            catch { }
            return new ParcaCozumu();
        }

        static bool Coz(string p, string marka, ParcaCozumu c, CozumEki e)
        {
            return Kingston(p, c, e)
                || Samsung(p, c, e)
                || Hynix(p, c, e)
                || Micron(p, c, e)
                || Crucial(p, c, e)
                || Corsair(p, c, e)
                || GSkill(p, c, e)
                || Adata(p, c, e)
                || Team(p, marka, c, e)
                || Patriot(p, c, e)
                || Digerleri(p, c, e);
        }

        static string ParcaHazirla(string parcaNo)
        {
            string s = Temizle(parcaNo).ToUpperInvariant();
            if (s.Length == 0 || Anlamsiz(s)) return "";
            int bosluk = s.IndexOf(' ');
            if (bosluk >= 5) s = s.Substring(0, bosluk);
            else s = s.Replace(" ", "");
            return s;
        }

        /// <summary>Bazı BIOS'lar parça numarasını ASCII'nin onaltılık dökümü olarak verir.</summary>
        static string HexMetinCoz(string p)
        {
            string b = p;
            bool onekli = b.StartsWith("0X", StringComparison.Ordinal);
            if (onekli) b = b.Substring(2);
            if (b.Length < 8 || b.Length % 2 != 0 || !rxHex.IsMatch(b)) return null;
            if (!onekli && b.Length < 16) return null;
            StringBuilder sb = new StringBuilder();
            int harf = 0;
            for (int i = 0; i < b.Length; i += 2)
            {
                int v = int.Parse(b.Substring(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (v == 0) continue;
                if (v < 0x20 || v > 0x7E) return null;
                if (char.IsLetter((char)v)) harf++;
                sb.Append((char)v);
            }
            if (harf < 2) return null;
            return sb.ToString();
        }

        // ---- yardımcılar ----------------------------------------------------

        static int Sayi(string s)
        {
            int n;
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) return n;
            return 0;
        }

        /// <summary>İki haneli hız kodu (Kingston, Crucial, Micron DDR5...) → MT/s. Bilinmeyen → 0.</summary>
        static int Hiz2(int n)
        {
            switch (n)
            {
                case 8: return 800;
                case 10: return 1066;
                case 13: return 1333;
                case 16: return 1600;
                case 18: return 1866;
                case 21: return 2133;
                case 24: return 2400;
                case 26: return 2666;
                case 29: return 2933;
                case 30: return 3000;
                case 32: return 3200;
                case 33: return 3333;
                case 34: return 3466;
                case 36: return 3600;
                case 37: return 3733;
                case 40: return 4000;
                case 44: return 4400;
                case 46: return 4600;
                case 48: return 4800;
                case 52: return 5200;
                case 56: return 5600;
                case 60: return 6000;
                case 62: return 6200;
                case 64: return 6400;
                case 66: return 6600;
                case 68: return 6800;
                case 72: return 7200;
                case 76: return 7600;
                case 80: return 8000;
                case 82: return 8200;
                case 84: return 8400;
            }
            return 0;
        }

        /// <summary>Üç haneli hız kodu (Crucial "266", Patriot "320") → MT/s.</summary>
        static int Hiz3(int n)
        {
            switch (n)
            {
                case 106: return 1066;
                case 133: return 1333;
                case 186: return 1866;
                case 213: return 2133;
                case 266: return 2666;
                case 293: return 2933;
                case 333: return 3333;
                case 346: return 3466;
                case 373: return 3733;
                case 413: return 4133;
                case 426: return 4266;
            }
            if (n >= 80 && n <= 999 && n % 10 == 0) return n * 10;
            return 0;
        }

        static string TurHizdan(int hiz)
        {
            if (hiz >= 4400) return "DDR5";
            if (hiz >= 2133) return "DDR4";
            if (hiz >= 1066) return "DDR3";
            return "";
        }

        static string TurNesilden(string nesil)
        {
            if (nesil == "3") return "DDR3";
            if (nesil == "4") return "DDR4";
            if (nesil == "5") return "DDR5";
            return "";
        }

        /// <summary>Derinlik kodu (M471A"2K"43, HMA8"1G"S6 ...) → GB (64 bit modül).</summary>
        static int DerinlikGB(string kod)
        {
            switch (kod)
            {
                case "51": case "52": return 4;
                case "1G": case "1K": return 8;
                case "2G": case "2K": return 16;
                case "4G": case "4K": return 32;
                case "8G": case "8K": return 64;
            }
            return 0;
        }

        static void Kapasite(ParcaCozumu c, CozumEki e, int toplamGB, int kit)
        {
            if (kit > 1)
            {
                e.Kit = kit;
                c.KapasiteGB = (toplamGB > 0 && toplamGB % kit == 0) ? toplamGB / kit : 0;
            }
            else c.KapasiteGB = toplamGB;
        }

        static string AciklamaYap(ParcaCozumu c, CozumEki e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(c.Marka);
            if (c.Seri.Length > 0) sb.Append(' ').Append(c.Seri);
            List<string> o = new List<string>();
            if (c.Tur.Length > 0) o.Add(c.HizMTs > 0 ? c.Tur + "-" + c.HizMTs : c.Tur);
            else if (c.HizMTs > 0) o.Add(c.HizMTs + " MT/s");
            if (c.FormFaktor.Length > 0) o.Add(c.FormFaktor);
            if (c.KapasiteGB > 0) o.Add(c.KapasiteGB + " GB" + (e.Kit > 1 ? " (modül başına)" : ""));
            if (c.CL > 0) o.Add("CL" + c.CL);
            if (c.Ecc.HasValue) o.Add(c.Ecc.Value ? "ECC" : "ECC'siz");
            o.AddRange(e.Notlar);
            if (e.Kit > 1)
            {
                if (c.KapasiteGB > 0)
                    o.Add(e.Kit + "'li kit: " + e.Kit + " × " + c.KapasiteGB + " GB = " + (e.Kit * c.KapasiteGB) + " GB");
                else
                    o.Add(e.Kit + "'li kit");
            }
            if (o.Count > 0) sb.Append(": ").Append(string.Join(", ", o.ToArray()));
            return sb.ToString();
        }

        // ---- Kingston -------------------------------------------------------

        static void KingstonForm(ParcaCozumu c, CozumEki e, string harf)
        {
            switch (harf)
            {
                case "N": case "U": c.FormFaktor = "DIMM"; c.Ecc = false; break;
                case "S": c.FormFaktor = "SODIMM"; c.Ecc = false; break;
                case "E": c.FormFaktor = "DIMM"; c.Ecc = true; e.Notlar.Add("tamponsuz ECC (ECC UDIMM)"); break;
                case "R": c.FormFaktor = "DIMM"; c.Ecc = true; e.Notlar.Add("Registered (RDIMM, sunucu belleği)"); break;
            }
        }

        static bool Kingston(string p, ParcaCozumu c, CozumEki e)
        {
            Match m;
            if (p.StartsWith("KVR", StringComparison.Ordinal))
            {
                m = rxKvr.Match(p);
                if (!m.Success) return false;
                c.Marka = "Kingston"; c.Seri = "ValueRAM";
                string hs = m.Groups[1].Value;
                int hiz = hs.Length == 2 ? Hiz2(Sayi(hs)) : Sayi(hs);
                c.HizMTs = hiz;
                string d = m.Groups[2].Value;
                bool dusukV = m.Groups[3].Success && m.Groups[3].Value == "L";
                if (d == "D2") c.Tur = "DDR2";
                else if (d == "D3") c.Tur = "DDR3";
                else if (d == "D4") c.Tur = "DDR4";
                else c.Tur = TurHizdan(hiz);
                if (c.Tur == "DDR3" && dusukV) c.Tur = "DDR3L";
                KingstonForm(c, e, m.Groups[4].Value);
                c.CL = Sayi(m.Groups[5].Value);
                int kit = m.Groups[7].Success ? Sayi(m.Groups[7].Value) : 1;
                Kapasite(c, e, Sayi(m.Groups[8].Value), kit);
                return true;
            }
            if (p.StartsWith("KF", StringComparison.Ordinal) || p.StartsWith("HX", StringComparison.Ordinal))
            {
                m = rxKf.Match(p);
                if (!m.Success)
                {
                    m = rxKfSpd.Match(p);
                    if (!m.Success) return false;
                    c.Marka = "Kingston";
                    c.Seri = m.Groups[1].Value == "KF" ? "FURY" : "HyperX";
                    c.HizMTs = Sayi(m.Groups[2].Value);
                    c.CL = Sayi(m.Groups[3].Value);
                    c.Tur = TurNesilden(m.Groups[4].Value);
                    if (c.Tur.Length == 0) return false;
                    c.KapasiteGB = Sayi(m.Groups[5].Value);
                    c.Ecc = false;
                    return true;
                }
                bool fury = m.Groups[1].Value == "KF";
                c.Marka = "Kingston";
                c.Tur = TurNesilden(m.Groups[2].Value);
                if (c.Tur.Length == 0) return false;
                if (c.Tur == "DDR3" && m.Groups[4].Success && m.Groups[4].Value == "L") c.Tur = "DDR3L";
                c.HizMTs = Hiz2(Sayi(m.Groups[3].Value));
                c.FormFaktor = m.Groups[5].Value == "S" ? "SODIMM" : "DIMM";
                c.Ecc = false;
                c.CL = Sayi(m.Groups[6].Value);
                string seri = m.Groups[7].Value;
                if (fury)
                {
                    if (seri.StartsWith("R", StringComparison.Ordinal)) c.Seri = "FURY Renegade";
                    else if (seri.StartsWith("I", StringComparison.Ordinal)) c.Seri = "FURY Impact";
                    else if (seri.StartsWith("B", StringComparison.Ordinal)) c.Seri = "FURY Beast";
                    else c.Seri = "FURY";
                    if (seri.Length > 1 && seri.IndexOf('A', 1) > 0) c.Seri += " RGB";
                }
                else
                {
                    if (seri.StartsWith("F", StringComparison.Ordinal)) c.Seri = "HyperX FURY";
                    else if (seri.StartsWith("P", StringComparison.Ordinal)) c.Seri = "HyperX Predator";
                    else if (seri.StartsWith("I", StringComparison.Ordinal)) c.Seri = "HyperX Impact";
                    else if (seri.StartsWith("S", StringComparison.Ordinal)) c.Seri = "HyperX Savage";
                    else c.Seri = "HyperX";
                }
                int kit = m.Groups[8].Success ? Sayi(m.Groups[8].Value) : 1;
                Kapasite(c, e, Sayi(m.Groups[9].Value), kit);
                return true;
            }
            if (p.StartsWith("KHX", StringComparison.Ordinal))
            {
                m = rxKhx.Match(p);
                if (!m.Success) return false;
                c.Marka = "Kingston"; c.Seri = "HyperX";
                c.HizMTs = Sayi(m.Groups[1].Value);
                string kalan = m.Groups[3].Value;
                if (kalan.IndexOf("D3", StringComparison.Ordinal) >= 0) c.Tur = "DDR3";
                else if (kalan.IndexOf("D4", StringComparison.Ordinal) >= 0) c.Tur = "DDR4";
                else c.Tur = c.HizMTs >= 2133 ? "DDR4" : "DDR3";
                if (kalan.StartsWith("S", StringComparison.Ordinal)) c.FormFaktor = "SODIMM";
                c.CL = Sayi(m.Groups[2].Value);
                int kit = m.Groups[4].Success ? Sayi(m.Groups[4].Value) : 1;
                Kapasite(c, e, Sayi(m.Groups[5].Value), kit);
                return true;
            }
            if (p.StartsWith("KCP", StringComparison.Ordinal))
            {
                m = rxKcp.Match(p);
                if (!m.Success) return false;
                c.Marka = "Kingston"; c.Seri = "sisteme özel (KCP)";
                c.Tur = TurNesilden(m.Groups[1].Value);
                if (c.Tur.Length == 0) return false;
                if (c.Tur == "DDR3" && m.Groups[2].Success && m.Groups[2].Value == "L") c.Tur = "DDR3L";
                c.HizMTs = Hiz2(Sayi(m.Groups[3].Value));
                KingstonForm(c, e, m.Groups[4].Value);
                int kit = m.Groups[6].Success ? Sayi(m.Groups[6].Value) : 1;
                Kapasite(c, e, Sayi(m.Groups[7].Value), kit);
                return true;
            }
            if (p.StartsWith("KSM", StringComparison.Ordinal))
            {
                m = rxKsm.Match(p);
                if (!m.Success) return false;
                c.Marka = "Kingston"; c.Seri = "Server Premier";
                c.HizMTs = Hiz2(Sayi(m.Groups[1].Value));
                c.Tur = TurHizdan(c.HizMTs);
                string f = m.Groups[2].Value;
                c.Ecc = true;
                if (f.StartsWith("R", StringComparison.Ordinal)) { c.FormFaktor = "DIMM"; e.Notlar.Add("Registered (RDIMM, sunucu belleği)"); }
                else if (f.StartsWith("E", StringComparison.Ordinal)) { c.FormFaktor = "DIMM"; e.Notlar.Add("tamponsuz ECC (ECC UDIMM)"); }
                else if (f.StartsWith("S", StringComparison.Ordinal)) { c.FormFaktor = "SODIMM"; e.Notlar.Add("ECC SODIMM"); }
                int kit = m.Groups[4].Success ? Sayi(m.Groups[4].Value) : 1;
                Kapasite(c, e, Sayi(m.Groups[5].Value), kit);
                return true;
            }
            if (p.StartsWith("9905", StringComparison.Ordinal) || p.StartsWith("9965", StringComparison.Ordinal)
                || p.StartsWith("99U5", StringComparison.Ordinal) || p.StartsWith("99P5", StringComparison.Ordinal))
            {
                c.Marka = "Kingston"; c.Seri = "OEM";
                c.Aciklama = "Kingston OEM modülü — bu parça numarası kapasite/hız bilgisi taşımaz; modülün üzerindeki etikete bakın.";
                return true;
            }
            return false;
        }

        // ---- Samsung --------------------------------------------------------

        static bool Samsung(string p, ParcaCozumu c, CozumEki e)
        {
            Match m = rxSamsung.Match(p);
            if (!m.Success) return false;
            string tip = m.Groups[1].Value, nesil = m.Groups[2].Value, derinlik = m.Groups[3].Value;
            bool ddr5Tip = tip == "425" || tip == "323" || tip == "321" || tip == "324";
            if (ddr5Tip != (nesil == "R")) return false;
            c.Marka = "Samsung";
            switch (tip)
            {
                case "471": case "425": c.FormFaktor = "SODIMM"; c.Ecc = false; break;
                case "378": case "323": c.FormFaktor = "DIMM"; c.Ecc = false; break;
                case "474": c.FormFaktor = "SODIMM"; c.Ecc = true; break;
                case "391": case "324": c.FormFaktor = "DIMM"; c.Ecc = true; e.Notlar.Add("tamponsuz ECC (ECC UDIMM)"); break;
                case "393": case "321": c.FormFaktor = "DIMM"; c.Ecc = true; e.Notlar.Add("Registered (RDIMM, sunucu belleği)"); break;
                case "386": c.FormFaktor = "DIMM"; c.Ecc = true; e.Notlar.Add("Load-Reduced (LRDIMM, sunucu belleği)"); break;
            }
            int gb = DerinlikGB(derinlik);
            if (gb == 0 && derinlik == "57") gb = 2;
            if (gb == 0 && derinlik == "28") gb = 1;
            c.KapasiteGB = gb;

            int tire = p.IndexOf('-');
            string hk = (tire > 0 && p.Length >= tire + 4) ? p.Substring(tire + 1, 3) : "";
            if (nesil == "A")
            {
                c.Tur = "DDR4";
                switch (hk)
                {
                    case "CPB": c.HizMTs = 2133; c.CL = 15; break;
                    case "CRC": c.HizMTs = 2400; c.CL = 17; break;
                    case "CTD": c.HizMTs = 2666; c.CL = 19; break;
                    case "CVF": c.HizMTs = 2933; c.CL = 21; break;
                    case "CWE": c.HizMTs = 3200; c.CL = 22; break;
                }
            }
            else if (nesil == "R")
            {
                c.Tur = "DDR5";
                switch (hk)
                {
                    case "CQK": c.HizMTs = 4800; c.CL = 40; break;
                    case "CWM": c.HizMTs = 5600; c.CL = 46; break;
                }
            }
            else
            {
                c.Tur = "DDR3";
                if (hk.Length == 3)
                {
                    if (hk[0] == 'Y') c.Tur = "DDR3L";
                    switch (hk.Substring(1))
                    {
                        case "H9": c.HizMTs = 1333; c.CL = 9; break;
                        case "K0": c.HizMTs = 1600; c.CL = 11; break;
                        case "MA": c.HizMTs = 1866; c.CL = 13; break;
                    }
                }
            }
            return true;
        }

        // ---- SK hynix -------------------------------------------------------

        static bool Hynix(string p, ParcaCozumu c, CozumEki e)
        {
            Match m = rxHynix.Match(p);
            if (m.Success)
            {
                c.Marka = "SK hynix";
                bool ddr4 = m.Groups[1].Value == "A";
                c.Tur = ddr4 ? "DDR4" : "DDR3";
                int gb = DerinlikGB(m.Groups[3].Value);
                if (gb == 0 && m.Groups[3].Value == "25") gb = 2;
                c.KapasiteGB = gb;
                string f = m.Groups[4].Value;
                bool ecc = m.Groups[5].Value == "7" || m.Groups[5].Value == "4" || f == "R";
                c.Ecc = ecc;
                if (f == "S") c.FormFaktor = "SODIMM";
                else c.FormFaktor = "DIMM";
                if (f == "R") e.Notlar.Add("Registered (RDIMM, sunucu belleği)");
                else if (ecc && f == "U") e.Notlar.Add("tamponsuz ECC (ECC UDIMM)");
                int tire = p.IndexOf('-');
                if (!ddr4 && tire > 0 && p[tire - 1] == 'A') c.Tur = "DDR3L";   // voltaj eki: A = 1,35 V, C = 1,5 V
                string hk = (tire > 0 && p.Length >= tire + 3) ? p.Substring(tire + 1, 2) : "";
                if (ddr4)
                {
                    switch (hk)
                    {
                        case "TF": c.HizMTs = 2133; c.CL = 15; break;
                        case "UH": c.HizMTs = 2400; c.CL = 17; break;
                        case "VK": c.HizMTs = 2666; c.CL = 19; break;
                        case "WM": c.HizMTs = 2933; c.CL = 21; break;
                        case "XN": c.HizMTs = 3200; c.CL = 22; break;
                    }
                }
                else
                {
                    switch (hk)
                    {
                        case "G7": c.HizMTs = 1066; c.CL = 7; break;
                        case "H9": c.HizMTs = 1333; c.CL = 9; break;
                        case "PB": c.HizMTs = 1600; c.CL = 11; break;
                        case "RD": c.HizMTs = 1866; c.CL = 13; break;
                    }
                }
                return true;
            }
            if (rxHynix5.IsMatch(p))
            {
                c.Marka = "SK hynix";
                c.Tur = "DDR5";
                char f = p[9];
                if (f == 'S') { c.FormFaktor = "SODIMM"; }
                else if (f == 'U') { c.FormFaktor = "DIMM"; }
                else if (f == 'R') { c.FormFaktor = "DIMM"; c.Ecc = true; e.Notlar.Add("Registered (RDIMM, sunucu belleği)"); }
                return true;
            }
            return false;
        }

        // ---- Micron ---------------------------------------------------------

        static int MicronDerinlikGB(string sayi, bool g)
        {
            long derinlikM = Sayi(sayi);
            if (g) derinlikM *= 1024;
            long gb = derinlikM * 8 / 1024;
            if (gb <= 0 || gb > 512 || (derinlikM * 8) % 1024 != 0) return 0;
            return (int)gb;
        }

        static void MicronForm(ParcaCozumu c, CozumEki e, string harf, bool ecc)
        {
            c.Ecc = ecc;
            switch (harf)
            {
                case "H": c.FormFaktor = "SODIMM"; break;
                case "A": c.FormFaktor = "DIMM"; if (ecc) e.Notlar.Add("tamponsuz ECC (ECC UDIMM)"); break;
                case "P": c.FormFaktor = "DIMM"; e.Notlar.Add("Registered (RDIMM, sunucu belleği)"); break;
            }
        }

        static bool Micron(string p, ParcaCozumu c, CozumEki e)
        {
            if (!p.StartsWith("MT", StringComparison.Ordinal)) return false;
            Match m = rxMicron5.Match(p);
            if (m.Success)
            {
                c.Marka = "Micron";
                c.Tur = "DDR5";
                int yonga = Sayi(m.Groups[1].Value);
                int rank = Sayi(m.Groups[2].Value);
                int genislik = Sayi(m.Groups[3].Value);
                int kod = Sayi(m.Groups[4].Value);
                string f = m.Groups[5].Value;
                if (genislik > 0)
                {
                    int veriYongasi = rank * 64 / genislik;
                    c.Ecc = yonga > veriYongasi;
                    // 16 Gbit yonga varsayımı yalnızca parça numarasındaki yoğunluk koduyla (2^kod GB) örtüşürse kabul edilir
                    int gb16 = veriYongasi * 2;
                    if (kod >= 0 && kod <= 9 && (1 << kod) == gb16) c.KapasiteGB = gb16;
                }
                if (f == "S") c.FormFaktor = "SODIMM";
                else if (f == "U") c.FormFaktor = "DIMM";
                else if (f == "R") { c.FormFaktor = "DIMM"; c.Ecc = true; e.Notlar.Add("Registered (RDIMM, sunucu belleği)"); }
                c.HizMTs = Hiz2(Sayi(m.Groups[6].Value));
                return true;
            }
            m = rxMicron4.Match(p);
            if (m.Success)
            {
                c.Marka = "Micron";
                c.Tur = "DDR4";
                c.KapasiteGB = MicronDerinlikGB(m.Groups[2].Value, m.Groups[3].Value == "G");
                MicronForm(c, e, m.Groups[5].Value, m.Groups[4].Value == "72");
                switch (m.Groups[6].Value)
                {
                    case "2G1": c.HizMTs = 2133; break;
                    case "2G3": c.HizMTs = 2400; break;
                    case "2G6": c.HizMTs = 2666; break;
                    case "2G9": c.HizMTs = 2933; break;
                    case "3G2": c.HizMTs = 3200; break;
                }
                return true;
            }
            m = rxMicron3.Match(p);
            if (m.Success)
            {
                c.Marka = "Micron";
                c.Tur = m.Groups[2].Value == "K" ? "DDR3L" : "DDR3";
                c.KapasiteGB = MicronDerinlikGB(m.Groups[3].Value, m.Groups[4].Value == "G");
                MicronForm(c, e, m.Groups[6].Value, m.Groups[5].Value == "72");
                switch (m.Groups[7].Value)
                {
                    case "1G1": c.HizMTs = 1066; break;
                    case "1G4": c.HizMTs = 1333; break;
                    case "1G6": c.HizMTs = 1600; break;
                    case "1G9": c.HizMTs = 1866; break;
                }
                return true;
            }
            return false;
        }

        // ---- Crucial --------------------------------------------------------

        static bool Crucial(string p, ParcaCozumu c, CozumEki e)
        {
            Match m = rxCrucial5.Match(p);
            if (m.Success)
            {
                c.Marka = "Crucial";
                c.Seri = m.Groups[1].Value == "P" ? "Pro" : "";
                c.Tur = "DDR5";
                c.HizMTs = Hiz2(Sayi(m.Groups[4].Value));
                c.CL = Sayi(m.Groups[5].Value);
                c.FormFaktor = m.Groups[6].Value == "S" ? "SODIMM" : "DIMM";
                c.Ecc = false;
                int kit = m.Groups[2].Success ? Sayi(m.Groups[2].Value) : 1;
                // Crucial "CT2K16G" = 2 adet 16 GB: sayı zaten modül başı
                c.KapasiteGB = Sayi(m.Groups[3].Value);
                if (kit > 1) e.Kit = kit;
                return true;
            }
            m = rxCrucial4.Match(p);
            if (m.Success)
            {
                c.Marka = "Crucial";
                c.Seri = m.Groups[1].Value == "P" ? "Pro" : "";
                c.Tur = "DDR4";
                string f = m.Groups[4].Value;
                if (f == "S") { c.FormFaktor = "SODIMM"; c.Ecc = false; }
                else if (f == "D") { c.FormFaktor = "DIMM"; c.Ecc = false; }
                else if (f == "W") { c.FormFaktor = "DIMM"; c.Ecc = true; e.Notlar.Add("tamponsuz ECC (ECC UDIMM)"); }
                else if (f == "R") { c.FormFaktor = "DIMM"; c.Ecc = true; e.Notlar.Add("Registered (RDIMM, sunucu belleği)"); }
                string hs = m.Groups[6].Value;
                c.HizMTs = hs.Length == 3 ? Hiz3(Sayi(hs)) : Hiz2(Sayi(hs));
                if (c.HizMTs > 0 && TurHizdan(c.HizMTs) != "DDR4") c.HizMTs = 0;
                int kit = m.Groups[2].Success ? Sayi(m.Groups[2].Value) : 1;
                c.KapasiteGB = Sayi(m.Groups[3].Value);
                if (kit > 1) e.Kit = kit;
                return true;
            }
            m = rxBallistix.Match(p);
            if (m.Success)
            {
                c.Marka = "Crucial"; c.Seri = "Ballistix";
                c.Tur = "DDR4";
                c.HizMTs = Hiz2(Sayi(m.Groups[3].Value));
                c.CL = Sayi(m.Groups[4].Value);
                c.FormFaktor = m.Groups[5].Value == "S" ? "SODIMM" : "DIMM";
                c.Ecc = false;
                int kit = m.Groups[1].Success ? Sayi(m.Groups[1].Value) : 1;
                c.KapasiteGB = Sayi(m.Groups[2].Value);
                if (kit > 1) e.Kit = kit;
                return true;
            }
            return false;
        }

        // ---- Corsair --------------------------------------------------------

        static bool Corsair(string p, ParcaCozumu c, CozumEki e)
        {
            Match m = rxCorsair.Match(p);
            if (!m.Success) return false;
            c.Marka = "Corsair";
            string s = m.Groups[1].Value;
            c.Tur = TurNesilden(m.Groups[3].Value);
            c.HizMTs = Sayi(m.Groups[6].Value);
            c.CL = Sayi(m.Groups[7].Value);
            c.Ecc = false;
            c.FormFaktor = s.StartsWith("S", StringComparison.Ordinal) ? "SODIMM" : "DIMM";
            switch (s)
            {
                case "K": c.Seri = c.Tur == "DDR4" ? "Vengeance LPX" : "Vengeance"; break;
                case "W": c.Seri = c.Tur == "DDR4" ? "Vengeance RGB PRO" : ""; break;
                case "H": c.Seri = c.Tur == "DDR5" ? "Vengeance RGB" : ""; break;
                case "T": c.Seri = "Dominator Platinum RGB"; break;
                case "SX": c.Seri = "Vengeance (SODIMM)"; break;
                case "V": c.Seri = "ValueSelect"; break;
                case "Z": c.Seri = "Vengeance"; break;
            }
            Kapasite(c, e, Sayi(m.Groups[2].Value), Sayi(m.Groups[4].Value));
            return true;
        }

        // ---- G.Skill --------------------------------------------------------

        static void GSkillSeri(ParcaCozumu c, string s)
        {
            if (s.StartsWith("RS", StringComparison.Ordinal) && !s.StartsWith("RS5", StringComparison.Ordinal))
            {
                c.Seri = "Ripjaws (SODIMM)"; c.FormFaktor = "SODIMM"; return;
            }
            string ad = "";
            if (s.StartsWith("TZ5N", StringComparison.Ordinal)) ad = "Trident Z5 Neo";
            else if (s.StartsWith("TZ5R", StringComparison.Ordinal)) ad = "Trident Z5 RGB";
            else if (s.StartsWith("TZ5", StringComparison.Ordinal)) ad = "Trident Z5";
            else if (s.StartsWith("TZN", StringComparison.Ordinal)) ad = "Trident Z Neo";
            else if (s.StartsWith("TZR", StringComparison.Ordinal)) ad = "Trident Z RGB";
            else if (s.StartsWith("TZ", StringComparison.Ordinal)) ad = "Trident Z";
            else if (s.StartsWith("RS5", StringComparison.Ordinal)) ad = "Ripjaws S5";
            else if (s.StartsWith("VK", StringComparison.Ordinal) || s.StartsWith("VR", StringComparison.Ordinal)) ad = "Ripjaws V";
            else if (s.StartsWith("IS", StringComparison.Ordinal)) ad = "Aegis";
            else if (s.StartsWith("NT", StringComparison.Ordinal)) ad = "Value";
            if (ad.Length > 0) { c.Seri = ad; c.FormFaktor = "DIMM"; }
        }

        static bool GSkill(string p, ParcaCozumu c, CozumEki e)
        {
            if (!p.StartsWith("F", StringComparison.Ordinal)) return false;
            Match m = rxGskill5.Match(p);
            if (m.Success)
            {
                c.Marka = "G.Skill";
                c.Tur = "DDR5";
                c.HizMTs = Sayi(m.Groups[1].Value);
                c.CL = Sayi(m.Groups[3].Value);
                c.Ecc = false;
                c.KapasiteGB = Sayi(m.Groups[6].Value); // "16GX2" = 2 × 16 GB
                int adet = m.Groups[7].Success ? Sayi(m.Groups[7].Value) : 1;   // SPD biçiminde (F5-...F16G) kit eki yok
                if (adet > 1) e.Kit = adet;
                if (m.Groups[8].Success) GSkillSeri(c, m.Groups[8].Value);
                return true;
            }
            m = rxGskill.Match(p);
            if (m.Success)
            {
                c.Marka = "G.Skill";
                c.Tur = TurNesilden(m.Groups[1].Value);
                c.HizMTs = Sayi(m.Groups[2].Value);
                c.CL = Sayi(m.Groups[3].Value);
                c.Ecc = false;
                int adet = 0;
                switch (m.Groups[4].Value)
                {
                    case "S": adet = 1; break;
                    case "D": adet = 2; break;
                    case "Q": adet = 4; break;
                    case "O": adet = 8; break;
                }
                int toplam = Sayi(m.Groups[5].Value);
                if (adet > 0) Kapasite(c, e, toplam, adet);
                else c.KapasiteGB = toplam;   // kit harfi yok: modülün SPD'sindeki modül başı ad (F4-3200C16-8GVKB)
                GSkillSeri(c, m.Groups[6].Value);
                return true;
            }
            return false;
        }

        // ---- ADATA / XPG ----------------------------------------------------

        static bool Adata(string p, ParcaCozumu c, CozumEki e)
        {
            Match m = rxAdata.Match(p);
            if (!m.Success) return false;
            c.Marka = "ADATA";
            c.Seri = m.Groups[1].Value == "X" ? "XPG" : "";
            c.Tur = TurNesilden(m.Groups[2].Value);
            c.FormFaktor = m.Groups[3].Value == "S" ? "SODIMM" : "DIMM";
            c.Ecc = false;
            c.HizMTs = Sayi(m.Groups[4].Value);
            if (m.Groups[5].Success) c.CL = Sayi(m.Groups[5].Value);
            else if (m.Groups[7].Success) c.CL = Sayi(m.Groups[7].Value);
            // ADATA hız ile kapasite arasına tek haneli yapılandırma rakamı koyabilir (AD4S320038G22 = 8 GB,
            // AD4U3200716G22 = 16 GB): geçerli bir modül kapasitesi değilse ilk hane atılır.
            string ks = m.Groups[6].Value;
            int[] gecerli = { 4, 8, 16, 24, 32, 48, 64, 96, 128, 256 };
            int k = Sayi(ks);
            if (Array.IndexOf(gecerli, k) < 0)
            {
                k = ks.Length > 1 ? Sayi(ks.Substring(1)) : 0;
                if (Array.IndexOf(gecerli, k) < 0) k = 0;
            }
            c.KapasiteGB = k;
            return true;
        }

        // ---- Team Group -----------------------------------------------------

        static bool Team(string p, string marka, ParcaCozumu c, CozumEki e)
        {
            if (marka.Length > 0 && marka != "Team Group") return false;
            Match m = rxTeam.Match(p);
            if (!m.Success) return false;
            c.Marka = "Team Group";
            string s = m.Groups[1].Value;
            string kalan = m.Groups[6].Value;
            bool elite = s == "E" || s == "P";
            c.Seri = s == "E" ? "Elite" : (s == "P" ? "Elite Plus" : "T-Force");
            c.Tur = TurNesilden(m.Groups[2].Value);
            c.HizMTs = Sayi(m.Groups[4].Value);
            c.CL = Sayi(m.Groups[5].Value);
            c.Ecc = false;
            if (kalan.StartsWith("-S", StringComparison.Ordinal)) c.FormFaktor = "SODIMM";
            else if (elite) c.FormFaktor = "DIMM";
            int kit = 1;
            if (kalan.IndexOf("DC", StringComparison.Ordinal) >= 0) kit = 2;
            else if (kalan.IndexOf("QC", StringComparison.Ordinal) >= 0) kit = 4;
            Kapasite(c, e, Sayi(m.Groups[3].Value), kit);
            return true;
        }

        // ---- Patriot --------------------------------------------------------

        static bool Patriot(string p, ParcaCozumu c, CozumEki e)
        {
            if (!p.StartsWith("P", StringComparison.Ordinal)) return false;
            Match m = rxPatriotSig.Match(p);
            if (m.Success)
            {
                c.Marka = "Patriot"; c.Seri = "Signature";
                c.Tur = TurNesilden(m.Groups[1].Value);
                c.KapasiteGB = Sayi(m.Groups[2].Value);
                c.HizMTs = Sayi(m.Groups[3].Value);
                c.Ecc = false;
                if (m.Groups[5].Value == "S") c.FormFaktor = "SODIMM";
                return true;
            }
            m = rxPatriotViper.Match(p);
            if (m.Success)
            {
                c.Marka = "Patriot"; c.Seri = "Viper";
                c.Tur = TurNesilden(m.Groups[2].Value);
                c.HizMTs = Hiz3(Sayi(m.Groups[4].Value));
                string cl = m.Groups[5].Value;
                if (cl.Length == 2) c.CL = Sayi(cl);
                c.Ecc = false;
                int kit = 1;
                if (m.Groups[7].Success && m.Groups[7].Value == "K") kit = (m.Groups[6].Success && m.Groups[6].Value == "Q") ? 4 : 2;
                Kapasite(c, e, Sayi(m.Groups[3].Value), kit);
                return true;
            }
            return false;
        }

        // ---- Lexar, Ramaxel, Transcend JetRam ------------------------------

        static bool Digerleri(string p, ParcaCozumu c, CozumEki e)
        {
            Match m = rxLexar.Match(p);
            if (m.Success)
            {
                c.Marka = "Lexar";
                c.Tur = "DDR" + m.Groups[1].Value;
                c.FormFaktor = m.Groups[2].Value == "S" ? "SODIMM" : "DIMM";
                c.KapasiteGB = Sayi(m.Groups[3].Value);
                c.HizMTs = Sayi(m.Groups[4].Value);
                c.Ecc = false;
                return true;
            }
            m = rxRamaxel.Match(p);
            if (m.Success)
            {
                c.Marka = "Ramaxel";
                c.Tur = "DDR4";
                c.FormFaktor = m.Groups[1].Value == "S" ? "SODIMM" : "DIMM";
                int hiz = Sayi(m.Groups[2].Value);
                if (TurHizdan(hiz) == "DDR4") c.HizMTs = hiz;
                return true;
            }
            m = rxJetram.Match(p);
            if (m.Success)
            {
                int hiz = Sayi(m.Groups[1].Value);
                if (TurHizdan(hiz) != "DDR4") return false;
                c.Marka = "Transcend"; c.Seri = "JetRam";
                c.Tur = "DDR4";
                c.HizMTs = hiz;
                c.FormFaktor = m.Groups[2].Value == "S" ? "SODIMM" : "DIMM";
                c.KapasiteGB = Sayi(m.Groups[3].Value);
                c.Ecc = false;
                return true;
            }
            return false;
        }
    }
}
