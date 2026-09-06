using Content.Shared._IS.Attachable.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._IS.Attachable.Systems;

/// <summary>
/// Attaching and detaching items in a holder's named slots, and relaying events from the holder
/// down to whatever is attached.
/// </summary>
/// <remarks>
/// Built on the same container-and-relay shape as upstream's <c>GunUpgradeSystem</c>, generalised
/// to named slots with removal and a doafter. Deliberately has no BUI: an attachment that fits more
/// than one slot takes the first declared match, and detaching is a per-slot verb.
/// </remarks>
public sealed partial class AttachableHolderSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = null!;
    [Dependency] private SharedContainerSystem _container = null!;
    [Dependency] private SharedDoAfterSystem _doAfter = null!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = null!;
    [Dependency] private SharedGunSystem _gun = null!;
    [Dependency] private SharedHandsSystem _hands = null!;
    [Dependency] private SharedPopupSystem _popup = null!;
    [Dependency] private IComponentFactory _componentFactory = null!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableHolderComponent, ComponentInit>(OnHolderInit);
        SubscribeLocalEvent<AttachableHolderComponent, MapInitEvent>(OnHolderMapInit);
        SubscribeLocalEvent<AttachableHolderComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AttachableHolderComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<AttachableHolderComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<AttachableHolderComponent, AttachableAttachDoAfterEvent>(OnAttachDoAfter);
        SubscribeLocalEvent<AttachableHolderComponent, AttachableDetachDoAfterEvent>(OnDetachDoAfter);

        // Anything raised on the holder that an attachment might want to answer.
        SubscribeLocalEvent<AttachableHolderComponent, GunRefreshModifiersEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, GunMuzzleFlashAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, GunShotEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, MeleeHitEvent>(RelayEvent);

        // Attachments grant their actions off these rather than GetItemActionsEvent: that event's
        // GrantActions call resolves the action container against the *holder*, but an attachment's
        // actions live in the attachment's own container, so it would find nothing and bail.
        SubscribeLocalEvent<AttachableHolderComponent, GotEquippedHandEvent>(RelayEventNotByRef);
        SubscribeLocalEvent<AttachableHolderComponent, GotUnequippedHandEvent>(RelayEventNotByRef);
    }

    #region Relay

    private void RelayEvent<T>(Entity<AttachableHolderComponent> ent, ref T args) where T : notnull
    {
        foreach (var attachable in GetAttachables(ent))
        {
            RaiseLocalEvent(attachable, ref args);
        }
    }

    private void RelayEventNotByRef<T>(EntityUid uid, AttachableHolderComponent comp, T args)
        where T : EntityEventArgs
    {
        foreach (var attachable in GetAttachables((uid, comp)))
        {
            RaiseLocalEvent(attachable, args);
        }
    }

    #endregion

    /// <summary>
    /// Recomputes the holder's stats. Holders that are not guns simply have nothing to refresh.
    /// </summary>
    public void RefreshHolder(EntityUid holder)
    {
        if (TryComp(holder, out GunComponent? gun))
            _gun.RefreshModifiers((holder, gun));
    }

    #region Queries

    /// <summary>
    /// Every attachment currently fitted, in slot declaration order.
    /// </summary>
    public IEnumerable<EntityUid> GetAttachables(Entity<AttachableHolderComponent> ent)
    {
        foreach (var slotId in ent.Comp.Slots.Keys)
        {
            if (GetAttachable(ent, slotId) is { } attachable)
                yield return attachable;
        }
    }

    /// <summary>
    /// What is in the given slot, if anything.
    /// </summary>
    public EntityUid? GetAttachable(Entity<AttachableHolderComponent> ent, string slotId)
    {
        if (!_container.TryGetContainer(ent, slotId, out var container))
            return null;

        return container.ContainedEntities.Count > 0 ? container.ContainedEntities[0] : null;
    }

    /// <summary>
    /// The first slot that would accept this item, or null if none would.
    /// </summary>
    public string? GetValidSlot(Entity<AttachableHolderComponent> ent, EntityUid attachable)
    {
        if (!HasComp<AttachableComponent>(attachable))
            return null;

        foreach (var (slotId, slot) in ent.Comp.Slots)
        {
            if (slot.Whitelist == null)
                continue;

            if (_entityWhitelist.IsWhitelistPass(slot.Whitelist, attachable))
                return slotId;
        }

        return null;
    }

    #endregion

    #region Setup

    private void OnHolderInit(Entity<AttachableHolderComponent> ent, ref ComponentInit args)
    {
        foreach (var slotId in ent.Comp.Slots.Keys)
        {
            var container = _container.EnsureContainer<ContainerSlot>(ent, slotId);
            // Otherwise a rail flashlight would be swallowed by the container it lives in.
            container.OccludesLight = false;
        }
    }

    private void OnHolderMapInit(Entity<AttachableHolderComponent> ent, ref MapInitEvent args)
    {
        foreach (var (slotId, slot) in ent.Comp.Slots)
        {
            if (slot.StartingAttachable is not { } proto)
                continue;

            var attachable = Spawn(proto, Transform(ent).Coordinates);
            if (!Attach(ent, attachable, slotId))
                QueueDel(attachable);
        }
    }

    #endregion

    #region Attach / detach

    private void OnInteractUsing(Entity<AttachableHolderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out AttachableComponent? attachable))
            return;

        if (GetValidSlot(ent, args.Used) is not { } slotId)
            return;

        args.Handled = true;
        StartAttach(ent, (args.Used, attachable), slotId, args.User);
    }

    private void StartAttach(
        Entity<AttachableHolderComponent> ent,
        Entity<AttachableComponent> attachable,
        string slotId,
        EntityUid user)
    {
        var doAfter = new DoAfterArgs(
            EntityManager,
            user,
            attachable.Comp.AttachDoAfter,
            new AttachableAttachDoAfterEvent(slotId),
            ent,
            target: ent,
            used: attachable)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnAttachDoAfter(Entity<AttachableHolderComponent> ent, ref AttachableAttachDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } used)
            return;

        args.Handled = true;

        // The slot may have been filled while the doafter ran.
        if (GetAttachable(ent, args.SlotId) != null)
        {
            _popup.PopupEntity(Loc.GetString("is-attachable-slot-occupied"), ent, args.User);
            return;
        }

        Attach(ent, used, args.SlotId, args.User);
    }

    /// <summary>
    /// Puts an attachment into a slot, wiring up anything it grants and refreshing the host's stats.
    /// </summary>
    public bool Attach(
        Entity<AttachableHolderComponent> ent,
        EntityUid attachable,
        string slotId,
        EntityUid? user = null)
    {
        if (!_container.TryGetContainer(ent, slotId, out var container))
            return false;

        if (!_container.Insert(attachable, container))
            return false;

        if (TryComp(attachable, out AttachableGrantsComponent? grants))
            ApplyGrants(ent, (attachable, grants));

        if (TryComp(attachable, out AttachableComponent? comp))
            _audio.PlayPredicted(comp.AttachSound, ent, user);

        RefreshHolder(ent.Owner);
        return true;
    }

    private void OnGetVerbs(Entity<AttachableHolderComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        var user = args.User;
        foreach (var (slotId, slot) in ent.Comp.Slots)
        {
            if (slot.Locked || GetAttachable(ent, slotId) is not { } attachable)
                continue;

            var id = slotId;
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("is-attachable-verb-detach", ("attachable", attachable)),
                Act = () => StartDetach(ent, id, user),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
            });
        }
    }

    private void StartDetach(Entity<AttachableHolderComponent> ent, string slotId, EntityUid user)
    {
        if (GetAttachable(ent, slotId) is not { } attachable)
            return;

        var delay = TryComp(attachable, out AttachableComponent? comp) ? comp.AttachDoAfter : 1.5f;

        var doAfter = new DoAfterArgs(
            EntityManager,
            user,
            delay,
            new AttachableDetachDoAfterEvent(slotId),
            ent,
            target: ent,
            used: attachable)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDetachDoAfter(Entity<AttachableHolderComponent> ent, ref AttachableDetachDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        Detach(ent, args.SlotId, args.User);
    }

    /// <summary>
    /// Takes whatever is in a slot back out, undoing anything it granted.
    /// </summary>
    public bool Detach(Entity<AttachableHolderComponent> ent, string slotId, EntityUid? user = null)
    {
        if (GetAttachable(ent, slotId) is not { } attachable)
            return false;

        if (!_container.TryGetContainer(ent, slotId, out var container))
            return false;

        if (TryComp(attachable, out AttachableGrantsComponent? grants))
            RemoveGrants(ent, (attachable, grants));

        _container.Remove(attachable, container, force: true);

        if (TryComp(attachable, out AttachableComponent? comp))
            _audio.PlayPredicted(comp.DetachSound, ent, user);

        if (user != null)
            _hands.TryPickupAnyHand(user.Value, attachable);

        RefreshHolder(ent.Owner);
        return true;
    }

    #endregion

    #region Grants

    // Adds the attachment's granted components to the host, remembering which ones we actually
    // added so detaching does not strip something the host already had.
    private void ApplyGrants(EntityUid holder, Entity<AttachableGrantsComponent> attachable)
    {
        attachable.Comp.Granted.Clear();

        var toAdd = new ComponentRegistry();
        foreach (var (name, entry) in attachable.Comp.Components)
        {
            if (HasComp(holder, _componentFactory.GetRegistration(name).Type))
                continue;

            toAdd[name] = entry;
            attachable.Comp.Granted.Add(name);
        }

        if (toAdd.Count > 0)
            EntityManager.AddComponents(holder, toAdd, removeExisting: false);
    }

    private void RemoveGrants(EntityUid holder, Entity<AttachableGrantsComponent> attachable)
    {
        foreach (var name in attachable.Comp.Granted)
        {
            RemComp(holder, _componentFactory.GetRegistration(name).Type);
        }

        attachable.Comp.Granted.Clear();
    }

    #endregion

    private void OnExamine(Entity<AttachableHolderComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(AttachableHolderComponent)))
        {
            foreach (var (slotId, slot) in ent.Comp.Slots)
            {
                var slotName = Loc.GetString(slot.Name);

                if (GetAttachable(ent, slotId) is not { } attachable)
                {
                    args.PushMarkup(Loc.GetString("is-attachable-examine-empty", ("slot", slotName)));
                    continue;
                }

                args.PushMarkup(Loc.GetString(
                    "is-attachable-examine-filled",
                    ("slot", slotName),
                    ("attachable", attachable)));

                if (TryComp(attachable, out AttachableComponent? comp) && comp.ExamineText is { } text)
                    args.PushMarkup(Loc.GetString(text));
            }
        }
    }
}
