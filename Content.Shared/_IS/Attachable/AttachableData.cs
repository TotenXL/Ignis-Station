using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._IS.Attachable;

/// <summary>
/// One attachment point on a holder. Each slot backs a ContainerSlot whose id is the slot id.
/// </summary>
[DataDefinition]
public sealed partial class AttachableSlot
{
    /// <summary>
    /// What this slot will accept. Nothing fits a slot with no whitelist.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Spawned into the slot on map init.
    /// </summary>
    [DataField]
    public EntProtoId? StartingAttachable;

    /// <summary>
    /// Locked slots cannot be detached from.
    /// </summary>
    [DataField]
    public bool Locked;

    /// <summary>
    /// Display name for verbs and examine text.
    /// </summary>
    [DataField]
    public LocId Name = "is-attachable-slot-generic";
}

/// <summary>
/// Gates when a modifier set applies. None means always.
/// </summary>
[Flags]
public enum AttachableConditions : byte
{
    None = 0,
    Wielded = 1 << 0,
    Unwielded = 1 << 1,

    /// <summary>Attachment's own toggle is on.</summary>
    Active = 1 << 2,

    /// <summary>Attachment's own toggle is off.</summary>
    Inactive = 1 << 3,
}

/// <summary>
/// Flat deltas applied to the gun's stats. Mirrors RMC's AttachableWeaponRangedModifierSet,
/// restricted to the fields vanilla's GunRefreshModifiersEvent actually exposes.
/// </summary>
[DataDefinition]
public sealed partial class AttachableGunModifierSet
{
    [DataField]
    public AttachableConditions Conditions = AttachableConditions.None;

    /// <summary>
    /// Degrees added to both ends of the scatter cone. Negative tightens it.
    /// </summary>
    [DataField]
    public double ScatterFlat;

    /// <summary>
    /// Added to the camera kick. Negative softens it.
    /// </summary>
    [DataField]
    public float RecoilFlat;

    /// <summary>
    /// Seconds added to the interval between shots. Negative fires faster.
    /// </summary>
    [DataField]
    public float FireDelayFlat;

    [DataField]
    public int ShotsPerBurstFlat;

    [DataField]
    public float ProjectileSpeedFlat;

    /// <summary>
    /// Degrees added to how fast the cone climbs per shot.
    /// </summary>
    [DataField]
    public double AngleIncreaseFlat;

    /// <summary>
    /// Degrees per second added to how fast the cone recovers.
    /// </summary>
    [DataField]
    public double AngleDecayFlat;
}

[DataDefinition]
public sealed partial class AttachableMeleeModifierSet
{
    [DataField]
    public AttachableConditions Conditions = AttachableConditions.None;

    [DataField]
    public DamageSpecifier? BonusDamage;
}

/// <summary>
/// Raised on an attachment when its granted toggle action is used.
/// </summary>
public sealed partial class AttachableToggleActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class AttachableToggleDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class AttachableAttachDoAfterEvent : DoAfterEvent
{
    [DataField]
    public string SlotId = string.Empty;

    public AttachableAttachDoAfterEvent(string slotId)
    {
        SlotId = slotId;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class AttachableDetachDoAfterEvent : DoAfterEvent
{
    [DataField]
    public string SlotId = string.Empty;

    public AttachableDetachDoAfterEvent(string slotId)
    {
        SlotId = slotId;
    }

    public override DoAfterEvent Clone() => this;
}
