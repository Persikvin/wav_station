using Content.Server._White.GameTicking.Rules.Components;
using Content.Server.Announcements.Systems;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server._White.Roles;
using Content.Server._White.Vulpification;
using Content.Shared._White.Vulpification;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Globalization;

namespace Content.Server._White.GameTicking.Rules;

/// <summary>
///     Game rule for the vulpification antagonist: the crew slowly turn into vulpkanins.
///     Based on the zombie rule, but instead of the dead the crew gets... foxy.
/// </summary>
public sealed class VulpificationRuleSystem : GameRuleSystem<VulpificationRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VulpifyInitialInfectedRoleComponent, GetBriefingEvent>(OnInitialGetBriefing);
        SubscribeLocalEvent<VulpifyRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void OnInitialGetBriefing(Entity<VulpifyInitialInfectedRoleComponent> role, ref GetBriefingEvent args)
    {
        if (!_roles.MindHasRole<VulpifyRoleComponent>(args.Mind.Owner))
            args.Append(Loc.GetString("vulpification-patientzero-role-greeting"));
    }

    private void OnGetBriefing(Entity<VulpifyRoleComponent> role, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("vulpification-role-greeting"));
    }

    protected override void Started(EntityUid uid, VulpificationRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.NextRoundEndCheck = _timing.CurTime + component.EndCheckDelay;
    }

    protected override void ActiveTick(EntityUid uid, VulpificationRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
        if (!component.NextRoundEndCheck.HasValue || component.NextRoundEndCheck > _timing.CurTime)
            return;
        CheckRoundEnd(component);
        component.NextRoundEndCheck = _timing.CurTime + component.EndCheckDelay;
    }

    /// <summary>
    ///     Checks whether the round should end: the whole crew are foxes now.
    /// </summary>
    private void CheckRoundEnd(VulpificationRuleComponent component)
    {
        var healthy = GetHealthyHumans();
        if (healthy.Count == 1) // Only one human left. spooky.
            _popup.PopupEntity(Loc.GetString("vulpification-alone"), healthy[0], healthy[0]);

        if (GetInfectedFraction(false) > component.VulpifiedShuttleCallPercentage && !_roundEnd.IsRoundEndRequested())
        {
            foreach (var station in _station.GetStations())
            {
                _announcer.SendAnnouncement(_announcer.GetAnnouncementId("ShuttleCalled"),
                    "vulpification-shuttle-call", filter: _station.GetInOwningStation(station),
                    colorOverride: Color.Orange);
            }
            _roundEnd.RequestRoundEnd(null, false);
        }

        // Include dead in this count so we don't end the round when everyone gets on the shuttle.
        if (GetInfectedFraction() >= 1) // Oops, all foxes.
            _roundEnd.EndRound();
    }

    protected override void AppendRoundEndText(EntityUid uid,
        VulpificationRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var fraction = GetInfectedFraction(true, true);

        if (fraction <= 0)
            args.AddLine(Loc.GetString("vulpification-round-end-amount-none"));
        else if (fraction <= 0.25)
            args.AddLine(Loc.GetString("vulpification-round-end-amount-low"));
        else if (fraction <= 0.5)
            args.AddLine(Loc.GetString("vulpification-round-end-amount-medium", ("percent", Math.Round((fraction * 100), 2).ToString(CultureInfo.InvariantCulture))));
        else if (fraction < 1)
            args.AddLine(Loc.GetString("vulpification-round-end-amount-high", ("percent", Math.Round((fraction * 100), 2).ToString(CultureInfo.InvariantCulture))));
        else
            args.AddLine(Loc.GetString("vulpification-round-end-amount-all"));

        var antags = _antag.GetAntagIdentifiers(uid);
        args.AddLine(Loc.GetString("vulpification-round-end-initial-count", ("initialCount", antags.Count)));
        foreach (var (_, data, entName) in antags)
        {
            args.AddLine(Loc.GetString("vulpification-round-end-user-was-initial",
                ("name", entName),
                ("username", data.UserName)));
        }

        var healthy = GetHealthyHumans();
        if (healthy.Count <= 0 || healthy.Count > 2 * antags.Count)
            return;
        args.AddLine("");
        args.AddLine(Loc.GetString("vulpification-round-end-survivor-count", ("count", healthy.Count)));
        foreach (var survivor in healthy)
        {
            var meta = MetaData(survivor);
            var username = string.Empty;
            if (_mind.TryGetMind(survivor, out _, out var mind) && mind.Session != null)
            {
                username = mind.Session.Name;
            }

            args.AddLine(Loc.GetString("vulpification-round-end-user-was-survivor",
                ("name", meta.EntityName),
                ("username", username)));
        }
    }

    /// <summary>
    ///     Get the fraction of players that are vulpified, between 0 and 1.
    /// </summary>
    private float GetInfectedFraction(bool includeOffStation = false, bool includeDead = true)
    {
        var players = GetHealthyHumans(includeOffStation);
        var vulpifiedCount = 0;
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, VulpifiedComponent, MobStateComponent>();
        while (query.MoveNext(out _, out _, out _, out var mob))
        {
            if (!includeDead && mob.CurrentState == MobState.Dead)
                continue;
            vulpifiedCount++;
        }

        return vulpifiedCount / (float) (players.Count + vulpifiedCount);
    }

    /// <summary>
    ///     Gets the list of humans who are alive, not vulpified, and are on a station.
    /// </summary>
    private List<EntityUid> GetHealthyHumans(bool includeOffStation = false)
    {
        var healthy = new List<EntityUid>();

        var stationGrids = new HashSet<EntityUid>();
        if (!includeOffStation)
        {
            foreach (var station in _gameTicker.GetSpawnableStations())
            {
                if (TryComp<StationDataComponent>(station, out var data) && _station.GetLargestGrid(data) is { } grid)
                    stationGrids.Add(grid);
            }
        }

        var players = AllEntityQuery<HumanoidAppearanceComponent, ActorComponent, MobStateComponent, TransformComponent>();
        var vulpified = GetEntityQuery<VulpifiedComponent>();
        while (players.MoveNext(out var uid, out _, out _, out var mob, out var xform))
        {
            if (!_mobState.IsAlive(uid, mob)
                || HasComp<PendingVulpificationComponent>(uid)
                || HasComp<VulpifyOnDeathComponent>(uid)
                || vulpified.HasComponent(uid)
                || !includeOffStation && !stationGrids.Contains(xform.GridUid ?? EntityUid.Invalid))
                continue;

            healthy.Add(uid);
        }
        return healthy;
    }
}