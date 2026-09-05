namespace Content.Shared._Funkystation.Footprints;

[RegisterComponent]
public sealed partial class FootprintOwnerComponent : Component
{
    [DataField] public float MaxFootVolume = 10f;
    [DataField] public float MaxBodyVolume = 20f;

    [DataField] public float MinPrintVolume = 0.5f;
    [DataField] public float MaxFootprintVolume = 1f;

    [DataField] public float MinBodyPrintVolume = 2f;
    [DataField] public float MaxBodyprintVolume = 5f;

    [DataField] public float FootstepDistance = 0.5f;
    [DataField] public float DragDistance = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float DistanceWalked;

    [DataField] public float AlternateStepOffset = 0.0625f;

    /// <summary>
    /// Scales print alpha relative to how much solution the print holds.
    /// Upstream hardcoded this at 0.5, which capped prints at half transparency; 1 means a
    /// full-volume print is fully opaque. Raise above 1 to make faint prints show up sooner.
    /// </summary>
    [DataField] public float PrintOpacity = 1f;
}
