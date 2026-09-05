using Content.Shared.Movement.Systems;
using Content.Shared.NameModifier.EntitySystems;

namespace Content.Shared._White.Vulpification;

/// <summary>
///     Shared logic for transformed vulpkanins: movement speed and name modifiers.
/// </summary>
public abstract class SharedVulpificationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VulpifiedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<VulpifiedComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
    }

    private void OnRefreshSpeed(EntityUid uid, VulpifiedComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        var mod = component.MovementSpeedMultiplier;
        args.ModifySpeed(mod, mod);
    }

    private void OnRefreshNameModifiers(Entity<VulpifiedComponent> entity, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("vulpification-name-prefix");
    }
}