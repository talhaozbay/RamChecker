// RAM Danışmanı — SMBIOS ayrıştırıcı testleri ve sentetik SMBIOS tablo kurucusu.
// SmbiosKurucu diğer modüllerin testlerinde de kullanılabilir (Toplayici, Danisman ...).
// C# 5 uyumlu.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace RamDanismani
{
    // ==== Sentetik SMBIOS kurucusu =================================================

    /// <summary>Tek bir SMBIOS yapısını bayt bayt yazar. Ofsetler DSP0134'teki gibi yapı
    /// başlığından (Type) itibaren verilir; Length dışına düşen yazmalar sessizce yok sayılır.</summary>
    public class SmbiosYapiYazici
    {
        readonly byte[] veri;
        readonly List<string> dizgiler = new List<string>();

        public SmbiosYapiYazici(int tip, int uzunluk, int handle)
        {
            if (uzunluk < 4) uzunluk = 4;
            if (uzunluk > 255) uzunluk = 255;
            veri = new byte[uzunluk];
            veri[0] = (byte)tip;
            veri[1] = (byte)uzunluk;
            veri[2] = (byte)(handle & 0xFF);
            veri[3] = (byte)((handle >> 8) & 0xFF);
        }

        public int Uzunluk { get { return veri.Length; } }

        public SmbiosYapiYazici Bayt(int ofset, int deger)
        {
            if (ofset >= 4 && ofset < veri.Length) veri[ofset] = (byte)(deger & 0xFF);
            return this;
        }

        public SmbiosYapiYazici Kelime(int ofset, int deger)
        {
            Bayt(ofset, deger); Bayt(ofset + 1, deger >> 8);
            return this;
        }

        public SmbiosYapiYazici CiftKelime(int ofset, long deger)
        {
            for (int i = 0; i < 4; i++) Bayt(ofset + i, (int)((deger >> (8 * i)) & 0xFF));
            return this;
        }

        public SmbiosYapiYazici Dortlu(int ofset, long deger)
        {
            for (int i = 0; i < 8; i++) Bayt(ofset + i, (int)((deger >> (8 * i)) & 0xFF));
            return this;
        }

        /// <summary>String alanı: boş/null -> indeks 0; değilse string setine ekler ve 1 tabanlı indeksi yazar.
        /// Alan Length dışındaysa string eklenmez.</summary>
        public SmbiosYapiYazici Dizgi(int ofset, string deger)
        {
            if (ofset < 4 || ofset >= veri.Length) return this;
            if (string.IsNullOrEmpty(deger)) { veri[ofset] = 0; return this; }
            dizgiler.Add(deger);
            veri[ofset] = (byte)dizgiler.Count;
            return this;
        }

        /// <summary>Biçimli alan + string seti (çift NUL ile biter).</summary>
        public byte[] Bitir()
        {
            List<byte> l = new List<byte>(veri);
            if (dizgiler.Count == 0) { l.Add(0); l.Add(0); }
            else
            {
                foreach (string s in dizgiler) { l.AddRange(Encoding.ASCII.GetBytes(s)); l.Add(0); }
                l.Add(0);
            }
            return l.ToArray();
        }
    }

    /// <summary>Tip 17 tanımı (SmbiosKurucu.Modul için). Varsayılanlar: 3.3 uzunluğu (0x5C),
    /// boş DDR4 DIMM yuvası, 64/64 bit, Synchronous, DRAM.</summary>
    public class ModulTanimi
    {
        public int Uzunluk = 0x5C;          // 0x15 (2.1), 0x1B (2.3), 0x1C (2.6), 0x22 (2.7), 0x28 (2.8), 0x54 (3.2), 0x5C (3.3), 0x64 (3.7)
        public int Handle = 0x1100;
        public int DiziHandle = 0x1000;
        public long BoyutMB;                // 0 = boş yuva; >= 32767 MB otomatik genişletilmiş boyut
        public int BoyutHam = -1;           // >= 0 ise Size alanına aynen yazılır (0x8000|KB, 0xFFFF ...)
        public int FormKodu = 0x09;         // DIMM
        public int TurKodu = 0x1A;          // DDR4
        public int TurDetay = 0x0080;       // Synchronous
        public int Hiz;                     // > 0xFFFE ise genişletilmiş hız
        public int YapHiz;
        public string Yuva = "";
        public string Banka = "";
        public string Uretici = "";
        public string SeriNo = "";
        public string VarlikEtiketi = "";
        public string ParcaNo = "";
        public int Rank;
        public int VeriGenisligi = 64;
        public int ToplamGenislik = 64;
        public int MinMv;
        public int MaxMv;
        public int YapMv;
        public int Teknoloji = 3;
        public int ModulUreticiId;
        public int ModulUrunId;
    }

    public static class SmbiosKurucu
    {
        public static byte[] Bios(string uretici, string surum, string tarih, int handle = 0x0000)
        {
            SmbiosYapiYazici y = new SmbiosYapiYazici(0, 0x1A, handle);
            y.Dizgi(0x04, uretici).Dizgi(0x05, surum).Kelime(0x06, 0xE000).Dizgi(0x08, tarih).Bayt(0x09, 0xFF);
            return y.Bitir();
        }

        /// <summary>Tip 1: üretici, Product Name (Model), Version (ModelAdi), seri no.</summary>
        public static byte[] Sistem(string uretici, string urun, string surum, string seri, int handle = 0x0001)
        {
            SmbiosYapiYazici y = new SmbiosYapiYazici(1, 0x1B, handle);
            y.Dizgi(0x04, uretici).Dizgi(0x05, urun).Dizgi(0x06, surum).Dizgi(0x07, seri).Bayt(0x18, 6);
            return y.Bitir();
        }

        public static byte[] Anakart(string uretici, string urun, int handle = 0x0002)
        {
            SmbiosYapiYazici y = new SmbiosYapiYazici(2, 0x0F, handle);
            y.Dizgi(0x04, uretici).Dizgi(0x05, urun).Bayt(0x0D, 0x0A);
            return y.Bitir();
        }

        public static byte[] Kasa(int kod, int handle = 0x0003)
        {
            SmbiosYapiYazici y = new SmbiosYapiYazici(3, 0x16, handle);
            y.Bayt(0x05, kod).Bayt(0x09, 3).Bayt(0x0A, 3).Bayt(0x0B, 3).Bayt(0x0C, 3);
            return y.Bitir();
        }

        /// <summary>Tip 4 (3.0 uzunluğu 0x30). Çekirdek/izlek > 255 ise Core Count 2 / Thread Count 2 kullanılır.</summary>
        public static byte[] Islemci(string ad, int cekirdek, int izlek, int handle = 0x0004, bool takili = true)
        {
            SmbiosYapiYazici y = new SmbiosYapiYazici(4, 0x30, handle);
            y.Bayt(0x05, 3).Bayt(0x06, 0xCD).Dizgi(0x04, takili ? "CPU" + handle : "")
             .Dizgi(0x10, takili ? ad : "").Bayt(0x18, takili ? 0x41 : 0x00);
            y.Bayt(0x23, cekirdek > 255 ? 0xFF : cekirdek).Bayt(0x24, cekirdek > 255 ? 0xFF : cekirdek)
             .Bayt(0x25, izlek > 255 ? 0xFF : izlek).Kelime(0x28, 0x00FC).Kelime(0x26, 0x00CD)
             .Kelime(0x2A, cekirdek).Kelime(0x2C, cekirdek).Kelime(0x2E, izlek);
            return y.Bitir();
        }

        /// <summary>Tip 16. maxKapasiteMB >= 2 TB ise 0x80000000 + Extended Maximum Capacity (bayt) yazılır.</summary>
        public static byte[] Dizi(int handle, long maxKapasiteMB, int yuvaSayisi, int eccKodu = 3, int kullanimKodu = 3)
        {
            SmbiosYapiYazici y = new SmbiosYapiYazici(16, 0x17, handle);
            y.Bayt(0x04, 3).Bayt(0x05, kullanimKodu).Bayt(0x06, eccKodu).Kelime(0x0B, 0xFFFE).Kelime(0x0D, yuvaSayisi);
            long kb = maxKapasiteMB * 1024;
            if (kb >= 0x80000000L)
            {
                y.CiftKelime(0x07, 0x80000000L);
                y.Dortlu(0x0F, maxKapasiteMB * 1024L * 1024L);
            }
            else y.CiftKelime(0x07, kb);
            return y.Bitir();
        }

        /// <summary>Tip 17 — m.Uzunluk dışına düşen alanlar yazılmaz (eski SMBIOS sürümlerini taklit eder).</summary>
        public static byte[] Modul(ModulTanimi m)
        {
            SmbiosYapiYazici y = new SmbiosYapiYazici(17, m.Uzunluk, m.Handle);
            y.Kelime(0x04, m.DiziHandle).Kelime(0x06, 0xFFFE)
             .Kelime(0x08, m.ToplamGenislik == 0 ? 0xFFFF : m.ToplamGenislik)
             .Kelime(0x0A, m.VeriGenisligi == 0 ? 0xFFFF : m.VeriGenisligi);
            if (m.BoyutHam >= 0) y.Kelime(0x0C, m.BoyutHam);
            else if (m.BoyutMB >= 0x7FFF) { y.Kelime(0x0C, 0x7FFF); y.CiftKelime(0x1C, m.BoyutMB); }
            else y.Kelime(0x0C, (int)m.BoyutMB);
            y.Bayt(0x0E, m.FormKodu).Dizgi(0x10, m.Yuva).Dizgi(0x11, m.Banka).Bayt(0x12, m.TurKodu).Kelime(0x13, m.TurDetay);
            if (m.Hiz > 0xFFFE) { y.Kelime(0x15, 0xFFFF); y.CiftKelime(0x54, m.Hiz); }
            else y.Kelime(0x15, m.Hiz);
            y.Dizgi(0x17, m.Uretici).Dizgi(0x18, m.SeriNo).Dizgi(0x19, m.VarlikEtiketi).Dizgi(0x1A, m.ParcaNo).Bayt(0x1B, m.Rank);
            if (m.YapHiz > 0xFFFE) { y.Kelime(0x20, 0xFFFF); y.CiftKelime(0x58, m.YapHiz); }
            else y.Kelime(0x20, m.YapHiz);
            y.Kelime(0x22, m.MinMv).Kelime(0x24, m.MaxMv).Kelime(0x26, m.YapMv)
             .Bayt(0x28, m.Teknoloji).Kelime(0x29, 0x0008)
             .Kelime(0x2C, m.ModulUreticiId).Kelime(0x2E, m.ModulUrunId);
            if (m.BoyutMB > 0) y.Dortlu(0x3C, m.BoyutMB * 1024L * 1024L); // Volatile Size
            if (m.Uzunluk >= 0x64) y.Kelime(0x5C, 0x8A85).Kelime(0x5E, 0x0012).Kelime(0x60, 0x3286).Kelime(0x62, 0x0021); // PMIC0 / RCD (3.7)
            return y.Bitir();
        }

        /// <summary>Herhangi bir yapı: govde = 4 baytlık başlıktan sonraki biçimli alan.</summary>
        public static byte[] Yapi(int tip, int handle, byte[] govde, params string[] dizgiler)
        {
            int uz = 4 + (govde == null ? 0 : govde.Length);
            List<byte> l = new List<byte>();
            l.Add((byte)tip); l.Add((byte)uz); l.Add((byte)(handle & 0xFF)); l.Add((byte)(handle >> 8));
            if (govde != null) l.AddRange(govde);
            if (dizgiler == null || dizgiler.Length == 0) { l.Add(0); l.Add(0); }
            else
            {
                foreach (string s in dizgiler) { l.AddRange(Encoding.ASCII.GetBytes(s)); l.Add(0); }
                l.Add(0);
            }
            return l.ToArray();
        }

        /// <summary>Tip 127 (tablo sonu).</summary>
        public static byte[] Son(int handle = 0xFEFF)
        {
            return new SmbiosYapiYazici(127, 4, handle).Bitir();
        }

        /// <summary>Yapıları birleştirir; son yapı tip 127 değilse ekler.</summary>
        public static byte[] Tablo(params byte[][] yapilar)
        {
            List<byte> l = new List<byte>();
            byte[] son = null;
            foreach (byte[] y in yapilar) { if (y == null) continue; l.AddRange(y); son = y; }
            if (son == null || son[0] != 127) l.AddRange(Son());
            return l.ToArray();
        }

        /// <summary>8 baytlık RawSMBIOSData başlığı ekler (GetSystemFirmwareTable / --smbios-kaydet biçimi).</summary>
        public static byte[] Baslikli(byte[] tablo, int major, int minor)
        {
            byte[] c = new byte[8 + tablo.Length];
            c[0] = 0; c[1] = (byte)major; c[2] = (byte)minor; c[3] = 0;
            Array.Copy(BitConverter.GetBytes((uint)tablo.Length), 0, c, 4, 4);
            Array.Copy(tablo, 0, c, 8, tablo.Length);
            return c;
        }

        // ---- Hazır örnek tablolar ------------------------------------------------

        /// <summary>Gerçek Lenovo LOQ 15IAX9E (83LK) SMBIOS tablosu (başlıksız; SMBIOS 3.4).
        /// 1 × 16 GB DDR5-5600 SODIMM (4800'de), BIOS 1 yuva / azami 16 GB bildiriyor (fiziksel 2 yuva var).</summary>
        public static byte[] LenovoLoq()
        {
            return Convert.FromBase64String(LenovoLoqB64);
        }

        /// <summary>4 yuvalı masaüstü: ASUS PRIME B460M-A, i5-10400, DDR4 DIMM; DIMM_A2 ve DIMM_B2'de
        /// 2 × 16 GB Kingston KF432C16BB/16 (3200, 2666'da), DIMM_A1/B1 boş (boyut 0). Azami 128 GB.</summary>
        public static byte[] Masaustu4YuvaDdr4()
        {
            return Tablo(
                Bios("American Megatrends Inc.", "1401", "07/20/2021"),
                Sistem("ASUS", "System Product Name", "System Version", "System Serial Number"),
                Anakart("ASUSTeK COMPUTER INC.", "PRIME B460M-A"),
                Kasa(3),
                Islemci("Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz", 6, 12),
                Dizi(0x1000, 128 * 1024, 4),
                Modul(DdrDimm(0x1100, "DIMM_A1", "BANK 0", 0)),
                Modul(DdrDimm(0x1101, "DIMM_A2", "BANK 1", 16384)),
                Modul(DdrDimm(0x1102, "DIMM_B1", "BANK 2", 0)),
                Modul(DdrDimm(0x1103, "DIMM_B2", "BANK 3", 16384)));
        }

        static ModulTanimi DdrDimm(int handle, string yuva, string banka, long mb)
        {
            ModulTanimi m = new ModulTanimi();
            m.Handle = handle; m.Yuva = yuva; m.Banka = banka; m.BoyutMB = mb;
            if (mb > 0)
            {
                m.TurDetay = 0x4080; // Synchronous + Unbuffered
                m.Hiz = 3200; m.YapHiz = 2666; m.Uretici = "Kingston"; m.ParcaNo = "KF432C16BB/16";
                m.SeriNo = "1A2B3C4D"; m.Rank = 2; m.MinMv = 1200; m.MaxMv = 1350; m.YapMv = 1200;
                m.ModulUreticiId = 0x9801;
            }
            else
            {
                m.TurDetay = 0x0004; // Unknown
                m.Uretici = "Unknown"; m.ParcaNo = "NO DIMM"; m.SeriNo = "Not Specified"; m.VarlikEtiketi = "Not Specified";
                m.VeriGenisligi = 0; m.ToplamGenislik = 0;
            }
            return m;
        }

        /// <summary>İnce dizüstü, anakarta lehimli LPDDR5 (Row of chips): i7-1355U, 2 × 8 GB Samsung 6400 (5200'de).</summary>
        public static byte[] DizustuLpddr5Lehimli()
        {
            ModulTanimi a = new ModulTanimi();
            a.Handle = 0x1100; a.Yuva = "Controller0-ChannelA"; a.Banka = "BANK 0"; a.BoyutMB = 8192; a.FormKodu = 0x0B;
            a.TurKodu = 0x23; a.TurDetay = 0x4080; a.Hiz = 6400; a.YapHiz = 5200; a.Uretici = "Samsung";
            a.ParcaNo = "K3LKBKB0BM-MGCP"; a.VeriGenisligi = 16; a.ToplamGenislik = 16; a.Rank = 1;
            a.MinMv = 500; a.MaxMv = 500; a.YapMv = 500; a.ModulUreticiId = 0xCE80;
            ModulTanimi b = new ModulTanimi();
            b.Handle = 0x1101; b.Yuva = "Controller1-ChannelA"; b.Banka = "BANK 0"; b.BoyutMB = 8192; b.FormKodu = 0x0B;
            b.TurKodu = 0x23; b.TurDetay = 0x4080; b.Hiz = 6400; b.YapHiz = 5200; b.Uretici = "Samsung";
            b.ParcaNo = "K3LKBKB0BM-MGCP"; b.VeriGenisligi = 16; b.ToplamGenislik = 16; b.Rank = 1;
            b.MinMv = 500; b.MaxMv = 500; b.YapMv = 500; b.ModulUreticiId = 0xCE80;
            return Tablo(
                Bios("LENOVO", "R2HET30W (1.12 )", "03/14/2024"),
                Sistem("LENOVO", "21HD0001TX", "ThinkPad T14 Gen 4", "PF4XXXXX"),
                Anakart("LENOVO", "21HD0001TX"),
                Kasa(10),
                Islemci("13th Gen Intel(R) Core(TM) i7-1355U", 10, 12),
                Dizi(0x1000, 64 * 1024, 2),
                Modul(a), Modul(b));
        }

        /// <summary>Çift soketli sunucu: 2 × Xeon (16 çekirdek), ECC RDIMM DDR4-2933 (72/64 bit), 2 dolu / 2 boş, Multi-bit ECC.</summary>
        public static byte[] SunucuEccRdimm()
        {
            List<byte[]> l = new List<byte[]>();
            l.Add(Sistem("Dell Inc.", "PowerEdge R740", "", "ABC1234"));
            l.Add(Kasa(0x17 | 0x80)); // Rack Mount, kilitli
            l.Add(Islemci("Intel(R) Xeon(R) Silver 4216 CPU @ 2.10GHz", 16, 32, 0x0400));
            l.Add(Islemci("Intel(R) Xeon(R) Silver 4216 CPU @ 2.10GHz", 16, 32, 0x0401));
            l.Add(Dizi(0x1000, 1536 * 1024, 4, 6));
            for (int i = 0; i < 4; i++)
            {
                ModulTanimi m = new ModulTanimi();
                m.Handle = 0x1100 + i; m.Yuva = "A" + (i + 1); m.Banka = "";
                if (i < 2)
                {
                    m.BoyutMB = 32768; m.TurDetay = 0x2080; m.Hiz = 2933; m.YapHiz = 2933; m.Uretici = "00AD063200AD";
                    m.ParcaNo = "HMA84GR7CJR4N-WM"; m.SeriNo = "12345678"; m.Rank = 2;
                    m.VeriGenisligi = 64; m.ToplamGenislik = 72; m.MinMv = 1200; m.MaxMv = 1200; m.YapMv = 1200;
                    m.ModulUreticiId = 0xAD80;
                }
                else
                {
                    m.BoyutMB = 0; m.TurDetay = 0x0004; m.VeriGenisligi = 0; m.ToplamGenislik = 0; m.TurKodu = 0x02;
                }
                l.Add(Modul(m));
            }
            return Tablo(l.ToArray());
        }

        // Gerçek tablo (testler\veri\lenovo-loq-15iax9e-83LK.bin), exe konumundan bağımsız olsun diye gömülü.
        const string LenovoLoqB64 =
            "ABoAAAECAOAD/4CY+UsAABoAAw0BEQEOEABMRU5PVk8AUThDTjE3V1cAMDEvMjMvMjAyNQAAARsBAAECAwQAAAAAAAAAAAAAAAAAAAAABgUGTE" +
            "VOT1ZPADgzTEsATE9RIDE1SUFYOUUAVEVTVDAwMDEATEVOT1ZPX01UXzgzTEtfQlVfaWRlYV9GTV9MT1EgMTVJQVg5RQBMT1EgMTVJQVg5RQAA" +
            "Ag8CAAECAwQFCQYDAAoATEVOT1ZPAExOVk5CMTYxMjE2ACBOTyBEUEsAVEVTVDAwMDEATk8gQXNzZXQgVGFnAFR5cGUyIC0gQm9hcmQgQ2hhc3" +
            "NpcyBMb2NhdGlvbgAAAxYDAAEKAgMEAwMDAwAAAAAAAQAABUxFTk9WTwBMT1EgMTVJQVg5RQBURVNUMDAwMQBOTyBBc3NldCBUYWcAU0tVIE51" +
            "bWJlcgAABDAEAAEDzQJyBgkA//vrvwOMZAAwET4QQUAKAAsADAAEBQYICAz8AM0ACAAIAAwAVTNFMQBJbnRlbChSKSBDb3Jwb3JhdGlvbgAxMn" +
            "RoIEdlbiBJbnRlbChSKSBDb3JlKFRNKSBpNS0xMjQ1MEhYAFRvIEJlIEZpbGxlZCBCeSBPLkUuTS4AVG8gQmUgRmlsbGVkIEJ5IE8uRS5NLgBU" +
            "byBCZSBGaWxsZWQgQnkgTy5FLk0uAAAHGwUAAYABwADAACAAIAAABAQJwAAAAMAAAABMMSBDYWNoZQAABxsGAAGAAYAAgAAgACAAAAQDB4AAAA" +
            "CAAAAATDEgQ2FjaGUAAAcbBwABgQEAFAAUIAAgAAAFBQEAFAAAABQAAEwyIENhY2hlAAAHGwgAAYIBADAAMCAAIAAABgUHADAAAAAwAABMMyBD" +
            "YWNoZQAABxsJAAGAAYAAgAAgACAAAAQEB4AAAACAAAAATDEgQ2FjaGUAAAcbCgABgAEAAQABIAAgAAAEAwcAAQAAAAEAAEwxIENhY2hlAAAHGw" +
            "sAAYEBAAgACCAAIAAABQUIAAgAAAAIAABMMiBDYWNoZQAABxsMAAGCAQAwADAgACAAAAYFBwAwAAAAMAAATDMgQ2FjaGUAAAgJDQABAAIPDUox" +
            "QTEAS2V5Ym9hcmQAAAgJDgABAAIPDkoxQTEATW91c2UAAAgJDwABAAINHEoyQTEAVFYgT1VUAAAICRAAAQACBxxKMkEyAENSVAAACAkRAAEAAg" +
            "gJSjJBMgBDT00gMQAACAkSAAEAAhIQSjNBMQBVU0IAAAgJEwABAAISEEozQTEAVVNCAAAICRQAAQACEhBKM0ExAFVTQgAACAkVAAEAAhIQSjVB" +
            "MQBVU0IAAAgJFgABAAISEEo1QTEAVVNCAAAICRcAAQACEhBKNUEyAFVTQgAACAkYAAEAAgsfSjVBMQBOZXR3b3JrAAAICRkAARcCAP9KOUcyAE" +
            "9uQm9hcmQgRmxvcHB5IFR5cGUAAAgJGgABFgIA/0o3SjEAT25Cb2FyZCBQcmltYXJ5IElERQAACAkbAAEAAh8dSjMwAE1pY3JvcGhvbmUgSW4A" +
            "AAgJHAABAAIfHUozMABMaW5lIEluAAAICR0AAQACHx1KMzAAU3BlYWtlciBPdXQAAAkTHgABpggDAQEAAAMAAADgCABKNkMxAAAJEx8AAaYIAw" +
            "ECAAADAAAA4QgASjZEMgAACRMgAAGmCAMBAwAAAwAAAOIIAEo3QzEAAAkTIQABpggDAQQAAAMAAADjCABKN0QxAAAJEyIAAagKAwEFAAADAAAA" +
            "5AoASjhDMQAACwUjAAFNb2Rlcm4gUHJlbG9hZAAADAUkAANDb25maWdPcHRpb25zMQBDb25maWdPcHRpb25zMgBDb25maWdPcHRpb25zMwAADR" +
            "YlAAgAAAAAAAAAAAAAAAAAAAAAAWVufFVTfGlzbzg4NTktMSwwAGZyfEZSfGlzbzg4NTktMSwwAHpofFRXfHVuaWNvZGUsMABqYXxKUHx1bmlj" +
            "b2RlLDAAaXR8SVR8aXNvODg1OS0xLDAAZXN8RVN8aXNvODg1OS0xLDAAZGV8REV8aXNvODg1OS0xLDAAcHR8UFR8aXNvODg1OS0xLDAAAA4IJg" +
            "AB2z0AJE1FSQAADhdGAAHdPgDdPwDdQADdQQDdQgDdQwBGaXJtd2FyZSBWZXJzaW9uIEluZm8AAA8dJwAAAAAAACAEAXhWNBIAAAAAgAMCBwAI" +
            "BBYAAAAQFygAAwMDAAAAAf7/AQAAAAAAAAAAAAAAEVwpACgA/v9AAEAAAEANAAECIoAA4BUDBAUGAQAAAADAEgAAAABMBAMIAACALAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABDb250cm9sbGVyMC1DaGFubmVsQS1ESU1NMABCQU5LIDAATWljcm9uIFRl" +
            "Y2hub2xvZ3kAMUEyQjNDNEQAOTg3NjU0MzIxMABNVEM4QzEwODRTMVNDNTZCRDEgSwAAEx8qAAAAAAD///8AKAABAAAAAAAAAAAAAAAAAAAAAA" +
            "AAFCMrAAAAAAD///8AKQAqAP8BAQAAAAAAAAAAAAAAAAAAAAAAABUHLAAHBAQAABYaLQABAgMEBQcAAAAAAP8AAAAABgAAAAAARmFrZQAtVmly" +
            "dHVhbCBCYXR0ZXJ5IDAtADA4LzA4LzIwMTAAQmF0dGVyeSAwAENSQiBCYXR0ZXJ5IDAATGl0aGl1bVBvbHltZXIAABgFLgAAAAAaGC8AAUIAgA" +
            "CAAIAAgACAAIAAAAAAAIBWb2x0YWdlIFByb2JlIERlc2NyaXB0aW9uAAAbDzAAMQBjAAAAAAAAIAFDb29saW5nIERldmljZSBEZXNjcmlwdGlv" +
            "bgAAHBgxAAFCAIAAgACAAIAAgACAAAAAAACAVGVtcGVyYXR1cmUgUHJvYmUgRGVzY3JpcHRpb24AACALMgAAAAAAAAAAAAAnFjMAAAECAwQFBg" +
            "dLAKQh////////T0VNIERlZmluZSAwAE9FTSBEZWZpbmUgMQBPRU0gRGVmaW5lIDIAT0VNIERlZmluZSAzAE9FTSBEZWZpbmUgNABPRU0gRGVm" +
            "aW5lIDUAT0VNIERlZmluZSA2AAAoETQAAgYSAAUBqgYAAAUCAFBDSUV4cHJlc3N4MTYAQ29tcGlsZXIgVmVyc2lvbjogVkMgOS4wAAApCzUAAQ" +
            "MBAAAAEElHRAAAKx82AElOVEMCABIAWALPCB4AAQQAAAAAAAAAAAAAAFRQTSBEZXZpY2UAAIAINwBVqlWqT2VtIFR5cGUgMTI4IFRlc3QgMQBP" +
            "ZW0gVHlwZSAxMjggVGVzdCAyAACBCDgAAQECAUluc3lkZV9BU0ZfMDAxAEluc3lkZV9BU0ZfMDAyAACDQDkAMQAAAAAAAAAAAAAA+ACMegAAAA" +
            "AhIAAAAQAQAM8IHgAAAAAA/gD//wAAAAAAAAAAIgAAAHZQcm8AAAAAAACFBToAAVhYWFhYWFhYWFhYWFhYAACIBjsA//8AAMgQPAABAgEoARQB" +
            "VbKAAQVJZGVhUGFkADgzTEswMDhKVFIAANtqPQABBAFFAgCQBgGFMCAAAAAAQAAAAx8gAskDYEQC////////////////////////////////Aw" +
            "AAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABNRUkxAE1FSTIATUVJMwBNRUk0AADdDD4AAQEABAAAAABG" +
            "U1AgQmluYXJ5IFZlcnNpb24AAN0aPwADAQAMALoQAAIAAAAALgADAP//////UmVmZXJlbmNlIENvZGUgLSBDUFUAdUNvZGUgVmVyc2lvbgBUWF" +
            "QgQUNNIHZlcnNpb24AAN0aQAADAQAMALoQAAIAAAAAAAADBBABHs8IUmVmZXJlbmNlIENvZGUgLSBNRQBNRUJ4IHZlcnNpb24ATUUgRmlybXdh" +
            "cmUgVmVyc2lvbgBDb25zdW1lciBTS1UAAN0vQQAGAQAMALoQAAID//////8EAP///xEABQD///8RAAYAAkYAAAAHAAQAAAAAUmVmZXJlbmNlIE" +
            "NvZGUgLSBQQ0gAUENILUNSSUQgU3RhdHVzAERpc2FibGVkAFBDSC1DUklEIE9yaWdpbmFsIFZhbHVlAFBDSC1DUklEIE5ldyBWYWx1ZQBPUFJP" +
            "TSAtIFJTVCAtIFJBSUQAUENIIEhzaW8gVmVyc2lvbgAA3VJCAAsBAAwAuhAAAgAAAAS4AAMADAC6EAAEBf//////BgAAAAACAAcAAAAAAgAIAP" +
            "//////CQD//////woA//////8LAP//////DAD//////1JlZmVyZW5jZSBDb2RlIC0gU0EgLSBTeXN0ZW0gQWdlbnQAUmVmZXJlbmNlIENvZGUg" +
            "LSBNUkMAU0EgLSBQQ0llIFZlcnNpb24AU0EtQ1JJRCBTdGF0dXMARGlzYWJsZWQAU0EtQ1JJRCBPcmlnaW5hbCBWYWx1ZQBTQS1DUklEIE5ldy" +
            "BWYWx1ZQBPUFJPTSAtIFZCSU9TAElPIE1hbmFnZWFiaWxpdHkgRW5naW5lIEZXIFZlcnNpb24AUEhZIEJ1aWxkIFZlcnNpb24AVGh1bmRlcmJv" +
            "bHQoVE0pIEZXIFZlcnNpb24AU3lzdGVtIEFnZW50IE1hbmFnZWFiaWxpdHkgRW5naW5lIEZXIFZlcnNpb24AAN1SQwALAQD//////wIA//////" +
            "8DBP//////BQb//////wcI//////8JAP//////CgD//////wsAAQ4AAAAMAAAXAIEQDQD//////w4AAAcAAABMYW4gUGh5IFZlcnNpb24AU2Vu" +
            "c29yIEZpcm13YXJlIFZlcnNpb24ARGVidWcgTW9kZSBTdGF0dXMARGlzYWJsZWQAUGVyZm9ybWFuY2UgTW9kZSBTdGF0dXMARGlzYWJsZWQARG" +
            "VidWcgVXNlIFVTQihEaXNhYmxlZDpTZXJpYWwpAERpc2FibGVkAElDQyBPdmVyY2xvY2tpbmcgVmVyc2lvbgBVTkRJIFZlcnNpb24ARUMgRlcg" +
            "VmVyc2lvbgBHT1AgVmVyc2lvbgBSb3lhbCBQYXJrIFZlcnNpb24AUGxhdGZvcm0gVmVyc2lvbgAA3hVEAAEAViUDbKOrdOYTkuOjzNfoTENGQw" +
            "AA+BJFAKysqhcAAAAAAAAAAAAAAAB/BP/+AAA=" +            "";
    }

    // ==== Testler ===================================================================

    public static class SmbiosTestleri
    {
        public static void Calistir(TestKosucu t)
        {
            t.Grup("SMBIOS — gerçek tablo: Lenovo LOQ 15IAX9E (83LK)");
            t.Kos("Lenovo", delegate { Lenovo(t); });
            t.Grup("SMBIOS — başlıklı dosya / DosyadanOku");
            t.Kos("Dosya", delegate { Dosya(t); });
            t.Grup("SMBIOS — 4 yuvalı masaüstü DDR4");
            t.Kos("Masaustu", delegate { Masaustu(t); });
            t.Grup("SMBIOS — lehimli LPDDR5 (Row of chips)");
            t.Kos("Lpddr5", delegate { Lpddr5(t); });
            t.Grup("SMBIOS — boyut kodlamaları");
            t.Kos("Boyutlar", delegate { Boyutlar(t); });
            t.Kos("Kesik son dizgi", delegate { KesikSonDizgi(t); });
            t.Grup("SMBIOS — eski sürüm kısa tip 17 (DDR3L)");
            t.Kos("Eski", delegate { EskiSurum(t); });
            t.Grup("SMBIOS — ECC RDIMM sunucu");
            t.Kos("Sunucu", delegate { Sunucu(t); });
            t.Grup("SMBIOS — tip 16 genişletilmiş azami kapasite");
            t.Kos("Tip16", delegate { Tip16Genis(t); });
            t.Grup("SMBIOS — doldurmalık değerler ve model adı");
            t.Kos("Doldurmalik", delegate { Doldurmalik(t); });
            t.Grup("SMBIOS — string indeksi aralık dışı");
            t.Kos("Indeks", delegate { IndeksDisi(t); });
            t.Grup("SMBIOS — bozuk/kesik tablolar");
            t.Kos("Bozuk", delegate { Bozuk(t); });
            t.Grup("SMBIOS — kod tabloları ve diğer");
            t.Kos("Tablolar", delegate { Tablolar(t); });
            t.Kos("Digerleri", delegate { Digerleri(t); });
        }

        static BellekYuvasi Bul(SmbiosSonucu s, string yuva)
        {
            foreach (BellekYuvasi y in s.Yuvalar) if (y.Yuva == yuva) return y;
            return null;
        }

        static bool NotVar(SmbiosSonucu s, string parca)
        {
            foreach (string n in s.Notlar) if (n.IndexOf(parca, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        static void Lenovo(TestKosucu t)
        {
            byte[] tablo = SmbiosKurucu.LenovoLoq();
            t.Esit(3905, tablo.Length, "gömülü tablo uzunluğu");
            SmbiosSonucu s = Smbios.Coz(tablo, 3, 4);
            SistemBilgisi si = s.Sistem;
            t.Esit("3.4", si.SmbiosSurum, "SMBIOS sürümü");
            t.Esit("LENOVO", si.Uretici, "üretici");
            t.Esit("83LK", si.Model, "model (Product Name)");
            t.Esit("LOQ 15IAX9E", si.ModelAdi, "model adı (Version)");
            t.Esit("TEST0001", si.SeriNo, "seri no");
            t.Esit("Q8CN17WW", si.BiosSurum, "BIOS sürümü");
            t.Esit("01/23/2025", si.BiosTarih, "BIOS tarihi");
            t.Esit("LENOVO", si.AnakartUretici, "anakart üreticisi");
            t.Esit("LNVNB161216", si.AnakartModel, "anakart modeli");
            t.Esit(0x0A, si.KasaTuruKodu, "kasa kodu 0x0A");
            t.Esit(KasaTipi.Dizustu, si.Kasa, "kasa: dizüstü");
            t.Esit("Notebook", Smbios.KasaTuruAdi(si.KasaTuruKodu), "kasa adı");
            t.Esit("12th Gen Intel(R) Core(TM) i5-12450HX", si.IslemciAdi, "işlemci adı");
            t.Esit(8, si.CekirdekSayisi, "çekirdek");
            t.Esit(12, si.IsParcacigiSayisi, "iş parçacığı");

            t.Esit(1, s.Diziler.Count, "tip 16 sayısı");
            BellekDizisi d = s.Diziler[0];
            t.Esit(0x0028, d.Handle, "dizi handle");
            t.Esit(16384L, d.MaxKapasiteMB, "azami kapasite 16 GB (BIOS)");
            t.Esit(1, d.YuvaSayisi, "BIOS yuva sayısı 1");
            t.Esit(3, d.EccKodu, "ECC kodu 3");
            t.Esit("None", d.EccTuru, "ECC yok");
            t.Esit(3, d.KullanimKodu, "kullanım kodu");
            t.Esit("System memory", d.Kullanim, "kullanım adı");
            t.Esit("System board or motherboard", d.Konum, "konum");

            t.Esit(1, s.Yuvalar.Count, "tek tip 17");
            BellekYuvasi y = s.Yuvalar[0];
            t.Esit("Controller0-ChannelA-DIMM0", y.Yuva, "yuva adı");
            t.Esit("BANK 0", y.Banka, "banka");
            t.Dogru(y.Dolu, "dolu");
            t.Esit(16384L, y.BoyutMB, "16 GB");
            t.Esit(0x0D, y.FormFaktorKodu, "form kodu");
            t.Esit("SODIMM", y.FormFaktor, "SODIMM");
            t.Esit(0x22, y.TurKodu, "tür kodu");
            t.Esit("DDR5", y.Tur, "DDR5");
            t.Esit(0x0080, y.TurDetayBayraklari, "tür detay bayrakları");
            t.Dogru(y.TurDetay.Contains("Synchronous") && y.TurDetay.Count == 1, "tür detay: Synchronous");
            t.Esit(5600, y.HizMTs, "hız 5600");
            t.Esit(4800, y.YapilandirilmisHizMTs, "yapılandırılmış 4800");
            t.Esit("Micron Technology", y.Uretici, "üretici ham (normalleştirme Toplayici'de)");
            t.Esit("Micron Technology", y.UreticiHam, "üretici ham");
            t.Esit("MTC8C1084S1SC56BD1 K", y.ParcaNo, "parça no");
            t.Esit("1A2B3C4D", y.SeriNo, "modül seri no");
            t.Esit(1100, y.YapilandirilmisVoltajMv, "1.1 V");
            t.Esit(0, y.MinVoltajMv, "min voltaj bilinmiyor");
            t.Esit(64, y.VeriGenisligi, "veri genişliği");
            t.Esit(64, y.ToplamGenislik, "toplam genişlik");
            t.Dogru(!y.Ecc, "ECC değil");
            t.Dogru(!y.Kayitli, "Registered değil");
            t.Esit(1, y.Rank, "rank 1");
            t.Esit(3, y.TeknolojiKodu, "teknoloji kodu");
            t.Esit("DRAM", y.Teknoloji, "teknoloji DRAM");
            t.Esit(0x2C80, y.ModulUreticiId, "JEDEC modül üretici kimliği (Micron 80 2C)");
            t.Esit(0x0028, y.DiziHandle, "bağlı dizi");
            t.Esit("SMBIOS", y.Kaynak, "kaynak");
            t.Esit(0, s.Notlar.Count, "not yok (" + string.Join(" | ", s.Notlar.ToArray()) + ")");

            SmbiosSonucu s0 = Smbios.Coz(tablo, 0, 0);
            t.Esit("", s0.Sistem.SmbiosSurum, "major=0 ise sürüm boş");
            t.Esit(1, s0.Yuvalar.Count, "major=0 olsa da tip 17 okunur");
            t.Esit(4800, s0.Yuvalar[0].YapilandirilmisHizMTs, "major=0: alanlar Length'e göre");
        }

        static string GeciciDosya(byte[] veri)
        {
            string yol = Path.Combine(Path.GetTempPath(), "ramdanismani-test-" + Guid.NewGuid().ToString("N") + ".bin");
            File.WriteAllBytes(yol, veri);
            return yol;
        }

        static void Dosya(TestKosucu t)
        {
            byte[] tablo = SmbiosKurucu.LenovoLoq();
            int major, minor;
            string yol = GeciciDosya(SmbiosKurucu.Baslikli(tablo, 3, 4));
            try
            {
                byte[] okunan = Smbios.DosyadanOku(yol, out major, out minor);
                t.Esit(3, major, "başlıklı dosya: major");
                t.Esit(4, minor, "başlıklı dosya: minor");
                t.Dogru(okunan != null && okunan.Length == tablo.Length, "başlık ayıklandı");
                t.Esit("LOQ 15IAX9E", Smbios.Coz(okunan, major, minor).Sistem.ModelAdi, "başlıklı dosyadan çözüm");
            }
            finally { try { File.Delete(yol); } catch (Exception) { } }

            yol = GeciciDosya(tablo);
            try
            {
                byte[] okunan = Smbios.DosyadanOku(yol, out major, out minor);
                t.Esit(0, major, "başlıksız dosya: major 0");
                t.Dogru(okunan != null && okunan.Length == tablo.Length, "başlıksız dosya: tamamı tablo");
            }
            finally { try { File.Delete(yol); } catch (Exception) { } }

            byte[] yok = Smbios.DosyadanOku(Path.Combine(Path.GetTempPath(), "yok-" + Guid.NewGuid().ToString("N") + ".bin"), out major, out minor);
            t.Dogru(yok == null, "olmayan dosya: null (istisna yok)");
        }

        static void Masaustu(TestKosucu t)
        {
            SmbiosSonucu s = Smbios.Coz(SmbiosKurucu.Masaustu4YuvaDdr4(), 3, 3);
            t.Esit(KasaTipi.Masaustu, s.Sistem.Kasa, "masaüstü kasa");
            t.Esit("", s.Sistem.Model, "\"System Product Name\" doldurmalık");
            t.Esit("", s.Sistem.ModelAdi, "\"System Version\" model adı değil");
            t.Esit("", s.Sistem.SeriNo, "\"System Serial Number\" doldurmalık");
            t.Esit("PRIME B460M-A", s.Sistem.AnakartModel, "anakart");
            t.Esit("1401", s.Sistem.BiosSurum, "BIOS");
            t.Esit(6, s.Sistem.CekirdekSayisi, "6 çekirdek");
            t.Esit(4, s.Yuvalar.Count, "4 yuva (boşlar dahil)");
            t.Esit(4, s.Diziler[0].YuvaSayisi, "dizi 4 yuva");
            t.Esit(131072L, s.Diziler[0].MaxKapasiteMB, "azami 128 GB");
            int dolu = 0; long toplam = 0;
            foreach (BellekYuvasi y in s.Yuvalar) if (y.Dolu) { dolu++; toplam += y.BoyutMB; }
            t.Esit(2, dolu, "2 dolu");
            t.Esit(32768L, toplam, "toplam 32 GB");
            BellekYuvasi a1 = Bul(s, "DIMM_A1"), a2 = Bul(s, "DIMM_A2");
            t.Dogru(a1 != null && !a1.Dolu && a1.BoyutMB == 0, "DIMM_A1 boş");
            t.Esit("", a1.ParcaNo, "boş yuva: NO DIMM -> boş");
            t.Esit("", a1.SeriNo, "boş yuva: Not Specified -> boş");
            t.Esit("", a1.VarlikEtiketi, "boş yuva varlık etiketi boş");
            t.Esit("", a1.Uretici, "boş yuva: Unknown üretici -> boş");
            t.Esit("Unknown", a1.UreticiHam, "ham üretici korunur");
            t.Esit(0, a1.VeriGenisligi, "0xFFFF genişlik -> 0");
            t.Dogru(a2 != null && a2.Dolu, "DIMM_A2 dolu");
            t.Esit("DDR4", a2.Tur, "DDR4");
            t.Esit("DIMM", a2.FormFaktor, "DIMM");
            t.Esit("BANK 1", a2.Banka, "banka");
            t.Esit("KF432C16BB/16", a2.ParcaNo, "parça no");
            t.Esit("Kingston", a2.Uretici, "üretici");
            t.Esit(3200, a2.HizMTs, "3200");
            t.Esit(2666, a2.YapilandirilmisHizMTs, "2666'da çalışıyor");
            t.Esit(2, a2.Rank, "çift rank");
            t.Esit(1350, a2.MaxVoltajMv, "max 1.35 V");
            t.Esit(1200, a2.YapilandirilmisVoltajMv, "1.2 V");
            t.Dogru(a2.TurDetay.Contains("Unbuffered (Unregistered)") && a2.TurDetay.Contains("Synchronous"), "Unbuffered + Synchronous");
            t.Dogru(!a2.Kayitli, "Unbuffered -> Kayitli değil");
            t.Esit(0x9801, a2.ModulUreticiId, "JEDEC kimliği");
            t.Esit(0, s.Notlar.Count, "not yok (" + string.Join(" | ", s.Notlar.ToArray()) + ")");
        }

        static void Lpddr5(TestKosucu t)
        {
            SmbiosSonucu s = Smbios.Coz(SmbiosKurucu.DizustuLpddr5Lehimli(), 3, 5);
            t.Esit("ThinkPad T14 Gen 4", s.Sistem.ModelAdi, "model adı");
            t.Esit("21HD0001TX", s.Sistem.Model, "model");
            t.Esit(KasaTipi.Dizustu, s.Sistem.Kasa, "dizüstü");
            t.Esit(2, s.Yuvalar.Count, "2 kanal");
            BellekYuvasi y = s.Yuvalar[0];
            t.Esit("LPDDR5", y.Tur, "LPDDR5");
            t.Esit(0x23, y.TurKodu, "0x23");
            t.Esit("Row of chips", y.FormFaktor, "Row of chips");
            t.Esit(0x0B, y.FormFaktorKodu, "0x0B");
            t.Esit(8192L, y.BoyutMB, "8 GB");
            t.Esit(16, y.VeriGenisligi, "16 bit kanal");
            t.Esit(6400, y.HizMTs, "6400");
            t.Esit(5200, y.YapilandirilmisHizMTs, "5200");
            t.Esit(500, y.YapilandirilmisVoltajMv, "0.5 V");
            t.Esit("Samsung", y.Uretici, "Samsung");
            t.Dogru(!y.Lehimli, "Lehimli kararını Toplayici verir (Smbios işaretlemez)");
        }

        static void KesikSonDizgi(TestKosucu t)
        {
            ModulTanimi m = new ModulTanimi();
            m.Yuva = "DIMM A"; m.BoyutMB = 8192; m.FormKodu = 0x0D; m.Hiz = 3200; m.YapHiz = 3200; m.ParcaNo = "M471A1K43DB1-CWE";
            byte[] tam = SmbiosKurucu.Tablo(SmbiosKurucu.Modul(m));
            byte[] kesik = new byte[tam.Length - 12];   // tip 127 (6 bayt) + "-CWE" ve iki NUL
            Array.Copy(tam, kesik, kesik.Length);
            SmbiosSonucu s = Smbios.Coz(kesik, 3, 3);
            t.Esit(1, s.Yuvalar.Count, "kesik yapı yine okunur");
            t.Esit("DIMM A", s.Yuvalar[0].Yuva, "tam dizgiler korunur");
            t.Esit("", s.Yuvalar[0].ParcaNo, "yarım kalan son dizgi (parça no) atılır");
            t.Dogru(NotVar(s, "Son string atıldı"), "atıldı notu");
        }

        static void Boyutlar(TestKosucu t)
        {
            ModulTanimi m = new ModulTanimi();
            m.Yuva = "DIMM0"; m.BoyutMB = 65536; m.TurKodu = 0x22; m.Hiz = 5600; m.YapHiz = 5600; m.ParcaNo = "KF556C40BB-64";
            SmbiosSonucu s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Modul(m)), 3, 3);
            t.Esit(65536L, s.Yuvalar[0].BoyutMB, "0x7FFF -> genişletilmiş boyut 64 GB");
            t.Dogru(s.Yuvalar[0].Dolu, "64 GB dolu");

            m = new ModulTanimi();
            m.Yuva = "DIMM0"; m.BoyutHam = 0x7FFF; m.Uzunluk = 0x1C; m.ParcaNo = "X";
            s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Modul(m)), 2, 6);
            t.Dogru(s.Yuvalar[0].Dolu && s.Yuvalar[0].BoyutMB == 0x7FFF, "0x7FFF ama genişletilmiş alan yok -> 32767 MB");
            t.Dogru(NotVar(s, "genişletilmiş"), "not düşüldü");

            m = new ModulTanimi();
            m.Yuva = "CHIP0"; m.BoyutHam = 0x8000 | 8192; // 8192 KB = 8 MB
            s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Modul(m)), 3, 0);
            t.Esit(8L, s.Yuvalar[0].BoyutMB, "KB birimli boyut (bit15) -> 8 MB");
            t.Dogru(s.Yuvalar[0].Dolu, "KB boyutlu dolu");

            m = new ModulTanimi();
            m.Yuva = "CHIP1"; m.BoyutHam = 0x8000 | 512; // 512 KB
            s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Modul(m)), 3, 0);
            t.Esit(1L, s.Yuvalar[0].BoyutMB, "512 KB -> 1 MB'ye yuvarlanır");

            m = new ModulTanimi();
            m.Yuva = "DIMM1"; m.BoyutHam = 0xFFFF; m.ParcaNo = "M378A2K43EB1-CWE"; m.Uretici = "Samsung";
            s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Modul(m)), 3, 0);
            t.Dogru(s.Yuvalar[0].Dolu && s.Yuvalar[0].BoyutMB == 0, "0xFFFF + parça no -> dolu, boyut bilinmiyor");
            t.Dogru(NotVar(s, "bilinmiyor"), "bilinmeyen boyut notu");

            m = new ModulTanimi();
            m.Yuva = "DIMM2"; m.BoyutHam = 0xFFFF; m.ParcaNo = "Not Specified";
            s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Modul(m)), 3, 0);
            t.Dogru(!s.Yuvalar[0].Dolu, "0xFFFF + kanıt yok -> boş");

            m = new ModulTanimi();
            m.Yuva = "DIMM3"; m.BoyutMB = 49152; m.TurKodu = 0x22; m.Hiz = 8800; m.YapHiz = 70000; m.ParcaNo = "X";
            s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Modul(m)), 3, 3);
            t.Esit(49152L, s.Yuvalar[0].BoyutMB, "48 GB (genişletilmiş)");
            t.Esit(8800, s.Yuvalar[0].HizMTs, "8800 normal alan");
            t.Esit(70000, s.Yuvalar[0].YapilandirilmisHizMTs, "0xFFFF -> genişletilmiş yapılandırılmış hız");
            m.Hiz = 70000;
            s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Modul(m)), 3, 3);
            t.Esit(70000, s.Yuvalar[0].HizMTs, "0xFFFF -> genişletilmiş hız");

            m = new ModulTanimi();
            m.Yuva = "DIMM4"; m.Uzunluk = 0x64; m.BoyutMB = 32768; m.TurKodu = 0x22; m.FormKodu = 0x11; m.Hiz = 6400; m.ParcaNo = "CAMM2";
            s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Modul(m), SmbiosKurucu.Sistem("Dell Inc.", "Precision 7680", "", "")), 3, 7);
            t.Esit(1, s.Yuvalar.Count, "3.7 tip 17 (PMIC/RCD alanlı) okunur");
            t.Esit("CAMM", s.Yuvalar[0].FormFaktor, "CAMM form");
            t.Esit(32768L, s.Yuvalar[0].BoyutMB, "3.7: 32 GB");
            t.Esit("Precision 7680", s.Sistem.ModelAdi, "sonraki yapı da okunur; Version boş -> Model");
        }

        static void EskiSurum(TestKosucu t)
        {
            ModulTanimi m = new ModulTanimi();
            m.Uzunluk = 0x22; m.Yuva = "ChannelA-DIMM0"; m.Banka = "BANK 0"; m.FormKodu = 0x0D; m.TurKodu = 0x18;
            m.BoyutMB = 4096; m.Hiz = 1600; m.YapHiz = 1600; m.Uretici = "Hynix/Hyundai"; m.ParcaNo = "HMT451S6BFR8A-PB  ";
            m.MinMv = 1350; m.MaxMv = 1350; m.YapMv = 1350; m.ModulUreticiId = 0xAD80; m.Rank = 1;
            byte[] tablo = SmbiosKurucu.Tablo(SmbiosKurucu.Kasa(9), SmbiosKurucu.Modul(m));
            SmbiosSonucu s = Smbios.Coz(tablo, 2, 7);
            t.Esit(1, s.Yuvalar.Count, "2.7 tip 17");
            BellekYuvasi y = s.Yuvalar[0];
            t.Esit("DDR3", y.Tur, "DDR3");
            t.Esit("SODIMM", y.FormFaktor, "SODIMM");
            t.Esit(4096L, y.BoyutMB, "4 GB");
            t.Esit(1600, y.YapilandirilmisHizMTs, "2.7: yapılandırılmış hız var");
            t.Esit(0, y.YapilandirilmisVoltajMv, "Length 0x22: voltaj alanları yok -> 0");
            t.Esit(0, y.MinVoltajMv, "min voltaj yok");
            t.Esit(0, y.TeknolojiKodu, "teknoloji alanı yok");
            t.Esit("", y.Teknoloji, "teknoloji adı boş");
            t.Esit(0, y.ModulUreticiId, "JEDEC alanı yok");
            t.Esit("HMT451S6BFR8A-PB", y.ParcaNo, "parça no kırpıldı");
            t.Esit(1, y.Rank, "rank");
            t.Esit(KasaTipi.Dizustu, s.Sistem.Kasa, "Laptop (9) -> dizüstü");

            m.Uzunluk = 0x28;
            s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Modul(m)), 2, 8);
            t.Esit(1350, s.Yuvalar[0].YapilandirilmisVoltajMv, "2.8: DDR3L 1.35 V");
            t.Esit(1350, s.Yuvalar[0].MinVoltajMv, "2.8: min 1.35 V");
            t.Esit(0, s.Yuvalar[0].TeknolojiKodu, "2.8: teknoloji alanı yok");

            m.Uzunluk = 0x15; // SMBIOS 2.1: hız/üretici/parça yok
            s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Modul(m)), 2, 1);
            t.Esit(4096L, s.Yuvalar[0].BoyutMB, "2.1: boyut okunur");
            t.Esit(0, s.Yuvalar[0].HizMTs, "2.1: hız yok");
            t.Esit("", s.Yuvalar[0].ParcaNo, "2.1: parça no yok");
            t.Esit(0, s.Yuvalar[0].Rank, "2.1: rank bilinmiyor");
        }

        static void Sunucu(TestKosucu t)
        {
            SmbiosSonucu s = Smbios.Coz(SmbiosKurucu.SunucuEccRdimm(), 3, 2);
            t.Esit(0x17, s.Sistem.KasaTuruKodu, "kilit biti maskelendi (0x97 -> 0x17)");
            t.Esit(KasaTipi.Sunucu, s.Sistem.Kasa, "rack -> sunucu");
            t.Esit("PowerEdge R740", s.Sistem.ModelAdi, "Version boş -> Model");
            t.Esit("Intel(R) Xeon(R) Silver 4216 CPU @ 2.10GHz", s.Sistem.IslemciAdi, "işlemci");
            t.Esit(32, s.Sistem.CekirdekSayisi, "2 soket toplam 32 çekirdek");
            t.Esit(64, s.Sistem.IsParcacigiSayisi, "64 iş parçacığı");
            t.Esit("Multi-bit ECC", s.Diziler[0].EccTuru, "Multi-bit ECC");
            t.Esit(6, s.Diziler[0].EccKodu, "ECC kodu 6");
            t.Esit(1572864L, s.Diziler[0].MaxKapasiteMB, "1.5 TB");
            t.Esit(4, s.Yuvalar.Count, "4 yuva");
            BellekYuvasi y = s.Yuvalar[0];
            t.Dogru(y.Ecc, "72/64 -> ECC");
            t.Dogru(y.Kayitli, "Registered");
            t.Dogru(y.TurDetay.Contains("Registered (Buffered)"), "Registered (Buffered) adı");
            t.Esit(72, y.ToplamGenislik, "72 bit");
            t.Esit("00AD063200AD", y.UreticiHam, "ham JEDEC metni");
            t.Esit(0xAD80, y.ModulUreticiId, "JEDEC kimliği");
            t.Dogru(!s.Yuvalar[2].Dolu && !s.Yuvalar[3].Dolu, "boş yuvalar");
            t.Esit("Unknown", s.Yuvalar[2].Tur, "boş yuva tür Unknown");

            // takılı olmayan ikinci soket sayılmaz
            byte[] tek = SmbiosKurucu.Tablo(
                SmbiosKurucu.Islemci("", 0, 0, 0x0400, false),
                SmbiosKurucu.Islemci("AMD Ryzen Threadripper PRO 7995WX 96-Cores", 96, 192, 0x0401),
                SmbiosKurucu.Islemci("", 0, 0, 0x0402, false));
            s = Smbios.Coz(tek, 3, 6);
            t.Esit("AMD Ryzen Threadripper PRO 7995WX 96-Cores", s.Sistem.IslemciAdi, "ilk DOLU soketin adı");
            t.Esit(96, s.Sistem.CekirdekSayisi, "boş soketler sayılmaz");
            s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Islemci("Intel(R) Xeon(R) 6980P", 128, 256)), 3, 6);
            t.Esit(128, s.Sistem.CekirdekSayisi, "0xFF -> Core Count 2");
            t.Esit(256, s.Sistem.IsParcacigiSayisi, "0xFF -> Thread Count 2");
        }

        static void Tip16Genis(TestKosucu t)
        {
            SmbiosSonucu s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Dizi(0x1000, 2L * 1024 * 1024, 8, 5)), 3, 0);
            t.Esit(1, s.Diziler.Count, "dizi");
            t.Esit(2097152L, s.Diziler[0].MaxKapasiteMB, "0x80000000 -> Extended Maximum Capacity 2 TB");
            t.Esit(8, s.Diziler[0].YuvaSayisi, "8 yuva");
            t.Esit("Single-bit ECC", s.Diziler[0].EccTuru, "Single-bit ECC");
            t.Dogru(NotVar(s, "8 yuva bildiriyor"), "yuva sayısı uyuşmazlık notu");

            s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Dizi(0x1000, 4L * 1024 * 1024 * 1024, 16)), 3, 0);
            t.Esit(4L * 1024 * 1024 * 1024, s.Diziler[0].MaxKapasiteMB, "4 PB");

            // Use = Flash memory dizisi ve ona bağlı aygıt atlanır
            ModulTanimi m = new ModulTanimi();
            m.DiziHandle = 0x2000; m.Yuva = "FLASH"; m.BoyutMB = 16; m.TurKodu = 0x09;
            ModulTanimi r = new ModulTanimi();
            r.Yuva = "DIMM0"; r.BoyutMB = 8192;
            s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Dizi(0x2000, 16, 1, 3, 5), SmbiosKurucu.Dizi(0x1000, 32768, 1),
                SmbiosKurucu.Modul(m), SmbiosKurucu.Modul(r)), 3, 0);
            t.Esit(1, s.Diziler.Count, "flash dizisi atlandı");
            t.Esit(1, s.Yuvalar.Count, "flash aygıtı atlandı");
            t.Esit("DIMM0", s.Yuvalar[0].Yuva, "RAM yuvası kaldı");
        }

        static void Doldurmalik(TestKosucu t)
        {
            CultureInfo eski = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                string[] bos = new string[] { "Not Specified", "not specified", "Unknown", "None", "NO DIMM", "Empty", "[Empty]",
                    "Module Part Number", "To Be Filled By O.E.M.", "PartNum0", "SerNum3", "AssetTagNum1", "Manufacturer00",
                    "Array1 Manufacturer", "0000000000000000", "FFFFFFFF", "Default string", "invalid", "  ", "", null, "N/A" };
                foreach (string b in bos) t.Dogru(Smbios.DoldurmalikMi(b), "doldurmalık (tr-TR): \"" + b + "\"");
                string[] gercek = new string[] { "Micron Technology", "80CE", "KF432C16BB/16", "MTC8C1084S1SC56BD1 K", "Kingston",
                    "SK Hynix", "Unifosa", "1A2B3C4D", "Samsung", "0x2C00" };
                foreach (string g in gercek) t.Dogru(!Smbios.DoldurmalikMi(g), "gerçek değer: \"" + g + "\"");

                // tr-TR kültüründe de "DIMM", "Unbuffered" vb. doğru ayrıştırılır
                SmbiosSonucu s = Smbios.Coz(SmbiosKurucu.LenovoLoq(), 3, 4);
                t.Esit("DDR5", s.Yuvalar[0].Tur, "tr-TR: DDR5");
                t.Esit("LOQ 15IAX9E", s.Sistem.ModelAdi, "tr-TR: model adı");
            }
            finally { Thread.CurrentThread.CurrentCulture = eski; }

            string[][] surumler = new string[][] {
                new string[] { "None", "20XW" }, new string[] { "1.0", "20XW" }, new string[] { "V1.02", "20XW" },
                new string[] { "Rev 1.xx", "20XW" }, new string[] { "To Be Filled By O.E.M.", "20XW" },
                new string[] { "ThinkPad X1 Carbon Gen 9", "ThinkPad X1 Carbon Gen 9" }, new string[] { "V15 G4 IRU", "V15 G4 IRU" },
                new string[] { "IdeaPad 5 14ALC05", "IdeaPad 5 14ALC05" } };
            foreach (string[] sv in surumler)
            {
                SmbiosSonucu s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Sistem("LENOVO", "20XW", sv[0], "")), 3, 4);
                t.Esit(sv[1], s.Sistem.ModelAdi, "Version \"" + sv[0] + "\" -> ModelAdi");
            }
        }

        static void IndeksDisi(TestKosucu t)
        {
            ModulTanimi m = new ModulTanimi();
            m.Yuva = "DIMM_A1"; m.BoyutMB = 8192; m.ParcaNo = "CT8G4SFRA32A.C8FE"; m.Uretici = "Crucial";
            byte[] y = SmbiosKurucu.Modul(m);
            y[0x1A] = 9; // parça no string indeksi: sadece 3 string var
            SmbiosSonucu s = Smbios.Coz(SmbiosKurucu.Tablo(y), 3, 0);
            t.Esit(1, s.Yuvalar.Count, "yuva yine de okunur");
            t.Esit("", s.Yuvalar[0].ParcaNo, "aralık dışı indeks -> boş");
            t.Esit("Crucial", s.Yuvalar[0].Uretici, "diğer stringler doğru");
            t.Esit(8192L, s.Yuvalar[0].BoyutMB, "boyut doğru");
            t.Dogru(NotVar(s, "aralık dışı"), "not düşüldü");
        }

        static void Bozuk(TestKosucu t)
        {
            byte[] tam = SmbiosKurucu.LenovoLoq();
            bool istisna = false, hepsiSonuc = true, kesikNotu = false;
            for (int n = 0; n <= tam.Length; n++)
            {
                byte[] k = new byte[n];
                Array.Copy(tam, k, n);
                try
                {
                    SmbiosSonucu s = Smbios.Coz(k, 3, 4);
                    if (s == null || s.Sistem == null || s.Yuvalar == null) hepsiSonuc = false;
                    if (n > 0 && n < tam.Length - 6 && s.Notlar.Count == 0) { /* yapı sınırında kesilmiş olabilir */ }
                    if (NotVar(s, "kesik") || NotVar(s, "sonlanmamış")) kesikNotu = true;
                }
                catch (Exception) { istisna = true; }
            }
            t.Dogru(!istisna, "her uzunlukta kesik tablo istisnasız");
            t.Dogru(hepsiSonuc, "her zaman sonuç nesnesi döner");
            t.Dogru(kesikNotu, "kesiklik notlara yazılır");

            // tip 17'nin ortasından kesilmiş: önceki yapılar korunur
            int tip17 = 0x690;
            byte[] yarim = new byte[tip17 + 0x20];
            Array.Copy(tam, yarim, yarim.Length);
            SmbiosSonucu sy = Smbios.Coz(yarim, 3, 4);
            t.Esit("LOQ 15IAX9E", sy.Sistem.ModelAdi, "kesik tablo: önceki yapılar okundu");
            t.Esit(0, sy.Yuvalar.Count, "kesik tip 17 eklenmedi");

            // rastgele bayt bozma
            Random r = new Random(12345);
            istisna = false;
            for (int d = 0; d < 400; d++)
            {
                byte[] b = (byte[])tam.Clone();
                int adet = 1 + r.Next(20);
                for (int i = 0; i < adet; i++) b[r.Next(b.Length)] = (byte)r.Next(256);
                try { Smbios.Coz(b, 3, 4); } catch (Exception) { istisna = true; }
            }
            t.Dogru(!istisna, "rastgele bozulmuş tablolar istisnasız");
            istisna = false;
            for (int d = 0; d < 200; d++)
            {
                byte[] b = new byte[r.Next(600)];
                r.NextBytes(b);
                try { Smbios.Coz(b, 0, 0); } catch (Exception) { istisna = true; }
            }
            t.Dogru(!istisna, "tamamen rastgele veri istisnasız");

            SmbiosSonucu sn = Smbios.Coz(null, 0, 0);
            t.Dogru(sn != null && sn.Yuvalar.Count == 0 && sn.Notlar.Count > 0, "null tablo -> boş sonuç + not");
            sn = Smbios.Coz(new byte[0], 3, 4);
            t.Dogru(sn != null && sn.Notlar.Count > 0, "boş tablo -> not");

            byte[] kotu = new byte[] { 17, 2, 0, 0, 0, 0 }; // Length < 4
            sn = Smbios.Coz(kotu, 3, 4);
            t.Dogru(NotVar(sn, "geçersiz yapı uzunluğu"), "Length < 4 -> durur ve not düşer");

            byte[] sonsuz = new byte[] { 17, 0x5C, 0, 0 }; // Length tablodan uzun
            sn = Smbios.Coz(sonsuz, 3, 4);
            t.Dogru(sn.Yuvalar.Count == 0 && NotVar(sn, "kesik"), "Length taşması -> not");

            byte[] sonsuzDizgi = SmbiosKurucu.Sistem("ACME", "X1", "", "");
            Array.Resize(ref sonsuzDizgi, sonsuzDizgi.Length - 2); // çift NUL yok
            sn = Smbios.Coz(sonsuzDizgi, 3, 4);
            t.Dogru(NotVar(sn, "sonlanmamış"), "string bölgesi sonlanmamış -> not");
            t.Esit("ACME", sn.Sistem.Uretici, "sonlanmamış bölgede tamamlanmış stringler okunur");

            // Tip 127'den sonrası okunmaz
            byte[] sonra = SmbiosKurucu.Tablo(SmbiosKurucu.Son(), SmbiosKurucu.Kasa(3));
            sn = Smbios.Coz(sonra, 3, 4);
            t.Esit(0, sn.Sistem.KasaTuruKodu, "tip 127 sonrası yok sayılır");
        }

        static void Tablolar(TestKosucu t)
        {
            t.Esit("DDR", Smbios.BellekTuruAdi(0x12), "0x12 DDR");
            t.Esit("DDR2", Smbios.BellekTuruAdi(0x13), "0x13 DDR2");
            t.Esit("DDR3", Smbios.BellekTuruAdi(0x18), "0x18 DDR3");
            t.Esit("DDR4", Smbios.BellekTuruAdi(0x1A), "0x1A DDR4");
            t.Esit("LPDDR", Smbios.BellekTuruAdi(0x1B), "0x1B LPDDR");
            t.Esit("LPDDR3", Smbios.BellekTuruAdi(0x1D), "0x1D LPDDR3");
            t.Esit("LPDDR4", Smbios.BellekTuruAdi(0x1E), "0x1E LPDDR4");
            t.Esit("Logical non-volatile device", Smbios.BellekTuruAdi(0x1F), "0x1F");
            t.Esit("HBM", Smbios.BellekTuruAdi(0x20), "0x20 HBM");
            t.Esit("HBM2", Smbios.BellekTuruAdi(0x21), "0x21 HBM2");
            t.Esit("DDR5", Smbios.BellekTuruAdi(0x22), "0x22 DDR5");
            t.Esit("LPDDR5", Smbios.BellekTuruAdi(0x23), "0x23 LPDDR5");
            t.Esit("HBM3", Smbios.BellekTuruAdi(0x24), "0x24 HBM3");
            t.Esit("SDRAM", Smbios.BellekTuruAdi(0x0F), "0x0F SDRAM");
            t.Icerir(Smbios.BellekTuruAdi(0x16), "Reserved", "0x16 ayrılmış");
            t.Icerir(Smbios.BellekTuruAdi(0x30), "0x30", "bilinmeyen kod");
            t.Esit("DIMM", Smbios.FormFaktorAdi(0x09), "DIMM");
            t.Esit("SODIMM", Smbios.FormFaktorAdi(0x0D), "SODIMM");
            t.Esit("FB-DIMM", Smbios.FormFaktorAdi(0x0F), "FB-DIMM");
            t.Esit("Die", Smbios.FormFaktorAdi(0x10), "Die");
            t.Esit("CAMM", Smbios.FormFaktorAdi(0x11), "CAMM");
            List<string> d = Smbios.TurDetayAdlari(0xE000 | 0x0001);
            t.Dogru(d.Count == 3 && d.Contains("LRDIMM") && d.Contains("Registered (Buffered)") && d.Contains("Unbuffered (Unregistered)"),
                "tür detay: bit 0 atlanır, 13-15 adlandırılır");
            t.Esit(15, Smbios.TurDetayAdlari(0xFFFE).Count, "15 bit adı");
            t.Esit("Intel Optane persistent memory", Smbios.TeknolojiAdi(7), "teknoloji 7");
            t.Esit("CRC", Smbios.EccAdi(7), "ECC 7");
            t.Esit("Parity", Smbios.EccAdi(4), "ECC 4");
            t.Esit("Cache memory", Smbios.DiziKullanimAdi(7), "kullanım 7");
            t.Esit("CXL add-on card", Smbios.DiziKonumuAdi(0xA4), "konum A4");
            t.Esit("Stick PC", Smbios.KasaTuruAdi(0x24), "kasa 0x24");
            t.Esit("All in One", Smbios.KasaTuruAdi(0x8D), "kasa adı kilit biti maskeli");
            int[] masa = new int[] { 3, 4, 5, 6, 7, 15, 24 };
            foreach (int k in masa) t.Esit(KasaTipi.Masaustu, Smbios.KasaTipiBul(k), "kasa " + k + " masaüstü");
            int[] mini = new int[] { 16, 35, 36 };
            foreach (int k in mini) t.Esit(KasaTipi.MiniPc, Smbios.KasaTipiBul(k), "kasa " + k + " mini PC");
            int[] diz = new int[] { 8, 9, 10, 14, 31 };
            foreach (int k in diz) t.Esit(KasaTipi.Dizustu, Smbios.KasaTipiBul(k), "kasa " + k + " dizüstü");
            int[] sun = new int[] { 17, 23, 25, 28, 29 };
            foreach (int k in sun) t.Esit(KasaTipi.Sunucu, Smbios.KasaTipiBul(k), "kasa " + k + " sunucu");
            t.Esit(KasaTipi.HepsiBirArada, Smbios.KasaTipiBul(13), "kasa 13 AIO");
            t.Esit(KasaTipi.Tablet, Smbios.KasaTipiBul(30), "kasa 30 tablet");
            t.Esit(KasaTipi.Tablet, Smbios.KasaTipiBul(11), "kasa 11 el tipi");
            t.Esit(KasaTipi.Tablet, Smbios.KasaTipiBul(32), "kasa 32 detachable");
            t.Esit(KasaTipi.Bilinmiyor, Smbios.KasaTipiBul(2), "kasa 2 bilinmiyor");
        }

        static void Digerleri(TestKosucu t)
        {
            // Boş yuvayı hiç listelemeyen BIOS: tek tip 17, dizi 2 yuva diyor -> not
            ModulTanimi m = new ModulTanimi();
            m.Yuva = "DIMM A"; m.BoyutMB = 8192; m.FormKodu = 0x0D; m.ParcaNo = "M471A1K43DB1-CWE";
            SmbiosSonucu s = Smbios.Coz(SmbiosKurucu.Tablo(SmbiosKurucu.Dizi(0x1000, 32768, 2), SmbiosKurucu.Modul(m)), 3, 2);
            t.Esit(1, s.Yuvalar.Count, "listelenmeyen boş yuva uydurulmaz");
            t.Dogru(NotVar(s, "2 yuva bildiriyor, ancak 1 yuva listeleniyor"), "uyuşmazlık notu");

            // Yapıların içinde boş string seti (00 00) ve tablo sonu olmadan biten tablo
            byte[] sonsuz = new List<byte>(SmbiosKurucu.Kasa(10)).ToArray();
            s = Smbios.Coz(sonsuz, 3, 0);
            t.Esit(KasaTipi.Dizustu, s.Sistem.Kasa, "tip 127 olmadan biten tablo");

            // Yapi() genel kurucusu: OEM tipi atlanır
            byte[] oem = SmbiosKurucu.Yapi(0x85, 0x9000, new byte[] { 1, 2, 3 }, "OEM", "veri");
            s = Smbios.Coz(SmbiosKurucu.Tablo(oem, SmbiosKurucu.Kasa(13)), 3, 0);
            t.Esit(KasaTipi.HepsiBirArada, s.Sistem.Kasa, "OEM yapı sonrası okunur");
            t.Dogru(!NotVar(s, "kesik") && !NotVar(s, "geçersiz") && !NotVar(s, "aralık"), "OEM yapı hata notu üretmez");

            // Başlıklı tablo tespiti: başlığa benzemeyen tablo başlıklı sayılmaz
            byte[] lenovo = SmbiosKurucu.LenovoLoq();
            string yol = GeciciDosya(lenovo);
            try
            {
                int ma, mi;
                byte[] b = Smbios.DosyadanOku(yol, out ma, out mi);
                t.Esit("LOQ 15IAX9E", Smbios.Coz(b, ma, mi).Sistem.ModelAdi, "başlıksız gerçek döküm");
            }
            finally { try { File.Delete(yol); } catch (Exception) { } }

            // Canlı okuma istisna fırlatmaz (sonuç makineye bağlı)
            bool istisna = false;
            try
            {
                byte[] canli; int ma2, mi2;
                if (Smbios.HamOku(out canli, out ma2, out mi2))
                {
                    SmbiosSonucu c = Smbios.Coz(canli, ma2, mi2);
                    t.Dogru(ma2 >= 2, "canlı: SMBIOS major >= 2");
                    t.Dogru(c != null && c.Sistem != null, "canlı tablo çözüldü");
                }
            }
            catch (Exception) { istisna = true; }
            t.Dogru(!istisna, "HamOku istisna fırlatmaz");
        }
    }
}
