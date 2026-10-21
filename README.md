# RAM Danışmanı (RamChecker.exe)

Bir bilgisayardaki RAM'in tüm ayrıntılarını (dolu ve boş yuvalar, tür, form, hız, voltaj, parça no) gösterir ve
**bu makineye özel** "hangi RAM'i almalıyım, nasıl yükseltirim" cevabını verir. Genel bir RAM alma rehberi de içerir.

- Tek dosya: `RamChecker.exe`. Kurulum yok, yönetici izni gerekmez.
- İnternete bağlanmaz, hiçbir veri göndermez. Bilgiyi BIOS'tan (SMBIOS) ve Windows'tan (WMI) okur.
- 3. taraf bileşen yoktur; Windows'taki yerleşik .NET Framework 4 ile çalışır.

## Kullanım

`RamChecker.exe` dosyasına çift tıklayın. Sekmeler:

| Sekme | İçerik |
|---|---|
| **Özet** | Tek cümlelik cevap (yükseltilebilir mi, ne alınmalı), toplam/yuva/tür/hız/azami kapasite/kanal kartları, yuva haritası, bulgular |
| **Modüller** | Her yuvanın tüm ayrıntıları (üretici, parça no çözümü, seri no, rank, voltaj, genişlik) |
| **Yükseltme ve Satın Alma** | Alınacak modülün şartnamesi, mağazada aranacak metin, seçenekler, satın almadan önce kontrol listesi, takma adımları |
| **Kullanım (canlı)** | Anlık bellek kullanımı, taahhüt, en çok bellek kullanan programlar, "RAM gerçekten gerekli mi?" analizi |
| **RAM Alırken Dikkat / Yeni Bilgisayar / Sözlük ve SSS** | Genel Türkçe rehber |

Üst menüden rapor HTML olarak kaydedilebilir, tarayıcıda açılabilir ya da metin/alışveriş listesi panoya kopyalanabilir.

## Komut satırı

| Seçenek | Ne yapar |
|---|---|
| `--metin` | Raporu düz metin olarak yazar |
| `--html <dosya>` | Tam raporu HTML dosyasına kaydeder |
| `--csv <dosya>` | CSV dosyasına bu bilgisayar için bir satır ekler (dosya yoksa başlıkla oluşturur) |
| `--smbios <dosya>` | Canlı okuma yerine kayıtlı SMBIOS dökümünü kullanır (diğer seçeneklerle ve pencereyle birleşir) |
| `--smbios-kaydet <dosya>` | Bu bilgisayarın SMBIOS tablosunu dosyaya kaydeder (destek / sonradan inceleme için) |
| `--test` | Gömülü testleri çalıştırır (çıkış kodu 0 = hepsi geçti) |
| `--ekran <klasör>` | Pencereyi açar, her sekmenin PNG görüntüsünü kaydeder ve kapanır (yalnız `--smbios` ile birleşir) |
| `--yardim` | Yardım |

Çıkış kodları: `0` başarılı, `1` hata, `2` hatalı kullanım.

**Önemli:** Bu bir pencere programıdır. PowerShell ve etkileşimli cmd istemi, çıktı boruya bağlanmadıkça bitmesini
**beklemez** ve çıkış kodunu almaz:

```powershell
& .\RamChecker.exe --metin | Out-String
& .\RamChecker.exe --csv C:\temp\ram.csv | Out-Null; $LASTEXITCODE
(Start-Process .\RamChecker.exe -ArgumentList '--csv','C:\temp\ram.csv' -Wait -PassThru).ExitCode
```

cmd isteminde `start /wait RamChecker.exe ...` kullanın; `.bat` dosyası içinde gerekmez.

### 150 PC'den ağ paylaşımına envanter toplama

