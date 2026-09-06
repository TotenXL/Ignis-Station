using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._IS.Weapons.Ranged;

/// <summary>
/// Drives a gun's vanilla scatter cone, camera recoil and fire rate from RMC-14 style inputs:
/// a wielded/unwielded split plus per-fire-mode scatter that climbs over a set number of shots.
/// Ported from RMC-14's RMCSelectiveFireComponent, which is itself just a conversion layer over
/// the same vanilla <see cref="GunComponent"/> fields.
/// </summary>
/// <remarks>
/// All the work happens inside <see cref="ISScatterSystem"/>'s GunRefreshModifiersEvent handler,
/// so attachment modifiers stack cleanly on top of whatever this computes.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ISScatterSystem))]
public sealed partial class ISScatterComponent : Component
{
    /// <summary>
    /// Base cone width while wielded. This is the full cone, not a half-angle: vanilla's
    /// GetRecoilAngle multiplies it by random(-0.5, 0.5).
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle ScatterWielded = Angle.FromDegrees(10);

    /// <summary>
    /// Base cone width while not wielded. Usually much worse than <see cref="ScatterWielded"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle ScatterUnwielded = Angle.FromDegrees(10);

    /// <summary>
    /// Camera kick while wielded. Feeds GunComponent.CameraRecoilScalar.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RecoilWielded = 1f;

    /// <summary>
    /// Camera kick while not wielded.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RecoilUnwielded = 1f;

    /// <summary>
    /// Shots per second before any per-fire-mode fire delay is folded in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseFireRate = 1.429f;

    /// <summary>
    /// Burst mode multiplies <see cref="BaseFireRate"/> by this to get the intra-burst rate.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BurstFireRateMultiplier = 2f;

    /// <summary>
    /// Scales <see cref="ISScatterModifierSet.MaxScatterModifier"/> for modes that opt in via
    /// <see cref="ISScatterModifierSet.UseBurstScatterMult"/>. Lower is tighter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public double BurstScatterMult = 4.0;

    /// <summary>
    /// How fast the cone recovers, in degrees per second.
    /// </summary>
    /// <remarks>
    /// RMC instead sets decay to zero and snaps CurrentAngle back to the minimum when the trigger
    /// is released. GunComponent is [Access]-restricted to SharedGunSystem, so we can't write
    /// CurrentAngle from here without editing core; a high decay rate approximates it closely
    /// enough. Revisit if the recovery ever feels sluggish.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public Angle ScatterDecay = Angle.FromDegrees(75);

    /// <summary>
    /// Per-fire-mode tuning. A mode with no entry here gets a flat cone that never climbs.
    /// </summary>
    [DataField]
    public Dictionary<SelectiveFire, ISScatterModifierSet> Modifiers = new()
    {
        [SelectiveFire.Burst] = new ISScatterModifierSet
        {
            FireDelay = 0.1f,
            MaxScatterModifier = 10.0,
            ShotsToMaxScatter = 6,
        },
        [SelectiveFire.FullAuto] = new ISScatterModifierSet
        {
            MaxScatterModifier = 26.0,
            ShotsToMaxScatter = 4,
        },
    };
}

[DataDefinition]
public sealed partial class ISScatterModifierSet
{
    /// <summary>
    /// Seconds added to the interval between shots in this mode.
    /// </summary>
    [DataField]
    public float FireDelay;

    /// <summary>
    /// Degrees the cone may climb above the base scatter in this mode.
    /// </summary>
    [DataField]
    public double MaxScatterModifier;

    /// <summary>
    /// Whether <see cref="MaxScatterModifier"/> is scaled by the gun's BurstScatterMult.
    /// </summary>
    [DataField]
    public bool UseBurstScatterMult = true;

    /// <summary>
    /// Extra multiplier on <see cref="MaxScatterModifier"/> when firing unwielded.
    /// </summary>
    [DataField]
    public double UnwieldedScatterMultiplier = 2.0;

    /// <summary>
    /// Shots needed to climb from the base scatter to the maximum. Null means the cone is flat.
    /// </summary>
    [DataField]
    public int? ShotsToMaxScatter;
}
