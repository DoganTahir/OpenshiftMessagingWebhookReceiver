# Kod Dokümantasyonu - OpenshiftWebHook

## 📋 Genel Bakış

Bu proje, OpenShift Prometheus Alertmanager'dan gelen alarmları alıp, kritik/warning seviyesindeki alarmlar için SMS gönderen bir .NET Core Web API servisidir.

---

## 🏗️ Proje Yapısı

### **Program.cs** - Uygulama Giriş Noktası
**Ne yapar:** Uygulamanın başlangıç konfigürasyonunu yapar.

**Önemli İşlevler:**
- Serilog logging yapılandırması
- Dependency Injection (DI) container'a servisleri kaydetme
- HTTP pipeline yapılandırması
- Swagger UI (her ortamda: `/swagger`)

**Kayıt Edilen Servisler:**
- `SmsService` → HttpClient ile (her istek için yeni instance)

---

## 📦 Models (Veri Modelleri)

### **Alert.cs** - Tek Bir Alarmı Temsil Eder
**Ne yapar:** Alertmanager'dan gelen her bir alarmın JSON formatını C# nesnesine çevirir.

**Özellikler:**
- `Status`: Alarm durumu ("firing" veya "resolved")
- `Labels`: Alarm etiketleri (alertname, namespace, service, severity vb.)
- `Annotations`: Alarm açıklamaları (summary, description)
- `StartsAt`: Alarm başlangıç zamanı
- `EndsAt`: Alarm bitiş zamanı (resolved ise)
- `Fingerprint`: Alarmın benzersiz kimliği

**Helper Metodlar:**
- `GetLabel(string key)`: Labels dictionary'den güvenli şekilde değer okur
- `GetAnnotation(string key)`: Annotations dictionary'den güvenli şekilde değer okur

### **AlertPayload.cs** - Alertmanager Webhook Payload'ını Temsil Eder
**Ne yapar:** Alertmanager'dan gelen tüm webhook payload'ını temsil eder.

**Özellikler:**
- `Version`: Alertmanager versiyonu
- `Alerts`: Alarm listesi (List<Alert>) — zorunlu
- `GroupKey`, `Status`, `Receiver`, `GroupLabels`, `CommonLabels`, `CommonAnnotations`, `ExternalURL`: Payload metadata'sı

### **AlertResponse.cs** - Yanıt Modeli
**Ne yapar:** POST /alert/alert endpoint'inin döndürdüğü JSON gövdesini temsil eder.

**Özellikler:**
- `Processed`: SMS gönderilen alarm sayısı
- `Skipped`: Filtre nedeniyle atlanan alarm sayısı
- `Total`: Toplam alarm sayısı

---

## 🎮 Controllers (API Endpoint'leri)

### **AlertController.cs** - Ana İş Mantığı
**Ne yapar:** Alertmanager'dan gelen webhook isteklerini işler ve SMS gönderir.

#### **ReceiveAlert()** - POST /alert/alert
**Ne yapar:** 
1. Gelen payload'ı kontrol eder
2. Her alarm için:
   - Filtreleme yapar (severity=critical/warning, status=firing)
   - SMS mesajı oluşturur
   - SMS gönderir

**Döndürdüğü Değer:**
```json
{
  "processed": 1,    // İşlenen alarm sayısı
  "skipped": 0,     // Atlanan alarm sayısı
  "total": 1        // Toplam alarm sayısı
}
```

#### **ShouldProcessAlert()** - Private Helper
**Ne yapar:** Alarmın işlenip işlenmeyeceğini kontrol eder.

**Kriterler:**
- ✅ Status = "firing" olmalı
- ✅ Severity = "critical" VEYA "warning" olmalı

#### **GenerateSmsMessage()** - Private Helper
**Ne yapar:** Alert bilgilerinden SMS mesajı oluşturur.

**Mesaj Formatı:**
```
[CRITICAL] HighCPU
NS: production
Svc: api
Summary: CPU usage is above 80%
Started: 2026-02-04 10:00:00 UTC
```

**Özellikler:**
- Maksimum 500 karakter (SMS limiti)
- Eksik bilgiler için "Unknown" kullanır

---

### **HealthController.cs** - Sağlık Kontrolü
**Ne yapar:** OpenShift'in pod sağlığını kontrol etmesi için endpoint'ler sağlar.

