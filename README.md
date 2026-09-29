# 👁 EyeCare ZewZack - Ultra-Minimal Göz & Duruş Takipçisi

Bilgisayar başında uzun süre çalışanlar, yazılımcılar ve oyuncular için özel olarak tasarlanmış; **sıfıra yakın CPU, GPU ve RAM yüküyle** çalışan modern ve minimalist masaüstü göz sağlığı ve oturma süresi kontrol uygulaması.

---

## ⚡ Neden Farklı? (Sıfır Darboğaz Felsefesi)

- **%0 GPU & ~%0 CPU Yükü:** Ağır Electron/Chromium altyapıları yerine yerel Windows Desktop (.NET 10 WPF + Win32) mimarisi kullanılmıştır. Oyunlarda FPS düşüşü veya render alırken gecikme yaratmaz.
- **Ultra Düşük RAM:** Arka plana / Sistem Tepsisine (System Tray) küçültüldüğünde Windows bellek temizliği tetiklenir ve bellek kullanımı minimuma iner.
- **Oyun ve İş Akışı Dostu (Focus-Free Bildirim):** 1 saat dolduğunda çıkan modern köşe bildirimi Windows'un `WS_EX_NOACTIVATE` bayrağını kullanır. Tam ekran oyunu alta almaz, yazarken veya kodlarken odağınızı çalmaz.
- **Akıllı Boşta Kalma (Idle) Algılama:** Masadan kalkıp kahve almaya veya dinlenmeye gittiğinizde Windows klavye/fare hareketlerini algılar; süre sayacını durdurur. 5 dakikadan uzun süre uzaktaysanız bunu mola sayıp sayacı otomatik sıfırlar.

---

## 🎯 Temel Özellikler

1. **1 Saatlik Maksimum Oturma Limiti:**
   - 0-50 dk: Güvenli yeşil odaklanma modu.
   - 50-60 dk: Yaklaşan mola uyarısı (kehribar rengi gösterge).
   - 60+ dk: Mola zamanı uyarısı (nazik ses ve köşe bildirimi).
2. **Aşırı Meşguliyet / Ek Süre Butonları (+5 Dk / +10 Dk):**
   - Tam bir oyunun ortasındaysanız, render bekliyorsanız veya acil bir işiniz varsa:
     - `+5 Dk Uzat`
     - `+10 Dk Uzat`
     butonlarına tek tıkla basarak süreyi uzatabilirsiniz. Süre bittiğinde uygulama sizi tekrar nazikçe uyarır.
   - 75+ dakika kesintisiz aşımda kritik ergonomi ve göz uyarısı verir.
3. **Göz & Beden Dinlendirme Ekranı:**
   - 5 dakikalık mola geri sayımı.
   - **20-20-20 Kuralı** ve dönüşümlü ergonomi rehberi:
     - *20 saniye boyunca 6 metre uzağa bakarak göz merceğini gevşetme.*
     - *Bilinçli göz kırpma egzersizi.*
     - *Avuç içi dinlendirme (Palming).*
     - *Omuz ve boyun dairesel esneme.*
     - *Su içme hatırlatıcısı.*
4. **Sistem Tepsisi (System Tray) Entegrasyonu:**
   - Görev çubuğu köşesinde durum rengini gösteren dinamik simge (Yeşil, Kehribar, Kırmızı, Mor).
   - Fareyi üzerine getirince kalan süreyi gösteren canlı ipucu.
   - Sağ tık menüsü: Hızlı mola başlat, +5 dk / +10 dk ekle, duraklat, sıfırla, ayarlar.
   - Çarpı (X) butonuna basıldığında arkada çalışmaya devam ederek tepsiyi kullanır.
5. **Gelişmiş Ayarlar:**
   - Hedef oturma süresi (30, 45, 60, 90 dk).
   - Mola süresi (3, 5, 10 dk).
   - Boşta kalma eşiği (2, 3, 5 dk).
   - Sesli uyarı aç/kapa.
   - Windows ile birlikte otomatik başlatma seçeneği.

---

## 🚀 Çalıştırma

### Hazır Tek Dosya (Standalone EXE)
Derlenmiş tek parça çalıştırılabilir dosya:
```text
c:\Users\zewzew\zewzack\antigravity\eyecarebyzewzack\publish\eyecarebyzewzack.exe
```
Bu `.exe` dosyasını doğrudan çift tıklayarak çalıştırabilir veya masaüstünüze kısayol oluşturabilirsiniz.

### Geliştirici Modunda Çalıştırma
```powershell
dotnet run
```

---

## 🎨 Tasarım Dili

- **Palet:** Obsidian Black (`#101217`), Slate (`#1B202D`), Zümrüt Yeşili (`#10B981`), Kehribar (`#F59E0B`), Mercan Kırmızı (`#EF4444`), Sakin İndigo (`#6366F1`).
- **Minimalist Vektör Çember:** Gözü yormayan pürüzsüz yay animasyonu ile kalan süreyi sezgisel olarak yansıtır.
