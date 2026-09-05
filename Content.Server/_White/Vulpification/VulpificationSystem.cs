using Content.Server.Actions;
using Content.Shared._White.Blocking;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared._White.Vulpification;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._White.Vulpification;

/// <summary>
///     Handles the vulpification infection: spreading it via melee, damaging
///     the pending infected, and giving the initial infected the turn action.
/// </summary>
public sealed partial class VulpificationSystem : SharedVulpificationSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VulpifiedComponent, MeleeHitEvent>(OnMeleeHit,
            after: new[] { typeof(MeleeBlockSystem) });
        SubscribeLocalEvent<PendingVulpificationComponent, MapInitEvent>(OnPendingMapInit);
        SubscribeLocalEvent<IncurableVulpificationComponent, MapInitEvent>(OnIncurableMapInit);
        SubscribeLocalEvent<IncurableVulpificationComponent, VulpifySelfActionEvent>(OnVulpifySelf);
        SubscribeLocalEvent<VulpifyOnDeathComponent, MobStateChangedEvent>(OnVulpifyOnDeath);
    }

    private void OnIncurableMapInit(EntityUid uid, IncurableVulpificationComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.Action, component.VulpifySelfActionPrototype);
    }

    private void OnPendingMapInit(EntityUid uid, PendingVulpificationComponent component, MapInitEvent args)
    {
        if (_mobState.IsDead(uid))
        {
            VulpifyEntity(uid);
            return;
        }

        component.NextTick = _timing.CurTime + TimeSpan.FromSeconds(1f);
        component.GracePeriod = _random.Next(component.MinInitialInfectedGrace, component.MaxInitialInfectedGrace);
    }

    private void OnVulpifySelf(EntityUid uid, IncurableVulpificationComponent component, VulpifySelfActionEvent args)
    {
        VulpifyEntity(uid);
        if (component.Action != null)
            Del(component.Action.Value);
    }

    private void OnVulpifyOnDeath(EntityUid uid, VulpifyOnDeathComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            VulpifyEntity(uid, args.Component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        // Damage the living infected so they eventually drop and turn.
        var query = EntityQueryEnumerator<PendingVulpificationComponent, DamageableComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var comp, out var damage, out var mobState))
        {
            if (comp.NextTick > curTime)
                continue;

            comp.NextTick = curTime + TimeSpan.FromSeconds(1f);

            comp.GracePeriod -= TimeSpan.FromSeconds(1f);
            if (comp.GracePeriod > TimeSpan.Zero)
                continue;

            if (_random.Prob(comp.InfectionWarningChance))
                _popup.PopupEntity(Loc.GetString(_random.Pick(comp.InfectionWarnings)), uid, uid);

            var multiplier = _mobState.IsCritical(uid, mobState)
                ? comp.CritDamageMultiplier
                : 1f;

            _damageable.TryChangeDamage(uid, comp.Damage * multiplier, true, false, damage);
        }
    }

    private void OnMeleeHit(EntityUid uid, VulpifiedComponent component, MeleeHitEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<VulpifiedComponent>(args.User, out _))
            return;

        foreach (var entity in args.HitEntities)
        {
            if (args.User == entity)
                continue;

            if (!TryComp<MobStateComponent>(entity, out var mobState))
                continue;

            if (HasComp<VulpifiedComponent>(entity))
                continue;

            // Spread the pathogen, little fox.
            if (!HasComp<VulpificationImmuneComponent>(entity)
                && !HasComp<VulpificationImmuneComponent>(args.User)
                && !HasComp<NonSpreaderVulpificationComponent>(args.User)
                && _random.Prob(component.InfectionChance))
            {
                EnsureComp<PendingVulpificationComponent>(entity);
                EnsureComp<VulpifyOnDeathComponent>(entity);
            }

            // Incapacitated targets are converted on the spot.
            if (_mobState.IsIncapacitated(entity, mobState)
                && !HasComp<VulpificationImmuneComponent>(entity))
            {
                VulpifyEntity(entity);
            }
        }
    }
}