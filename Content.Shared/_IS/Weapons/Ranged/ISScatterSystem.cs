using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;

namespace Content.Shared._IS.Weapons.Ranged;

/// <summary>
/// Computes a gun's scatter cone, camera recoil and fire rate from <see cref="ISScatterComponent"/>,
/// taking wield state and the selected fire mode into account.
/// </summary>
/// <remarks>
/// Everything is written into the GunRefreshModifiersEvent rather than onto GunComponent directly,
/// so attachment modifiers layer on top. Attachment systems must subscribe after this one.
/// </remarks>
public sealed partial class ISScatterSystem : EntitySystem
{
    [Dependency] private SharedGunSystem _gun = null!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ISScatterComponent, GunRefreshModifiersEvent>(OnRefreshModifiers);
        SubscribeLocalEvent<ISScatterComponent, ItemWieldedEvent>(OnWielded);
        SubscribeLocalEvent<ISScatterComponent, ItemUnwieldedEvent>(OnUnwielded);
    }

    // SharedWieldableSystem only refreshes guns that carry a GunWieldBonusComponent, so guns using
    // this component instead have to ask for the refresh themselves.
    private void OnWielded(Entity<ISScatterComponent> ent, ref ItemWieldedEvent args)
    {
        _gun.RefreshModifiers(ent.Owner);
    }

    private void OnUnwielded(Entity<ISScatterComponent> ent, ref ItemUnwieldedEvent args)
    {
        _gun.RefreshModifiers(ent.Owner);
    }

    private void OnRefreshModifiers(Entity<ISScatterComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var comp = ent.Comp;
        var wielded = TryComp(ent, out WieldableComponent? wieldable) && wieldable.Wielded;

        var min = wielded ? comp.ScatterWielded : comp.ScatterUnwielded;
        var max = min;
        var angleIncrease = Angle.Zero;

        var fireRate = comp.BaseFireRate;
        if (args.Gun.Comp.SelectedMode == SelectiveFire.Burst)
            fireRate *= comp.BurstFireRateMultiplier;

        if (comp.Modifiers.TryGetValue(args.Gun.Comp.SelectedMode, out var mods))
        {
            var mult = mods.UseBurstScatterMult ? comp.BurstScatterMult : 1.0;
            if (!wielded)
                mult *= mods.UnwieldedScatterMultiplier;

            // Modifier sets only ever widen the cone; they never tighten it below the base scatter.
            max = Angle.FromDegrees(Math.Max(min.Degrees + mods.MaxScatterModifier * mult, min.Degrees));

            if (mods.ShotsToMaxScatter is { } shots && shots > 0)
                angleIncrease = new Angle((max - min).Theta / shots);

            if (mods.FireDelay != 0f && fireRate > 0f)
                fireRate = 1f / (1f / fireRate + mods.FireDelay);
        }

        args.MinAngle = min;
        args.MaxAngle = max;
        args.AngleIncrease = angleIncrease;
        args.AngleDecay = comp.ScatterDecay;
        args.FireRate = fireRate;
        args.CameraRecoilScalar = wielded ? comp.RecoilWielded : comp.RecoilUnwielded;
    }
}
