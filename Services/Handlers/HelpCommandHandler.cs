using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YouTubeConverterBot.Interfaces;
using YouTubeConverterBot.Models.Telegram;

namespace YouTubeConverterBot.Services.Handlers
{
    public class HelpCommandHandler : BaseCommandHandler
    {
        private readonly Lazy<IEnumerable<ICommandHandler>> _commandHandlers;
        
        public override string Command => "/help";
        public override string Description => "Показать справку по командам";
        
        public HelpCommandHandler(
            ILogger<HelpCommandHandler> logger,
            MessageSenderService messageSender,
            IServiceProvider serviceProvider) 
            : base(logger, messageSender)
        {
            _commandHandlers = new Lazy<IEnumerable<ICommandHandler>>(() =>
                serviceProvider.GetServices<ICommandHandler>()
                    .Where(h => h is not HelpCommandHandler)  // Исключаем себя
                    .ToList());
        }
        
        public override async Task HandleAsync(TelegramMessage message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Обработка команды /help от {UserId}", message.From?.Id);
            
            var commandText = message.Text ?? string.Empty;
            var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length > 1)
            {
                await ShowCommandHelpAsync(parts[1], message.Chat.Id, cancellationToken);
            }
            else
            {
                await ShowAllCommandsAsync(message.Chat.Id, cancellationToken);
            }
        }
        
        private async Task ShowCommandHelpAsync(string commandName, long chatId, CancellationToken cancellationToken)
        {
            var handler = _commandHandlers.Value.FirstOrDefault(h => h.CanHandle(commandName));
            
            if (handler != null)
            {
                var helpText = $"""
                    <b>Справка по команде {commandName}</b>
                    
                    <b>Описание:</b> {handler.GetDescription()}
                    <b>Использование:</b> {commandName}
                    """;
                
                await SendResponseAsync(chatId, helpText, cancellationToken);
            }
            else
            {
                await SendResponseAsync(chatId, $"Команда '{commandName}' не найдена", cancellationToken);
            }
        }
        
        private async Task ShowAllCommandsAsync(long chatId, CancellationToken cancellationToken)
        {
            var helpText = "<b>Доступные команды:</b>\n\n";
            
            foreach (var handler in _commandHandlers.Value)
            {
                helpText += $"• <code>{handler.GetDescription()}</code>\n";
            }
            
            helpText += $"\n• <code>{Description}</code>\n";
            helpText += "\nНапишите <code>/help [команда]</code> для подробной справки";
            
            await SendResponseAsync(chatId, helpText, cancellationToken);
        }
    }
}