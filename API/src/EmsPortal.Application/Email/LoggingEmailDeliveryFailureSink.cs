using EmsPortal.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;

namespace EmsPortal.Application.Email;

/// <summary>
/// Default <see cref="IEmailDeliveryFailureSink"/>: writes the failure to the application log. A module
/// that keeps a per-record delivery log registers its own sink in place of this one.
/// </summary>
public sealed class LoggingEmailDeliveryFailureSink : IEmailDeliveryFailureSink
{
    private readonly ILogger<LoggingEmailDeliveryFailureSink> _logger;

    public LoggingEmailDeliveryFailureSink(ILogger<LoggingEmailDeliveryFailureSink> logger)
    {
        _logger = logger;
    }

    public Task RecordAsync(EmailDeliveryFailure failure, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Email {TemplateKey} to {Recipient} (tenant {TenantId}, message {MessageId}) was not delivered: {Reason} {Detail}",
            failure.Key, failure.Recipient, failure.TenantId, failure.MessageId, failure.Reason, failure.Detail);
        return Task.CompletedTask;
    }
}
