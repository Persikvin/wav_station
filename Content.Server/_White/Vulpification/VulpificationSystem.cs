using Content.Server.Actions;
using Content.Server.Stunnable;
using Content.Shared._White.Blocking;
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
///     Handles the vulpification infection: spreading it via melee bites,
///     timing the infected's transformation, and giving the initial infected
///     the turn action.
/// </summary>
public sealed partial class VulpificationSystem : SharedVulpificationSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly StunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VulpifiedComponent, MeleeHitEvent>(OnMeleeHit,
            after: new[] { typeof(MeleeBlockSystem) });
        SubscribeLocalEvent<PendingVulpificationComponent, ComponentStartup>(OnPendingStartup);
        SubscribeLocalEvent<IncurableVulpificationComponent, ComponentStartup>(OnIncurableStartup);
        SubscribeLocalEvent<IncurableVulpificationComponent, VulpifySelfActionEvent>(OnVulpifySelf);
        SubscribeLocalEvent<VulpifyOnDeathComponent, MobStateChangedEvent>(OnVulpifyOnDeath);
    }

    private void OnIncurableStartup(EntityUid uid, IncurableVulpificationComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.Action, component.VulpifySelfActionPrototype);
    }

    private void OnPendingStartup(EntityUid uid, PendingVulpificationComponent component, ComponentStartup args)
    {
        // The initial infected transform right away instead of waiting out the timer.
        if (_mobState.IsDead(uid) || HasComp<VulpifyInitialInfectedComponent>(uid))
        {
            VulpifyEntity(uid);
            return;
        }

        component.TransformAt = _timing.CurTime + component.TransformDelay;
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

        var query = EntityQueryEnumerator<PendingVulpificationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ConversionStarted)
            {
                // Still shaking - not a vulp yet.
                if (curTime < comp.ConvertAt)
                    continue;

                // Welcome to the pack, little fox.
                VulpifyEntity(uid);
                continue;
            }

            if (curTime < comp.TransformAt)
            {
                // Still waiting out the infection. Show the odd warning.
                if (curTime < comp.NextTick)
                    continue;

                comp.NextTick = curTime + TimeSpan.FromSeconds(1f);
                if (_random.Prob(comp.InfectionWarningChance))
                    _popup.PopupEntity(Loc.GetString(_random.Pick(comp.InfectionWarnings)), uid, uid);
                continue;
            }

            // Transformation finale: shake & lock in place, smoke, then change.
            comp.ConversionStarted = true;
            comp.ConvertAt = curTime + comp.ConversionTime;

            _stun.TryStun(uid, comp.ConversionTime, true);
            Spawn("WizardSmoke", Transform(uid).Coordinates);
            _popup.PopupEntity(Loc.GetString("vulpification-transform-start"), uid, uid);
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