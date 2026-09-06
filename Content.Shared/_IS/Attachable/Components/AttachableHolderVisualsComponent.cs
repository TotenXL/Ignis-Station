using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._IS.Attachable.Components;

/// <summary>
/// Where each slot's attachment sprite sits on the holder.
/// </summary>
/// <remarks>
/// A slot with no entry here never draws its attachment, which is the way to have a functional
/// slot with no art for it.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class AttachableHolderVisualsComponent : Component
{
    /// <summary>
    /// Slot id to sprite offset, in tiles.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, Vector2> Offsets = new();
}
