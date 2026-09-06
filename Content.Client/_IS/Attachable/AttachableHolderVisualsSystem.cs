using Content.Shared._IS.Attachable.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Client._IS.Attachable;

/// <summary>
/// Draws attachments onto their holder, one sprite layer per slot.
/// </summary>
/// <remarks>
/// Redraws off container insert/remove rather than a custom networked event, since those already
/// fire on both sides. Layers are keyed in the sprite layer map by slot id.
/// </remarks>
public sealed partial class AttachableHolderVisualsSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = null!;
    [Dependency] private SpriteSystem _sprite = null!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableHolderVisualsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AttachableHolderVisualsComponent, EntInsertedIntoContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<AttachableHolderVisualsComponent, EntRemovedFromContainerMessage>(OnContainerChanged);

        // Toggling changes which state we draw, and Active is networked on the attachment.
        SubscribeLocalEvent<AttachableToggleableComponent, AfterAutoHandleStateEvent>(OnToggleStateChanged);
    }

    private void OnStartup(Entity<AttachableHolderVisualsComponent> ent, ref ComponentStartup args)
    {
        RefreshAll(ent);
    }

    private void OnContainerChanged(Entity<AttachableHolderVisualsComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        RefreshSlot(ent, args.Container.ID);
    }

    private void OnContainerChanged(Entity<AttachableHolderVisualsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        RefreshSlot(ent, args.Container.ID);
    }

    private void OnToggleStateChanged(Entity<AttachableToggleableComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            return;

        if (!TryComp(container.Owner, out AttachableHolderVisualsComponent? holder))
            return;

        RefreshSlot((container.Owner, holder), container.ID);
    }

    private void RefreshAll(Entity<AttachableHolderVisualsComponent> ent)
    {
        foreach (var slotId in ent.Comp.Offsets.Keys)
        {
            RefreshSlot(ent, slotId);
        }
    }

    private void RefreshSlot(Entity<AttachableHolderVisualsComponent> ent, string slotId)
    {
        if (!ent.Comp.Offsets.TryGetValue(slotId, out var slotOffset))
            return;

        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var attachable = GetAttachable(ent, slotId);

        if (attachable == null ||
            !TryComp(attachable, out AttachableVisualsComponent? visuals) ||
            ResolveRsi(attachable.Value, visuals) is not { } rsi)
        {
            _sprite.RemoveLayer((ent.Owner, sprite), slotId, logMissing: false);
            return;
        }

        var state = visuals.State;
        if (visuals.ShowActive &&
            TryComp(attachable, out AttachableToggleableComponent? toggle) &&
            toggle.Active)
        {
            state += "-on";
        }

        var data = new PrototypeLayerData
        {
            RsiPath = rsi.ToString(),
            State = state,
            Offset = slotOffset + visuals.Offset,
            Visible = true,
        };

        _sprite.LayerMapReserve((ent.Owner, sprite), slotId);
        _sprite.LayerSetData((ent.Owner, sprite), slotId, data);
    }

    private ResPath? ResolveRsi(EntityUid attachable, AttachableVisualsComponent visuals)
    {
        if (visuals.Rsi is { } rsi)
            return rsi;

        if (TryComp(attachable, out SpriteComponent? sprite) && sprite.BaseRSI?.Path is { } path)
            return path;

        return null;
    }

    private EntityUid? GetAttachable(EntityUid holder, string slotId)
    {
        if (!_container.TryGetContainer(holder, slotId, out var container))
            return null;

        return container.ContainedEntities.Count > 0 ? container.ContainedEntities[0] : null;
    }
}
