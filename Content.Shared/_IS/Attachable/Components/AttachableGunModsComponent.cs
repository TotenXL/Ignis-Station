using Content.Shared._IS.Attachable.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._IS.Attachable.Components;

/// <summary>
/// An attachment that changes the host gun's stats while fitted.
/// </summary>
/// <remarks>
/// Sets are applied additively in order, and each is gated on its own conditions, so a single
/// attachment can buff while wielded and penalise while fired from the hip.
/// </remarks>
[RegisterComponent, NetworkedComponent]
[Access(typeof(AttachableModifiersSystem))]
public sealed partial class AttachableGunModsComponent : Component
{
    [DataField(required: true)]
    public List<AttachableGunModifierSet> Modifiers = new();
}
