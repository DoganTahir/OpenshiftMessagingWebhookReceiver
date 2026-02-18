using System.Collections.Concurrent;
using System.Text.Json;
using Polly;
using Polly.Extensions.Http;

namespace OpenshiftWebHook.Services;

public class TokenService : ITokenService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;
    private readonly ConcurrentDictionary<string, CachedToken> _tokenCache = new();
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    public TokenService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TokenService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var authEndpoint = configuration["SmsProvider:AuthEndpoint"]
            ?? throw new InvalidOperationException("SmsProvider:AuthEndpoint configuration is required");

        _httpClient.BaseAddress = new Uri(authEndpoint);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        // Retry policy: maksimum 3 deneme, exponential backoff
        var maxRetries = configuration.GetValue<int>("SmsProvider:TokenMaxRetries", 3);
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
                        "Token request attempt {RetryCount} failed. Retrying in {Delay}ms. Error: {Error}",
                        retryCount, timespan.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "sms_access_token";

        // Cache'den token kontrolü
        if (_tokenCache.TryGetValue(cacheKey, out var cachedToken))
        {
            if (cachedToken.ExpiresAt > DateTime.UtcNow.AddMinutes(1)) // 1 dakika buffer
            {
                _logger.LogDebug("Using cached access token");
                return cachedToken.Token;
            }
        }

        // Token yok veya expire olmuş, yeni token al (async-safe lock)
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check: başka thread token almış olabilir
            if (_tokenCache.TryGetValue(cacheKey, out cachedToken) &&
                cachedToken.ExpiresAt > DateTime.UtcNow.AddMinutes(1))
            {
                return cachedToken.Token;
            }

            return await GetTokenAsync(cacheKey, cancellationToken);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<string> GetTokenAsync(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            var grantType = _configuration["SmsProvider:GrantType"] ?? "client_credentials";
            var clientId = _configuration["SmsProvider:ClientId"]
                ?? throw new InvalidOperationException("SmsProvider:ClientId configuration is required");
            var clientSecret = _configuration["SmsProvider:ClientSecret"]
                ?? throw new InvalidOperationException("SmsProvider:ClientSecret configuration is required");

            var requestBody = new
            {
                grantType = grantType,
                clientId = clientId,
                clientSecret = clientSecret
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("Requesting access token from auth endpoint");

            // Retry policy ile token isteği
            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _httpClient.PostAsync("", content, cancellationToken);
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Token request failed with status {StatusCode}: {Error}",
                    response.StatusCode, errorContent);
                throw new InvalidOperationException(
                    $"Failed to obtain access token. Status: {response.StatusCode}, Error: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("Invalid token response: accessToken is missing");
            }

            // Token'ı cache'e kaydet
            var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.AccessTokenExpiresIn);
            _tokenCache.AddOrUpdate(cacheKey,
                new CachedToken { Token = tokenResponse.AccessToken, ExpiresAt = expiresAt },
                (key, oldValue) => new CachedToken { Token = tokenResponse.AccessToken, ExpiresAt = expiresAt });

            _logger.LogInformation("Access token obtained successfully, expires in {ExpiresIn} seconds",
                tokenResponse.AccessTokenExpiresIn);

            return tokenResponse.AccessToken;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Exception occurred while obtaining access token");
            throw new InvalidOperationException("Failed to obtain access token", ex);
        }
    }

    private class CachedToken
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    private class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int AccessTokenExpiresIn { get; set; }
        public string? RefreshToken { get; set; }
        public int? RefreshTokenExpiresIn { get; set; }
    }
}
