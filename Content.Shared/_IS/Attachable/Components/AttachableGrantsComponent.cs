using Content.Shared._IS.Attachable.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._IS.Attachable.Components;

/// <summary>
/// An attachment that adds components to the host while fitted, and takes them away on detach.
/// </summary>
/// <remarks>
/// Stands in for a handful of one-off RMC subsystems: it is how the bayonet gives a gun melee
/// damage and door prying, and how a scope gives it the Estoc's cursor-offset behaviour.
/// Only components this attachment added are removed again, so it will not strip something the
/// host already had.
/// </remarks>
[RegisterComponent, NetworkedComponent]
[Access(typeof(AttachableHolderSystem))]
public sealed partial class AttachableGrantsComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Names of the components we actually added, so detaching does not remove pre-existing ones.
    /// </summary>
    [DataField]
    public List<string> Granted = new();
}
