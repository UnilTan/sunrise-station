using Content.Server.Construction;
using Content.Server.Power.Components;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Content.Shared.Tools;
using Content.Shared.Verbs;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
/// Handles degradation of telecom servers over time and usage
/// </summary>
public sealed class TelecomDegradationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    
    private const float UpdateInterval = 1.0f; // Update every second
    private float _timeSinceLastUpdate = 0.0f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelecomServerComponent, ComponentStartup>(OnTelecomStartup);
        SubscribeLocalEvent<TelecomServerComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        
        _timeSinceLastUpdate += frameTime;
        if (_timeSinceLastUpdate < UpdateInterval)
            return;
            
        _timeSinceLastUpdate = 0.0f;
        
        // Process degradation for all telecom servers
        var query = EntityQueryEnumerator<TelecomServerComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var telecom, out var power))
        {
            // Only degrade if powered (inactive servers don't degrade as fast)
            if (power.Powered)
            {
                DegradeTelecom((uid, telecom), UpdateInterval);
            }
        }
    }

    private void OnTelecomStartup(EntityUid uid, TelecomServerComponent component, ComponentStartup args)
    {
        // Initialize with random slight degradation to simulate real-world variance
        component.Degradation = Math.Clamp(_random.NextFloat(0.0f, 0.05f), 0.0f, 1.0f);
    }

    private void OnGetInteractionVerbs(EntityUid uid, TelecomServerComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Only show repair verb if there's degradation
        if (component.Degradation <= 0.1f)
            return;

        InteractionVerb verb = new()
        {
            Act = () => {
                RepairTelecom((uid, component), 0.5f);
                _popup.PopupEntity($"Телеком отремонтирован. Текущее состояние: {(1.0f - component.Degradation):P0}", uid, args.User);
            },
            Text = "Ремонт телекома",
            Icon = new SpriteSpecifier.Texture(new("Interface/VerbIcons/settings.svg.192dpi.png")),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Degrades a telecom server based on time passed
    /// </summary>
    public void DegradeTelecom(Entity<TelecomServerComponent> telecom, float deltaTime)
    {
        var degradationIncrease = telecom.Comp.DegradationRate * deltaTime;
        telecom.Comp.Degradation = Math.Clamp(telecom.Comp.Degradation + degradationIncrease, 0.0f, 1.0f);
    }

    /// <summary>
    /// Adds degradation to a telecom server for message usage
    /// </summary>
    public void AddUsageDegradation(Entity<TelecomServerComponent> telecom)
    {
        telecom.Comp.Degradation = Math.Clamp(telecom.Comp.Degradation + telecom.Comp.UsageDegradation, 0.0f, 1.0f);
    }

    /// <summary>
    /// Repairs a telecom server, reducing its degradation
    /// Can be called by construction systems, admin commands, or engineer interactions
    /// </summary>
    public void RepairTelecom(Entity<TelecomServerComponent> telecom, float repairAmount = 0.3f)
    {
        var oldDegradation = telecom.Comp.Degradation;
        telecom.Comp.Degradation = Math.Clamp(telecom.Comp.Degradation - repairAmount, 0.0f, 1.0f);
        
        // Log the repair for debugging
        Logger.Info($"Telecom {telecom.Owner} repaired: {oldDegradation:P1} -> {telecom.Comp.Degradation:P1}");
    }

    /// <summary>
    /// Gets the signal quality modifier based on degradation level
    /// </summary>
    public float GetSignalQuality(Entity<TelecomServerComponent> telecom)
    {
        // Signal quality decreases with degradation
        // 0% degradation = 100% quality, 100% degradation = 20% quality
        return Math.Clamp(1.0f - (telecom.Comp.Degradation * 0.8f), 0.2f, 1.0f);
    }

    /// <summary>
    /// Checks if the telecom server is functioning at minimum operational level
    /// </summary>
    public bool IsFunctional(Entity<TelecomServerComponent> telecom)
    {
        return telecom.Comp.Degradation < 0.95f; // Still functional until 95% degradation
    }

    /// <summary>
    /// Gets distance-based signal degradation
    /// </summary>
    public float GetDistanceDegradation(Entity<TelecomServerComponent> telecom, float distance)
    {
        if (distance <= telecom.Comp.MaxRange)
        {
            // Linear degradation within range
            var degradationFactor = telecom.Comp.Degradation * 0.5f; // Max 50% degradation from server state
            var distanceFactor = (distance / telecom.Comp.MaxRange) * 0.3f; // Max 30% degradation from distance
            return Math.Clamp(1.0f - degradationFactor - distanceFactor, 0.1f, 1.0f);
        }
        
        // Beyond max range, very poor signal
        return 0.1f;
    }
}