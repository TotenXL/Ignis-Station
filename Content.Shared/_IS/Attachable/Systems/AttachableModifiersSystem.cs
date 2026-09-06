using Content.Shared._IS.Attachable.Components;
using Content.Shared._IS.Weapons.Ranged;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;

namespace Content.Shared._IS.Attachable.Systems;

/// <summary>
/// Applies attachment stat modifiers to the host gun.
/// </summary>
/// <remarks>
/// These run after <see cref="ISScatterSystem"/> so that attachment deltas layer on top of the
/// base cone it computes, rather than being overwritten by it.
/// </remarks>
public sealed partial class AttachableModifiersSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableGunModsComponent, GunRefreshModifiersEvent>(
            OnGunRefreshModifiers,
            after: [typeof(ISScatterSystem)]);

        SubscribeLocalEvent<AttachableMeleeModsComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnGunRefreshModifiers(Entity<AttachableGunModsComponent> ent, ref GunRefreshModifiersEvent args)
    {
        foreach (var mods in ent.Comp.Modifiers)
        {
            if (!CanApply(ent, mods.Conditions, args.Gun.Owner))
                continue;

            // The cone can be tightened but never inverted, and max must stay at or above min.
            args.MinAngle = Angle.FromDegrees(Math.Max(args.MinAngle.Degrees + mods.ScatterFlat, 0));
            args.MaxAngle = Angle.FromDegrees(Math.Max(args.MaxAngle.Degrees + mods.ScatterFlat, args.MinAngle.Degrees));

            args.AngleIncrease = Angle.FromDegrees(Math.Max(args.AngleIncrease.Degrees + mods.AngleIncreaseFlat, 0));
            args.AngleDecay = Angle.FromDegrees(Math.Max(args.AngleDecay.Degrees + mods.AngleDecayFlat, 0));

            args.CameraRecoilScalar = Math.Max(args.CameraRecoilScalar + mods.RecoilFlat, 0);
            args.ShotsPerBurst = Math.Max(args.ShotsPerBurst + mods.ShotsPerBurstFlat, 1);
            args.ProjectileSpeed = Math.Max(args.ProjectileSpeed + mods.ProjectileSpeedFlat, 0.1f);

            if (mods.FireDelayFlat != 0f && args.FireRate > 0f)
            {
                var interval = 1f / args.FireRate + mods.FireDelayFlat;
                args.FireRate = interval > 0f ? 1f / interval : args.FireRate;
            }
        }
    }

    private void OnMeleeHit(Entity<AttachableMeleeModsComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        foreach (var mods in ent.Comp.Modifiers)
        {
            if (mods.BonusDamage == null || !CanApply(ent, mods.Conditions, args.Weapon))
                continue;

            args.BonusDamage += mods.BonusDamage;
        }
    }

    /// <summary>
    /// Whether a modifier set's conditions are met, given the attachment and its host.
    /// </summary>
    private bool CanApply(EntityUid attachable, AttachableConditions conditions, EntityUid holder)
    {
        if (conditions == AttachableConditions.None)
            return true;

        if ((conditions & (AttachableConditions.Wielded | AttachableConditions.Unwielded)) != 0)
        {
            var wielded = TryComp(holder, out WieldableComponent? wieldable) && wieldable.Wielded;

            if (wielded && (conditions & AttachableConditions.Unwielded) != 0)
                return false;

            if (!wielded && (conditions & AttachableConditions.Wielded) != 0)
                return false;
        }

        if ((conditions & (AttachableConditions.Active | AttachableConditions.Inactive)) != 0)
        {
            var active = TryComp(attachable, out AttachableToggleableComponent? toggle) && toggle.Active;

            if (active && (conditions & AttachableConditions.Inactive) != 0)
                return false;

            if (!active && (conditions & AttachableConditions.Active) != 0)
                return false;
        }

        return true;
    }
}
