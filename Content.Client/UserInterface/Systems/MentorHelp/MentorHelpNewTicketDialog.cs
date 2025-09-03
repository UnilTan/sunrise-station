using System;
using System.Numerics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.UserInterface.Systems.MentorHelp
{
    /// <summary>
    /// Dialog for creating a new mentor help ticket
    /// </summary>
    public sealed class MentorHelpNewTicketDialog : DefaultWindow
    {
        public event Action<string, string>? OnTicketCreated;

        public MentorHelpNewTicketDialog()
        {
            Title = "Новый тикет ментор-помощи";
            SetSize = new Vector2(450, 300);

            var vbox = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 10
            };

            // Instructions
            var instructions = new RichTextLabel
            {
                Text = "[color=#CCCCCC]Опишите ваш вопрос по игровым механикам. Ментор-помощь предназначена для вопросов о том, как что-то работает в игре, а не для жалоб на игроков или сообщений о багах.[/color]",
                SetHeight = 60
            };

            // Subject input
            var subjectLabel = new Label { Text = "Тема:" };
            var subjectInput = new LineEdit
            {
                PlaceHolder = "Краткое описание вопроса",
                SetHeight = 30
            };

            // Message input
            var messageLabel = new Label { Text = "Подробное описание:" };
            var messageInput = new TextEdit
            {
                VerticalExpand = true
            };

            // Buttons
            var buttonContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 10,
                SetHeight = 30
            };

            var cancelButton = new Button { Text = "Отмена" };
            var createButton = new Button { Text = "Создать тикет" };

            cancelButton.OnPressed += _ => Close();
            createButton.OnPressed += _ =>
            {
                var subject = (subjectInput.Text?.Trim()) ?? "";
                var message = (messageInput.TextRope?.ToString()?.Trim()) ?? "";

                if (string.IsNullOrEmpty(subject))
                {
                    // TODO: Show error message
                    return;
                }

                if (string.IsNullOrEmpty(message))
                {
                    // TODO: Show error message  
                    return;
                }

                OnTicketCreated?.Invoke(subject, message);
            };

            buttonContainer.AddChild(cancelButton);
            buttonContainer.AddChild(createButton);

            vbox.AddChild(instructions);
            vbox.AddChild(subjectLabel);
            vbox.AddChild(subjectInput);
            vbox.AddChild(messageLabel);
            vbox.AddChild(messageInput);
            vbox.AddChild(buttonContainer);

            Contents.AddChild(vbox);
        }
    }
}