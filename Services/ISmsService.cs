namespace OpenshiftWebHook.Services;

public interface ISmsService
{
    Task<bool> SendSmsAsync(string message, CancellationToken cancellationToken = default);
}
