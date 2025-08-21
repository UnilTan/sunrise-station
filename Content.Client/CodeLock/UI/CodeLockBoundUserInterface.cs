using Content.Shared.CodeLock;
using Robust.Client.UserInterface;

namespace Content.Client.CodeLock.UI;

/// <summary>
/// Bound user interface for the code lock system.
/// </summary>
public sealed class CodeLockBoundUserInterface : BoundUserInterface
{
    private CodeLockWindow? _window;

    public CodeLockBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CodeLockWindow>();
        
        _window.KeypadPressed += OnKeypadPressed;
        _window.ClearPressed += OnClearPressed;
        _window.EnterPressed += OnEnterPressed;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Dispose();
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is CodeLockUserInterfaceState codeLockState)
        {
            _window?.UpdateState(codeLockState);
        }
    }

    private void OnKeypadPressed(int value)
    {
        SendMessage(new CodeLockKeypadPressedMessage(value));
    }

    private void OnClearPressed()
    {
        SendMessage(new CodeLockKeypadClearMessage());
    }

    private void OnEnterPressed()
    {
        SendMessage(new CodeLockKeypadEnterMessage());
    }
}