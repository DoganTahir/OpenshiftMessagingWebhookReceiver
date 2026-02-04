using System.Text.Json;

namespace OpenshiftWebHook.Services;

public class SmsService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SmsService> _logger;
    private readonly string _smsApiUrl;
    private readonly string? _smsApiKey;
    private readonly string? _smsApiSecret;
    private readonly string? _smsFromNumber;
    private readonly string _smsToNumber;

    public SmsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SmsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Get configuration from appsettings.json
        _smsApiUrl = configuration["SmsProvider:ApiUrl"] 
            ?? throw new InvalidOperationException("SmsProvider:ApiUrl configuration is required");
        _smsApiKey = configuration["SmsProvider:ApiKey"];
        _smsApiSecret = configuration["SmsProvider:ApiSecret"];
        _smsFromNumber = configuration["SmsProvider:FromNumber"];
        _smsToNumber = configuration["SmsProvider:ToNumber"] 
            ?? throw new InvalidOperationException("SmsProvider:ToNumber configuration is required");

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(_smsApiUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        if (!string.IsNullOrEmpty(_smsApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _smsApiKey);
        }
    }

    public async Task<bool> SendSmsAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new
            {
                to = _smsToNumber,
                from = _smsFromNumber,
                message = message
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/send", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS sent successfully");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("SMS send failed with status {StatusCode}: {Error}", 
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending SMS");
            return false;
        }
    }
}
