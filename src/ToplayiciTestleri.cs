// Toplayici testleri: Lenovo LOQ 15IAX9E SMBIOS fikstürü, sentetik zenginleştirme,
// WMI kod eşlemeleri, kasa/sanal makine tespiti ve canlı kullanım okuma.
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace RamDanismani
{
    public static class ToplayiciTestleri
    {
        // testler\veri\lenovo-loq-15iax9e-83LK.bin (3905 bayt, RawSMBIOSData başlığı yok, SMBIOS 3.4)
        const string LenovoFiksturu =
            "ABoAAAECAOAD/4CY+UsAABoAAw0BEQEOEABMRU5PVk8AUThDTjE3V1cAMDEvMjMvMjAyNQAAARsBAAECAwQAAAAAAAAAAAAAAAAA" +
            "AAAABgUGTEVOT1ZPADgzTEsATE9RIDE1SUFYOUUAVEVTVDAwMDEATEVOT1ZPX01UXzgzTEtfQlVfaWRlYV9GTV9MT1EgMTVJQVg5" +
            "RQBMT1EgMTVJQVg5RQAAAg8CAAECAwQFCQYDAAoATEVOT1ZPAExOVk5CMTYxMjE2ACBOTyBEUEsAVEVTVDAwMDEATk8gQXNzZXQg" +
            "VGFnAFR5cGUyIC0gQm9hcmQgQ2hhc3NpcyBMb2NhdGlvbgAAAxYDAAEKAgMEAwMDAwAAAAAAAQAABUxFTk9WTwBMT1EgMTVJQVg5" +
            "RQBURVNUMDAwMQBOTyBBc3NldCBUYWcAU0tVIE51bWJlcgAABDAEAAEDzQJyBgkA//vrvwOMZAAwET4QQUAKAAsADAAEBQYICAz8" +
            "AM0ACAAIAAwAVTNFMQBJbnRlbChSKSBDb3Jwb3JhdGlvbgAxMnRoIEdlbiBJbnRlbChSKSBDb3JlKFRNKSBpNS0xMjQ1MEhYAFRv" +
            "IEJlIEZpbGxlZCBCeSBPLkUuTS4AVG8gQmUgRmlsbGVkIEJ5IE8uRS5NLgBUbyBCZSBGaWxsZWQgQnkgTy5FLk0uAAAHGwUAAYAB" +
            "wADAACAAIAAABAQJwAAAAMAAAABMMSBDYWNoZQAABxsGAAGAAYAAgAAgACAAAAQDB4AAAACAAAAATDEgQ2FjaGUAAAcbBwABgQEA" +
            "FAAUIAAgAAAFBQEAFAAAABQAAEwyIENhY2hlAAAHGwgAAYIBADAAMCAAIAAABgUHADAAAAAwAABMMyBDYWNoZQAABxsJAAGAAYAA" +
            "gAAgACAAAAQEB4AAAACAAAAATDEgQ2FjaGUAAAcbCgABgAEAAQABIAAgAAAEAwcAAQAAAAEAAEwxIENhY2hlAAAHGwsAAYEBAAgA" +
            "CCAAIAAABQUIAAgAAAAIAABMMiBDYWNoZQAABxsMAAGCAQAwADAgACAAAAYFBwAwAAAAMAAATDMgQ2FjaGUAAAgJDQABAAIPDUox" +
            "QTEAS2V5Ym9hcmQAAAgJDgABAAIPDkoxQTEATW91c2UAAAgJDwABAAINHEoyQTEAVFYgT1VUAAAICRAAAQACBxxKMkEyAENSVAAA" +
            "CAkRAAEAAggJSjJBMgBDT00gMQAACAkSAAEAAhIQSjNBMQBVU0IAAAgJEwABAAISEEozQTEAVVNCAAAICRQAAQACEhBKM0ExAFVT" +
            "QgAACAkVAAEAAhIQSjVBMQBVU0IAAAgJFgABAAISEEo1QTEAVVNCAAAICRcAAQACEhBKNUEyAFVTQgAACAkYAAEAAgsfSjVBMQBO" +
            "ZXR3b3JrAAAICRkAARcCAP9KOUcyAE9uQm9hcmQgRmxvcHB5IFR5cGUAAAgJGgABFgIA/0o3SjEAT25Cb2FyZCBQcmltYXJ5IElE" +
            "RQAACAkbAAEAAh8dSjMwAE1pY3JvcGhvbmUgSW4AAAgJHAABAAIfHUozMABMaW5lIEluAAAICR0AAQACHx1KMzAAU3BlYWtlciBP" +
            "dXQAAAkTHgABpggDAQEAAAMAAADgCABKNkMxAAAJEx8AAaYIAwECAAADAAAA4QgASjZEMgAACRMgAAGmCAMBAwAAAwAAAOIIAEo3" +
            "QzEAAAkTIQABpggDAQQAAAMAAADjCABKN0QxAAAJEyIAAagKAwEFAAADAAAA5AoASjhDMQAACwUjAAFNb2Rlcm4gUHJlbG9hZAAA" +
            "DAUkAANDb25maWdPcHRpb25zMQBDb25maWdPcHRpb25zMgBDb25maWdPcHRpb25zMwAADRYlAAgAAAAAAAAAAAAAAAAAAAAAAWVu" +
            "fFVTfGlzbzg4NTktMSwwAGZyfEZSfGlzbzg4NTktMSwwAHpofFRXfHVuaWNvZGUsMABqYXxKUHx1bmljb2RlLDAAaXR8SVR8aXNv" +
            "ODg1OS0xLDAAZXN8RVN8aXNvODg1OS0xLDAAZGV8REV8aXNvODg1OS0xLDAAcHR8UFR8aXNvODg1OS0xLDAAAA4IJgAB2z0AJE1F" +
            "SQAADhdGAAHdPgDdPwDdQADdQQDdQgDdQwBGaXJtd2FyZSBWZXJzaW9uIEluZm8AAA8dJwAAAAAAACAEAXhWNBIAAAAAgAMCBwAI" +
            "BBYAAAAQFygAAwMDAAAAAf7/AQAAAAAAAAAAAAAAEVwpACgA/v9AAEAAAEANAAECIoAA4BUDBAUGAQAAAADAEgAAAABMBAMIAACA" +
            "LAAAAAAAAAAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABDb250cm9sbGVyMC1DaGFubmVsQS1ESU1NMABC" +
            "QU5LIDAATWljcm9uIFRlY2hub2xvZ3kAMUEyQjNDNEQAOTg3NjU0MzIxMABNVEM4QzEwODRTMVNDNTZCRDEgSwAAEx8qAAAAAAD/" +
            "//8AKAABAAAAAAAAAAAAAAAAAAAAAAAAFCMrAAAAAAD///8AKQAqAP8BAQAAAAAAAAAAAAAAAAAAAAAAABUHLAAHBAQAABYaLQAB" +
            "AgMEBQcAAAAAAP8AAAAABgAAAAAARmFrZQAtVmlydHVhbCBCYXR0ZXJ5IDAtADA4LzA4LzIwMTAAQmF0dGVyeSAwAENSQiBCYXR0" +
            "ZXJ5IDAATGl0aGl1bVBvbHltZXIAABgFLgAAAAAaGC8AAUIAgACAAIAAgACAAIAAAAAAAIBWb2x0YWdlIFByb2JlIERlc2NyaXB0" +
            "aW9uAAAbDzAAMQBjAAAAAAAAIAFDb29saW5nIERldmljZSBEZXNjcmlwdGlvbgAAHBgxAAFCAIAAgACAAIAAgACAAAAAAACAVGVt" +
            "cGVyYXR1cmUgUHJvYmUgRGVzY3JpcHRpb24AACALMgAAAAAAAAAAAAAnFjMAAAECAwQFBgdLAKQh////////T0VNIERlZmluZSAw" +
            "AE9FTSBEZWZpbmUgMQBPRU0gRGVmaW5lIDIAT0VNIERlZmluZSAzAE9FTSBEZWZpbmUgNABPRU0gRGVmaW5lIDUAT0VNIERlZmlu" +
            "ZSA2AAAoETQAAgYSAAUBqgYAAAUCAFBDSUV4cHJlc3N4MTYAQ29tcGlsZXIgVmVyc2lvbjogVkMgOS4wAAApCzUAAQMBAAAAEElH" +
            "RAAAKx82AElOVEMCABIAWALPCB4AAQQAAAAAAAAAAAAAAFRQTSBEZXZpY2UAAIAINwBVqlWqT2VtIFR5cGUgMTI4IFRlc3QgMQBP" +
            "ZW0gVHlwZSAxMjggVGVzdCAyAACBCDgAAQECAUluc3lkZV9BU0ZfMDAxAEluc3lkZV9BU0ZfMDAyAACDQDkAMQAAAAAAAAAAAAAA" +
            "+ACMegAAAAAhIAAAAQAQAM8IHgAAAAAA/gD//wAAAAAAAAAAIgAAAHZQcm8AAAAAAACFBToAAVhYWFhYWFhYWFhYWFhYAACIBjsA" +
            "//8AAMgQPAABAgEoARQBVbKAAQVJZGVhUGFkADgzTEswMDhKVFIAANtqPQABBAFFAgCQBgGFMCAAAAAAQAAAAx8gAskDYEQC////" +
            "////////////////////////////AwAAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABNRUkx" +
            "AE1FSTIATUVJMwBNRUk0AADdDD4AAQEABAAAAABGU1AgQmluYXJ5IFZlcnNpb24AAN0aPwADAQAMALoQAAIAAAAALgADAP//////" +
            "UmVmZXJlbmNlIENvZGUgLSBDUFUAdUNvZGUgVmVyc2lvbgBUWFQgQUNNIHZlcnNpb24AAN0aQAADAQAMALoQAAIAAAAAAAADBBAB" +
            "Hs8IUmVmZXJlbmNlIENvZGUgLSBNRQBNRUJ4IHZlcnNpb24ATUUgRmlybXdhcmUgVmVyc2lvbgBDb25zdW1lciBTS1UAAN0vQQAG" +
            "AQAMALoQAAID//////8EAP///xEABQD///8RAAYAAkYAAAAHAAQAAAAAUmVmZXJlbmNlIENvZGUgLSBQQ0gAUENILUNSSUQgU3Rh" +
            "dHVzAERpc2FibGVkAFBDSC1DUklEIE9yaWdpbmFsIFZhbHVlAFBDSC1DUklEIE5ldyBWYWx1ZQBPUFJPTSAtIFJTVCAtIFJBSUQA" +
            "UENIIEhzaW8gVmVyc2lvbgAA3VJCAAsBAAwAuhAAAgAAAAS4AAMADAC6EAAEBf//////BgAAAAACAAcAAAAAAgAIAP//////CQD/" +
            "/////woA//////8LAP//////DAD//////1JlZmVyZW5jZSBDb2RlIC0gU0EgLSBTeXN0ZW0gQWdlbnQAUmVmZXJlbmNlIENvZGUg" +
            "LSBNUkMAU0EgLSBQQ0llIFZlcnNpb24AU0EtQ1JJRCBTdGF0dXMARGlzYWJsZWQAU0EtQ1JJRCBPcmlnaW5hbCBWYWx1ZQBTQS1D" +
            "UklEIE5ldyBWYWx1ZQBPUFJPTSAtIFZCSU9TAElPIE1hbmFnZWFiaWxpdHkgRW5naW5lIEZXIFZlcnNpb24AUEhZIEJ1aWxkIFZl" +
            "cnNpb24AVGh1bmRlcmJvbHQoVE0pIEZXIFZlcnNpb24AU3lzdGVtIEFnZW50IE1hbmFnZWFiaWxpdHkgRW5naW5lIEZXIFZlcnNp" +
            "b24AAN1SQwALAQD//////wIA//////8DBP//////BQb//////wcI//////8JAP//////CgD//////wsAAQ4AAAAMAAAXAIEQDQD/" +
            "/////w4AAAcAAABMYW4gUGh5IFZlcnNpb24AU2Vuc29yIEZpcm13YXJlIFZlcnNpb24ARGVidWcgTW9kZSBTdGF0dXMARGlzYWJs" +
            "ZWQAUGVyZm9ybWFuY2UgTW9kZSBTdGF0dXMARGlzYWJsZWQARGVidWcgVXNlIFVTQihEaXNhYmxlZDpTZXJpYWwpAERpc2FibGVk" +
            "AElDQyBPdmVyY2xvY2tpbmcgVmVyc2lvbgBVTkRJIFZlcnNpb24ARUMgRlcgVmVyc2lvbgBHT1AgVmVyc2lvbgBSb3lhbCBQYXJr" +
            "IFZlcnNpb24AUGxhdGZvcm0gVmVyc2lvbgAA3hVEAAEAViUDbKOrdOYTkuOjzNfoTENGQwAA+BJFAKysqhcAAAAAAAAAAAAAAAB/" +
            "BP/+AAA=" +
            "";

        public static void Calistir(TestKosucu t)
        {
            t.Grup("Toplayici: Lenovo LOQ fikstürü");
            t.Kos("Lenovo fikstürü", delegate { LenovoFiksturTesti(t); });
            t.Kos("Fikstür sürümsüz (major=0)", delegate { SurumsuzFiksturTesti(t); });

            t.Grup("Toplayici: bozuk girdi");
            t.Kos("Boş/bozuk tablo", delegate { BozukGirdiTesti(t); });

            t.Grup("Toplayici: Zenginlestir");
            t.Kos("Lehimli sezgisi", delegate { LehimliTestleri(t); });
            t.Kos("Tür tamamlama", delegate { TurTamamlamaTestleri(t); });
            t.Kos("Türkçe kültür", delegate { TurkceKulturTesti(t); });
            t.Kos("Doldurmalık değerler", delegate { DoldurmalikTestleri(t); });
            t.Kos("İnceleme düzeltmeleri", delegate { IncelemeTestleri(t); });

            t.Grup("Toplayici: kod eşlemeleri, kasa, sanal makine");
            t.Kos("WMI kodları", delegate { WmiKodTestleri(t); });
            t.Kos("Kasa", delegate { KasaTestleri(t); });
            t.Kos("Sanal makine", delegate { SanalMakineTestleri(t); });

            t.Grup("Toplayici: canlı");
            t.Kos("KullanimOku", delegate { KullanimTesti(t); });
            t.Kos("Topla (canlı)", delegate { CanliToplaTesti(t); });
        }

        static byte[] Fikstur()
        {
            return Convert.FromBase64String(LenovoFiksturu);
        }

        static void LenovoFiksturTesti(TestKosucu t)
        {
            byte[] tablo = Fikstur();
            t.Esit(3905, tablo.Length, "fikstür boyutu");
            Envanter e = Toplayici.Topla(tablo, 3, 4, false);

            t.Dogru(e.SmbiosOkundu, "SmbiosOkundu");
            t.Esit(1, e.Yuvalar.Count, "BIOS tek yuva listeliyor");
            t.Esit(1, e.DoluYuvalar.Count, "1 dolu yuva");
            t.Esit(16384L, e.ToplamTakiliMB, "toplam 16 GB");
            if (e.DoluYuvalar.Count == 1)
            {
                BellekYuvasi y = e.DoluYuvalar[0];
                t.Esit("Micron", y.Uretici, "üretici normalleştirildi");
                t.Icerir(y.UreticiHam, "Micron", "ham üretici korunur");
                t.Esit("DDR5", y.Tur, "tür");
                t.Esit(0x22, y.TurKodu, "tür kodu");
                t.Esit("SODIMM", y.FormFaktor, "form faktörü");
                t.Esit(0x0D, y.FormFaktorKodu, "form kodu");
                t.Esit(5600, y.HizMTs, "modül hızı");
                t.Esit(4800, y.YapilandirilmisHizMTs, "çalışma hızı");
                t.Esit(1100, y.YapilandirilmisVoltajMv, "voltaj");
                t.Dogru(!y.Lehimli, "SODIMM lehimli değil");
                t.Esit("", y.LehimliNedeni, "lehimli nedeni boş");
                t.Esit("SMBIOS", y.Kaynak, "kaynak");
                t.Icerir(y.ParcaNo, "MTC8C1084S1SC56BD1", "parça no");
                t.Icerir(y.Yuva, "DIMM0", "yuva adı");
                t.Dogru(y.ParcaBilgisi != null, "parça bilgisi çözüldü");
                t.Dogru(y.ParcaBilgisi != null && y.ParcaBilgisi.Bulundu, "Micron MTC parça no tanındı");
                t.Dogru(!y.Ecc, "ECC yok");
            }
            t.Esit(1, e.Diziler.Count, "1 bellek dizisi");
            if (e.Diziler.Count == 1)
            {
                t.Esit(16384L, e.Diziler[0].MaxKapasiteMB, "BIOS azami kapasite 16 GB");
                t.Esit(1, e.Diziler[0].YuvaSayisi, "BIOS yuva sayısı 1");
            }
            t.Esit("LENOVO", e.Sistem.Uretici, "sistem üreticisi");
            t.Esit("83LK", e.Sistem.Model, "model");
            t.Esit("LOQ 15IAX9E", e.Sistem.ModelAdi, "model adı");
            t.Icerir(e.Sistem.IslemciAdi, "i5-12450HX", "işlemci");
            t.Esit(KasaTipi.Dizustu, e.Sistem.Kasa, "kasa dizüstü");
            t.Dogru(!e.Sistem.SanalMakine, "sanal makine değil");
            t.Dogru(e.Platform != null && e.Platform.Bulundu, "platform bulundu (i5-12450HX)");
            t.Esit(0L, e.Kullanim.ToplamMB, "dosya modunda kullanım yok");
            t.Esit(0L, e.Sistem.TakiliRamMB, "dosya modunda takılı RAM sorgulanmaz");
            t.Icerir(string.Join("\n", e.ToplamaNotlari.ToArray()), "Dosyadan okundu", "dosya notu");
        }

        static void SurumsuzFiksturTesti(TestKosucu t)
        {
            Envanter e = Toplayici.Topla(Fikstur(), 0, 0, false);
            t.Esit(1, e.DoluYuvalar.Count, "sürüm 0 iken de 1 dolu yuva");
            if (e.DoluYuvalar.Count == 1)
                t.Esit("DDR5", e.DoluYuvalar[0].Tur, "sürüm 0 iken tür");
        }

        static void BozukGirdiTesti(TestKosucu t)
        {
            Envanter e = Toplayici.Topla(null, 0, 0, false);
            t.Dogru(e != null, "null tablo istisnasız");
            t.Dogru(!e.SmbiosOkundu, "null tablo okunmadı");
            t.Esit(0, e.Yuvalar.Count, "null tablo yuva yok");
            t.Dogru(e.ToplamaNotlari.Count > 0, "null tablo not var");

            e = Toplayici.Topla(new byte[0], 3, 4, false);
            t.Dogru(e != null && !e.SmbiosOkundu, "boş tablo");

            byte[] tam = Fikstur();
            byte[] kesik = new byte[300];
            Array.Copy(tam, kesik, kesik.Length);
            e = Toplayici.Topla(kesik, 3, 4, false);
            t.Dogru(e != null, "kesik tablo istisnasız");

            byte[] cop = new byte[512];
            new Random(42).NextBytes(cop);
            e = Toplayici.Topla(cop, 3, 4, false);
            t.Dogru(e != null, "rastgele bayt istisnasız");

            Toplayici.Zenginlestir(null);
            Envanter bos = new Envanter();
            bos.Yuvalar.Add(null);
            bos.Yuvalar.Add(new BellekYuvasi { Yuva = null, Banka = null, ParcaNo = null, Uretici = null, UreticiHam = null, Tur = null, Dolu = true, BoyutMB = 8192 });
            Toplayici.Zenginlestir(bos);
            t.Esit(1, bos.Yuvalar.Count, "null yuva ayıklandı, null alanlı yuva istisnasız");
            t.Dogru(bos.Platform != null, "platform null değil");
        }

        static void IncelemeTestleri(TestKosucu t)
        {
            // LPDDR + belirsiz form faktörü: lehimli ama LPCAMM2 olabilir
            BellekYuvasi y = Modul("Controller1-ChannelA", "BANK 1", 0x01, 0x23);
            Toplayici.Zenginlestir(Sentetik(y));
            t.Dogru(y.Lehimli && y.LehimliSupheli, "LPDDR5 Form=Other: lehimli, şüpheli");
            t.Icerir(y.LehimliNedeni, "LPCAMM2", "neden LPCAMM2 olasılığını söyler");
            y = Modul("Controller0-ChannelA", "BANK 0", 0x0B, 0x23);
            Toplayici.Zenginlestir(Sentetik(y));
            t.Dogru(y.Lehimli && !y.LehimliSupheli, "LPDDR5 Row of chips: kesin lehimli");

            // Eski SMBIOS: çalışma hızı MHz cinsinden (DDR4 2666/1333)
            y = Modul("DIMM A", "BANK 0", 0x09, 0x1A);
            y.HizMTs = 2666; y.YapilandirilmisHizMTs = 1333;
            Envanter e = Sentetik(y);
            Toplayici.Zenginlestir(e);
            t.Esit(2666, y.YapilandirilmisHizMTs, "DDR4 1333 (MHz) → 2666 MT/s");
            bool not = false;
            foreach (string n in e.ToplamaNotlari) if (n.IndexOf("MHz", StringComparison.Ordinal) >= 0) not = true;
            t.Dogru(not, "MHz çevirme notu");
            y = Modul("DIMM A", "BANK 0", 0x09, 0x1A);
            y.HizMTs = 3200; y.YapilandirilmisHizMTs = 1600;
            Toplayici.Zenginlestir(Sentetik(y));
            t.Esit(1600, y.YapilandirilmisHizMTs, "DDR4 3200/1600 değişmez (gerçekten 1600 olabilir)");

            // Boyut 0xFFFF (bilinmiyor) + çözülebilir parça no → boyut parça numarasından
            ModulTanimi a = new ModulTanimi();
            a.Handle = 0x1100; a.Yuva = "DIMM A"; a.Banka = "BANK 0"; a.BoyutHam = 0xFFFF; a.FormKodu = 0x0D; a.TurKodu = 0x1A;
            a.Hiz = 3200; a.YapHiz = 3200; a.Uretici = "Samsung"; a.SeriNo = "12345678"; a.ParcaNo = "M471A1K43DB1-CWE";
            ModulTanimi b = new ModulTanimi();
            b.Handle = 0x1101; b.Yuva = "DIMM B"; b.Banka = "BANK 2"; b.BoyutMB = 8192; b.FormKodu = 0x0D; b.TurKodu = 0x1A;
            b.Hiz = 3200; b.YapHiz = 3200; b.Uretici = "Samsung"; b.SeriNo = "87654321"; b.ParcaNo = "M471A1K43DB1-CWE";
            byte[] tablo = SmbiosKurucu.Tablo(
                SmbiosKurucu.Sistem("LENOVO", "20XX", "ThinkPad E14", "PF000000"),
                SmbiosKurucu.Kasa(0x0A),
                SmbiosKurucu.Islemci("11th Gen Intel(R) Core(TM) i5-1135G7 @ 2.40GHz", 4, 8),
                SmbiosKurucu.Dizi(0x1000, 32768, 2),
                SmbiosKurucu.Modul(a), SmbiosKurucu.Modul(b));
            Envanter e2 = Toplayici.Topla(tablo, 3, 3, false);
            long toplam = 0;
            foreach (BellekYuvasi m in e2.Yuvalar) if (m.Dolu) toplam += m.BoyutMB;
            t.Esit(16384L, toplam, "0xFFFF modül parça numarasından 8 GB → toplam 16 GB");
            DanismanSonucu d = Danisman.Degerlendir(e2);
            t.Esit(16, d.MevcutToplamGB, "Danışman toplamı 16 GB");
            bool gereksizKit = false;
            foreach (YukseltmeSenaryosu s in d.Senaryolar) if (s.SonucToplamGB <= 16) gereksizKit = true;
            t.Dogru(!gereksizKit, "kazançsız (16 GB'lık) senaryo önerilmez");
        }

        static Envanter Sentetik(BellekYuvasi y)
        {
            Envanter e = new Envanter();
            e.Sistem.IslemciAdi = "Intel(R) Core(TM) i7-1165G7 @ 2.80GHz";
            e.Yuvalar.Add(y);
            return e;
        }

        static BellekYuvasi Modul(string yuva, string banka, int formKodu, int turKodu)
        {
            BellekYuvasi y = new BellekYuvasi();
            y.Dolu = true;
            y.BoyutMB = 8192;
            y.Yuva = yuva;
            y.Banka = banka;
            y.FormFaktorKodu = formKodu;
            y.TurKodu = turKodu;
            y.Kaynak = "SMBIOS";
            return y;
        }

        static void LehimliTestleri(TestKosucu t)
        {
            BellekYuvasi y = Modul("Controller0-ChannelA", "BANK 0", 0x0B, 0x23);
            Toplayici.Zenginlestir(Sentetik(y));
            t.Dogru(y.Lehimli, "LPDDR5 Row of chips lehimli");
            t.Icerir(y.LehimliNedeni, "LPDDR5", "neden LPDDR5'i söyler");
            t.Esit("LPDDR5", y.Tur, "tür adı koddan");
            t.Esit("Row of chips", y.FormFaktor, "form adı koddan");

            y = Modul("Controller1-ChannelA", "BANK 1", 0x01, 0x1E);
            Toplayici.Zenginlestir(Sentetik(y));
            t.Dogru(y.Lehimli, "LPDDR4 form Other lehimli");

            y = Modul("LPCAMM2", "P0 CHANNEL A", 0x11, 0x23);
            Toplayici.Zenginlestir(Sentetik(y));
            t.Dogru(!y.Lehimli, "LPCAMM2 (CAMM) lehimli değil");

            y = Modul("ONBOARD MEMORY", "BANK 0", 0x02, 0x1A);
            Toplayici.Zenginlestir(Sentetik(y));
            t.Dogru(y.Lehimli, "ONBOARD locator lehimli");
            t.Icerir(y.LehimliNedeni, "ONBOARD", "neden yuva adını söyler");

            y = Modul("ChannelA-DIMM0", "Memory Down", 0x0D, 0x1A);
            Toplayici.Zenginlestir(Sentetik(y));
            t.Dogru(y.Lehimli, "Memory Down banka lehimli");

            y = Modul("DIMM 0", "BANK 0", 0x0B, 0x1A);
            Toplayici.Zenginlestir(Sentetik(y));
            t.Dogru(y.Lehimli, "DDR4 Row of chips lehimli");

            y = Modul("ChannelA-DIMM0", "BANK 0", 0x0D, 0x1A);
            Toplayici.Zenginlestir(Sentetik(y));
            t.Dogru(!y.Lehimli, "DDR4 SODIMM lehimli değil");
            t.Esit("DDR4", y.Tur, "DDR4 adı");
            t.Esit("SODIMM", y.FormFaktor, "SODIMM adı");

            y = Modul("DIMM_A2", "P0 CHANNEL A", 0x09, 0x22);
            Toplayici.Zenginlestir(Sentetik(y));
            t.Dogru(!y.Lehimli, "DDR5 DIMM lehimli değil");

            y = Modul("ChannelB-DIMM0", "BANK 2", 0x02, 0x02);
            y.Dolu = false;
            y.BoyutMB = 0;
            y.ParcaNo = "M471A1K43DB1-CWE";
            Toplayici.Zenginlestir(Sentetik(y));
            t.Dogru(!y.Lehimli, "boş yuva lehimli değil");
            t.Dogru(y.ParcaBilgisi == null, "boş yuvada parça çözümü yok");
        }

        static void TurTamamlamaTestleri(TestKosucu t)
        {
            BellekYuvasi y = Modul("ChannelA-DIMM0", "BANK 0", 0x0D, 0x02);
            y.Tur = "Unknown";
            y.UreticiHam = "Samsung";
            y.ParcaNo = "M471A1K43DB1-CWE  ";
            Envanter e = Sentetik(y);
            Toplayici.Zenginlestir(e);
            t.Esit("M471A1K43DB1-CWE", y.ParcaNo, "parça no kırpıldı");
            t.Dogru(y.ParcaBilgisi != null && y.ParcaBilgisi.Bulundu, "Samsung parça no çözüldü");
            t.Esit("DDR4", y.Tur, "tür parça numarasından tamamlandı");
            t.Esit(0x1A, y.TurKodu, "tür kodu da güncellendi");
            t.Icerir(string.Join("\n", e.ToplamaNotlari.ToArray()), "parça numarasından DDR4", "tamamlama notu");
            t.Esit("Samsung", y.Uretici, "üretici");

            // Parça no çözülemiyorsa tür bilinmiyor kalır, istisna yok.
            y = Modul("DIMM1", "BANK 1", 0x09, 0x02);
            y.ParcaNo = "ZZZ-BILINMEYEN-123";
            e = Sentetik(y);
            Toplayici.Zenginlestir(e);
            t.Dogru(y.Tur == "Unknown" || y.Tur == "", "çözülemeyen parça ile tür değişmez");

            // Ham üretici yoksa Uretici alanı ham kabul edilir.
            y = Modul("DIMM2", "BANK 2", 0x09, 0x1A);
            y.Uretici = "Samsung Electronics";
            Toplayici.Zenginlestir(Sentetik(y));
            t.Esit("Samsung Electronics", y.UreticiHam, "UreticiHam doldu");
            t.Esit("Samsung", y.Uretici, "Uretici normalleşti");

            // Tür adı verilip kodu verilmezse kod adından bulunur.
            y = Modul("DIMM3", "BANK 3", 0, 0);
            y.Tur = "LPDDR5";
            y.FormFaktor = "Row of chips";
            Toplayici.Zenginlestir(Sentetik(y));
            t.Esit(0x23, y.TurKodu, "addan tür kodu");
            t.Esit(0x0B, y.FormFaktorKodu, "addan form kodu");
            t.Dogru(y.Lehimli, "addan LPDDR5 lehimli");

            // TurDetay bayraklardan
            y = Modul("DIMM4", "BANK 4", 0x09, 0x1A);
            y.TurDetayBayraklari = 0x2080;   // Synchronous + Registered
            Toplayici.Zenginlestir(Sentetik(y));
            t.Dogru(y.TurDetay.Contains("Synchronous"), "TurDetay Synchronous");
            t.Dogru(y.Kayitli, "Registered bayrağı Kayitli");
        }

        static void TurkceKulturTesti(TestKosucu t)
        {
            CultureInfo eski = Thread.CurrentThread.CurrentCulture;
            CultureInfo eskiUi = Thread.CurrentThread.CurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("tr-TR");
                BellekYuvasi y = Modul("Built-in Memory", "bank 0", 0x02, 0x1A);
                Toplayici.Zenginlestir(Sentetik(y));
                t.Dogru(y.Lehimli, "tr-TR kültüründe 'Built-in' tanınır (I/İ tuzağı)");

                y = Modul("dimm 1", "bank 1", 0x0D, 0x1A);
                Toplayici.Zenginlestir(Sentetik(y));
                t.Dogru(!y.Lehimli, "tr-TR kültüründe küçük harfli dimm lehimli değil");
                t.Dogru(Toplayici.Doldurmalik("not specified"), "tr-TR doldurmalık 'not specified'");
                t.Esit(0x1A, Toplayici.TurKoduAdindan("ddr4"), "tr-TR küçük harfli tür adı");
                t.Esit(0x0D, Toplayici.FormFaktorKoduAdindan("so-dimm"), "tr-TR küçük harfli so-dimm");

                Envanter e = Toplayici.Topla(Fikstur(), 3, 4, false);
                t.Esit(1, e.DoluYuvalar.Count, "tr-TR kültüründe fikstür");
                t.Esit(KasaTipi.Dizustu, e.Sistem.Kasa, "tr-TR kasa");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = eski;
                Thread.CurrentThread.CurrentUICulture = eskiUi;
            }
        }

        static void DoldurmalikTestleri(TestKosucu t)
        {
            string[] doldurmalik = { null, "", "  ", "To Be Filled By O.E.M.", "Default string", "Not Specified", "Unknown",
                                     "00000000", "FFFFFFFF", "PartNum0", "SerNum3", "Array1_PartNumber0", "None", "System Product Name" };
            foreach (string s in doldurmalik)
                t.Dogru(Toplayici.Doldurmalik(s), "doldurmalık: <" + s + ">");
            string[] gercek = { "MTC8C1084S1SC56BD1 K", "83LK", "LENOVO", "KF432C16BB/16", "12345678", "0x80CE", "F4-3200C16D-32GVK" };
            foreach (string s in gercek)
                t.Dogru(!Toplayici.Doldurmalik(s), "gerçek değer: <" + s + ">");

            BellekYuvasi y = Modul("DIMM0", "BANK 0", 0x09, 0x1A);
            y.ParcaNo = "Unknown";
            y.SeriNo = "00000000";
            Toplayici.Zenginlestir(Sentetik(y));
            t.Esit("", y.ParcaNo, "doldurmalık parça no temizlendi");
            t.Esit("", y.SeriNo, "doldurmalık seri no temizlendi");
            t.Dogru(y.ParcaBilgisi == null, "doldurmalık parça no çözülmez");
        }

        static void WmiKodTestleri(TestKosucu t)
        {
            t.Esit(0x09, Toplayici.WmiFormFaktorunuSmbiosaCevir(8), "WMI 8 = DIMM");
            t.Esit(0x0D, Toplayici.WmiFormFaktorunuSmbiosaCevir(12), "WMI 12 = SODIMM");
            t.Esit(0x02, Toplayici.WmiFormFaktorunuSmbiosaCevir(0), "WMI 0 = Unknown");
            t.Esit(0x01, Toplayici.WmiFormFaktorunuSmbiosaCevir(1), "WMI 1 = Other");
            t.Esit(0x0A, Toplayici.WmiFormFaktorunuSmbiosaCevir(9), "WMI 9 = TSOP");
            t.Esit(0x05, Toplayici.WmiFormFaktorunuSmbiosaCevir(21), "WMI 21 BGA = Chip");
            t.Esit(0x18, Toplayici.WmiBellekTurunuSmbiosaCevir(24), "WMI MemoryType 24 = DDR3");
            t.Esit(0x13, Toplayici.WmiBellekTurunuSmbiosaCevir(21), "WMI MemoryType 21 = DDR2");
            t.Esit("DDR5", Toplayici.TurAdi(0x22), "0x22 DDR5");
            t.Esit("LPDDR5", Toplayici.TurAdi(0x23), "0x23 LPDDR5");
            t.Esit("CAMM", Toplayici.FormFaktorAdi(0x11), "0x11 CAMM");
            t.Esit(0x1E, Toplayici.TurKoduAdindan("LPDDR4X"), "LPDDR4X kodu");
        }

        static void KasaTestleri(TestKosucu t)
        {
            t.Esit(KasaTipi.Dizustu, Toplayici.KasaKodundan(0x0A), "Notebook");
            t.Esit(KasaTipi.Dizustu, Toplayici.KasaKodundan(0x09), "Laptop");
            t.Esit(KasaTipi.Dizustu, Toplayici.KasaKodundan(0x8A), "kilit bitli Notebook");
            t.Esit(KasaTipi.Masaustu, Toplayici.KasaKodundan(0x03), "Desktop");
            t.Esit(KasaTipi.Masaustu, Toplayici.KasaKodundan(0x07), "Tower");
            t.Esit(KasaTipi.HepsiBirArada, Toplayici.KasaKodundan(0x0D), "All in One");
            t.Esit(KasaTipi.MiniPc, Toplayici.KasaKodundan(0x23), "Mini PC");
            t.Esit(KasaTipi.Sunucu, Toplayici.KasaKodundan(0x17), "Rack");
            t.Esit(KasaTipi.Tablet, Toplayici.KasaKodundan(0x1E), "Tablet");
            t.Esit(KasaTipi.Bilinmiyor, Toplayici.KasaKodundan(0x02), "Unknown");

            SistemBilgisi s = new SistemBilgisi();
            s.KasaTuruKodu = 0x02;
            Toplayici.KasaBelirle(s, 0, 2, false);
            t.Esit(KasaTipi.Dizustu, s.Kasa, "PCSystemType 2 → dizüstü");

            s = new SistemBilgisi();
            s.KasaTuruKodu = 0x01;
            Toplayici.KasaBelirle(s, 0, 0, true);
            t.Esit(KasaTipi.Dizustu, s.Kasa, "pil var → dizüstü");

            s = new SistemBilgisi();
            Toplayici.KasaBelirle(s, 0x06, 1, false);
            t.Esit(KasaTipi.Masaustu, s.Kasa, "WMI kasa kodu Mini Tower");
            t.Esit(0x06, s.KasaTuruKodu, "WMI kasa kodu yazıldı");

            s = new SistemBilgisi();
            s.KasaTuruKodu = 0x03;
            Toplayici.KasaBelirle(s, 0, 2, true);
            t.Esit(KasaTipi.Masaustu, s.Kasa, "SMBIOS kasa kodu öncelikli");

            s = new SistemBilgisi();
            Toplayici.KasaBelirle(s, 0, 0, false);
            t.Esit(KasaTipi.Bilinmiyor, s.Kasa, "hiç ipucu yok");
        }

        static void SanalMakineTestleri(TestKosucu t)
        {
            string[][] vm =
            {
                new string[] { "VMware, Inc.", "VMware7,1", "VMware" },
                new string[] { "innotek GmbH", "VirtualBox", "VirtualBox" },
                new string[] { "Microsoft Corporation", "Virtual Machine", "Hyper-V" },
                new string[] { "QEMU", "Standard PC (Q35 + ICH9, 2009)", "QEMU" },
                new string[] { "Xen", "HVM domU", "Xen" },
                new string[] { "Parallels Software International Inc.", "Parallels Virtual Platform", "Parallels" },
                new string[] { "Amazon EC2", "t3.large", "Amazon EC2" },
                new string[] { "Google", "Google Compute Engine", "Google" },
            };
            foreach (string[] v in vm)
            {
                SistemBilgisi s = new SistemBilgisi();
                s.Uretici = v[0];
                s.Model = v[1];
                s.KasaTuruKodu = 0x01;
                Toplayici.SanalMakineBelirle(s, "");
                t.Dogru(s.SanalMakine, "sanal makine: " + v[0]);
                t.Esit(KasaTipi.SanalMakine, s.Kasa, "kasa SanalMakine: " + v[0]);
                t.Icerir(s.SanalMakineNotu, v[2], "not adı: " + v[0]);
            }

            string[][] fiziksel =
            {
                new string[] { "LENOVO", "83LK" },
                new string[] { "Dell Inc.", "PowerEdge R740 Intel(R) Xeon(R)" },
                new string[] { "HP", "HP EliteBook 840 G8 Notebook PC" },
                new string[] { "Microsoft Corporation", "Surface Laptop 5" },
                new string[] { "ASUSTeK COMPUTER INC.", "ROG Strix G15" },
            };
            foreach (string[] f in fiziksel)
            {
                SistemBilgisi s = new SistemBilgisi();
                s.Uretici = f[0];
                s.Model = f[1];
                Toplayici.SanalMakineBelirle(s, "");
                t.Dogru(!s.SanalMakine, "fiziksel: " + f[0] + " " + f[1]);
            }

            SistemBilgisi b = new SistemBilgisi();
            b.Uretici = "System manufacturer";
            Toplayici.SanalMakineBelirle(b, "innotek GmbH VirtualBox");
            t.Dogru(b.SanalMakine, "ek WMI metninden sanal makine");
        }

        static void KullanimTesti(TestKosucu t)
        {
            KullanimBilgisi k = Toplayici.KullanimOku();
            t.Dogru(k != null, "KullanimOku döndü");
            if (k == null) return;
            t.Dogru(k.ToplamMB > 0, "ToplamMB > 0");
            t.Dogru(k.KullanilanMB > 0 && k.KullanilanMB <= k.ToplamMB, "KullanilanMB makul");
            t.Dogru(k.BosMB >= 0 && k.BosMB <= k.ToplamMB, "BosMB makul");
            t.Dogru(k.YuzdeKullanim > 0 && k.YuzdeKullanim <= 100, "yüzde 1..100");
            t.Dogru(k.TaahhutMB > 0, "taahhüt > 0");
            t.Dogru(k.TaahhutSiniriMB >= k.ToplamMB / 2, "taahhüt sınırı makul");
            t.Dogru(k.EnCokKullananlar.Count > 0, "süreç listesi dolu");
            t.Dogru(k.EnCokKullananlar.Count <= 15, "en fazla 15 süreç");
            bool sirali = true, adetli = true, ayri = true;
            System.Collections.Generic.HashSet<string> adlar = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < k.EnCokKullananlar.Count; i++)
            {
                SurecBellek s = k.EnCokKullananlar[i];
                if (i > 0 && k.EnCokKullananlar[i - 1].OzelMB < s.OzelMB) sirali = false;
                if (s.Adet < 1) adetli = false;
                if (!adlar.Add(s.Ad)) ayri = false;
            }
            t.Dogru(sirali, "OzelMB'ye göre azalan");
            t.Dogru(adetli, "Adet >= 1");
            t.Dogru(ayri, "aynı ad tek satır");

            Stopwatch sw = Stopwatch.StartNew();
            KullanimBilgisi k1 = Toplayici.KullanimOku();
            KullanimBilgisi k2 = Toplayici.KullanimOku();
            sw.Stop();
            t.Dogru(k1 != null && k2 != null, "ardışık çağrılar döndü");
            t.Dogru(sw.ElapsedMilliseconds < 1000, "iki ardışık KullanimOku < 1 sn (" + sw.ElapsedMilliseconds + " ms)");
        }

        static void CanliToplaTesti(TestKosucu t)
        {
            Envanter e = Toplayici.Topla();
            t.Dogru(e != null, "Topla() döndü");
            if (e == null) return;
            t.Dogru(e.Yuvalar.Count > 0, "canlı: en az bir yuva");
            t.Dogru(e.DoluYuvalar.Count > 0, "canlı: en az bir dolu yuva");
            t.Dogru(e.Kullanim.ToplamMB > 0, "canlı: kullanım okundu");
            t.Dogru(e.Sistem.KullanilabilirRamMB > 0, "canlı: kullanılabilir RAM");
            t.Dogru(e.Sistem.IsletimSistemi.Length > 0, "canlı: işletim sistemi");
            t.Dogru(e.Sistem.IslemciAdi.Length > 0, "canlı: işlemci adı");
            t.Dogru(e.Platform != null, "canlı: platform nesnesi");
            t.Icermez(string.Join("\n", e.ToplamaNotlari.ToArray()), "Dosyadan okundu", "canlı modda dosya notu yok");

            // SMBIOS yokmuş gibi: WMI yedeği (Win32_PhysicalMemory) devreye girmeli.
            Envanter w = Toplayici.Topla(null, 0, 0, true);
            t.Dogru(w != null, "WMI yedeği döndü");
            if (w == null) return;
            t.Dogru(w.DoluYuvalar.Count > 0, "WMI yedeği: dolu yuva var");
            t.Dogru(w.Yuvalar.TrueForAll(delegate(BellekYuvasi y) { return y.Kaynak == "WMI"; }), "WMI yedeği: kaynak WMI");
            t.Dogru(w.DoluYuvalar.TrueForAll(delegate(BellekYuvasi y) { return y.BoyutMB > 0; }), "WMI yedeği: boyutlar");
            t.Esit(e.ToplamTakiliMB, w.ToplamTakiliMB, "WMI ve SMBIOS toplamı aynı");
        }
    }
}
