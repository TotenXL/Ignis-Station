using Content.Shared._IS.Attachable.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._IS.Attachable.Systems;

/// <summary>
/// Swaps the host gun's gunshot sound and optionally hides its muzzle flash.
/// </summary>
public sealed partial class AttachableSilencerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableSilencerComponent, GunRefreshModifiersEvent>(OnRefreshModifiers);
        SubscribeLocalEvent<AttachableSilencerComponent, GunMuzzleFlashAttemptEvent>(OnMuzzleFlashAttempt);
    }

    private void OnRefreshModifiers(Entity<AttachableSilencerComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (ent.Comp.Sound != null)
            args.SoundGunshot = ent.Comp.Sound;
    }

    private void OnMuzzleFlashAttempt(Entity<AttachableSilencerComponent> ent, ref GunMuzzleFlashAttemptEvent args)
    {
        if (ent.Comp.HideMuzzleFlash)
            args.Cancelled = true;
    }
}
