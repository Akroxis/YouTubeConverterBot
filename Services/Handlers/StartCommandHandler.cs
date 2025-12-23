using Microsoft.Extensions.Logging;
using YouTubeConverterBot.Models.Telegram;

namespace YouTubeConverterBot.Services.Handlers
{
    /// <summary>
    /// Обработчик команды /start.
    /// </summary>
    public class StartCommandHandler : BaseCommandHandler
    {
        public override string Command => "/start";
        public override string Description => "Начать работу с ботом";
        
        public StartCommandHandler(
            ILogger<StartCommandHandler> logger,
            MessageSenderService messageSender)
            : base(logger, messageSender)
        {
        }
        
        public override async Task HandleAsync(TelegramMessage message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Обработка команды /start от {UserId}", message.From?.Id);
            
            var username = message.From?.Username ?? message.From?.FirstName ?? "пользователь";
            var responseText = $"""
                                Привет, {username}

                                <b>YouTube Converter Bot</b>

                                Я помогу конвертировать YouTube видео в аудио форматы.

                                <b>Доступные команды:</b>
                                /start - Начать работу
                                /help - Получить список команд
                                /convert - Конвертировать видео (не работает чичяс)

                                <b>Что я умею (буду уметь):</b>
                                • Конвертировать в MP3, AAC
                                • Выбирать качество звука
                                • Отправлять файлы в Telegram
                                
                                Пишите письма в спортлото
                                """;
            
            await SendResponseAsync(message.Chat.Id, responseText, cancellationToken);
        }
    }
}