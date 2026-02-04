# OpenshiftWebHook

Prometheus Alertmanager v2 webhook payload'larını alan, kritik/warning ve firing alarmlar için SMS gönderen .NET Web API.

## Özellikler

- Alertmanager v2 webhook (POST /alert/alert)
- Filtre: severity=critical|warning, status=firing
- SMS: Harici REST API (appsettings ile yapılandırma)
- Health: /health/health, /health/ready, /health/live
- Swagger: /swagger (API dokümantasyonu ve test)
- Logging: Serilog

## Endpoints

| Method | Path | Açıklama |
|--------|------|----------|
| POST | /alert/alert | Alertmanager webhook — request body: AlertPayload (alerts[], labels, annotations, startsAt, fingerprint) |
| GET | /health/health | Sağlık kontrolü |
| GET | /health/ready | Readiness probe |
| GET | /health/live | Liveness probe |
| GET | /swagger | Swagger UI |

## Konfigürasyon (appsettings.json)

```json
{
  "SmsProvider": {
    "ApiUrl": "https://your-sms-provider.com/api",
    "ApiKey": "",
    "ApiSecret": "",
    "FromNumber": "+1234567890",
    "ToNumber": "+0987654321"
  }
}
```

- **ApiUrl**, **ToNumber** zorunlu. Diğerleri isteğe bağlı.

## Geliştirme

```bash
dotnet restore
dotnet build
dotnet run
```

- Uygulama: http://localhost:5000 (veya launchSettings’e göre)
- Swagger: http://localhost:5000/swagger
- Örnek istek: `sample-payload.json` ile POST /alert/alert test edilebilir

```bash
curl -X POST http://localhost:5000/alert/alert \
  -H "Content-Type: application/json" \
  -d @sample-payload.json
```

## Docker

```bash
docker build -t openshift-webhook:latest .
```

## Proje Yapısı (sade)

- **Controllers:** AlertController (/alert), HealthController (/health)
- **Models:** Alert, AlertPayload, AlertResponse
- **Services:** SmsService (harici SMS API çağrısı), ISmsService
- **Swagger:** AlertPayloadExampleFilter (örnek request body)

Detaylı kod açıklaması için `CODE_DOCUMENTATION.md` dosyasına bakın.