#### **Health()** - GET /health/health
**Ne yapar:** Genel sağlık durumunu döndürür.

#### **Ready()** - GET /health/ready
**Ne yapar:** Pod'un hazır olup olmadığını kontrol eder (readiness probe).

#### **Live()** - GET /health/live
**Ne yapar:** Pod'un çalışıp çalışmadığını kontrol eder (liveness probe).

---

## 🔧 Services (İş Mantığı Servisleri)

### **SmsService.cs** - SMS Gönderme Servisi
**Ne yapar:** Harici SMS provider API'sine HTTP isteği gönderir.

**Özellikler:**
- Error handling ve logging
- Basit HTTP POST isteği

#### **SendSmsAsync(string message)**
**Ne yapar:** 
1. SMS mesajını harici provider formatına çevirir
2. JSON request body oluşturur: `{ "to": "...", "from": "...", "message": "..." }`
3. HTTP POST isteği gönderir (`/send` endpoint'ine)
4. Sonucu döndürür

**Döndürür:**
- `true`: SMS başarıyla gönderildi
- `false`: SMS gönderilemedi (hata loglanır)

**Konfigürasyon (appsettings.json):**
- `SmsProvider:ApiUrl`: SMS provider API URL'i
- `SmsProvider:ApiKey`: API key (header'a eklenir)
- `SmsProvider:ToNumber`: Alıcı telefon numarası
- `SmsProvider:FromNumber`: Gönderen telefon numarası

---

## 🔌 Interfaces (Arayüzler)

### **ISmsService.cs**
**Ne yapar:** SmsService için contract tanımlar (dependency injection ve test için).

**Metod:**
- `Task<bool> SendSmsAsync(string message)`

---

## 🔄 İş Akışı (Flow)

```
1. Alertmanager → POST /alert/alert (JSON payload)
   ↓
2. AlertController.ReceiveAlert()
   ↓
3. Her Alert için:
   ├─ ShouldProcessAlert() → Filtreleme (firing + critical/warning)
   ├─ GenerateSmsMessage() → SMS mesajı oluştur
   └─ SmsService.SendSmsAsync() → SMS gönder
   ↓
4. Response döndür: { processed, skipped, total }
```

---

## 📊 Veri Akışı

### **Gelen Veri (Alertmanager):**
```json
{
  "alerts": [
    {
      "status": "firing",
      "labels": { "alertname": "HighCPU", "severity": "critical" },
      "fingerprint": "abc123"
    }
  ]
}
```

### **İşlenmiş Veri:**
- Alert → Alert nesnesine deserialize edilir
- Filtreleme → Sadece firing + critical/warning işlenir
- SMS Mesajı → "[CRITICAL] HighCPU\n..."

### **Gönderilen Veri (SMS Provider):**
```json
{
  "to": "+1234567890",
  "from": "+0987654321",
  "message": "[CRITICAL] HighCPU\n..."
}
```

---

## 📘 Swagger

- **UI:** `GET /swagger` — API dokümantasyonu ve "Try it out" ile test
- **JSON:** `GET /swagger/v1/swagger.json` — OpenAPI spec
- **AlertPayloadExampleFilter:** POST /alert/alert için örnek request body otomatik doldurulur

---

## 🎯 Önemli Özellikler

1. **Stateless:** Veritabanı yok, her istek bağımsız
2. **Filtreleme:** Sadece firing + critical/warning alarmlar işlenir
3. **Health:** /health/health, /health/ready, /health/live
4. **Swagger:** Her ortamda `/swagger` ile API testi
5. **Logging:** Serilog ile yapılandırılmış log

---

## 🔧 Konfigürasyon

**appsettings.json:**
- `SmsProvider:ApiUrl`: SMS provider API URL (zorunlu)
- `SmsProvider:ApiKey`: API key (isteğe bağlı, header'da kullanılır)
- `SmsProvider:ApiSecret`: API secret (isteğe bağlı)
- `SmsProvider:FromNumber`: Gönderen numara
- `SmsProvider:ToNumber`: Alıcı numara (zorunlu)

---

## 📝 Notlar

- **Error Handling:** Tüm hatalar loglanır, uygulama çökmez
