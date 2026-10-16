using Microsoft.Extensions.Configuration;

namespace SafeRoute.MonitorService.Services;

public class TelegramNotifier
{
    private readonly HttpClient _httpClient;
    private readonly string _botToken;
    private readonly string _chatId;

    public TelegramNotifier(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _botToken = configuration["Telegram:BotToken"] ?? string.Empty;
        _chatId = configuration["Telegram:ChatId"] ?? string.Empty;
    }

    public async Task SendAsync(string message)
    {
        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["chat_id"] = _chatId,
            ["text"] = message
        });

        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
    }
}