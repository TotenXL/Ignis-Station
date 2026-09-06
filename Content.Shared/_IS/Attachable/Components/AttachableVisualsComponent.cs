using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._IS.Attachable.Components;

/// <summary>
/// The sprite an attachment draws on its holder while fitted.
/// </summary>
/// <remarks>
/// States are named explicitly rather than derived from the attachment's own sprite. RMC infers
/// them from the item's current state plus an "_a" suffix, which is terse but breaks quietly when
/// an attachment's inventory sprite changes.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class AttachableVisualsComponent : Component
{
    /// <summary>
    /// RSI to draw from. Falls back to the attachment's own sprite.
    /// </summary>
    [DataField]
    public ResPath? Rsi;

    /// <summary>
    /// State drawn while attached.
    /// </summary>
    [DataField(required: true)]
    public string State = string.Empty;

    /// <summary>
    /// Draw "<see cref="State"/>-on" instead while the attachment's toggle is active.
    /// </summary>
    [DataField]
    public bool ShowActive;

    /// <summary>
    /// Nudge applied on top of the holder's offset for this slot.
    /// </summary>
    [DataField]
    public Vector2 Offset = Vector2.Zero;
}
