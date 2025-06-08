using System.Text.RegularExpressions;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;

namespace Content.Server._Sunrise.ChatSan;

/// <summary>
/// Санитизация чата
/// </summary>
public sealed class ChatSanSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private bool _enabled;
    private bool _aggressive;

    private static readonly Regex UrlRegex = new("[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&\\/\\/=]*)");

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _enabled = _configurationManager.GetCVar(SunriseCCVars.ChatSanitizationEnable);
        _aggressive = _configurationManager.GetCVar(SunriseCCVars.ChatSanitizationAggressive);

        _configurationManager.OnValueChanged(SunriseCCVars.ChatSanitizationEnable, obj => { _enabled = obj; });
        _configurationManager.OnValueChanged(SunriseCCVars.ChatSanitizationAggressive, obj => { _aggressive = obj; });

        SubscribeLocalEvent<ChatSanRequestEvent>(HandleChatSanRequest);
    }

    private void HandleChatSanRequest(ref ChatSanRequestEvent ev)
    {
        if (!_enabled)
            return;

        if (ev.Handled)
            return;
        ev.Handled = true;

        // 1. Если regex нашел url: [-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&\/\/=]*)
        switch (_aggressive)
        {
            case true when UrlRegex.IsMatch(ev.Message):
                ev.Cancelled = true;
                return;
            case false:
                ev.Message = UrlRegex.Replace(ev.Message, "");
                break;
        }

    }
}
