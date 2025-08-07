using Content.Shared._Sunrise.InteractionsPanel.Data.UI;
using Robust.Shared.Configuration;

namespace Content.Client._Sunrise.InteractionsPanel;

public sealed class InteractionsPanelSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _cfg.OnValueChanged(InteractionsCVars.EmoteVisibility, SendCurrentEmoteStatus);
        SendCurrentEmoteStatus(_cfg.GetCVar<bool>("interactions.emote"));
    }

    /// <summary>
    /// Отправляет на сервер сообщение с текущим статусом.
    /// Обратите внимание на то, что эта функция берет статус из аргумента, а не из самого цвара.
    /// </summary>
    private void SendCurrentEmoteStatus(bool obj)
    {
        var ev = new EmoteVisibilityChangedEvent { Status = obj };
        RaiseNetworkEvent(ev);
    }
}
