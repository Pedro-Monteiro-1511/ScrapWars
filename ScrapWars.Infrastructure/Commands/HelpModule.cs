using NetCord.Services.ApplicationCommands;
using ScrapWars.Application.Interfaces;

namespace ScrapWars.Infrastructure.Commands;

public class HelpModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly IDirectMessageService _directMessageService;

    public HelpModule(IDirectMessageService directMessageService)
    {
        _directMessageService = directMessageService;
    }

    [SlashCommand("help", "Receive a private message with bot documentation")]
    public async Task<string> HelpAsync()
    {
        try
        {
            await _directMessageService.SendHelpMessageAsync(Context.User.Id);
            return "Enviei-te uma mensagem privada com a documentacao do BOT.";
        }
        catch
        {
            return "Nao consegui enviar a mensagem privada. Verifica se aceitas DMs deste servidor.";
        }
    }
}
