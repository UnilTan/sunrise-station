using System.Numerics;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Client.UserInterface.Systems.MentorHelp;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input.Binding;

namespace Content.Client.UserInterface.Systems.HelpChoice;

/// <summary>
/// UI controller that shows choice dialog between Admin Help and Mentor Help
/// </summary>
[UsedImplicitly]
public sealed class HelpChoiceUIController : UIController
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenHelpChoice,
                InputCmdHandler.FromDelegate(_ => ShowHelpChoiceDialog()))
            .Register<HelpChoiceUIController>();
    }

    private void ShowHelpChoiceDialog()
    {
        var dialog = new DefaultWindow
        {
            Title = "Выберите тип помощи",
            SetSize = new Vector2(400, 200)
        };

        var container = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        dialog.Contents.AddChild(container);

        // Title label
        var titleLabel = new RichTextLabel
        {
            SetSize = new Vector2(380, 40),
            Text = "[color=white][font size=16]Какой тип помощи вам нужен?[/font][/color]"
        };
        container.AddChild(titleLabel);

        // Button container
        var buttonContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
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
            _uiManager.GetUIController<AHelpUIController>().Open();
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
            _uiManager.GetUIController<MentorHelpUIController>().Open();
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