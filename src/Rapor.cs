// RAM Danışmanı — HTML / düz metin / CSV rapor üretimi.
// IE11 uyumlu (WinForms WebBrowser): CSS değişkeni yok, flex/grid yok, JavaScript yok.
// Donanımdan ya da kullanıcıdan gelen HER metin Html.Ka() ile kaçırılır.
// C# 5 (Windows'taki yerleşik csc.exe).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RamDanismani
{
    public static class Rapor
    {
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // =====================================================================
        // CSS ve sayfa iskeleti
        // =====================================================================

        static double OlcekDuzelt(double olcek)
        {
            if (double.IsNaN(olcek) || double.IsInfinity(olcek) || olcek < 0.5) return 1.0;
            if (olcek > 4.0) return 4.0;
            return olcek;
        }

        /// <summary>Tüm boyutlar olcek (ekran DPI / 96) ile çarpılmış px olarak üretilir.</summary>
        public static string Css(double olcek)
        {
            double o = OlcekDuzelt(olcek);
            Func<double, string> p = delegate(double n)
            {
                if (n == 0) return "0";
                int v = (int)Math.Round(n * o);
                if (v < 1) v = 1;
                return v.ToString(Inv) + "px";
            };

            StringBuilder c = new StringBuilder(8000);
            c.Append("html,body{margin:0;padding:0;}");
            c.Append("body{background:#f3f5f8;color:#1d2633;font-family:'Segoe UI',Tahoma,Arial,sans-serif;font-size:" + p(14) + ";line-height:1.45;}");
            c.Append(".sayfa{padding:" + p(18) + " " + p(22) + ";max-width:" + p(1180) + ";margin:0 auto;}");
            c.Append("h1{font-size:" + p(24) + ";font-weight:600;margin:" + p(6) + " 0 " + p(12) + ";color:#1d2633;}");
            c.Append("h2{font-size:" + p(19) + ";font-weight:600;margin:" + p(22) + " 0 " + p(10) + ";color:#1d2633;}");
            c.Append("h3{font-size:" + p(16) + ";font-weight:600;margin:" + p(2) + " 0 " + p(8) + ";color:#1d2633;}");
            c.Append("h1.bolum{font-size:" + p(22) + ";margin-top:" + p(34) + ";padding-bottom:" + p(6) + ";border-bottom:" + p(2) + " solid #1e63d6;}");
            c.Append("p{margin:" + p(6) + " 0;}");
            c.Append("a{color:#1e63d6;text-decoration:none;}a:hover{text-decoration:underline;}");

            // kartlar ve bulgu kutuları
            c.Append(".kart{background:#fff;border:1px solid #e1e6ee;border-radius:" + p(8) + ";padding:" + p(14) + " " + p(16) + ";margin:0 0 " + p(12) + ";box-shadow:0 1px " + p(3) + " rgba(20,30,50,0.07);}");
            c.Append(".iyi,.bilgi,.uyari,.kritik{padding:" + p(10) + " " + p(14) + ";margin:0 0 " + p(10) + ";border-radius:" + p(6) + ";}");
            c.Append(".iyi{border-left:" + p(5) + " solid #1f8f4e;background:#f1faf4;}");
            c.Append(".bilgi{border-left:" + p(5) + " solid #1e63d6;background:#f3f7fe;}");
            c.Append(".uyari{border-left:" + p(5) + " solid #c77700;background:#fff8ec;}");
            c.Append(".kritik{border-left:" + p(5) + " solid #c62828;background:#fdf1f1;}");
            c.Append(".bulgu-baslik{font-weight:600;margin-bottom:" + p(3) + ";}");
            c.Append(".bulgu-aciklama{color:#2c3848;}");

            // en üstteki cevap kutusu
            c.Append(".cevap{background:#f4f8ff;border:1px solid #c9dafa;border-left:" + p(6) + " solid #1e63d6;border-radius:" + p(8) + ";padding:" + p(16) + " " + p(20) + ";margin:0 0 " + p(14) + ";box-shadow:0 1px " + p(4) + " rgba(30,99,214,0.10);}");
            c.Append(".cevap-baslik{font-size:" + p(12) + ";font-weight:600;color:#4a5a73;letter-spacing:0.04em;margin-bottom:" + p(4) + ";}");
            c.Append(".cevap-cumle{font-size:" + p(20) + ";font-weight:600;line-height:1.35;color:#1d2633;margin:" + p(2) + " 0 " + p(8) + ";}");
            c.Append(".gerekce{color:#38465a;}");

            // tablolar
            c.Append("table.tablo{border-collapse:collapse;width:100%;background:#fff;margin:" + p(4) + " 0 " + p(10) + ";}");
            c.Append("table.tablo th,table.tablo td{text-align:left;vertical-align:top;padding:" + p(6) + " " + p(10) + ";border-bottom:1px solid #e6eaf0;}");
            c.Append("table.tablo th{background:#eef2f7;font-weight:600;color:#38465a;}");
            c.Append("table.tablo tr:nth-child(even) td{background:#f8fafc;}");
            c.Append("table.tablo td.anahtar{width:" + p(210) + ";color:#5b6675;}");
            c.Append("table.tablo td.sayi,table.tablo th.sayi{text-align:right;white-space:nowrap;}");

            // etiketler, notlar, ipucu, listeler, kod
            c.Append(".etiket{display:inline-block;padding:" + p(1) + " " + p(8) + ";border-radius:" + p(10) + ";font-size:" + p(12) + ";font-weight:600;line-height:1.5;background:#e6ebf2;color:#38465a;vertical-align:middle;white-space:nowrap;}");
            c.Append(".etiket-iyi{background:#dff3e7;color:#1f8f4e;}");
            c.Append(".etiket-uyari{background:#fdeccc;color:#9a5c00;}");
            c.Append(".etiket-kritik{background:#fbdcdc;color:#c62828;}");
            c.Append(".etiket-bilgi{background:#e0ebfc;color:#1e63d6;}");
            c.Append(".etiket-vurgu{background:#1e63d6;color:#fff;}");
            c.Append(".not{color:#6b7686;font-size:" + p(12) + ";}");
            c.Append(".ipucu{background:#fffbea;border:1px solid #f0e2a8;border-radius:" + p(6) + ";padding:" + p(10) + " " + p(14) + ";margin:" + p(8) + " 0 " + p(12) + ";}");
            c.Append("ul.liste,ol.adimlar{margin:" + p(6) + " 0 " + p(10) + ";padding-left:" + p(24) + ";}");
            c.Append("ul.liste li,ol.adimlar li{margin:" + p(3) + " 0;}");
            c.Append("ol.adimlar li{padding-left:" + p(4) + ";}");
            c.Append("ul.kontrol{list-style:none;margin:" + p(6) + " 0 " + p(10) + ";padding-left:" + p(4) + ";}");
            c.Append("ul.kontrol li{margin:" + p(5) + " 0;}");
            c.Append(".kutucuk{font-size:" + p(16) + ";color:#1e63d6;margin-right:" + p(6) + ";}");
            c.Append(".kod{font-family:Consolas,'Courier New',monospace;background:#eef2f7;border:1px solid #dde3ec;border-radius:" + p(4) + ";padding:" + p(1) + " " + p(6) + ";}");
            c.Append(".arama-metni{display:inline-block;font-size:" + p(20) + ";font-weight:600;padding:" + p(6) + " " + p(14) + ";margin:" + p(4) + " 0 " + p(8) + ";background:#fff;border:1px solid #9dbcf2;color:#123f8c;}");

            // rakam kutucukları
            c.Append(".rakamlar{margin:0 0 " + p(6) + ";}");
            c.Append(".rakam{display:inline-block;vertical-align:top;box-sizing:border-box;width:" + p(162) + ";min-height:" + p(98) + ";margin:0 " + p(8) + " " + p(8) + " 0;padding:" + p(10) + " " + p(12) + ";background:#fff;border:1px solid #e1e6ee;border-radius:" + p(8) + ";box-shadow:0 1px " + p(3) + " rgba(20,30,50,0.06);}");
            c.Append(".rakam-ad{font-size:" + p(12) + ";color:#5b6675;}");
            c.Append(".rakam-deger{font-size:" + p(20) + ";font-weight:600;color:#1d2633;margin:" + p(2) + " 0;}");
            c.Append(".rakam-alt{font-size:" + p(11.5) + ";color:#6b7686;line-height:1.3;}");

            // yuva haritası
            c.Append(".yuva-harita{background:#fff;border:1px solid #e1e6ee;border-radius:" + p(8) + ";padding:" + p(12) + " " + p(12) + " " + p(2) + ";margin:0 0 " + p(12) + ";}");
            c.Append(".yuva{display:inline-block;vertical-align:top;box-sizing:border-box;width:" + p(184) + ";min-height:" + p(132) + ";margin:0 " + p(10) + " " + p(10) + " 0;padding:" + p(10) + ";border-radius:" + p(8) + ";text-align:center;border:" + p(2) + " solid #c8d0db;background:#fff;}");
            c.Append(".yuva-ad{font-size:" + p(11.5) + ";color:#5b6675;word-wrap:break-word;}");
            c.Append(".yuva-boyut{font-size:" + p(22) + ";font-weight:700;margin:" + p(4) + " 0 " + p(2) + ";}");
            c.Append(".yuva-detay{font-size:" + p(12.5) + ";line-height:1.35;}");
            c.Append(".yuva-kontak{height:" + p(6) + ";margin:" + p(8) + " " + p(6) + " 0;border-radius:" + p(2) + ";background:#d4a72c;}");
            c.Append(".yuva-dolu{border-color:#1f8f4e;background:#f1faf4;}");
            c.Append(".yuva-bos{border:" + p(2) + " dashed #9aa5b4;background:#fafbfc;color:#4a5566;}");
            c.Append(".yuva-bos .yuva-boyut{color:#7d8899;font-size:" + p(18) + ";}");
            c.Append(".yuva-bos .yuva-kontak{background:#e3e7ec;}");
            c.Append(".yuva-lehimli{border-color:#8a94a3;background:#eceff3;color:#4a5566;}");
            c.Append(".yuva-lehimli .yuva-kontak{background:#9aa3ae;}");
            c.Append(".yuva-belirsiz{border:" + p(2) + " dashed #ccd3dd;background:#fbfcfd;color:#8a94a3;}");
            c.Append(".yuva-belirsiz .yuva-boyut{font-size:" + p(26) + ";color:#aab3c0;}");

            // önerilen senaryo, iki sütun, kullanım çubuğu, içindekiler
            c.Append(".onerilen{border:" + p(2) + " solid #1e63d6;box-shadow:0 0 0 " + p(3) + " rgba(30,99,214,0.13);}");
            c.Append(".iki-sutun{overflow:hidden;margin:0 0 " + p(12) + ";}");
            c.Append(".iki-sutun .sol{float:left;width:49%;}");
            c.Append(".iki-sutun .sag{float:right;width:49%;}");
            c.Append(".cubuk{height:" + p(22) + ";background:#e6eaf0;border-radius:" + p(11) + ";overflow:hidden;margin:" + p(6) + " 0 " + p(10) + ";}");
            c.Append(".cubuk-dolu{height:100%;background:#1f8f4e;}");
            c.Append(".cubuk-uyari{background:#c77700;}");
            c.Append(".cubuk-kritik{background:#c62828;}");
            c.Append(".icindekiler ol{margin:" + p(4) + " 0;padding-left:" + p(24) + ";}");
            c.Append(".icindekiler li{margin:" + p(2) + " 0;}");
            c.Append(".rapor-baslik h1{font-size:" + p(28) + ";margin-bottom:" + p(2) + ";}");

            // yazdırma
            c.Append("@media print{");
            c.Append("body{background:#fff;font-size:10.5pt;}");
            c.Append(".sayfa{max-width:none;padding:0;}");
            c.Append(".kart,.rakam,.yuva-harita,.cevap,.onerilen{box-shadow:none;}");
            c.Append(".kart,.yuva,.rakam,.cevap,table.tablo tr{page-break-inside:avoid;}");
            c.Append("h1,h2,h3{page-break-after:avoid;}");
            c.Append("h1.bolum{page-break-before:always;}");
            c.Append("a{color:#1d2633;}");
            c.Append(".icindekiler{display:none;}");
            c.Append("}");
            return c.ToString();
        }

        /// <summary>Tam HTML belgesi (IE11 kip, UTF-8).</summary>
        public static string Sayfa(string baslik, string govdeHtml, double olcek)
        {
            StringBuilder sb = new StringBuilder((govdeHtml ?? "").Length + 10000);
            sb.Append("<!DOCTYPE html>\n<html lang=\"tr\">\n<head>\n");
            sb.Append("<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">\n");
            sb.Append("<meta charset=\"utf-8\">\n");
            sb.Append("<title>").Append(Html.Ka(baslik ?? "RAM Danışmanı")).Append("</title>\n");
            sb.Append("<style>\n").Append(Css(olcek)).Append("\n</style>\n");
            sb.Append("</head>\n<body>\n<div class=\"sayfa\">\n");
            sb.Append(govdeHtml ?? "");
            sb.Append("\n</div>\n</body>\n</html>\n");
            return sb.ToString();
        }

        // =====================================================================
        // Özet
        // =====================================================================

        public static string OzetHtml(Envanter e, DanismanSonucu d)
        {
            try { return OzetHtmlIc(Duzelt(e), Duzelt(d)); }
            catch (Exception ex) { return HataKutusu("Özet", ex); }
        }

        static string OzetHtmlIc(Envanter e, DanismanSonucu d)
        {
            StringBuilder sb = new StringBuilder(8000);

            // (1) cevap kutusu
            sb.Append("<div class=\"cevap\">");
            sb.Append("<div class=\"cevap-baslik\">BU BİLGİSAYARIN RAM'İ YÜKSELTİLEBİLİR Mİ? ");
            sb.Append(DurumEtiketi(d.Durum)).Append("</div>");
            string cumle = S(d.OzetCumle);
            if (cumle.Length == 0) cumle = "Değerlendirme yapılamadı; ayrıntılar aşağıda.";
            sb.Append("<div class=\"cevap-cumle\">").Append(Html.Ka(cumle)).Append("</div>");
            if (d.Alinacak != null && d.Alinacak.Gecerli && S(d.Alinacak.AramaMetni).Length > 0)
                sb.Append("<div>Mağazada aranacak: <span class=\"kod\">").Append(Html.Ka(d.Alinacak.AramaMetni)).Append("</span> <span class=\"not\">(ayrıntılar “Yükseltme ve Satın Alma” bölümünde)</span></div>");
            sb.Append("</div>");

            // (2) rakam kutucukları
            List<BellekYuvasi> dolu = DoluYuvalar(e);
            sb.Append("<div class=\"rakamlar\">");

            long takili = e.Sistem.TakiliRamMB > 0 ? e.Sistem.TakiliRamMB : ToplamTakili(e);
            string ramAlt = e.Sistem.KullanilabilirRamMB > 0 ? "Windows'un kullanabildiği: " + Bicim.Boyut(e.Sistem.KullanilabilirRamMB) : "takılı bellek";
            sb.Append(Rakam("Toplam RAM", Html.Ka(Bicim.Boyut(takili)), Html.Ka(ramAlt)));

            int toplamYuva = ToplamYuva(e, d);
            string yuvaDeger = toplamYuva > 0 ? dolu.Count + " / " + toplamYuva : (e.Yuvalar.Count > 0 ? dolu.Count.ToString(Inv) : "—");
            string yuvaAlt;
            if (!d.YuvaBilgisiGuvenilir)
            {
                yuvaDeger += " <span class=\"etiket etiket-uyari\" title=\"BIOS bilgisi güvenilir değil\">?</span>";
                yuvaAlt = "BIOS'un bildirdiği yuva bilgisi güvenilir değil — üretici belgesinden doğrulayın";
            }
            else
            {
                yuvaAlt = "dolu / toplam" + (d.BosYuva > 0 ? " — " + d.BosYuva + " boş yuva" : "");
            }
            sb.Append(Rakam("Yuva", yuvaDeger, Html.Ka(yuvaAlt)));

            string tur = Birlestir(dolu.Select(y => S(y.Tur)));
            string form = Birlestir(dolu.Select(y => S(y.FormFaktor)));
            sb.Append(Rakam("Tür ve form", Html.Ka(Bos(tur, "—")), Html.Ka(form.Length > 0 ? form + FormAciklamasi(form) : "form faktörü bilinmiyor")));

            sb.Append(Rakam("Hız", Html.Ka(HizOzeti(dolu, false)), Html.Ka(HizOzeti(dolu, true))));

            // Pratik sınır (yuva x güvenli modül) biliniyorsa ana rakam odur; işlemcinin
            // teorik sınırı (ör. 128 GB) 2 yuvalı bir dizüstünde yanıltıcı olur, alt satıra iner.
            string azami, azamiAlt;
            if (d.PratikAzamiGB > 0)
            {
                azami = d.PratikAzamiGB + " GB";
                azamiAlt = "pratikte: " + S(d.PratikAzamiMetni) +
                    (d.AzamiGB > 0 ? " · " + Bos(S(d.AzamiGBKaynagi), "üst sınır") + ": " + d.AzamiGB + " GB" : "");
            }
            else
            {
                azami = d.AzamiGB > 0 ? d.AzamiGB + " GB" : "Bilinmiyor";
                azamiAlt = Bos(S(d.AzamiGBKaynagi), "kaynak yok");
            }
            sb.Append(Rakam("Azami kapasite", Html.Ka(azami), Html.Ka(azamiAlt)));

            string kanal = S(d.KanalDurumu);
            sb.Append(Rakam("Kanal", "<span style=\"font-size:85%\">" + Html.Ka(Bos(kanal, "—")) + "</span>",
                Html.Ka(e.Platform != null && e.Platform.KanalSayisi > 0 ? "İşlemci: " + e.Platform.KanalSayisi + " kanal" : "çift kanal = 2 eş modül")));
            sb.Append("</div>");

            // (3) yuva haritası
            sb.Append("<h2>Yuva haritası</h2>");
            sb.Append(YuvaHaritasi(e, d));

            // (4) sistem kartı
            sb.Append("<h2>Bilgisayar</h2>");
            sb.Append(SistemKarti(e));

            // (5) bulgular
            sb.Append("<h2>Bulgular</h2>");
            if (d.Bulgular.Count == 0) sb.Append("<p class=\"not\">Kayda değer bulgu yok.</p>");
            else foreach (Bulgu b in SiraliBulgular(d.Bulgular)) sb.Append(BulguHtml(b));

            return sb.ToString();
        }

        static string Rakam(string ad, string degerHtml, string altHtml)
        {
            return "<div class=\"rakam\"><div class=\"rakam-ad\">" + Html.Ka(ad) + "</div><div class=\"rakam-deger\">" + degerHtml +
                   "</div><div class=\"rakam-alt\">" + altHtml + "</div></div>";
        }

        static string YuvaHaritasi(Envanter e, DanismanSonucu d)
        {
            StringBuilder sb = new StringBuilder(3000);
            sb.Append("<div class=\"yuva-harita\">");
            string takilabilir = BosYuvayaTakilabilir(e, d);
            int sira = 0;
            foreach (BellekYuvasi y in e.Yuvalar)
            {
                if (y == null) continue;
                sira++;
                string ad = S(y.Yuva).Length > 0 ? y.Yuva : (sira + ". yuva");
                if (y.Lehimli)
                {
                    sb.Append("<div class=\"yuva yuva-lehimli\">");
                    sb.Append("<div class=\"yuva-ad\">").Append(Html.Ka(ad)).Append("</div>");
                    sb.Append("<div class=\"yuva-boyut\">").Append(y.Dolu ? Html.Ka(Bicim.Boyut(y.BoyutMB)) : "—").Append("</div>");
                    sb.Append("<div class=\"yuva-detay\"><b>Lehimli</b><br>").Append(Html.Ka(Birlestir(new string[] { S(y.Tur), Hiz(y) }, " "))).Append("<br>sökülemez</div>");
                    sb.Append("<div class=\"yuva-kontak\"></div></div>");
                }
                else if (y.Dolu)
                {
                    sb.Append("<div class=\"yuva yuva-dolu\">");
                    sb.Append("<div class=\"yuva-ad\">").Append(Html.Ka(ad)).Append("</div>");
                    sb.Append("<div class=\"yuva-boyut\">").Append(Html.Ka(Bicim.Boyut(y.BoyutMB))).Append("</div>");
                    sb.Append("<div class=\"yuva-detay\">").Append(Html.Ka(Birlestir(new string[] { S(y.Tur), S(y.FormFaktor) }, " "))).Append("<br>");
                    sb.Append(Html.Ka(Hiz(y))).Append("<br>");
                    sb.Append(Html.Ka(Bos(S(y.Uretici), Bos(S(y.UreticiHam), "marka bilinmiyor")))).Append("</div>");
                    sb.Append("<div class=\"yuva-kontak\"></div></div>");
                }
                else
                {
                    sb.Append("<div class=\"yuva yuva-bos\">");
                    sb.Append("<div class=\"yuva-ad\">").Append(Html.Ka(ad)).Append("</div>");
                    sb.Append("<div class=\"yuva-boyut\">+</div>");
                    sb.Append("<div class=\"yuva-detay\">BOŞ — buraya ").Append(Html.Ka(takilabilir)).Append(" takılabilir</div>");
                    sb.Append("<div class=\"yuva-kontak\"></div></div>");
                }
            }

            if (d.SupheliEkYuva > 0)
            {
                int ek = Math.Min(d.SupheliEkYuva, 6);
                for (int i = 0; i < ek; i++)
                {
                    int no = e.Yuvalar.Count + i + 1;
                    sb.Append("<div class=\"yuva yuva-belirsiz\">");
                    sb.Append("<div class=\"yuva-ad\">BIOS listesinde yok</div>");
                    sb.Append("<div class=\"yuva-boyut\">?</div>");
                    sb.Append("<div class=\"yuva-detay\">Muhtemel ").Append(no).Append(". yuva (BIOS göstermiyor)<br>Varsa buraya ")
                      .Append(Html.Ka(takilabilir)).Append(" takılabilir</div>");
                    sb.Append("</div>");
                }
            }

            if (e.Yuvalar.Count == 0)
                sb.Append("<p class=\"not\">Bellek yuvası bilgisi okunamadı.</p>");
            sb.Append("</div>");

            if (d.SupheliEkYuva > 0)
                sb.Append("<p class=\"not\">Kesikli soluk kutular BIOS'un listelemediği ama bu tür cihazlarda genellikle bulunan yuvaları gösterir. BIOS bazen sadece dolu yuvaları ve yanlış bir azami kapasite bildirir; kesin bilgi için cihazın üretici belgesine (donanım bakım kılavuzu / PSREF) bakın.</p>");
            return sb.ToString();
        }

        static string BosYuvayaTakilabilir(Envanter e, DanismanSonucu d)
        {
            SatinAlmaOzeti a = d.Alinacak;
            if (a != null && a.Gecerli && a.ModulGB > 0)
                return Birlestir(new string[] { a.ModulGB + " GB", S(a.Tur), S(a.FormFaktor) }, " ");
            List<BellekYuvasi> dolu = DoluYuvalar(e);
            if (dolu.Count > 0)
            {
                string s = Birlestir(new string[] { S(dolu[0].Tur), S(dolu[0].FormFaktor) }, " ");
                if (s.Length > 0) return "aynı tür " + s;
            }
            return "uygun bir modül";
        }

        static string SistemKarti(Envanter e)
        {
            SistemBilgisi s = e.Sistem;
            StringBuilder sb = new StringBuilder(2000);
            sb.Append("<div class=\"kart\"><table class=\"tablo\">");
            Satir(sb, "Üretici", s.Uretici);
            Satir(sb, "Model adı", s.ModelAdi);
            Satir(sb, "Model (makine türü)", s.Model);
            Satir(sb, "Seri no", s.SeriNo);
            string cpu = S(s.IslemciAdi);
            if (s.CekirdekSayisi > 0) cpu += " (" + s.CekirdekSayisi + " çekirdek" + (s.IsParcacigiSayisi > 0 ? ", " + s.IsParcacigiSayisi + " iş parçacığı" : "") + ")";
            Satir(sb, "İşlemci", cpu);
            string os = S(s.IsletimSistemi);
            if (os.Length > 0) os += s.Isletim64Bit ? " (64 bit)" : " (32 bit)";
            Satir(sb, "İşletim sistemi", os);
            Satir(sb, "BIOS", Birlestir(new string[] { S(s.BiosSurum), S(s.BiosTarih) }, " — "));
            Satir(sb, "Anakart", Birlestir(new string[] { S(s.AnakartUretici), S(s.AnakartModel) }, " "));
            Satir(sb, "Kasa türü", KasaAdi(s.Kasa) + (s.KasaTuruKodu > 0 ? " (SMBIOS kodu " + Hex(s.KasaTuruKodu) + ")" : ""));
            if (s.SanalMakine) Satir(sb, "Sanal makine", "Evet" + (S(s.SanalMakineNotu).Length > 0 ? " — " + s.SanalMakineNotu : ""));
            Satir(sb, "SMBIOS sürümü", s.SmbiosSurum);
            sb.Append("</table></div>");
            return sb.ToString();
        }

        // =====================================================================
        // Modüller
        // =====================================================================

        public static string ModullerHtml(Envanter e)
        {
            try { return ModullerHtmlIc(Duzelt(e)); }
            catch (Exception ex) { return HataKutusu("Modüller", ex); }
        }

        static string ModullerHtmlIc(Envanter e)
        {
            StringBuilder sb = new StringBuilder(12000);
            int doluSayi = DoluYuvalar(e).Count;
            sb.Append("<h2>Bellek yuvaları (").Append(e.Yuvalar.Count).Append(" yuva, ").Append(doluSayi).Append(" dolu)</h2>");
            if (e.Yuvalar.Count == 0)
                sb.Append("<div class=\"kart uyari\"><div class=\"bulgu-baslik\">Yuva bilgisi okunamadı</div><div class=\"bulgu-aciklama\">SMBIOS ve WMI'dan bellek modülü bilgisi alınamadı. Toplama notlarına bakın.</div></div>");

            int sira = 0;
            foreach (BellekYuvasi y in e.Yuvalar)
            {
                if (y == null) continue;
                sira++;
                string ad = S(y.Yuva).Length > 0 ? y.Yuva : (sira + ". yuva");
                string durumEtiket = y.Lehimli ? "<span class=\"etiket\">LEHİMLİ</span>"
                    : (y.Dolu ? "<span class=\"etiket etiket-iyi\">DOLU</span>" : "<span class=\"etiket etiket-uyari\">BOŞ</span>");
                sb.Append("<div class=\"kart\">");
                sb.Append("<h3>").Append(Html.Ka(ad)).Append(" ").Append(durumEtiket);
                if (y.Dolu) sb.Append(" <span class=\"not\">").Append(Html.Ka(Birlestir(new string[] { Bicim.Boyut(y.BoyutMB), S(y.Tur), S(y.FormFaktor), S(y.Uretici) }, " · "))).Append("</span>");
                sb.Append("</h3>");
                sb.Append("<table class=\"tablo\">");
                Satir(sb, "Yuva", y.Yuva);
                Satir(sb, "Banka", y.Banka);
                Satir(sb, "Durum", y.Lehimli ? (y.Dolu ? "Dolu (lehimli)" : "Lehimli") : (y.Dolu ? "Dolu" : "Boş"));
                if (y.Dolu)
                {
                    Satir(sb, "Boyut", Bicim.Boyut(y.BoyutMB));
                    Satir(sb, "Tür", y.Tur);
                    Satir(sb, "Form faktörü", Bos(S(y.FormFaktor), "") + (S(y.FormFaktor).Length > 0 ? FormAciklamasi(y.FormFaktor) : ""));
                    Satir(sb, "Etiket hızı", Bicim.Hiz(y.HizMTs));
                    string calisan = Bicim.Hiz(y.YapilandirilmisHizMTs);
                    if (y.HizMTs > 0 && y.YapilandirilmisHizMTs > 0 && y.YapilandirilmisHizMTs < y.HizMTs)
                    {
                        int islemciAzami = 0;
                        try { islemciAzami = Platform.AzamiHiz(e.Platform, S(y.Tur)); } catch { islemciAzami = 0; }
                        if (islemciAzami > 0 && y.YapilandirilmisHizMTs >= islemciAzami)
                            calisan += " (etiketten düşük çünkü işlemcinin resmi azamisi " + islemciAzami + " MT/s — normal)";
                        else
                            calisan += " (etiketten düşük — işlemci/anakart sınırı ya da karışık modüller; normal olabilir)";
                    }
                    Satir(sb, "Çalışan hız", calisan);
                    string ur = S(y.Uretici);
                    string ham = S(y.UreticiHam);
                    if (ham.Length > 0 && !string.Equals(ham, ur, StringComparison.OrdinalIgnoreCase))
                        ur = Bos(ur, "?") + " (ham: " + ham + ")";
                    if (y.ModulUreticiId != 0) ur += " [JEDEC " + Hex(y.ModulUreticiId) + "]";
                    Satir(sb, "Üretici", ur);
                    string pn = S(y.ParcaNo).Trim();
                    if (y.ParcaBilgisi != null && S(y.ParcaBilgisi.Aciklama).Length > 0)
                        pn = Bos(pn, "—") + "\nÇözüm: " + y.ParcaBilgisi.Aciklama;
                    SatirKod(sb, "Parça no", pn);
                    Satir(sb, "Seri no", y.SeriNo);
                    if (S(y.VarlikEtiketi).Length > 0) Satir(sb, "Varlık etiketi", y.VarlikEtiketi);
                    Satir(sb, "Rank", y.Rank > 0 ? y.Rank + (y.Rank == 1 ? " (single rank)" : y.Rank == 2 ? " (dual rank)" : " rank") : "—");
                    string gen = (y.VeriGenisligi > 0 ? y.VeriGenisligi.ToString(Inv) : "?") + " / " + (y.ToplamGenislik > 0 ? y.ToplamGenislik.ToString(Inv) : "?") + " bit";
                    if (y.VeriGenisligi > 0 && y.ToplamGenislik > 0) gen += y.Ecc ? " — ECC VAR" : " — ECC yok";
                    Satir(sb, "Veri / toplam genişlik", gen);
                    Satir(sb, "Voltaj (min / maks / yapılandırılmış)", Bicim.Voltaj(y.MinVoltajMv) + " / " + Bicim.Voltaj(y.MaxVoltajMv) + " / " + Bicim.Voltaj(y.YapilandirilmisVoltajMv));
                    Satir(sb, "Tür detayları", Birlestir(y.TurDetay ?? new List<string>(), ", ") + (y.TurDetay != null && y.Kayitli ? " — Registered (sunucu tipi)" : ""));
                    Satir(sb, "Teknoloji", S(y.Teknoloji) + (y.TeknolojiKodu > 0 ? " (kod " + y.TeknolojiKodu + ")" : ""));
                }
                else
                {
                    if (S(y.FormFaktor).Length > 0) Satir(sb, "Form faktörü", y.FormFaktor);
                    if (S(y.Tur).Length > 0) Satir(sb, "Tür (BIOS'a göre)", y.Tur);
                }
                Satir(sb, "Lehimli", y.Lehimli ? "Evet" + (S(y.LehimliNedeni).Length > 0 ? " — " + y.LehimliNedeni : "") : "Hayır" + (S(y.LehimliNedeni).Length > 0 ? " — " + y.LehimliNedeni : ""));
                Satir(sb, "Kaynak", y.Kaynak);
                SatirKod(sb, "Ham kodlar", "tür " + Hex(y.TurKodu) + ", form " + Hex(y.FormFaktorKodu) + ", tür detay " + Hex(y.TurDetayBayraklari) +
                    ", handle " + Hex(y.Handle) + ", dizi " + Hex(y.DiziHandle));
                sb.Append("</table></div>");
            }

            // diziler
            sb.Append("<h2>Bellek dizileri (SMBIOS tip 16)</h2>");
            if (e.Diziler.Count == 0) sb.Append("<p class=\"not\">Bellek dizisi bilgisi yok.</p>");
            else
            {
                sb.Append("<div class=\"kart\"><table class=\"tablo\"><tr><th>Handle</th><th>Konum</th><th>Kullanım</th><th>ECC</th><th class=\"sayi\">Azami kapasite</th><th class=\"sayi\">Yuva sayısı</th></tr>");
                foreach (BellekDizisi z in e.Diziler)
                {
                    if (z == null) continue;
                    sb.Append("<tr><td>").Append(Hex(z.Handle)).Append("</td><td>").Append(Html.Ka(Bos(S(z.Konum), "—")))
                      .Append("</td><td>").Append(Html.Ka(Bos(S(z.Kullanim), "—"))).Append("</td><td>").Append(Html.Ka(Bos(S(z.EccTuru), "—")))
                      .Append("</td><td class=\"sayi\">").Append(Html.Ka(Bicim.Boyut(z.MaxKapasiteMB))).Append("</td><td class=\"sayi\">")
                      .Append(z.YuvaSayisi > 0 ? z.YuvaSayisi.ToString(Inv) : "—").Append("</td></tr>");
                }
                sb.Append("</table><p class=\"not\">Bu değerler BIOS'un bildirdiğidir ve bazı cihazlarda (özellikle dizüstülerde) eksik ya da yanlış olabilir: yuva sayısı yalnızca dolu yuvaları, azami kapasite ise fabrika çıkışındaki miktarı gösterebilir.</p></div>");
            }

            // platform
            sb.Append("<h2>İşlemcinin bellek desteği</h2>");
            sb.Append(PlatformKarti(e));

            // toplama notları
            sb.Append("<h2>Toplama notları</h2>");
            sb.Append("<div class=\"kart\"><p>SMBIOS tablosu: ").Append(e.SmbiosOkundu ? "<span class=\"etiket etiket-iyi\">okundu</span>" : "<span class=\"etiket etiket-uyari\">okunamadı</span>");
            if (e.Zaman != default(DateTime)) sb.Append(" <span class=\"not\">Toplama zamanı: ").Append(e.Zaman.ToString("yyyy-MM-dd HH:mm:ss", Inv)).Append("</span>");
            sb.Append("</p>");
            if (e.ToplamaNotlari.Count == 0) sb.Append("<p class=\"not\">Not yok.</p>");
            else
            {
                sb.Append("<ul class=\"liste\">");
                foreach (string n in e.ToplamaNotlari) sb.Append("<li>").Append(Html.Ka(n)).Append("</li>");
                sb.Append("</ul>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        static string PlatformKarti(Envanter e)
        {
            PlatformBilgisi p = e.Platform ?? new PlatformBilgisi();
            StringBuilder sb = new StringBuilder(1500);
            sb.Append("<div class=\"kart\">");
            if (!p.Bulundu)
            {
                sb.Append("<p>İşlemci (").Append(Html.Ka(Bos(S(e.Sistem.IslemciAdi), "bilinmiyor"))).Append(") bellek denetleyicisi tablosunda bulunamadı. Desteklenen bellek türü ve azami kapasite için işlemci üreticisinin sayfasına bakın.</p>");
                if (S(p.Not).Length > 0) sb.Append("<p class=\"not\">").Append(Html.Ka(p.Not)).Append("</p>");
                sb.Append("</div>");
                return sb.ToString();
            }
            sb.Append("<table class=\"tablo\">");
            Satir(sb, "İşlemci", e.Sistem.IslemciAdi);
            Satir(sb, "Aile", p.Aile);
            Satir(sb, "Desteklenen türler ve resmi azami hız", PlatformHizlari(p));
            Satir(sb, "Azami kapasite (işlemci)", p.MaxKapasiteGB > 0 ? p.MaxKapasiteGB + " GB" : "—");
            Satir(sb, "Bellek kanalı", p.KanalSayisi > 0 ? p.KanalSayisi.ToString(Inv) : "—");
            Satir(sb, "ECC desteği", p.EccDestekli ? "Var" : "Yok / bilinmiyor");
            if (S(p.Not).Length > 0) Satir(sb, "Not", p.Not);
            Satir(sb, "Kesinlik", p.Kesinlik);
            sb.Append("</table><p class=\"not\">Cihazın kendi sınırı (anakart / BIOS) işlemcininkinden düşük olabilir.</p></div>");
            return sb.ToString();
        }

        static string PlatformHizlari(PlatformBilgisi p)
        {
            List<string> parcalar = new List<string>();
            List<string> turler = new List<string>(p.DesteklenenTurler ?? new List<string>());
            if (p.MaxHizMTs != null)
                foreach (string k in p.MaxHizMTs.Keys)
                    if (!turler.Contains(k)) turler.Add(k);
            foreach (string t in turler)
            {
                int h;
                if (p.MaxHizMTs != null && p.MaxHizMTs.TryGetValue(t, out h) && h > 0) parcalar.Add(t + "-" + h);
                else parcalar.Add(S(t));
            }
            return Bos(Birlestir(parcalar, ", "), "—");
        }

        // =====================================================================
        // Yükseltme ve satın alma
        // =====================================================================

        public static string YukseltmeHtml(Envanter e, DanismanSonucu d)
        {
            try { return YukseltmeHtmlIc(Duzelt(e), Duzelt(d)); }
            catch (Exception ex) { return HataKutusu("Yükseltme", ex); }
        }

        static string YukseltmeHtmlIc(Envanter e, DanismanSonucu d)
        {
            StringBuilder sb = new StringBuilder(10000);
            SatinAlmaOzeti a = d.Alinacak ?? new SatinAlmaOzeti();

            sb.Append("<div class=\"cevap\"><div class=\"cevap-baslik\">NE ALMALIYIM? ").Append(DurumEtiketi(d.Durum)).Append("</div>");
            if (a.Gecerli)
            {
                sb.Append("<div class=\"cevap-cumle\">").Append(Html.Ka(AlinacakBasligi(a))).Append("</div>");
                if (S(a.AramaMetni).Length > 0)
                {
                    sb.Append("<div>Mağazada / satın alma talebinde şunu aratın:</div>");
                    sb.Append("<div class=\"kod arama-metni\">").Append(Html.Ka(a.AramaMetni)).Append("</div>");
                }
                List<string> ozellikler = (a.Ozellikler != null && a.Ozellikler.Count > 0) ? a.Ozellikler : VarsayilanOzellikler(a);
                sb.Append("<ul class=\"liste\">");
                foreach (string o in ozellikler) sb.Append("<li>").Append(Html.Ka(o)).Append("</li>");
                sb.Append("</ul>");
                if (S(a.OnerilenParcaNo).Length > 0)
                    sb.Append("<p>Birebir eşi (mevcut modülün parça numarası): <span class=\"kod\">").Append(Html.Ka(a.OnerilenParcaNo)).Append("</span> <span class=\"not\">— bulunursa en sorunsuz seçenek; bulunamazsa aynı özellikte başka marka da olur.</span></p>");
                if (S(a.Gerekce).Length > 0) sb.Append("<p class=\"gerekce\">").Append(Html.Ka(a.Gerekce)).Append("</p>");
                string uyarilar = Birlestir(KarisiklikUyarilari(a), "; ");
                if (uyarilar.Length > 0) sb.Append("<p><span class=\"etiket etiket-uyari\">DİKKAT</span> ").Append(Html.Ka(uyarilar)).Append("</p>");
            }
            else
            {
                string baslik;
                switch (d.Durum)
                {
                    case YukseltmeDurumu.Hayir: baslik = "Bu bilgisayarın RAM'i yükseltilemiyor; RAM satın almayın."; break;
                    case YukseltmeDurumu.Bilinmiyor: baslik = "Ne alınacağı kesin belirlenemedi."; break;
                    default: baslik = "Şu an için bir RAM satın alma önerisi yok."; break;
                }
                sb.Append("<div class=\"cevap-cumle\">").Append(Html.Ka(baslik)).Append("</div>");
                string neden = Bos(S(a.Gerekce), S(d.OzetCumle));
                if (neden.Length > 0) sb.Append("<p class=\"gerekce\">").Append(Html.Ka(neden)).Append("</p>");
                sb.Append("<p class=\"not\">Cihazın üretici destek sayfasında model adıyla (").Append(Html.Ka(Bos(Birlestir(new string[] { S(e.Sistem.Uretici), S(e.Sistem.ModelAdi), S(e.Sistem.Model) }, " "), "model bilinmiyor"))).Append(") bellek özelliklerini kontrol edin.</p>");
            }
            sb.Append("</div>");

            // senaryolar
            sb.Append("<h2>Yükseltme seçenekleri</h2>");
            if (d.Senaryolar.Count == 0) sb.Append("<p class=\"not\">Bu cihaz için yükseltme senaryosu üretilmedi.</p>");
            foreach (YukseltmeSenaryosu s in SiraliSenaryolar(d.Senaryolar))
            {
                sb.Append(s.Tavsiye ? "<div class=\"kart onerilen\">" : "<div class=\"kart\">");
                sb.Append("<h3>").Append(Html.Ka(Bos(S(s.Ad), "Senaryo")));
                if (s.Tavsiye) sb.Append(" <span class=\"etiket etiket-vurgu\">ÖNERİLEN</span>");
                sb.Append("</h3>");
                if (S(s.Aciklama).Length > 0) sb.Append("<p>").Append(Html.Ka(s.Aciklama)).Append("</p>");
                sb.Append("<table class=\"tablo\">");
                List<string> al = new List<string>();
                if (s.Alinacaklar != null) foreach (AlinacakModul m in s.Alinacaklar) if (m != null) al.Add(ModulMetni(m));
                Satir(sb, "Alınacaklar", al.Count > 0 ? string.Join("\n", al.ToArray()) : "Hiçbir şey");
                Satir(sb, "Çıkarılacaklar", s.Cikarilacaklar != null && s.Cikarilacaklar.Count > 0 ? string.Join(", ", s.Cikarilacaklar.ToArray()) : "Yok (mevcut modüller kalır)");
                Satir(sb, "Sonuç toplam", s.SonucToplamGB > 0 ? s.SonucToplamGB + " GB" : "—");
                sb.Append("<tr><td class=\"anahtar\">Çift kanal</td><td>").Append(s.Sunucu ? "<span class=\"etiket etiket-bilgi\">Kılavuza göre</span>" : s.CiftKanal ? "<span class=\"etiket etiket-iyi\">Evet</span>" : (FlexSenaryo(s) ? "<span class=\"etiket etiket-uyari\">Kısmen (flex mode)</span>" : "<span class=\"etiket etiket-uyari\">Hayır</span>")).Append("</td></tr>");
                sb.Append("</table>");
                if (s.Notlar != null && s.Notlar.Count > 0)
                {
                    sb.Append("<ul class=\"liste\">");
                    foreach (string n in s.Notlar) sb.Append("<li>").Append(Html.Ka(n)).Append("</li>");
                    sb.Append("</ul>");
                }
                sb.Append("</div>");
            }

            // kontrol listesi
            sb.Append("<h2>Satın almadan önce kontrol listesi</h2>");
            if (d.KontrolListesi.Count == 0) sb.Append("<p class=\"not\">Kontrol listesi yok.</p>");
            else
            {
                sb.Append("<div class=\"kart\"><ul class=\"kontrol\">");
                foreach (string k in d.KontrolListesi) sb.Append("<li><span class=\"kutucuk\">☐</span>").Append(Html.Ka(k)).Append("</li>");
                sb.Append("</ul></div>");
            }

            // takma adımları
            sb.Append("<h2>Takma adımları</h2>");
            if (d.TakmaAdimlari.Count == 0) sb.Append("<p class=\"not\">Bu cihaza özel takma adımı yok.</p>");
            else
            {
                sb.Append("<div class=\"kart\"><ol class=\"adimlar\">");
                foreach (string k in d.TakmaAdimlari) sb.Append("<li>").Append(Html.Ka(k)).Append("</li>");
                sb.Append("</ol></div>");
            }

            // sık yapılan hatalar
            sb.Append("<h2>Sık yapılan hatalar</h2>");
            sb.Append("<div class=\"ipucu\"><ul class=\"liste\">");
            sb.Append("<li><b>Masaüstü belleğini (DIMM) dizüstüne almak</b> — dizüstüler kısa SODIMM kullanır; uzun DIMM takılmaz.</li>");
            sb.Append("<li><b>DDR4 ile DDR5'i karıştırmak</b> — çentik yerleri farklıdır, biri ötekinin yuvasına girmez.</li>");
            sb.Append("<li><b>Tek büyük modülle çift kanalı kaybetmek</b> — 2 × 8 GB çoğu zaman 1 × 16 GB'tan hızlıdır; yuvaları eş modüllerle doldurun.</li>");
            sb.Append("<li><b>Sunucu belleği (RDIMM / Registered ECC) almak</b> — normal masaüstü ve dizüstülerde çalışmaz.</li>");
            sb.Append("<li><b>Daha hızlı modülün sistemi hızlandıracağını sanmak</b> — bellek, işlemcinin/anakartın sınırında ve en yavaş modülün hızında çalışır.</li>");
            sb.Append("<li><b>Farklı marka/zamanlamadaki modülleri karıştırmak</b> — çoğu zaman çalışır ama sorun çıkarsa ilk şüpheli budur; mümkünse aynı model ya da hazır kit alın.</li>");
            sb.Append("<li><b>Fişi/bataryayı ayırmadan takmak</b> — önce kapatın, fişi çekin, dizüstünde iç bataryayı ayırın (BIOS'ta “battery disconnect” seçeneği olabilir), statik elektriği boşaltın.</li>");
            sb.Append("</ul></div>");

            // bağlantılar
            sb.Append("<h2>Bağlantılar</h2>");
            if (d.Baglantilar.Count == 0) sb.Append("<p class=\"not\">Bağlantı yok.</p>");
            else
            {
                sb.Append("<div class=\"kart\"><ul class=\"liste\">");
                foreach (Baglanti b in d.Baglantilar) if (b != null) sb.Append("<li>").Append(BaglantiHtml(b)).Append("</li>");
                sb.Append("</ul><p class=\"not\">Bağlantılar varsayılan tarayıcıda açılır.</p></div>");
            }
            return sb.ToString();
        }

        static string AlinacakBasligi(SatinAlmaOzeti a)
        {
            string adet = a.Adet > 0 ? a.Adet + " adet " : "";
            string s = adet + Birlestir(new string[] { a.ModulGB > 0 ? a.ModulGB + " GB" : "", S(a.Tur), S(a.FormFaktor) }, " ");
            if (a.HizMTs > 0) s += ", " + a.HizMTs + " MT/s";
            return s.Trim();
        }

        static List<string> VarsayilanOzellikler(SatinAlmaOzeti a)
        {
            List<string> l = new List<string>();
            if (S(a.Tur).Length > 0) l.Add("Tür: " + a.Tur);
            if (S(a.FormFaktor).Length > 0) l.Add("Form: " + a.FormFaktor + FormAciklamasi(a.FormFaktor));
            if (a.ModulGB > 0) l.Add("Modül başına kapasite: " + a.ModulGB + " GB");
            if (a.HizMTs > 0) l.Add("Hız: " + a.HizMTs + " MT/s (daha yüksek olan da çalışır, bu hızda çalışır)");
            if (a.VoltajMv > 0) l.Add("Voltaj: " + Bicim.Voltaj(a.VoltajMv));
            l.Add(a.Ecc ? "ECC: Var" : "ECC: Yok (non-ECC)");
            l.Add(a.Kayitli ? "Registered (RDIMM)" : "Unbuffered (normal, UDIMM/SODIMM)");
            return l;
        }

        static List<string> KarisiklikUyarilari(SatinAlmaOzeti a)
        {
            List<string> l = new List<string>();
            string tur = S(a.Tur).ToUpperInvariant();
            string form = S(a.FormFaktor).ToUpperInvariant();
            if (tur.StartsWith("LPDDR", StringComparison.Ordinal)) l.Add(a.Tur + " genellikle lehimlidir, ayrı modül olarak satılmaz");
            else if (tur.StartsWith("DDR5", StringComparison.Ordinal)) l.Add("DDR4 değil DDR5");
            else if (tur.StartsWith("DDR4", StringComparison.Ordinal)) l.Add("DDR5 değil DDR4");
            else if (tur.StartsWith("DDR3", StringComparison.Ordinal)) l.Add("DDR4 değil DDR3" + (a.VoltajMv > 0 && a.VoltajMv <= 1350 ? " (DDR3L, 1,35 V)" : ""));
            if (form.IndexOf("SODIMM", StringComparison.Ordinal) >= 0 || form.IndexOf("SO-DIMM", StringComparison.Ordinal) >= 0)
                l.Add("masaüstü (DIMM) değil dizüstü tipi kısa modül (SODIMM)");
            else if (form == "DIMM" || form == "UDIMM")
                l.Add("dizüstü (SODIMM) değil masaüstü tipi uzun modül (DIMM)");
            if (!a.Kayitli && (form == "DIMM" || form == "UDIMM")) l.Add("sunucu tipi (RDIMM / Registered) değil");
            return l;
        }

        static string ModulMetni(AlinacakModul m)
        {
            string s = (m.Adet > 0 ? m.Adet + " × " : "") + Birlestir(new string[] { m.ModulGB > 0 ? m.ModulGB + " GB" : "", S(m.Tur), S(m.FormFaktor) }, " ");
            List<string> ek = new List<string>();
            if (m.HizMTs > 0) ek.Add(m.HizMTs + " MT/s");
            if (m.VoltajMv > 0) ek.Add(Bicim.Voltaj(m.VoltajMv));
            if (m.Ecc) ek.Add("ECC");
            if (m.Kayitli) ek.Add("Registered");
            if (ek.Count > 0) s += ", " + string.Join(", ", ek.ToArray());
            if (S(m.Aciklama).Length > 0) s += " — " + m.Aciklama;
            return s;
        }

        static string BaglantiHtml(Baglanti b)
        {
            string metin = Bos(S(b.Metin), S(b.Url));
            string url = GuvenliUrl(b.Url);
            if (url.Length == 0) return Html.Ka(metin);
            return "<a href=\"" + Html.Ka(url) + "\">" + Html.Ka(metin) + "</a>";
        }

        /// <summary>Sadece http/https mutlak URL'leri kabul eder; aksi halde "".</summary>
        static string GuvenliUrl(string url)
        {
            string u = S(url).Trim();
            if (u.Length == 0 || u.Length > 2048) return "";
            foreach (char ch in u) if (ch < 0x21 || ch == 0x7F) return "";
            if (!(u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))) return "";
            Uri sonuc;
            if (!Uri.TryCreate(u, UriKind.Absolute, out sonuc)) return "";
            if (sonuc.Scheme != Uri.UriSchemeHttp && sonuc.Scheme != Uri.UriSchemeHttps) return "";
            return u;
        }

        // =====================================================================
        // Kullanım
        // =====================================================================

        public static string KullanimHtml(Envanter e, DanismanSonucu d)
        {
            try { return KullanimHtmlIc(Duzelt(e), Duzelt(d)); }
            catch (Exception ex) { return HataKutusu("Kullanım", ex); }
        }

        static string KullanimHtmlIc(Envanter e, DanismanSonucu d)
        {
            StringBuilder sb = new StringBuilder(5000);
            sb.Append("<h2>Gerçekten RAM gerekiyor mu?</h2>");
            if (d.IhtiyacAnalizi.Count == 0) sb.Append("<p class=\"not\">İhtiyaç analizi yapılmadı.</p>");
            else foreach (Bulgu b in SiraliBulgular(d.IhtiyacAnalizi)) sb.Append(BulguHtml(b));

            KullanimBilgisi k = e.Kullanim ?? new KullanimBilgisi();
            sb.Append("<h2>Bellek kullanımı (anlık ölçüm)</h2>");
            if (k.ToplamMB <= 0)
            {
                sb.Append("<p class=\"not\">Kullanım ölçümü yok (ölçüm yok — rapor dosyadan/SMBIOS kaydından üretildi ya da ölçüm alınamadı).</p>");
                return sb.ToString();
            }
            int yuzde = k.YuzdeKullanim;
            if (yuzde <= 0 && k.ToplamMB > 0) yuzde = (int)Math.Round(100.0 * k.KullanilanMB / k.ToplamMB);
            if (yuzde < 0) yuzde = 0;
            if (yuzde > 100) yuzde = 100;
            string renk = yuzde >= 90 ? " cubuk-kritik" : (yuzde >= 75 ? " cubuk-uyari" : "");

            sb.Append("<div class=\"kart\">");
            sb.Append("<div><b>%").Append(yuzde).Append(" kullanımda</b> — ").Append(Html.Ka(Bicim.Boyut(k.KullanilanMB))).Append(" / ").Append(Html.Ka(Bicim.Boyut(k.ToplamMB))).Append("</div>");
            sb.Append("<div class=\"cubuk\"><div class=\"cubuk-dolu").Append(renk).Append("\" style=\"width:").Append(yuzde).Append("%\"></div></div>");
            sb.Append("<table class=\"tablo\">");
            Satir(sb, "Toplam (Windows'un gördüğü)", Bicim.Boyut(k.ToplamMB));
            Satir(sb, "Kullanılan", Bicim.Boyut(k.KullanilanMB));
            Satir(sb, "Boş (kullanılabilir)", Bicim.Boyut(k.BosMB));
            string taahhut = Bicim.Boyut(k.TaahhutMB) + " / " + Bicim.Boyut(k.TaahhutSiniriMB);
            if (k.TaahhutSiniriMB > 0) taahhut += " (%" + (int)Math.Round(100.0 * k.TaahhutMB / k.TaahhutSiniriMB) + ")";
            Satir(sb, "Taahhüt / sınır", taahhut);
            Satir(sb, "Sayfa dosyası (kullanılan / toplam)", Bicim.Boyut(k.SayfaDosyasiKullanilanMB) + " / " + Bicim.Boyut(k.SayfaDosyasiMB));
            Satir(sb, "Önbellek", Bicim.Boyut(k.OnbellekMB));
            if (k.SikistirilmisMB > 0) Satir(sb, "Sıkıştırılmış bellek", Bicim.Boyut(k.SikistirilmisMB));
            if (k.Zaman != default(DateTime)) Satir(sb, "Ölçüm zamanı", k.Zaman.ToString("yyyy-MM-dd HH:mm:ss", Inv));
            sb.Append("</table>");
            sb.Append("<p class=\"not\">Bu tek bir anlık ölçümdür. Gerçek ihtiyacı görmek için programı, en ağır işlerinizi yaparken açık bırakıp “Kullanım (canlı)” sekmesindeki zirve değerine bakın.</p>");
            sb.Append("</div>");

            sb.Append("<h2>En çok bellek kullananlar</h2>");
            if (k.EnCokKullananlar == null || k.EnCokKullananlar.Count == 0) sb.Append("<p class=\"not\">Süreç bilgisi yok.</p>");
            else
            {
                sb.Append("<div class=\"kart\"><table class=\"tablo\"><tr><th>Süreç</th><th class=\"sayi\">Adet</th><th class=\"sayi\">Özel bellek</th><th class=\"sayi\">Çalışma kümesi</th></tr>");
                foreach (SurecBellek s in k.EnCokKullananlar)
                {
                    if (s == null) continue;
                    sb.Append("<tr><td>").Append(Html.Ka(Bos(S(s.Ad), "?"))).Append("</td><td class=\"sayi\">").Append(s.Adet)
                      .Append("</td><td class=\"sayi\">").Append(Html.Ka(Bicim.Boyut(s.OzelMB))).Append("</td><td class=\"sayi\">")
                      .Append(Html.Ka(Bicim.Boyut(s.CalismaKumesiMB))).Append("</td></tr>");
                }
                sb.Append("</table></div>");
            }
            return sb.ToString();
        }

        // =====================================================================
        // Tam rapor
        // =====================================================================

        public static string TamRaporHtml(Envanter e, DanismanSonucu d)
        {
            e = Duzelt(e);
            d = Duzelt(d);
            StringBuilder sb = new StringBuilder(60000);
            string makine = MakineAdi(e);
            string model = Birlestir(new string[] { S(e.Sistem.Uretici), S(e.Sistem.ModelAdi) }, " ");
            if (S(e.Sistem.Model).Length > 0 && !string.Equals(e.Sistem.Model, e.Sistem.ModelAdi, StringComparison.OrdinalIgnoreCase)) model += " (" + e.Sistem.Model + ")";

            sb.Append("<div class=\"rapor-baslik\"><h1>RAM Danışmanı Raporu</h1>");
            sb.Append("<p>").Append(Html.Ka(Bos(model.Trim(), "Bilinmeyen cihaz"))).Append("</p>");
            sb.Append("<p class=\"not\">Oluşturma: ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm", Inv))
              .Append(" · Bilgisayar adı: ").Append(Html.Ka(makine)).Append("</p></div>");

            sb.Append("<div class=\"kart icindekiler\"><b>İçindekiler</b><ol>");
            string[,] bolumler = new string[,] {
                { "ozet", "Özet" }, { "yukseltme", "Yükseltme ve satın alma" }, { "moduller", "Modüller" }, { "kullanim", "Kullanım" },
                { "dikkat", "RAM alırken nelere dikkat etmeli" }, { "yeni", "Yeni bilgisayar alırken RAM" }, { "sozluk", "Sözlük" }, { "sss", "Sık sorulan sorular" } };
            for (int i = 0; i < bolumler.GetLength(0); i++)
                sb.Append("<li><a href=\"#").Append(bolumler[i, 0]).Append("\">").Append(Html.Ka(bolumler[i, 1])).Append("</a></li>");
            sb.Append("</ol></div>");

            Bolum(sb, "ozet", "1. Özet", OzetHtml(e, d));
            Bolum(sb, "yukseltme", "2. Yükseltme ve satın alma", YukseltmeHtml(e, d));
            Bolum(sb, "moduller", "3. Modüller", ModullerHtml(e));
            Bolum(sb, "kullanim", "4. Kullanım", KullanimHtml(e, d));
            Bolum(sb, "dikkat", "5. RAM alırken nelere dikkat etmeli", RehberGuvenli(0));
            Bolum(sb, "yeni", "6. Yeni bilgisayar alırken RAM", RehberGuvenli(1));
            Bolum(sb, "sozluk", "7. Sözlük", RehberGuvenli(2));
            Bolum(sb, "sss", "8. Sık sorulan sorular", RehberGuvenli(3));

            sb.Append("<p class=\"not\" style=\"margin-top:24px\">Bu rapor RAM Danışmanı tarafından cihazın SMBIOS/WMI bilgilerinden üretilmiştir. BIOS bilgileri bazı cihazlarda eksik ya da yanlış olabilir; satın almadan önce cihaz üreticisinin belgesinden doğrulayın.</p>");
            string baslik = "RAM Danışmanı Raporu" + (S(e.Sistem.ModelAdi).Length > 0 ? " — " + e.Sistem.ModelAdi : "");
            return Sayfa(baslik, sb.ToString(), 1.0);
        }

        static void Bolum(StringBuilder sb, string id, string baslik, string icerik)
        {
            sb.Append("<h1 class=\"bolum\" id=\"").Append(id).Append("\">").Append(Html.Ka(baslik)).Append("</h1>");
            sb.Append(icerik ?? "");
        }

        static string RehberGuvenli(int hangi)
        {
            try
            {
                string s;
                switch (hangi)
                {
                    case 0: s = Rehber.SatinAlmaRehberiHtml(); break;
                    case 1: s = Rehber.YeniBilgisayarRehberiHtml(); break;
                    case 2: s = Rehber.SozlukHtml(); break;
                    default: s = Rehber.SssHtml(); break;
                }
                // Rehber metni kendi <h1 id=...> başlığıyla gelir; raporda bölüm başlığı zaten var:
                // çift başlık ve yinelenen id olmaması için ilk h1 atılır (programdaki sekmeler başlığı görmeye devam eder).
                return System.Text.RegularExpressions.Regex.Replace(S(s), @"^\s*<h1[^>]*>.*?</h1>", "",
                    System.Text.RegularExpressions.RegexOptions.Singleline);
            }
            catch (Exception ex) { return HataKutusu("Rehber", ex); }
        }

        // =====================================================================
        // Düz metin
        // =====================================================================

        const int Genislik = 100;

        public static string Metin(Envanter e, DanismanSonucu d)
        {
            try { return MetinIc(Duzelt(e), Duzelt(d)); }
            catch (Exception ex) { return "RAM Danışmanı: rapor üretilemedi (" + ex.GetType().Name + ": " + ex.Message + ")" + Environment.NewLine; }
        }

        static string MetinIc(Envanter e, DanismanSonucu d)
        {
            StringBuilder sb = new StringBuilder(8000);
            SistemBilgisi s = e.Sistem;
            sb.AppendLine("RAM DANIŞMANI RAPORU");
            sb.AppendLine("Oluşturma: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", Inv) + "   Bilgisayar adı: " + Duz(MakineAdi(e)));

            MBaslik(sb, "CEVAP");
            Sar(sb, Bos(S(d.OzetCumle), "Değerlendirme yapılamadı."), "", "");
            MSatir(sb, "Durum", DurumMetni(d.Durum));

            MBaslik(sb, "SİSTEM");
            MSatir(sb, "Üretici", s.Uretici);
            MSatir(sb, "Model adı", s.ModelAdi);
            MSatir(sb, "Model", s.Model);
            MSatir(sb, "Seri no", s.SeriNo);
            MSatir(sb, "İşlemci", s.IslemciAdi);
            MSatir(sb, "İşletim sistemi", S(s.IsletimSistemi) + (S(s.IsletimSistemi).Length > 0 ? (s.Isletim64Bit ? " (64 bit)" : " (32 bit)") : ""));
            MSatir(sb, "BIOS", Birlestir(new string[] { S(s.BiosSurum), S(s.BiosTarih) }, " — "));
            MSatir(sb, "Anakart", Birlestir(new string[] { S(s.AnakartUretici), S(s.AnakartModel) }, " "));
            MSatir(sb, "Kasa türü", KasaAdi(s.Kasa));
            if (s.SanalMakine) MSatir(sb, "Sanal makine", "Evet " + S(s.SanalMakineNotu));

            List<BellekYuvasi> dolu = DoluYuvalar(e);
            MBaslik(sb, "BELLEK");
            long takili = s.TakiliRamMB > 0 ? s.TakiliRamMB : ToplamTakili(e);
            MSatir(sb, "Toplam takılı", Bicim.Boyut(takili) + (s.KullanilabilirRamMB > 0 ? "  (Windows'un kullanabildiği: " + Bicim.Boyut(s.KullanilabilirRamMB) + ")" : ""));
            int toplamYuva = ToplamYuva(e, d);
            MSatir(sb, "Yuva", dolu.Count + " dolu / " + (toplamYuva > 0 ? toplamYuva.ToString(Inv) : "?") + " toplam" +
                (d.YuvaBilgisiGuvenilir ? "" : "  (BIOS bilgisi güvenilir değil — üretici belgesinden doğrulayın)"));
            MSatir(sb, "Tür / form", Birlestir(new string[] { Birlestir(dolu.Select(y => S(y.Tur))), Birlestir(dolu.Select(y => S(y.FormFaktor))) }, " "));
            string hizAna = HizOzeti(dolu, false);
            MSatir(sb, "Hız", hizAna.IndexOf(" / ", StringComparison.Ordinal) > 0 ? hizAna + " MT/s (etiket / çalışan)" : hizAna);
            if (d.PratikAzamiGB > 0)
                MSatir(sb, "Azami", d.PratikAzamiGB + " GB pratikte (" + S(d.PratikAzamiMetni) + ")" +
                    (d.AzamiGB > 0 ? "; " + Bos(S(d.AzamiGBKaynagi), "üst sınır") + ": " + d.AzamiGB + " GB" : ""));
            else
                MSatir(sb, "Azami", (d.AzamiGB > 0 ? d.AzamiGB + " GB" : "Bilinmiyor") + (S(d.AzamiGBKaynagi).Length > 0 ? "  (" + d.AzamiGBKaynagi + ")" : ""));
            MSatir(sb, "Kanal", d.KanalDurumu);

            MBaslik(sb, "YUVALAR");
            sb.AppendLine(Sutun("Yuva", 28) + Sutun("Durum", 8) + Sutun("Boyut", 8) + Sutun("Tür", 8) + Sutun("Form", 8) + Sutun("Hız etk/çlş", 12) + Sutun("Üretici", 12) + "Parça no");
            sb.AppendLine(new string('-', Genislik));
            int sira = 0;
            foreach (BellekYuvasi y in e.Yuvalar)
            {
                if (y == null) continue;
                sira++;
                string durum = y.Lehimli ? "Lehimli" : (y.Dolu ? "Dolu" : "BOŞ");
                string hiz = y.Dolu ? (y.HizMTs > 0 ? y.HizMTs.ToString(Inv) : "?") + "/" + (y.YapilandirilmisHizMTs > 0 ? y.YapilandirilmisHizMTs.ToString(Inv) : "?") : "";
                string satir = Sutun(Bos(S(y.Yuva), sira + ". yuva"), 28) + Sutun(durum, 8) + Sutun(y.Dolu ? Bicim.Boyut(y.BoyutMB) : "", 8) +
                    Sutun(y.Dolu ? S(y.Tur) : "", 8) + Sutun(S(y.FormFaktor), 8) + Sutun(hiz, 12) + Sutun(y.Dolu ? Bos(S(y.Uretici), S(y.UreticiHam)) : "", 12) +
                    Kes(y.Dolu ? Duz(S(y.ParcaNo).Trim()) : "", 24);
                sb.AppendLine(satir.TrimEnd());
            }
            if (e.Yuvalar.Count == 0) sb.AppendLine("(yuva bilgisi okunamadı)");
            for (int i = 0; i < Math.Min(d.SupheliEkYuva, 6); i++)
                sb.AppendLine(Sutun("? Muhtemel " + (e.Yuvalar.Count + i + 1) + ". yuva", 28) + "BIOS göstermiyor — üretici belgesinden doğrulayın");

            MBaslik(sb, "BULGULAR");
            if (d.Bulgular.Count == 0) sb.AppendLine("Kayda değer bulgu yok.");
            foreach (Bulgu b in SiraliBulgular(d.Bulgular)) MBulgu(sb, b);

            MBaslik(sb, "NE ALMALIYIM?");
            sb.Append(AlisverisMetni(e, d));

            MBaslik(sb, "YÜKSELTME SEÇENEKLERİ");
            if (d.Senaryolar.Count == 0) sb.AppendLine("Senaryo yok.");
            foreach (YukseltmeSenaryosu sn in SiraliSenaryolar(d.Senaryolar))
            {
                sb.AppendLine("* " + Duz(Bos(S(sn.Ad), "Senaryo")) + (sn.Tavsiye ? "  [ÖNERİLEN]" : ""));
                if (S(sn.Aciklama).Length > 0) Sar(sb, sn.Aciklama, "    ", "    ");
                if (sn.Alinacaklar != null) foreach (AlinacakModul m in sn.Alinacaklar) if (m != null) Sar(sb, ModulMetni(m), "    Al   : ", "           ");
                if (sn.Cikarilacaklar != null && sn.Cikarilacaklar.Count > 0) Sar(sb, string.Join(", ", sn.Cikarilacaklar.ToArray()), "    Çıkar: ", "           ");
                sb.AppendLine("    Sonuç: " + (sn.SonucToplamGB > 0 ? sn.SonucToplamGB + " GB" : "?") + ", " + KanalMetni(sn));
                if (sn.Notlar != null) foreach (string n in sn.Notlar) Sar(sb, n, "    - ", "      ");
            }

            if (d.KontrolListesi.Count > 0)
            {
                MBaslik(sb, "SATIN ALMADAN ÖNCE KONTROL");
                foreach (string k in d.KontrolListesi) Sar(sb, k, "[ ] ", "    ");
            }
            if (d.TakmaAdimlari.Count > 0)
            {
                MBaslik(sb, "TAKMA ADIMLARI");
                int no = 0;
                foreach (string k in d.TakmaAdimlari) { no++; Sar(sb, k, (no + ".").PadRight(4), "    "); }
            }

            MBaslik(sb, "İHTİYAÇ ANALİZİ VE KULLANIM");
            foreach (Bulgu b in SiraliBulgular(d.IhtiyacAnalizi)) MBulgu(sb, b);
            KullanimBilgisi ku = e.Kullanim ?? new KullanimBilgisi();
            if (ku.ToplamMB <= 0) { if (d.IhtiyacAnalizi.Count == 0) sb.AppendLine("Kullanım ölçümü yok."); }
            else
            {
                MSatir(sb, "Kullanım", "%" + ku.YuzdeKullanim + "  (" + Bicim.Boyut(ku.KullanilanMB) + " / " + Bicim.Boyut(ku.ToplamMB) + ", boş " + Bicim.Boyut(ku.BosMB) + ")");
                MSatir(sb, "Taahhüt / sınır", Bicim.Boyut(ku.TaahhutMB) + " / " + Bicim.Boyut(ku.TaahhutSiniriMB));
                MSatir(sb, "Sayfa dosyası", Bicim.Boyut(ku.SayfaDosyasiKullanilanMB) + " / " + Bicim.Boyut(ku.SayfaDosyasiMB));
                if (ku.EnCokKullananlar != null && ku.EnCokKullananlar.Count > 0)
                {
                    sb.AppendLine("En çok bellek kullananlar (özel bellek):");
                    foreach (SurecBellek p in ku.EnCokKullananlar.Take(10))
                        if (p != null) sb.AppendLine("  " + Sutun(Duz(S(p.Ad)) + (p.Adet > 1 ? " (" + p.Adet + ")" : ""), 40) + Bicim.Boyut(p.OzelMB));
                }
            }

            if (d.Baglantilar.Count > 0)
            {
                MBaslik(sb, "BAĞLANTILAR");
                foreach (Baglanti b in d.Baglantilar) if (b != null) sb.AppendLine("- " + Duz(S(b.Metin)) + ": " + Duz(S(b.Url)));
            }
            if (e.ToplamaNotlari.Count > 0)
            {
                MBaslik(sb, "TOPLAMA NOTLARI");
                foreach (string n in e.ToplamaNotlari) Sar(sb, n, "- ", "  ");
            }
            return sb.ToString();
        }

        static void MBaslik(StringBuilder sb, string baslik)
        {
            sb.AppendLine();
            sb.AppendLine("=== " + baslik + " ===");
        }

        static void MSatir(StringBuilder sb, string anahtar, string deger)
        {
            string on = anahtar.PadRight(18) + ": ";
            Sar(sb, Bos(S(deger), "—"), on, new string(' ', on.Length));
        }

        static void MBulgu(StringBuilder sb, Bulgu b)
        {
            if (b == null) return;
            string on = "[" + SeviyeMetni(b.Seviye) + "] ";
            string metin = Duz(S(b.Baslik)) + (S(b.Aciklama).Length > 0 ? " — " + S(b.Aciklama) : "");
            Sar(sb, metin, on, new string(' ', on.Length));
        }

        /// <summary>Metni satır genişliğine sarar; \n satır sonlarını korur.</summary>
        static void Sar(StringBuilder sb, string metin, string ilkOn, string sonrakiOn)
        {
            string[] paragraflar = S(metin).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            bool ilk = true;
            foreach (string par in paragraflar)
            {
                string kalan = Duz(par).Trim();
                do
                {
                    string on = ilk ? ilkOn : sonrakiOn;
                    ilk = false;
                    int sigan = Genislik - on.Length;
                    if (sigan < 20) sigan = 20;
                    string parca;
                    if (kalan.Length <= sigan) { parca = kalan; kalan = ""; }
                    else
                    {
                        int kes = kalan.LastIndexOf(' ', sigan);
                        if (kes < sigan / 2) kes = sigan;
                        parca = kalan.Substring(0, kes).TrimEnd();
                        kalan = kalan.Substring(kes).TrimStart();
                    }
                    sb.AppendLine((on + parca).TrimEnd());
                } while (kalan.Length > 0);
            }
        }

        static string Sutun(string s, int genislik)
        {
            string t = Kes(Duz(S(s)), genislik - 1);
            return t.PadRight(genislik);
        }

        static string Kes(string s, int uzunluk)
        {
            s = S(s);
            if (uzunluk <= 0) return "";
            if (s.Length <= uzunluk) return s;
            if (uzunluk <= 1) return s.Substring(0, uzunluk);
            return s.Substring(0, uzunluk - 1) + "…";
        }

        /// <summary>Konsol için kontrol karakterlerini boşluğa çevirir (\n hariç).</summary>
        static string Duz(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            StringBuilder sb = new StringBuilder(s.Length);
            foreach (char c in s) sb.Append(c == '\n' ? '\n' : (char.IsControl(c) ? ' ' : c));
            return sb.ToString();
        }

        // =====================================================================
        // Alışveriş metni
        // =====================================================================

        public static string AlisverisMetni(DanismanSonucu d)
        {
            return AlisverisMetni(null, d);
        }

        /// <summary>Ek: cihaz modeli satırını da yazan sürüm (e null olabilir).</summary>
        public static string AlisverisMetni(Envanter e, DanismanSonucu d)
        {
            try
            {
                d = Duzelt(d);
                SatinAlmaOzeti a = d.Alinacak ?? new SatinAlmaOzeti();
                StringBuilder sb = new StringBuilder(800);
                string cihaz = "";
                if (e != null && e.Sistem != null)
                {
                    cihaz = Birlestir(new string[] { S(e.Sistem.Uretici), S(e.Sistem.ModelAdi) }, " ");
                    if (S(e.Sistem.Model).Length > 0 && !string.Equals(e.Sistem.Model, e.Sistem.ModelAdi, StringComparison.OrdinalIgnoreCase))
                        cihaz += " (" + e.Sistem.Model + ")";
                    cihaz = Duz(cihaz.Trim());
                }

                if (!a.Gecerli)
                {
                    sb.AppendLine("RAM satın alma önerisi: " + (d.Durum == YukseltmeDurumu.Hayir ? "YOK — bu cihazın RAM'i yükseltilemiyor." : "BELİRSİZ — ne alınacağı kesin belirlenemedi."));
                    Sar(sb, Bos(S(a.Gerekce), Bos(S(d.OzetCumle), "Bu bilgisayar için uygun bellek modülü belirlenemedi.")), "Neden: ", "       ");
                    if (cihaz.Length > 0) sb.AppendLine("Cihaz: " + cihaz);
                    sb.AppendLine("Not: Satın almadan önce cihaz üreticisinin destek sayfasından bellek özelliklerini kontrol edin.");
                    return sb.ToString();
                }

                sb.AppendLine("Alınacak: " + Duz(AlinacakBasligi(a)));
                List<string> oz = new List<string>();
                if (a.HizMTs > 0) oz.Add(a.HizMTs + " MT/s");
                if (a.VoltajMv > 0) oz.Add(Bicim.Voltaj(a.VoltajMv));
                oz.Add(a.Ecc ? "ECC" : "ECC'siz (non-ECC)");
                oz.Add(a.Kayitli ? "Registered (RDIMM)" : "Unbuffered");
                sb.AppendLine("Özellikler: " + string.Join(", ", oz.ToArray()));
                if (S(a.AramaMetni).Length > 0) sb.AppendLine("Mağazada aranacak: " + Duz(a.AramaMetni));
                if (S(a.OnerilenParcaNo).Length > 0) sb.AppendLine("Birebir eş parça no (bulunursa tercih edin): " + Duz(a.OnerilenParcaNo.Trim()));
                List<string> uy = KarisiklikUyarilari(a);
                if (uy.Count > 0) Sar(sb, string.Join("; ", uy.ToArray()) + ".", "Dikkat: ", "        ");
                if (cihaz.Length > 0) sb.AppendLine("Cihaz: " + cihaz);
                if (S(a.Gerekce).Length > 0) Sar(sb, a.Gerekce, "Gerekçe: ", "         ");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Alışveriş metni üretilemedi (" + ex.GetType().Name + ")" + Environment.NewLine;
            }
        }

        // =====================================================================
        // CSV (';' ayraçlı, Türkçe Excel)
        // =====================================================================

        static readonly string[] CsvAlanlari = new string[] {
            "Tarih", "Bilgisayar adı", "Kullanıcı", "Üretici", "Model adı", "Model", "Seri no", "İşlemci", "İşletim sistemi",
            "Toplam RAM (GB)", "Kullanılabilir RAM (GB)", "Dolu yuva", "Toplam yuva", "Yuva bilgisi güvenilir", "Tür", "Form",
            "Etiket hızı (MT/s)", "Çalışan hız (MT/s)", "Modüller", "Azami (GB)", "Kanal", "Durum", "Özet", "Alınacak", "Arama metni", "Parça no" };

        public static string CsvBaslik()
        {
            List<string> l = new List<string>();
            foreach (string a in CsvAlanlari) l.Add(CsvAlan(a));
            return string.Join(";", l.ToArray());
        }

        public static string CsvSatir(Envanter e, DanismanSonucu d)
        {
            try
            {
                e = Duzelt(e);
                d = Duzelt(d);
                SistemBilgisi s = e.Sistem;
                List<BellekYuvasi> dolu = DoluYuvalar(e);
                long takili = s.TakiliRamMB > 0 ? s.TakiliRamMB : ToplamTakili(e);
                int etiket = 0, calisan = 0;
                foreach (BellekYuvasi y in dolu)
                {
                    if (y.HizMTs > etiket) etiket = y.HizMTs;
                    if (y.YapilandirilmisHizMTs > 0 && (calisan == 0 || y.YapilandirilmisHizMTs < calisan)) calisan = y.YapilandirilmisHizMTs;
                }
                List<string> moduller = new List<string>();
                foreach (BellekYuvasi y in dolu)
                    moduller.Add(Birlestir(new string[] { Bicim.Boyut(y.BoyutMB).Replace(" ", ""), S(y.Tur) + (y.HizMTs > 0 ? "-" + y.HizMTs : ""), Bos(S(y.Uretici), S(y.UreticiHam)), S(y.ParcaNo).Trim() }, " "));
                int toplamYuva = ToplamYuva(e, d);
                SatinAlmaOzeti a = d.Alinacak ?? new SatinAlmaOzeti();

                List<string> alanlar = new List<string>();
                alanlar.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm", Inv));
                alanlar.Add(MakineAdi(e));
                alanlar.Add((DosyadanMi(e) ? "" : KullaniciAdi()));
                alanlar.Add(S(s.Uretici));
                alanlar.Add(S(s.ModelAdi));
                alanlar.Add(S(s.Model));
                alanlar.Add(S(s.SeriNo));
                alanlar.Add(S(s.IslemciAdi));
                alanlar.Add(S(s.IsletimSistemi));
                alanlar.Add(GbSayi(takili));
                alanlar.Add(GbSayi(s.KullanilabilirRamMB));
                alanlar.Add(dolu.Count.ToString(Inv));
                alanlar.Add(toplamYuva > 0 ? toplamYuva.ToString(Inv) : "");
                alanlar.Add(d.YuvaBilgisiGuvenilir ? "Evet" : "Hayır");
                alanlar.Add(Birlestir(dolu.Select(y => S(y.Tur))));
                alanlar.Add(Birlestir(dolu.Select(y => S(y.FormFaktor))));
                alanlar.Add(etiket > 0 ? etiket.ToString(Inv) : "");
                alanlar.Add(calisan > 0 ? calisan.ToString(Inv) : "");
                alanlar.Add(string.Join(" | ", moduller.ToArray()));
                alanlar.Add(d.AzamiGB > 0 ? d.AzamiGB.ToString(Inv) : "");
                alanlar.Add(S(d.KanalDurumu));
                alanlar.Add(DurumMetni(d.Durum));
                alanlar.Add(S(d.OzetCumle));
                alanlar.Add(a.Gecerli ? AlinacakBasligi(a) : "");
                alanlar.Add(a.Gecerli ? S(a.AramaMetni) : "");
                alanlar.Add(a.Gecerli ? S(a.OnerilenParcaNo).Trim() : "");

                List<string> kacik = new List<string>();
                foreach (string f in alanlar) kacik.Add(CsvAlan(f));
                return string.Join(";", kacik.ToArray());
            }
            catch (Exception ex)
            {
                return CsvAlan(DateTime.Now.ToString("yyyy-MM-dd HH:mm", Inv)) + ";" + CsvAlan(MakineAdi()) + ";" + CsvAlan("HATA: " + ex.Message);
            }
        }

        /// <summary>Tek CSV alanı: satır sonları boşluğa çevrilir (bir PC = bir satır), ; " varsa çift tırnakla sarılır,
        /// Excel formül enjeksiyonuna karşı = + - @ ile başlayan metnin başına ' eklenir.</summary>
        static string CsvAlan(string s)
        {
            s = S(s).Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
            if (s.Length > 0 && (s[0] == '=' || s[0] == '+' || s[0] == '-' || s[0] == '@')) s = "'" + s;
            if (s.IndexOf(';') >= 0 || s.IndexOf('"') >= 0)
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        static string GbSayi(long mb)
        {
            if (mb <= 0) return "";
            double gb = mb / 1024.0;
            return gb.ToString("0.#", Tr);
        }

        // =====================================================================
        // Ortak yardımcılar
        // =====================================================================

        /// <summary>Ek: durum metni (Evet → "Yükseltilebilir" ...).</summary>
        public static string DurumMetni(YukseltmeDurumu durum)
        {
            switch (durum)
            {
                case YukseltmeDurumu.Evet: return "Yükseltilebilir";
                case YukseltmeDurumu.Kismen: return "Kısmen";
                case YukseltmeDurumu.Hayir: return "Yükseltilemez";
                default: return "Belirsiz";
            }
        }

        static string DurumEtiketi(YukseltmeDurumu durum)
        {
            string sinif;
            switch (durum)
            {
                case YukseltmeDurumu.Evet: sinif = "etiket etiket-iyi"; break;
                case YukseltmeDurumu.Kismen: sinif = "etiket etiket-uyari"; break;
                case YukseltmeDurumu.Hayir: sinif = "etiket etiket-kritik"; break;
                default: sinif = "etiket"; break;
            }
            return "<span class=\"" + sinif + "\">" + Html.Ka(DurumMetni(durum)) + "</span>";
        }

        static string SeviyeMetni(Seviye s)
        {
            switch (s)
            {
                case Seviye.Kritik: return "KRİTİK";
                case Seviye.Uyari: return "UYARI";
                case Seviye.Iyi: return "İYİ";
                default: return "BİLGİ";
            }
        }

        static string SeviyeSinifi(Seviye s)
        {
            switch (s)
            {
                case Seviye.Kritik: return "kritik";
                case Seviye.Uyari: return "uyari";
                case Seviye.Iyi: return "iyi";
                default: return "bilgi";
            }
        }

        static List<Bulgu> SiraliBulgular(List<Bulgu> l)
        {
            if (l == null) return new List<Bulgu>();
            // Kritik → Uyarı → İyi → Bilgi; aynı seviyede orijinal sıra korunur (OrderBy kararlıdır)
            return l.Where(b => b != null).OrderByDescending(b => (int)b.Seviye).ToList();
        }

        /// <summary>Önerilen senaryo(lar) önce; diğerleri orijinal sırada.</summary>
        /// <summary>Asimetrik (flex mode) senaryo: bir kısmı çift, kalanı tek kanal çalışır.</summary>
        static bool FlexSenaryo(YukseltmeSenaryosu s)
        {
            if (s == null || s.CiftKanal) return false;
            if (s.Flex) return true;
            return S(s.Ad).IndexOf("flex", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   S(s.Aciklama).IndexOf("flex", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static string KanalMetni(YukseltmeSenaryosu s)
        {
            if (s.Sunucu) return "kanal yerleşimi üretici kılavuzuna göre";
            if (s.CiftKanal) return "çift kanal";
            if (FlexSenaryo(s)) return "kısmi çift kanal (flex mode)";
            return "tek kanal";
        }

        static List<YukseltmeSenaryosu> SiraliSenaryolar(List<YukseltmeSenaryosu> l)
        {
            if (l == null) return new List<YukseltmeSenaryosu>();
            return l.Where(s => s != null).OrderBy(s => s.Tavsiye ? 0 : 1).ToList();
        }

        static string BulguHtml(Bulgu b)
        {
            string sinif = SeviyeSinifi(b.Seviye);
            string etiketSinif = b.Seviye == Seviye.Bilgi ? "etiket etiket-bilgi" : "etiket etiket-" + sinif;
            StringBuilder sb = new StringBuilder(300);
            sb.Append("<div class=\"kart ").Append(sinif).Append("\"><div class=\"bulgu-baslik\"><span class=\"").Append(etiketSinif).Append("\">")
              .Append(Html.Ka(SeviyeMetni(b.Seviye))).Append("</span> ").Append(Html.Ka(S(b.Baslik))).Append("</div>");
            if (S(b.Aciklama).Length > 0) sb.Append("<div class=\"bulgu-aciklama\">").Append(Html.Ka(b.Aciklama)).Append("</div>");
            sb.Append("</div>");
            return sb.ToString();
        }

        static void Satir(StringBuilder sb, string anahtar, string deger)
        {
            sb.Append("<tr><td class=\"anahtar\">").Append(Html.Ka(anahtar)).Append("</td><td>").Append(Html.Ka(Bos(S(deger).Trim(), "—"))).Append("</td></tr>");
        }

        static void SatirKod(StringBuilder sb, string anahtar, string deger)
        {
            string d = S(deger).Trim();
            if (d.Length == 0) { Satir(sb, anahtar, ""); return; }
            int nl = d.IndexOf('\n');
            string ilk = nl >= 0 ? d.Substring(0, nl) : d;
            string kalan = nl >= 0 ? d.Substring(nl + 1) : "";
            sb.Append("<tr><td class=\"anahtar\">").Append(Html.Ka(anahtar)).Append("</td><td><span class=\"kod\">").Append(Html.Ka(ilk)).Append("</span>");
            if (kalan.Length > 0) sb.Append("<br><span class=\"not\">").Append(Html.Ka(kalan)).Append("</span>");
            sb.Append("</td></tr>");
        }

        static string HataKutusu(string bolum, Exception ex)
        {
            return "<div class=\"kart kritik\"><div class=\"bulgu-baslik\">" + Html.Ka(bolum) + " bölümü üretilemedi</div><div class=\"bulgu-aciklama\">" +
                   Html.Ka(ex.GetType().Name + ": " + ex.Message) + "</div></div>";
        }

        static List<BellekYuvasi> DoluYuvalar(Envanter e)
        {
            List<BellekYuvasi> l = new List<BellekYuvasi>();
            if (e == null || e.Yuvalar == null) return l;
            foreach (BellekYuvasi y in e.Yuvalar) if (y != null && y.Dolu) l.Add(y);
            return l;
        }

        static long ToplamTakili(Envanter e)
        {
            long t = 0;
            foreach (BellekYuvasi y in DoluYuvalar(e)) t += y.BoyutMB;
            return t;
        }

        static int ToplamYuva(Envanter e, DanismanSonucu d)
        {
            if (d.ToplamYuva > 0) return d.ToplamYuva;
            return e.Yuvalar.Count;
        }

        static string Hiz(BellekYuvasi y)
        {
            if (y.HizMTs > 0 && y.YapilandirilmisHizMTs > 0 && y.YapilandirilmisHizMTs != y.HizMTs)
                return y.HizMTs + " (çalışan " + y.YapilandirilmisHizMTs + ") MT/s";
            int h = y.YapilandirilmisHizMTs > 0 ? y.YapilandirilmisHizMTs : y.HizMTs;
            return h > 0 ? h + " MT/s" : "";
        }

        static string HizOzeti(List<BellekYuvasi> dolu, bool alt)
        {
            int etiket = 0, calisan = 0;
            foreach (BellekYuvasi y in dolu)
            {
                if (y.HizMTs > etiket) etiket = y.HizMTs;
                if (y.YapilandirilmisHizMTs > 0 && (calisan == 0 || y.YapilandirilmisHizMTs < calisan)) calisan = y.YapilandirilmisHizMTs;
            }
            bool farkli = etiket > 0 && calisan > 0 && etiket != calisan;
            if (alt)
            {
                if (farkli) return "MT/s — etiket / çalışan";
                if (etiket == 0 && calisan == 0) return "hız bilgisi yok";
                return "etiket ve çalışan hız aynı";
            }
            if (farkli) return etiket + " / " + calisan;
            int h = calisan > 0 ? calisan : etiket;
            return h > 0 ? h + " MT/s" : "—";
        }

        static string FormAciklamasi(string form)
        {
            string f = S(form).ToUpperInvariant();
            if (f.IndexOf("SODIMM", StringComparison.Ordinal) >= 0 || f.IndexOf("SO-DIMM", StringComparison.Ordinal) >= 0) return " (dizüstü tipi kısa modül)";
            if (f == "DIMM" || f == "UDIMM") return " (masaüstü tipi uzun modül)";
            if (f.IndexOf("CAMM", StringComparison.Ordinal) >= 0) return " (yeni nesil düz modül)";
            if (f.IndexOf("ROW OF CHIPS", StringComparison.Ordinal) >= 0 || f.IndexOf("CHIP", StringComparison.Ordinal) >= 0) return " (anakarta lehimli yongalar)";
            return "";
        }

        static string KasaAdi(KasaTipi k)
        {
            switch (k)
            {
                case KasaTipi.Masaustu: return "Masaüstü";
                case KasaTipi.Dizustu: return "Dizüstü";
                case KasaTipi.HepsiBirArada: return "Hepsi bir arada";
                case KasaTipi.MiniPc: return "Mini PC";
                case KasaTipi.Sunucu: return "Sunucu";
                case KasaTipi.Tablet: return "Tablet";
                case KasaTipi.SanalMakine: return "Sanal makine";
                default: return "Bilinmiyor";
            }
        }

        static string Hex(int kod)
        {
            if (kod <= 0xFF) return "0x" + kod.ToString("X2", Inv);
            return "0x" + kod.ToString("X4", Inv);
        }

        static string Birlestir(IEnumerable<string> parcalar)
        {
            // tekrarları atarak "/" ile birleştirir (karışık modüller için "DDR4/DDR5")
            List<string> l = new List<string>();
            foreach (string p in parcalar)
            {
                string t = S(p).Trim();
                if (t.Length == 0) continue;
                bool var = false;
                foreach (string x in l) if (string.Equals(x, t, StringComparison.OrdinalIgnoreCase)) { var = true; break; }
                if (!var) l.Add(t);
            }
            return string.Join("/", l.ToArray());
        }

        static string Birlestir(IEnumerable<string> parcalar, string ayrac)
        {
            List<string> l = new List<string>();
            foreach (string p in parcalar)
            {
                string t = S(p).Trim();
                if (t.Length > 0) l.Add(t);
            }
            return string.Join(ayrac, l.ToArray());
        }

        static string S(string s) { return s ?? ""; }

        static string Bos(string s, string yedek) { return string.IsNullOrEmpty(s) ? (yedek ?? "") : s; }

        static string MakineAdi()
        {
            try { return Environment.MachineName ?? ""; } catch { return ""; }
        }

        /// <summary>Envanter --smbios dosyasından mı okundu? (O zaman bu makinenin adı/kullanıcısı rapora yazılmaz.)</summary>
        static bool DosyadanMi(Envanter e)
        {
            if (e == null || e.ToplamaNotlari == null) return false;
            foreach (string n in e.ToplamaNotlari)
                if (n != null && n.StartsWith("Dosyadan okundu", StringComparison.Ordinal)) return true;
            return false;
        }

        static string MakineAdi(Envanter e)
        {
            return DosyadanMi(e) ? "(SMBIOS dosyasından)" : MakineAdi();
        }

        static string KullaniciAdi()
        {
            try { return Environment.UserName ?? ""; } catch { return ""; }
        }

        static Envanter Duzelt(Envanter e)
        {
            if (e == null) e = new Envanter();
            if (e.Sistem == null) e.Sistem = new SistemBilgisi();
            if (e.Diziler == null) e.Diziler = new List<BellekDizisi>();
            if (e.Yuvalar == null) e.Yuvalar = new List<BellekYuvasi>();
            if (e.Kullanim == null) e.Kullanim = new KullanimBilgisi();
            if (e.Platform == null) e.Platform = new PlatformBilgisi();
            if (e.ToplamaNotlari == null) e.ToplamaNotlari = new List<string>();
            return e;
        }

        static DanismanSonucu Duzelt(DanismanSonucu d)
        {
            if (d == null) d = new DanismanSonucu();
            if (d.Bulgular == null) d.Bulgular = new List<Bulgu>();
            if (d.IhtiyacAnalizi == null) d.IhtiyacAnalizi = new List<Bulgu>();
            if (d.Senaryolar == null) d.Senaryolar = new List<YukseltmeSenaryosu>();
            if (d.Alinacak == null) d.Alinacak = new SatinAlmaOzeti();
            if (d.KontrolListesi == null) d.KontrolListesi = new List<string>();
            if (d.TakmaAdimlari == null) d.TakmaAdimlari = new List<string>();
            if (d.Baglantilar == null) d.Baglantilar = new List<Baglanti>();
            return d;
        }
    }
}
