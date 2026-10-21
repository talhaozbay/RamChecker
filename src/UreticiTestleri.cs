// RAM Danışmanı — Ureticiler (üretici normalleştirme + parça numarası çözümü) testleri.
// C# 5 uyumlu.
using System;
using System.Globalization;
using System.Threading;

namespace RamDanismani
{
    public static class UreticiTestleri
    {
        public static void Calistir(TestKosucu t)
        {
            t.Grup("Ureticiler.Normallestir — metin");
            t.Kos("metin", delegate
            {
                t.Esit("Samsung", Ureticiler.Normallestir("Samsung", 0), "Samsung");
                t.Esit("Samsung", Ureticiler.Normallestir("Samsung Electronics", 0), "Samsung Electronics");
                t.Esit("Samsung", Ureticiler.Normallestir("SAMSUNG", 0), "SAMSUNG");
                t.Esit("Micron", Ureticiler.Normallestir("Micron Technology", 0), "Micron Technology");
                t.Esit("SK hynix", Ureticiler.Normallestir("Hynix Semiconductor", 0), "Hynix Semiconductor");
                t.Esit("SK hynix", Ureticiler.Normallestir("SK Hynix", 0), "SK Hynix");
                t.Esit("Kingston", Ureticiler.Normallestir("Kingston Technology", 0), "Kingston Technology");
                t.Esit("Crucial", Ureticiler.Normallestir("Crucial Technology", 0), "Crucial Technology");
                t.Esit("G.Skill", Ureticiler.Normallestir("G.Skill Intl", 0), "G.Skill Intl");
                t.Esit("ADATA", Ureticiler.Normallestir("A-DATA Technology", 0), "A-DATA");
                t.Esit("Team Group", Ureticiler.Normallestir("Team Group Inc.", 0), "Team Group Inc.");
                t.Esit("Patriot", Ureticiler.Normallestir("Patriot Memory", 0), "Patriot Memory");
                t.Esit("HP", Ureticiler.Normallestir("Hewlett-Packard", 0), "Hewlett-Packard");
                t.Esit("Corsair", Ureticiler.Normallestir("  Corsair  ", 0), "boşluklu Corsair");
                t.Esit("Ramaxel", Ureticiler.Normallestir("Ramaxel Technology", 0), "Ramaxel");
                t.Esit("Smart Modular", Ureticiler.Normallestir("SMART Modular Technologies", 0), "Smart Modular");
                t.Esit("Foo Bellek Ltd", Ureticiler.Normallestir("  Foo   Bellek Ltd ", 0), "tanınmayan -> Trim");
                t.Esit("Kingston", Ureticiler.Normallestir("Kingston", 0xCE80), "metin JEDEC kimliğinden önce gelir");
            });

            t.Grup("Ureticiler.Normallestir — JEDEC onaltılık");
            t.Kos("hex", delegate
            {
                t.Esit("Samsung", Ureticiler.Normallestir("80CE", 0), "80CE");
                t.Esit("Samsung", Ureticiler.Normallestir("CE00000000000000", 0), "CE00000000000000");
                t.Esit("Samsung", Ureticiler.Normallestir("0x80CE", 0), "0x80CE");
                t.Esit("Samsung", Ureticiler.Normallestir("00CE", 0), "00CE (paritesiz banka)");
                t.Esit("Samsung", Ureticiler.Normallestir("0xCE", 0), "0xCE");
                t.Esit("Micron", Ureticiler.Normallestir("802C", 0), "802C");
                t.Esit("Micron", Ureticiler.Normallestir("2C00000000000000", 0), "2C00...");
                t.Esit("SK hynix", Ureticiler.Normallestir("80AD", 0), "80AD");
                t.Esit("SK hynix", Ureticiler.Normallestir("80AD000080AD", 0), "80AD000080AD");
                t.Esit("SK hynix", Ureticiler.Normallestir("AD00000000000000", 0), "AD00...");
                t.Esit("Kingston", Ureticiler.Normallestir("0198", 0), "0198");
                t.Esit("Kingston", Ureticiler.Normallestir("9801", 0), "9801 (kod önce)");
                t.Esit("Kingston", Ureticiler.Normallestir("7F98", 0), "7F98 (SPD devam kodu)");
                t.Esit("Crucial", Ureticiler.Normallestir("859B", 0), "859B");
                t.Esit("Crucial", Ureticiler.Normallestir("7F7F7F7F7F9B", 0), "7F x5 + 9B");
                t.Esit("Corsair", Ureticiler.Normallestir("029E", 0), "029E");
                t.Esit("G.Skill", Ureticiler.Normallestir("04CD", 0), "04CD");
                t.Esit("ADATA", Ureticiler.Normallestir("04CB", 0), "04CB");
                t.Esit("Team Group", Ureticiler.Normallestir("04EF", 0), "04EF");
                t.Esit("Nanya", Ureticiler.Normallestir("830B", 0), "830B");
                t.Esit("Qimonda", Ureticiler.Normallestir("8551", 0), "8551");
                t.Esit("Elpida", Ureticiler.Normallestir("80FE", 0), "80FE");
                t.Esit("8A45", Ureticiler.Normallestir("8A45", 0), "tanınmayan kod ham kalır");
            });

            t.Grup("Ureticiler.Normallestir — SMBIOS 3.2 JEDEC kimliği");
            t.Kos("jedecId", delegate
            {
                t.Esit("Micron", Ureticiler.Normallestir("", 0x2C80), "0x2C80");
                t.Esit("Samsung", Ureticiler.Normallestir("Unknown", 0xCE80), "Unknown + 0xCE80");
                t.Esit("Crucial", Ureticiler.Normallestir(null, 0x9B85), "0x9B85");
                t.Esit("Kingston", Ureticiler.Normallestir("", 0x9801), "0x9801");
                t.Esit("SK hynix", Ureticiler.Normallestir("Manufacturer00", 0xAD80), "anlamsız metin + 0xAD80");
                t.Esit("", Ureticiler.Normallestir("", 0x1234), "tanınmayan kimlik + boş metin");
            });

            t.Grup("Ureticiler.Normallestir — anlamsız değerler");
            t.Kos("anlamsiz", delegate
            {
                string[] bos = new string[] { "Unknown", "Undefined", "Not Specified", "0000", "00000000",
                    "Manufacturer0", "Manufacturer00", "Mfg 0", "To Be Filled By O.E.M.", "", "   ", "FFFF",
                    "0x0000", "NO DIMM", "Module Manufacturer", "[Empty]", "Default string", "Manufacturer 1" };
                foreach (string s in bos)
                    t.Esit("", Ureticiler.Normallestir(s, 0), "anlamsız: \"" + s + "\"");
                t.Esit("", Ureticiler.Normallestir(null, 0), "null");
            });

            t.Grup("Ureticiler.ParcaNoCoz — Kingston");
            t.Kos("kingston", delegate
            {
                ParcaCozumu c = Parca(t, "KVR32S22S8/16", "Kingston", "Kingston", "DDR4", 3200, "SODIMM", 16, 22);
                t.Esit("ValueRAM", c.Seri, "KVR32S22S8/16 seri");
                t.Esit(false, c.Ecc, "KVR32S22S8/16 ECC yok");
                Parca(t, "KVR48U40BS8-16", "Kingston", "Kingston", "DDR5", 4800, "DIMM", 16, 40);
                Parca(t, "KVR16LS11/8", "", "Kingston", "DDR3L", 1600, "SODIMM", 8, 11);
                Parca(t, "KVR16N11/8", "", "Kingston", "DDR3", 1600, "DIMM", 8, 11);
                c = Parca(t, "KVR24R17D4/32", "", "Kingston", "DDR4", 2400, "DIMM", 32, 17);
                t.Esit(true, c.Ecc, "KVR24R17D4/32 ECC");
                t.Icerir(c.Aciklama, "Registered", "KVR24R17D4/32 açıklama");
                c = Parca(t, "KVR16N11K2/16", "", "Kingston", "DDR3", 1600, "DIMM", 8, 11);
                t.Icerir(c.Aciklama, "2'li kit", "KVR16N11K2/16 kit");
                c = Parca(t, "KF432C16BB/16", "Kingston", "Kingston", "DDR4", 3200, "DIMM", 16, 16);
                t.Esit("FURY Beast", c.Seri, "KF432C16BB seri");
                c = Parca(t, "KF556C40BBK2-32", "Kingston", "Kingston", "DDR5", 5600, "DIMM", 16, 40);
                t.Icerir(c.Aciklama, "2'li kit", "KF556C40BBK2-32 kit açıklaması");
                t.Icerir(c.Aciklama, "32 GB", "KF556C40BBK2-32 kit toplamı");
                c = Parca(t, "HX426S15IB2/8", "", "Kingston", "DDR4", 2666, "SODIMM", 8, 15);
                t.Esit("HyperX Impact", c.Seri, "HX426S15IB2/8 seri");
                Parca(t, "KCP426NS8/8", "", "Kingston", "DDR4", 2666, "DIMM", 8, 0);
                Parca(t, "KHX1600C10D3/8G", "", "Kingston", "DDR3", 1600, "", 8, 10);
                c = Ureticiler.ParcaNoCoz("9905701-017.A00G", "Kingston");
                t.Esit(true, c.Bulundu, "9905... bulundu (sadece marka)");
                t.Esit("Kingston", c.Marka, "9905... marka");
                t.Esit(0, c.KapasiteGB, "9905... kapasite uydurulmaz");
                t.Esit("", c.Tur, "9905... tür uydurulmaz");
            });

            t.Grup("Ureticiler.ParcaNoCoz — Samsung / SK hynix / Micron");
            t.Kos("dram", delegate
            {
                Parca(t, "M471A2K43EB1-CWE", "Samsung", "Samsung", "DDR4", 3200, "SODIMM", 16, 22);
                Parca(t, "M378A1K43EB2-CWE", "80CE", "Samsung", "DDR4", 3200, "DIMM", 8, 22);
                Parca(t, "M425R1GB4BB0-CQK", "Samsung", "Samsung", "DDR5", 4800, "SODIMM", 8, 40);
                Parca(t, "M323R2GA3DB0-CWM", "Samsung", "Samsung", "DDR5", 5600, "DIMM", 16, 46);
                ParcaCozumu c = Parca(t, "M393A2K40DB3-CWE", "Samsung", "Samsung", "DDR4", 3200, "DIMM", 16, 22);
                t.Esit(true, c.Ecc, "M393 ECC");
                t.Icerir(c.Aciklama, "RDIMM", "M393 RDIMM açıklaması");
                Parca(t, "M471B5173QH0-YK0", "Samsung", "Samsung", "DDR3L", 1600, "SODIMM", 4, 11);
                Parca(t, "HMA81GS6CJR8N-XN", "SK hynix", "SK hynix", "DDR4", 3200, "SODIMM", 8, 22);
                Parca(t, "HMA82GU6CJR8N-VK", "80AD", "SK hynix", "DDR4", 2666, "DIMM", 16, 19);
                c = Parca(t, "HMCG78AGBSA092N", "SK hynix", "SK hynix", "DDR5", 0, "SODIMM", 0, 0);
                // Voltaj eki: A = 1,35 V (DDR3L), C = 1,5 V (DDR3)
                Parca(t, "HMT451S6BFR8A-PB", "Hynix", "SK hynix", "DDR3L", 1600, "SODIMM", 4, 11);
                Parca(t, "HMT41GS6BFR8A-PB", "Hynix", "SK hynix", "DDR3L", 1600, "SODIMM", 8, 11);
                Parca(t, "HMT351S6CFR8C-PB", "Hynix", "SK hynix", "DDR3", 1600, "SODIMM", 4, 11);
                Parca(t, "MTA8ATF1G64HZ-3G2R1", "Micron", "Micron", "DDR4", 3200, "SODIMM", 8, 0);
                Parca(t, "MTA4ATF51264HZ-2G6E1", "Micron", "Micron", "DDR4", 2666, "SODIMM", 4, 0);
                c = Parca(t, "MTA18ASF2G72PZ-2G9E1", "Micron", "Micron", "DDR4", 2933, "DIMM", 16, 0);
                t.Esit(true, c.Ecc, "MTA18ASF2G72PZ ECC");
                Parca(t, "MT8KTF51264HZ-1G6E1", "Micron", "Micron", "DDR3L", 1600, "SODIMM", 4, 0);
                // Bu makinedeki (Lenovo LOQ 15IAX9E) gerçek modül — sondaki " K" dahil
                c = Parca(t, "MTC8C1084S1SC56BD1 K", "Micron Technology", "Micron", "DDR5", 5600, "SODIMM", 16, 0);
                t.Esit(false, c.Ecc, "MTC8C1084S1SC56BD1 ECC yok");
                t.Icerir(c.Aciklama, "DDR5-5600", "MTC8C1084 açıklama");
                Parca(t, "MTC4C10163S1SC48BA1", "Micron", "Micron", "DDR5", 4800, "SODIMM", 8, 0);
                // ASCII onaltılık döküm olarak gelen parça numarası
                Parca(t, "0x4D5443384331303834533153433536424431", "", "Micron", "DDR5", 5600, "SODIMM", 16, 0);
            });

            t.Grup("Ureticiler.ParcaNoCoz — Crucial / Corsair / G.Skill / ADATA / Team");
            t.Kos("perakende", delegate
            {
                Parca(t, "CT16G4SFRA32A", "Crucial", "Crucial", "DDR4", 3200, "SODIMM", 16, 0);
                Parca(t, "CT8G4DFS832A", "859B", "Crucial", "DDR4", 3200, "DIMM", 8, 0);
                Parca(t, "CT8G4DFS8266", "Crucial", "Crucial", "DDR4", 2666, "DIMM", 8, 0);
                Parca(t, "CT16G56C46S5", "Crucial", "Crucial", "DDR5", 5600, "SODIMM", 16, 46);
                ParcaCozumu c = Parca(t, "CT2K16G48C40U5", "Crucial", "Crucial", "DDR5", 4800, "DIMM", 16, 40);
                t.Icerir(c.Aciklama, "2'li kit", "CT2K16G48C40U5 kit");
                c = Parca(t, "BL2K8G32C16U4B", "Crucial", "Crucial", "DDR4", 3200, "DIMM", 8, 16);
                t.Icerir(c.Aciklama, "2'li kit", "Ballistix kit");
                c = Parca(t, "CMK16GX4M2B3200C16", "Corsair", "Corsair", "DDR4", 3200, "DIMM", 8, 16);
                t.Icerir(c.Aciklama, "2'li kit", "CMK16GX4M2B3200C16 kit");
                t.Esit("Vengeance LPX", c.Seri, "CMK DDR4 seri");
                c = Parca(t, "CMSX16GX4M2A3200C22", "Corsair", "Corsair", "DDR4", 3200, "SODIMM", 8, 22);
                t.Icerir(c.Aciklama, "2'li kit", "CMSX kit");
                Parca(t, "CMK32GX5M2B5600C36", "029E", "Corsair", "DDR5", 5600, "DIMM", 16, 36);
                c = Parca(t, "F4-3200C16D-16GVKB", "G.Skill", "G.Skill", "DDR4", 3200, "DIMM", 8, 16);
                t.Esit("Ripjaws V", c.Seri, "F4 VKB seri");
                t.Icerir(c.Aciklama, "2'li kit", "F4-3200C16D kit");
                c = Parca(t, "F5-6000J3038F16GX2-TZ5RK", "G.Skill", "G.Skill", "DDR5", 6000, "DIMM", 16, 30);
                t.Esit("Trident Z5 RGB", c.Seri, "TZ5RK seri");
                t.Icerir(c.Aciklama, "2'li kit", "F5 kit");
                c = Parca(t, "F4-3200C22S-8GRS", "G.Skill", "G.Skill", "DDR4", 3200, "SODIMM", 8, 22);
                t.Icermez(c.Aciklama, "kit", "F4 S tek modül");
                Parca(t, "AD4S32008G22-SGN", "ADATA", "ADATA", "DDR4", 3200, "SODIMM", 8, 22);
                c = Parca(t, "AX4U320016G16A-DT60", "ADATA", "ADATA", "DDR4", 3200, "DIMM", 16, 16);
                t.Esit("XPG", c.Seri, "AX4U seri");
                Parca(t, "AD5S48008G-S", "04CB", "ADATA", "DDR5", 4800, "SODIMM", 8, 0);
                // ADATA'nın hız ile kapasite arasındaki yapılandırma rakamı kapasiteye katılmamalı (38 GB değil 8 GB)
                Parca(t, "AD4S320038G22-BGN", "ADATA", "ADATA", "DDR4", 3200, "SODIMM", 8, 22);
                Parca(t, "AD4U3200716G22-SGN", "ADATA", "ADATA", "DDR4", 3200, "DIMM", 16, 22);
                Parca(t, "AD4S3200316G22-BHYD", "ADATA", "ADATA", "DDR4", 3200, "SODIMM", 16, 22);
                c = Parca(t, "AX4U320038G16A-SB10", "ADATA", "ADATA", "DDR4", 3200, "DIMM", 8, 16);
                t.Esit("XPG", c.Seri, "AX4U320038G16A seri");
                Parca(t, "AD5S560048G-S", "ADATA", "ADATA", "DDR5", 5600, "SODIMM", 48, 0);
                // G.Skill: kit harfi olmayan SPD biçimi modül başı kapasitedir
                c = Parca(t, "F4-3200C16-8GVKB", "G.Skill", "G.Skill", "DDR4", 3200, "DIMM", 8, 16);
                t.Icermez(c.Aciklama, "belirsiz", "F4 harfsiz: modül sayısı belirsiz notu yok");
                Parca(t, "F5-6000J3038F16G", "G.Skill", "G.Skill", "DDR5", 6000, "", 16, 30);   // seri eki yok: form belirsiz
                // Kingston FURY Beast RGB / HyperX Predator RGB ve SPD biçimi
                c = Parca(t, "KF432C16BB2A/16", "Kingston", "Kingston", "DDR4", 3200, "DIMM", 16, 16);
                t.Esit("FURY Beast RGB", c.Seri, "KF432C16BB2A seri");
                c = Parca(t, "HX432C16PB3AK2/16", "Kingston", "Kingston", "DDR4", 3200, "DIMM", 8, 16);
                t.Esit("HyperX Predator", c.Seri, "HX432C16PB3AK2 seri");
                t.Icerir(c.Aciklama, "2'li kit", "HX432C16PB3AK2 kit");
                Parca(t, "KF436C17BBK2/16", "Kingston", "Kingston", "DDR4", 3600, "DIMM", 8, 17);
                Parca(t, "KF3200C16D4/8GX", "Kingston", "Kingston", "DDR4", 3200, "", 8, 16);
                Parca(t, "TED416G3200C22-S01", "Team Group", "Team Group", "DDR4", 3200, "SODIMM", 16, 22);
                Parca(t, "TED48G3200C2201", "", "Team Group", "DDR4", 3200, "DIMM", 8, 22);
                c = Parca(t, "PVS416G320C6K", "Patriot", "Patriot", "DDR4", 3200, "", 8, 0);
                t.Icerir(c.Aciklama, "2'li kit", "Patriot Viper kit");
            });

            t.Grup("Ureticiler.ParcaNoCoz — çözülemeyenler");
            t.Kos("bilinmeyen", delegate
            {
                ParcaCozumu c = Ureticiler.ParcaNoCoz("ABC123", "Foo");
                t.Esit(false, c.Bulundu, "ABC123 bulunamadı");
                t.Esit("", c.Aciklama, "ABC123 açıklama boş");
                c = Ureticiler.ParcaNoCoz(null, null);
                t.Esit(false, c.Bulundu, "null güvenli");
                c = Ureticiler.ParcaNoCoz("Unknown", "Unknown");
                t.Esit(false, c.Bulundu, "Unknown");
                c = Ureticiler.ParcaNoCoz("00000000000000000000", "");
                t.Esit(false, c.Bulundu, "sıfır dolgulu parça no");
                c = Ureticiler.ParcaNoCoz("TS1GSH64V6B", "Transcend");
                t.Esit(false, c.Bulundu, "Team kalıbı başka markada tetiklenmez");
            });

            t.Grup("Ureticiler — Türkçe kültürde (tr-TR)");
            t.Kos("turkce", delegate
            {
                CultureInfo eski = Thread.CurrentThread.CurrentCulture;
                CultureInfo eskiUi = Thread.CurrentThread.CurrentUICulture;
                try
                {
                    CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");
                    Thread.CurrentThread.CurrentCulture = tr;
                    Thread.CurrentThread.CurrentUICulture = tr;
                    t.Esit("Micron", Ureticiler.Normallestir("micron technology", 0), "tr: micron technology");
                    t.Esit("Kingston", Ureticiler.Normallestir("kingston", 0), "tr: kingston");
                    t.Esit("SK hynix", Ureticiler.Normallestir("sk hynix", 0), "tr: sk hynix");
                    t.Esit("G.Skill", Ureticiler.Normallestir("g.skill", 0), "tr: g.skill");
                    t.Esit("Samsung", Ureticiler.Normallestir("0x80ce", 0), "tr: 0x80ce");
                    t.Esit("", Ureticiler.Normallestir("undefined", 0), "tr: undefined");
                    t.Esit("", Ureticiler.Normallestir("Not Specified", 0), "tr: Not Specified");
                    t.Esit("", Ureticiler.Normallestir("Manufacturer01", 0), "tr: Manufacturer01");
                    ParcaCozumu c = Ureticiler.ParcaNoCoz("hx426s15ib2/8", "kingston");
                    t.Esit("SODIMM", c.FormFaktor, "tr: hx426s15ib2/8 form");
                    t.Esit("HyperX Impact", c.Seri, "tr: hx426s15ib2/8 seri (küçük i)");
                    c = Ureticiler.ParcaNoCoz("kvr48u40bs8-16", "kingston");
                    t.Esit("DIMM", c.FormFaktor, "tr: kvr48u40bs8-16 DIMM");
                    t.Esit("DDR5", c.Tur, "tr: kvr48u40bs8-16 tür");
                    c = Ureticiler.ParcaNoCoz("mtc8c1084s1sc56bd1", "micron technology");
                    t.Esit(16, c.KapasiteGB, "tr: mtc8c1084 kapasite");
                    t.Esit("SODIMM", c.FormFaktor, "tr: mtc8c1084 SODIMM");
                    c = Ureticiler.ParcaNoCoz("f4-3200c16d-16gvkb", "");
                    t.Esit("Ripjaws V", c.Seri, "tr: f4 vkb seri");
                }
                finally
                {
                    Thread.CurrentThread.CurrentCulture = eski;
                    Thread.CurrentThread.CurrentUICulture = eskiUi;
                }
            });
        }

        /// <summary>Parça numarasını çözer ve beklenen alanları doğrular. 0 / "" = o alanın
        /// çözülmemesi (uydurulmaması) beklenir.</summary>
        static ParcaCozumu Parca(TestKosucu t, string pn, string uretici, string marka, string tur, int hiz, string form, int gb, int cl)
        {
            ParcaCozumu c = Ureticiler.ParcaNoCoz(pn, uretici);
            t.Esit(true, c.Bulundu, pn + " bulundu");
            t.Esit(marka, c.Marka, pn + " marka");
            t.Esit(tur, c.Tur, pn + " tür");
            t.Esit(hiz, c.HizMTs, pn + " hız");
            t.Esit(form, c.FormFaktor, pn + " form");
            t.Esit(gb, c.KapasiteGB, pn + " kapasite (modül başı)");
            t.Esit(cl, c.CL, pn + " CL");
            t.Dogru(c.Aciklama.Length > 0, pn + " açıklama dolu");
            return c;
        }
    }
}
