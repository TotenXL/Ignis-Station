using Content.Shared._IS.Attachable.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._IS.Attachable.Components;

/// <summary>
/// An attachment that muffles the host gun, such as a suppressor.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(AttachableSilencerSystem))]
public sealed partial class AttachableSilencerComponent : Component
{
    /// <summary>
    /// Replaces the gun's normal gunshot sound while attached.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public bool HideMuzzleFlash = true;
}
