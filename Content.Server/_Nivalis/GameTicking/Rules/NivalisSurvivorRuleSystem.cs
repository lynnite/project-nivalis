using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server._Nivalis.Hands;
using Content.Server.RoundEnd;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Server._Nivalis.GameTicking.Rules.Components;
using Content.Server._Nivalis.Survivor.Components;
using Content.Shared._Nivalis.GameTicking.Components;
using Content.Shared._Nivalis.Combat;
using Content.Shared._Nivalis.Environment;
using Content.Shared._Nivalis.Morale;
using Content.Shared._Nivalis.Perks;
using Content.Shared._Nivalis.Stamina;
using Content.Shared._Nivalis.Status;
using Content.Shared._Nivalis.Survivor.Components;
using Content.Shared._Nivalis.Traits;
using Content.Shared._Nivalis.Weapons;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.UserInterface;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Nivalis.GameTicking.Rules;

public sealed partial class NivalisSurvivorRuleSystem : GameRuleSystem<NivalisSurvivorRuleComponent>
{
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private RoleSystem _roles = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private StationJobsSystem _stationJobs = default!;
    [Dependency] private StationSpawningSystem _stationSpawning = default!;
    [Dependency] private NivalisHandsSystem _nivalisHands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayerSpawning);
        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<StationPostInitEvent>(OnStationPostInit);
    }

    protected override void Started(EntityUid uid, NivalisSurvivorRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        component.NextRoundEndCheck = Timing.CurTime + TimeSpan.FromSeconds(component.EndCheckDelay);

        ConfigureStationJobs(component.Job);
    }

    private void OnStationPostInit(ref StationPostInitEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var comp, out _))
        {
            TryConfigureStation(ev.Station.Owner, comp.Job);
        }
    }

    private void ConfigureStationJobs(ProtoId<JobPrototype> job)
    {
        var query = EntityQueryEnumerator<StationJobsComponent>();
        while (query.MoveNext(out var station, out _))
        {
            TryConfigureStation(station, job);
        }
    }

    private void TryConfigureStation(EntityUid station, ProtoId<JobPrototype> job)
    {
        if (!HasComp<StationJobsComponent>(station))
            return;

        foreach (var jobId in _stationJobs.GetJobs(station).Keys)
        {
            _stationJobs.TrySetJobSlot(station, jobId, 0);
        }

        _stationJobs.MakeJobUnlimited(station, job);
    }

    private void OnPlayerSpawning(RulePlayerSpawningEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out _))
        {
            foreach (var session in ev.PlayerPool)
            {
                if (!ev.Profiles.TryGetValue(session.UserId, out var profile))
                    continue;

                var mob = SpawnAsSurvivor((uid, comp), session, profile);
                if (mob == EntityUid.Invalid)
                    continue;

                GameTicker.PlayerJoinGame(session);
                GameTicker.PlayersJoinedRoundNormally++;
                RaiseLocalEvent(mob, new PlayerSpawnCompleteEvent(mob, session, null, false, false,
                    GameTicker.PlayersJoinedRoundNormally, EntityUid.Invalid, profile), true);
            }

            ev.PlayerPool.Clear();
            return;
        }
    }

    private void OnBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out _))
        {
            var mob = SpawnAsSurvivor((uid, comp), ev.Player, ev.Profile);
            if (mob != EntityUid.Invalid)
            {
                GameTicker.PlayersJoinedRoundNormally++;
                RaiseLocalEvent(mob, new PlayerSpawnCompleteEvent(mob, ev.Player, null, ev.LateJoin, false,
                    GameTicker.PlayersJoinedRoundNormally, ev.Station, ev.Profile), true);
            }

            ev.Handled = true;
            return;
        }
    }

    private EntityUid SpawnAsSurvivor(Entity<NivalisSurvivorRuleComponent> rule, ICommonSession session, HumanoidCharacterProfile profile)
    {
        if (!TryGetSpawnCoordinates(out var spawnCoords))
            return EntityUid.Invalid;

        var mob = _stationSpawning.SpawnPlayerMob(spawnCoords, null, profile, null);
        _stationSpawning.EquipStartingGear(mob, rule.Comp.Gear, false);

        var mind = _mind.CreateMind(session.UserId, profile.Name);
        _mind.SetUserId(mind, session.UserId);
        _mind.TransferTo(mind, mob);

        EnsureComp<NivalisSurvivorComponent>(mob);

        EnsureComp<NivalisEnvironmentImmunityComponent>(mob);

        EnsureComp<NivalisPerkComponent>(mob);

        EnsureComp<NivalisSurvivalResourceComponent>(mob);
        EnsureComp<NivalisStaminaComponent>(mob);
        EnsureComp<NivalisMoraleComponent>(mob);

        _nivalisHands.EnsureHandCount(mob, 4);

        var ff = EnsureComp<NivalisFriendlyFireComponent>(mob);
        ff.Team = NivalisCombatTeam.Survivor;
        Dirty(mob, ff);

        EnsureComp<NivalisAmmoHudComponent>(mob);

        var ui = EnsureComp<UserInterfaceComponent>(mob);
        _ui.SetUi((mob, ui), NivalisTraitDraftUiKey.Key, new InterfaceData("NivalisTraitDraftBoundUserInterface"));

        _roles.MindAddRole(mind, rule.Comp.MindRole);
        _roles.MindAddJobRole(mind, jobPrototype: rule.Comp.Job);
        return mob;
    }

    private bool TryGetSpawnCoordinates(out EntityCoordinates coordinates)
    {
        var query = EntityQueryEnumerator<NivalisSurvivorSpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out var marker, out _, out var xform))
        {
            if (xform.Anchored && !Deleted(xform.ParentUid))
            {
                coordinates = xform.Coordinates;
                return true;
            }
        }

        var fallback = EntityQueryEnumerator<NivalisSurvivorSpawnPointComponent, TransformComponent>();
        if (fallback.MoveNext(out _, out _, out var fxform))
        {
            coordinates = fxform.Coordinates;
            return true;
        }

        Log.Warning($"No {nameof(NivalisSurvivorSpawnPointComponent)} marker found for Nivalis Survivor gamemode.");
        coordinates = EntityCoordinates.Invalid;
        return false;
    }

    protected override void ActiveTick(EntityUid uid, NivalisSurvivorRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.NextRoundEndCheck > Timing.CurTime)
            return;

        component.NextRoundEndCheck = Timing.CurTime + TimeSpan.FromSeconds(component.EndCheckDelay);

        if (component.RoundEndTriggered)
            return;

        var survivorsRemaining = false;
        var query = EntityQueryEnumerator<NivalisSurvivorComponent, MobStateComponent>();
        while (query.MoveNext(out var survivor, out _, out var mobState))
        {
            if (_mobState.IsAlive(survivor, mobState))
            {
                survivorsRemaining = true;
                break;
            }
        }

        if (survivorsRemaining)
            return;

        component.RoundEndTriggered = true;
        _roundEnd.EndRound(component.RoundEndDelay);
    }

    protected override void AppendRoundEndText(EntityUid uid,
        NivalisSurvivorRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);
        args.AddLine(Loc.GetString("nivalis-survivor-round-end"));

        var waves = 0;
        var cycleQuery = EntityQueryEnumerator<NivalisSurvivalCycleComponent>();
        while (cycleQuery.MoveNext(out _, out var cycle))
        {
            waves = cycle.WavesCleared;
            break;
        }
        args.AddLine(Loc.GetString("nivalis-round-end-waves", ("waves", waves)));
    }
}
