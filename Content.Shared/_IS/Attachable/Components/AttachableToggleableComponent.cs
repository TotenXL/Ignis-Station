using Content.Shared._IS.Attachable.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._IS.Attachable.Components;

/// <summary>
/// An attachment the user can switch on and off, granting an action while the host is held.
/// </summary>
/// <remarks>
/// Trimmed hard from RMC-14's 34-datafield version. Attachments that have real on/off behaviour of
/// their own (a flashlight's light, say) carry ItemToggle as well; this just drives that toggle and
/// exposes <see cref="Active"/> so modifier sets and sprite states can key off it.
/// </remarks>
// raiseAfterAutoHandleState lets the client redraw the holder when Active flips.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
[Access(typeof(AttachableToggleableSystem))]
public sealed partial class AttachableToggleableComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// Action granted to whoever is holding the host.
    /// </summary>
    [DataField]
    public EntProtoId Action = "ActionToggleAttachable";

    /// <summary>
    /// Spawned action entity, while granted.
    /// </summary>
    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public LocId? ActionName;

    [DataField]
    public LocId? ActionDesc;

    [DataField]
    public SpriteSpecifier? Icon;

    [DataField]
    public SpriteSpecifier? IconActive;

    /// <summary>
    /// Seconds to toggle. Zero toggles instantly.
    /// </summary>
    [DataField]
    public float DoAfter;

    /// <summary>
    /// Cannot be toggled while off the host.
    /// </summary>
    [DataField]
    public bool AttachedOnly = true;

    /// <summary>
    /// Cannot be toggled unless the host is wielded.
    /// </summary>
    [DataField]
    public bool WieldedOnly;

    /// <summary>
    /// Switches back off when the user moves. Used by the bipod.
    /// </summary>
    [DataField]
    public bool BreakOnMove;

    [DataField]
    public SoundSpecifier? ActivateSound;

    [DataField]
    public SoundSpecifier? DeactivateSound;

    [DataField]
    public LocId? ActivatePopup;

    [DataField]
    public LocId? DeactivatePopup;
}
