// Platform.Bul() testleri — RamChecker.exe --test ile koşar.
// Tüm testler tr-TR kültüründe de koşulur ("Intel" içindeki I harfi tuzağı).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace RamDanismani
{
    public static class PlatformTestleri
    {
        public static void Calistir(TestKosucu t)
        {
            CultureInfo eski = Thread.CurrentThread.CurrentCulture;
            CultureInfo eskiUi = Thread.CurrentThread.CurrentUICulture;
            try
            {
                t.Grup("Platform (geçerli kültür)");
                Hepsi(t);
                CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");
                Thread.CurrentThread.CurrentCulture = tr;
                Thread.CurrentThread.CurrentUICulture = tr;
                t.Grup("Platform (tr-TR)");
                Hepsi(t);
                TurkceOzel(t);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = eski;
                Thread.CurrentThread.CurrentUICulture = eskiUi;
            }
        }

        static int Hiz(PlatformBilgisi p, string tur)
        {
            int v;
            return p.MaxHizMTs.TryGetValue(tur, out v) ? v : 0;
        }

        static bool Var(PlatformBilgisi p, string tur)
        {
            return p.DesteklenenTurler.Contains(tur);
        }

        static void Hepsi(TestKosucu t)
        {
            t.Kos("i5-12450HX (bu makine)", delegate
            {
                PlatformBilgisi p = Platform.Bul("12th Gen Intel(R) Core(TM) i5-12450HX");
                t.Dogru(p.Bulundu, "12450HX bulundu");
                t.Icerir(p.Aile, "12. nesil", "12450HX aile");
                t.Icerir(p.Aile, "HX", "12450HX segment");
                t.Esit(4800, Hiz(p, "DDR5"), "12450HX DDR5 4800");
                t.Esit(3200, Hiz(p, "DDR4"), "12450HX DDR4 3200");
                t.Dogru(Var(p, "DDR5") && Var(p, "DDR4"), "12450HX DDR4+DDR5");
                t.Dogru(!Var(p, "LPDDR5"), "12450HX LPDDR yok");
                t.Esit(2, p.KanalSayisi, "12450HX çift kanal");
                t.Esit(128, p.MaxKapasiteGB, "12450HX 128 GB");
                t.Dogru(!p.EccDestekli, "12450HX ECC yok");
                t.Esit(Platform.KesinlikResmi, p.Kesinlik, "12450HX kesinlik");
                t.Icerir(p.Not, "lehimli", "12450HX dizüstü notu");
                t.Esit(4800, Hiz(p, "ddr5"), "MaxHizMTs büyük/küçük harf duyarsız");
            });

            t.Kos("İnceleme düzeltmeleri", delegate
            {
                PlatformBilgisi p = Platform.Bul("13th Gen Intel(R) Core(TM) i5-13450HX");
                t.Esit(4800, Hiz(p, "DDR5"), "13450HX DDR5 4800 (ARK)");
                t.Esit(4800, Hiz(Platform.Bul("13th Gen Intel(R) Core(TM) i7-13650HX"), "DDR5"), "13650HX DDR5 4800");
                t.Esit(5600, Hiz(Platform.Bul("13th Gen Intel(R) Core(TM) i7-13850HX"), "DDR5"), "13850HX DDR5 5600");
                t.Esit(5600, Hiz(Platform.Bul("13th Gen Intel(R) Core(TM) i9-13900HX"), "DDR5"), "13900HX DDR5 5600");
                t.Esit(5600, Hiz(Platform.Bul("Intel(R) Core(TM) i5-14600"), "DDR5"), "14600 DDR5 5600");
                t.Esit(5600, Hiz(Platform.Bul("Intel(R) Core(TM) i5-14600T"), "DDR5"), "14600T DDR5 5600");
                t.Esit(4800, Hiz(Platform.Bul("Intel(R) Core(TM) i5-14500"), "DDR5"), "14500 DDR5 4800");
                t.Esit(4800, Hiz(Platform.Bul("13th Gen Intel(R) Core(TM) i5-13600"), "DDR5"), "13600 non-K DDR5 4800");
                p = Platform.Bul("AMD Ryzen AI 7 450 w/ Radeon 860M");
                t.Dogru(p.Bulundu, "Ryzen AI 7 450 bulundu");
                t.Esit(5600, Hiz(p, "DDR5"), "Ryzen AI 400 DDR5 5600");
                t.Dogru(Platform.Bul("AMD Ryzen AI 9 HX 470 w/ Radeon 890M").Bulundu, "Ryzen AI 9 HX 470 bulundu");
                p = Platform.Bul("Intel(R) Core(TM) i5-10210U CPU @ 1.60GHz");
                t.Esit(2933, Hiz(p, "LPDDR4"), "10210U LPDDR4 2933");
                t.Dogru(Platform.Bul("Snapdragon(R) X 12-core X1E80100 @ 3.40 GHz").Bulundu, "Snapdragon X1E80100 bulundu");
                p = Platform.Bul("AMD Ryzen 9 8945HX with Radeon Graphics");
                t.Icerir(p.Aile, "Dragon Range", "8945HX Dragon Range");
                t.Esit(5200, Hiz(p, "DDR5"), "8945HX DDR5 5200");
                t.Dogru(!Var(p, "LPDDR5X"), "8945HX LPDDR5X yok");
                t.Icerir(Platform.Bul("AMD Ryzen 7 8845HS w/ Radeon 780M Graphics").Aile, "Hawk Point", "8845HS Hawk Point kalır");
                t.Icerir(Platform.Bul("Intel(R) Core(TM) i5-1030NG7 CPU @ 1.10GHz").Aile, "Ice Lake", "1030NG7 Ice Lake");
                p = Platform.Bul("12th Gen Intel(R) Core(TM) i7-1270PE");
                t.Dogru(p.Aile.IndexOf("masaüstü", StringComparison.OrdinalIgnoreCase) < 0 && p.MaxKapasiteGB <= 64, "1270PE mobil P satırı");
                t.Icerir(Platform.Bul("Intel(R) Celeron(R) N5095 @ 2.00GHz").Aile, "Jasper Lake", "N5095 Jasper Lake");
                t.Icerir(Platform.Bul("Intel(R) Celeron(R) N5095A @ 2.00GHz").Aile, "Jasper Lake", "N5095A Jasper Lake");
            });

            t.Kos("i7-8550U", delegate
            {
                PlatformBilgisi p = Platform.Bul("Intel(R) Core(TM) i7-8550U CPU @ 1.80GHz");
                t.Dogru(p.Bulundu, "8550U bulundu");
                t.Icerir(p.Aile, "8. nesil", "8550U nesil");
                t.Esit(2400, Hiz(p, "DDR4"), "8550U DDR4 2400");
                t.Esit(2133, Hiz(p, "LPDDR3"), "8550U LPDDR3 2133");
                t.Esit(32, p.MaxKapasiteGB, "8550U 32 GB");
                t.Dogru(!Var(p, "DDR5"), "8550U DDR5 yok");
            });

            t.Kos("Whiskey Lake 8565U", delegate
            {
                PlatformBilgisi p = Platform.Bul("Intel(R) Core(TM) i7-8565U CPU @ 1.80GHz");
                t.Icerir(p.Aile, "Whiskey", "8565U Whiskey Lake");
                t.Esit(64, p.MaxKapasiteGB, "8565U 64 GB");
            });

            t.Kos("Core Ultra 7 155H", delegate
            {
                PlatformBilgisi p = Platform.Bul("Intel(R) Core(TM) Ultra 7 155H");
                t.Icerir(p.Aile, "Meteor Lake", "155H Meteor Lake");
                t.Esit(5600, Hiz(p, "DDR5"), "155H DDR5 5600");
                t.Esit(7467, Hiz(p, "LPDDR5X"), "155H LPDDR5X 7467");
                t.Esit(96, p.MaxKapasiteGB, "155H 96 GB");
                t.Dogru(!Var(p, "DDR4"), "155H DDR4 yok");
            });

            t.Kos("Core Ultra 5 226V (Lunar Lake)", delegate
            {
                PlatformBilgisi p = Platform.Bul("Intel(R) Core(TM) Ultra 5 226V");
                t.Icerir(p.Aile, "Lunar Lake", "226V Lunar Lake");
                t.Esit(1, p.DesteklenenTurler.Count, "226V tek tür");
                t.Dogru(Var(p, "LPDDR5X"), "226V LPDDR5X");
                t.Esit(16, p.MaxKapasiteGB, "226V 16 GB");
                t.Icerir(p.Not, "yükseltilemez", "226V yükseltilemez notu");
                PlatformBilgisi p2 = Platform.Bul("Intel(R) Core(TM) Ultra 7 258V");
                t.Esit(32, p2.MaxKapasiteGB, "258V 32 GB");
            });

            t.Kos("i5-3470", delegate
            {
                PlatformBilgisi p = Platform.Bul("Intel(R) Core(TM) i5-3470 CPU @ 3.20GHz");
                t.Icerir(p.Aile, "Ivy Bridge", "3470 Ivy Bridge");
                t.Esit(1600, Hiz(p, "DDR3"), "3470 DDR3 1600");
                t.Esit(32, p.MaxKapasiteGB, "3470 32 GB");
                t.Icerir(p.Aile, "3. nesil", "3470 nesil");
            });

            t.Kos("Ryzen 7 5800H", delegate
            {
                PlatformBilgisi p = Platform.Bul("AMD Ryzen 7 5800H with Radeon Graphics");
                t.Icerir(p.Aile, "5000 mobil", "5800H aile");
                t.Esit(3200, Hiz(p, "DDR4"), "5800H DDR4 3200");
                t.Esit(4266, Hiz(p, "LPDDR4X"), "5800H LPDDR4X 4266");
                t.Dogru(!Var(p, "DDR5"), "5800H DDR5 yok");
                t.Esit(Platform.KesinlikTahmini, p.Kesinlik, "5800H kapasite bilinmediği için tahmini");
            });

            t.Kos("Ryzen 5 7600X", delegate
            {
                PlatformBilgisi p = Platform.Bul("AMD Ryzen 5 7600X 6-Core Processor");
                t.Icerir(p.Aile, "7000 masaüstü", "7600X aile");
                t.Esit(1, p.DesteklenenTurler.Count, "7600X sadece DDR5");
                t.Esit(5200, Hiz(p, "DDR5"), "7600X DDR5 5200");
                t.Dogru(!Var(p, "DDR4"), "7600X DDR4 yok");
                t.Dogru(p.EccDestekli, "7600X ECC (anakarta bağlı)");
            });

            t.Kos("Ryzen 7 PRO 4750U", delegate
            {
                PlatformBilgisi p = Platform.Bul("AMD Ryzen 7 PRO 4750U with Radeon Graphics");
                t.Dogru(p.Bulundu, "4750U bulundu");
                t.Icerir(p.Aile, "4000 mobil", "4750U aile");
                t.Icerir(p.Aile, "PRO", "4750U PRO");
                t.Esit(3200, Hiz(p, "DDR4"), "4750U DDR4 3200");
            });

            t.Kos("Ryzen AI 9 HX 370", delegate
            {
                PlatformBilgisi p = Platform.Bul("AMD Ryzen AI 9 HX 370 w/ Radeon 890M");
                t.Icerir(p.Aile, "Strix Point", "HX 370 aile");
                t.Esit(5600, Hiz(p, "DDR5"), "HX 370 DDR5 5600");
                t.Esit(7500, Hiz(p, "LPDDR5X"), "HX 370 LPDDR5X 7500");
                PlatformBilgisi m = Platform.Bul("AMD RYZEN AI MAX+ 395 w/ Radeon 8060S");
                t.Icerir(m.Aile, "Strix Halo", "AI Max aile");
                t.Esit(128, m.MaxKapasiteGB, "AI Max 128 GB");
            });

            t.Kos("Celeron N4020 / Pentium Silver N5030", delegate
            {
                PlatformBilgisi p = Platform.Bul("Intel(R) Celeron(R) N4020 CPU @ 1.10GHz");
                t.Icerir(p.Aile, "Gemini Lake", "N4020 aile");
                t.Esit(2400, Hiz(p, "DDR4"), "N4020 DDR4 2400");
                t.Esit(8, p.MaxKapasiteGB, "N4020 8 GB");
                PlatformBilgisi s = Platform.Bul("Intel(R) Pentium(R) Silver N5030 CPU @ 1.10GHz");
                t.Icerir(s.Aile, "Gemini Lake", "N5030 aile");
                t.Esit(8, s.MaxKapasiteGB, "N5030 8 GB");
            });

            t.Kos("Intel N100", delegate
            {
                PlatformBilgisi p = Platform.Bul("Intel(R) N100");
                t.Icerir(p.Aile, "Alder Lake-N", "N100 aile");
                t.Esit(1, p.KanalSayisi, "N100 tek kanal");
                t.Esit(16, p.MaxKapasiteGB, "N100 16 GB");
                t.Esit(4800, Hiz(p, "DDR5"), "N100 DDR5 4800");
                PlatformBilgisi i3 = Platform.Bul("Intel(R) Core(TM) i3-N305");
                t.Icerir(i3.Aile, "Alder Lake-N", "i3-N305 aile");
            });

            t.Kos("Xeon W-2245", delegate
            {
                PlatformBilgisi p = Platform.Bul("Intel(R) Xeon(R) W-2245 CPU @ 3.90GHz");
                t.Icerir(p.Aile, "Cascade Lake-W", "W-2245 aile");
                t.Esit(2933, Hiz(p, "DDR4"), "W-2245 DDR4 2933");
                t.Esit(4, p.KanalSayisi, "W-2245 dört kanal");
                t.Dogru(p.EccDestekli, "W-2245 ECC");
                t.Esit(1024, p.MaxKapasiteGB, "W-2245 1 TB");
            });

            t.Kos("Core 2 Duo E8400", delegate
            {
                PlatformBilgisi p = Platform.Bul("Intel(R) Core(TM)2 Duo CPU     E8400  @ 3.00GHz");
                t.Dogru(p.Bulundu, "E8400 bulundu");
                t.Dogru(Var(p, "DDR2"), "E8400 DDR2");
                t.Esit(Platform.KesinlikTahmini, p.Kesinlik, "E8400 tahmini");
                t.Icerir(p.Not, "yonga set", "E8400 yonga seti notu");
            });

            t.Kos("i3-10110U", delegate
            {
                PlatformBilgisi p = Platform.Bul("Intel(R) Core(TM) i3-10110U CPU @ 2.10GHz");
                t.Icerir(p.Aile, "Comet Lake", "10110U aile");
                t.Esit(2666, Hiz(p, "DDR4"), "10110U DDR4 2666");
                t.Esit(64, p.MaxKapasiteGB, "10110U 64 GB");
            });

            t.Kos("13th Gen i7-1355U", delegate
            {
                PlatformBilgisi p = Platform.Bul("13th Gen Intel(R) Core(TM) i7-1355U");
                t.Icerir(p.Aile, "13. nesil", "1355U nesil");
                t.Esit(5200, Hiz(p, "DDR5"), "1355U DDR5 5200");
                t.Esit(3200, Hiz(p, "DDR4"), "1355U DDR4 3200");
            });

            t.Kos("i9-14900K", delegate
            {
                PlatformBilgisi p = Platform.Bul("Intel(R) Core(TM) i9-14900K");
                t.Icerir(p.Aile, "14. nesil", "14900K nesil");
                t.Esit(5600, Hiz(p, "DDR5"), "14900K DDR5 5600");
                t.Esit(3200, Hiz(p, "DDR4"), "14900K DDR4 3200");
                t.Esit(192, p.MaxKapasiteGB, "14900K 192 GB");
                PlatformBilgisi i5 = Platform.Bul("13th Gen Intel(R) Core(TM) i5-13400");
                t.Esit(4800, Hiz(i5, "DDR5"), "13400 DDR5 4800");
            });

            t.Kos("Core Ultra 9 285K", delegate
            {
                PlatformBilgisi p = Platform.Bul("Intel(R) Core(TM) Ultra 9 285K");
                t.Icerir(p.Aile, "Arrow Lake", "285K aile");
                t.Esit(6400, Hiz(p, "DDR5"), "285K DDR5 6400");
                t.Dogru(!Var(p, "DDR4"), "285K DDR4 yok");
                t.Esit(192, p.MaxKapasiteGB, "285K 192 GB");
            });

            t.Kos("Diğer aileler", delegate
            {
                t.Icerir(Platform.Bul("Intel(R) Core(TM) i7-1065G7 CPU @ 1.30GHz").Aile, "Ice Lake", "1065G7 Ice Lake");
                t.Esit(4267, Hiz(Platform.Bul("11th Gen Intel(R) Core(TM) i7-1165G7 @ 2.80GHz"), "LPDDR4X"), "1165G7 LPDDR4X 4267");
                t.Esit(128, Platform.Bul("11th Gen Intel(R) Core(TM) i7-11800H @ 2.30GHz").MaxKapasiteGB, "11800H 128 GB");
                t.Esit(64, Platform.Bul("11th Gen Intel(R) Core(TM) i7-11370H @ 3.30GHz").MaxKapasiteGB, "11370H (H35) 64 GB");
                t.Esit(4, Platform.Bul("Intel(R) Core(TM) i9-9980XE CPU @ 3.00GHz").KanalSayisi, "9980XE dört kanal");
                t.Esit(2, Platform.Bul("Intel(R) Core(TM) i9-9900K CPU @ 3.60GHz").KanalSayisi, "9900K çift kanal");
                t.Esit(3, Platform.Bul("Intel(R) Core(TM) i7 CPU         920  @ 2.67GHz").KanalSayisi, "i7-920 üç kanal (eski ad)");
                t.Esit(8, Platform.Bul("Intel(R) Core(TM) i5 CPU       M 520  @ 2.40GHz").MaxKapasiteGB, "i5 M 520 8 GB");
                t.Dogru(Var(Platform.Bul("Intel(R) Core(TM) i5-4200U CPU @ 1.60GHz"), "DDR3L"), "4200U DDR3L");
                t.Icerir(Platform.Bul("Intel(R) Core(TM) m3-7Y30 CPU @ 1.00GHz").Aile, "Y", "m3-7Y30 Y serisi");
                t.Esit(5200, Hiz(Platform.Bul("AMD Ryzen 9 7945HX with Radeon Graphics"), "DDR5"), "7945HX DDR5 5200");
                t.Icerir(Platform.Bul("AMD Ryzen 9 7945HX with Radeon Graphics").Aile, "Dragon Range", "7945HX Dragon Range");
                PlatformBilgisi mend = Platform.Bul("AMD Ryzen 5 7520U with Radeon Graphics");
                t.Dogru(Var(mend, "LPDDR5") && mend.DesteklenenTurler.Count == 1, "7520U sadece LPDDR5");
                t.Esit(7500, Hiz(Platform.Bul("AMD Ryzen 7 7840HS w/ Radeon 780M Graphics"), "LPDDR5X"), "7840HS LPDDR5X 7500");
                t.Esit(5600, Hiz(Platform.Bul("AMD Ryzen 7 9700X 8-Core Processor"), "DDR5"), "9700X DDR5 5600");
                t.Esit(2933, Hiz(Platform.Bul("AMD Ryzen 5 2400G with Radeon Vega Graphics"), "DDR4"), "2400G DDR4 2933");
                PlatformBilgisi pro = Platform.Bul("AMD Ryzen 5 PRO 5650G with Radeon Graphics");
                t.Dogru(pro.EccDestekli, "Ryzen PRO masaüstü ECC");
                t.Esit(1600, Hiz(Platform.Bul("Intel(R) Xeon(R) CPU E3-1230 V2 @ 3.30GHz"), "DDR3"), "E3-1230 v2 DDR3 1600");
                t.Icerir(Platform.Bul("Snapdragon(R) X Elite - X1E78100 - Qualcomm(R) Oryon(TM) CPU").Aile, "Snapdragon", "Snapdragon X");
                t.Icerir(Platform.Bul("Intel(R) Pentium(R) Gold G5400 CPU @ 3.70GHz").Aile, "8. nesil", "Pentium G5400 8. nesil platform");
                t.Esit(Platform.KesinlikTahmini, Platform.Bul("Intel(R) Pentium(R) Gold G5400 CPU @ 3.70GHz").Kesinlik, "Pentium G5400 tahmini");
                t.Icerir(Platform.Bul("Intel(R) Core(TM) 7 150U").Aile, "Raptor Lake-U", "Core 7 150U");
                t.Icerir(Platform.Bul("Intel(R) Core(TM) Ultra X9 388H").Aile, "Panther Lake", "Ultra X9 388H");
                t.Icerir(Platform.Bul("AMD Ryzen Threadripper 3970X 32-Core Processor").Aile, "Threadripper 3000", "TR 3970X");
                t.Icerir(Platform.Bul("AMD Athlon Silver 3050U with Radeon Graphics").Aile, "Athlon mobil", "Athlon 3050U");
                t.Esit(1866, Hiz(Platform.Bul("AMD FX(tm)-8350 Eight-Core Processor"), "DDR3"), "FX-8350 DDR3 1866");
                t.Dogru(Platform.Bul("Intel(R) Core(TM) i7-5820K CPU @ 3.30GHz").KanalSayisi == 4, "5820K HEDT dört kanal");
                t.Esit(64, Platform.Bul("Intel(R) Pentium(R) CPU G4560 @ 3.50GHz").MaxKapasiteGB, "G4560 7. nesil platform 64 GB");
            });

            t.Kos("Yardımcılar", delegate
            {
                PlatformBilgisi h = Platform.Bul("Intel(R) Core(TM) i5-4210U CPU @ 1.70GHz");
                t.Dogru(Platform.TurDestekleniyor(h, "DDR3"), "SMBIOS 'DDR3' ~ DDR3L kabul");
                t.Esit(1600, Platform.AzamiHiz(h, "DDR3"), "AzamiHiz DDR3 -> DDR3L");
                PlatformBilgisi a = Platform.Bul("12th Gen Intel(R) Core(TM) i5-1235U");
                t.Dogru(Platform.TurDestekleniyor(a, "LPDDR4"), "LPDDR4 ~ LPDDR4X");
                t.Dogru(Platform.TurDestekleniyor(a, "ddr5"), "küçük harf ddr5");
                t.Dogru(!Platform.TurDestekleniyor(a, "DDR3"), "1235U DDR3 yok");
                t.Esit(0, Platform.AzamiHiz(null, "DDR4"), "AzamiHiz null güvenli");
                t.Dogru(!Platform.TurDestekleniyor(null, null), "TurDestekleniyor null güvenli");
            });

            t.Kos("Bulunamayan / boş girdi", delegate
            {
                string[] kotu = { null, "", "   ", "Apple M1", "Bilinmeyen İşlemci XYZ", "Intel(R) Xeon(R) CPU", "AMD Ryzen", "Intel(R) Core(TM) i5-99999Z", "ıııİİİ", "\0ÿ" };
                foreach (string s in kotu)
                {
                    PlatformBilgisi p = Platform.Bul(s);
                    string ad = s == null ? "(null)" : "\"" + s + "\"";
                    t.Dogru(p != null && !p.Bulundu, ad + " bulunamadı");
                    t.Dogru(p != null && p.DesteklenenTurler.Count == 0 && p.MaxHizMTs.Count == 0, ad + " boş listeler");
                }
            });
        }

        static void TurkceOzel(TestKosucu t)
        {
            t.Kos("Türkçe kültür: küçük harfli ad", delegate
            {
                // tr-TR'de "i".ToUpper() = "İ" olurdu ve "I5-" araması bozulurdu.
                PlatformBilgisi p = Platform.Bul("12th gen intel(r) core(tm) i5-12450hx");
                t.Dogru(p.Bulundu, "küçük harfli 12450hx bulundu");
                t.Esit(4800, Hiz(p, "DDR5"), "küçük harfli 12450hx DDR5 4800");
                PlatformBilgisi c = Platform.Bul("intel(r) celeron(r) n4020 cpu @ 1.10ghz");
                t.Icerir(c.Aile, "Gemini Lake", "küçük harfli n4020");
                PlatformBilgisi r = Platform.Bul("amd ryzen 7 pro 4750u with radeon graphics");
                t.Icerir(r.Aile, "PRO", "küçük harfli ryzen pro");
            });
        }
    }
}
