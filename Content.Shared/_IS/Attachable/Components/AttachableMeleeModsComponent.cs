using Content.Shared._IS.Attachable.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._IS.Attachable.Components;

/// <summary>
/// An attachment that adds damage to the host's melee attacks, such as a bayonet.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(AttachableModifiersSystem))]
public sealed partial class AttachableMeleeModsComponent : Component
{
    [DataField(required: true)]
    public List<AttachableMeleeModifierSet> Modifiers = new();
}
