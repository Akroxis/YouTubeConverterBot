using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YouTubeConverterBot.Models;
using YouTubeConverterBot.Models.Telegram;
using System.Net.Http.Json;
using YouTubeConverterBot.Interfaces;

namespace YouTubeConverterBot.Services
{
    /// <summary>
    /// Основной сервис бота.
    /// Реализует BackgroundService для фоновой работы.
    /// </summary>
    public class TelegramBotService : BackgroundService
    {
        private readonly ILogger<TelegramBotService> _logger;
        private readonly BotConfiguration _config;
        private readonly HttpClient _httpClient;
        private long _lastUpdateId = 0;
        private readonly IEnumerable<ICommandHandler> _commandHandlers;
        private readonly MessageSenderService _messageSender;
        
        public TelegramBotService(
            ILogger<TelegramBotService> logger,
            IOptions<BotConfiguration> config,
            HttpClient httpClient, IEnumerable<ICommandHandler> commandHandlers, MessageSenderService messageSender)
        {
            _logger = logger;
            _config = config.Value;
            _httpClient = httpClient;
            _commandHandlers = commandHandlers;
            _messageSender = messageSender;
            
            _httpClient.BaseAddress = new Uri($"https://api.telegram.org/bot{_config.BotToken}/");
            _httpClient.Timeout = TimeSpan.FromMinutes(2);
            
            _logger.LogInformation("TelegramBotService создан с {Count} обработчиками", 
                _commandHandlers.Count());
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Запуск Telegram Bot Service...");
            
            try
            {
                var botInfo = await GetBotInfoAsync(stoppingToken);
                if (botInfo != null)
                {
                    _logger.LogInformation("Бот авторизован: @{BotUsername}", botInfo.Username);
                }
                else
                {
                    _logger.LogError("Не удалось авторизовать бота. Проверьте токен в appsettings.json");
                    return;
                }
                
                _logger.LogInformation("Бот готов к работе. Ожидаю сообщения...");
                
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ProcessUpdatesAsync(stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Ошибка при обработке обновлений");
                        await Task.Delay(5000, stoppingToken);
                    }
                    
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Остановка бота из-за полученного сигнала");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Критическая ошибка в работе бота");
                throw;
            }
        }
        
        private async Task ProcessUpdatesAsync(CancellationToken cancellationToken)
        {
            var updates = await GetUpdatesAsync(cancellationToken);
            
            foreach (var update in updates)
            {
                try
                {
                    await ProcessUpdateAsync(update, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка обработки update {UpdateId}", update.UpdateId);
                }
                
                _lastUpdateId = update.UpdateId;
            }
        }
        
        private async Task ProcessUpdateAsync(TelegramUpdate update, CancellationToken cancellationToken)
        {
            if (update.Message == null)
                return;
            
            // Игнорируем сообщения от ботов
            if (update.Message.From?.IsBot == true)
                return;
            
            _logger.LogInformation("\n \n =====> Сообщение от {UserName} ({UserId}): {Text}", 
                update.Message.From?.Username,
                update.Message.From?.Id, 
                update.Message.Text ?? "[без текста]");
            
            if (string.IsNullOrEmpty(update.Message.Text))
            {
                await HandleNonTextMessageAsync(update.Message, cancellationToken);
                return;
            }
            
            // Проверяем, является ли сообщение командой
            if (update.Message.Text.StartsWith('/'))
            {
                await ProcessCommandAsync(update.Message, cancellationToken);
            }
            else
            {
                await HandleNonCommandMessageAsync(update.Message, cancellationToken);
            }
        }
        
        private async Task ProcessCommandAsync(TelegramMessage message, CancellationToken cancellationToken)
        {
            var commandText = message.Text ?? string.Empty;
            var commandParts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = commandParts[0];
            
            _logger.LogDebug("Обработка команды: {Command}", command);
            
            var handler = _commandHandlers.FirstOrDefault(h => h.CanHandle(command));
            
            if (handler != null)
            {
                try
                {
                    await handler.HandleAsync(message, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка обработки команды {Command}", command);
                    await _messageSender.SendTextMessageAsync(
                        message.Chat.Id,
                        "Произошла ошибка при обработке команды",
                        cancellationToken);
                }
            }
            else
            {
                _logger.LogWarning("Неизвестная команда: {Command}", command);
                await HandleUnknownMessageAsync(message, cancellationToken);
            }
        }
        
        private async Task HandleNonCommandMessageAsync(TelegramMessage message, CancellationToken cancellationToken)
        {
            var responseText = "Я понимаю только команды, начинающиеся с /\n\n" +
                               "Напишите /help чтобы увидеть список доступных команд";
            
            await _messageSender.SendTextMessageAsync(message.Chat.Id, responseText, cancellationToken);
        }
        
        private async Task HandleNonTextMessageAsync(TelegramMessage message, CancellationToken cancellationToken)
        {
            var responseText = "Я пока умею работать только с текстовыми сообщениями\n\n" +
                               "Напишите /help чтобы увидеть список команд";
            
            await _messageSender.SendTextMessageAsync(message.Chat.Id, responseText, cancellationToken);
        }
        
        
        private async Task HandleUnknownMessageAsync(TelegramMessage message, CancellationToken cancellationToken)
        {
            var responseText = "Я пока умею только отвечать на команду /start\n" +
                              "Попробуйте написать /start для начала работы";
            
            await SendTextMessageAsync(message.Chat.Id, responseText, cancellationToken);
        }
        
        private async Task<List<TelegramUpdate>> GetUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Long polling запрос к Telegram API
                var url = $"getUpdates?offset={_lastUpdateId + 1}&timeout=30";
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var apiResponse = await response.Content.ReadFromJsonAsync<TelegramApiResponse<List<TelegramUpdate>>>(cancellationToken);
                
                return apiResponse?.Result ?? new List<TelegramUpdate>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении обновлений");
                return new List<TelegramUpdate>();
            }
        }
        
        private async Task<BotInfo?> GetBotInfoAsync(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetAsync("getMe", cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var apiResponse = await response.Content.ReadFromJsonAsync<TelegramApiResponse<BotInfo>>(cancellationToken);
                
                return apiResponse?.Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении информации о боте");
                return null;
            }
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
            }
        }
    }
}