using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Roles;
using Content.Shared._Sunrise.VoxRaider;
using Content.Shared.Humanoid;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles.Components;

namespace Content.Server._Sunrise.VoxRaider;

public sealed class VoxRaiderRuleSystem : GameRuleSystem<VoxRaiderRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoxRaiderRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);
        SubscribeLocalEvent<VoxRaiderRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    // Greeting upon vox raider activation
    private void AfterAntagSelected(Entity<VoxRaiderRuleComponent> rule, ref AfterAntagEntitySelectedEvent args)
    {
        var ent = args.EntityUid;
        _antag.SendBriefing(ent, MakeBriefing(ent), null, null);

        Log.Info($"VoxRaider {ToPrettyString(ent)} - Change faction");
        _npcFaction.RemoveFaction(ent, rule.Comp.NanoTrasenFaction, false);
        _npcFaction.AddFaction(ent, rule.Comp.VoxRaiderFaction);
    }

    // Character screen briefing
    private void OnGetBriefing(Entity<VoxRaiderRoleComponent> role, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;

        if (ent is null)
            return;
            
        args.Append(MakeBriefing(ent.Value));
    }

    private string MakeBriefing(EntityUid ent)
    {
        var isVox = HasComp<HumanoidAppearanceComponent>(ent);
        var briefing = isVox
            ? Loc.GetString("vox-raider-role-greeting")
            : Loc.GetString("vox-raider-role-greeting-fallback");

        return briefing;
    }
}