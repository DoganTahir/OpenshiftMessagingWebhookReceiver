namespace OpenshiftWebHook.Services;

/// <summary>
/// SMS provider için access token alma ve cache yönetimi.
/// </summary>
public interface ITokenService
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
