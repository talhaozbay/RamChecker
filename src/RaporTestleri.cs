// Rapor modülü testleri (RamChecker.exe --test).
// DanismanSonucu elle kurulur; Danisman modülüne bağımlı değildir.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RamDanismani
{
    public static class RaporTestleri
    {
        public static void Calistir(TestKosucu t)
        {
            t.Grup("Rapor");
            t.Kos("Sayfa ve CSS", delegate { SayfaVeCss(t); });
            t.Kos("Özet", delegate { Ozet(t); });
            t.Kos("Modüller", delegate { Moduller(t); });
            t.Kos("HTML güvenliği", delegate { Guvenlik(t); });
            t.Kos("Yükseltme", delegate { Yukseltme(t); });
            t.Kos("Kullanım", delegate { Kullanim(t); });
            t.Kos("Tam rapor", delegate { TamRapor(t); });
            t.Kos("Düz metin", delegate { Metin(t); });
            t.Kos("Alışveriş metni", delegate { Alisveris(t); });
            t.Kos("CSV", delegate { Csv(t); });
            t.Kos("Boş/null girdiler", delegate { BosGirdiler(t); });
            t.Kos("Rehber içeriği", delegate { RehberIcerik(t); });
            t.Kos("İnceleme düzeltmeleri", delegate { IncelemeTestleri(t); });
        }

        static int Say(string metin, string parca)
        {
            int n = 0, i = 0;
            while ((i = metin.IndexOf(parca, i, StringComparison.Ordinal)) >= 0) { n++; i += parca.Length; }
            return n;
        }

        static void IncelemeTestleri(TestKosucu t)
        {
            // Sanal makine ve veri yok: "Muhtemel N. yuva" kutusu çıkmaz
            ModulTanimi m = new ModulTanimi();
            m.Yuva = "RAM slot #0"; m.BoyutMB = 8192; m.TurKodu = 0x1A; m.Hiz = 3200; m.YapHiz = 3200;
            byte[] vm = SmbiosKurucu.Tablo(
                SmbiosKurucu.Sistem("VMware, Inc.", "VMware Virtual Platform", "None", "VMware-56 4d"),
                SmbiosKurucu.Dizi(0x1000, 65536, 64),
                SmbiosKurucu.Modul(m));
            Envanter e = Toplayici.Topla(vm, 3, 3, false);
            DanismanSonucu d = Danisman.Degerlendir(e);
            t.Icermez(Rapor.TamRaporHtml(e, d), "Muhtemel", "VM: HTML'de muhtemel yuva yok");
            t.Icermez(Rapor.Metin(e, d), "Muhtemel", "VM: metinde muhtemel yuva yok");

            byte[] bos = SmbiosKurucu.Tablo(SmbiosKurucu.Sistem("LENOVO", "20XX", "ThinkPad", "PF0"));
            e = Toplayici.Topla(bos, 3, 3, false);
            d = Danisman.Degerlendir(e);
            string h = Rapor.TamRaporHtml(e, d);
            t.Icermez(h, "Muhtemel", "veri yok: muhtemel yuva yok");
            t.Icerir(h, "Bellek yuvası bilgisi okunamadı", "veri yok: okunamadı notu");

            // Rehber bölümleri: çift başlık / yinelenen id yok
            h = Rapor.TamRaporHtml(LoqEnvanter(), Danisman.Degerlendir(LoqEnvanter()));
            t.Esit(1, Say(h, "id=\"sozluk\""), "id=sozluk tek");
            t.Esit(1, Say(h, "id=\"sss\""), "id=sss tek");
            t.Esit(1, Say(h, "id=\"yeni\""), "id=yeni tek");

            // Azami kapasite kartı pratik değeri de gösterir
            DanismanSonucu dl = Danisman.Degerlendir(LoqEnvanter());
            if (dl.PratikAzamiGB > 0)
            {
                t.Icerir(Rapor.TamRaporHtml(LoqEnvanter(), dl), "pratikte", "azami kartında pratik değer");
                t.Icerir(Rapor.Metin(LoqEnvanter(), dl), dl.PratikAzamiGB + " GB pratikte", "metinde pratik azami ana değer");
            }
        }

        // ---- örnek veriler ---------------------------------------------------

        /// <summary>Bu makine: Lenovo LOQ 15IAX9E, tek 16 GB DDR5 SODIMM, BIOS 2. yuvayı göstermiyor.</summary>
        static Envanter LoqEnvanter()
        {
            Envanter e = new Envanter();
            e.SmbiosOkundu = true;
            e.Zaman = new DateTime(2026, 10, 20, 9, 30, 0);
            e.Sistem.Uretici = "LENOVO";
            e.Sistem.Model = "83LK";
            e.Sistem.ModelAdi = "LOQ 15IAX9E";
            e.Sistem.SeriNo = "PF0ABCDE";
            e.Sistem.IslemciAdi = "12th Gen Intel(R) Core(TM) i5-12450HX";
            e.Sistem.CekirdekSayisi = 8;
            e.Sistem.IsParcacigiSayisi = 12;
            e.Sistem.IsletimSistemi = "Microsoft Windows 11 Pro 10.0.26200";
            e.Sistem.Isletim64Bit = true;
            e.Sistem.Kasa = KasaTipi.Dizustu;
            e.Sistem.KasaTuruKodu = 0x0A;
            e.Sistem.SmbiosSurum = "3.4";
            e.Sistem.TakiliRamMB = 16384;
            e.Sistem.KullanilabilirRamMB = 16076;
            e.Diziler.Add(new BellekDizisi { Handle = 0x10, Konum = "System board or motherboard", Kullanim = "System memory", EccTuru = "None", EccKodu = 3, MaxKapasiteMB = 16384, YuvaSayisi = 1 });
            BellekYuvasi y = new BellekYuvasi();
            y.Handle = 0x11; y.DiziHandle = 0x10; y.Dolu = true; y.BoyutMB = 16384;
            y.Yuva = "Controller0-ChannelA-DIMM0"; y.Banka = "BANK 0";
            y.FormFaktorKodu = 0x0D; y.FormFaktor = "SODIMM"; y.TurKodu = 0x22; y.Tur = "DDR5";
            y.TurDetay.Add("Synchronous"); y.TurDetay.Add("Unbuffered (Unregistered)");
            y.HizMTs = 5600; y.YapilandirilmisHizMTs = 4800;
            y.Uretici = "Micron"; y.UreticiHam = "Micron Technology"; y.ParcaNo = "MTC8C1084S1SC56BD1 K";
            y.SeriNo = "E1234567"; y.Rank = 1; y.VeriGenisligi = 64; y.ToplamGenislik = 64;
            y.MinVoltajMv = 1100; y.MaxVoltajMv = 1100; y.YapilandirilmisVoltajMv = 1100;
            y.TeknolojiKodu = 3; y.Teknoloji = "DRAM"; y.Kaynak = "SMBIOS";
            y.ParcaBilgisi = new ParcaCozumu { Bulundu = true, Marka = "Micron", KapasiteGB = 16, Tur = "DDR5", HizMTs = 5600, FormFaktor = "SODIMM", Aciklama = "Micron 16 GB DDR5-5600 SODIMM" };
            e.Yuvalar.Add(y);
            e.Platform.Bulundu = true;
            e.Platform.Aile = "Intel Core 12. nesil (Alder Lake) — HX mobil";
            e.Platform.DesteklenenTurler.Add("DDR5"); e.Platform.DesteklenenTurler.Add("DDR4");
            e.Platform.MaxHizMTs["DDR5"] = 4800; e.Platform.MaxHizMTs["DDR4"] = 3200;
            e.Platform.MaxKapasiteGB = 128; e.Platform.KanalSayisi = 2;
            e.Platform.Kesinlik = "Üretici belgesi";
            e.ToplamaNotlari.Add("WMI ile karşılaştırıldı.");
            e.Kullanim.ToplamMB = 16076; e.Kullanim.KullanilanMB = 11735; e.Kullanim.BosMB = 4341; e.Kullanim.YuzdeKullanim = 73;
            e.Kullanim.TaahhutMB = 19000; e.Kullanim.TaahhutSiniriMB = 26000; e.Kullanim.SayfaDosyasiMB = 10000; e.Kullanim.SayfaDosyasiKullanilanMB = 2100;
            e.Kullanim.OnbellekMB = 3000; e.Kullanim.Zaman = e.Zaman;
            e.Kullanim.EnCokKullananlar.Add(new SurecBellek { Ad = "chrome", OzelMB = 2900, CalismaKumesiMB = 2500, Adet = 31 });
            e.Kullanim.EnCokKullananlar.Add(new SurecBellek { Ad = "Teams", OzelMB = 900, CalismaKumesiMB = 700, Adet = 4 });
            return e;
        }

        static DanismanSonucu LoqSonuc()
        {
            DanismanSonucu d = new DanismanSonucu();
            d.Durum = YukseltmeDurumu.Evet;
            d.OzetCumle = "Aynı modülden bir tane daha (16 GB DDR5 SODIMM) takarak 32 GB çift kanala çıkın; şu an tek kanalda çalışıyorsunuz.";
            d.MevcutToplamGB = 16; d.ToplamYuva = 2; d.BosYuva = 1; d.YuvaBilgisiGuvenilir = false; d.SupheliEkYuva = 1;
            d.AzamiGB = 64; d.AzamiGBKaynagi = "Tahmin: platform + yuva sayısı (BIOS 16 GB diyor, güvenilmez)";
            d.KanalDurumu = "Tek kanal";
            d.Bulgular.Add(new Bulgu(Seviye.Bilgi, "Hız işlemci sınırında", "Modül 5600, işlemci 4800 destekliyor.\nBu normaldir."));
            d.Bulgular.Add(new Bulgu(Seviye.Uyari, "Tek kanalda çalışıyor", "Bellek bant genişliği yarıda."));
            d.Bulgular.Add(new Bulgu(Seviye.Kritik, "Kritik örnek", "Örnek kritik bulgu."));
            d.IhtiyacAnalizi.Add(new Bulgu(Seviye.Uyari, "Kullanım yüksek", "Anlık %73."));
            d.Alinacak.Gecerli = true; d.Alinacak.Tur = "DDR5"; d.Alinacak.FormFaktor = "SODIMM"; d.Alinacak.HizMTs = 5600; d.Alinacak.VoltajMv = 1100;
            d.Alinacak.ModulGB = 16; d.Alinacak.Adet = 1; d.Alinacak.AramaMetni = "16GB DDR5 5600 SODIMM";
            d.Alinacak.OnerilenParcaNo = "MTC8C1084S1SC56BD1"; d.Alinacak.Gerekce = "Eş modül çift kanalı açar.";
            d.Alinacak.Ozellikler.Add("DDR5 SODIMM (dizüstü)"); d.Alinacak.Ozellikler.Add("16 GB, 5600 MT/s, 1,1 V, non-ECC");
            YukseltmeSenaryosu s1 = new YukseltmeSenaryosu { Ad = "Boş yuvaya ekle", Aciklama = "Mevcut modül kalır.", SonucToplamGB = 32, CiftKanal = true, Tavsiye = true };
            s1.Alinacaklar.Add(new AlinacakModul { Adet = 1, ModulGB = 16, Tur = "DDR5", FormFaktor = "SODIMM", HizMTs = 5600 });
            s1.Notlar.Add("Üretici belgesinden 2. yuvayı doğrulayın.");
            YukseltmeSenaryosu s2 = new YukseltmeSenaryosu { Ad = "Hepsini değiştir (kit)", SonucToplamGB = 64, CiftKanal = true };
            s2.Alinacaklar.Add(new AlinacakModul { Adet = 2, ModulGB = 32, Tur = "DDR5", FormFaktor = "SODIMM", HizMTs = 5600 });
            s2.Cikarilacaklar.Add("Controller0-ChannelA-DIMM0");
            d.Senaryolar.Add(s2); d.Senaryolar.Add(s1);
            d.KontrolListesi.Add("Üretici belgesinde 2 yuva olduğunu doğrulayın");
            d.TakmaAdimlari.Add("Bilgisayarı kapatın ve fişi çekin");
            d.TakmaAdimlari.Add("Alt kapağı açın");
            d.Baglantilar.Add(new Baglanti("Lenovo PSREF", "https://psref.lenovo.com/"));
            return d;
        }

        /// <summary>4 yuvalı masaüstü, 2 dolu 2 boş, BIOS bilgisi güvenilir.</summary>
        static Envanter MasaustuEnvanter()
        {
            Envanter e = new Envanter();
            e.Sistem.Uretici = "Dell Inc."; e.Sistem.ModelAdi = "OptiPlex 7090"; e.Sistem.Kasa = KasaTipi.Masaustu;
            string[] adlar = { "DIMM1", "DIMM2", "DIMM3", "DIMM4" };
            for (int i = 0; i < 4; i++)
            {
                BellekYuvasi y = new BellekYuvasi { Yuva = adlar[i], FormFaktor = "DIMM", Kaynak = "SMBIOS" };
                if (i % 2 == 0)
                {
                    y.Dolu = true; y.BoyutMB = 8192; y.Tur = "DDR4"; y.HizMTs = 3200; y.YapilandirilmisHizMTs = 3200;
                    y.Uretici = "SK hynix"; y.UreticiHam = "80AD"; y.ParcaNo = "HMA81GU6DJR8N-XN"; y.TurKodu = 0x1A; y.FormFaktorKodu = 0x09;
                }
                else y.FormFaktorKodu = 0x09;
                e.Yuvalar.Add(y);
            }
            return e;
        }

        static DanismanSonucu MasaustuSonuc()
        {
            DanismanSonucu d = new DanismanSonucu();
            d.Durum = YukseltmeDurumu.Evet; d.OzetCumle = "2 boş yuvaya 2 × 8 GB DDR4 DIMM ekleyin.";
            d.ToplamYuva = 4; d.BosYuva = 2; d.YuvaBilgisiGuvenilir = true; d.AzamiGB = 128; d.KanalDurumu = "Çift kanal";
            d.Alinacak.Gecerli = true; d.Alinacak.Tur = "DDR4"; d.Alinacak.FormFaktor = "DIMM"; d.Alinacak.ModulGB = 8; d.Alinacak.Adet = 2;
            d.Alinacak.HizMTs = 3200; d.Alinacak.AramaMetni = "8GB DDR4 3200 DIMM";
            return d;
        }

        static Envanter LehimliEnvanter()
        {
            Envanter e = new Envanter();
            e.Sistem.Kasa = KasaTipi.Dizustu;
            e.Yuvalar.Add(new BellekYuvasi { Yuva = "Onboard", Dolu = true, BoyutMB = 8192, Tur = "LPDDR5", FormFaktor = "Row of chips", Lehimli = true, LehimliNedeni = "Form faktörü 'Row of chips'" });
            return e;
        }

        static DanismanSonucu LehimliSonuc()
        {
            DanismanSonucu d = new DanismanSonucu();
            d.Durum = YukseltmeDurumu.Hayir; d.OzetCumle = "RAM anakarta lehimli; yükseltilemez.";
            d.YuvaBilgisiGuvenilir = true; d.ToplamYuva = 1;
            d.Alinacak.Gecerli = false; d.Alinacak.Gerekce = "Bellek lehimli olduğu için modül eklenemez.";
            return d;
        }

        // ---- testler ---------------------------------------------------------

        static void SayfaVeCss(TestKosucu t)
        {
            string s = Rapor.Sayfa("Deneme", "<p>gövde</p>", 1.0);
            t.Icerir(s, "X-UA-Compatible", "Sayfa X-UA-Compatible içerir");
            t.Icerir(s, "content=\"IE=edge\"", "Sayfa IE=edge");
            t.Icerir(s, "<meta charset=\"utf-8\">", "Sayfa charset utf-8");
            t.Dogru(s.StartsWith("<!DOCTYPE html>", StringComparison.Ordinal), "Sayfa DOCTYPE ile başlar");
            t.Icerir(s, "<p>gövde</p>", "Sayfa gövdeyi içerir");
            t.Icerir(Rapor.Sayfa("<b>x</b>", "", 1.0), "<title>&lt;b&gt;x&lt;/b&gt;</title>", "Sayfa başlığı kaçırılır");

            foreach (double o in new double[] { 1.0, 1.25, 1.5, 2.0 })
            {
                string css = Rapor.Css(o).ToLowerInvariant();
                string ad = " (ölçek " + o.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
                t.Icermez(css, "var(--", "CSS değişkeni yok" + ad);
                t.Icermez(css.Replace(" ", ""), "display:flex", "flex yok" + ad);
                t.Icermez(css.Replace(" ", ""), "display:grid", "grid yok" + ad);
                t.Icermez(css, "inline-flex", "inline-flex yok" + ad);
            }
            string c10 = Rapor.Css(1.0);
            string c15 = Rapor.Css(1.5);
            t.Icerir(c10, "font-size:14px", "Ölçek 1.0'da gövde yazısı 14px");
            t.Icerir(c15, "font-size:21px", "Ölçek 1.5'te gövde yazısı 21px");
            t.Icerir(c15, "font-size:30px", "Ölçek 1.5'te cevap/rakam yazısı 30px (20×1.5)");
            t.Dogru(c15.Length > 0 && c15 != c10, "Ölçek CSS'i değiştirir");
            t.Esit(Rapor.Css(1.0), Rapor.Css(0), "Geçersiz ölçek (0) → 1.0");
            t.Esit(Rapor.Css(1.0), Rapor.Css(double.NaN), "Geçersiz ölçek (NaN) → 1.0");
            t.Icerir(c10, "@media print", "CSS yazdırma kuralları içerir");
            t.Icermez(Rapor.Sayfa("x", "", 1.0).ToLowerInvariant(), "<script", "Sayfa JavaScript içermez");

            // CSS sınıf sözlüğü
            foreach (string sinif in new string[] { ".kart", ".cevap", ".iyi", ".bilgi", ".uyari", ".kritik", "table.tablo", ".etiket", ".etiket-iyi", ".etiket-uyari", ".etiket-kritik",
                ".not", ".ipucu", "ul.liste", "ol.adimlar", ".kod", ".yuva-harita", ".yuva", ".yuva-dolu", ".yuva-bos", ".yuva-lehimli", ".onerilen", ".iki-sutun" })
                t.Icerir(c10, sinif, "CSS sınıfı tanımlı: " + sinif);
        }

        static void Ozet(TestKosucu t)
        {
            Envanter e = LoqEnvanter();
            DanismanSonucu d = LoqSonuc();
            string h = Rapor.OzetHtml(e, d);
            t.Icerir(h, "class=\"cevap\"", "Özet cevap kutusu");
            t.Icerir(h, "32 GB çift kanala çıkın", "Özet cümlesi görünür");
            t.Icerir(h, "Yükseltilebilir", "Evet → Yükseltilebilir etiketi");
            t.Icerir(h, "yuva yuva-dolu", "Dolu yuva kutusu");
            t.Icerir(h, "Controller0-ChannelA-DIMM0", "Yuva adı haritada");
            t.Icerir(h, "yuva-belirsiz", "Güvenilmez yuva bilgisinde '?' kutusu");
            t.Icerir(h, "Muhtemel 2. yuva (BIOS göstermiyor)", "Muhtemel 2. yuva metni");
            t.Icerir(h, "1 / 2", "Yuva rakamı dolu/toplam");
            t.Icerir(h, "5600 / 4800", "Hız etiket/çalışan");
            t.Icerir(h, "64 GB", "Azami GB");
            t.Icerir(h, "Tek kanal", "Kanal durumu");
            t.Icerir(h, "LOQ 15IAX9E", "Sistem kartında model adı");
            t.Icerir(h, "Dizüstü", "Kasa türü Türkçe");
            t.Icerir(h, "15,7 GB", "Windows'un kullanabildiği (Türkçe ondalık)");
            int kritik = h.IndexOf("Kritik örnek", StringComparison.Ordinal);
            int uyari = h.IndexOf("Tek kanalda çalışıyor", StringComparison.Ordinal);
            int bilgi = h.IndexOf("Hız işlemci sınırında", StringComparison.Ordinal);
            t.Dogru(kritik >= 0 && uyari > kritik && bilgi > uyari, "Bulgular sırası: Kritik → Uyarı → Bilgi");
            t.Icerir(h, "Modül 5600, işlemci 4800 destekliyor.<br>Bu normaldir.", "Bulgu açıklamasında satır sonu <br>");

            string m = Rapor.OzetHtml(MasaustuEnvanter(), MasaustuSonuc());
            t.Icerir(m, "yuva yuva-bos", "Boş yuva kutusu");
            t.Icerir(m, "BOŞ — buraya 8 GB DDR4 DIMM takılabilir", "Boş yuvaya ne takılabileceği");
            t.Icermez(m, "yuva-belirsiz", "Güvenilir yuva bilgisinde '?' kutusu yok");
            t.Icerir(m, "2 / 4", "Masaüstü 2/4 yuva");

            string l = Rapor.OzetHtml(LehimliEnvanter(), LehimliSonuc());
            t.Icerir(l, "yuva yuva-lehimli", "Lehimli yuva kutusu");
            t.Icerir(l, "Yükseltilemez", "Hayir → Yükseltilemez");

            DanismanSonucu k = MasaustuSonuc(); k.Durum = YukseltmeDurumu.Kismen;
            t.Icerir(Rapor.OzetHtml(MasaustuEnvanter(), k), "Kısmen", "Kismen → Kısmen");
            DanismanSonucu b = new DanismanSonucu();
            t.Icerir(Rapor.OzetHtml(new Envanter(), b), "Belirsiz", "Bilinmiyor → Belirsiz");
        }

        static void Moduller(TestKosucu t)
        {
            string h = Rapor.ModullerHtml(LoqEnvanter());
            t.Icerir(h, "MTC8C1084S1SC56BD1 K", "Parça no");
            t.Icerir(h, "Micron 16 GB DDR5-5600 SODIMM", "Parça no çözümü");
            t.Icerir(h, "Micron (ham: Micron Technology)", "Üretici normalleştirilmiş + ham");
            t.Icerir(h, "0x22", "Ham tür kodu hex");
            t.Icerir(h, "0x0D", "Ham form kodu hex");
            t.Icerir(h, Bicim.Voltaj(1100), "Voltaj Türkçe biçim");
            t.Icerir(h, "1,1", "Voltaj virgüllü");
            t.Icerir(h, "ECC yok", "ECC durumu");
            t.Icerir(h, "single rank", "Rank");
            t.Icerir(h, "DDR5-4800", "Platform hızları");
            t.Icerir(h, "System board or motherboard", "Diziler tablosu");
            t.Icerir(h, "WMI ile karşılaştırıldı.", "Toplama notları");
            string m = Rapor.ModullerHtml(MasaustuEnvanter());
            t.Icerir(m, "DIMM2", "Boş yuva da listelenir");
            t.Icerir(m, "etiket etiket-uyari\">BOŞ", "Boş yuva etiketi");
            t.Icerir(m, "SK hynix (ham: 80AD)", "JEDEC ham kod gösterimi");
        }

        static void Guvenlik(TestKosucu t)
        {
            const string xss = "<script>alert(1)</script>";
            Envanter e = LoqEnvanter();
            e.Yuvalar[0].ParcaNo = xss;
            e.Yuvalar[0].Uretici = "<img src=x onerror=alert(1)>";
            e.Yuvalar[0].Yuva = "\"><script>alert(2)</script>";
            e.Sistem.ModelAdi = xss;
            e.Sistem.Uretici = "A&B";
            e.ToplamaNotlari.Add(xss);
            e.Kullanim.EnCokKullananlar.Add(new SurecBellek { Ad = xss, OzelMB = 10 });
            DanismanSonucu d = LoqSonuc();
            d.OzetCumle = xss;
            d.Bulgular.Add(new Bulgu(Seviye.Uyari, xss, xss));
            d.Alinacak.AramaMetni = xss;
            d.Alinacak.OnerilenParcaNo = xss;
            d.Senaryolar[0].Ad = xss;
            d.KontrolListesi.Add(xss);
            d.Baglantilar.Add(new Baglanti("kötü", "javascript:alert(1)"));
            d.Baglantilar.Add(new Baglanti("kötü2", "https://ornek.com/\"><script>alert(3)</script>"));
            d.Baglantilar.Add(new Baglanti("dosya", "file:///C:/Windows/System32/calc.exe"));

            string[] ciktilar = { Rapor.OzetHtml(e, d), Rapor.ModullerHtml(e), Rapor.YukseltmeHtml(e, d), Rapor.KullanimHtml(e, d), Rapor.TamRaporHtml(e, d) };
            string[] adlar = { "Özet", "Modüller", "Yükseltme", "Kullanım", "Tam rapor" };
            for (int i = 0; i < ciktilar.Length; i++)
            {
                t.Icermez(ciktilar[i], "<script", adlar[i] + ": ham <script> yok");
                t.Icermez(ciktilar[i], "<img", adlar[i] + ": ham <img> yok");
            }
            t.Icerir(ciktilar[1], "&lt;script&gt;alert(1)&lt;/script&gt;", "Parça no kaçırılmış");
            t.Icerir(ciktilar[4], "&lt;script&gt;", "Tam raporda kaçırılmış metin var");
            t.Icerir(ciktilar[4], "A&amp;B", "& kaçırılır");
            t.Icermez(ciktilar[2], "href=\"javascript", "javascript: bağlantısı href'e konmaz");
            t.Icermez(ciktilar[2], "href=\"file", "file: bağlantısı href'e konmaz");
            t.Icerir(ciktilar[2], "<a href=\"https://psref.lenovo.com/\">Lenovo PSREF</a>", "https bağlantısı href'te");
            t.Icermez(ciktilar[2], "/\"><script", "URL içindeki tırnak kaçırılır");
        }

        static void Yukseltme(TestKosucu t)
        {
            string h = Rapor.YukseltmeHtml(LoqEnvanter(), LoqSonuc());
            t.Icerir(h, "NE ALMALIYIM?", "Ne almalıyım kutusu");
            t.Icerir(h, "kod arama-metni\">16GB DDR5 5600 SODIMM", "Arama metni kod biçiminde");
            t.Icerir(h, "1 adet 16 GB DDR5 SODIMM", "Alınacak başlığı");
            t.Icerir(h, "MTC8C1084S1SC56BD1", "Önerilen parça no");
            t.Icerir(h, "DDR4 değil DDR5", "Karışıklık uyarısı (tür)");
            t.Icerir(h, "kart onerilen", "Önerilen senaryo vurgulu");
            t.Icerir(h, "ÖNERİLEN", "ÖNERİLEN etiketi");
            int onerilen = h.IndexOf("kart onerilen", StringComparison.Ordinal);
            int bos = h.IndexOf("Boş yuvaya ekle", StringComparison.Ordinal);
            t.Dogru(onerilen >= 0 && bos > onerilen, "ÖNERİLEN etiketi doğru senaryoda");
            int kit = h.IndexOf("Hepsini değiştir (kit)", StringComparison.Ordinal);
            t.Dogru(bos >= 0 && kit > bos, "Önerilen senaryo listede önce gelir");
            t.Icerir(h, "2 × 32 GB DDR5 SODIMM, 5600 MT/s", "Senaryo alınacakları");
            t.Icerir(h, "☐", "Kontrol listesi kutucuğu");
            t.Icerir(h, "<ol class=\"adimlar\">", "Takma adımları numaralı liste");
            t.Icerir(h, "Sık yapılan hatalar", "Sık yapılan hatalar kutusu");

            string l = Rapor.YukseltmeHtml(LehimliEnvanter(), LehimliSonuc());
            t.Icerir(l, "Bellek lehimli olduğu için modül eklenemez.", "Geçersiz satın almada gerekçe");
            t.Icermez(l, "arama-metni\"", "Geçersiz satın almada arama metni kutusu yok");
        }

        static void Kullanim(TestKosucu t)
        {
            string h = Rapor.KullanimHtml(LoqEnvanter(), LoqSonuc());
            t.Icerir(h, "width:73%", "Kullanım çubuğu yüzdesi");
            t.Icerir(h, "chrome", "Süreç tablosu");
            t.Icerir(h, "Kullanım yüksek", "İhtiyaç analizi bulgusu");
            Envanter e = LoqEnvanter(); e.Kullanim = new KullanimBilgisi();
            string b = Rapor.KullanimHtml(e, LoqSonuc());
            t.Icerir(b, "ölçüm yok", "Ölçüm yoksa not");
            t.Icermez(b, "class=\"cubuk\"", "Ölçüm yoksa çubuk yok");
            e.Kullanim.ToplamMB = 1000; e.Kullanim.KullanilanMB = 990; e.Kullanim.YuzdeKullanim = 150;
            t.Icerir(Rapor.KullanimHtml(e, LoqSonuc()), "width:100%", "Yüzde 100'e kırpılır");
        }

        static void TamRapor(TestKosucu t)
        {
            string h = Rapor.TamRaporHtml(LoqEnvanter(), LoqSonuc());
            t.Icerir(h, "X-UA-Compatible", "Tam rapor X-UA-Compatible");
            t.Icerir(h, "<meta charset=\"utf-8\">", "Tam rapor charset");
            t.Icerir(h, Html.Ka(Environment.MachineName), "Tam raporda bilgisayar adı");
            t.Icerir(h, "font-size:14px", "Tam rapor ölçek 1.0");
            t.Icerir(h, "İçindekiler", "İçindekiler");
            foreach (string id in new string[] { "ozet", "yukseltme", "moduller", "kullanim", "dikkat", "yeni", "sozluk", "sss" })
                t.Icerir(h, "id=\"" + id + "\"", "Bölüm kimliği: " + id);
            t.Dogru(Regex.IsMatch(h, @"Oluşturma: \d{4}-\d{2}-\d{2} \d{2}:\d{2}"), "Oluşturma zamanı biçimi");
            t.Icerir(h, "yuva-harita", "Tam raporda yuva haritası");
            t.Icerir(h, "NE ALMALIYIM?", "Tam raporda satın alma");
            t.Dogru(h.EndsWith("</html>\n", StringComparison.Ordinal), "Tam rapor kapanır");
        }

        static void Metin(TestKosucu t)
        {
            string m = Rapor.Metin(LoqEnvanter(), LoqSonuc());
            t.Icerir(m, "=== CEVAP ===", "Metin başlıkları");
            t.Icerir(m, "=== YUVALAR ===", "Yuva tablosu başlığı");
            t.Icerir(m, "Aynı modülden bir tane daha", "Metinde özet cümlesi");
            t.Icerir(m, "çift kanala çıkın;", "Türkçe karakterler bozulmaz (ç, ı)");
            t.Icerir(m, "çalışıyorsunuz.", "Türkçe karakterler bozulmaz (ş)");
            t.Icerir(m, "İşlemci", "Türkçe büyük İ bozulmaz");
            t.Icerir(m, "Yükseltilebilir", "Metinde durum");
            t.Icerir(m, "[UYARI] Tek kanalda çalışıyor — Bellek bant genişliği yarıda.", "Bulgu biçimi");
            t.Icerir(m, "[KRİTİK] Kritik örnek", "Kritik bulgu");
            t.Icerir(m, "Controller0-ChannelA-DIMM0", "Yuva tablosunda yuva adı");
            t.Icerir(m, "5600/4800", "Yuva tablosunda hız");
            t.Icerir(m, "? Muhtemel 2. yuva", "Metinde muhtemel yuva");
            t.Icerir(m, "[ÖNERİLEN]", "Metinde önerilen senaryo");
            t.Icerir(m, "16GB DDR5 5600 SODIMM", "Metinde arama metni");
            t.Icermez(m, "&lt;", "Metinde HTML kaçırma yok");
            int enUzun = m.Replace("\r", "").Split('\n').Max(s => s.Length);
            t.Dogru(enUzun <= 112, "Metin satırları ~100 karakter (en uzun: " + enUzun + ")");
            Envanter e = LoqEnvanter(); e.Yuvalar[0].ParcaNo = "A\u0007B\u001bC";
            t.Icermez(Rapor.Metin(e, LoqSonuc()), "\u001b", "Metinde kontrol karakteri temizlenir");
        }

        static void Alisveris(TestKosucu t)
        {
            string a = Rapor.AlisverisMetni(LoqSonuc());
            int satir = a.Replace("\r", "").TrimEnd('\n').Split('\n').Length;
            t.Dogru(satir >= 5 && satir <= 10, "Alışveriş metni 5–10 satır (" + satir + ")");
            t.Icerir(a, "1 adet 16 GB DDR5 SODIMM", "Adet × GB tür form");
            t.Icerir(a, "16GB DDR5 5600 SODIMM", "Mağazada aranacak metin");
            t.Icerir(a, "MTC8C1084S1SC56BD1", "Parça no");
            t.Icerir(a, "DDR4 değil DDR5", "Tür uyarısı");
            t.Icerir(a, "masaüstü (DIMM) değil dizüstü tipi kısa modül (SODIMM)", "Form uyarısı");
            t.Icerir(a, Bicim.Voltaj(1100), "Voltaj");
            t.Icerir(a, "ECC'siz", "ECC bilgisi");
            t.Icerir(Rapor.AlisverisMetni(LoqEnvanter(), LoqSonuc()), "Cihaz: LENOVO LOQ 15IAX9E (83LK)", "Cihaz modeli satırı (Envanter'li sürüm)");
            string g = Rapor.AlisverisMetni(LehimliSonuc());
            t.Icerir(g, "YOK", "Geçersiz satın alma açıklanır");
            t.Icerir(g, "Bellek lehimli olduğu için modül eklenemez.", "Geçersiz satın almada neden");
            string ms = Rapor.AlisverisMetni(MasaustuSonuc());
            t.Icerir(ms, "dizüstü (SODIMM) değil masaüstü tipi uzun modül (DIMM)", "Masaüstü form uyarısı");
            t.Icerir(ms, "DDR5 değil DDR4", "DDR4 uyarısı");
        }

        static int CsvAlanSayisi(string satir)
        {
            int n = 1; bool tirnak = false;
            foreach (char c in satir)
            {
                if (c == '"') tirnak = !tirnak;
                else if (c == ';' && !tirnak) n++;
            }
            return n;
        }

        static void Csv(TestKosucu t)
        {
            string b = Rapor.CsvBaslik();
            string s = Rapor.CsvSatir(LoqEnvanter(), LoqSonuc());
            t.Icerir(b, "Bilgisayar adı", "CSV başlığı Türkçe");
            t.Esit(CsvAlanSayisi(b), CsvAlanSayisi(s), "CSV başlık ve satır alan sayısı eşit");
            t.Dogru(Regex.IsMatch(s, @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2};"), "CSV tarih yyyy-MM-dd HH:mm");
            t.Icerir(s, ";LENOVO;LOQ 15IAX9E;83LK;", "CSV sistem alanları");
            t.Icerir(s, ";16;", "CSV toplam GB");
            t.Icerir(s, ";15,7;", "CSV kullanılabilir GB Türkçe ondalık");
            t.Icerir(s, "Yükseltilebilir", "CSV durum");

            Envanter e = LoqEnvanter();
            e.Sistem.ModelAdi = "A;B\"C";
            e.Sistem.SeriNo = "satır1\r\nsatır2";
            e.Sistem.Uretici = "=HYPERLINK(\"x\")";
            string k = Rapor.CsvSatir(e, LoqSonuc());
            t.Icerir(k, ";\"A;B\"\"C\";", "CSV ; ve \" kaçırma");
            t.Icermez(k, "\n", "CSV satırında satır sonu yok");
            t.Icermez(k, "\r", "CSV satırında CR yok");
            t.Icerir(k, "satır1 satır2", "Satır sonu boşluğa çevrilir");
            t.Icerir(k, "\"'=HYPERLINK(\"\"x\"\")\"", "Excel formül enjeksiyonu engellenir");
            t.Esit(CsvAlanSayisi(b), CsvAlanSayisi(k), "Kaçırılmış satırda alan sayısı eşit");
        }

        static void BosGirdiler(TestKosucu t)
        {
            bool hata = false;
            string[] c = new string[0];
            try
            {
                c = new string[] {
                    Rapor.OzetHtml(null, null), Rapor.ModullerHtml(null), Rapor.YukseltmeHtml(null, null), Rapor.KullanimHtml(null, null),
                    Rapor.TamRaporHtml(null, null), Rapor.Metin(null, null), Rapor.AlisverisMetni(null), Rapor.CsvSatir(null, null),
                    Rapor.OzetHtml(new Envanter(), new DanismanSonucu()), Rapor.Sayfa(null, null, 1.0) };
            }
            catch (Exception) { hata = true; }
            t.Dogru(!hata, "Null/boş girdilerde istisna yok");
            t.Dogru(c.Length > 0 && c.All(x => !string.IsNullOrEmpty(x)), "Null/boş girdilerde boş olmayan çıktı");
            t.Icermez(string.Join("", c), "bölümü üretilemedi", "Null/boş girdilerde hata kutusu yok");

            Envanter e = new Envanter();
            e.Sistem = null; e.Yuvalar = null; e.Kullanim = null; e.Platform = null;
            DanismanSonucu d = new DanismanSonucu(); d.Alinacak = null; d.Bulgular = null; d.Senaryolar = null;
            string t1 = Rapor.TamRaporHtml(e, d);
            t.Icermez(t1, "bölümü üretilemedi", "Null alanlı modelde hata kutusu yok");
            BellekYuvasi y = new BellekYuvasi { Dolu = true, BoyutMB = 4096, Yuva = null, Tur = null, ParcaNo = null, Uretici = null, UreticiHam = null, TurDetay = null };
            Envanter e2 = new Envanter(); e2.Yuvalar.Add(y); e2.Yuvalar.Add(null);
            string t2 = Rapor.TamRaporHtml(e2, new DanismanSonucu()) + Rapor.Metin(e2, new DanismanSonucu()) + Rapor.CsvSatir(e2, new DanismanSonucu());
            t.Icermez(t2, "üretilemedi", "Null alanlı yuvada hata yok");
        }

        static void RehberIcerik(TestKosucu t)
        {
            // Rehber modülü birleştirildiğinde geçer (stub ile başarısız olması beklenir).
            t.Dogru(Rehber.SatinAlmaRehberiHtml().Length > 2000, "Rehber.SatinAlmaRehberiHtml dolu (> 2000 karakter)");
            t.Dogru(Rehber.YeniBilgisayarRehberiHtml().Length > 500, "Rehber.YeniBilgisayarRehberiHtml dolu");
            t.Dogru(Rehber.SozlukHtml().Length > 500, "Rehber.SozlukHtml dolu");
            t.Dogru(Rehber.SssHtml().Length > 500, "Rehber.SssHtml dolu");
            string tam = Rapor.TamRaporHtml(LoqEnvanter(), LoqSonuc());
            string rehber = Rehber.SatinAlmaRehberiHtml();
            // Raporda rehberin kendi ilk <h1> başlığı atılır (bölüm başlığı zaten var); gövdenin geri kalanı aynen bulunur.
            string govde = System.Text.RegularExpressions.Regex.Replace(rehber, @"^\s*<h1[^>]*>.*?</h1>", "",
                System.Text.RegularExpressions.RegexOptions.Singleline);
            t.Dogru(govde.Length > 0 && tam.IndexOf(govde, StringComparison.Ordinal) >= 0, "Tam rapor Rehber içeriğini (başlık hariç) olduğu gibi içerir");
            t.Icermez(tam, "bölümü üretilemedi", "Tam raporda Rehber hatası yok");
        }
    }
}
