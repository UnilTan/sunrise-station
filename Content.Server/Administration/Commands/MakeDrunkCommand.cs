using Content.Server.Drunk;
using Content.Shared.Administration;
using Content.Shared.Drunk;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    /// <summary>
    /// Command that applies drunk status effect to an entity for testing drunk falling feature.
    /// </summary>
    [AdminCommand(AdminFlags.Fun)]
    public sealed class MakeDrunkCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "makedrunk";
        public string Description => "Apply drunk status effect to an entity for testing purposes.";
        public string Help => $"Usage: {Command} <target> [duration_seconds=120]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteLine($"Not enough arguments.\n{Help}");
                return;
            }

            if (!NetEntity.TryParse(args[0], out var uidNet) || !_entManager.TryGetEntity(uidNet, out var uid))
            {
                shell.WriteLine($"Invalid entity id.");
                return;
            }

            var duration = TimeSpan.FromSeconds(120); // Default 2 minutes
            if (args.Length >= 2 && int.TryParse(args[1], out var seconds))
            {
                duration = TimeSpan.FromSeconds(seconds);
            }

            var drunkSystem = _entManager.EntitySysManager.GetEntitySystem<DrunkSystem>();
            drunkSystem.TryApplyDrunkenness(uid.Value, duration);

            shell.WriteLine($"Applied drunk status effect to entity {uid} for {duration.TotalSeconds} seconds.");
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return CompletionResult.Empty;
        }
    }
}