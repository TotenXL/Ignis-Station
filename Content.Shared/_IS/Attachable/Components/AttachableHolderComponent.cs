using Content.Shared._IS.Attachable.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._IS.Attachable.Components;

/// <summary>
/// A gun (or other item) that accepts attachments in named slots. Each slot id backs a
/// ContainerSlot of the same name.
/// </summary>
/// <remarks>
/// Ported thin from RMC-14's AttachableHolderComponent, built on the same container-and-relay
/// pattern upstream already uses for <c>UpgradeableGunComponent</c>.
/// </remarks>
[RegisterComponent, NetworkedComponent]
[Access(typeof(AttachableHolderSystem))]
public sealed partial class AttachableHolderComponent : Component
{
    /// <summary>
    /// Slot id to slot definition. Ids double as container ids, so keep them stable.
    /// Convention: is-aslot-barrel, is-aslot-rail, is-aslot-stock, is-aslot-underbarrel.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, AttachableSlot> Slots = new();
}
