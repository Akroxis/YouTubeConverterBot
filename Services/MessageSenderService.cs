using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YouTubeConverterBot.Models;

namespace YouTubeConverterBot.Services
{
    public class MessageSenderService
    {
        private readonly ILogger<MessageSenderService> _logger;
        private readonly HttpClient _httpClient;
        private readonly BotConfiguration _config;
        
        public MessageSenderService(
            ILogger<MessageSenderService> logger,
            IHttpClientFactory httpClientFactory, 
            IOptions<BotConfiguration> config)
        {
            _logger = logger;
            _config = config.Value;
            
            _httpClient = httpClientFactory.CreateClient("Telegram");
        }
        
        public async Task SendTextMessageAsync(long chatId, string text, CancellationToken cancellationToken)
        {
            try
            {
                var formData = new Dictionary<string, string>
                {
                    { "chat_id", chatId.ToString() },
                    { "text", text },
                    { "parse_mode", "HTML" }
                };
                
                var content = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync("sendMessage", content, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                _logger.LogDebug("Сообщение отправлено в чат {ChatId}", chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки сообщения в чат {ChatId}", chatId);
                throw;
            }
        }
    }
}