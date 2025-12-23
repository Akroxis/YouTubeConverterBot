using Microsoft.Extensions.Logging;
using YouTubeConverterBot.Interfaces;
using YouTubeConverterBot.Models.Telegram;

namespace YouTubeConverterBot.Services.Handlers
{
    /// <summary>
    /// Базовый класс для всех обработчиков команд.
    /// Содержит общую логику для отправки сообщений.
    /// </summary>
    public abstract class BaseCommandHandler : ICommandHandler
    {
        protected readonly ILogger<BaseCommandHandler> _logger;
        protected readonly MessageSenderService _messageSender;
        
        public abstract string Command { get; }
        public abstract string Description { get; }
        
        protected BaseCommandHandler(ILogger<BaseCommandHandler> logger, MessageSenderService messageSender)
        {
            _logger = logger;
            _messageSender = messageSender;
        }
        
        public bool CanHandle(string command)
        {
            return command.Equals(Command, StringComparison.OrdinalIgnoreCase);
        }
        
        public abstract Task HandleAsync(TelegramMessage message, CancellationToken cancellationToken);
        
        public string GetDescription() => Description;

        protected async Task SendResponseAsync(long chatId, string text, CancellationToken cancellationToken)
        {
            await _messageSender.SendTextMessageAsync(chatId, text, cancellationToken);
        }
    }
}