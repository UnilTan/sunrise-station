using Content.Client.Administration.UI.Complaint;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.Mind.Components;
using Content.Shared.Verbs;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Client.Administration.Systems
{
    /// <summary>
    ///     Client-side admin verb system. These usually open some sort of UIs.
    /// </summary>
    sealed class AdminVerbSystem : EntitySystem
    {
        [Dependency] private readonly IClientConGroupController _clientConGroupController = default!;
        [Dependency] private readonly IClientConsoleHost _clientConsoleHost = default!;
        [Dependency] private readonly ISharedAdminManager _admin = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddAdminVerbs);

        }

        private void AddAdminVerbs(GetVerbsEvent<Verb> args)
        {
            // Currently this is only the ViewVariables verb, but more admin-UI related verbs can be added here.

            // View variables verbs
            if (_clientConGroupController.CanViewVar())
            {
                var verb = new VvVerb()
                {
                    Text = Loc.GetString("view-variables"),
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/vv.svg.192dpi.png")),
                    Act = () => _clientConsoleHost.ExecuteCommand($"vv {GetNetEntity(args.Target)}"),
                    ClientExclusive = true // opening VV window is client-side. Don't ask server to run this verb.
                };
                args.Verbs.Add(verb);
            }

            // Add complaint verb for players with minds (other players)
            AddComplaintVerb(args);

            if (!_admin.IsAdmin(args.User))
                return;

            if (_admin.HasAdminFlag(args.User, AdminFlags.Admin))
                args.ExtraCategories.Add(VerbCategory.Admin);

            if (_admin.HasAdminFlag(args.User, AdminFlags.Fun) && HasComp<MindContainerComponent>(args.Target))
                args.ExtraCategories.Add(VerbCategory.Antag);

            if (_admin.HasAdminFlag(args.User, AdminFlags.Debug))
                args.ExtraCategories.Add(VerbCategory.Debug);

            if (_admin.HasAdminFlag(args.User, AdminFlags.Fun))
                args.ExtraCategories.Add(VerbCategory.Smite);

            if (_admin.HasAdminFlag(args.User, AdminFlags.Admin))
                args.ExtraCategories.Add(VerbCategory.Tricks);
        }

        private void AddComplaintVerb(GetVerbsEvent<Verb> args)
        {
            // Only add complaint verb if:
            // 1. Target has a mind (is a player)
            // 2. Target is not the user themselves
            // 3. User has a player session
            if (!HasComp<MindContainerComponent>(args.Target))
                return;

            if (!_playerManager.LocalSession.HasValue)
                return;

            var localSession = _playerManager.LocalSession.Value;

            // Don't allow complaining against yourself
            if (args.Target == args.User)
                return;

            // Find the target player session
            var targetSession = _playerManager.Sessions.FirstOrDefault(s => s.AttachedEntity == args.Target);
            if (targetSession == null)
                return;

            var verb = new Verb()
            {
                Text = Loc.GetString("complaint-verb-file"),
                Message = Loc.GetString("complaint-verb-file-desc"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/exclamation.svg.192dpi.png")),
                Act = () =>
                {
                    var playerName = targetSession.Name;
                    var dialog = new ComplaintCreateDialog(targetSession.UserId, playerName);
                    dialog.OpenCentered();
                },
                ClientExclusive = true,
                Priority = -1 // Lower priority so it appears at bottom of context menu
            };

            args.Verbs.Add(verb);
        }
    }
}
