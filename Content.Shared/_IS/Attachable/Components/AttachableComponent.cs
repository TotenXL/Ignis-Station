using Content.Shared._IS.Attachable.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._IS.Attachable.Components;

/// <summary>
/// An item that can be fitted into an <see cref="AttachableHolderComponent"/> slot.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(AttachableHolderSystem))]
public sealed partial class AttachableComponent : Component
{
    /// <summary>
    /// Seconds to attach or detach.
    /// </summary>
    [DataField]
    public float AttachDoAfter = 1.5f;

    [DataField]
    public SoundSpecifier? AttachSound = new SoundPathSpecifier("/Audio/_IS/Attachable/attachment_add.ogg");

    [DataField]
    public SoundSpecifier? DetachSound = new SoundPathSpecifier("/Audio/_IS/Attachable/attachment_remove.ogg");

    /// <summary>
    /// Line added to the holder's examine text while this is attached.
    /// </summary>
    [DataField]
    public LocId? ExamineText;
}
