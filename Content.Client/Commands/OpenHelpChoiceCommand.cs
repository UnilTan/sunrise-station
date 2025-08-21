using Content.Client.UserInterface.Systems.Bwoink;
using Content.Client.UserInterface.Systems.MentorHelp;
using Content.Shared.Administration;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Console;

namespace Content.Client.Commands;

/// <summary>
/// Opens a choice dialog for Admin Help or Mentor Help when F1 is pressed
/// </summary>
[AnyCommand]
public sealed class OpenHelpChoiceCommand : LocalizedCommands
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    public override string Command => "openhelpChoice";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        ShowHelpChoiceDialog();
    }

    private void ShowHelpChoiceDialog()
    {
        var dialog = new DefaultWindow
        {
            Title = "Выберите тип помощи",
            SetSize = new Vector2(400, 200)
        };

        var container = new VBoxContainer();
        dialog.Contents.AddChild(container);

        // Title label
        var titleLabel = new RichTextLabel
        {
            SetSize = new Vector2(380, 40),
            Text = "[color=white][font size=16]Какой тип помощи вам нужен?[/font][/color]"
        };
        container.AddChild(titleLabel);

        // Button container
        var buttonContainer = new HBoxContainer
        {
            HorizontalAlignment = Control.HAlignment.Center,
            SeparationOverride = 20
        };
        container.AddChild(buttonContainer);

        // Admin Help button
        var ahelpButton = new Button
        {
            Text = "Админ-помощь\n(для нарушений правил)",
            SetSize = new Vector2(150, 80),
            HorizontalAlignment = Control.HAlignment.Center
        };
        ahelpButton.OnPressed += _ =>
        {
            dialog.Close();
            _userInterfaceManager.GetUIController<AHelpUIController>().Open();
        };
        buttonContainer.AddChild(ahelpButton);

        // Mentor Help button
        var mhelpButton = new Button
        {
            Text = "Ментор-помощь\n(для вопросов по игре)",
            SetSize = new Vector2(150, 80),
            HorizontalAlignment = Control.HAlignment.Center
        };
        mhelpButton.OnPressed += _ =>
        {
            dialog.Close();
            _userInterfaceManager.GetUIController<MentorHelpUIController>().Open();
        };
        buttonContainer.AddChild(mhelpButton);

        // Description
        var descLabel = new RichTextLabel
        {
            SetSize = new Vector2(380, 60),
            Text = "[color=#CCCCCC][font size=12]• Админ-помощь - для жалоб на игроков, сообщений о багах и нарушениях правил\n• Ментор-помощь - для вопросов о механиках игры и помощи новичкам[/font][/color]"
        };
        container.AddChild(descLabel);

        dialog.OpenCentered();
    }
}