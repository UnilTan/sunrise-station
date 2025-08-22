using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Storage.Components;

namespace Content.Shared.Eye.Blinding.Systems;

/// <summary>
/// Shared system that manages limited vision for entities when they are inside containers.
/// The disposal part is handled by the server system.
/// </summary>
public sealed class VisionLimitedInContainersSystem : EntitySystem
{
    [Dependency] private readonly BlurryVisionSystem _blurryVisionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Listen for entities entering/leaving storage containers
        SubscribeLocalEvent<InsideEntityStorageComponent, ComponentInit>(OnInsideStorageInit);
        SubscribeLocalEvent<InsideEntityStorageComponent, ComponentShutdown>(OnInsideStorageShutdown);

        // Listen for blur events to add our container blur
        SubscribeLocalEvent<VisionLimitedInContainersComponent, GetBlurEvent>(OnGetBlur);
    }

    private void OnGetBlur(EntityUid uid, VisionLimitedInContainersComponent component, ref GetBlurEvent args)
    {
        if (component.IsLimitedVision)
        {
            args.Blur += component.BlurMagnitude;
        }
    }

    private void OnInsideStorageInit(EntityUid uid, InsideEntityStorageComponent component, ComponentInit args)
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

    private void OnInsideStorageShutdown(EntityUid uid, InsideEntityStorageComponent component, ComponentShutdown args)
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