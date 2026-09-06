using Content.Shared._IS.Attachable.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._IS.Attachable.Systems;

/// <summary>
/// Attachments the user can switch on and off, such as a rail flashlight or a bipod.
/// </summary>
/// <remarks>
/// The action lives on the attachment itself and is handed to whoever picks up the host, the same
/// way upstream's <c>ActionGrantSystem</c> does it. Attachments with real behaviour of their own
/// carry ItemToggle; this drives that and exposes Active for modifier sets and sprite states.
/// </remarks>
public sealed partial class AttachableToggleableSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = null!;
    [Dependency] private SharedAudioSystem _audio = null!;
    [Dependency] private SharedContainerSystem _container = null!;
    [Dependency] private SharedDoAfterSystem _doAfter = null!;
    [Dependency] private SharedGunSystem _gun = null!;
    [Dependency] private SharedPopupSystem _popup = null!;
    [Dependency] private ItemToggleSystem _itemToggle = null!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableToggleableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AttachableToggleableComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AttachableToggleableComponent, GotEquippedHandEvent>(OnHolderEquipped);
        SubscribeLocalEvent<AttachableToggleableComponent, GotUnequippedHandEvent>(OnHolderUnequipped);
        SubscribeLocalEvent<AttachableToggleableComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleDoAfterEvent>(OnToggleDoAfter);
        SubscribeLocalEvent<AttachableToggleableComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);

        SubscribeLocalEvent<AttachableBracedComponent, MoveEvent>(OnBracedMove);
    }

    // Anything that braces (the bipod) drops out of it as soon as the user actually moves.
    private void OnBracedMove(Entity<AttachableBracedComponent> ent, ref MoveEvent args)
    {
        if (args.OnlyRotation)
            return;

        foreach (var attachable in new List<EntityUid>(ent.Comp.Attachables))
        {
            if (TryComp(attachable, out AttachableToggleableComponent? toggle) && toggle.Active)
            {
                _container.TryGetContainingContainer((attachable, null, null), out var container);
                SetActive((attachable, toggle), false, container?.Owner, ent.Owner);
            }
            else
            {
                ent.Comp.Attachables.Remove(attachable);
            }
        }

        if (ent.Comp.Attachables.Count == 0)
            RemCompDeferred<AttachableBracedComponent>(ent);
    }

    private void OnMapInit(Entity<AttachableToggleableComponent> ent, ref MapInitEvent args)
    {
        var action = ent.Comp.ActionEntity;
        _actions.AddAction(ent.Owner, ref action, ent.Comp.Action);
        ent.Comp.ActionEntity = action;
    }

    private void OnShutdown(Entity<AttachableToggleableComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ActionEntity is { } action)
            _actions.RemoveAction(ent.Owner, action);
    }

    // These reach us because AttachableHolderSystem relays them to everything attached.
    // GrantContainedActions takes the action container as the attachment itself, which is where
    // OnMapInit put the action - handing the holder over instead is what silently granted nothing.
    private void OnHolderEquipped(EntityUid uid, AttachableToggleableComponent comp, GotEquippedHandEvent args)
    {
        _actions.GrantContainedActions(args.User, uid);
    }

    private void OnHolderUnequipped(EntityUid uid, AttachableToggleableComponent comp, GotUnequippedHandEvent args)
    {
        _actions.RemoveProvidedActions(args.User, uid);
    }

    // Attaching to a gun someone is already holding should hand them the action there and then,
    // rather than making them put the gun down and pick it back up.
    private void OnInsertedIntoContainer(Entity<AttachableToggleableComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (GetHolderUser(args.Container.Owner) is { } user)
            _actions.GrantContainedActions(user, ent.Owner);
    }

    // Switching off when pulled out of the gun keeps Active from getting stuck on.
    private void OnRemovedFromContainer(Entity<AttachableToggleableComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (GetHolderUser(args.Container.Owner) is { } user)
            _actions.RemoveProvidedActions(user, ent.Owner);

        if (ent.Comp.Active)
            SetActive(ent, false, args.Container.Owner);
    }

    /// <summary>
    /// Whoever is holding the given holder, if anyone.
    /// </summary>
    private EntityUid? GetHolderUser(EntityUid holder)
    {
        if (!_container.TryGetContainingContainer((holder, null, null), out var container))
            return null;

        return HasComp<HandsComponent>(container.Owner) ? container.Owner : null;
    }

    private void OnToggleAction(EntityUid uid, AttachableToggleableComponent comp, AttachableToggleActionEvent args)
    {
        if (args.Handled)
            return;

        var ent = new Entity<AttachableToggleableComponent>(uid, comp);
        args.Handled = true;

        if (!_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
        {
            if (ent.Comp.AttachedOnly)
            {
                _popup.PopupEntity(Loc.GetString("is-attachable-toggle-fail-detached"), ent, args.Performer);
                return;
            }
        }

        var holder = container?.Owner;

        if (ent.Comp.WieldedOnly && holder != null &&
            (!TryComp(holder, out WieldableComponent? wieldable) || !wieldable.Wielded))
        {
            _popup.PopupEntity(Loc.GetString("is-attachable-toggle-fail-unwielded"), ent, args.Performer);
            return;
        }

        if (ent.Comp.DoAfter <= 0f)
        {
            SetActive(ent, !ent.Comp.Active, holder, args.Performer);
            return;
        }

        var doAfter = new DoAfterArgs(
            EntityManager,
            args.Performer,
            ent.Comp.DoAfter,
            new AttachableToggleDoAfterEvent(),
            ent,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnToggleDoAfter(Entity<AttachableToggleableComponent> ent, ref AttachableToggleDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        _container.TryGetContainingContainer((ent.Owner, null, null), out var container);
        SetActive(ent, !ent.Comp.Active, container?.Owner, args.User);
    }

    /// <summary>
    /// Flips the attachment on or off and refreshes whatever it is fitted to.
    /// </summary>
    public void SetActive(
        Entity<AttachableToggleableComponent> ent,
        bool active,
        EntityUid? holder = null,
        EntityUid? user = null)
    {
        if (ent.Comp.Active == active)
            return;

        ent.Comp.Active = active;
        Dirty(ent);

        // Attachments with their own behaviour (a flashlight's light, say) hang it off ItemToggle.
        if (HasComp<ItemToggleComponent>(ent))
            _itemToggle.TrySetActive(ent.Owner, active, user);

        if (ent.Comp.BreakOnMove && user != null)
            SetBraced(user.Value, ent.Owner, active);

        var sound = active ? ent.Comp.ActivateSound : ent.Comp.DeactivateSound;
        _audio.PlayPredicted(sound, ent, user);

        var popup = active ? ent.Comp.ActivatePopup : ent.Comp.DeactivatePopup;
        if (popup != null && user != null)
            _popup.PopupEntity(Loc.GetString(popup, ("attachable", ent.Owner)), ent, user.Value);

        if (holder != null && HasComp<GunComponent>(holder.Value))
            _gun.RefreshModifiers(holder.Value);
    }

    private void SetBraced(EntityUid user, EntityUid attachable, bool braced)
    {
        if (braced)
        {
            var comp = EnsureComp<AttachableBracedComponent>(user);
            comp.Attachables.Add(attachable);
            Dirty(user, comp);
            return;
        }

        if (!TryComp(user, out AttachableBracedComponent? braceComp))
            return;

        braceComp.Attachables.Remove(attachable);
        Dirty(user, braceComp);

        if (braceComp.Attachables.Count == 0)
            RemCompDeferred<AttachableBracedComponent>(user);
    }
}
