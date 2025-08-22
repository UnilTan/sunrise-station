using Content.Server.Disposal.Unit;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;

namespace Content.Server.Eye.Blinding.Systems;

/// <summary>
/// Server system that handles limited vision for entities when they are in disposal tubes.
/// </summary>
public sealed class VisionLimitedInDisposalSystem : EntitySystem
{
    [Dependency] private readonly BlurryVisionSystem _blurryVisionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Listen for entities entering/leaving disposal
        SubscribeLocalEvent<BeingDisposedComponent, ComponentInit>(OnBeingDisposedInit);
        SubscribeLocalEvent<BeingDisposedComponent, ComponentShutdown>(OnBeingDisposedShutdown);
    }

    private void OnBeingDisposedInit(EntityUid uid, BeingDisposedComponent component, ComponentInit args)
    {
        if (TryComp<VisionLimitedInContainersComponent>(uid, out var visionComp))
        {
            visionComp.IsLimitedVision = true;
            Dirty(uid, visionComp);
            
            // Update blur magnitude using the existing system
            if (TryComp<BlindableComponent>(uid, out var blindable))
            {
                _blurryVisionSystem.UpdateBlurMagnitude((uid, blindable));
            }
        }
    }

    private void OnBeingDisposedShutdown(EntityUid uid, BeingDisposedComponent component, ComponentShutdown args)
    {
        if (TryComp<VisionLimitedInContainersComponent>(uid, out var visionComp))
        {
            visionComp.IsLimitedVision = false;
            Dirty(uid, visionComp);
            
            // Update blur magnitude using the existing system
            if (TryComp<BlindableComponent>(uid, out var blindable))
            {
                _blurryVisionSystem.UpdateBlurMagnitude((uid, blindable));
            }
        }
    }
}