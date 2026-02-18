using System.Net.Http.Headers;
using Polly;
using Polly.Extensions.Http;

namespace OpenshiftWebHook.Services;

public class SmsService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsService> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    private readonly string _messageEndpoint;
    private readonly string _mobileNumber;

    public SmsService(
        HttpClient httpClient,
        ITokenService tokenService,
        IConfiguration configuration,
        ILogger<SmsService> logger)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
        _configuration = configuration;
        _logger = logger;

        _messageEndpoint = configuration["SmsProvider:MessageEndpoint"]
            ?? throw new InvalidOperationException("SmsProvider:MessageEndpoint configuration is required");
        _mobileNumber = configuration["SmsProvider:MobileNumber"]
            ?? throw new InvalidOperationException("SmsProvider:MobileNumber configuration is required");

        // Retry policy: maksimum 3 deneme, exponential backoff
        var maxRetries = configuration.GetValue<int>("SmsProvider:MaxRetries", 3);
        var baseDelay = TimeSpan.FromSeconds(configuration.GetValue<int>("SmsProvider:RetryBaseDelaySeconds", 2));

        _retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                maxRetries,
                retryAttempt => baseDelay * Math.Pow(2, retryAttempt),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "SMS send attempt {RetryCount} failed. Retrying in {Delay}ms. Error: {Error}",
                        retryCount, timespan.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });

        _httpClient.BaseAddress = new Uri(_messageEndpoint);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<bool> SendSmsAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Access token al (cache'den veya yeni)
            var accessToken = await _tokenService.GetAccessTokenAsync(cancellationToken);

            // 2. Message ID oluştur (opsiyonel, timestamp bazlı)
            var messageId = _configuration["SmsProvider:MessageId"] 
                ?? $"A{DateTime.UtcNow:yyyyMMddHHmmss}";

            // 3. Form-urlencoded body oluştur
            var formData = new List<KeyValuePair<string, string>>
            {
                new("message", message),
                new("messageId", messageId),
                new("mobileNumber", _mobileNumber)
            };

            var formContent = new FormUrlEncodedContent(formData);

            // 4. Bearer token ile istek gönder (retry ile)
            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = formContent;
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                return await _httpClient.SendAsync(request, cancellationToken);
            });

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS sent successfully. MessageId: {MessageId}", messageId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("SMS send failed with status {StatusCode}: {Error}. MessageId: {MessageId}",
                    response.StatusCode, errorContent, messageId);
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
