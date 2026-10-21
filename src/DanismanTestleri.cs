// Danışman testleri — sentetik Envanter'lerle karar motorunu doğrular.
// Gerçek makine ya da dosya gerekmez; her senaryo Model.cs nesneleriyle elle kurulur.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace RamDanismani
{
    public static class DanismanTestleri
    {
        public static void Calistir(TestKosucu t)
        {
            t.Grup("Danışman — kanal anahtarı");
            t.Kos("KanalAnahtari", delegate { KanalAnahtariTestleri(t); });

            t.Grup("Danışman — Lenovo LOQ 15IAX9E (tek SODIMM, BIOS 2. yuvayı göstermiyor)");
            t.Kos("Lenovo", delegate { LenovoTesti(t); });

            t.Grup("Danışman — masaüstü 4 yuva, 2x8 DDR4 (A2/B2)");
            t.Kos("Masaustu", delegate { MasaustuTesti(t); });

            t.Grup("Danışman — tümü lehimli LPDDR5");
            t.Kos("LPDDR5", delegate { LehimliTesti(t); });

            t.Grup("Danışman — 8 GB lehimli + boş SODIMM yuvası");
            t.Kos("LehimliYuva", delegate { LehimliYuvaTesti(t); });

            t.Grup("Danışman — DDR3L dizüstü");
            t.Kos("DDR3L", delegate { Ddr3LTesti(t); });

            t.Grup("Danışman — ECC RDIMM sunucu");
            t.Kos("Sunucu", delegate { SunucuTesti(t); });

            t.Grup("Danışman — 32-bit Windows");
            t.Kos("32bit", delegate { Isletim32Testi(t); });

            t.Grup("Danışman — sanal makine");
            t.Kos("VM", delegate { SanalMakineTesti(t); });

            t.Grup("Danışman — veri yok");
            t.Kos("VeriYok", delegate { VeriYokTesti(t); });

            t.Grup("Danışman — karışık 8+16, farklı hız");
            t.Kos("Karisik", delegate { KarisikTesti(t); });

            t.Grup("Danışman — XMP kapalı");
            t.Kos("XMP", delegate { XmpTesti(t); });

            t.Grup("Danışman — dolu 2 yuva, aynı kanal, video dizisi");
            t.Kos("Diger", delegate { DigerTestler(t); });

            t.Grup("Danışman — ihtiyaç analizi");
            t.Kos("Ihtiyac", delegate { IhtiyacTestleri(t); });

            t.Grup("Danışman — sağlamlık");
            t.Kos("Saglamlik", delegate { SaglamlikTestleri(t); });

            t.Grup("Danışman — inceleme gerileme testleri");
            t.Kos("Azami/pratik", delegate { AzamiPratikTestleri(t); });
            t.Kos("Boyut bilinmiyor", delegate { BoyutBilinmiyorTesti(t); });
            t.Kos("Lehimli + gizli yuva", delegate { LehimliGizliYuvaTesti(t); });
            t.Kos("LPCAMM2 şüphesi", delegate { LpcammTesti(t); });
            t.Kos("Kanal", delegate { KanalIncelemeTestleri(t); });
            t.Kos("Senaryolar", delegate { SenaryoIncelemeTestleri(t); });
            t.Kos("Tür eşdeğerliği", delegate { TurEsdegerTesti(t); });
            t.Kos("Takma adımları", delegate { TakmaAdimIncelemeTestleri(t); });
            t.Kos("İhtiyaç: yalnız taahhüt", delegate { IhtiyacTaahhutTesti(t); });
        }

        static void AzamiPratikTestleri(TestKosucu t)
        {
            DanismanSonucu d = Danisman.Degerlendir(Lenovo());
            t.Esit(128, d.AzamiGB, "LOQ AzamiGB işlemci sınırı 128 kalır");
            t.Esit(64, d.PratikAzamiGB, "LOQ pratik azami 2 yuva x 32 GB = 64");
            t.Icerir(d.PratikAzamiMetni, "2 yuva x 32 GB", "pratik metni");

            // 8 GB'lık LOQ: BIOS 1 yuva ve 16 GB bildiriyor (aynı eksik tablo) → 16 GB'a kesilmemeli
            Envanter e = Lenovo();
            e.Yuvalar[0].BoyutMB = 8192;
            d = Danisman.Degerlendir(e);
            t.Dogru(d.AzamiGB >= 32, "8 GB LOQ: şüpheli yuvada BIOS azamisi (16 GB) kullanılmaz");
            t.Dogru(d.Senaryolar.Any(s => s.Alinacaklar.Count > 0 && s.Alinacaklar[0].Adet == 1 && s.Alinacaklar[0].ModulGB == 8 && s.SonucToplamGB == 16), "8 GB ekle → 16 GB");
            t.Dogru(d.Senaryolar.Any(s => s.SonucToplamGB == 32), "en az bir 32 GB seçeneği");
        }

        static void BoyutBilinmiyorTesti(TestKosucu t)
        {
            Envanter e = Env(KasaTipi.Dizustu, Plat("Intel Core 11. nesil (Tiger Lake) — U", 0, 3200, 2, 64));
            e.Diziler.Add(Dizi(65536, 2));
            BellekYuvasi a = Modul("DIMM A", "BANK 0", 8, "DDR4", "SODIMM", 3200, 3200);
            a.BoyutMB = 0;   // BIOS 0xFFFF, parça no da çözülemedi
            e.Yuvalar.Add(a);
            e.Yuvalar.Add(Modul("DIMM B", "BANK 2", 8, "DDR4", "SODIMM", 3200, 3200));
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Esit(YukseltmeDurumu.Bilinmiyor, d.Durum, "boyut bilinmiyor → Durum Bilinmiyor");
            t.Esit(0, d.Senaryolar.Count, "senaryo üretilmez");
            t.Esit(false, d.Alinacak.Gecerli, "satın alma yok");
            t.Dogru(BulguVar(d, Seviye.Uyari, "boyutu BIOS'ta bilinmiyor"), "uyarı bulgusu");
            t.Dogru(!BulguVar(d, Seviye.Bilgi, "flex mode"), "0 GB'lık modülle flex mode yok");
        }

        static void LehimliGizliYuvaTesti(TestKosucu t)
        {
            Envanter e = Env(KasaTipi.Dizustu, Plat("Intel Core 11. nesil (Tiger Lake) — U", 0, 3200, 2, 64));
            e.Sistem.Uretici = "LENOVO";
            e.Diziler.Add(Dizi(16384, 1));
            BellekYuvasi y = Modul("ChannelA-DIMM0", "BANK 0", 8, "DDR4", "Row of chips", 3200, 3200);
            y.Lehimli = true;
            e.Yuvalar.Add(y);
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Dogru(d.Durum != YukseltmeDurumu.Hayir, "lehimli + gizli yuva olasılığı: Yükseltilemez denmez");
            t.Dogru(!BulguVarHerhangi(d.Bulgular, "boş yuvaya modül takılmadığı"), "olmayan boş yuvadan söz edilmez");
            t.Dogru(BulguVar(d, Seviye.Uyari, "Ek SODIMM yuvası"), "yuva doğrulama uyarısı");
            t.Dogru(d.Senaryolar.Any(s => s.Ad.StartsWith("Yuva varsa", StringComparison.Ordinal) && s.SonucToplamGB == 16), "koşullu 8 GB SODIMM senaryosu");
            t.Esit(1, d.SupheliEkYuva, "haritada 1 muhtemel yuva");

            // Gerçekten yuvasız LPDDR cihaz: kesin Hayır kalır
            Envanter e2 = Env(KasaTipi.Dizustu, Plat("Intel Core 11. nesil (Tiger Lake) — U", 0, 3200, 2, 64));
            e2.Diziler.Add(Dizi(16384, 2));
            BellekYuvasi l1 = Modul("ChannelA", "BANK 0", 8, "LPDDR4", "Row of chips", 4267, 4267); l1.Lehimli = true;
            BellekYuvasi l2 = Modul("ChannelB", "BANK 1", 8, "LPDDR4", "Row of chips", 4267, 4267); l2.Lehimli = true;
            e2.Yuvalar.Add(l1); e2.Yuvalar.Add(l2);
            t.Esit(YukseltmeDurumu.Hayir, Danisman.Degerlendir(e2).Durum, "LPDDR4 lehimli: Hayır");
        }

        static void LpcammTesti(TestKosucu t)
        {
            Envanter e = Env(KasaTipi.Dizustu, Plat("Intel Core Ultra (Meteor Lake)", 5600, 0, 2, 96));
            e.Platform.DesteklenenTurler.Add("LPDDR5X"); e.Platform.MaxHizMTs["LPDDR5X"] = 7467;
            e.Diziler.Add(Dizi(65536, 1));
            BellekYuvasi y = Modul("Controller0-ChannelA", "BANK 0", 32, "LPDDR5", "", 7467, 7467);
            y.FormFaktorKodu = 0x01; y.Lehimli = true; y.LehimliSupheli = true;
            e.Yuvalar.Add(y);
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Esit(YukseltmeDurumu.Bilinmiyor, d.Durum, "form belirsiz LPDDR5: Durum Bilinmiyor");
            t.Esit(false, d.Alinacak.Gecerli, "satın alma önerisi yok");
            t.Dogru(BulguVar(d, Seviye.Uyari, "LPCAMM2"), "LPCAMM2 uyarısı");
            t.Icermez(d.OzetCumle, "cihazı değiştirmek gerekir", "cihaz değişimi önerilmez");
        }

        static void KanalIncelemeTestleri(TestKosucu t)
        {
            // İki modül de aynı konum adını bildiriyor (BIOS etiket hatası), boş yuva yok
            Envanter e = Env(KasaTipi.Dizustu, Plat("Intel Core 11. nesil (Tiger Lake) — U", 0, 3200, 2, 64));
            e.Diziler.Add(Dizi(65536, 2));
            e.Yuvalar.Add(Modul("ChannelA-DIMM0", "BANK 0", 8, "DDR4", "SODIMM", 3200, 3200));
            e.Yuvalar.Add(Modul("ChannelA-DIMM0", "BANK 0", 8, "DDR4", "SODIMM", 3200, 3200));
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Icerir(d.KanalDurumu, "Doğrulanamadı", "tutarsız yuva adı: kanal doğrulanamadı");
            t.Dogru(!BulguVar(d, Seviye.Uyari, "aynı kanalda"), "aynı kanal uyarısı yok");
            t.Dogru(!BulguVarHerhangi(d.Bulgular, "diğer kanalın yuvasına taşı"), "taşıma tavsiyesi yok");

            // Dell OptiPlex tarzı adlar (DIMM1..4, banka boş): çift kanal doğrulanamaz
            e = Env(KasaTipi.Masaustu, Plat("Intel Core 10. nesil (Comet Lake)", 0, 2666, 2, 128));
            e.Diziler.Add(Dizi(65536, 4));
            e.Yuvalar.Add(Modul("DIMM1", "", 8, "DDR4", "DIMM", 2666, 2666));
            e.Yuvalar.Add(Modul("DIMM2", "", 8, "DDR4", "DIMM", 2666, 2666));
            e.Yuvalar.Add(BosYuva("DIMM3", "", "DIMM"));
            e.Yuvalar.Add(BosYuva("DIMM4", "", "DIMM"));
            d = Danisman.Degerlendir(e);
            t.Dogru(!BulguVar(d, Seviye.Iyi, "Çift kanal çalışıyor"), "doğrulanmamış kanal için İyi yok");
            t.Dogru(BulguVar(d, Seviye.Bilgi, "Çift kanal muhtemel"), "muhtemel çift kanal bilgisi");
        }

        static void SenaryoIncelemeTestleri(TestKosucu t)
        {
            // 8 lehimli + 8 SODIMM: yuvadaki modülü büyütmek flex mode'dur, tek kanal değil
            Envanter e = Env(KasaTipi.Dizustu, Plat("Intel Core 11. nesil (Tiger Lake) — U", 0, 3200, 2, 64));
            e.Diziler.Add(Dizi(65536, 2));
            BellekYuvasi l = Modul("ChannelA-DIMM0", "BANK 0", 8, "DDR4", "Row of chips", 3200, 3200); l.Lehimli = true;
            e.Yuvalar.Add(l);
            e.Yuvalar.Add(Modul("ChannelB-DIMM0", "BANK 2", 8, "DDR4", "SODIMM", 3200, 3200));
            DanismanSonucu d = Danisman.Degerlendir(e);
            YukseltmeSenaryosu s = d.Senaryolar.FirstOrDefault(x => x.Ad.StartsWith("Yuvadaki modülü büyüt", StringComparison.Ordinal));
            t.Dogru(s != null && s.Flex, "SenYuvaBuyut flex");
            t.Dogru(s != null && Rapor.KanalMetni(s) != "tek kanal", "SenYuvaBuyut kanal metni tek kanal değil");

            // 8 GB DDR5 SODIMM, 1 boş yuva, güvenilir azami 16 GB → gereksiz 2x8 kit yok
            e = Env(KasaTipi.Dizustu, Plat("Intel Core 12. nesil (Alder Lake) — HX mobil", 4800, 3200, 2, 128));
            e.Diziler.Add(Dizi(16384, 2));
            e.Yuvalar.Add(Modul("ChannelA-DIMM0", "BANK 0", 8, "DDR5", "SODIMM", 4800, 4800));
            e.Yuvalar.Add(BosYuva("ChannelB-DIMM0", "BANK 2", "SODIMM"));
            d = Danisman.Degerlendir(e);
            t.Dogru(!d.Senaryolar.Any(x => x.Ad.StartsWith("Hepsini eş kitle", StringComparison.Ordinal)), "aynı sonuçlu kit listelenmez");
            YukseltmeSenaryosu tv = Tavsiye(d);
            t.Dogru(tv != null && tv.SonucToplamGB == 16 && tv.Alinacaklar[0].Adet == 1, "tavsiye: 1 x 8 GB ekle");

            // Sunucu senaryosu "tek kanal" yazmaz
            YukseltmeSenaryosu sn = new YukseltmeSenaryosu(); sn.Sunucu = true;
            t.Icerir(Rapor.KanalMetni(sn), "kılavuz", "sunucu kanal metni");
        }

        static void TurEsdegerTesti(TestKosucu t)
        {
            Envanter e = Env(KasaTipi.Dizustu, Plat("Intel Core 11. nesil (Tiger Lake) — U", 0, 3200, 2, 64));
            e.Platform.DesteklenenTurler.Add("LPDDR4X"); e.Platform.MaxHizMTs["LPDDR4X"] = 4267;
            e.Diziler.Add(Dizi(16384, 2));
            BellekYuvasi l1 = Modul("ChannelA", "BANK 0", 8, "LPDDR4", "Row of chips", 4267, 4267); l1.Lehimli = true;
            BellekYuvasi l2 = Modul("ChannelB", "BANK 1", 8, "LPDDR4", "Row of chips", 4267, 4267); l2.Lehimli = true;
            e.Yuvalar.Add(l1); e.Yuvalar.Add(l2);
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Dogru(!BulguVarHerhangi(d.Bulgular, "uyuşmuyor"), "LPDDR4 ~ LPDDR4X: uyuşmuyor bulgusu yok");

            e = Env(KasaTipi.Dizustu, Plat("Intel Core Ultra (Meteor Lake)", 5600, 0, 2, 96));
            e.Platform.DesteklenenTurler.Add("LPDDR5X"); e.Platform.MaxHizMTs["LPDDR5X"] = 7467;
            e.Diziler.Add(Dizi(32768, 2));
            BellekYuvasi m1 = Modul("ChannelA", "BANK 0", 8, "LPDDR5", "Row of chips", 7467, 7467); m1.Lehimli = true;
            BellekYuvasi m2 = Modul("ChannelB", "BANK 1", 8, "LPDDR5", "Row of chips", 7467, 7467); m2.Lehimli = true;
            e.Yuvalar.Add(m1); e.Yuvalar.Add(m2);
            d = Danisman.Degerlendir(e);
            t.Dogru(!BulguVarHerhangi(d.Bulgular, "uyuşmuyor"), "LPDDR5 ~ LPDDR5X: uyuşmuyor bulgusu yok");
        }

        static void TakmaAdimIncelemeTestleri(TestKosucu t)
        {
            // Lenovo mini PC (ThinkCentre Tiny): pil talimatı yok
            Envanter e = Env(KasaTipi.MiniPc, Plat("Intel Core 12. nesil (Alder Lake)", 4800, 3200, 2, 64));
            e.Sistem.Uretici = "LENOVO";
            e.Diziler.Add(Dizi(65536, 2));
            e.Yuvalar.Add(Modul("ChannelA-DIMM0", "BANK 0", 8, "DDR4", "SODIMM", 3200, 3200));
            e.Yuvalar.Add(BosYuva("ChannelB-DIMM0", "BANK 2", "SODIMM"));
            DanismanSonucu d = Danisman.Degerlendir(e);
            string adimlar = string.Join("\n", d.TakmaAdimlari.ToArray());
            t.Icermez(adimlar, "Disable Built-in Battery", "mini PC: pil devre dışı adımı yok");
            t.Icermez(adimlar, "şarj aleti", "mini PC: şarj aleti yok");
            t.Icerir(adimlar, "10–15 saniye", "mini PC: güç tuşu 10–15 sn");

            // ThinkPad: F1 > Config > Power
            e = Lenovo();
            e.Sistem.ModelAdi = "ThinkPad T14 Gen 3";
            d = Danisman.Degerlendir(e);
            adimlar = string.Join("\n", d.TakmaAdimlari.ToArray());
            t.Icerir(adimlar, "F1", "ThinkPad BIOS F1");
            t.Icerir(adimlar, "Disable Built-in Battery", "ThinkPad pil adımı");

            // Masaüstü: XMP açma önerilmez
            e = Env(KasaTipi.Masaustu, Plat("Intel Core 10. nesil (Comet Lake)", 0, 2666, 2, 128));
            e.Diziler.Add(Dizi(65536, 2));
            e.Yuvalar.Add(Modul("DIMM_A1", "BANK 0", 8, "DDR4", "DIMM", 2666, 2666));
            e.Yuvalar.Add(BosYuva("DIMM_B1", "BANK 1", "DIMM"));
            d = Danisman.Degerlendir(e);
            adimlar = string.Join("\n", d.TakmaAdimlari.ToArray());
            t.Icermez(adimlar, "İstersen BIOS'ta XMP", "XMP açma önerisi yok");
            t.Icerir(adimlar, "açmayın", "şirket PC'sinde XMP açmayın");
            t.Icerir(adimlar, "10–15 saniye", "masaüstü güç tuşu 10–15 sn");
        }

        static void IhtiyacTaahhutTesti(TestKosucu t)
        {
            Envanter e = Lenovo();
            KullanimBilgisi k = Kullanim(16000, 8800, 17000);   // kullanım %55, taahhüt %106, baskı yok
            k.SikistirilmisMB = 0; k.SayfaDosyasiKullanilanMB = 100;
            e.Kullanim = k;
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Dogru(!d.IhtiyacAnalizi.Any(b => b.Baslik.IndexOf("yetersiz", StringComparison.Ordinal) >= 0), "yalnız taahhüt > RAM: yetersiz denmez");
            t.Dogru(d.IhtiyacAnalizi.Any(b => b.Baslik.IndexOf("sınırda", StringComparison.Ordinal) >= 0), "sonuç sınırda");
            t.Dogru(!d.IhtiyacAnalizi.Any(b => b.Aciklama.IndexOf("yavaşlamanın ana nedeni", StringComparison.Ordinal) >= 0), "kesin sayfalama iddiası yok");
        }

        // ---------------------------------------------------------------------
        //  Kurucular
        // ---------------------------------------------------------------------

        static int TurKodu(string tur)
        {
            switch (tur)
            {
                case "DDR3": return 0x18;
                case "DDR4": return 0x1A;
                case "DDR5": return 0x22;
                case "LPDDR4": return 0x1E;
                case "LPDDR5": return 0x23;
                default: return 2;
            }
        }

        static int FormKodu(string form)
        {
            switch (form)
            {
                case "DIMM": return 0x09;
                case "SODIMM": return 0x0D;
                case "Row of chips": return 0x0B;
                default: return 2;
            }
        }

        static BellekYuvasi Modul(string yuva, string banka, int gb, string tur, string form, int hiz, int calisan)
        {
            BellekYuvasi y = new BellekYuvasi();
            y.Handle = 0x40;
            y.DiziHandle = 0x30;
            y.Dolu = true;
            y.BoyutMB = gb * 1024L;
            y.Yuva = yuva;
            y.Banka = banka;
            y.Tur = tur;
            y.TurKodu = TurKodu(tur);
            y.FormFaktor = form;
            y.FormFaktorKodu = FormKodu(form);
            y.HizMTs = hiz;
            y.YapilandirilmisHizMTs = calisan;
            y.VeriGenisligi = 64;
            y.ToplamGenislik = 64;
            y.TurDetay.Add("Synchronous");
            y.TurDetay.Add("Unbuffered (Unregistered)");
            y.Kaynak = "SMBIOS";
            if (tur == "DDR5") { y.MinVoltajMv = 1100; y.MaxVoltajMv = 1100; y.YapilandirilmisVoltajMv = 1100; }
            else if (tur == "DDR4") { y.MinVoltajMv = 1200; y.MaxVoltajMv = 1200; y.YapilandirilmisVoltajMv = 1200; }
            return y;
        }

        static BellekYuvasi BosYuva(string yuva, string banka, string form)
        {
            BellekYuvasi y = new BellekYuvasi();
            y.Handle = 0x41;
            y.DiziHandle = 0x30;
            y.Dolu = false;
            y.Yuva = yuva;
            y.Banka = banka;
            y.FormFaktor = form;
            y.FormFaktorKodu = FormKodu(form);
            y.Tur = "Unknown";
            y.TurKodu = 2;
            y.Kaynak = "SMBIOS";
            return y;
        }

        static BellekDizisi Dizi(long maxMB, int yuva)
        {
            BellekDizisi d = new BellekDizisi();
            d.Handle = 0x30;
            d.Konum = "System board or motherboard";
            d.Kullanim = "System memory";
            d.KullanimKodu = 3;
            d.EccTuru = "None";
            d.EccKodu = 3;
            d.MaxKapasiteMB = maxMB;
            d.YuvaSayisi = yuva;
            return d;
        }

        static PlatformBilgisi Plat(string aile, int ddr5, int ddr4, int kanal, int maxGB)
        {
            PlatformBilgisi p = new PlatformBilgisi();
            p.Bulundu = true;
            p.Aile = aile;
            if (ddr5 > 0) { p.DesteklenenTurler.Add("DDR5"); p.MaxHizMTs["DDR5"] = ddr5; }
            if (ddr4 > 0) { p.DesteklenenTurler.Add("DDR4"); p.MaxHizMTs["DDR4"] = ddr4; }
            p.KanalSayisi = kanal;
            p.MaxKapasiteGB = maxGB;
            p.Kesinlik = "Üretici belgesi";
            return p;
        }

        static Envanter Env(KasaTipi kasa, PlatformBilgisi p)
        {
            Envanter e = new Envanter();
            e.Sistem.Kasa = kasa;
            e.Platform = p ?? new PlatformBilgisi();
            e.SmbiosOkundu = true;
            return e;
        }

        static Envanter Lenovo()
        {
            Envanter e = Env(KasaTipi.Dizustu, Plat("Intel Core 12. nesil (Alder Lake) — HX mobil", 4800, 3200, 2, 128));
            e.Sistem.Uretici = "LENOVO";
            e.Sistem.Model = "83LK";
            e.Sistem.ModelAdi = "LOQ 15IAX9E";
            e.Sistem.SeriNo = "PF0ABCDE";
            e.Sistem.KasaTuruKodu = 0x0A;
            e.Sistem.IslemciAdi = "12th Gen Intel(R) Core(TM) i5-12450HX";
            e.Diziler.Add(Dizi(16384, 1));
            BellekYuvasi y = Modul("Controller0-ChannelA-DIMM0", "BANK 0", 16, "DDR5", "SODIMM", 5600, 4800);
            y.Uretici = "Micron";
            y.UreticiHam = "Micron Technology";
            y.ParcaNo = "MTC8C1084S1SC56BD1 K";
            e.Yuvalar.Add(y);
            return e;
        }

        static bool BulguVar(DanismanSonucu d, Seviye s, string parca)
        {
            return d.Bulgular.Any(b => b.Seviye == s && (b.Baslik.IndexOf(parca, StringComparison.Ordinal) >= 0));
        }

        static bool BulguVarHerhangi(List<Bulgu> l, string parca)
        {
            return l.Any(b => b.Baslik.IndexOf(parca, StringComparison.Ordinal) >= 0 || b.Aciklama.IndexOf(parca, StringComparison.Ordinal) >= 0);
        }

        static YukseltmeSenaryosu Tavsiye(DanismanSonucu d)
        {
            return d.Senaryolar.FirstOrDefault(s => s.Tavsiye);
        }

        // ---------------------------------------------------------------------
        //  Testler
        // ---------------------------------------------------------------------

        static void KanalAnahtariTestleri(TestKosucu t)
        {
            CultureInfo eski = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR"); // Türkçe I tuzağı
                bool th;
                t.Esit("C0A", Danisman.KanalAnahtari(Modul("Controller0-ChannelA-DIMM0", "BANK 0", 8, "DDR5", "SODIMM", 0, 0), out th), "Controller0-ChannelA-DIMM0");
                t.Esit("C1A", Danisman.KanalAnahtari(Modul("Controller1-ChannelA-DIMM0", "BANK 0", 8, "DDR5", "SODIMM", 0, 0), out th), "Controller1-ChannelA-DIMM0");
                t.Esit("B", Danisman.KanalAnahtari(Modul("DIMM_B2", "", 8, "DDR4", "DIMM", 0, 0), out th), "DIMM_B2");
                t.Esit("P0A", Danisman.KanalAnahtari(Modul("DIMM 0", "P0 CHANNEL A", 8, "DDR4", "DIMM", 0, 0), out th), "P0 CHANNEL A");
                t.Esit("B", Danisman.KanalAnahtari(Modul("ChannelB-DIMM0", "BANK 2", 8, "DDR4", "SODIMM", 0, 0), out th), "ChannelB-DIMM0");
                t.Esit("A", Danisman.KanalAnahtari(Modul("A1", "", 8, "DDR4", "DIMM", 0, 0), out th), "A1");
                t.Esit("A", Danisman.KanalAnahtari(Modul("ChA_DIMM0", "", 8, "DDR4", "DIMM", 0, 0), out th), "ChA_DIMM0");
                string k = Danisman.KanalAnahtari(Modul("DIMM 0", "BANK 2", 8, "DDR4", "SODIMM", 0, 0), out th);
                t.Dogru(th && k == "BANK2", "BANK 2 yalnızca tahmini ipucu");
                t.Esit("", Danisman.KanalAnahtari(Modul("Bottom-Slot 1(left)", "", 8, "DDR4", "SODIMM", 0, 0), out th), "bilinmeyen ad boş döner");
                t.Esit("DDR5", Danisman.TurAdi(Modul("x", "", 8, "ddr5", "SODIMM", 0, 0)), "TurAdi Türkçe kültürde büyütür");
                t.Esit("SODIMM", Danisman.FormAdi(Modul("x", "", 8, "DDR4", "so-dimm", 0, 0)), "FormAdi SO-DIMM");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = eski;
            }
        }

        static void LenovoTesti(TestKosucu t)
        {
            CultureInfo eski = Thread.CurrentThread.CurrentCulture;
            DanismanSonucu d;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                d = Danisman.Degerlendir(Lenovo());
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = eski;
            }
            t.Esit(false, d.YuvaBilgisiGuvenilir, "YuvaBilgisiGuvenilir=false");
            t.Dogru(d.Durum != YukseltmeDurumu.Bilinmiyor, "Durum Bilinmiyor değil");
            t.Esit(YukseltmeDurumu.Evet, d.Durum, "Durum Evet");
            t.Esit(16, d.MevcutToplamGB, "MevcutToplamGB");
            t.Dogru(BulguVar(d, Seviye.Uyari, "Yuva sayısı doğrulanmalı"), "yuva şüphesi uyarısı");
            t.Dogru(BulguVar(d, Seviye.Uyari, "Tek kanal"), "tek kanal uyarısı");
            t.Dogru(BulguVar(d, Seviye.Bilgi, "Normal"), "hız bilgi notu 'Normal' (CPU sınırı)");
            t.Dogru(BulguVar(d, Seviye.Bilgi, "DDR5 alt kanalları"), "DDR5 alt kanal açıklaması");
            t.Dogru(BulguVar(d, Seviye.Bilgi, "BIOS'un azami kapasite bilgisi güvenilmez"), "BIOS azami 16 GB güvenilmez");
            t.Esit(128, d.AzamiGB, "AzamiGB işlemci sınırı");
            t.Icerir(d.AzamiGBKaynagi, "İşlemci", "AzamiGB kaynağı");
            t.Icerir(d.KanalDurumu, "Tek kanal", "KanalDurumu");
            t.Esit(true, d.Alinacak.Gecerli, "Alinacak.Gecerli");
            t.Esit("DDR5", d.Alinacak.Tur, "Alinacak.Tur");
            t.Esit("SODIMM", d.Alinacak.FormFaktor, "Alinacak.FormFaktor");
            t.Esit(16, d.Alinacak.ModulGB, "Alinacak.ModulGB");
            t.Esit(1, d.Alinacak.Adet, "Alinacak.Adet");
            t.Esit(5600, d.Alinacak.HizMTs, "Alinacak.HizMTs");
            t.Esit(1100, d.Alinacak.VoltajMv, "Alinacak.VoltajMv");
            t.Icerir(d.Alinacak.AramaMetni, "16GB DDR5", "AramaMetni tür/boyut");
            t.Icerir(d.Alinacak.AramaMetni, "SODIMM", "AramaMetni form");
            t.Icerir(d.Alinacak.AramaMetni, "5600MHz", "AramaMetni MHz");
            t.Icerir(d.Alinacak.OnerilenParcaNo, "MTC8C1084S1SC56BD1", "OnerilenParcaNo");
            string oz = string.Join("\n", d.Alinacak.Ozellikler.ToArray());
            t.Icerir(oz, "DDR4 TAKILMAZ", "Özellikler tür");
            t.Icerir(oz, "262 pin", "Özellikler pin");
            t.Icerir(oz, "en az 4800", "Özellikler hız alt sınırı");
            t.Icerir(oz, "non-ECC", "Özellikler ECC");
            t.Icerir(oz, "Micron MTC8C1084S1SC56BD1", "Özellikler aynı parça");
            t.Dogru(d.Senaryolar.Any(s => s.SonucToplamGB == 32 && s.CiftKanal), "32 GB çift kanal senaryosu");
            YukseltmeSenaryosu tv = Tavsiye(d);
            t.Dogru(tv != null && tv.SonucToplamGB == 32 && tv.CiftKanal, "tavsiye: aynısından ekle → 32 GB çift kanal");
            t.Dogru(tv != null && tv.Ad.IndexOf("2. yuva varsa", StringComparison.Ordinal) >= 0, "tavsiye koşullu (2. yuva varsa)");
            t.Esit(1, d.Senaryolar.Count(s => s.Tavsiye), "tek tavsiye");
            t.Dogru(d.Senaryolar.Any(s => s.Ad.StartsWith("Tek yuva varsa", StringComparison.Ordinal)), "tek yuva alternatifi");
            t.Dogru(d.Senaryolar.All(s => s.Alinacaklar.All(m => m.ModulGB <= 32)), "modül başı ≤ 32 GB (DDR5 SODIMM güvenli)");
            t.Icerir(d.OzetCumle, "16 GB DDR5 SODIMM tek modül (tek kanal)", "özet: mevcut");
            t.Icerir(d.OzetCumle, "üretici belgesinden doğruladıktan sonra", "özet: doğrula");
            t.Icerir(d.OzetCumle, "32 GB çift kanala", "özet: sonuç");
            t.Icerir(d.OzetCumle, "16 GB DDR5-5600 SODIMM", "özet: modül");
            t.Dogru(d.Baglantilar.Any(b => b.Url.IndexOf("psref.lenovo.com", StringComparison.Ordinal) >= 0), "PSREF bağlantısı");
            t.Dogru(d.Baglantilar.All(b => b.Url.StartsWith("https://", StringComparison.Ordinal) && b.Url.IndexOf(' ') < 0), "bağlantılar kaçırılmış https");
            t.Dogru(d.TakmaAdimlari.Any(s => s.IndexOf("Disable Built-in Battery", StringComparison.Ordinal) >= 0), "Lenovo dahili pil adımı");
            t.Dogru(d.TakmaAdimlari.Any(s => s.IndexOf("mdsched", StringComparison.Ordinal) >= 0), "mdsched adımı");
            t.Dogru(d.KontrolListesi.Any(s => s.IndexOf("DOĞRULA", StringComparison.Ordinal) >= 0), "kontrol listesi: yuva doğrula");
            t.Dogru(d.IhtiyacAnalizi.Count == 1 && d.IhtiyacAnalizi[0].Baslik.IndexOf("ölçümü yok", StringComparison.Ordinal) >= 0, "ölçüm yok bilgisi");
        }

        static void MasaustuTesti(TestKosucu t)
        {
            Envanter e = Env(KasaTipi.Masaustu, Plat("Intel Core 10. nesil (Comet Lake) — masaüstü", 0, 2933, 2, 128));
            e.Sistem.AnakartUretici = "ASUSTeK COMPUTER INC.";
            e.Sistem.AnakartModel = "PRIME B460M-A";
            e.Sistem.Uretici = "System manufacturer";
            e.Diziler.Add(Dizi(128 * 1024, 4));
            e.Yuvalar.Add(BosYuva("DIMM_A1", "BANK 0", "DIMM"));
            e.Yuvalar.Add(Modul("DIMM_A2", "BANK 1", 8, "DDR4", "DIMM", 2933, 2933));
            e.Yuvalar.Add(BosYuva("DIMM_B1", "BANK 2", "DIMM"));
            e.Yuvalar.Add(Modul("DIMM_B2", "BANK 3", 8, "DDR4", "DIMM", 2933, 2933));
            foreach (BellekYuvasi y in e.DoluYuvalar) { y.Uretici = "Kingston"; y.ParcaNo = "KF429C17BB/8"; }
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Esit(true, d.YuvaBilgisiGuvenilir, "yuva bilgisi güvenilir");
            t.Esit(4, d.ToplamYuva, "ToplamYuva");
            t.Esit(2, d.BosYuva, "BosYuva");
            t.Icerir(d.KanalDurumu, "Çift kanal", "KanalDurumu çift");
            t.Dogru(BulguVar(d, Seviye.Iyi, "Çift kanal"), "çift kanal Iyi");
            t.Dogru(BulguVar(d, Seviye.Iyi, "birebir eş"), "modüller birebir eş");
            t.Dogru(BulguVar(d, Seviye.Iyi, "etiket hızında"), "tam hız Iyi");
            t.Dogru(!BulguVar(d, Seviye.Uyari, "Tek kanal"), "tek kanal uyarısı yok");
            YukseltmeSenaryosu tv = Tavsiye(d);
            t.Dogru(tv != null && tv.SonucToplamGB == 32 && tv.Alinacaklar[0].Adet == 2 && tv.Alinacaklar[0].ModulGB == 8, "tavsiye: 2 x 8 GB ekle → 32 GB");
            // Aynı 32 GB'ı 2 x 8 GB eklemek veriyor: daha pahalı 2x16 kit listelenmez (DDR5 4 modül istisnası yok).
            t.Dogru(!d.Senaryolar.Any(s => s.Ad.StartsWith("Hepsini eş kitle", StringComparison.Ordinal) && s.SonucToplamGB <= 32), "gereksiz 2x16 kit yok");
            t.Esit("DIMM", d.Alinacak.FormFaktor, "Alinacak DIMM");
            t.Icerir(d.Alinacak.AramaMetni, "16GB (2x8GB) DDR4 2933MHz DIMM", "AramaMetni kit biçimi");
            t.Esit(YukseltmeDurumu.Evet, d.Durum, "Durum Evet");
            t.Dogru(d.Baglantilar.Any(b => b.Url.IndexOf("QVL", StringComparison.Ordinal) >= 0), "QVL bağlantısı (anakart)");
            t.Dogru(!d.Baglantilar.Any(b => b.Url.IndexOf("System%20manufacturer", StringComparison.Ordinal) >= 0), "yer tutucu üretici adı aranmaz");
            t.Dogru(d.TakmaAdimlari.Any(s => s.IndexOf("A2 + B2", StringComparison.Ordinal) >= 0), "masaüstü A2/B2 adımı");
        }

        static void LehimliTesti(TestKosucu t)
        {
            Envanter e = Env(KasaTipi.Dizustu, Plat("Intel Core Ultra (Lunar Lake)", 0, 0, 2, 32));
            e.Diziler.Add(Dizi(32 * 1024, 2));
            BellekYuvasi a = Modul("Controller0-ChannelA", "BANK 0", 8, "LPDDR5", "Row of chips", 8533, 8533);
            BellekYuvasi b = Modul("Controller1-ChannelA", "BANK 0", 8, "LPDDR5", "Row of chips", 8533, 8533);
            e.Yuvalar.Add(a); e.Yuvalar.Add(b);
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Esit(YukseltmeDurumu.Hayir, d.Durum, "Durum Hayir");
            t.Esit(false, d.Alinacak.Gecerli, "Alinacak.Gecerli false");
            t.Esit(0, d.Senaryolar.Count, "senaryo yok");
            t.Icerir(d.OzetCumle, "lehimli", "özet: lehimli");
            t.Icerir(d.OzetCumle, "yükseltilemez", "özet: yükseltilemez");
            t.Dogru(BulguVar(d, Seviye.Uyari, "lehimli"), "lehimli uyarısı");
        }

        static void LehimliYuvaTesti(TestKosucu t)
        {
            Envanter e = Env(KasaTipi.Dizustu, Plat("Intel Core 11. nesil (Tiger Lake) — U mobil", 0, 3200, 2, 64));
            e.Diziler.Add(Dizi(64 * 1024, 2));
            BellekYuvasi l = Modul("ChannelA-DIMM0", "BANK 0", 8, "DDR4", "Row of chips", 3200, 3200);
            l.Lehimli = true;
            l.LehimliNedeni = "Row of chips";
            e.Yuvalar.Add(l);
            e.Yuvalar.Add(BosYuva("ChannelB-DIMM0", "BANK 2", "SODIMM"));
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Esit(YukseltmeDurumu.Kismen, d.Durum, "Durum Kismen");
            t.Esit(true, d.Alinacak.Gecerli, "Gecerli");
            t.Esit(8, d.Alinacak.ModulGB, "8 GB ekle");
            t.Esit("SODIMM", d.Alinacak.FormFaktor, "form boş yuvadan");
            t.Esit("DDR4", d.Alinacak.Tur, "tür lehimliden");
            YukseltmeSenaryosu tv = Tavsiye(d);
            t.Dogru(tv != null && tv.SonucToplamGB == 16 && tv.CiftKanal, "tavsiye 16 GB simetrik çift kanal");
            t.Dogru(d.Senaryolar.Any(s => s.SonucToplamGB == 24 && !s.CiftKanal), "daha büyük (flex) alternatif 24 GB");
            t.Icerir(d.OzetCumle, "lehimli", "özet lehimli");
            t.Icerir(d.OzetCumle, "16 GB çift kanala", "özet sonuç");
        }

        static void Ddr3LTesti(TestKosucu t)
        {
            PlatformBilgisi p = new PlatformBilgisi();
            p.Bulundu = true;
            p.Aile = "Intel Core 4. nesil (Haswell) — U mobil";
            p.DesteklenenTurler.Add("DDR3L"); p.DesteklenenTurler.Add("LPDDR3");
            p.MaxHizMTs["DDR3L"] = 1600;
            p.KanalSayisi = 2;
            p.MaxKapasiteGB = 16;
            Envanter e = Env(KasaTipi.Dizustu, p);
            e.Diziler.Add(Dizi(16 * 1024, 2));
            BellekYuvasi y = Modul("ChannelA-DIMM0", "BANK 0", 4, "DDR3", "SODIMM", 1600, 1600);
            y.MinVoltajMv = 1350; y.MaxVoltajMv = 1500; y.YapilandirilmisVoltajMv = 1350;
            y.Uretici = "SK hynix"; y.ParcaNo = "HMT451S6BFR8A-PB";
            e.Yuvalar.Add(y);
            e.Yuvalar.Add(BosYuva("ChannelB-DIMM0", "BANK 2", "SODIMM"));
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Esit(1350, d.Alinacak.VoltajMv, "VoltajMv 1350");
            t.Esit("DDR3L", d.Alinacak.Tur, "Tur DDR3L");
            t.Dogru(BulguVar(d, Seviye.Kritik, "DDR3L"), "kritik DDR3L uyarısı");
            t.Dogru(d.Senaryolar.Count > 0 && d.Senaryolar.All(s => s.Alinacaklar.All(m => m.ModulGB <= 8)), "modül başı ≤ 8 GB");
            t.Icerir(d.Alinacak.AramaMetni, "DDR3L", "AramaMetni DDR3L");
            t.Icerir(string.Join("\n", d.Alinacak.Ozellikler.ToArray()), "1,35 V", "şartname 1,35 V");
            t.Dogru(BulguVar(d, Seviye.Bilgi, "Eski platform"), "eski platform / SSD önerisi");
        }

        static void SunucuTesti(TestKosucu t)
        {
            PlatformBilgisi p = new PlatformBilgisi();
            p.Bulundu = true; p.Aile = "Intel Xeon Scalable"; p.DesteklenenTurler.Add("DDR4"); p.MaxHizMTs["DDR4"] = 2933;
            p.KanalSayisi = 6; p.MaxKapasiteGB = 1024; p.EccDestekli = true;
            Envanter e = Env(KasaTipi.Sunucu, p);
            BellekDizisi dz = Dizi(1024L * 1024, 8); dz.EccKodu = 6; dz.EccTuru = "Multi-bit ECC";
            e.Diziler.Add(dz);
            string[] adlar = { "CPU1_DIMM_A1", "CPU1_DIMM_B1", "CPU1_DIMM_C1", "CPU1_DIMM_D1" };
            foreach (string a in adlar)
            {
                BellekYuvasi y = Modul(a, "NODE 1", 16, "DDR4", "DIMM", 2933, 2933);
                y.ToplamGenislik = 72;
                y.TurDetay.Clear(); y.TurDetay.Add("Synchronous"); y.TurDetay.Add("Registered (Buffered)");
                y.Rank = 2;
                e.Yuvalar.Add(y);
            }
            foreach (string a in new string[] { "CPU1_DIMM_A2", "CPU1_DIMM_B2", "CPU1_DIMM_C2", "CPU1_DIMM_D2" })
                e.Yuvalar.Add(BosYuva(a, "NODE 1", "DIMM"));
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Esit(true, d.Alinacak.Kayitli, "Kayitli");
            t.Esit(true, d.Alinacak.Ecc, "Ecc");
            t.Icerir(d.Alinacak.AramaMetni, "ECC Registered", "AramaMetni ECC Registered");
            t.Dogru(BulguVar(d, Seviye.Kritik, "Registered"), "kritik RDIMM uyarısı");
            t.Dogru(Tavsiye(d) != null && Tavsiye(d).SonucToplamGB == 128, "4 modül ekle → 128 GB");
        }

        static void Isletim32Testi(TestKosucu t)
        {
            Envanter e = Env(KasaTipi.Masaustu, Plat("Intel Core 4. nesil (Haswell) — masaüstü", 0, 0, 2, 32));
            e.Platform.DesteklenenTurler.Add("DDR3"); e.Platform.MaxHizMTs["DDR3"] = 1600;
            e.Sistem.IsletimSistemi = "Microsoft Windows 10 Pro 10.0.19045";
            e.Sistem.Isletim64Bit = false;
            e.Sistem.TakiliRamMB = 8192;
            e.Sistem.KullanilabilirRamMB = 3500;
            e.Diziler.Add(Dizi(32 * 1024, 4));
            e.Yuvalar.Add(Modul("ChannelA-DIMM0", "BANK 0", 4, "DDR3", "DIMM", 1600, 1600));
            e.Yuvalar.Add(Modul("ChannelB-DIMM0", "BANK 2", 4, "DDR3", "DIMM", 1600, 1600));
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Dogru(BulguVar(d, Seviye.Kritik, "64-bit"), "32-bit Windows kritik uyarısı");
            t.Icerir(d.OzetCumle, "64-bit", "özet 64-bit uyarısı");
            t.Dogru(!BulguVar(d, Seviye.Uyari, "Donanıma ayrılan"), "32-bit'te donanım payı ayrıca uyarılmaz");

            Envanter e2 = Env(KasaTipi.Masaustu, null);
            e2.Sistem.IsletimSistemi = "";
            e2.Yuvalar.Add(Modul("DIMM_A1", "", 8, "DDR4", "DIMM", 3200, 3200));
            DanismanSonucu d2 = Danisman.Degerlendir(e2);
            t.Dogru(!BulguVar(d2, Seviye.Kritik, "64-bit"), "işletim sistemi bilinmiyorsa 32-bit sanma");
        }

        static void SanalMakineTesti(TestKosucu t)
        {
            Envanter e = Env(KasaTipi.SanalMakine, null);
            e.Sistem.SanalMakine = true;
            e.Sistem.SanalMakineNotu = "Hyper-V";
            e.Yuvalar.Add(Modul("M0001", "", 4, "Other", "Unknown", 0, 0));
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Dogru(d.Durum != YukseltmeDurumu.Bilinmiyor, "VM Durum Bilinmiyor değil");
            t.Esit(0, d.Senaryolar.Count, "VM senaryo yok");
            t.Esit(false, d.Alinacak.Gecerli, "VM satın alma yok");
            t.Icerir(d.OzetCumle, "hipervizör", "özet hipervizör");
            t.Dogru(BulguVar(d, Seviye.Bilgi, "Sanal makine"), "VM bulgusu");
        }

        static void VeriYokTesti(TestKosucu t)
        {
            Envanter e = Env(KasaTipi.Dizustu, null);
            e.Sistem.TakiliRamMB = 8192;
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Esit(YukseltmeDurumu.Bilinmiyor, d.Durum, "Durum Bilinmiyor");
            t.Esit(false, d.Alinacak.Gecerli, "satın alma yok");
            t.Dogru(BulguVarHerhangi(d.Bulgular, "Görev Yöneticisi"), "Görev Yöneticisi talimatı");
            t.Icerir(d.OzetCumle, "Görev Yöneticisi", "özet talimat");
            t.Esit(8, d.MevcutToplamGB, "Windows'un gördüğü miktar");
        }

        static void KarisikTesti(TestKosucu t)
        {
            Envanter e = Env(KasaTipi.Dizustu, Plat("AMD Ryzen 5000 — H mobil", 0, 3200, 2, 64));
            e.Diziler.Add(Dizi(64 * 1024, 2));
            BellekYuvasi a = Modul("DIMM 0", "P0 CHANNEL A", 8, "DDR4", "SODIMM", 2666, 2666);
            a.Uretici = "Samsung"; a.ParcaNo = "M471A1K43CB1-CTD";
            BellekYuvasi b = Modul("DIMM 0", "P0 CHANNEL B", 16, "DDR4", "SODIMM", 3200, 2666);
            b.Uretici = "Kingston"; b.ParcaNo = "KVR32S22D8/16";
            e.Yuvalar.Add(a); e.Yuvalar.Add(b);
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Dogru(BulguVar(d, Seviye.Bilgi, "flex mode"), "flex mode bulgusu");
            t.Dogru(BulguVar(d, Seviye.Uyari, "Farklı hızlarda"), "farklı hız uyarısı");
            t.Dogru(BulguVar(d, Seviye.Bilgi, "Farklı marka"), "farklı marka bilgisi");
            t.Icerir(d.KanalDurumu, "flex", "KanalDurumu flex");
            YukseltmeSenaryosu tv = Tavsiye(d);
            t.Dogru(tv != null && tv.Ad.StartsWith("Küçük modülü", StringComparison.Ordinal) && tv.SonucToplamGB == 32 &&
                    tv.Alinacaklar[0].Adet == 1 && tv.Alinacaklar[0].ModulGB == 16, "tavsiye: yalnız 8 GB'ı 1 x 16 GB ile değiştir");
            t.Dogru(tv != null && tv.Cikarilacaklar.Count == 1 && tv.CiftKanal, "yalnız küçük modül çıkar, çift kanal");
            t.Dogru(d.Senaryolar.Any(s => s.Ad.StartsWith("Hepsini eş kitle", StringComparison.Ordinal)), "alternatif eş kit hâlâ listede");
            t.Esit(3200, d.Alinacak.HizMTs, "kit hızı platform azamisi");
            t.Esit(YukseltmeDurumu.Evet, d.Durum, "Durum Evet");
        }

        static void XmpTesti(TestKosucu t)
        {
            Envanter e = Env(KasaTipi.Masaustu, Plat("AMD Ryzen 5000 — masaüstü", 0, 3200, 2, 128));
            e.Diziler.Add(Dizi(128 * 1024, 4));
            e.Yuvalar.Add(Modul("DIMM 1", "P0 CHANNEL A", 16, "DDR4", "DIMM", 3200, 2133));
            e.Yuvalar.Add(Modul("DIMM 1", "P0 CHANNEL B", 16, "DDR4", "DIMM", 3200, 2133));
            e.Yuvalar.Add(BosYuva("DIMM 0", "P0 CHANNEL A", "DIMM"));
            e.Yuvalar.Add(BosYuva("DIMM 0", "P0 CHANNEL B", "DIMM"));
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Dogru(BulguVar(d, Seviye.Uyari, "XMP"), "XMP uyarısı");
            t.Dogru(!BulguVar(d, Seviye.Bilgi, "Normal"), "işlemci sınırı denmemeli");
            t.Icerir(d.KanalDurumu, "Çift kanal", "çift kanal");

            // Parça numarasından çözülen XMP hızı (SMBIOS JEDEC 2133 gösteriyor)
            Envanter e2 = Env(KasaTipi.Masaustu, Plat("Intel Core 12. nesil — masaüstü", 4800, 3200, 2, 128));
            BellekYuvasi y = Modul("Controller0-DIMMA2", "BANK 0", 8, "DDR4", "DIMM", 2133, 2133);
            ParcaCozumu pc = new ParcaCozumu(); pc.Bulundu = true; pc.HizMTs = 3600; pc.Tur = "DDR4"; pc.KapasiteGB = 8;
            y.ParcaBilgisi = pc;
            e2.Yuvalar.Add(y);
            DanismanSonucu d2 = Danisman.Degerlendir(e2);
            t.Dogru(BulguVar(d2, Seviye.Uyari, "XMP"), "parça no hızıyla XMP uyarısı");
        }

        static void DigerTestler(TestKosucu t)
        {
            // 2 yuvalı dizüstü, ikisi de dolu → kit ile değiştir
            Envanter e = Env(KasaTipi.Dizustu, Plat("Intel Core 13. nesil — H mobil", 5200, 3200, 2, 96));
            e.Diziler.Add(Dizi(64 * 1024, 2));
            e.Yuvalar.Add(Modul("Controller0-ChannelA-DIMM0", "BANK 0", 8, "DDR5", "SODIMM", 5200, 5200));
            e.Yuvalar.Add(Modul("Controller1-ChannelA-DIMM0", "BANK 0", 8, "DDR5", "SODIMM", 5200, 5200));
            DanismanSonucu d = Danisman.Degerlendir(e);
            YukseltmeSenaryosu tv = Tavsiye(d);
            t.Dogru(tv != null && tv.Alinacaklar[0].Adet == 2 && tv.Alinacaklar[0].ModulGB == 16 && tv.SonucToplamGB == 32, "dolu 2 yuva → 2x16 kit");
            t.Dogru(tv != null && tv.Cikarilacaklar.Count == 2, "çıkarılacak 2 modül");
            t.Icerir(d.OzetCumle, "boş yuva yok", "özet: boş yuva yok");
            t.Icerir(d.KanalDurumu, "Çift kanal", "Controller0/1 çift kanal");

            // Aynı kanalda iki modül (masaüstü A1 + A2)
            Envanter e2 = Env(KasaTipi.Masaustu, Plat("Intel Core 10. nesil — masaüstü", 0, 2933, 2, 128));
            e2.Diziler.Add(Dizi(128 * 1024, 4));
            e2.Yuvalar.Add(Modul("DIMM_A1", "BANK 0", 8, "DDR4", "DIMM", 2933, 2933));
            e2.Yuvalar.Add(Modul("DIMM_A2", "BANK 1", 8, "DDR4", "DIMM", 2933, 2933));
            e2.Yuvalar.Add(BosYuva("DIMM_B1", "BANK 2", "DIMM"));
            e2.Yuvalar.Add(BosYuva("DIMM_B2", "BANK 3", "DIMM"));
            DanismanSonucu d2 = Danisman.Degerlendir(e2);
            t.Dogru(BulguVar(d2, Seviye.Uyari, "aynı kanalda"), "aynı kanal uyarısı");
            t.Icerir(d2.OzetCumle, "ücretsiz", "özet ücretsiz düzeltme");

            // Video dizisi ve ona bağlı yuva sayılmaz
            Envanter e3 = Lenovo();
            BellekDizisi v = new BellekDizisi(); v.Handle = 0x50; v.KullanimKodu = 4; v.Kullanim = "Video memory"; v.YuvaSayisi = 1; v.MaxKapasiteMB = 8192;
            e3.Diziler.Add(v);
            BellekYuvasi vy = Modul("VRAM", "", 8, "Other", "Chip", 0, 0); vy.DiziHandle = 0x50;
            e3.Yuvalar.Add(vy);
            DanismanSonucu d3 = Danisman.Degerlendir(e3);
            t.Esit(16, d3.MevcutToplamGB, "video belleği sayılmaz");
            t.Esit(1, d3.ToplamYuva, "video yuvası sayılmaz");

            // BIOS güvenilir ve işlemciden küçük: BIOS kaynak olur
            Envanter e4 = Env(KasaTipi.Masaustu, Plat("Intel Core 12. nesil — masaüstü", 4800, 3200, 2, 128));
            e4.Diziler.Add(Dizi(64 * 1024, 2));
            e4.Yuvalar.Add(Modul("Controller0-DIMMA1", "BANK 0", 16, "DDR4", "DIMM", 3200, 3200));
            e4.Yuvalar.Add(BosYuva("Controller1-DIMMB1", "BANK 0", "DIMM"));
            DanismanSonucu d4 = Danisman.Degerlendir(e4);
            t.Esit(64, d4.AzamiGB, "BIOS azamisi (64) işlemciden (128) küçük");
            t.Esit("Üretici BIOS'u", d4.AzamiGBKaynagi, "kaynak BIOS");
            t.Esit(true, d4.YuvaBilgisiGuvenilir, "2 yuva listelenmiş: güvenilir");
        }

        static KullanimBilgisi Kullanim(long toplam, long kullanilan, long taahhut)
        {
            KullanimBilgisi k = new KullanimBilgisi();
            k.ToplamMB = toplam;
            k.KullanilanMB = kullanilan;
            k.BosMB = toplam - kullanilan;
            k.YuzdeKullanim = (int)(kullanilan * 100 / toplam);
            k.TaahhutMB = taahhut;
            k.TaahhutSiniriMB = toplam + 4096;
            k.Zaman = DateTime.Now;
            SurecBellek s = new SurecBellek(); s.Ad = "chrome.exe"; s.OzelMB = 2500; s.CalismaKumesiMB = 1800; s.Adet = 25;
            k.EnCokKullananlar.Add(s);
            return k;
        }

        static void IhtiyacTestleri(TestKosucu t)
        {
            Envanter e = Lenovo();
            e.Kullanim = Kullanim(16000, 14000, 19200); // taahhüt %120
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Dogru(d.IhtiyacAnalizi.Any(b => b.Baslik.IndexOf("yetersiz", StringComparison.Ordinal) >= 0), "taahhüt %120 → yetersiz");
            t.Dogru(d.IhtiyacAnalizi.Any(b => b.Baslik.IndexOf("fazla bellek ayrılmış", StringComparison.Ordinal) >= 0), "taahhüt uyarısı");
            t.Dogru(BulguVarHerhangi(d.IhtiyacAnalizi, "Tarayıcılar"), "tarayıcı yorumu");

            Envanter e2 = Lenovo();
            e2.Kullanim = Kullanim(16000, 5000, 6400); // taahhüt %40
            DanismanSonucu d2 = Danisman.Degerlendir(e2);
            t.Dogru(d2.IhtiyacAnalizi.Any(b => b.Baslik.IndexOf("yeterli", StringComparison.Ordinal) >= 0 && b.Seviye == Seviye.Iyi), "taahhüt %40 → yeterli");
            t.Dogru(!d2.IhtiyacAnalizi.Any(b => b.Baslik.IndexOf("yetersiz", StringComparison.Ordinal) >= 0), "yeterli durumda yetersiz denmez");

            // Çift kanal + yeterli + 16 GB → yükseltme gerekmiyor
            Envanter e3 = Env(KasaTipi.Dizustu, Plat("Intel Core 13. nesil — H mobil", 5200, 3200, 2, 96));
            e3.Diziler.Add(Dizi(64 * 1024, 2));
            e3.Yuvalar.Add(Modul("Controller0-ChannelA-DIMM0", "BANK 0", 8, "DDR5", "SODIMM", 5200, 5200));
            e3.Yuvalar.Add(Modul("Controller1-ChannelA-DIMM0", "BANK 0", 8, "DDR5", "SODIMM", 5200, 5200));
            e3.Kullanim = Kullanim(16000, 6000, 7000);
            DanismanSonucu d3 = Danisman.Degerlendir(e3);
            t.Icerir(d3.OzetCumle, "gerekmiyor", "yeterli + çift kanal → gerekmiyor");
        }

        static void SaglamlikTestleri(TestKosucu t)
        {
            DanismanSonucu d = null;
            bool hata = false;
            try { d = Danisman.Degerlendir(null); } catch { hata = true; }
            t.Dogru(!hata && d != null, "null envanter istisna fırlatmaz");
            t.Esit(YukseltmeDurumu.Bilinmiyor, d != null ? d.Durum : YukseltmeDurumu.Evet, "null → Bilinmiyor");

            Envanter e = new Envanter();
            e.Sistem = null; e.Platform = null; e.Kullanim = null; e.Diziler = null;
            e.Yuvalar = new List<BellekYuvasi>();
            e.Yuvalar.Add(null);
            BellekYuvasi tuhaf = new BellekYuvasi();
            tuhaf.Dolu = true; tuhaf.BoyutMB = 0; tuhaf.Yuva = null; tuhaf.Banka = null; tuhaf.Tur = null; tuhaf.FormFaktor = null;
            tuhaf.TurDetay = null; tuhaf.ParcaNo = null; tuhaf.Uretici = null;
            e.Yuvalar.Add(tuhaf);
            hata = false;
            try { d = Danisman.Degerlendir(e); } catch { hata = true; }
            t.Dogru(!hata && d != null && d.OzetCumle != "", "null alanlı envanter istisna fırlatmaz ve özet üretir");

            // Kullanım verisi tuhaf (sıfır bölme riski)
            Envanter e2 = Lenovo();
            e2.Kullanim.ToplamMB = 1; e2.Kullanim.EnCokKullananlar = null;
            hata = false;
            try { d = Danisman.Degerlendir(e2); } catch { hata = true; }
            t.Dogru(!hata, "bozuk kullanım verisi istisna fırlatmaz");
        }
    }
}
