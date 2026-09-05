using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared._DV.Abilities.Felinid;

/// <summary>
/// Makes eating <see cref="FelinidFoodComponent"/> enable a felinids hairball action.
/// Other interactions are in the server system.
/// </summary>
public abstract partial class SharedFelinidSystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private ItemCougherSystem _cougher = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FelinidFoodComponent, FullyEatenEvent>(OnMouseEaten);
    }

    private void OnMouseEaten(Entity<FelinidFoodComponent> ent, ref FullyEatenEvent args)
    {
        var user = args.User;
        if (!HasComp<FelinidComponent>(user))
            return;

        if (TryComp<SatiationComponent>(user, out var satiation))
            _satiation.ModifyValue((user, satiation), SatiationSystem.Hunger, ent.Comp.BonusHunger);

        _cougher.EnableAction(user);
    }
}
