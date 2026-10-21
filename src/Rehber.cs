// RAM Danışmanı — statik Türkçe rehber içerikleri.
// Her yöntem bir HTML GÖVDE PARÇASI döndürür (<html>/<head> yok, JavaScript yok,
// IE11 uyumlu). Yalnızca Rapor.Css içinde tanımlı sınıflar kullanılır.
// Tüm id'ler benzersizdir ve "rehber-", "yeni-", "sozluk-", "sss-" önekleri taşır;
// Rapor bu parçaları tek belgede birleştirebilir.
// C# 5 ile derlenir: büyük metinler verbatim @"..." (içteki " karakteri "" olarak).
using System;
using System.Text;

namespace RamDanismani
{
    public static class Rehber
    {
        // =====================================================================
        //  RAM ALIRKEN NELERE DİKKAT ETMELİYİM
        // =====================================================================
        public static string SatinAlmaRehberiHtml()
        {
            StringBuilder sb = new StringBuilder(64 * 1024);

            sb.Append(@"
<h1 id=""rehber"">RAM alırken nelere dikkat etmeliyim?</h1>
<p>Bu bölüm, bir bilgisayara RAM (bellek) alırken ya da var olan RAM'i yükseltirken bilinmesi gerekenleri
sırasıyla anlatır. Bu bilgisayara özel önerileri <b>Yükseltme ve Satın Alma</b> sekmesinde bulabilirsiniz;
buradaki bilgiler her bilgisayar için geçerli genel kurallardır.</p>

<div class=""cevap"">
<b>Kısa özet — en sık yapılan 5 hata:</b>
<ul class=""liste"">
<li>Yanlış <b>nesil</b> almak (DDR4 yerine DDR5 ya da tersi). Nesiller birbirine takılmaz.</li>
<li>Yanlış <b>boyut</b> almak: dizüstüne masaüstü RAM'i (DIMM) ya da masaüstüne dizüstü RAM'i (SODIMM).</li>
<li>Bilgisayarın RAM'inin <b>lehimli</b> (yükseltilemez) olduğunu bilmeden RAM sipariş etmek.</li>
<li>Tek modül takıp <b>çift kanal</b> performansını kaçırmak.</li>
<li>İşlemcinin desteklemediği kadar <b>hızlı</b> (ve pahalı) RAM alıp farkı hiç kullanamamak.</li>
</ul>
</div>

<div class=""kart"">
<h3 id=""rehber-icindekiler"">İçindekiler</h3>
<ol class=""adimlar"">
<li><a href=""#rehber-ihtiyac"">Önce: Gerçekten RAM'e ihtiyacınız var mı?</a></li>
<li><a href=""#rehber-tur"">Nesil / tür uyumu (DDR3, DDR4, DDR5, LPDDR)</a></li>
<li><a href=""#rehber-form"">Form faktörü: DIMM, SODIMM, CAMM2</a></li>
<li><a href=""#rehber-kapasite"">Kapasite: Kaç GB almalıyım?</a></li>
<li><a href=""#rehber-kanal"">Çift kanal ve yuva doluluk sırası</a></li>
<li><a href=""#rehber-hiz"">Hız (MT/s), gecikme (CL), XMP ve EXPO</a></li>
<li><a href=""#rehber-voltaj"">Voltaj</a></li>
<li><a href=""#rehber-ecc"">ECC, Registered (RDIMM) ve UDIMM</a></li>
<li><a href=""#rehber-rank"">Rank ve yonga yoğunluğu</a></li>
<li><a href=""#rehber-ekleme"">Var olan RAM'e ekleme yaparken</a></li>
<li><a href=""#rehber-32bit"">32 bit Windows ve 4 GB sınırı</a></li>
<li><a href=""#rehber-grafik"">Dahili grafik ve paylaşımlı bellek</a></li>
<li><a href=""#rehber-satici"">Marka, satıcı, sahte ürün, garanti ve iade</a></li>
<li><a href=""#rehber-training"">DDR5'te ilk açılış: memory training</a></li>
<li><a href=""#rehber-dogrulama"">Taktıktan sonra doğrulama</a></li>
<li><a href=""#rehber-guvenlik"">Takarken güvenlik: statik elektrik, pil, garanti etiketi</a></li>
<li><a href=""#rehber-kontrol"">Satın almadan önce kontrol listesi</a></li>
</ol>
</div>
");

            // ---- 0) İhtiyaç ------------------------------------------------
            sb.Append(@"
<h2 id=""rehber-ihtiyac"">0. Önce: Gerçekten RAM'e ihtiyacınız var mı?</h2>
<p>RAM, açık olan programların ve belgelerin o an üzerinde çalışıldığı hızlı çalışma alanıdır. RAM yetmediğinde
Windows verileri diske (sayfa dosyasına) taşır; bilgisayar takılır, sekmeler yeniden yüklenir, program geçişleri
yavaşlar. Ama her yavaşlığın sebebi RAM değildir. Para harcamadan önce ölçün:</p>

<h3 id=""rehber-ihtiyac-gorev"">Görev Yöneticisi ile kontrol</h3>
<ol class=""adimlar"">
<li><span class=""kod"">Ctrl + Shift + Esc</span> ile Görev Yöneticisi'ni açın, <b>Performans</b> → <b>Bellek</b> bölümüne gelin.</li>
<li><b>Kullanımda</b> değeri ve grafik: En yoğun çalıştığınız anda kullanım sürekli <b>%85–90'ın üstündeyse</b> RAM yetmiyordur.</li>
<li><b>Kullanımda (Sıkıştırılmış)</b>: Parantez içindeki sıkıştırılmış miktar yüzlerce MB'ı, hatta birkaç GB'ı buluyorsa
Windows sıkışmış demektir; bellek sıkıştırma, RAM yetmediğinde devreye giren bir yedek çözümdür.</li>
<li><b>Taahhüt edilen</b> (Türkçe Windows'ta “Kaydedildi”, İngilizcede “Committed” olarak geçebilir): Programların
ayırdığı toplam bellek. İlk sayı fiziksel RAM miktarını sürekli aşıyorsa sistem diske yaslanıyor demektir.</li>
<li><b>Kullanılan yuvalar</b>: Örneğin “1/2” ise bir yuva boştur; ek modül takılabilir. “2/2” ise yuvalar doludur;
yükseltmek için modül değiştirmek gerekir. (Bazı BIOS'lar boş yuvaları hiç listelemez; o zaman “1/1” görünebilir,
bu her zaman tek yuva olduğu anlamına gelmez.)</li>
<li><b>Form faktörü</b> ve <b>Hız</b>: SODIMM/DIMM bilgisini ve RAM'in çalıştığı hızı burada da görebilirsiniz.</li>
</ol>

<div class=""ipucu"">
<b>Bu programın “Kullanım (canlı)” sekmesi</b> bellek kullanımını 2 saniyede bir ölçer ve oturum boyunca görülen
<b>zirveyi</b> saklar. Programı açık bırakıp en ağır işinizi yapın (Teams toplantısı + Excel + 30 sekme gibi);
sonra zirve değere bakın. Zirve %90 ve üstündeyse RAM eklemek gerçekten fark yaratır; %60'ın altında kalıyorsa
yavaşlığın sebebi büyük olasılıkla başka bir şeydir.
</div>

<h3 id=""rehber-ihtiyac-baska"">RAM değilse ne olabilir?</h3>
<ul class=""liste"">
<li><b>Mekanik disk (HDD)</b>: Görev Yöneticisi → Performans → Disk bölümünde tür “HDD” yazıyorsa ve disk etkinliği
sık sık %100'e çıkıyorsa, en büyük hızlanmayı SSD'ye geçmek sağlar; RAM'den bile önce gelir.</li>
<li><b>İşlemci</b>: CPU sürekli %90–100 ise RAM eklemek pek işe yaramaz.</li>
<li><b>Isınma ve toz</b>: Fanın sürekli bağırması, işlemcinin ısınıp yavaşlaması (thermal throttling).</li>
<li><b>Açılışta çalışan gereksiz programlar</b>, eski sürücüler, dolmuş disk (C: sürücüsünde %10'dan az boş alan).</li>
</ul>
");

            // ---- 1) Tür uyumu ----------------------------------------------
            sb.Append(@"
<h2 id=""rehber-tur"">1. Nesil / tür uyumu: DDR3, DDR4, DDR5, LPDDR</h2>
<p>RAM nesilleri <b>birbirinin yerine takılamaz</b>. Her neslin modül üzerindeki çentiği (kertik) farklı yerdedir,
yanlış modül yuvaya fiziksel olarak girmez. Elektriksel olarak da (voltaj, sinyal yapısı) uyumsuzdurlar.
Hangi nesli kullanacağınızı <b>anakart ve işlemci</b> belirler, siz seçemezsiniz.</p>

<table class=""tablo"">
<tr><th>Nesil</th><th>Yaygın olduğu dönem</th><th>Standart voltaj</th><th>Tipik hızlar (MT/s)</th><th>Not</th></tr>
<tr><td>DDR3 / DDR3L</td><td>2008–2015</td><td>1,5 V / 1,35 V</td><td>1066–1866</td><td>Eski sistemler. Yeni satın almaya değmez; sadece eski bir makineyi ayakta tutmak için.</td></tr>
<tr><td>DDR4</td><td>2015–2022 (hâlâ yaygın)</td><td>1,2 V</td><td>2133–3200 (XMP ile 3600+)</td><td>Intel 6.–11. nesil, bazı 12.–14. nesil anakartlar; AMD Ryzen AM4.</td></tr>
<tr><td>DDR5</td><td>2021–günümüz</td><td>1,1 V</td><td>4800–6400 (XMP/EXPO ile 8000+)</td><td>Intel 12. nesil ve sonrası (anakarta bağlı), Core Ultra; AMD Ryzen 7000/8000/9000 (AM5).</td></tr>
<tr><td>LPDDR4X / LPDDR5 / LPDDR5X</td><td>İnce dizüstü ve tabletler</td><td>Çok düşük</td><td>4266–8533</td><td>Neredeyse her zaman anakarta <b>lehimlidir</b> (ya da LPCAMM2 modülündedir); sonradan RAM eklenemez.</td></tr>
</table>

<div class=""kart uyari"">
<b>Dikkat — Intel 12., 13. ve 14. nesil:</b> Bu işlemciler hem DDR4 hem DDR5 destekler, ama <b>anakart yalnızca birini</b>
destekler. Aynı işlemciye sahip iki bilgisayardan biri DDR4, diğeri DDR5 kullanıyor olabilir. Bu yüzden “işlemcim
i5-12400, DDR5 alayım” demek yanlıştır; bilgisayarınızda <b>şu an takılı olan</b> RAM'in türüne bakın.
Bu programın <b>Modüller</b> sekmesi bunu gösterir.
</div>

<ul class=""liste"">
<li><b>LPDDR</b> (Low Power DDR), aynı numaralı DDR ile aynı şey değildir. LPDDR5, DDR5 yuvasına takılan bir modül
değildir; yongalar doğrudan anakarta lehimlenir. Teknik özelliklerde “LPDDR”, “onboard”, “soldered” görüyorsanız o RAM
değiştirilemez.</li>
<li><b>DDR5'te iki tür masaüstü modülü</b> vardır: normal UDIMM ve saat sürücülü <b>CUDIMM</b>. CUDIMM'ler çok yüksek
hızlar için Intel Core Ultra 200S gibi yeni platformlarda kullanılır; çoğu sistemde normal UDIMM gibi de çalışırlar
ama ek fayda sağlamazlar. Sıradan bir yükseltme için normal UDIMM yeterlidir.</li>
</ul>
");

            // ---- 2) Form faktörü -------------------------------------------
            sb.Append(@"
<h2 id=""rehber-form"">2. Form faktörü: DIMM, SODIMM, CAMM2</h2>
<p>Aynı nesil RAM'in iki ana boyutu vardır: masaüstü bilgisayarlar için uzun <b>DIMM</b>, dizüstü ve mini PC'ler için
kısa <b>SODIMM</b>. Birbirinin yerine takılmazlar ve dönüştürücüyle kullanılmaları önerilmez.</p>

<table class=""tablo"">
<tr><th>Nesil</th><th>Masaüstü DIMM</th><th>Dizüstü SODIMM</th></tr>
<tr><td>DDR3 / DDR3L</td><td>240 pin</td><td>204 pin</td></tr>
<tr><td>DDR4</td><td>288 pin</td><td>260 pin</td></tr>
<tr><td>DDR5</td><td>288 pin (çentik yeri DDR4'ten farklı)</td><td>262 pin</td></tr>
</table>

<ul class=""liste"">
<li>DDR4 ve DDR5 masaüstü modüllerin ikisi de 288 pindir ama <b>çentik yeri farklıdır</b>; birbirinin yuvasına girmez.</li>
<li>Mini PC'ler (NUC benzeri), All-in-One bilgisayarlar ve bazı ince masaüstüler de <b>SODIMM</b> kullanır.
Kasa masaüstü gibi görünse bile türü kontrol edin.</li>
<li><b>CAMM2 / LPCAMM2</b>: 2023'te JEDEC tarafından standartlaştırılan yeni, yatay vidalanan dizüstü modülleridir.
LPCAMM2, LPDDR5X yongalarını değiştirilebilir bir modüle taşır (ör. bazı yeni iş istasyonu dizüstülerinde).
SODIMM ile uyumsuzdur ve henüz yaygın değildir; yedek parçayı genelde bilgisayar üreticisinden ya da az sayıdaki
modül üreticisinden almanız gerekir.</li>
</ul>
<div class=""ipucu"">
Modül üzerindeki etiket genelde türü söyler: “PC4-25600S” = DDR4-3200 <b>SODIMM</b> (S harfi), “PC4-3200AA-U” = DDR4
masaüstü <b>UDIMM</b>, “PC5-44800S” veya “PC5-5600B-S” = DDR5-5600 SODIMM. Sondaki <b>S = SODIMM, U = UDIMM,
R = Registered (sunucu)</b> olarak okunur.
</div>
");

            // ---- 3) Kapasite -----------------------------------------------
            sb.Append(@"
<h2 id=""rehber-kapasite"">3. Kapasite: Kaç GB almalıyım?</h2>
<p>2026 itibarıyla Windows 11, tarayıcılar ve Teams/Zoom gibi uygulamalar birkaç yıl öncesine göre çok daha fazla bellek
kullanıyor. Aşağıdaki tablo <b>rahat çalışma</b> için önerilerdir; asgari değil.</p>

<table class=""tablo"">
<tr><th>Kullanım profili</th><th>Asgari</th><th>Önerilen</th><th>Açıklama</th></tr>
<tr><td>Web, e-posta, temel ofis (tek iş, az sekme)</td><td>8 GB</td><td><b>16 GB</b></td><td>8 GB artık sınırda; birkaç yıl sonra yetmeyecek.</td></tr>
<tr><td>Yoğun ofis: Teams/Zoom + Outlook + büyük Excel + çok sekme</td><td>16 GB</td><td><b>16–32 GB</b></td><td>Şirket bilgisayarlarının çoğu bu gruptadır.</td></tr>
<tr><td>Yazılım geliştirme, sanal makine, Docker, CAD (AutoCAD vb.), GIS</td><td>16 GB</td><td><b>32 GB</b></td><td>Her sanal makine kendi RAM'ini ayırır; IDE + tarayıcı + derleyici birlikte çalışır.</td></tr>
<tr><td>Oyun</td><td>16 GB</td><td><b>32 GB</b></td><td>Yeni oyunlar 16 GB'ı zorluyor; arka planda Discord/tarayıcı da açık kalır.</td></tr>
<tr><td>Video düzenleme (4K), 3D modelleme/render, büyük montaj CAD, büyük veri</td><td>32 GB</td><td><b>64 GB ve üstü</b></td><td>Proje büyüdükçe ihtiyaç doğrusal artar.</td></tr>
<tr><td>Yerel yapay zekâ modelleri (LLM) çalıştırma</td><td>32 GB</td><td><b>64 GB+</b></td><td>Ekran kartı belleği (VRAM) yetmezse model sistem RAM'ine taşar.</td></tr>
</table>

<div class=""kart bilgi"">
<b>Kural:</b> RAM'in fazlası bilgisayarı hızlandırmaz, sadece <b>yetmezliği</b> yavaşlatır. 16 GB'ın yettiği bir işte
64 GB almak gözle görülür fark yaratmaz. Ama birkaç yıl kullanılacak bir bilgisayarda bir basamak üstü almak mantıklıdır.
</div>
<p class=""not"">Not: 2025 sonundan itibaren yapay zekâ sunucularının talebi nedeniyle bellek fiyatları belirgin şekilde
yükseldi ve dalgalı seyrediyor; DDR4 üretimi de azaltılıyor. Fiyatlar haftalar içinde değişebilir, birkaç satıcıyı
karşılaştırın. Eski bir DDR4 makine için ek RAM gerekiyorsa, piyasadan tamamen kalkmadan almak mantıklı olabilir.</p>
");

            // ---- 4) Çift kanal ---------------------------------------------
            sb.Append(@"
<h2 id=""rehber-kanal"">4. Çift kanal: neden önemli, nasıl yapılır?</h2>
<p>Masaüstü ve dizüstü işlemcilerin çoğunda <b>iki bellek kanalı</b> vardır. Her kanala bir modül takıldığında işlemci iki
modüle aynı anda erişir ve <b>bellek bant genişliği yaklaşık iki katına</b> çıkar. Tek modülle sistem tek kanalda
çalışır. Örnek: DDR4-3200 tek kanal ≈ 25,6 GB/sn, çift kanal ≈ 51,2 GB/sn; DDR5-5600 tek modül ≈ 44,8 GB/sn,
iki modül ≈ 89,6 GB/sn.</p>
<ul class=""liste"">
<li><b>Dahili grafik (Intel Iris Xe / Arc, AMD Radeon) kullanan bilgisayarlarda</b> fark çok büyüktür; ekran kartının belleği
de sistem RAM'idir. Oyun ve görüntü işlemede %30'u aşan farklar görülebilir.</li>
<li>Günlük ofis işinde fark daha küçüktür (genelde %5–15), ama ücretsiz bir performanstır.</li>
<li><b>DDR5 notu:</b> Her DDR5 modülü kendi içinde iki adet 32 bitlik alt kanala ayrılır. Bu, tek modülü “çift kanal”
yapmaz; toplam genişlik yine 64 bittir. Gerçek çift kanal için DDR5'te de <b>iki modül</b> gerekir.</li>
</ul>

<h3 id=""rehber-kanal-nasil"">Nasıl çift kanal yapılır?</h3>
<ul class=""liste"">
<li>En kolayı <b>2'li kit</b> (ör. 2x16 GB) almaktır: modüller birlikte test edilmiş, eş parçalardır.</li>
<li>Var olan tek modülün <b>yanına eşini</b> eklemek de çift kanal sağlar (aynı kapasite, tür, hız; ideal olarak aynı parça numarası).</li>
<li><b>Farklı kapasiteler</b> (ör. 8 + 16 GB): Intel'de “Flex Mode” sayesinde 16 GB'ın 8+8'lik kısmı çift kanal,
kalan 8 GB tek kanal çalışır. AMD platformları da benzer şekilde kısmi çift kanal yapabilir. Çalışır, ama eş modül kadar
iyi değildir.</li>
</ul>

<h3 id=""rehber-kanal-sira"">4 yuvalı masaüstü anakartta doluluk sırası</h3>
<p>İki modül takarken, çoğu anakart <b>A2 ve B2</b> yuvalarını ister (işlemciden sayınca genellikle 2. ve 4. yuva).
Yanlış yuvalara (ör. A1 + A2) takarsanız iki modül de <b>aynı kanalda</b> kalır ve tek kanal çalışırsınız.
Kesin sıra anakart kılavuzunda ve genelde anakart üzerinde yazar.</p>
<table class=""tablo"">
<tr><th>Modül sayısı</th><th>Tipik yuva</th></tr>
<tr><td>1</td><td>A2</td></tr>
<tr><td>2</td><td>A2 + B2</td></tr>
<tr><td>4</td><td>A1 + A2 + B1 + B2 (hepsi)</td></tr>
</table>
<div class=""kart uyari"">
<b>DDR5'te 4 modül:</b> Tüketici anakartlarında 4 adet DDR5 modül takıldığında desteklenen hız belirgin düşer (ör. 5600
yerine 3600–4800) ve sistem daha zor kararlı olur. 64 GB istiyorsanız <b>4x16 yerine 2x32</b>, 96 GB için 2x48,
128 GB için 2x64 GB tercih edin.
</div>
");

            // ---- 5) Hız ----------------------------------------------------
            sb.Append(@"
<h2 id=""rehber-hiz"">5. Hız (MT/s), gecikme (CL), XMP ve EXPO</h2>
<h3 id=""rehber-hiz-mts"">MT/s ile MHz aynı şey değil</h3>
<p>RAM hızı doğrusu <b>MT/s</b> (saniyede milyon aktarım) birimiyle ifade edilir. DDR (Double Data Rate) bellek her saat
çevriminde iki aktarım yaptığı için, “DDR4-3200” modülün gerçek saat frekansı 1600 MHz'dir. Mağazalar ve ürün sayfaları
çoğunlukla “3200 MHz” yazar; bunu <b>3200 MT/s</b> olarak okuyun. Bazı araçlar 1600 MHz gösterir; bu da doğrudur, aynı şeydir.</p>

<h3 id=""rehber-hiz-sinir"">Daha hızlı RAM her zaman daha hızlı çalışmaz</h3>
<ul class=""liste"">
<li>RAM, işlemcinin ve anakartın desteklediği hızın üstüne çıkamaz. Örneğin işlemciniz resmi olarak DDR5-4800
destekliyorsa, DDR5-5600 bir modül 4800'de çalışır. Bu bir arıza değildir; modül geriye doğru uyumludur.</li>
<li>Hızlı modül almak zarar vermez ve bazen fiyat farkı yoktur; ama <b>fazladan para ödemeye değmez</b>.</li>
<li>Farklı hızlarda modüller karışırsa <b>hepsi en yavaş modülün hızına</b> (ve onun gecikmelerine) iner.</li>
<li>Bilgisayarın desteklediği hız: işlemci üreticisinin sayfasında (Intel ARK / AMD ürün sayfası) “Bellek türleri”
satırında yazar. Bu programın <b>Yükseltme</b> sekmesi işlemciden bu sınırı tahmin eder.</li>
</ul>

<h3 id=""rehber-hiz-cl"">Gecikme: CL ve gerçek gecikme (ns)</h3>
<p><b>CL</b> (CAS Latency), saat çevrimi cinsinden bir gecikme değeridir. Tek başına küçük olması iyi demek değildir;
hızla birlikte değerlendirilir. Gerçek gecikme yaklaşık olarak şöyle hesaplanır:</p>
<p class=""kod"">gerçek gecikme (ns) = CL × 2000 / hız (MT/s)</p>
<table class=""tablo"">
<tr><th>Modül</th><th>CL</th><th>Gerçek gecikme</th></tr>
<tr><td>DDR4-2666</td><td>19</td><td>≈ 14,3 ns</td></tr>
<tr><td>DDR4-3200</td><td>22 (standart JEDEC)</td><td>≈ 13,8 ns</td></tr>
<tr><td>DDR4-3200</td><td>16 (XMP)</td><td>10,0 ns</td></tr>
<tr><td>DDR4-3600</td><td>18 (XMP)</td><td>10,0 ns</td></tr>
<tr><td>DDR5-4800</td><td>40 (standart JEDEC)</td><td>≈ 16,7 ns</td></tr>
<tr><td>DDR5-5600</td><td>46 (standart JEDEC)</td><td>≈ 16,4 ns</td></tr>
<tr><td>DDR5-6000</td><td>30 (EXPO/XMP)</td><td>10,0 ns</td></tr>
<tr><td>DDR5-6400</td><td>32 (XMP)</td><td>10,0 ns</td></tr>
</table>
<p>Görüldüğü gibi standart (JEDEC) hızlarda DDR5'in gecikmesi nanosaniye olarak DDR4'ten biraz yüksektir; XMP/EXPO kitlerinde ise ikisi aynı düzeydedir (~10 ns). DDR5'in asıl üstünlüğü <b>bant genişliğidir</b>.
Ofis kullanımında CL farkı hissedilmez; oyun ve bazı bilimsel/teknik yüklerde küçük fark yaratır.
tRCD, tRP, tRAS gibi diğer zamanlamalar da vardır; ürün sayfasında “36-36-36-76” gibi sıralanırlar. İlk sayı CL'dir.</p>

<h3 id=""rehber-hiz-xmp"">XMP (Intel) ve EXPO (AMD)</h3>
<ul class=""liste"">
<li>Her modülün içinde <b>SPD</b> adlı küçük bir yonga bulunur ve standart (JEDEC) hız ayarlarını taşır. Kutuda yazan
yüksek hızlar (ör. DDR5-6000 CL30) çoğunlukla <b>XMP</b> veya <b>EXPO</b> profili ile gelir ve <b>otomatik açılmaz</b>.</li>
<li>Masaüstünde BIOS/UEFI'ye girip profili açmanız gerekir (ASUS: “AI Overclock Tuner → XMP/EXPO”, MSI: “A-XMP/EXPO”,
Gigabyte: “XMP/EXPO Profile”). Açılmazsa modül JEDEC hızında, daha yavaş çalışır.</li>
<li>XMP/EXPO teknik olarak üretici onaylı bir hız aşırtmadır; çoğu sistemde sorunsuz çalışır ama kararlılık garanti değildir.
Sistem açılmazsa profili kapatın ya da bir alt profili seçin.</li>
<li><b>Dizüstülerde</b> XMP genellikle yoktur (bazı oyun dizüstüleri hariç). Dizüstü için JEDEC standart hızındaki
modül en güvenli seçimdir; “XMP'li yüksek hız” için fazladan ödemeyin.</li>
<li>Şirket bilgisayarlarında kararlılık hızdan önemlidir: XMP/EXPO açmayın, JEDEC hızında bırakın.</li>
</ul>
");

            // ---- 6) Voltaj -------------------------------------------------
            sb.Append(@"
<h2 id=""rehber-voltaj"">6. Voltaj</h2>
<table class=""tablo"">
<tr><th>Tür</th><th>Standart voltaj</th><th>XMP/EXPO kitlerinde tipik</th></tr>
<tr><td>DDR3</td><td>1,5 V</td><td>1,5–1,65 V</td></tr>
<tr><td>DDR3L</td><td>1,35 V (çoğu 1,5 V'ta da çalışır)</td><td>—</td></tr>
<tr><td>DDR4</td><td>1,2 V</td><td>1,35 V (bazen 1,4 V+)</td></tr>
<tr><td>DDR5</td><td>1,1 V (voltaj modül üzerindeki PMIC ile ayarlanır)</td><td>1,25–1,45 V</td></tr>
</table>
<div class=""kart uyari"">
<b>DDR3L tuzağı:</b> DDR3L isteyen dizüstüler (genellikle 2013–2016 Intel Haswell/Broadwell/Skylake dönemi) yalnızca
1,35 V modül kabul eder. Bu makinelere 1,5 V'luk düz <b>DDR3</b> takılmaz: ya hiç açılmaz ya da kararsız çalışır.
Tersi genelde sorun değildir: DDR3L modüllerin çoğu 1,5 V isteyen eski sistemlerde de çalışır. Emin değilseniz <b>DDR3L</b> alın.
</div>
<p>DDR4 ve DDR5'te standart modüller arasında voltaj sorunu pratikte yaşanmaz. Yalnızca farklı XMP voltajlı modülleri
karıştırmaktan kaçının.</p>
");

            // ---- 7) ECC / RDIMM --------------------------------------------
            sb.Append(@"
<h2 id=""rehber-ecc"">7. ECC, Registered (RDIMM) ve UDIMM</h2>
<ul class=""liste"">
<li><b>Normal masaüstü ve dizüstü bilgisayarlar</b>: <b>UDIMM / SODIMM, ECC'siz (non-ECC)</b> modül kullanır.
Ürün adında “Unbuffered”, “Non-ECC” yazar. Sıradan alımda doğru seçim budur.</li>
<li><b>Sunucular ve bazı iş istasyonları</b>: <b>RDIMM</b> (Registered) veya <b>LRDIMM</b> kullanır, neredeyse hep ECC'lidir.
Sunucu RAM'i normal PC'de çalışmaz; normal PC RAM'i de RDIMM isteyen sunucuda çalışmaz.</li>
<li>DDR5'te UDIMM ve RDIMM'in çentik yerleri bile farklıdır. DDR4'te çentik aynı olduğu için yanlış modül yuvaya girer
ama sistem açılmaz — dikkat.</li>
<li>RDIMM ile UDIMM, ECC ile ECC'siz modüller <b>aynı sistemde karıştırılmaz</b>.</li>
<li><b>ECC UDIMM</b> (ECC'li ama registered olmayan) bazı iş istasyonlarında ve küçük sunucularda kullanılır. Bilgisayarınız
ECC kullanıyorsa (bu program “Toplam genişlik” 72 veya 80 bit olarak gösterir), ekleyeceğiniz modül de ECC olmalıdır.</li>
</ul>
<div class=""kart bilgi"">
<b>DDR5 “on-die ECC” gerçek ECC değildir.</b> Tüm DDR5 yongalarında, yonganın kendi içindeki hataları düzelten bir
on-die ECC bulunur; bu, yüksek yoğunluklu yongaların güvenilir üretilebilmesi içindir. Modül ile işlemci arasındaki
yolda oluşan hataları yakalamaz ve işletim sistemine hata raporlamaz. Uçtan uca koruma isteyen sistemler (sunucu,
kritik iş istasyonu) için ürün adında açıkça “ECC” yazan modül ve bunu destekleyen işlemci/anakart gerekir.
</div>
");

            // ---- 8) Rank / yoğunluk ----------------------------------------
            sb.Append(@"
<h2 id=""rehber-rank"">8. Rank ve yonga yoğunluğu (kısaca)</h2>
<ul class=""liste"">
<li><b>Rank</b>, modül üzerindeki aynı anda erişilen 64 bitlik yonga grubudur. Etikette “1Rx8”, “2Rx8”, “1Rx16” gibi
yazar: 1R = tek rank, 2R = çift rank; x8/x16 yonga genişliğidir.</li>
<li>Aynı hızda <b>çift rank</b> modüller bazı işlerde biraz daha hızlıdır, ama kanal başına toplam rank arttıkça
(özellikle 4 modülde) desteklenen azami hız düşebilir.</li>
<li>Eklemede farklı rank'li modüller genelde çalışır; ama mümkünse var olanla aynı rank'i seçin.</li>
<li><b>Yoğunluk</b>: Yongaların kapasitesidir (DDR4'te 4/8/16 Gbit, DDR5'te 16/24/32 Gbit). DDR5'teki 24 Gbit yongalar sayesinde
24 GB ve 48 GB gibi “ara” kapasiteli modüller vardır; bunlar için bazı anakartlar <b>BIOS güncellemesi</b> ister.</li>
<li>Eski platformlar yüksek yoğunluklu yeni modülleri tanımayabilir (ör. bazı eski Intel sistemleri “yüksek yoğunluklu”
DDR3 modülleri ya da 32 GB'lık DDR4 modülleri görmez). Eski bilgisayarlarda üreticinin desteklediği modül kapasitesine bakın.</li>
</ul>
");

            // ---- 9) Ekleme ---------------------------------------------------
            sb.Append(@"
<h2 id=""rehber-ekleme"">9. Var olan RAM'e ekleme yaparken</h2>
<p>Boş bir yuvaya modül eklemek en ucuz yükseltmedir ama uyumsuzluk riski de en çok burada vardır. Öncelik sırası:</p>
<ol class=""adimlar"">
<li><b>Aynı parça numarası</b> (en iyisi). Bu programın <b>Modüller</b> sekmesindeki “Parça no” değerini kopyalayıp
aratın. Dizüstülerde fabrika modülü genelde Samsung, SK hynix veya Micron'dur; aynı parça numarası bulunabilir.</li>
<li>Bulunamazsa <b>aynı tür, aynı form faktörü, aynı kapasite, aynı (ya da daha yüksek) hız, aynı voltaj</b> ve
mümkünse aynı CL ve rank. Daha yüksek hızlı modül, var olanın hızına iner — sorun değildir.</li>
<li>Farklı marka modüller çoğunlukla sorunsuz çalışır; ama nadiren açılmama, mavi ekran ya da hızın daha da düşmesi
görülebilir. Satın aldığınız yerde iade hakkınız olduğundan emin olun.</li>
<li>Uyumsuzluk yaşarsanız veya en iyi sonucu istiyorsanız <b>tüm modülleri bir kit olarak değiştirin</b> (ör. 1x8 GB
yerine 2x16 GB kit). Çıkan modülü başka bir makinede kullanabilirsiniz.</li>
</ol>
<div class=""ipucu"">
Bilgisayar üreticisinin destek sayfası (Lenovo PSREF, Dell “Kurulum ve Teknik Özellikler”, HP QuickSpecs) ile bellek
üreticilerinin web sitesindeki “uyumlu modül bulucu” sayfaları, model numaranızı girince doğru modülü listeler.
Bunlar bir şey kurmayı gerektirmez; sadece web sayfasıdır.
</div>
");

            // ---- 10) 32 bit ------------------------------------------------
            sb.Append(@"
<h2 id=""rehber-32bit"">10. 32 bit Windows ve 4 GB sınırı</h2>
<p>32 bit Windows (Windows 10'un 32 bit sürümü ve öncesi) en fazla 4 GB adres alanı kullanabilir; pratikte
<b>3–3,5 GB</b> civarı görülür. Bu sistemlere 8 GB takmak işe yaramaz. Windows 11'in 32 bit sürümü yoktur.
Kontrol: <b>Ayarlar → Sistem → Hakkında → Sistem türü</b>. “32 bit işletim sistemi” yazıyorsa önce 64 bit
Windows'a geçilmesi gerekir (bu temiz kurulum demektir).</p>
");

            // ---- 11) Grafik ------------------------------------------------
            sb.Append(@"
<h2 id=""rehber-grafik"">11. Dahili grafik ve paylaşımlı bellek</h2>
<ul class=""liste"">
<li>Ayrı ekran kartı olmayan bilgisayarlarda grafik işlemci, sistem RAM'ini kullanır. Bu yüzden 8 GB'lık bir dizüstüde
programlara kalan bellek daha da azdır; çift kanal da bu sistemlerde en çok fark yaratır.</li>
<li>BIOS'un grafik için <b>sabit ayırdığı</b> miktar (özellikle AMD sistemlerde “UMA Frame Buffer” ayarı: 512 MB, 2 GB
gibi) Görev Yöneticisi'nde <b>Donanıma ayrılmış</b> olarak görünür ve Windows tarafından kullanılamaz.</li>
<li>Görev Yöneticisi → GPU bölümündeki <b>Paylaşılan GPU belleği</b> (genelde RAM'in yarısı) bir <b>üst sınırdır</b>,
sürekli ayrılmış bellek değildir; grafik ihtiyaç duydukça kullanır, işi bitince geri verir.</li>
</ul>
");

            // ---- 12) Satıcı ------------------------------------------------
            sb.Append(@"
<h2 id=""rehber-satici"">12. Marka, satıcı, sahte ürün, garanti ve iade</h2>
<ul class=""liste"">
<li><b>Güvenilir markalar</b>: Yongayı kendisi üretenler (Samsung, SK hynix, Micron/Crucial) ve köklü modül markaları
(Kingston, Corsair, G.Skill, ADATA/XPG, TeamGroup, Patriot, Lexar, PNY vb.). Fabrika çıkışı dizüstü RAM'leri çoğunlukla
Samsung, SK hynix veya Micron'dur.</li>
<li><b>Sahte ve yeniden etiketlenmiş ürünler</b>: Piyasa fiyatının çok altındaki, markasız, etiketi yamuk ya da
“Samsung 16GB DDR4” gibi genel başlıklı ilanlara dikkat edin. Pazar yerlerinde satıcının puanına ve
<b>yetkili distribütör / ithalatçı garantisi</b> olup olmadığına bakın.</li>
<li><b>Fatura ve garanti</b>: Şirket alımlarında mutlaka faturalı alın. Birçok marka bellek modüllerine uzun
(çoğu zaman “sınırlı ömür boyu”) garanti verir; garantinin Türkiye'de kim tarafından karşılandığını sorun.</li>
<li><b>İade</b>: İnternetten tüketici alımlarında genellikle 14 günlük cayma hakkı vardır; koşulları satın almadan önce
satıcıyla teyit edin. Uyumsuzluk çıkarsa iade edebilmek için ambalajı ve faturayı saklayın.</li>
<li><b>İkinci el</b>: Sunucu çıkması RDIMM'ler çok ucuz görünür ama normal PC'de çalışmaz. İkinci el UDIMM/SODIMM alırsanız
taktıktan sonra mutlaka bellek testi yapın.</li>
</ul>
");

            // ---- 13) Training ----------------------------------------------
            sb.Append(@"
<h2 id=""rehber-training"">13. DDR5'te ilk açılış: memory training</h2>
<div class=""kart uyari"">
<b>RAM taktıktan sonraki ilk açılışta ekran 1–3 dakika (büyük kitlerde daha uzun) siyah kalabilir.</b> Bu normaldir:
anakart yeni modüllerle en uygun zamanlamaları ölçüyor (“memory training”). Bazı sistemler bu sırada bir-iki kez kendini
yeniden başlatır. <b>Bu sırada bilgisayarı kapatmayın.</b> 5 dakikadan uzun sürerse ya da hiç görüntü gelmezse modüllerin
tam oturduğunu kontrol edin.
</div>
<p>Aynı durum XMP/EXPO açıldığında, BIOS güncellendiğinde veya CMOS sıfırlandığında da yaşanabilir. Masaüstü
anakartlarda “Memory Context Restore” / hızlı bellek eğitimi gibi ayarlar sonraki açılışları hızlandırır.
DDR4 sistemlerde bu süre çok daha kısadır ama yine birkaç saniye sürebilir.</p>
");

            // ---- 14) Doğrulama ---------------------------------------------
            sb.Append(@"
<h2 id=""rehber-dogrulama"">14. Taktıktan sonra doğrulama</h2>
<ol class=""adimlar"">
<li><b>Bu programı</b> yeniden çalıştırın: Modüller sekmesinde yeni modül, kapasite, hız ve “çift kanal” durumu görünmeli.</li>
<li><b>Görev Yöneticisi → Performans → Bellek</b>: Toplam miktar, “Kullanılan yuvalar” ve hız.</li>
<li><b>Sistem Bilgisi</b> (<span class=""kod"">msinfo32</span>): “Yüklü Fiziksel Bellek (RAM)” satırı.</li>
<li><b>Windows Bellek Tanılama</b>: Başlat'a <span class=""kod"">mdsched.exe</span> yazın → “Şimdi yeniden başlat ve sorunları
denetle”. Bilgisayar yeniden başlar ve RAM'i test eder; sonuç oturum açınca bildirim olarak gelir (gelmezse Olay Görüntüleyicisi →
Windows Günlükleri → Sistem içinde “MemoryDiagnostics-Results” kaynağına bakın). Daha kapsamlı test için seçeneklerden
(F1) “Genişletilmiş” test seçilebilir.</li>
<li>Bir hafta boyunca mavi ekran, beklenmedik kapanma ya da program çökmesi olmuyorsa yükseltme başarılıdır.</li>
</ol>
");

            // ---- 15) Güvenlik ----------------------------------------------
            sb.Append(@"
<h2 id=""rehber-guvenlik"">15. Takarken güvenlik: statik elektrik, pil, garanti etiketi</h2>
<ol class=""adimlar"">
<li>Bilgisayarı tamamen kapatın (Shift tuşu basılıyken “Kapat” seçilirse Windows “Hızlı başlangıç” atlanır ve tam kapanış yapılır),
<b>güç kablosunu çıkarın</b>.</li>
<li><b>Dizüstülerde dahili pil</b>: Pil sökülebiliyorsa çıkarın. Dahili pilli modellerde servis kılavuzu ya pil konnektörünün
sökülmesini ister ya da BIOS'ta bir seçenek sunar (ör. Lenovo'da “Disable Built-in Battery”: bilgisayar kapanır ve
şarj aleti takılana kadar açılmaz). Bu adımı atlamak anakartta enerji varken çalışmak demektir.</li>
<li>Güç tuşuna 10–15 saniye basılı tutarak kalan enerjiyi boşaltın.</li>
<li><b>Statik elektrik</b>: Kasanın boyasız metal bir kısmına dokunarak kendinizi topraklayın; varsa antistatik bileklik
kullanın. Halı üzerinde çalışmayın. Modülleri <b>kenarlarından</b> tutun; altın renkli kontaklara ve yongalara dokunmayın.</li>
<li><b>Masaüstü</b>: Yuvanın kilit mandallarını açın (bazı anakartlarda yalnız bir taraf açılır), modülün çentiğini yuvadaki
çıkıntıyla hizalayın, iki ucundan eşit kuvvetle bastırın; mandallar “klik” sesiyle kapanmalıdır. Az değil, sağlamca bastırmak gerekir.</li>
<li><b>Dizüstü (SODIMM)</b>: Modülü yaklaşık 30 derece açıyla yuvaya sonuna kadar itin, sonra iki yandaki klipsler oturana kadar
aşağı bastırın. Alt kapağı plastik bir açma aletiyle açın; metal tornavida ile kazımayın.</li>
<li><b>Garanti</b>: Birçok üretici RAM'i “müşteri tarafından değiştirilebilir parça” (CRU) sayar; servis kılavuzunda bu yazar.
Kapakta garanti etiketi/mühür varsa önce satıcıya ya da yetkili servise danışın. Şirket ve kiralık cihazlarda önce
IT birimine ya da kiralama sözleşmesine bakın.</li>
</ol>
");

            // ---- Kontrol listesi --------------------------------------------
            sb.Append(@"
<h2 id=""rehber-kontrol"">Satın almadan önce kontrol listesi</h2>
<table class=""tablo"">
<tr><th>Kontrol</th><th>Nereden bakılır</th></tr>
<tr><td>Bilgisayarın RAM'i lehimli mi, kaç yuva var, kaçı boş?</td><td>Bu programın Özet sekmesi (yuva haritası), üreticinin teknik özellik belgesi</td></tr>
<tr><td>Nesil: DDR3 / DDR3L / DDR4 / DDR5</td><td>Modüller sekmesi, Görev Yöneticisi, takılı modülün etiketi</td></tr>
<tr><td>Form: DIMM (masaüstü) mi, SODIMM (dizüstü/mini PC) mi?</td><td>Modüller sekmesi, Görev Yöneticisi “Form faktörü”</td></tr>
<tr><td>Azami toplam kapasite ve modül başına kapasite</td><td>Üreticinin belgesi (BIOS'un bildirdiği değer yanlış olabilir), işlemci sayfası</td></tr>
<tr><td>Desteklenen hız</td><td>İşlemci üreticisinin sayfası, Yükseltme sekmesi</td></tr>
<tr><td>Var olan modülün parça numarası, kapasitesi, hızı, rank'i</td><td>Modüller sekmesi</td></tr>
<tr><td>ECC / Registered gerekiyor mu?</td><td>Sadece sunucu ve iş istasyonlarında; Modüller sekmesi “Toplam genişlik” ve “Tür ayrıntısı”</td></tr>
<tr><td>Fatura, garanti, iade koşulu</td><td>Satıcı</td></tr>
</table>
");
            return sb.ToString();
        }

        // =====================================================================
        //  YENİ BİLGİSAYAR ALIRKEN RAM
        // =====================================================================
        public static string YeniBilgisayarRehberiHtml()
        {
            StringBuilder sb = new StringBuilder(32 * 1024);

            sb.Append(@"
<h1 id=""yeni"">Yeni bilgisayar alırken RAM</h1>
<p>Yeni bir bilgisayar alırken RAM konusunda verilecek en önemli karar, <b>sonradan yükseltilebilir mi</b> sorusudur.
Ultra ince dizüstülerin çoğunda RAM anakarta lehimlidir ve bilgisayarın ömrü boyunca aynı kalır. Bu bölüm, ilanı ve teknik
özellikleri okuyup doğru seçimi yapmanıza yardım eder.</p>

<div class=""kart"">
<h3 id=""yeni-icindekiler"">İçindekiler</h3>
<ol class=""adimlar"">
<li><a href=""#yeni-ne-kadar"">2026'da ne kadar RAM?</a></li>
<li><a href=""#yeni-lehimli"">Lehimli RAM tuzağı</a></li>
<li><a href=""#yeni-ozellik"">Teknik özelliklerde hangi satırlara bakılır?</a></li>
<li><a href=""#yeni-tek-cift"">1x16 mı, 2x8 mi?</a></li>
<li><a href=""#yeni-nesil"">DDR5, DDR4, LPDDR5X, CAMM2</a></li>
<li><a href=""#yeni-ilan"">Mağaza ilanını okuma örnekleri</a></li>
<li><a href=""#yeni-sorular"">Satıcıya sorulacak sorular</a></li>
<li><a href=""#yeni-sirket"">Şirket alımları için öneriler</a></li>
</ol>
</div>

<h2 id=""yeni-ne-kadar"">1. 2026'da ne kadar RAM?</h2>
<div class=""cevap"">
<b>Asgari 16 GB.</b> 8 GB'lık yeni bilgisayar 2026'da tavsiye edilmez: Windows 11, bir tarayıcı ve Teams birlikte 8 GB'ı
doldurur. Microsoft'un yapay zekâ özellikli “Copilot+ PC” sınıfı için de asgari 16 GB şarttır.
</div>
<table class=""tablo"">
<tr><th>Kullanıcı</th><th>Yükseltilebilir bilgisayar</th><th>RAM'i lehimli bilgisayar</th></tr>
<tr><td>Temel ofis, web, e-posta</td><td>16 GB (sonra 32'ye çıkabilir)</td><td>16 GB</td></tr>
<tr><td>Yoğun ofis, çok sekme, sürekli toplantı</td><td>16 GB, tercihen 32 GB</td><td><b>32 GB</b></td></tr>
<tr><td>Yazılım, sanal makine, CAD, GIS, analiz</td><td>32 GB</td><td>32 GB, tercihen 64 GB</td></tr>
<tr><td>Video, 3D, büyük veri, yerel yapay zekâ</td><td>32–64 GB</td><td>64 GB ve üstü</td></tr>
</table>
<p class=""not"">Kural: RAM'i lehimli bilgisayarda bugünkü ihtiyacın <b>bir basamak üstünü</b> alın; çünkü 3–5 yıl boyunca
değiştiremeyeceksiniz. Yükseltilebilir bilgisayarda bugünkü ihtiyaç kadar alıp sonra eklemek daha ekonomik olabilir.</p>

<h2 id=""yeni-lehimli"">2. Lehimli RAM tuzağı</h2>
<ul class=""liste"">
<li><b>Ultrabook ve ince-hafif dizüstüler</b> (ör. LPDDR5X kullanan çoğu model), tabletler ve 2'si 1 arada cihazlar:
RAM genellikle tamamen lehimlidir; <b>hiç yuva yoktur</b>. Bazı yeni işlemcilerde (ör. Intel Core Ultra 200V “Lunar Lake”)
bellek doğrudan işlemci paketinin üzerindedir. Bu cihazlarda baştan yeterli RAM alın.</li>
<li><b>Karma yapı</b>: “8 GB onboard + 1 boş SODIMM yuvası” gibi. Bir kısmı lehimli, bir yuva boştur. Yükseltilebilir,
ama en iyi performans için ekleyeceğiniz modül lehimli kısımla <b>aynı kapasitede</b> olmalıdır (8 + 8 = 16 GB çift kanal).
8 + 32 = 40 GB da olur ama 8+8'lik kısım çift kanal, kalanı tek kanal çalışır.</li>
<li><b>Tam yükseltilebilir</b>: İki SODIMM yuvası (iş dizüstüleri, oyun dizüstüleri, mobil iş istasyonları) veya masaüstü
anakartlar. En esnek seçenek.</li>
</ul>
<div class=""kart kritik"">
Lehimli RAM sonradan <b>değiştirilemez ve eklenemez</b>. Pratikte tek çözüm bilgisayarı değiştirmektir. Satın almadan önce
mutlaka teknik özellik belgesine bakın.
</div>

<h2 id=""yeni-ozellik"">3. Teknik özelliklerde hangi satırlara bakılır?</h2>
<p>Üreticinin teknik belgesinde (Lenovo PSREF, Dell “Setup and Specifications”, HP QuickSpecs, ASUS/MSI ürün sayfası
“Specifications”) bellek satırı şu ifadeleri içerir:</p>
<table class=""tablo"">
<tr><th>İfade</th><th>Anlamı</th></tr>
<tr><td>“2x SO-DIMM, up to 64GB”, “2 slots, dual-channel”</td><td>İki yuva, en fazla 64 GB. Yükseltilebilir.</td></tr>
<tr><td>“1x 16GB SO-DIMM” ve “2 slots”</td><td>Bir yuva dolu, bir yuva boş. Tek kanal çalışır; eş modül eklenebilir.</td></tr>
<tr><td>“Soldered”, “onboard”, “memory on package”, “non-upgradable”</td><td>Lehimli. Yükseltilemez.</td></tr>
<tr><td>“8GB soldered + 1x SO-DIMM slot”</td><td>Karma yapı. Bir yuva var.</td></tr>
<tr><td>“LPDDR5X-7467”, “LPDDR4X”</td><td>Neredeyse kesin lehimli (LPCAMM2 yazmıyorsa).</td></tr>
<tr><td>“LPCAMM2”, “CAMM2”</td><td>Yeni tip değiştirilebilir modül; yedek modülü bulmak SODIMM kadar kolay değil.</td></tr>
<tr><td>“Max memory: 32GB” (eski belge)</td><td>Belge yazıldığı tarihteki modüllere göredir; bazen daha büyük modüller de çalışır, ama garanti edilmez.</td></tr>
</table>

<h2 id=""yeni-tek-cift"">4. 1x16 mı, 2x8 mi?</h2>
<p>Aynı toplam kapasitenin nasıl dağıtıldığı önemlidir:</p>
<table class=""tablo"">
<tr><th>Yapı</th><th>Artısı</th><th>Eksisi</th></tr>
<tr><td><b>2x8 GB</b> (iki yuva dolu)</td><td>Hemen çift kanal: daha yüksek bant genişliği, dahili grafikte belirgin fark.</td><td>Yükseltmek için iki modülü de değiştirmek gerekir (8'ler boşa çıkar).</td></tr>
<tr><td><b>1x16 GB</b> (bir yuva boş)</td><td>Sonra aynı modülden bir tane ekleyip 32 GB çift kanala çıkmak ucuz ve kolay.</td><td>Ekleme yapılana kadar tek kanal çalışır; dahili grafikli sistemlerde yavaş.</td></tr>
<tr><td><b>2x16 GB</b></td><td>Çift kanal ve 2026 için rahat kapasite.</td><td>Daha pahalı; ileride 64 GB için ikisini de değiştirmek gerekir.</td></tr>
</table>
<div class=""ipucu"">
Dahili grafikli (ayrı ekran kartı olmayan) bir bilgisayarda <b>çift kanal yapıyı</b> tercih edin. Ayrı ekran kartlı bir
oyun/iş dizüstüsünde 1x16 + boş yuva da mantıklıdır, yeter ki kısa süre içinde eşini eklemeyi planlayın.
</div>

<h2 id=""yeni-nesil"">5. DDR5, DDR4, LPDDR5X, CAMM2</h2>
<ul class=""liste"">
<li><b>DDR5</b>: 2026'da yeni bilgisayarların standardıdır. Yeni bir masaüstü/dizüstü alırken DDR5 tercih edin.</li>
<li><b>DDR4</b>: Hâlâ bazı ucuz modellerde ve eski stok bilgisayarlarda bulunur. Çalışır, ama üretimi azaldığı için
gelecekte ek RAM bulmak zorlaşabilir ve pahalanabilir. Yeni alımda DDR4'lü bir modeli ancak belirgin fiyat avantajı
varsa ve RAM baştan yeterliyse düşünün.</li>
<li><b>LPDDR5X</b>: Çok hızlıdır (7467–8533 MT/s), az enerji harcar, pil ömrünü uzatır ve dahili grafiğe iyi gelir.
Bedeli: <b>yükseltilemez</b>. Kapasiteyi baştan doğru seçerseniz iyi bir tercihtir.</li>
<li><b>CAMM2 / LPCAMM2</b>: Lehimli belleğin hızını değiştirilebilir bir modülle sunan yeni standart. Henüz az sayıda
modelde var; yedek/yükseltme modülü genelde üreticiden temin edilir.</li>
</ul>

<h2 id=""yeni-ilan"">6. Mağaza ilanını okuma örnekleri</h2>
<div class=""kart"">
<p class=""kod"">16GB DDR5 5600MHz (1x16GB) SO-DIMM, 2 slot, max 64GB</p>
<p>Tek modül takılı, bir yuva boş. Şu an tek kanal. Aynı özellikte bir 16 GB SODIMM ekleyince 32 GB çift kanala çıkar.
“5600MHz” aslında 5600 MT/s'dir; işlemci daha düşük hız destekliyorsa o hızda çalışır.</p>
</div>
<div class=""kart"">
<p class=""kod"">16GB LPDDR5X 7467MHz Onboard</p>
<p>Lehimli. Çok hızlı ama asla yükseltilemez. Bu bilgisayar ömrü boyunca 16 GB kalacak; yoğun kullanımda 32 GB'lık sürümü seçin.</p>
</div>
<div class=""kart"">
<p class=""kod"">8GB (8GB onboard + 1 boş slot) DDR4 3200MHz</p>
<p>Karma yapı. Bir 8 GB SODIMM ekleyip 16 GB çift kanal yapmak ilk iş olmalı.</p>
</div>
<div class=""kart"">
<p class=""kod"">32GB (2x16GB) DDR5 6000MHz CL30, XMP/EXPO</p>
<p>Masaüstü için iki modüllü kit; çift kanal hazır. 6000 hızını almak için BIOS'ta XMP/EXPO açılmalıdır. 4 yuvalı anakartta
iki yuva boş kalır, ama DDR5'te ileride 4 modüle çıkmak hız kaybettirir; büyümek için 2x32 kit düşünülür.</p>
</div>
<div class=""kart uyari"">
<p class=""kod"">“16GB RAM” (başka ayrıntı yok)</p>
<p>Belirsiz ilan. Lehimli mi, kaç modül, kaç yuva — satıcıya sorun ya da tam model numarasıyla üreticinin belgesine bakın.</p>
</div>

<h2 id=""yeni-sorular"">7. Satıcıya sorulacak sorular</h2>
<ol class=""adimlar"">
<li>Tam model numarası nedir? (Ör. “83LK” gibi; pazarlama adı yetmez, aynı adın farklı yapılandırmaları olur.)</li>
<li>RAM lehimli mi, yuvaya takılı mı? Kaç yuva var, kaçı boş?</li>
<li>Takılı RAM kaç modülden oluşuyor (1x16 mı, 2x8 mi)?</li>
<li>Bellek türü ve hızı nedir (DDR4/DDR5/LPDDR5X, kaç MT/s)?</li>
<li>Üreticiye göre desteklenen azami RAM nedir?</li>
<li>RAM yükseltmesi garantiyi etkiler mi? Kapakta garanti etiketi var mı?</li>
<li>Bilgisayarı sizden daha fazla RAM ile yapılandırılmış olarak almak mümkün mü, fiyatı nedir?</li>
</ol>

<h2 id=""yeni-sirket"">8. Şirket alımları için öneriler</h2>
<ul class=""liste"">
<li>Standart ofis bilgisayarlarında <b>16 GB ve yükseltilebilir</b> (en az bir boş yuva ya da 2 yuvalı) model seçmek, 3–5 yıllık
kullanım boyunca esneklik sağlar.</li>
<li>Toplu alımda tüm cihazların aynı modelden olması, yedek RAM stoklamayı ve yükseltmeyi kolaylaştırır.</li>
<li>Bu programın <span class=""kod"">--csv</span> seçeneğiyle tüm bilgisayarların RAM durumunu tek tabloda toplayıp hangi makinelere
ek modül gerektiğini önceden planlayabilirsiniz.</li>
<li>Kiralık/leasing cihazlarda yükseltme izni sözleşmede yazar; önce kontrol edin.</li>
</ul>
");
            return sb.ToString();
        }

        // =====================================================================
        //  SÖZLÜK
        // =====================================================================
        private static readonly string[,] Terimler = new string[,]
        {
            { "RAM", "Rastgele erişimli bellek. Açık programların ve verilerin çalışırken tutulduğu hızlı, geçici bellek. Elektrik kesilince içeriği silinir." },
            { "DDR", "Double Data Rate: her saat çevriminde iki veri aktarımı yapan bellek türü. Nesiller: DDR3, DDR4, DDR5. Nesiller birbirine takılmaz." },
            { "DDR3L", "DDR3'ün 1,35 V'luk düşük voltajlı sürümü. DDR3L isteyen dizüstülere 1,5 V'luk DDR3 takılmaz." },
            { "LPDDR", "Low Power DDR (LPDDR4X, LPDDR5, LPDDR5X). Az enerji harcar, çok hızlıdır; neredeyse her zaman anakarta lehimlidir." },
            { "DIMM", "Masaüstü bilgisayarlarda kullanılan uzun bellek modülü (DDR4/DDR5: 288 pin, DDR3: 240 pin)." },
            { "SODIMM", "Small Outline DIMM. Dizüstü, mini PC ve All-in-One bilgisayarlarda kullanılan kısa modül (DDR5: 262, DDR4: 260, DDR3: 204 pin)." },
            { "UDIMM", "Unbuffered DIMM. Normal masaüstü bilgisayarlarda kullanılan, sinyal tamponu olmayan modül." },
            { "RDIMM", "Registered DIMM. Komut/adres sinyallerini tamponlayan sunucu modülü; çok büyük kapasitelere izin verir. Normal PC'de çalışmaz." },
            { "LRDIMM", "Load-Reduced DIMM. Veri sinyallerini de tamponlayan, en büyük kapasiteli sunucu modülü." },
            { "CUDIMM / CSODIMM", "Üzerinde saat sürücüsü (CKD) bulunan DDR5 modülü. Çok yüksek hızlarda daha kararlı çalışır; yeni Intel platformlarında kullanılır." },
            { "CAMM2 / LPCAMM2", "JEDEC'in yeni, anakarta yatay vidalanan bellek modülü standardı. LPCAMM2, LPDDR5X'i değiştirilebilir hale getirir." },
            { "Lehimli bellek", "Anakarta doğrudan lehimlenmiş bellek yongaları (soldered / onboard). Değiştirilemez, eklenemez." },
            { "MT/s", "Saniyede milyon aktarım. RAM hızının doğru birimi. DDR4-3200 = 3200 MT/s." },
            { "MHz", "Saat frekansı. DDR bellekte gerçek saat, MT/s değerinin yarısıdır (3200 MT/s = 1600 MHz). Mağazalar MT/s değerini MHz diye yazar." },
            { "CL (CAS gecikmesi)", "Okuma komutundan verinin gelmesine kadar geçen saat çevrimi sayısı. Gerçek gecikme ≈ CL × 2000 / MT/s (ns)." },
            { "tRCD, tRP, tRAS", "CL'den sonraki ana zamanlamalar (satır açma, satır kapama, satırın açık kalma süresi). Ürünlerde “16-18-18-38” gibi yazılır." },
            { "JEDEC", "Bellek standartlarını belirleyen kuruluş. “JEDEC hızı”, modülün her sistemde ayarsız çalıştığı standart hızdır." },
            { "SPD", "Serial Presence Detect: modül üzerindeki, kapasite, hız ve zamanlama bilgilerini taşıyan küçük yonga. BIOS RAM'i bununla tanır." },
            { "XMP", "Intel Extreme Memory Profile. SPD'de saklanan, JEDEC'ten hızlı üretici profili. BIOS'ta elle açılır." },
            { "EXPO", "AMD Extended Profiles for Overclocking. AMD platformları için XMP benzeri hız profili." },
            { "Kanal", "Bellek denetleyicisi ile modüller arasındaki bağımsız 64 bitlik veri yolu. Çoğu PC işlemcisinde iki kanal vardır." },
            { "Çift kanal", "İki kanalın her birine modül takılarak bant genişliğinin yaklaşık iki katına çıkarılması." },
            { "Alt kanal (DDR5)", "Her DDR5 modülü iki adet 32 bitlik bağımsız alt kanala ayrılır. Tek modül yine de çift kanal değildir." },
            { "Flex Mode", "Intel'in farklı kapasiteli modüllerde ortak kısmı çift kanal, fazlasını tek kanal çalıştırması (ör. 8 + 16 GB)." },
            { "Rank", "Modül üzerinde aynı anda erişilen 64 bitlik yonga grubu. 1R = tek rank, 2R = çift rank." },
            { "Yoğunluk", "Tek bir bellek yongasının kapasitesi (ör. 16 Gbit). Eski platformlar yüksek yoğunluklu modülleri tanımayabilir." },
            { "Kit", "Birlikte test edilip paketlenmiş eş modül seti (ör. 2x16 GB). Çift kanal için en güvenli seçenek." },
            { "ECC", "Error Correcting Code: tek bitlik hataları düzelten bellek. Ek veri bitleri taşır (DDR4 ECC: 72 bit; DDR5 ECC UDIMM/SODIMM: 2x36 = 72 bit; DDR5 RDIMM: genelde 2x40 = 80 bit). Sunucu ve iş istasyonlarında kullanılır." },
            { "On-die ECC", "DDR5 yongalarının içindeki hata düzeltme. Yalnızca yonga içini korur; gerçek (uçtan uca) ECC değildir." },
            { "PMIC", "Power Management IC. DDR5 modülü üzerindeki voltaj düzenleyici; DDR4'te bu iş anakarttaydı." },
            { "Memory training", "Açılışta anakartın modüller için en uygun zamanlamaları ölçmesi. DDR5'te ilk açılışta birkaç dakika sürebilir." },
            { "Taahhüt (Commit)", "Programların ayırdığı toplam sanal bellek. Sınırı = fiziksel RAM + sayfa dosyası. Sürekli RAM'den büyükse RAM yetmiyordur." },
            { "Sayfa dosyası", "pagefile.sys: RAM yetmediğinde az kullanılan verilerin taşındığı disk alanı. Diskten çalışmak RAM'den çok yavaştır." },
            { "Bellek sıkıştırma", "Windows'un RAM dolmaya başlayınca az kullanılan sayfaları sıkıştırarak RAM'de tutması. Görev Yöneticisi'nde “Sıkıştırılmış” olarak görünür." },
            { "Önbellek (Cached)", "Windows'un boş RAM'i disk verilerini hızlandırmak için kullanması. Gerektiğinde hemen programlara verilir; “boşa harcanmış” değildir." },
            { "Donanıma ayrılmış", "BIOS, dahili grafik veya cihazlar için ayrılan, Windows'un kullanamadığı RAM. Takılı ile kullanılabilir RAM arasındaki farktır." },
            { "Paylaşılan GPU belleği", "Dahili veya harici grafik işlemcinin gerektiğinde kullanabileceği sistem RAM'i üst sınırı. Sürekli ayrılmış değildir." },
            { "UMA Frame Buffer", "Özellikle AMD sistemlerde BIOS'tan dahili grafiğe sabit ayrılan bellek miktarı." },
            { "SMBIOS", "Bilgisayarın donanım bilgilerini (model, yuvalar, modüller) işletim sistemine bildiren BIOS tablosu. Bu program buradan okur; içerik üreticinin doğru doldurmasına bağlıdır." },
            { "Bant genişliği", "Saniyede taşınabilen veri miktarı. Kanal başına ≈ MT/s × 8 bayt (DDR4-3200 ≈ 25,6 GB/sn)." },
            { "Hız aşırtma (OC)", "Bileşeni standart değerlerinin üstünde çalıştırmak. XMP/EXPO da bir tür onaylı hız aşırtmadır." },
        };

        public static string SozlukHtml()
        {
            StringBuilder sb = new StringBuilder(16 * 1024);
            sb.Append(@"
<h1 id=""sozluk"">RAM terimleri sözlüğü</h1>
<p>Ürün ilanlarında, BIOS'ta ve bu programın raporlarında geçen terimlerin kısa açıklamaları. Terimler konularına göre
sıralanmıştır.</p>
<table class=""tablo"" id=""sozluk-tablo"">
<tr><th>Terim</th><th>Açıklama</th></tr>
");
            int n = Terimler.GetLength(0);
            for (int i = 0; i < n; i++)
            {
                sb.Append("<tr><td><b>");
                sb.Append(Html.Ka(Terimler[i, 0]));
                sb.Append("</b></td><td>");
                sb.Append(Html.Ka(Terimler[i, 1]));
                sb.Append("</td></tr>\r\n");
            }
            sb.Append("</table>\r\n");
            sb.Append("<p class=\"not\">Toplam ");
            sb.Append(n);
            sb.Append(" terim.</p>\r\n");
            return sb.ToString();
        }

        // =====================================================================
        //  SIK SORULAN SORULAR
        // =====================================================================
        private static readonly string[,] Sorular = new string[,]
        {
            { "sss-kac-gb", "Kaç GB yeterli?",
              @"2026 için: temel ofis ve web işleri için <b>16 GB</b>, yoğun ofis/çok sekme/sürekli toplantı için <b>16–32 GB</b>,
yazılım geliştirme, sanal makine ve CAD için <b>32 GB</b>, video/3D/büyük veri için <b>64 GB ve üstü</b>. 8 GB artık sadece
çok hafif kullanımda yeter. Kendi ihtiyacınızı ölçmek için bu programın <b>Kullanım (canlı)</b> sekmesini en yoğun
çalıştığınız sırada açık bırakın ve zirve değere bakın." },

            { "sss-farkli-marka", "Farklı marka RAM takabilir miyim?",
              @"Genellikle evet. Tür, form faktörü, voltaj aynıysa farklı marka modüller çoğunlukla birlikte çalışır. Ama nadiren
açılmama, mavi ekran ya da hızın daha da düşmesi görülebilir. En güvenlisi var olan modülle <b>aynı parça numarasını</b>
almak ya da tüm modülleri bir kit olarak değiştirmektir. İade hakkınız olan bir yerden alın." },

            { "sss-8-16", "8 GB + 16 GB olur mu?",
              @"Olur, toplam 24 GB görürsünüz. Intel'de “Flex Mode” sayesinde 16 GB'lık kısım (8+8) çift kanal, kalan 8 GB tek kanal
çalışır; AMD de benzer şekilde davranır. Eş modüllere (8+8 ya da 16+16) göre biraz daha yavaştır ama hiç eklememekten
iyidir. Mümkünse eş kapasite seçin." },

            { "sss-hizli-yavas", "Hızlı RAM'i yavaş bir bilgisayara takabilir miyim?",
              @"Evet. Modül, sistemin desteklediği en yüksek hızda (genelde JEDEC standardında) çalışır. Örneğin DDR4-3200 bir modül,
DDR4-2666 destekleyen bir bilgisayarda 2666'da çalışır. Ama hızlı modüle fazladan para vermek boşunadır; nesil ve form
faktörü yine aynı olmalıdır (DDR5 modül DDR4 yuvasına takılmaz)." },

            { "sss-ddr4-ddr5", "DDR4'ü DDR5 yuvasına (ya da tersini) takabilir miyim?",
              @"Hayır. Çentik yerleri farklıdır, modül yuvaya girmez; zorlarsanız yuva ve modül zarar görür. Voltaj ve sinyal yapısı da
farklıdır. Anakartınız hangi nesli destekliyorsa ancak onu takabilirsiniz." },

            { "sss-masaustu-dizustu", "Dizüstüme masaüstü RAM'i takılır mı?",
              @"Hayır. Dizüstüler kısa <b>SODIMM</b>, masaüstüler uzun <b>DIMM</b> kullanır; boyları ve pin sayıları farklıdır.
Mini PC ve All-in-One bilgisayarlar da çoğunlukla SODIMM kullanır. Ürün adında “SO-DIMM”, “Notebook” veya “Laptop”
ifadesini arayın." },

            { "sss-gorunmuyor", "RAM'i taktım ama Windows göremiyor. Ne yapmalıyım?",
              @"Sırayla deneyin: (1) Modülü çıkarıp yeniden, “klik” sesi gelene kadar sağlamca takın; dizüstüde iki klips de oturmalı.
(2) Masaüstünde doğru yuvaları kullandığınızdan emin olun (genelde A2 ve B2). (3) BIOS'a girip toplam belleği görüyor mu bakın;
BIOS görmüyorsa sorun donanımdadır. (4) Yeni modülü tek başına takıp deneyin: açılmıyorsa modül uyumsuz ya da arızalıdır.
(5) BIOS'u güncelleyin; yeni kapasiteli modüller için gerekebilir. (6) Windows'ta <span class=""kod"">msconfig</span> → Önyükleme →
Gelişmiş seçenekler → “En fazla bellek” kutusunun işaretli olmadığından emin olun. (7) 32 bit Windows kullanıyorsanız
4 GB'tan fazlası zaten görülmez." },

            { "sss-eksik-gosteriyor", "Görev Yöneticisi takılı RAM'den daha az gösteriyor. Neden?",
              @"Görev Yöneticisi'ndeki “Donanıma ayrılmış” miktar, BIOS'un dahili grafik ve cihazlar için ayırdığı bellektir. Örneğin
16 GB takılı bir dizüstüde 15,7 GB kullanılabilir görmek normaldir. AMD dahili grafikli sistemlerde BIOS'ta grafiğe 2–4 GB
ayrılmışsa fark daha büyük olur (BIOS'taki “UMA Frame Buffer” ayarı). Fark çok büyükse (ör. 8 GB takılı, 4 GB görünüyor)
32 bit Windows, msconfig'teki bellek sınırı ya da yerine oturmamış bir modül olabilir." },

            { "sss-bios-uretici", "BIOS (veya bu program) 16 GB azami diyor ama üretici 32 GB diyor. Hangisi doğru?",
              @"Genellikle <b>üreticinin teknik belgesi</b> daha güvenilirdir. BIOS'un SMBIOS tablosuna yazdığı “azami kapasite” ve
“yuva sayısı” bilgileri üretici tarafından elle doldurulur ve sık sık eksik ya da yanlıştır: bazı dizüstüler yalnızca
<b>dolu yuvaları</b> listeler, azami kapasiteyi de takılı RAM kadar gösterir. İşlemcinin desteklediği azami bellek
(Intel/AMD ürün sayfası) üst sınırı verir. Kesin karar için model numaranızla üreticinin belgesine (Lenovo PSREF, Dell
teknik özellikler, HP QuickSpecs) bakın; gerekiyorsa alt kapağı açıp yuvaları gözle sayın." },

            { "sss-kanal-anlama", "Tek kanal mı, çift kanal mı çalıştığımı nasıl anlarım?",
              @"Bu programın <b>Özet</b> sekmesindeki yuva haritası ve durum satırı bunu gösterir. Kabaca: tek modül takılıysa tek
kanal çalışırsınız (DDR5 dahil — modül içindeki iki alt kanal çift kanal sayılmaz). İki modül farklı kanallara (yuva adında
ChannelA / ChannelB, DIMM_A / DIMM_B gibi) takılıysa çift kanaldır. Görev Yöneticisi “Kullanılan yuvalar” sayısını gösterir
ama kanal bilgisini doğrudan vermez." },

            { "sss-ram-ssd", "Bilgisayarım yavaş. RAM mi almalıyım, SSD mi?",
              @"Önce diske bakın: Görev Yöneticisi → Performans → Disk'te tür <b>HDD</b> yazıyorsa ve disk sık sık %100'de duruyorsa,
<b>SSD</b> en büyük farkı yaratır — açılış, program açma ve genel tepki hızı birkaç kat artar. Zaten SSD varsa ve bellek
kullanımı yoğun anlarda %85–90'ı aşıyorsa <b>RAM</b> ekleyin. İkisi de yetersizse önce SSD, sonra RAM." },

            { "sss-xmp-acilmiyor", "XMP/EXPO'yu açınca bilgisayar açılmıyor. Ne yapmalıyım?",
              @"Birkaç dakika bekleyin; DDR5'te yeni ayarla ilk açılış (memory training) uzun sürer. Yine açılmazsa çoğu anakart birkaç
denemeden sonra güvenli ayarlara döner. Dönmezse <b>CMOS'u sıfırlayın</b> (anakart kılavuzundaki “Clear CMOS” düğmesi/köprüsü
ya da güç kablosu çıkarılmışken pili birkaç dakika çıkarmak). Sonra BIOS'u güncelleyip tekrar deneyin ya da XMP'nin daha
düşük hızlı profilini seçin. Dört modül takılıysa yüksek XMP hızları çoğu zaman kararlı olmaz." },

            { "sss-lehimli", "Lehimli RAM'i değiştirebilir veya artırabilir miyim?",
              @"Pratikte hayır. Lehimli bellek yongaları anakartın parçasıdır; değiştirmek özel ekipman gerektirir, garantiyi bitirir ve
çoğu zaman BIOS da yeni kapasiteyi tanımaz. Bilgisayarda lehimli belleğe ek olarak boş bir SODIMM yuvası varsa (karma yapı),
o yuvaya modül eklenebilir. Tamamen lehimliyse tek çözüm daha fazla RAM'li bir bilgisayardır." },

            { "sss-tek-yuva-gorunuyor", "Bu program (veya Görev Yöneticisi) tek yuva gösteriyor ama bilgisayarımda iki yuva olduğunu duydum.",
              @"Mümkün. Bazı üreticilerin BIOS'ları boş yuvaları SMBIOS tablosuna hiç yazmaz; bu durumda yazılımlar yalnızca dolu yuvayı
görür (“1/1”). Üreticinin teknik belgesinde “2x SO-DIMM” yazıyorsa ikinci yuva vardır ve boştur. Alt kapağı açıp gözle de
kontrol edebilirsiniz. Bu program böyle şüpheli durumları <b>uyarı</b> olarak gösterir." },

            { "sss-hiz-dusuk", "RAM'im 5600 ama 4800'de çalışıyor. Arızalı mı?",
              @"Hayır. Bilgisayar üreticisi, maliyet ve stok nedeniyle işlemcinin desteklediğinden hızlı bir modül takmış olabilir;
modül işlemcinin/anakartın izin verdiği hızda (ör. 4800) çalışır. Bu tamamen normaldir. Masaüstünde XMP/EXPO açılmadığında da
modül JEDEC hızında çalışır. Ek modül alırken var olanın <b>çalıştığı</b> hızdan düşük olmayan bir modül yeterlidir." },

            { "sss-ram-temizleyici", "“RAM temizleyici” / “RAM hızlandırıcı” programlar işe yarar mı?",
              @"Hayır. Windows boş RAM'i önbellek olarak zaten akıllıca kullanır; bu programlar önbelleği boşaltıp sistemi kısa süreliğine
daha da yavaşlatır. Gerçek çözüm gereksiz programları kapatmak ya da RAM eklemektir. Ayrıca şirket politikası gereği
bilinmeyen yazılım kurmayın." },

            { "sss-sayfa-dosyasi", "Sayfa dosyasını (sanal belleği) kapatmak veya büyütmek RAM eksikliğini çözer mi?",
              @"Hayır. Sayfa dosyası RAM'in yerini tutmaz, sadece yetmediğinde çökmeyi önler; diskten çalışmak çok yavaştır. Sayfa
dosyasını kapatmak ise bellek dolduğunda programların çökmesine yol açabilir. Windows'un “sistem tarafından yönetilen”
varsayılan ayarında bırakın." },

            { "sss-windows-sinir", "Windows'un desteklediği en fazla RAM ne kadar?",
              @"64 bit Windows 11 Home 128 GB; Pro ve Education 2 TB, Pro for Workstations ve Enterprise 6 TB'a kadar destekler. 32 bit Windows yaklaşık 4 GB ile sınırlıdır
(kullanılabilir kısım 3–3,5 GB). Pratikte sınırı neredeyse her zaman işlemci ve anakart belirler." },

            { "sss-mac-tablet", "Tabletime veya ince dizüstüme RAM ekleyebilir miyim?",
              @"Çoğunlukla hayır. Tabletler, 2'si 1 arada cihazlar ve LPDDR bellekli ince dizüstüler lehimli bellek kullanır. Teknik
özelliklerde “SO-DIMM” veya “slot” yazmıyorsa yükseltme yoktur. Bu program Özet sekmesinde belleğin lehimli olduğunu tahmin
ederse yuva haritasında lehimli olarak gösterir." },
        };

        public static string SssHtml()
        {
            StringBuilder sb = new StringBuilder(24 * 1024);
            sb.Append(@"
<h1 id=""sss"">Sık sorulan sorular</h1>
<div class=""kart"">
<h3 id=""sss-icindekiler"">Sorular</h3>
<ol class=""adimlar"">
");
            int n = Sorular.GetLength(0);
            for (int i = 0; i < n; i++)
            {
                sb.Append("<li><a href=\"#");
                sb.Append(Sorular[i, 0]);
                sb.Append("\">");
                sb.Append(Html.Ka(Sorular[i, 1]));
                sb.Append("</a></li>\r\n");
            }
            sb.Append("</ol>\r\n</div>\r\n");

            for (int i = 0; i < n; i++)
            {
                sb.Append("<div class=\"kart\">\r\n<h3 id=\"");
                sb.Append(Sorular[i, 0]);
                sb.Append("\">");
                sb.Append(Html.Ka(Sorular[i, 1]));
                sb.Append("</h3>\r\n<p>");
                // Cevaplar güvenilir, elle yazılmış HTML parçalarıdır (b, span.kod).
                sb.Append(Sorular[i, 2].Replace("\r\n", " ").Replace("\n", " "));
                sb.Append("</p>\r\n</div>\r\n");
            }
            return sb.ToString();
        }
    }
}
