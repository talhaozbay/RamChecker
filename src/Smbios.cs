// RAM Danışmanı — SMBIOS (DMTF DSP0134 3.x) okuyucu ve ayrıştırıcı.
// Ham tablo kernel32!GetSystemFirmwareTable('RSMB') ile yönetici izni olmadan okunur.
// Ayrıştırıcı hiçbir koşulda istisna fırlatmaz; bozuk/kesik veride elinden geleni
// döndürür ve sorunları Notlar listesine yazar. Alan varlığı SMBIOS sürümüne değil
// her yapının Length alanına göre belirlenir.
// C# 5 uyumlu (Windows'taki yerleşik csc.exe).
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace RamDanismani
{
    public class SmbiosSonucu
    {
        public int Major;
        public int Minor;
        public SistemBilgisi Sistem = new SistemBilgisi();
        public List<BellekDizisi> Diziler = new List<BellekDizisi>();
        public List<BellekYuvasi> Yuvalar = new List<BellekYuvasi>();
        public List<string> Notlar = new List<string>();
    }

    public static class Smbios
    {
        const uint RSMB = 0x52534D42; // 'RSMB'

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetSystemFirmwareTable(uint FirmwareTableProviderSignature, uint FirmwareTableID,
            byte[] pFirmwareTableBuffer, uint BufferSize);

        // ---- Okuma ---------------------------------------------------------------

        /// <summary>RawSMBIOSData yapısının tamamı (8 bayt başlık + veri). Hata -> null.</summary>
        static byte[] HamTamOku()
        {
            try
            {
                uint boyut = GetSystemFirmwareTable(RSMB, 0, null, 0);
                if (boyut < 8 || boyut > 64 * 1024 * 1024) return null;
                for (int deneme = 0; deneme < 3; deneme++)
                {
                    byte[] tampon = new byte[boyut];
                    uint okunan = GetSystemFirmwareTable(RSMB, 0, tampon, boyut);
                    if (okunan == 0) return null;
                    if (okunan > boyut) { boyut = okunan; continue; } // tablo arada büyüdüyse
                    if (okunan < 8) return null;
                    if (okunan < boyut)
                    {
                        byte[] kisa = new byte[okunan];
                        Array.Copy(tampon, kisa, (int)okunan);
                        return kisa;
                    }
                    return tampon;
                }
            }
            catch (Exception) { }
            return null;
        }

        /// <summary>RawSMBIOSData başlığını ayıklar. Başlık: bayt0 Used20CallingMethod, bayt1 Major,
        /// bayt2 Minor, bayt3 DmiRevision, bayt4..7 Length.</summary>
        static byte[] BasligiAyikla(byte[] tam, out int major, out int minor)
        {
            major = tam[1];
            minor = tam[2];
            long uzunluk = BitConverter.ToUInt32(tam, 4);
            long mevcut = tam.Length - 8;
            if (uzunluk <= 0 || uzunluk > mevcut) uzunluk = mevcut;
            byte[] tablo = new byte[uzunluk];
            Array.Copy(tam, 8, tablo, 0, (int)uzunluk);
            return tablo;
        }

        public static bool HamOku(out byte[] tablo, out int major, out int minor)
        {
            tablo = null; major = 0; minor = 0;
            try
            {
                byte[] tam = HamTamOku();
                if (tam == null || tam.Length < 8) return false;
                tablo = BasligiAyikla(tam, out major, out minor);
                return tablo.Length > 0;
            }
            catch (Exception)
            {
                tablo = null; major = 0; minor = 0;
                return false;
            }
        }

        /// <summary>Dosya RawSMBIOSData başlıklıysa (bayt4..7 uzunluk == dosya-8) başlığı ayıklar;
        /// değilse dosyanın tamamı tablodur ve major=minor=0. Dosya okunamazsa null döner (istisna yok).</summary>
        public static byte[] DosyadanOku(string yol, out int major, out int minor)
        {
            major = 0; minor = 0;
            try
            {
                byte[] veri = File.ReadAllBytes(yol);
                if (BaslikliMi(veri))
                    return BasligiAyikla(veri, out major, out minor);
                return veri;
            }
            catch (Exception)
            {
                major = 0; minor = 0;
                return null;
            }
        }

        static bool BaslikliMi(byte[] veri)
        {
            if (veri == null || veri.Length < 8 + 4) return false;
            if (veri[0] > 1) return false;                 // Used20CallingMethod 0/1
            if (veri[1] < 2 || veri[1] > 9) return false;  // makul major
            long uzunluk = BitConverter.ToUInt32(veri, 4);
            return uzunluk == veri.Length - 8;
        }

        /// <summary>Canlı tabloyu başlıklı biçimde (8 bayt RawSMBIOSData + veri) yazar.</summary>
        public static bool DosyayaKaydet(string yol)
        {
            try
            {
                byte[] tam = HamTamOku();
                if (tam == null || tam.Length < 8) return false;
                int major, minor;
                byte[] tablo = BasligiAyikla(tam, out major, out minor);
                byte[] cikti = new byte[8 + tablo.Length];
                Array.Copy(tam, 0, cikti, 0, 4);
                byte[] u = BitConverter.GetBytes((uint)tablo.Length);
                Array.Copy(u, 0, cikti, 4, 4);
                Array.Copy(tablo, 0, cikti, 8, tablo.Length);
                File.WriteAllBytes(yol, cikti);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ---- Ayrıştırma ----------------------------------------------------------

        /// <summary>Tek bir SMBIOS yapısı: biçimli alan + string seti. Okumalar yapının
        /// Length sınırına göre korunur; olmayan alan 0 / "" döner.</summary>
        class Yapi
        {
            public int Tip;
            public int Uzunluk;
            public int Handle;
            public byte[] Veri;                  // biçimli alan (başlık dahil), Uzunluk bayt
            public List<string> Dizgiler = new List<string>();
            public List<string> Notlar;          // ortak not listesi

            public bool Var(int ofset, int boyut) { return ofset >= 0 && ofset + boyut <= Uzunluk; }
            public int B(int o) { return Var(o, 1) ? Veri[o] : 0; }
            public int W(int o) { return Var(o, 2) ? (Veri[o] | (Veri[o + 1] << 8)) : 0; }
            public long D(int o) { return Var(o, 4) ? (long)BitConverter.ToUInt32(Veri, o) : 0; }
            public ulong Q(int o) { return Var(o, 8) ? BitConverter.ToUInt64(Veri, o) : 0; }

            public string S(int o)
            {
                if (!Var(o, 1)) return "";
                int i = Veri[o];
                if (i == 0) return "";
                if (i > Dizgiler.Count)
                {
                    Notlar.Add("SMBIOS tip " + Tip + " (handle 0x" + Handle.ToString("X4") + "): ofset 0x" + o.ToString("X2") +
                        " string indeksi " + i + " aralık dışı (" + Dizgiler.Count + " string var) — boş sayıldı.");
                    return "";
                }
                return Dizgiler[i - 1];
            }
        }

        public static SmbiosSonucu Coz(byte[] tablo, int major, int minor)
        {
            SmbiosSonucu s = new SmbiosSonucu();
            s.Major = major;
            s.Minor = minor;
            try
            {
                if (major > 0) s.Sistem.SmbiosSurum = major + "." + minor;
                if (tablo == null || tablo.Length == 0)
                {
                    s.Notlar.Add("SMBIOS tablosu boş ya da okunamadı.");
                    return s;
                }
                List<Yapi> yapilar = Bol(tablo, s.Notlar);
                Isle(yapilar, s);
            }
            catch (Exception ex)
            {
                s.Notlar.Add("SMBIOS ayrıştırılırken beklenmeyen hata: " + ex.GetType().Name + ": " + ex.Message);
            }
            return s;
        }

        static List<Yapi> Bol(byte[] t, List<string> notlar)
        {
            List<Yapi> liste = new List<Yapi>();
            int pos = 0;
            int n = t.Length;
            int sayac = 0;
            while (pos + 4 <= n)
            {
                if (++sayac > 10000) { notlar.Add("SMBIOS: yapı sayısı sınırı aşıldı, okuma durduruldu."); break; }
                int tip = t[pos];
                int uz = t[pos + 1];
                int handle = t[pos + 2] | (t[pos + 3] << 8);
                if (uz < 4)
                {
                    notlar.Add("SMBIOS: ofset " + pos + " konumunda geçersiz yapı uzunluğu (" + uz + "), okuma durduruldu.");
                    break;
                }
                if (pos + uz > n)
                {
                    notlar.Add("SMBIOS: tip " + tip + " yapısı tablonun sonunda kesik (ofset " + pos + "), okuma durduruldu.");
                    break;
                }
                Yapi y = new Yapi();
                y.Tip = tip; y.Uzunluk = uz; y.Handle = handle; y.Notlar = notlar;
                y.Veri = new byte[uz];
                Array.Copy(t, pos, y.Veri, 0, uz);

                // String seti: çift NUL ile biter. Hiç string yoksa da iki NUL vardır.
                int i = pos + uz;
                int son = -1;
                if (i + 1 < n && t[i] == 0 && t[i + 1] == 0)
                {
                    son = i + 2;
                }
                else
                {
                    int bas = i;
                    for (int k = i; k < n; k++)
                    {
                        if (t[k] == 0)
                        {
                            if (k > bas) y.Dizgiler.Add(Dizgi(t, bas, k - bas));
                            if (k + 1 < n && t[k + 1] == 0) { son = k + 2; break; }
                            bas = k + 1;
                        }
                    }
                    // Sonlandırılmamış son string yarım olabilir (kesik tablo): atılır, ilgili alan boş kalır.
                }
                liste.Add(y);
                if (son < 0)
                {
                    notlar.Add("SMBIOS: tip " + tip + " (handle 0x" + handle.ToString("X4") + ") string bölgesi sonlanmamış; tablo kesik olabilir, okuma durduruldu. Son string atıldı.");
                    break;
                }
                if (tip == 127) break;
                pos = son;
            }
            return liste;
        }

        static Encoding utf8Kati = new UTF8Encoding(false, true);

        static string Dizgi(byte[] t, int bas, int uz)
        {
            string s;
            try { s = utf8Kati.GetString(t, bas, uz); }
            catch (Exception)
            {
                StringBuilder l = new StringBuilder(uz);
                for (int k = 0; k < uz; k++) l.Append((char)t[bas + k]); // Latin-1
                s = l.ToString();
            }
            StringBuilder sb = new StringBuilder(s.Length);
            foreach (char c in s)
                if (!char.IsControl(c)) sb.Append(c);
            return sb.ToString().Trim();
        }

        static void Isle(List<Yapi> yapilar, SmbiosSonucu s)
        {
            bool islemciBulundu = false;
            Yapi ilkIslemci = null;
            int toplamCekirdek = 0, toplamIzlek = 0;
            List<int> disaridakiDiziler = new List<int>();
            List<Yapi> tip17ler = new List<Yapi>();

            foreach (Yapi y in yapilar)
            {
                try
                {
                    switch (y.Tip)
                    {
                        case 0:
                            if (s.Sistem.BiosSurum.Length == 0) s.Sistem.BiosSurum = Temiz(y.S(0x05));
                            if (s.Sistem.BiosTarih.Length == 0) s.Sistem.BiosTarih = Temiz(y.S(0x08));
                            break;
                        case 1:
                            Tip1(y, s.Sistem);
                            break;
                        case 2:
                            if (s.Sistem.AnakartUretici.Length == 0) s.Sistem.AnakartUretici = Temiz(y.S(0x04));
                            if (s.Sistem.AnakartModel.Length == 0) s.Sistem.AnakartModel = Temiz(y.S(0x05));
                            break;
                        case 3:
                            if (s.Sistem.KasaTuruKodu == 0 && y.Var(0x05, 1))
                            {
                                s.Sistem.KasaTuruKodu = y.B(0x05) & 0x7F; // bit7 = kilit var
                                s.Sistem.Kasa = KasaTipiBul(s.Sistem.KasaTuruKodu);
                            }
                            break;
                        case 4:
                            {
                                int islemciTuru = y.B(0x05);
                                if (islemciTuru != 0 && islemciTuru != 3) break; // sadece merkezi işlemci
                                int durum = y.B(0x18);
                                bool takili = !y.Var(0x18, 1) || (durum & 0x40) != 0;
                                if (ilkIslemci == null) ilkIslemci = y;
                                if (!takili) break;
                                int cekirdek = y.B(0x23);
                                if (cekirdek == 0xFF && y.Var(0x2A, 2)) cekirdek = y.W(0x2A);
                                int izlek = y.B(0x25);
                                if (izlek == 0xFF && y.Var(0x2E, 2)) izlek = y.W(0x2E);
                                toplamCekirdek += cekirdek;
                                toplamIzlek += izlek;
                                if (!islemciBulundu)
                                {
                                    string ad = Temiz(y.S(0x10));
                                    if (ad.Length > 0) { s.Sistem.IslemciAdi = ad; islemciBulundu = true; }
                                }
                                break;
                            }
                        case 16:
                            {
                                BellekDizisi d = Tip16(y);
                                if (d.KullanimKodu >= 4 && d.KullanimKodu <= 7)
                                {
                                    disaridakiDiziler.Add(d.Handle);
                                    s.Notlar.Add("SMBIOS: handle 0x" + d.Handle.ToString("X4") + " bellek dizisi \"" + d.Kullanim +
                                        "\" amaçlı; sistem belleği olmadığı için atlandı.");
                                }
                                else s.Diziler.Add(d);
                                break;
                            }
                        case 17:
                            tip17ler.Add(y);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    s.Notlar.Add("SMBIOS tip " + y.Tip + " (handle 0x" + y.Handle.ToString("X4") + ") ayrıştırılamadı: " + ex.Message);
                }
            }

            if (!islemciBulundu && ilkIslemci != null)
            {
                s.Sistem.IslemciAdi = Temiz(ilkIslemci.S(0x10));
            }
            if (toplamCekirdek == 0 && ilkIslemci != null)
            {
                int c = ilkIslemci.B(0x23);
                if (c == 0xFF) c = ilkIslemci.W(0x2A);
                int z = ilkIslemci.B(0x25);
                if (z == 0xFF) z = ilkIslemci.W(0x2E);
                toplamCekirdek = c; toplamIzlek = z;
            }
            s.Sistem.CekirdekSayisi = toplamCekirdek;
            s.Sistem.IsParcacigiSayisi = toplamIzlek;

            foreach (Yapi y in tip17ler)
            {
                try
                {
                    BellekYuvasi m = Tip17(y, s.Notlar);
                    if (disaridakiDiziler.Contains(m.DiziHandle)) continue;
                    if (m.TurKodu >= 0x08 && m.TurKodu <= 0x0C)
                    {
                        s.Notlar.Add("SMBIOS: \"" + m.Yuva + "\" aygıtı " + m.Tur + " türünde; RAM olmadığı için atlandı.");
                        continue;
                    }
                    s.Yuvalar.Add(m);
                }
                catch (Exception ex)
                {
                    s.Notlar.Add("SMBIOS tip 17 (handle 0x" + y.Handle.ToString("X4") + ") ayrıştırılamadı: " + ex.Message);
                }
            }

            // BIOS'un bildirdiği yuva sayısı ile listelenen yuva sayısını karşılaştır.
            foreach (BellekDizisi d in s.Diziler)
            {
                int listelenen = 0;
                foreach (BellekYuvasi m in s.Yuvalar) if (m.DiziHandle == d.Handle) listelenen++;
                if (d.YuvaSayisi > 0 && listelenen != d.YuvaSayisi)
                    s.Notlar.Add("SMBIOS: bellek dizisi (handle 0x" + d.Handle.ToString("X4") + ") " + d.YuvaSayisi +
                        " yuva bildiriyor, ancak " + listelenen + " yuva listeleniyor.");
            }
            if (yapilar.Count > 0 && s.Yuvalar.Count == 0)
                s.Notlar.Add("SMBIOS: bellek aygıtı (tip 17) kaydı bulunamadı.");
        }

        static void Tip1(Yapi y, SistemBilgisi si)
        {
            if (si.Uretici.Length > 0 || si.Model.Length > 0) return; // ilk tip 1 geçerli
            si.Uretici = Temiz(y.S(0x04));
            si.Model = Temiz(y.S(0x05));
            string surum = Temiz(y.S(0x06));
            si.SeriNo = Temiz(y.S(0x07));
            si.ModelAdi = ModelAdiGecerliMi(surum) ? surum : si.Model;
        }

        /// <summary>Tip 1 Version alanı gerçek bir model adı mı (Lenovo: "LOQ 15IAX9E")?
        /// "1.0", "Rev 1.xx", "System Version", "None" gibi değerler değil.</summary>
        static bool ModelAdiGecerliMi(string v)
        {
            if (string.IsNullOrEmpty(v)) return false;
            bool harf = false;
            foreach (char c in v) if (char.IsLetter(c) && c != 'x' && c != 'X') { harf = true; break; }
            if (!harf) return false;
            string u = v.ToUpperInvariant();
            if (u.StartsWith("REV", StringComparison.Ordinal) && u.Length <= 10) return false;
            // "V1.0", "1.02", "0.1x" gibi salt sürüm numaraları
            string govde = u.StartsWith("V", StringComparison.Ordinal) ? u.Substring(1) : u;
            bool salt = govde.Length > 0;
            foreach (char c in govde) if (!(char.IsDigit(c) || c == '.' || c == 'X' || c == ' ')) { salt = false; break; }
            if (salt) return false;
            if (u == "SYSTEM VERSION" || u == "VERSION" || u == "PRODUCT VERSION") return false;
            return true;
        }

        static BellekDizisi Tip16(Yapi y)
        {
            BellekDizisi d = new BellekDizisi();
            d.Handle = y.Handle;
            int konum = y.B(0x04);
            d.Konum = DiziKonumuAdi(konum);
            d.KullanimKodu = y.B(0x05);
            d.Kullanim = DiziKullanimAdi(d.KullanimKodu);
            d.EccKodu = y.B(0x06);
            d.EccTuru = EccAdi(d.EccKodu);
            long maxKb = y.D(0x07);
            if (maxKb == 0x80000000L)
            {
                ulong bayt = y.Q(0x0F);
                d.MaxKapasiteMB = bayt > 0 ? (long)(bayt / (1024UL * 1024UL)) : 0;
            }
            else d.MaxKapasiteMB = maxKb / 1024;
            d.YuvaSayisi = y.W(0x0D);
            return d;
        }

        static BellekYuvasi Tip17(Yapi y, List<string> notlar)
        {
            BellekYuvasi m = new BellekYuvasi();
            m.Kaynak = "SMBIOS";
            m.Handle = y.Handle;
            m.DiziHandle = y.Var(0x04, 2) ? y.W(0x04) : 0xFFFE;
            int toplam = y.W(0x08), veri = y.W(0x0A);
            m.ToplamGenislik = (toplam == 0xFFFF) ? 0 : toplam;
            m.VeriGenisligi = (veri == 0xFFFF) ? 0 : veri;

            m.FormFaktorKodu = y.B(0x0E);
            m.FormFaktor = FormFaktorAdi(m.FormFaktorKodu);
            m.Yuva = Temiz(y.S(0x10));
            m.Banka = Temiz(y.S(0x11));
            m.TurKodu = y.B(0x12);
            m.Tur = BellekTuruAdi(m.TurKodu);
            m.TurDetayBayraklari = y.W(0x13);
            m.TurDetay = TurDetayAdlari(m.TurDetayBayraklari);

            int hiz = y.W(0x15);
            if (hiz == 0xFFFF) hiz = (int)(y.D(0x54) & 0x7FFFFFFF);
            m.HizMTs = hiz;
            string ureticiHam = y.S(0x17);
            m.UreticiHam = ureticiHam;
            m.Uretici = DoldurmalikMi(ureticiHam) ? "" : ureticiHam;
            m.SeriNo = Temiz(y.S(0x18));
            m.VarlikEtiketi = Temiz(y.S(0x19));
            m.ParcaNo = Temiz(y.S(0x1A));
            m.Rank = y.B(0x1B) & 0x0F;
            int yh = y.W(0x20);
            if (yh == 0xFFFF) yh = (int)(y.D(0x58) & 0x7FFFFFFF);
            m.YapilandirilmisHizMTs = yh;
            m.MinVoltajMv = y.W(0x22);
            m.MaxVoltajMv = y.W(0x24);
            m.YapilandirilmisVoltajMv = y.W(0x26);
            m.TeknolojiKodu = y.B(0x28);
            m.Teknoloji = m.TeknolojiKodu == 0 ? "" : TeknolojiAdi(m.TeknolojiKodu);
            m.ModulUreticiId = y.W(0x2C);
            m.ModulUrunId = y.W(0x2E);

            // Boyut
            string yer = "\"" + (m.Yuva.Length > 0 ? m.Yuva : "handle 0x" + m.Handle.ToString("X4")) + "\"";
            if (!y.Var(0x0C, 2))
            {
                BilinmeyenBoyut(m, notlar, yer + " yuvasının boyut alanı yok (kısa yapı).");
            }
            else
            {
                int boyut = y.W(0x0C);
                if (boyut == 0)
                {
                    m.BoyutMB = 0; m.Dolu = false;
                }
                else if (boyut == 0xFFFF)
                {
                    BilinmeyenBoyut(m, notlar, yer + " yuvasının boyutu BIOS'ta 'bilinmiyor' (0xFFFF).");
                }
                else if (boyut == 0x7FFF)
                {
                    if (y.Var(0x1C, 4))
                    {
                        m.BoyutMB = y.D(0x1C) & 0x7FFFFFFF;
                        m.Dolu = m.BoyutMB > 0;
                    }
                    else
                    {
                        m.BoyutMB = 0x7FFF; m.Dolu = true;
                        notlar.Add("SMBIOS: " + yer + " boyutu genişletilmiş alana işaret ediyor ama alan yok; en az 32 GB kabul edildi.");
                    }
                }
                else if ((boyut & 0x8000) != 0)
                {
                    long kb = boyut & 0x7FFF;
                    m.BoyutMB = (kb + 1023) / 1024; // KB birimi; 1 MB'ye yuvarla
                    m.Dolu = kb > 0;
                }
                else
                {
                    m.BoyutMB = boyut;
                    m.Dolu = true;
                }
            }
            return m;
        }

        static void BilinmeyenBoyut(BellekYuvasi m, List<string> notlar, string neden)
        {
            m.BoyutMB = 0;
            bool kanit = m.ParcaNo.Length > 0 || m.Uretici.Length > 0 || m.SeriNo.Length > 0;
            m.Dolu = kanit;
            notlar.Add("SMBIOS: " + neden + (kanit ? " Parça/üretici bilgisi olduğu için dolu sayıldı." : " Boş sayıldı."));
        }

        // ---- Doldurmalık değerler ---------------------------------------------

        static readonly string[] doldurmaliklar = new string[] {
            "NOT SPECIFIED", "NOTSPECIFIED", "UNKNOWN", "NONE", "NO DIMM", "NODIMM", "EMPTY", "[EMPTY]", "N/A", "NA",
            "MODULE PART NUMBER", "MODULEPARTNUMBER", "MODULE SERIAL NUMBER", "TO BE FILLED BY O.E.M.", "TO BE FILLED BY OEM",
            "DEFAULT STRING", "DEFAULT", "UNDEFINED", "NOT AVAILABLE", "NOT APPLICABLE", "NOT PRESENT", "NOT INSTALLED",
            "SERIAL NUMBER", "SERIALNUMBER", "PART NUMBER", "PARTNUMBER", "ASSET TAG", "ASSETTAG", "ASSET TAG NUMBER",
            "NO ASSET TAG", "NO ASSET INFORMATION", "MANUFACTURER", "UNKNOWN MANUFACTURER", "OEM", "O.E.M.", "XXXXX",
            "SYSTEM SERIAL NUMBER", "SYSTEM PRODUCT NAME", "SYSTEM MANUFACTURER", "BASE BOARD SERIAL NUMBER", "INVALID", "NULL", "-",
            "9876543210", "0123456789", "1234567890"   // BIOS varsayılan varlık etiketi/seri dolgusu (ör. Lenovo tip 17 varlık etiketi)
        };

        static readonly string[] doldurmalikOnekleri = new string[] {
            "PARTNUM", "SERNUM", "ASSETTAGNUM", "MANUFACTURER", "MODULE PART", "MODULE SER", "ARRAY1", "ARRAY 1",
            "DIMM PART", "DIMM SER", "SERIAL NUM", "PART NUM", "ASSET TAG NUM"
        };

        /// <summary>SMBIOS'ta "boş" anlamına gelen doldurmalık metin mi? (Not Specified, Unknown,
        /// NO DIMM, [Empty], PartNum0, SerNum1, 00000000, FFFFFFFF ...)</summary>
        public static bool DoldurmalikMi(string s)
        {
            if (s == null) return true;
            string u = s.Trim().ToUpperInvariant();
            if (u.Length == 0) return true;
            foreach (string d in doldurmaliklar) if (u == d) return true;
            // Önek + sadece rakam (PartNum0, SerNum12, Manufacturer00, ArrayMfr...)
            foreach (string o in doldurmalikOnekleri)
            {
                if (u.StartsWith(o, StringComparison.Ordinal))
                {
                    string kalan = u.Substring(o.Length).Trim();
                    bool rakam = true;
                    foreach (char c in kalan) if (!(char.IsDigit(c) || c == ' ' || c == '_' || c == '-')) { rakam = false; break; }
                    if (rakam) return true;
                    // "Array1 Manufacturer", "Array1 PartNumber0" gibi AMI kalıpları
                    if (o.StartsWith("ARRAY", StringComparison.Ordinal)) return true;
                }
            }
            // Tamamı aynı karakter: 0000, FFFF, ...., ____ , 0xFF dolgu (ÿ)
            bool hepsiAyni = true;
            char ilk = u[0];
            foreach (char c in u) if (c != ilk) { hepsiAyni = false; break; }
            if (hepsiAyni && (ilk == '0' || ilk == 'F' || ilk == '.' || ilk == '_' || ilk == '-' || ilk == '*' || ilk == ' ' || ilk == 'ÿ' || ilk == 'X'))
                return true;
            return false;
        }

        static string Temiz(string s)
        {
            if (s == null) return "";
            s = s.Trim();
            return DoldurmalikMi(s) ? "" : s;
        }

        // ---- Kod -> ad tabloları (DSP0134 3.7) ---------------------------------

        static string Tablo(string[] adlar, int kod)
        {
            if (kod >= 0 && kod < adlar.Length && adlar[kod] != null) return adlar[kod];
            return "Bilinmeyen (0x" + kod.ToString("X2") + ")";
        }

        static readonly string[] bellekTurleri = new string[] {
            null, "Other", "Unknown", "DRAM", "EDRAM", "VRAM", "SRAM", "RAM", "ROM", "FLASH", "EEPROM", "FEPROM", "EPROM",
            "CDRAM", "3DRAM", "SDRAM", "SGRAM", "RDRAM", "DDR", "DDR2", "DDR2 FB-DIMM", null, null, null, "DDR3", "FBD2",
            "DDR4", "LPDDR", "LPDDR2", "LPDDR3", "LPDDR4", "Logical non-volatile device", "HBM", "HBM2", "DDR5", "LPDDR5", "HBM3"
        };

        /// <summary>Tip 17 Memory Type (7.18.2): 0x1A DDR4, 0x22 DDR5, 0x23 LPDDR5 ...</summary>
        public static string BellekTuruAdi(int kod)
        {
            if (kod >= 0x15 && kod <= 0x17) return "Reserved (0x" + kod.ToString("X2") + ")";
            return Tablo(bellekTurleri, kod);
        }

        static readonly string[] formFaktorleri = new string[] {
            null, "Other", "Unknown", "SIMM", "SIP", "Chip", "DIP", "ZIP", "Proprietary Card", "DIMM", "TSOP",
            "Row of chips", "RIMM", "SODIMM", "SRIMM", "FB-DIMM", "Die", "CAMM"
        };

        /// <summary>Tip 17 Form Factor (7.18.1): 0x09 DIMM, 0x0D SODIMM, 0x0B Row of chips, 0x11 CAMM ...</summary>
        public static string FormFaktorAdi(int kod) { return Tablo(formFaktorleri, kod); }

        static readonly string[] turDetaylari = new string[] {
            "Reserved", "Other", "Unknown", "Fast-paged", "Static column", "Pseudo-static", "RAMBUS", "Synchronous",
            "CMOS", "EDO", "Window DRAM", "Cache DRAM", "Non-volatile", "Registered (Buffered)", "Unbuffered (Unregistered)", "LRDIMM"
        };

        /// <summary>Tip 17 Type Detail bit alanı (7.18.3) -> İngilizce adlar (bit 0 ayrılmış, atlanır).</summary>
        public static List<string> TurDetayAdlari(int bayraklar)
        {
            List<string> l = new List<string>();
            for (int b = 1; b < 16; b++)
                if ((bayraklar & (1 << b)) != 0) l.Add(turDetaylari[b]);
            return l;
        }

        static readonly string[] teknolojiler = new string[] {
            null, "Other", "Unknown", "DRAM", "NVDIMM-N", "NVDIMM-F", "NVDIMM-P", "Intel Optane persistent memory"
        };

        /// <summary>Tip 17 Memory Technology (7.18.6).</summary>
        public static string TeknolojiAdi(int kod) { return Tablo(teknolojiler, kod); }

        static readonly string[] diziKonumlari = new string[] {
            null, "Other", "Unknown", "System board or motherboard", "ISA add-on card", "EISA add-on card",
            "PCI add-on card", "MCA add-on card", "PCMCIA add-on card", "Proprietary add-on card", "NuBus"
        };

        /// <summary>Tip 16 Location (7.17.1).</summary>
        public static string DiziKonumuAdi(int kod)
        {
            switch (kod)
            {
                case 0xA0: return "PC-98/C20 add-on card";
                case 0xA1: return "PC-98/C24 add-on card";
                case 0xA2: return "PC-98/E add-on card";
                case 0xA3: return "PC-98/Local bus add-on card";
                case 0xA4: return "CXL add-on card";
            }
            return Tablo(diziKonumlari, kod);
        }

        static readonly string[] diziKullanimlari = new string[] {
            null, "Other", "Unknown", "System memory", "Video memory", "Flash memory", "Non-volatile RAM", "Cache memory"
        };

        /// <summary>Tip 16 Use (7.17.2).</summary>
        public static string DiziKullanimAdi(int kod) { return Tablo(diziKullanimlari, kod); }

        static readonly string[] eccTurleri = new string[] {
            null, "Other", "Unknown", "None", "Parity", "Single-bit ECC", "Multi-bit ECC", "CRC"
        };

        /// <summary>Tip 16 Memory Error Correction (7.17.3).</summary>
        public static string EccAdi(int kod) { return Tablo(eccTurleri, kod); }

        static readonly string[] kasaTurleri = new string[] {
            null, "Other", "Unknown", "Desktop", "Low Profile Desktop", "Pizza Box", "Mini Tower", "Tower", "Portable",
            "Laptop", "Notebook", "Hand Held", "Docking Station", "All in One", "Sub Notebook", "Space-saving",
            "Lunch Box", "Main Server Chassis", "Expansion Chassis", "SubChassis", "Bus Expansion Chassis",
            "Peripheral Chassis", "RAID Chassis", "Rack Mount Chassis", "Sealed-case PC", "Multi-system chassis",
            "Compact PCI", "AdvancedTCA", "Blade", "Blade Enclosure", "Tablet", "Convertible", "Detachable",
            "IoT Gateway", "Embedded PC", "Mini PC", "Stick PC"
        };

        /// <summary>Tip 3 Chassis Type (7.4.1) adı. Kilit biti (bit 7) maskelenir.</summary>
        public static string KasaTuruAdi(int kod) { return Tablo(kasaTurleri, kod & 0x7F); }

        /// <summary>SMBIOS kasa kodu -> KasaTipi.</summary>
        public static KasaTipi KasaTipiBul(int kod)
        {
            switch (kod & 0x7F)
            {
                case 3: case 4: case 5: case 6: case 7: case 15: case 24:
                    return KasaTipi.Masaustu;
                case 16: case 33: case 34: case 35: case 36:
                    return KasaTipi.MiniPc;
                case 8: case 9: case 10: case 14: case 31:
                    return KasaTipi.Dizustu;
                case 13:
                    return KasaTipi.HepsiBirArada;
                case 17: case 23: case 25: case 28: case 29:
                    return KasaTipi.Sunucu;
                case 11: case 30: case 32:
                    return KasaTipi.Tablet;
                default:
                    return KasaTipi.Bilinmiyor;
            }
        }
    }
}
