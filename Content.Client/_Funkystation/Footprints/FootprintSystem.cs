using Content.Shared._Funkystation.Footprints;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Funkystation.Footprints;

public sealed partial class FootprintSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly ResPath RsiPath = new("/Textures/_Funkystation/Effects/footprints.rsi");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FootprintComponent, ComponentStartup>(OnStartup);
        SubscribeNetworkEvent<FootprintStateEvent>(OnStateUpdated);
    }

    private void OnStartup(EntityUid uid, FootprintComponent component, ref ComponentStartup args)
    {
        UpdateVisuals(uid, component);
    }

    private void OnStateUpdated(FootprintStateEvent args)
    {
        if (TryGetEntity(args.NetEntity, out var uid) && TryComp<FootprintComponent>(uid, out var comp))
        {
            UpdateVisuals(uid.Value, comp);
        }
    }

    private void UpdateVisuals(EntityUid uid, FootprintComponent component)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var ent = new Entity<SpriteComponent?>(uid, sprite);

        for (var i = 0; i < component.Prints.Count; i++)
        {
            var print = component.Prints[i];

            if (!_sprite.LayerExists(ent, i))
                _sprite.AddBlankLayer((uid, sprite), i);

            _sprite.LayerSetOffset(ent, i, print.Offset);
            _sprite.LayerSetRotation(ent, i, print.Rotation);
            _sprite.LayerSetColor(ent, i, print.Color);
            _sprite.LayerSetSprite(ent, i, new SpriteSpecifier.Rsi(RsiPath, print.State));
        }
    }
}
