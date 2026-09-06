using Content.Shared._IS.Attachable.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._IS.Attachable.Components;

/// <summary>
/// Put on a user who has a <see cref="AttachableToggleableComponent"/> with BreakOnMove switched on,
/// so we can switch it back off the moment they move. This is what stops someone bracing a bipod and
/// then walking off with the accuracy bonus.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableToggleableSystem))]
public sealed partial class AttachableBracedComponent : Component
{
    /// <summary>
    /// The attachments being braced. A user could conceivably brace two guns.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Attachables = new();
}