1. `RamChecker.exe`'yi herkesin okuyabildiği bir paylaşıma koyun, ör. `\\sunucu\araclar\RamChecker.exe`.
2. Sonuç klasörüne (ör. `\\sunucu\envanter\`) "Domain Users" için **yazma** izni verin.
3. GPO > Kullanıcı Yapılandırması > Windows Ayarları > Komut Dosyaları > **Oturum Açma** altına şu `ram-envanter.bat`'ı ekleyin:

```bat
@echo off
rem Her oturum açılışında bu bilgisayar için ram-envanteri.csv'ye bir satır ekler.
"\\sunucu\araclar\RamChecker.exe" --csv "\\sunucu\envanter\ram-envanteri.csv"
exit /b 0
```

- Aynı anda yazan PC'ler sırayla bekler (dosya kilidi, 10 deneme).
- Dosya biri tarafından **Excel'de açık** tutulursa kilit açılmaz; satır kaybolmasın diye aynı klasöre
  `ram-envanteri.bekleyen-<BİLGİSAYARADI>.csv` yazılır. Ana dosyayı inceledikten sonra Excel'i kapatın;
  bir sonraki başarılı oturumda bekleyen dosya silinir. Excel'de incelemek için dosyanın **kopyasını** açmak en iyisidir.
- CSV UTF-8 (BOM'lu) ve `;` ayraçlı; Türkçe Excel doğrudan açar.
- Bir PC her oturumda yeni satır ekler; Excel'de bilgisayar adına göre "yinelenenleri kaldır" ile en son satırı tutun.

### Sorunlu bir PC'yi uzaktan incelemek

```bat
RamChecker.exe --smbios-kaydet pc123.bin
```

Dosyayı kendi makinenizde açın: `RamChecker.exe --smbios pc123.bin` (pencere) ya da `--smbios pc123.bin --metin`.
Dosya modunda yalnız SMBIOS kullanılır (canlı kullanım ve Windows bilgisi yoktur).

## Derleme

```bat
derle.bat
```

Windows'ta yerleşik gelen `csc.exe` (.NET Framework 4, `C:\Windows\Microsoft.NET\Framework64\v4.0.30319`) kullanılır;
Visual Studio ya da SDK gerekmez. Çıktı: `RamChecker.exe`.

> **Uyarı:** Bu derleyici yalnız **C# 5** destekler. `$"..."` dize enterpolasyonu, `?.`, `=>` gövdeli üyeler,
> `nameof`, `out var`, tuple vb. **kullanmayın**; derleme hata verir.

Değişiklikten sonra mutlaka: `derle.bat` (hatasız/uyarısız) ve `RamChecker.exe --test | Out-String` (Kalan: 0).

## Dosya yapısı (`src\`)

| Dosya | Görev |
|---|---|
| `Model.cs` | Ortak veri tipleri (Envanter, BellekYuvasi, DanismanSonucu ...) ve biçim yardımcıları |
| `Smbios.cs` | SMBIOS tablosu okuma ve ayrıştırma (tip 1/2/3/4/16/17), kod tabloları |
| `Toplayici.cs` | SMBIOS + WMI birleştirme, canlı kullanım, kasa/sanal makine, lehimli bellek sezgisi |
| `Ureticiler.cs` | JEDEC üretici kodları ve marka parça numarası çözücüleri |
| `Platform.cs` | İşlemci adı → bellek denetleyicisi sınırları (tür, hız, kanal, azami kapasite) |
| `Danisman.cs` | Öneri motoru: yuva/kanal/hız analizi, yükseltme senaryoları, satın alma şartnamesi, takma adımları |
| `Rehber.cs` | Statik Türkçe rehber metinleri (HTML) |
| `Rapor.cs` | HTML, düz metin ve CSV raporları |
| `AnaForm.cs`, `Program.cs` | Arayüz (WinForms + WebBrowser) ve komut satırı |
| `*Testleri.cs`, `Testler.cs` | Gömülü testler (`--test`) |
| `app.manifest`, `ram.ico` | Uygulama bildirimi ve simgesi |

`testler\veri\` altında gerçek cihazlardan alınmış SMBIOS dökümleri bulunur (ör. `lenovo-loq-15iax9e-83LK.bin`).
Dökümler anonimleştirilmiştir: seri numaraları sahte değerlerle değiştirilmiş (`TEST0001` gibi), sistem UUID'si sıfırlanmıştır.
Yeni döküm eklerken (`--smbios-kaydet`) aynısını yapın; tip 1/2/3 seri no, UUID ve tip 17 modül seri no cihazı tanımlar.

## Bilinen sınırlamalar

- **BIOS bilgisi güvenilir olmayabilir.** Birçok BIOS (özellikle Lenovo) boş yuvayı hiç listelemez ve azami kapasiteyi
  takılı miktar kadar bildirir. Program bunu fark edince "yuva sayısı doğrulanmalı" der ve muhtemel yuvayı kesikli kutuyla
  gösterir; satın almadan önce üretici belgesinden (Lenovo PSREF, Dell/HP servis kılavuzu) ya da kapağı açarak doğrulayın.
- **Azami kapasite** kartındaki büyük rakam pratik sınırdır (yuva sayısı x güvenli modül boyutu); altında işlemcinin
  (ya da güvenilirse BIOS'un) üst sınırı yazar. Daha büyük modül (ör. 48/64 GB DDR5) desteği BIOS sürümüne bağlıdır.
- **İşlemci tablosu aile bazındadır;** çok yeni ya da nadir işlemcilerde "bulunamadı" ya da "tahmini" görünebilir.
  Değerler Intel ARK / AMD belgelerine göre girilmiştir ama her model tek tek doğrulanmamıştır.
- Lehimli/LPCAMM2 ayrımı, form faktörünü belirsiz bildiren BIOS'larda kesin değildir; program bu durumda "doğrulayın" der.
- Boyutu BIOS'ta "bilinmiyor" olan modülde parça numarası çözülemezse öneri üretilmez.
- Yalnız Windows (7 SP1 ve sonrası, .NET Framework 4.x) için yazılmıştır. Sanal makinelerde fiziksel RAM önerisi yapılmaz.
"# RamChecker" 
