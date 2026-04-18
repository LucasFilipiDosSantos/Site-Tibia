using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Notifications;

public interface IWhatsAppNotificationService
{
    Task<string> SendTemplateMessageAsync(
        string to,
        string templateName,
        string languageCode,
        Dictionary<string, string> parameters,
        CancellationToken ct = default);
}

public sealed class WhatsAppNotificationService : IWhatsAppNotificationService
{
    private readonly WhatsAppOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhatsAppNotificationService> _logger;

    public WhatsAppNotificationService(
        IOptions<WhatsAppOptions> options,
        HttpClient httpClient,
        ILogger<WhatsAppNotificationService> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> SendTemplateMessageAsync(
        string to,
        string templateName,
        string languageCode,
        Dictionary<string, string> parameters,
        CancellationToken ct = default)
    {
        var url = $"{_options.BaseUrl}/{_options.ApiVersion}/{_options.PhoneNumberId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            to = to,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = languageCode },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = parameters.Select(p => new
                        {
                            type = "text",
                            parameter_name = p.Key,
                            text = p.Value
                        }).ToArray()
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.AccessToken);
        request.Content = JsonContent.Create(payload);

        var dedupKey = GenerateDedupKey(to, templateName, parameters);
        _logger.LogInformation("Sending WhatsApp template {Template} to {To}, dedupKey: {DedupKey}",
            templateName, to, dedupKey[..16]);

        try
        {
            using var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("WhatsApp API error: {StatusCode}, {Error}", response.StatusCode, error);
                throw new WhatsAppNotificationException(
                    $"WhatsApp API returned {response.StatusCode}: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<WhatsAppResponse>(cancellationToken: ct);
            var messageId = result?.Messages?.FirstOrDefault()?.Id ?? "unknown";

            _logger.LogInformation("WhatsApp message sent successfully, messageId: {MessageId}", messageId);
            return messageId;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message to {To}", to);
            throw;
        }
    }

    private static string GenerateDedupKey(string to, string templateName, Dictionary<string, string> parameters)
    {
        var key = $"{to}:{templateName}:{string.Join(",", parameters.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}"))}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash).ToLower();
    }

    private class WhatsAppResponse
    {
        public List<WhatsAppMessage>? Messages { get; set; }
    }

    private class WhatsAppMessage
    {
        public string Id { get; set; } = string.Empty;
    }
}

public class WhatsAppNotificationException : Exception
{
    public WhatsAppNotificationException(string message) : base(message) { }
    public WhatsAppNotificationException(string message, Exception inner) : base(message, inner) { }
}