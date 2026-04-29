# VS Translator (EN -> TR)

Visual Studio 2022 / 2026 Insiders icin editorde secili Ingilizce metni Turkceye ceviren extension.

## Ozellikler

- Editorde metin sec, sag tikla **"Turkceye Cevir"** ya da **Ctrl+Shift+T** kisayoluna bas.
- Ceviri sonucu:
  - **Output** penceresinde "VS Translator" panelinde gosterilir (kaynak + ceviri birlikte, gecmis kalir),
  - **panoya** kopyalanir (istedigin yere yapistir),
  - **status bar**'da kisa bildirim gorulur.
- Secili metni degistirmez (yikici degil).

## Cevirinin Kaynagi

[MyMemory API](https://mymemory.translated.net/doc/spec.php) — ucretsiz, anahtar gerektirmez. Anonim kullanici icin gunluk **5000 karakter** limiti vardir; bunu **50.000 karaktere** cikarmak icin `TranslationService.cs` icindeki istek URL'sine `&de=mail@adres.com` parametresi eklenebilir.

500 karakterden uzun secimler API limitine takilmamasi icin otomatik olarak parcalanir (cumle/bosluk sinirinda).

## Gereksinimler

- Visual Studio 2022 (17.0+) veya VS 2026 Insiders
- "Visual Studio extension development" workload'i (Visual Studio Installer'dan)
- .NET Framework 4.7.2 Targeting Pack

## Build & Test

```
1. VsTranslator.sln dosyasini Visual Studio'da ac.
2. NuGet paketleri otomatik restore olur (Microsoft.VSSDK.BuildTools, Microsoft.VisualStudio.SDK).
3. Build > Build Solution (Ctrl+Shift+B).
4. F5 ile experimental instance'da debug et.
   - Yeni VS penceresi acilir, herhangi bir kod dosyasinda metin sec, Ctrl+Shift+T'ye bas.
```

## Kurulum (Yayindan sonra)

`bin\Release\VsTranslator.vsix` dosyasi olusturulur. Cift tikla, VSIX Installer extension'i kurar.

## Bilinen Sinirlar

- Sadece **EN -> TR**. Iki yonlu / cok dilli istersen `TranslateCommand.ExecuteAsync` icindeki `"en", "tr"` parametrelerini degistir veya yeni bir komut ekle.
- MyMemory ucretsiz katmaninda kalite makine cevirisidir; kritik metinlerde DeepL/Azure Translator gibi profesyonel bir API'ye gecilebilir.
- Klasik VSIX kullanildigi icin extension VS surecinin icinde calisir; cok agir senaryolarda yeni `VisualStudio.Extensibility` SDK'sina (out-of-process) tasimak dusunulebilir.

## Dosya Yapisi

```
VsTranslator.sln
VsTranslator/
  VsTranslator.csproj            # Klasik VSIX msbuild projesi (NET 4.7.2)
  source.extension.vsixmanifest  # [17.0,) acik ust sinir -> VS2022 + VS2026
  TranslatorPackage.cs           # AsyncPackage giris noktasi
  TranslatorPackage.vsct         # Komut, grup, kisayol tanimlari
  Commands/
    TranslateCommand.cs          # Editor secimi -> servis -> Output/Clipboard
  Services/
    TranslationService.cs        # MyMemory HTTP istemcisi, chunking
  Properties/
    AssemblyInfo.cs
```
