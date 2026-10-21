// RAM Danışmanı — ana pencere (designer dosyası yok, tamamen kodla kurulur).
// HTML sekmeleri WebBrowser (IE motoru) ile Rapor/Rehber çıktısını gösterir;
// "Kullanım (canlı)" sekmesi yerel kontrollerle 2 saniyede bir güncellenir.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RamDanismani
{
    public class AnaForm : Form
    {
        // ---- Girdi ---------------------------------------------------------
        readonly byte[] smbiosTablo;
        readonly int smbiosMajor;
        readonly int smbiosMinor;
        readonly string smbiosDosyasi;
        readonly string ekranKlasoru;

        /// <summary>Program'ın çıkış kodu (--ekran modunda 0 = görüntüler kaydedildi).</summary>
        public int CikisKodu;

        readonly double olcek = 1.0;
        static readonly CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");

        // ---- Kontroller ----------------------------------------------------
        ToolStrip arac;
        ToolStripButton btnYenile, btnKaydet, btnTarayici, btnMetin, btnAlisveris, btnHakkinda;
        Panel icerik;
        TabControl sekmeler;
        TabPage sekKullanim;
        WebBrowser wbOzet, wbModuller, wbYukseltme, wbDikkat, wbYeni, wbSozluk;
        readonly List<WebBrowser> tarayicilar = new List<WebBrowser>();
        readonly Dictionary<WebBrowser, bool> belgeBekleniyor = new Dictionary<WebBrowser, bool>();
        Panel yukleniyorPaneli;
        Label yukleniyorEtiketi;
        StatusStrip durumCubugu;
        ToolStripStatusLabel durumEtiketi, kaynakEtiketi;
        ToolTip ipucu;
        BackgroundWorker isci;

        // ---- Veri ----------------------------------------------------------
        Envanter envanter;
        DanismanSonucu sonuc;
        bool veriYuklendi;

        // ---- Canlı kullanım ------------------------------------------------
        System.Windows.Forms.Timer kullanimZamanlayici;
        KullanimCubugu cubukKullanim, cubukTaahhut;
        Label lToplam, lKullanilan, lBos, lOnbellek, lTaahhut, lTaahhutOran, lSayfa, lSikistirilmis;
        Label lZirve, lYorum, lAnaliz, lKaynakNotu;
        ListView lvSurecler;
        volatile bool kullanimOkunuyor;
        bool kullanimHatasiKaydedildi;
        KullanimBilgisi sonKullanim;
        int zirveYuzde = -1;
        DateTime zirveZaman;
        long zirveTaahhutMB;
        long zirveKullanilanMB;
        DateTime izlemeBasi = DateTime.Now;

        // ---- --ekran modu --------------------------------------------------
        System.Windows.Forms.Timer ekranZamanlayici;
        DateTime ekranBaslangic;
        bool ekranYakalaniyor;
        int ekranSekme;
        int ekranHata;

        // ====================================================================

        public AnaForm() : this(null, 0, 0, null, null) { }

        public AnaForm(byte[] smbiosTablo, int major, int minor, string smbiosDosyasi, string ekranKlasoru)
        {
            this.smbiosTablo = smbiosTablo;
            this.smbiosMajor = major;
            this.smbiosMinor = minor;
            this.smbiosDosyasi = smbiosDosyasi;
            this.ekranKlasoru = ekranKlasoru;

            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                    olcek = g.DpiX / 96.0;
            }
            catch { olcek = 1.0; }
            if (olcek < 1.0 || double.IsNaN(olcek)) olcek = 1.0;

            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F);
            Text = Program.ProgramAdi;
            ClientSize = new Size(1100, 780);
            MinimumSize = new Size(760, 520);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }

            ArayuzuKur();

            ResumeLayout(false);
            PerformLayout();
        }

        // ---- Arayüz kurulumu ---------------------------------------------

        void ArayuzuKur()
        {
            ipucu = new ToolTip();

            // Araç çubuğu
            arac = new ToolStrip();
            arac.GripStyle = ToolStripGripStyle.Hidden;
            arac.Padding = new Padding(6, 3, 6, 3);
            arac.RenderMode = ToolStripRenderMode.System;
            btnYenile = AracDugmesi("Yenile", "Bellek bilgilerini yeniden oku (F5)", Yenile_Click);
            btnKaydet = AracDugmesi("Raporu kaydet (HTML)…", "Tam raporu HTML dosyası olarak kaydet (Ctrl+S)", Kaydet_Click);
            btnTarayici = AracDugmesi("Tarayıcıda aç", "Tam raporu varsayılan tarayıcıda aç (yazdırmak için de kullanılabilir)", Tarayici_Click);
            btnMetin = AracDugmesi("Metni kopyala", "Raporu düz metin olarak panoya kopyala (e-posta / destek kaydı için)", MetinKopyala_Click);
            btnAlisveris = AracDugmesi("Alışveriş metnini kopyala", "Alınacak RAM'in kısa tanımını panoya kopyala (mağazaya / satın almaya iletmek için)", AlisverisKopyala_Click);
            btnHakkinda = AracDugmesi("Hakkında", "Program ve komut satırı seçenekleri hakkında", Hakkinda_Click);
            btnHakkinda.Alignment = ToolStripItemAlignment.Right;
            arac.Items.Add(btnYenile);
            arac.Items.Add(new ToolStripSeparator());
            arac.Items.Add(btnKaydet);
            arac.Items.Add(btnTarayici);
            arac.Items.Add(new ToolStripSeparator());
            arac.Items.Add(btnMetin);
            arac.Items.Add(btnAlisveris);
            arac.Items.Add(btnHakkinda);

            // Durum çubuğu
            durumCubugu = new StatusStrip();
            durumCubugu.SizingGrip = true;
            durumEtiketi = new ToolStripStatusLabel("Okunuyor…");
            durumEtiketi.Spring = true;
            durumEtiketi.TextAlign = ContentAlignment.MiddleLeft;
            kaynakEtiketi = new ToolStripStatusLabel(smbiosDosyasi != null
                ? "Kaynak: SMBIOS dosyası (" + DosyaAdi(smbiosDosyasi) + ")"
                : "Kaynak: bu bilgisayar");
            durumCubugu.Items.Add(durumEtiketi);
            durumCubugu.Items.Add(kaynakEtiketi);

            // Sekmeler
            sekmeler = new TabControl();
            sekmeler.Dock = DockStyle.Fill;
            sekmeler.Padding = new Point(12, 5);
            sekmeler.SelectedIndexChanged += Sekmeler_SelectedIndexChanged;

            wbOzet = TarayiciSekmesi("Özet");
            wbModuller = TarayiciSekmesi("Modüller");
            wbYukseltme = TarayiciSekmesi("Yükseltme ve Satın Alma");
            sekKullanim = new TabPage("Kullanım (canlı)");
            sekKullanim.BackColor = Color.White;
            sekKullanim.Controls.Add(KullanimPaneliKur());
            sekmeler.TabPages.Add(sekKullanim);
            wbDikkat = TarayiciSekmesi("RAM Alırken Dikkat");
            wbYeni = TarayiciSekmesi("Yeni Bilgisayar");
            wbSozluk = TarayiciSekmesi("Sözlük ve SSS");

            // Yükleniyor katmanı (sekmelerin üstünde, veri gelince gizlenir)
            yukleniyorPaneli = new Panel();
            yukleniyorPaneli.Dock = DockStyle.Fill;
            yukleniyorPaneli.BackColor = Color.White;
            yukleniyorEtiketi = new Label();
            yukleniyorEtiketi.Dock = DockStyle.Fill;
            yukleniyorEtiketi.TextAlign = ContentAlignment.MiddleCenter;
            yukleniyorEtiketi.Font = new Font("Segoe UI", 14F);
            yukleniyorEtiketi.ForeColor = Color.FromArgb(60, 70, 80);
            yukleniyorEtiketi.Text = "Bellek bilgileri okunuyor…\n\nBu birkaç saniye sürebilir.";
            yukleniyorPaneli.Controls.Add(yukleniyorEtiketi);

            icerik = new Panel();
            icerik.Dock = DockStyle.Fill;
            icerik.Controls.Add(sekmeler);
            icerik.Controls.Add(yukleniyorPaneli);
            yukleniyorPaneli.BringToFront();

            Controls.Add(icerik);
            Controls.Add(arac);
            Controls.Add(durumCubugu);
            icerik.BringToFront();

            // Arka plan veri toplama
            isci = new BackgroundWorker();
            isci.DoWork += Isci_DoWork;
            isci.RunWorkerCompleted += Isci_RunWorkerCompleted;

            // Canlı kullanım zamanlayıcısı
            kullanimZamanlayici = new System.Windows.Forms.Timer();
            kullanimZamanlayici.Interval = 2000;
            kullanimZamanlayici.Tick += KullanimZamanlayici_Tick;

            DugmeleriEtkinlestir(false);
        }

        ToolStripButton AracDugmesi(string metin, string ipucuMetni, EventHandler tikla)
        {
            ToolStripButton b = new ToolStripButton(metin);
            b.DisplayStyle = ToolStripItemDisplayStyle.Text;
            b.ToolTipText = ipucuMetni;
            b.Padding = new Padding(4, 1, 4, 1);
            b.Click += tikla;
            return b;
        }

        WebBrowser TarayiciSekmesi(string baslik)
        {
            TabPage sayfa = new TabPage(baslik);
            sayfa.BackColor = Color.White;
            WebBrowser wb = new WebBrowser();
            wb.Dock = DockStyle.Fill;
            wb.ScriptErrorsSuppressed = true;
            wb.IsWebBrowserContextMenuEnabled = false;
            wb.AllowWebBrowserDrop = false;
            wb.WebBrowserShortcutsEnabled = true; // Ctrl+C ile seçili metni kopyalayabilsin
            wb.Navigating += Tarayici_Navigating;
            wb.NewWindow += Tarayici_NewWindow;
            wb.DocumentCompleted += Tarayici_DocumentCompleted;
            sayfa.Controls.Add(wb);
            sekmeler.TabPages.Add(sayfa);
            tarayicilar.Add(wb);
            belgeBekleniyor[wb] = false;
            return wb;
        }

        Control KullanimPaneliKur()
        {
            TableLayoutPanel tl = new TableLayoutPanel();
            tl.Dock = DockStyle.Fill;
            tl.ColumnCount = 1;
            tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tl.Padding = new Padding(14, 10, 14, 10);
            tl.BackColor = Color.White;

            Font kalin = new Font("Segoe UI", 9F, FontStyle.Bold);
            Font baslikFont = new Font("Segoe UI", 13F, FontStyle.Bold);

            Label baslik = YeniEtiket("Canlı bellek kullanımı", baslikFont, Color.FromArgb(25, 45, 70));
            Label alt = YeniEtiket("Her 2 saniyede bir güncellenir. Pencere açık kaldıkça bu oturumdaki en yüksek (zirve) kullanım da kaydedilir.",
                null, Color.FromArgb(100, 110, 120));

            Label cubukBaslik1 = YeniEtiket("Fiziksel bellek (RAM)", kalin, Color.FromArgb(40, 40, 40));
            cubukKullanim = new KullanimCubugu();
            cubukKullanim.Dock = DockStyle.Fill;
            cubukKullanim.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            cubukKullanim.Margin = new Padding(3, 2, 3, 8);

            Label cubukBaslik2 = YeniEtiket("Taahhüt edilen bellek (programların ayırdığı; RAM + sayfa dosyası sınırına göre)", kalin, Color.FromArgb(40, 40, 40));
            cubukTaahhut = new KullanimCubugu();
            cubukTaahhut.Dock = DockStyle.Fill;
            cubukTaahhut.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            cubukTaahhut.Margin = new Padding(3, 2, 3, 8);

            // Ayrıntı ızgarası: 4 sütun (etiket, değer, etiket, değer)
            TableLayoutPanel izgara = new TableLayoutPanel();
            izgara.AutoSize = true;
            izgara.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            izgara.Dock = DockStyle.Top;
            izgara.ColumnCount = 4;
            izgara.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            izgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            izgara.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            izgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            izgara.Margin = new Padding(3, 0, 3, 6);
            lToplam = IzgaraSatiri(izgara, 0, 0, "Toplam (Windows'un gördüğü):", kalin);
            lKullanilan = IzgaraSatiri(izgara, 1, 0, "Kullanılan:", kalin);
            lBos = IzgaraSatiri(izgara, 2, 0, "Kullanılabilir (boş):", kalin);
            lOnbellek = IzgaraSatiri(izgara, 3, 0, "Önbellek (gerekirse boşaltılır):", kalin);
            lTaahhut = IzgaraSatiri(izgara, 0, 2, "Taahhüt / sınır:", kalin);
            lTaahhutOran = IzgaraSatiri(izgara, 1, 2, "Taahhüt / fiziksel RAM:", kalin);
            lSayfa = IzgaraSatiri(izgara, 2, 2, "Sayfa dosyası:", kalin);
            lSikistirilmis = IzgaraSatiri(izgara, 3, 2, "Sıkıştırılmış bellek:", kalin);

            // Zirve satırı
            FlowLayoutPanel zirveSatiri = new FlowLayoutPanel();
            zirveSatiri.AutoSize = true;
            zirveSatiri.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            zirveSatiri.WrapContents = false;
            zirveSatiri.Dock = DockStyle.Top;
            zirveSatiri.Margin = new Padding(0, 4, 0, 2);
            lZirve = YeniEtiket("Bu oturumdaki zirve: henüz ölçülmedi", kalin, Color.FromArgb(25, 45, 70));
            lZirve.Dock = DockStyle.None;
            lZirve.Anchor = AnchorStyles.Left;
            lZirve.Margin = new Padding(3, 7, 10, 3);
            Button btnSifirla = new Button();
            btnSifirla.Text = "Zirveyi sıfırla";
            btnSifirla.AutoSize = true;
            btnSifirla.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnSifirla.Padding = new Padding(6, 0, 6, 0);
            btnSifirla.Click += ZirveSifirla_Click;
            ipucu.SetToolTip(btnSifirla, "Zirve ölçümünü şimdi yeniden başlat (ör. ağır işe başlamadan hemen önce).");
            zirveSatiri.Controls.Add(lZirve);
            zirveSatiri.Controls.Add(btnSifirla);

            lYorum = YeniEtiket("Ölçüm bekleniyor…", new Font("Segoe UI", 10F, FontStyle.Bold), Color.FromArgb(60, 60, 60));
            lYorum.Margin = new Padding(3, 6, 3, 4);

            lAnaliz = YeniEtiket("", null, Color.FromArgb(50, 60, 70));
            lAnaliz.Visible = false;

            lKaynakNotu = YeniEtiket(
                "İpucu: Bu pencereyi en yoğun çalıştığınız anda (tüm programlarınız, tarayıcı sekmeleriniz açıkken) açık tutun; "
                + "zirve değer gerçek RAM ihtiyacınızı gösterir. Sadece açılıştaki anlık değere bakarak RAM almayın."
                + (smbiosDosyasi != null
                    ? "\nNot: Donanım bilgisi dosyadan okundu (" + DosyaAdi(smbiosDosyasi) + "); bu sekme ise ŞU AN çalıştığınız bilgisayarı gösterir."
                    : ""),
                new Font("Segoe UI", 9F, FontStyle.Italic), Color.FromArgb(95, 105, 115));
            lKaynakNotu.Margin = new Padding(3, 2, 3, 8);

            Label surecBaslik = YeniEtiket("En çok bellek kullanan programlar", kalin, Color.FromArgb(40, 40, 40));

            lvSurecler = new ListView();
            lvSurecler.View = View.Details;
            lvSurecler.FullRowSelect = true;
            lvSurecler.GridLines = true;
            lvSurecler.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lvSurecler.MultiSelect = false;
            lvSurecler.HideSelection = true;
            lvSurecler.Dock = DockStyle.Fill;
            lvSurecler.Columns.Add("Program", Olc(280));
            lvSurecler.Columns.Add("Adet", Olc(60), HorizontalAlignment.Right);
            lvSurecler.Columns.Add("Özel bellek", Olc(130), HorizontalAlignment.Right);
            lvSurecler.Columns.Add("Çalışma kümesi", Olc(140), HorizontalAlignment.Right);
            ipucu.SetToolTip(lvSurecler, "Özel bellek: programın kendisine ayırdığı bellek. Çalışma kümesi: şu an RAM'de duran kısmı.\n"
                + "Aynı adlı süreçler (ör. tarayıcının sekmeleri) tek satırda toplanır.");

            int satir = 0;
            EkleSatir(tl, baslik, ref satir, SizeType.AutoSize, 0);
            EkleSatir(tl, alt, ref satir, SizeType.AutoSize, 0);
            EkleSatir(tl, cubukBaslik1, ref satir, SizeType.AutoSize, 0);
            EkleSatir(tl, cubukKullanim, ref satir, SizeType.Absolute, 40);
            EkleSatir(tl, cubukBaslik2, ref satir, SizeType.AutoSize, 0);
            EkleSatir(tl, cubukTaahhut, ref satir, SizeType.Absolute, 30);
            EkleSatir(tl, izgara, ref satir, SizeType.AutoSize, 0);
            EkleSatir(tl, zirveSatiri, ref satir, SizeType.AutoSize, 0);
            EkleSatir(tl, lYorum, ref satir, SizeType.AutoSize, 0);
            EkleSatir(tl, lAnaliz, ref satir, SizeType.AutoSize, 0);
            EkleSatir(tl, lKaynakNotu, ref satir, SizeType.AutoSize, 0);
            EkleSatir(tl, surecBaslik, ref satir, SizeType.AutoSize, 0);
            EkleSatir(tl, lvSurecler, ref satir, SizeType.Percent, 100);
            return tl;
        }

        static void EkleSatir(TableLayoutPanel tl, Control c, ref int satir, SizeType tur, float boy)
        {
            tl.RowCount = satir + 1;
            tl.RowStyles.Add(tur == SizeType.AutoSize ? new RowStyle(SizeType.AutoSize) : new RowStyle(tur, boy));
            tl.Controls.Add(c, 0, satir);
            satir++;
        }

        static Label YeniEtiket(string metin, Font font, Color renk)
        {
            Label l = new Label();
            l.Text = metin;
            l.AutoSize = true;
            l.Dock = DockStyle.Fill; // TableLayoutPanel içinde sütun genişliğinde satır kaydırır
            if (font != null) l.Font = font;
            l.ForeColor = renk;
            l.Margin = new Padding(3, 3, 3, 3);
            return l;
        }

        static Label IzgaraSatiri(TableLayoutPanel izgara, int satir, int sutun, string baslik, Font kalin)
        {
            while (izgara.RowCount <= satir)
            {
                izgara.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                izgara.RowCount = izgara.RowStyles.Count;
            }
            Label b = new Label();
            b.Text = baslik;
            b.AutoSize = true;
            b.ForeColor = Color.FromArgb(90, 100, 110);
            b.Margin = new Padding(3, 3, 6, 3);
            Label d = new Label();
            d.Text = "—";
            d.AutoSize = true;
            d.Font = kalin;
            d.ForeColor = Color.FromArgb(30, 30, 30);
            d.Margin = new Padding(3, 3, 18, 3);
            izgara.Controls.Add(b, sutun, satir);
            izgara.Controls.Add(d, sutun + 1, satir);
            return d;
        }

        int Olc(int px)
        {
            return (int)Math.Round(px * olcek);
        }

        // ---- Form olayları -------------------------------------------------

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try
            {
                Rectangle alan = Screen.FromControl(this).WorkingArea;
                if (ekranKlasoru != null)
                {
                    StartPosition = FormStartPosition.Manual;
                    Rectangle hedef = new Rectangle(alan.Left + 10, alan.Top + 10,
                        Math.Min(Width, alan.Width - 20), Math.Min(Height, alan.Height - 20));
                    Bounds = hedef;
                    TopMost = true;
                }
                else if (Width > alan.Width || Height > alan.Height)
                {
                    Bounds = new Rectangle(alan.Left, alan.Top, Math.Min(Width, alan.Width), Math.Min(Height, alan.Height));
                }
            }
            catch (Exception ex) { Program.HataKaydet(ex, "OnLoad konum"); }

            try
            {
                // WebBrowser'ların ActiveX denetimleri belge atanmadan önce oluşsun
                foreach (WebBrowser wb in tarayicilar)
                {
                    IntPtr h = wb.Handle;
                    if (h == IntPtr.Zero) { }
                }
            }
            catch (Exception ex) { Program.HataKaydet(ex, "OnLoad tarayıcı"); }

            VeriYukle();
            izlemeBasi = DateTime.Now;
            kullanimZamanlayici.Start();
            KullanimZamanlayici_Tick(this, EventArgs.Empty);

            if (ekranKlasoru != null)
            {
                ekranBaslangic = DateTime.Now;
                ekranZamanlayici = new System.Windows.Forms.Timer();
                ekranZamanlayici.Interval = 250;
                ekranZamanlayici.Tick += EkranZamanlayici_Tick;
                ekranZamanlayici.Start();
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (ekranKlasoru != null)
            {
                try { Activate(); BringToFront(); }
                catch { }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                kullanimZamanlayici.Stop();
                if (ekranZamanlayici != null) ekranZamanlayici.Stop();
            }
            catch { }
            base.OnFormClosing(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            try
            {
                if (keyData == Keys.F5) { Yenile_Click(this, EventArgs.Empty); return true; }
                if (keyData == (Keys.Control | Keys.S)) { Kaydet_Click(this, EventArgs.Empty); return true; }
            }
            catch (Exception ex) { HataBildir("Kısayol tuşu işlenemedi.", ex); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ---- Veri toplama ----------------------------------------------------

        class YuklemeSonucu
        {
            public Envanter Envanter;
            public DanismanSonucu Sonuc;
            public Exception Hata;
            public double Saniye;
        }

        void VeriYukle()
        {
            if (isci.IsBusy) return;
            DugmeleriEtkinlestir(false);
            btnYenile.Enabled = false;
            durumEtiketi.Text = veriYuklendi ? "Yenileniyor…" : "Bellek bilgileri okunuyor…";
            UseWaitCursor = true;
            isci.RunWorkerAsync();
        }

        void Isci_DoWork(object sender, DoWorkEventArgs e)
        {
            YuklemeSonucu r = new YuklemeSonucu();
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                r.Envanter = Program.EnvanterTopla(smbiosTablo, smbiosMajor, smbiosMinor);
            }
            catch (Exception ex)
            {
                r.Hata = ex;
                Program.HataKaydet(ex, "Toplayici.Topla");
            }
            if (r.Envanter != null)
            {
                try
                {
                    r.Sonuc = Danisman.Degerlendir(r.Envanter);
                }
                catch (Exception ex)
                {
                    r.Hata = ex;
                    Program.HataKaydet(ex, "Danisman.Degerlendir");
                }
                if (r.Sonuc == null)
                {
                    r.Sonuc = new DanismanSonucu();
                    r.Sonuc.OzetCumle = "Değerlendirme yapılamadı" + (r.Hata != null ? ": " + r.Hata.Message : ".");
                }
            }
            r.Saniye = sw.Elapsed.TotalSeconds;
            e.Result = r;
        }

        void Isci_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                UseWaitCursor = false;
                btnYenile.Enabled = true;
                YuklemeSonucu r = e.Error == null ? e.Result as YuklemeSonucu : null;
                if (r == null)
                {
                    r = new YuklemeSonucu();
                    r.Hata = e.Error;
                    if (e.Error != null) Program.HataKaydet(e.Error, "BackgroundWorker");
                }
                VeriyiGoster(r);
            }
            catch (Exception ex)
            {
                HataBildir("Okunan bilgiler gösterilemedi.", ex);
            }
        }

        void VeriyiGoster(YuklemeSonucu r)
        {
            envanter = r.Envanter;
            sonuc = r.Sonuc;
            veriYuklendi = true;

            if (envanter != null)
            {
                Text = Program.ProgramAdi + " — " + ModelAdi(envanter);
                DugmeleriEtkinlestir(true);
            }
            else
            {
                Text = Program.ProgramAdi;
                DugmeleriEtkinlestir(false);
            }

            SayfalariDoldur(r.Hata);
            AnaliziGoster();

            yukleniyorPaneli.Visible = false;
            durumEtiketi.Text = DurumMetni(r);
        }

        string DurumMetni(YuklemeSonucu r)
        {
            if (envanter == null)
                return "Bellek bilgileri okunamadı" + (r.Hata != null ? ": " + r.Hata.Message : ".")
                    + " Ayrıntı: " + Program.HataDosyasi;
            List<string> p = new List<string>();
            long toplam = envanter.ToplamTakiliMB;
            if (toplam <= 0) toplam = envanter.Sistem.TakiliRamMB;
            p.Add(Bicim.Boyut(toplam) + " takılı");
            if (sonuc != null && sonuc.ToplamYuva > 0)
                p.Add((sonuc.ToplamYuva - sonuc.BosYuva) + "/" + sonuc.ToplamYuva + " yuva dolu"
                    + (sonuc.YuvaBilgisiGuvenilir ? "" : " (yuva sayısı kesin değil)"));
            BellekYuvasi ilk = envanter.DoluYuvalar.Count > 0 ? envanter.DoluYuvalar[0] : null;
            if (ilk != null)
            {
                string t = (ilk.Tur + " " + ilk.FormFaktor).Trim();
                if (t.Length > 0) p.Add(t);
            }
            p.Add(r.Saniye.ToString("0.0", tr) + " sn'de okundu (" + DateTime.Now.ToString("HH:mm", tr) + ")");
            if (envanter.ToplamaNotlari.Count > 0) p.Add(envanter.ToplamaNotlari.Count + " toplama notu (Modüller sekmesinin sonunda)");
            return string.Join(" · ", p.ToArray());
        }

        static string ModelAdi(Envanter e)
        {
            SistemBilgisi s = e.Sistem ?? new SistemBilgisi();
            string ad = Bos(s.ModelAdi) ? s.Model : s.ModelAdi;
            if (Bos(ad)) ad = s.AnakartModel;
            if (Bos(ad)) ad = Environment.MachineName;
            ad = ad.Trim();
            if (!Bos(s.Uretici) && ad.IndexOf(s.Uretici.Trim(), StringComparison.OrdinalIgnoreCase) < 0
                && !BilinmeyenMetin(s.Uretici))
                ad = s.Uretici.Trim() + " " + ad;
            return ad;
        }

        static bool BilinmeyenMetin(string s)
        {
            string u = (s ?? "").Trim().ToUpperInvariant();
            return u.Length == 0 || u == "SYSTEM MANUFACTURER" || u == "TO BE FILLED BY O.E.M." || u == "DEFAULT STRING"
                || u == "O.E.M." || u == "OEM";
        }

        static bool Bos(string s)
        {
            return s == null || s.Trim().Length == 0;
        }

        void SayfalariDoldur(Exception hata)
        {
            Envanter e = envanter;
            DanismanSonucu d = sonuc ?? new DanismanSonucu();
            if (e != null)
            {
                BelgeKoy(wbOzet, "Özet", delegate { return Rapor.OzetHtml(e, d); });
                BelgeKoy(wbModuller, "Modüller", delegate { return Rapor.ModullerHtml(e); });
                BelgeKoy(wbYukseltme, "Yükseltme ve Satın Alma", delegate { return Rapor.YukseltmeHtml(e, d); });
            }
            else
            {
                string govde = "<div class=\"kart kritik\"><h2>Bellek bilgileri okunamadı</h2><p>"
                    + Html.Ka(hata != null ? hata.Message : "Bilinmeyen hata")
                    + "</p><p class=\"not\">Ayrıntılar şu dosyaya yazıldı: " + Html.Ka(Program.HataDosyasi)
                    + "</p><p>Araç çubuğundaki <b>Yenile</b> ile tekrar deneyebilirsiniz.</p></div>";
                BelgeKoy(wbOzet, "Özet", delegate { return govde; });
                BelgeKoy(wbModuller, "Modüller", delegate { return govde; });
                BelgeKoy(wbYukseltme, "Yükseltme ve Satın Alma", delegate { return govde; });
            }
            BelgeKoy(wbDikkat, "RAM Alırken Dikkat", delegate { return Rehber.SatinAlmaRehberiHtml(); });
            BelgeKoy(wbYeni, "Yeni Bilgisayar", delegate { return Rehber.YeniBilgisayarRehberiHtml(); });
            BelgeKoy(wbSozluk, "Sözlük ve SSS", delegate { return Rehber.SozlukHtml() + Rehber.SssHtml(); });
        }

        delegate string GovdeUretici();

        void BelgeKoy(WebBrowser wb, string baslik, GovdeUretici uret)
        {
            string html;
            try
            {
                html = Rapor.Sayfa(baslik, uret(), olcek);
            }
            catch (Exception ex)
            {
                Program.HataKaydet(ex, "Sayfa oluşturma: " + baslik);
                html = YedekSayfa(baslik, "<h2>Bu bölüm oluşturulamadı</h2><p>" + Html.Ka(ex.Message)
                    + "</p><p style=\"color:#777\">Ayrıntılar: " + Html.Ka(Program.HataDosyasi) + "</p>");
            }
            try
            {
                belgeBekleniyor[wb] = true;
                wb.DocumentText = html;
            }
            catch (Exception ex)
            {
                belgeBekleniyor[wb] = false;
                Program.HataKaydet(ex, "WebBrowser.DocumentText: " + baslik);
            }
        }

        static string YedekSayfa(string baslik, string govde)
        {
            return "<!DOCTYPE html><html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\"><meta charset=\"utf-8\">"
                + "<title>" + Html.Ka(baslik) + "</title></head>"
                + "<body style=\"font-family:'Segoe UI',Arial,sans-serif;font-size:14px;margin:20px;color:#222\">"
                + govde + "</body></html>";
        }

        void AnaliziGoster()
        {
            try
            {
                if (sonuc == null || sonuc.IhtiyacAnalizi == null || sonuc.IhtiyacAnalizi.Count == 0)
                {
                    lAnaliz.Visible = false;
                    return;
                }
                StringBuilder sb = new StringBuilder();
                sb.Append("Danışmanın değerlendirmesi (program açılırkenki ölçüme göre):");
                int n = 0;
                foreach (Bulgu b in sonuc.IhtiyacAnalizi)
                {
                    if (b == null) continue;
                    // Süreç listesi zaten aşağıdaki tabloda canlı gösteriliyor; burada tekrarlanmaz.
                    if ((b.Baslik ?? "").StartsWith("En çok bellek kullananlar", StringComparison.Ordinal)) continue;
                    if (++n > 5) break;
                    sb.Append("\n•  ").Append(b.Baslik);
                    string ac = (b.Aciklama ?? "").Replace("\r", "").Trim();
                    int satirSonu = ac.IndexOf('\n');
                    if (satirSonu > 0) ac = ac.Substring(0, satirSonu);
                    if (ac.Length > 0) sb.Append(" — ").Append(ac);
                }
                lAnaliz.Text = sb.ToString();
                lAnaliz.Visible = true;
            }
            catch (Exception ex)
            {
                Program.HataKaydet(ex, "AnaliziGoster");
                lAnaliz.Visible = false;
            }
        }

        void DugmeleriEtkinlestir(bool veriVar)
        {
            btnKaydet.Enabled = veriVar;
            btnTarayici.Enabled = veriVar;
            btnMetin.Enabled = veriVar;
            btnAlisveris.Enabled = veriVar;
        }

        // ---- WebBrowser olayları -------------------------------------------

        void Tarayici_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            try
            {
                Uri u = e.Url;
                if (u == null) return;
                string adres = u.OriginalString ?? u.ToString();
                if (adres.Equals("about:blank", StringComparison.OrdinalIgnoreCase)
                    || adres.StartsWith("about:blank#", StringComparison.OrdinalIgnoreCase))
                    return; // kendi belgemiz ve sayfa içi çapa bağlantıları
                e.Cancel = true;
                DisAdresAc(u);
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                Program.HataKaydet(ex, "Navigating");
            }
        }

        void Tarayici_NewWindow(object sender, CancelEventArgs e)
        {
            e.Cancel = true; // yeni IE penceresi açılmasın
            try
            {
                WebBrowser wb = sender as WebBrowser;
                string adres = wb != null ? wb.StatusText : null;
                Uri u;
                if (!string.IsNullOrEmpty(adres) && Uri.TryCreate(adres, UriKind.Absolute, out u)) DisAdresAc(u);
            }
            catch (Exception ex) { Program.HataKaydet(ex, "NewWindow"); }
        }

        void DisAdresAc(Uri u)
        {
            if (u == null) return;
            if (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps) return;
            if (ekranKlasoru != null) return;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(u.AbsoluteUri);
                psi.UseShellExecute = true;
                Process.Start(psi);
                durumEtiketi.Text = "Bağlantı tarayıcıda açıldı: " + u.Host;
            }
            catch (Exception ex)
            {
                HataBildir("Bağlantı açılamadı: " + u.AbsoluteUri, ex);
            }
        }

        void Tarayici_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser wb = sender as WebBrowser;
            if (wb != null && belgeBekleniyor.ContainsKey(wb)) belgeBekleniyor[wb] = false;
        }

        void Sekmeler_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (sekmeler.SelectedTab == sekKullanim && sonKullanim != null) SurecleriGuncelle(sonKullanim);
            }
            catch (Exception ex) { Program.HataKaydet(ex, "Sekme değişimi"); }
        }

        // ---- Araç çubuğu -----------------------------------------------------

        void Yenile_Click(object sender, EventArgs e)
        {
            try { VeriYukle(); }
            catch (Exception ex) { HataBildir("Bilgiler yeniden okunamadı.", ex); }
        }

        void Kaydet_Click(object sender, EventArgs e)
        {
            if (envanter == null) return;
            try
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Raporu kaydet";
                    sfd.Filter = "HTML raporu (*.html)|*.html|Tüm dosyalar (*.*)|*.*";
                    sfd.DefaultExt = "html";
                    sfd.AddExtension = true;
                    sfd.OverwritePrompt = true;
                    sfd.FileName = VarsayilanRaporAdi();
                    try { sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); }
                    catch { }
                    if (sfd.ShowDialog(this) != DialogResult.OK) return;

                    File.WriteAllText(sfd.FileName, TamRapor(), new UTF8Encoding(true));
                    durumEtiketi.Text = "Rapor kaydedildi: " + sfd.FileName;
                    if (MessageBox.Show(this, "Rapor kaydedildi:\n" + sfd.FileName + "\n\nŞimdi tarayıcıda açılsın mı?",
                            Program.ProgramAdi, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        try { DosyaAc(sfd.FileName); }
                        catch (Exception ex2) { HataBildir("Rapor kaydedildi (" + sfd.FileName + ") ancak tarayıcıda açılamadı.", ex2); }
                    }
                }
            }
            catch (Exception ex)
            {
                HataBildir("Rapor kaydedilemedi.", ex);
            }
        }

        void Tarayici_Click(object sender, EventArgs e)
        {
            if (envanter == null) return;
            try
            {
                string yol = Path.Combine(Path.GetTempPath(), "RAM-Raporu-" + GuvenliAd(Environment.MachineName) + ".html");
                File.WriteAllText(yol, TamRapor(), new UTF8Encoding(true));
                DosyaAc(yol);
                durumEtiketi.Text = "Rapor tarayıcıda açıldı (" + yol + ")";
            }
            catch (Exception ex)
            {
                HataBildir("Rapor tarayıcıda açılamadı.", ex);
            }
        }

        void MetinKopyala_Click(object sender, EventArgs e)
        {
            if (envanter == null) return;
            try
            {
                if (PanoyaKopyala(Rapor.Metin(envanter, sonuc ?? new DanismanSonucu())))
                    Bildir(btnMetin, "Rapor metni panoya kopyalandı.");
            }
            catch (Exception ex)
            {
                HataBildir("Rapor metni oluşturulamadı.", ex);
            }
        }

        void AlisverisKopyala_Click(object sender, EventArgs e)
        {
            if (envanter == null) return;
            try
            {
                string metin = Rapor.AlisverisMetni(sonuc ?? new DanismanSonucu());
                if (Bos(metin))
                {
                    Program.MesajGoster(this, "Bu bilgisayar için kesin bir satın alma önerisi oluşturulamadı.\n"
                        + "Ayrıntılar için \"Yükseltme ve Satın Alma\" sekmesine bakın.", MessageBoxIcon.Information);
                    return;
                }
                if (PanoyaKopyala(metin))
                    Bildir(btnAlisveris, "Kopyalandı — mağazaya ya da satın alma birimine yapıştırabilirsiniz.");
            }
            catch (Exception ex)
            {
                HataBildir("Alışveriş metni oluşturulamadı.", ex);
            }
        }

        void Hakkinda_Click(object sender, EventArgs e)
        {
            try
            {
                string metin = Program.ProgramAdi + " " + Program.Surum + "\n\n"
                    + "Bilgisayardaki RAM modüllerini (dolu ve boş yuvalarıyla) okur; bu bilgisayara hangi RAM'in "
                    + "alınacağını, nasıl yükseltileceğini ve RAM alırken nelere dikkat edileceğini anlatır.\n\n"
                    + "• Kurulum gerektirmez, yönetici izni gerektirmez.\n"
                    + "• İnternete bağlanmaz, hiçbir veri göndermez.\n"
                    + "  (Rapordaki bağlantılar yalnızca siz tıklarsanız tarayıcınızda açılır.)\n"
                    + "• Sadece Windows'un yerleşik bileşenlerini kullanır.\n\n"
                    + "Komut satırı seçenekleri:\n"
                    + "  --metin                  düz metin rapor\n"
                    + "  --html <dosya>           HTML rapor kaydet\n"
                    + "  --csv <dosya>            CSV'ye satır ekle (toplu envanter)\n"
                    + "  --smbios <dosya>         kayıtlı SMBIOS dosyasını kullan\n"
                    + "  --smbios-kaydet <dosya>  SMBIOS tablosunu kaydet (destek için)\n"
                    + "  --test                   gömülü testler\n"
                    + "  --ekran <klasör>         sekmelerin ekran görüntülerini kaydet\n"
                    + "  --yardim                 ayrıntılı yardım\n\n"
                    + "Hata kaydı: " + Program.HataDosyasi;
                MessageBox.Show(this, metin, "Hakkında — " + Program.ProgramAdi, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { Program.HataKaydet(ex, "Hakkında"); }
        }

        string TamRapor()
        {
            return Rapor.TamRaporHtml(envanter, sonuc ?? new DanismanSonucu());
        }

        static string VarsayilanRaporAdi()
        {
            return "RAM-Raporu-" + GuvenliAd(Environment.MachineName) + "-" + DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) + ".html";
        }

        static string GuvenliAd(string ad)
        {
            if (Bos(ad)) return "PC";
            StringBuilder sb = new StringBuilder();
            char[] yasak = Path.GetInvalidFileNameChars();
            foreach (char c in ad.Trim())
                sb.Append(Array.IndexOf(yasak, c) >= 0 ? '_' : c);
            return sb.ToString();
        }

        static string DosyaAdi(string yol)
        {
            try { return Path.GetFileName(yol); }
            catch { return yol; }
        }

        void DosyaAc(string yol)
        {
            ProcessStartInfo psi = new ProcessStartInfo(yol);
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        bool PanoyaKopyala(string metin)
        {
            if (Bos(metin)) return false;
            try
            {
                Clipboard.SetDataObject(metin, true, 10, 100);
                return true;
            }
            catch (Exception ex)
            {
                Program.HataKaydet(ex, "Pano");
                Program.MesajGoster(this, "Panoya kopyalanamadı; pano şu an başka bir program tarafından kullanılıyor olabilir. "
                    + "Biraz sonra tekrar deneyin.\n\n(" + ex.Message + ")", MessageBoxIcon.Warning);
                return false;
            }
        }

        void Bildir(ToolStripItem oge, string metin)
        {
            durumEtiketi.Text = metin;
            try
            {
                Rectangle r = oge.Bounds;
                ipucu.Show(metin, arac, r.Left, r.Bottom + Olc(4), 2200);
            }
            catch { }
        }

        void HataBildir(string ozet, Exception ex)
        {
            Program.HataKaydet(ex, ozet);
            try { durumEtiketi.Text = ozet; }
            catch { }
            Program.MesajGoster(this, ozet + "\n\n" + (ex != null ? ex.Message : "")
                + "\n\nAyrıntılar şu dosyaya yazıldı:\n" + Program.HataDosyasi, MessageBoxIcon.Error);
        }

        // ---- Canlı kullanım ------------------------------------------------

        void KullanimZamanlayici_Tick(object sender, EventArgs e)
        {
            if (kullanimOkunuyor) return;
            kullanimOkunuyor = true;
            try
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    KullanimBilgisi k = null;
                    Exception hata = null;
                    try { k = Toplayici.KullanimOku(); }
                    catch (Exception ex) { hata = ex; }
                    try
                    {
                        if (IsDisposed || !IsHandleCreated) { kullanimOkunuyor = false; return; }
                        KullanimBilgisi kk = k;
                        Exception hh = hata;
                        BeginInvoke((MethodInvoker)delegate { KullanimiIsle(kk, hh); });
                    }
                    catch { kullanimOkunuyor = false; }
                });
            }
            catch (Exception ex)
            {
                kullanimOkunuyor = false;
                Program.HataKaydet(ex, "Kullanım zamanlayıcısı");
            }
        }

        void KullanimiIsle(KullanimBilgisi k, Exception hata)
        {
            kullanimOkunuyor = false;
            try
            {
                if (IsDisposed) return;
                if (hata != null && !kullanimHatasiKaydedildi)
                {
                    kullanimHatasiKaydedildi = true;
                    Program.HataKaydet(hata, "Toplayici.KullanimOku");
                }
                if (k == null || k.ToplamMB <= 0)
                {
                    lYorum.Text = "Bellek kullanımı okunamadı" + (hata != null ? ": " + hata.Message : ".");
                    lYorum.ForeColor = Color.FromArgb(150, 60, 0);
                    return;
                }
                sonKullanim = k;

                int yuzde = k.YuzdeKullanim;
                if (yuzde <= 0 && k.ToplamMB > 0) yuzde = (int)Math.Round(100.0 * k.KullanilanMB / k.ToplamMB);
                if (yuzde > zirveYuzde) { zirveYuzde = yuzde; zirveZaman = DateTime.Now; }
                if (k.KullanilanMB > zirveKullanilanMB) zirveKullanilanMB = k.KullanilanMB;
                if (k.TaahhutMB > zirveTaahhutMB) zirveTaahhutMB = k.TaahhutMB;

                // Çubuklar
                cubukKullanim.Oran = yuzde / 100.0;
                cubukKullanim.ZirveOran = zirveYuzde / 100.0;
                cubukKullanim.Metin = Bicim.Boyut(k.KullanilanMB) + " / " + Bicim.Boyut(k.ToplamMB) + " kullanılıyor  (%" + yuzde + ")";
                cubukKullanim.Invalidate();

                if (k.TaahhutSiniriMB > 0)
                {
                    double o = (double)k.TaahhutMB / k.TaahhutSiniriMB;
                    cubukTaahhut.Oran = o;
                    cubukTaahhut.ZirveOran = (double)zirveTaahhutMB / k.TaahhutSiniriMB;
                    cubukTaahhut.Metin = Bicim.Boyut(k.TaahhutMB) + " / " + Bicim.Boyut(k.TaahhutSiniriMB) + "  (%" + (int)Math.Round(o * 100) + ")";
                }
                else
                {
                    cubukTaahhut.Oran = 0;
                    cubukTaahhut.ZirveOran = -1;
                    cubukTaahhut.Metin = "bilinmiyor";
                }
                cubukTaahhut.Invalidate();

                // Ayrıntılar
                lToplam.Text = Bicim.Boyut(k.ToplamMB);
                lKullanilan.Text = Bicim.Boyut(k.KullanilanMB) + "  (%" + yuzde + ")";
                lBos.Text = Bicim.Boyut(k.BosMB);
                lOnbellek.Text = k.OnbellekMB > 0 ? Bicim.Boyut(k.OnbellekMB) : "bilinmiyor";
                lTaahhut.Text = k.TaahhutSiniriMB > 0 ? Bicim.Boyut(k.TaahhutMB) + " / " + Bicim.Boyut(k.TaahhutSiniriMB) : Bicim.Boyut(k.TaahhutMB);
                double taahhutOran = k.ToplamMB > 0 ? (double)k.TaahhutMB / k.ToplamMB : 0;
                lTaahhutOran.Text = "%" + (int)Math.Round(taahhutOran * 100)
                    + (taahhutOran > 1.0 ? "  (RAM'den fazla — fazlası sayfa dosyasına taşar)" : "");
                lSayfa.Text = k.SayfaDosyasiMB > 0
                    ? Bicim.Boyut(k.SayfaDosyasiKullanilanMB) + " kullanılıyor / " + Bicim.Boyut(k.SayfaDosyasiMB)
                    : "yok ya da bilinmiyor";
                lSikistirilmis.Text = k.SikistirilmisMB > 0 ? Bicim.Boyut(k.SikistirilmisMB) : "bilinmiyor";

                // Zirve
                TimeSpan sure = DateTime.Now - izlemeBasi;
                double zirveTaahhutOran = k.ToplamMB > 0 ? (double)zirveTaahhutMB / k.ToplamMB : 0;
                lZirve.Text = "Bu oturumdaki zirve: kullanım %" + zirveYuzde + " (" + Bicim.Boyut(zirveKullanilanMB) + ", saat "
                    + zirveZaman.ToString("HH:mm:ss", tr) + ") · taahhüt " + Bicim.Boyut(zirveTaahhutMB)
                    + " (fiziksel RAM'e oranı %" + (int)Math.Round(zirveTaahhutOran * 100) + ") · izleme süresi " + SureMetni(sure);

                Color renk;
                lYorum.Text = Yorum(zirveYuzde, zirveTaahhutOran, sure, out renk);
                lYorum.ForeColor = renk;

                if (sekmeler.SelectedTab == sekKullanim && WindowState != FormWindowState.Minimized)
                    SurecleriGuncelle(k);
            }
            catch (Exception ex)
            {
                Program.HataKaydet(ex, "KullanimiIsle");
            }
        }

        /// <summary>Zirve kullanıma göre kısa, anlaşılır yorum.</summary>
        public static string Yorum(int zirveYuzde, double zirveTaahhutOran, TimeSpan sure, out Color renk)
        {
            // Eşikler Danisman.IhtiyacAnalizi ile aynı: kullanım ≥%90 ya da taahhüt ≥ RAM → yetmiyor;
            // kullanım ≥%75 ya da taahhüt > RAM'in %90'ı → sınırda.
            string m;
            string bas = "Zirve %" + zirveYuzde;
            if (zirveTaahhutOran > 0.9) bas += ", taahhüt/RAM %" + (int)Math.Round(zirveTaahhutOran * 100);
            if (zirveYuzde >= 90 || zirveTaahhutOran >= 1.0 || (zirveTaahhutOran > 0.9 && zirveYuzde >= 80))
            {
                renk = Color.FromArgb(190, 30, 30);
                m = bas + " — RAM yetmiyor. Windows belleğin bir kısmını diske (sayfa dosyasına) taşıyor; "
                    + "bu yavaşlama demektir. RAM yükseltmesi belirgin fark yaratır.";
            }
            else if (zirveYuzde >= 75 || zirveTaahhutOran > 0.9)
            {
                renk = Color.FromArgb(200, 110, 0);
                m = bas + " — sınırda. Ağır işlerde yavaşlama başlayabilir; yükseltme faydalı olur.";
            }
            else if (zirveYuzde >= 60)
            {
                renk = Color.FromArgb(30, 120, 50);
                m = "Zirve %" + zirveYuzde + " — şimdilik yeterli. Daha ağır işler yapacaksanız yükseltmeyi düşünebilirsiniz.";
            }
            else
            {
                renk = Color.FromArgb(30, 120, 50);
                m = "Zirve %" + zirveYuzde + " — RAM rahat yetiyor; bu kullanımda yükseltme hız kazandırmaz.";
            }
            if (sure.TotalMinutes < 5)
                m += "\n(Henüz " + SureMetni(sure) + " ölçüldü — en yoğun çalıştığınız zamanlarda açık bırakırsanız sonuç daha doğru olur.)";
            return m;
        }

        static string SureMetni(TimeSpan t)
        {
            if (t.TotalSeconds < 60) return Math.Max(0, (int)t.TotalSeconds) + " sn";
            if (t.TotalMinutes < 60) return (int)t.TotalMinutes + " dk " + t.Seconds + " sn";
            return (int)t.TotalHours + " sa " + t.Minutes + " dk";
        }

        void SurecleriGuncelle(KullanimBilgisi k)
        {
            if (k == null || k.EnCokKullananlar == null) return;
            lvSurecler.BeginUpdate();
            try
            {
                lvSurecler.Items.Clear();
                foreach (SurecBellek s in k.EnCokKullananlar)
                {
                    if (s == null) continue;
                    ListViewItem it = new ListViewItem(Bos(s.Ad) ? "(adsız)" : s.Ad);
                    it.SubItems.Add(s.Adet.ToString(tr));
                    it.SubItems.Add(Bicim.Boyut(s.OzelMB));
                    it.SubItems.Add(Bicim.Boyut(s.CalismaKumesiMB));
                    lvSurecler.Items.Add(it);
                }
            }
            finally
            {
                lvSurecler.EndUpdate();
            }
        }

        void ZirveSifirla_Click(object sender, EventArgs e)
        {
            try
            {
                zirveYuzde = -1;
                zirveKullanilanMB = 0;
                zirveTaahhutMB = 0;
                izlemeBasi = DateTime.Now;
                lZirve.Text = "Bu oturumdaki zirve: sıfırlandı, ölçülüyor…";
                if (sonKullanim != null) KullanimiIsle(sonKullanim, null);
            }
            catch (Exception ex) { Program.HataKaydet(ex, "Zirve sıfırlama"); }
        }

        // ---- --ekran modu --------------------------------------------------

        void EkranZamanlayici_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!ekranYakalaniyor)
                {
                    bool zamanAsimi = (DateTime.Now - ekranBaslangic).TotalSeconds > 20;
                    if (!(veriYuklendi && TarayicilarHazir()) && !zamanAsimi) return;
                    if (zamanAsimi) Program.HataKaydet(null, "--ekran: 20 sn içinde tüm sayfalar yüklenmedi, yine de yakalanıyor");
                    ekranYakalaniyor = true;
                    ekranSekme = 0;
                    SekmeSecEkran(0);
                    return;
                }

                // Seçili sekme 700 ms'dir ekranda: yakala
                Yakala(ekranSekme);
                ekranSekme++;
                if (ekranSekme < sekmeler.TabCount)
                {
                    SekmeSecEkran(ekranSekme);
                    return;
                }

                ekranZamanlayici.Stop();
                SekmeListesiYaz();
                CikisKodu = ekranHata == 0 ? 0 : 1;
                TopMost = false;
                Close();
            }
            catch (Exception ex)
            {
                Program.HataKaydet(ex, "--ekran");
                try { ekranZamanlayici.Stop(); }
                catch { }
                CikisKodu = 1;
                Close();
            }
        }

        bool TarayicilarHazir()
        {
            foreach (WebBrowser wb in tarayicilar)
            {
                if (belgeBekleniyor[wb]) return false;
                if (wb.ReadyState != WebBrowserReadyState.Complete || wb.IsBusy) return false;
            }
            return true;
        }

        void SekmeSecEkran(int i)
        {
            sekmeler.SelectedIndex = i;
            try { Activate(); }
            catch { }
            Application.DoEvents();
            ekranZamanlayici.Interval = 700;
        }

        void Yakala(int i)
        {
            string yol = Path.Combine(ekranKlasoru, "sekme-" + (i + 1) + ".png");
            try
            {
                Refresh();
                Rectangle r = GorunurSinirlar();
                bool tamam = false;
                using (Bitmap bmp = new Bitmap(r.Width, r.Height, PixelFormat.Format32bppArgb))
                {
                    try
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                            g.CopyFromScreen(r.Location, Point.Empty, r.Size);
                        tamam = !TekRenk(bmp);
                    }
                    catch (Exception ex)
                    {
                        Program.HataKaydet(ex, "--ekran CopyFromScreen (PrintWindow deneniyor)");
                    }
                    if (tamam) bmp.Save(yol, ImageFormat.Png);
                }
                if (tamam) return;

                // Ekran kilitliyse ya da masaüstü görünmüyorsa CopyFromScreen siyah döndürür:
                // o zaman pencereye kendini çizdir (PrintWindow, WM_PRINT). Bayrak 0 bilerek seçildi:
                // PW_RENDERFULLCONTENT (2) DWM'nin son karesini verir ve ekran kapalıyken bayat kalır;
                // 0 ise eşzamanlı çizer (WebBrowser dahil), sadece pencere çerçevesi klasik görünür.
                Rectangle p = Bounds;
                using (Bitmap tam = new Bitmap(p.Width, p.Height, PixelFormat.Format24bppRgb))
                {
                    using (Graphics g = Graphics.FromImage(tam))
                    {
                        IntPtr hdc = g.GetHdc();
                        try { tamam = YerelYontemler.PrintWindow(Handle, hdc, 0); }
                        finally { g.ReleaseHdc(hdc); }
                    }
                    if (!tamam) { ekranHata++; return; }
                    tam.Save(yol, ImageFormat.Png);
                }
            }
            catch (Exception ex)
            {
                ekranHata++;
                Program.HataKaydet(ex, "--ekran yakalama: " + yol);
            }
        }

        static bool TekRenk(Bitmap bmp)
        {
            int ilk = bmp.GetPixel(0, 0).ToArgb();
            int adimX = Math.Max(1, bmp.Width / 37), adimY = Math.Max(1, bmp.Height / 29);
            for (int y = 0; y < bmp.Height; y += adimY)
                for (int x = 0; x < bmp.Width; x += adimX)
                    if (bmp.GetPixel(x, y).ToArgb() != ilk) return false;
            return true;
        }

        /// <summary>DWM'nin görünmez kenarlıkları hariç pencere dikdörtgeni.</summary>
        Rectangle GorunurSinirlar()
        {
            try
            {
                YerelDikdortgen d;
                if (YerelYontemler.DwmGetWindowAttribute(Handle, 9, out d, System.Runtime.InteropServices.Marshal.SizeOf(typeof(YerelDikdortgen))) == 0
                    && d.Sag > d.Sol && d.Alt > d.Ust)
                    return new Rectangle(d.Sol, d.Ust, d.Sag - d.Sol, d.Alt - d.Ust);
            }
            catch { }
            return Bounds;
        }

        void SekmeListesiYaz()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < sekmeler.TabCount; i++)
                    sb.AppendLine("sekme-" + (i + 1) + ".png\t" + sekmeler.TabPages[i].Text);
                sb.AppendLine();
                sb.AppendLine("Pencere başlığı: " + Text);
                sb.AppendLine("Durum: " + durumEtiketi.Text);
                sb.AppendLine("Ölçek: " + olcek.ToString("0.00", CultureInfo.InvariantCulture));
                File.WriteAllText(Path.Combine(ekranKlasoru, "sekmeler.txt"), sb.ToString(), new UTF8Encoding(true));
            }
            catch (Exception ex) { Program.HataKaydet(ex, "--ekran sekmeler.txt"); }
        }
    }

    /// <summary>Renkli kullanım çubuğu (ProgressBar renk değiştiremediği için kendimiz çiziyoruz).</summary>
    public class KullanimCubugu : Control
    {
        public double Oran;          // 0..1
        public double ZirveOran = -1; // <0 = gösterme
        public string Metin = "";

        public KullanimCubugu()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
        }

        public static Color Renk(double oran)
        {
            if (oran < 0.70) return Color.FromArgb(46, 160, 67);
            if (oran < 0.85) return Color.FromArgb(232, 140, 20);
            return Color.FromArgb(210, 45, 45);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                Graphics g = e.Graphics;
                g.Clear(BackColor);
                Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
                if (r.Width <= 2 || r.Height <= 2) return;
                using (SolidBrush arka = new SolidBrush(Color.FromArgb(232, 236, 240)))
                    g.FillRectangle(arka, r);

                double o = Math.Max(0, Math.Min(1, Oran));
                int w = (int)Math.Round((r.Width - 1) * o);
                if (w > 0)
                {
                    using (SolidBrush dolgu = new SolidBrush(Renk(o)))
                        g.FillRectangle(dolgu, r.X + 1, r.Y + 1, w, r.Height - 1);
                }

                if (ZirveOran >= 0)
                {
                    double z = Math.Max(0, Math.Min(1, ZirveOran));
                    int x = r.X + 1 + (int)Math.Round((r.Width - 2) * z);
                    int k = Math.Max(2, Height / 12);
                    using (Pen p = new Pen(Color.FromArgb(40, 40, 40), k))
                        g.DrawLine(p, x, r.Y + 1, x, r.Bottom);
                }

                using (Pen kenar = new Pen(Color.FromArgb(185, 192, 200)))
                    g.DrawRectangle(kenar, r);

                if (!string.IsNullOrEmpty(Metin))
                {
                    // Metin dolu kısmın üstünde beyaz, boş kısmın üstünde koyu görünsün
                    Size boy = TextRenderer.MeasureText(g, Metin, Font);
                    int metinOrta = r.Width / 2;
                    Color yaziRengi = (w > metinOrta + boy.Width / 2) ? Color.White : Color.FromArgb(25, 25, 25);
                    if (w > metinOrta - boy.Width / 2 && w <= metinOrta + boy.Width / 2) yaziRengi = Color.FromArgb(25, 25, 25);
                    TextRenderer.DrawText(g, Metin, Font, r, yaziRengi,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
                }
            }
            catch { }
        }
    }
}
