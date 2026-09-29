# EyeCare Crimson

Bilgisayar başında saatlerce oturanlar ve oyun oynayanlar için ultra hafif, sade ve AMOLED koyu kırmızı temalı bir göz/duruş takip uygulaması. 

Electron tabanlı hantal uygulamalar gibi 400-500 MB RAM ve arka planda GPU tüketmez. 

Yerel .NET WPF ile yazıldığı için arkada çalışırken sıfır GPU ve neredeyse sıfır CPU harcar, oyunlarda FPS düşürmez.

## Neler Var?

- **İki Farklı Mod (Ana ekrandan anında geçiş):**
  - **1 Saatlik Seans:** Maksimum 1 saat oturma limiti (30, 45, 60, 90 dk seçilebilir). Süre dolunca mola uyarısı verir.
  - **20-20-20 Modu:** Her 20 dakikada bir, 20 saniye boyunca 6 metre (20 feet) uzağa bakıp göz kaslarını gevşetmek için hızlı geri sayım yapar.
- **Aşırı Meşguliyet Butonları:** Oyundayken ya da acil bir işin ortasındayken tek tıkla `+5 Dk` veya `+10 Dk` erteleyebilirsiniz.
- **Oyun Dostu Bildirim (Focus-Free):** Uyarı penceresi Windows'un `WS_EX_NOACTIVATE` bayrağını kullanır; yani tam ekran bir oyunu asla alta almaz (alt-tab yapmaz) ve yazı yazarken klavye odağınızı çalmaz.
- **Akıllı Boşta Kalma (Idle) Algılama:** Masadan kalkıp gittiğinizde süreyi durdurur. 5 dakikadan uzun süre uzaktaysanız bunu mola sayarak oturumu otomatik sıfırlar.
- **Ekrana Sabitleme (Pin):** Sağ üstteki raptiye butonuyla pencereyi tüm pencerelerin en üstünde tutabilirsiniz.
- **AMOLED Siyah & Koyu Kırmızı Tasarım:** Saf siyah arka plan ve derin kırmızı detaylar.
- **Sistem Tepsisi (Tray):** Kapatıldığında arka planda görev çubuğunda sessizce çalışmaya devam eder.

## Derleme ve Çalıştırma

Projeyi çalıştırmak için sisteminizde .NET 10 SDK bulunması yeterlidir:

```bash
# Projeyi başlatmak için
dotnet run

# Tek parça .exe olarak derlemek için
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
```

Derleme sonrası `publish/eyecarebyzewzack.exe` dosyasını doğrudan çalıştırabilirsiniz.
