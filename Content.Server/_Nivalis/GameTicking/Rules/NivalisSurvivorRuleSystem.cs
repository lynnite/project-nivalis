using Content.Server.GameTicking;
using Content.Server.Administration.Managers;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Content.Server.GameTicking.Rules;
using Content.Server.Ghost;
using Content.Server.Hands.Systems;
using Content.Shared.Follower;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server._Nivalis.Hands;
using Content.Server._Nivalis.Traits;
using Content.Server._Nivalis.Perks;
using Content.Server.RoundEnd;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Server._Nivalis.GameTicking.Rules.Components;
using Content.Server._Nivalis.Survivor.Components;
using Content.Shared._Nivalis.GameTicking;
using Content.Shared._Nivalis.GameTicking.Components;
using Content.Shared._Nivalis.Combat;
using Content.Shared._Nivalis.Environment;
using Content.Shared._Nivalis.Morale;
using Content.Shared._Nivalis.Stamina;
using Content.Shared._Nivalis.Scrap;
using Content.Shared._Nivalis.Status;
using Content.Shared._Nivalis.Survivor.Components;
using Content.Shared._Nivalis.Traits;
using Content.Shared._Nivalis.Perks;
using Content.Shared._Nivalis.Weapons;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.UserInterface;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
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
    [Dependency] private ISharedPlayerManager _playerManager = default!;
    [Dependency] private FollowerSystem _follower = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private NivalisTraitSystem _traits = default!;

    /// <summary>
    ///     Traits the player selected in the lobby, applied when their survivor spawns.
    /// </summary>
    private readonly Dictionary<NetUserId, List<ProtoId<NivalisTraitPrototype>>> _pendingTraits = new();

    [Dependency] private NivalisPerkSystem _perks = default!;

    private readonly Dictionary<NetUserId, ProtoId<NivalisPerkPrototype>?> _pendingPerk = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayerSpawning);
        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<StationPostInitEvent>(OnStationPostInit);
        SubscribeLocalEvent<NivalisSurvivalPhaseChangedEvent>(OnPhaseChanged);
        SubscribeNetworkEvent<NivalisReturnToLobbyMessage>(OnReturnToLobby);
        SubscribeNetworkEvent<NivalisJoinGameMessage>(OnJoinGame);
        SubscribeNetworkEvent<NivalisSpectateCycleMessage>(OnSpectateCycle);
        SubscribeNetworkEvent<NivalisTraitsSelectedMessage>(OnTraitsSelected);
        SubscribeNetworkEvent<NivalisPerkSelectedMessage>(OnPerkSelected);
    }

    private void OnPerkSelected(NivalisPerkSelectedMessage msg, EntitySessionEventArgs args)
    {
        _pendingPerk[args.SenderSession.UserId] = msg.Perk;
    }

    private void OnTraitsSelected(NivalisTraitsSelectedMessage msg, EntitySessionEventArgs args)
    {
        _pendingTraits[args.SenderSession.UserId] = new List<ProtoId<NivalisTraitPrototype>>(msg.Traits);
    }

    private void OnJoinGame(NivalisJoinGameMessage msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (GameTicker.UserHasJoinedGame(session))
            return;

        MakeSpectator(session);
        GameTicker.PlayerJoinGame(session);
    }

    private void OnReturnToLobby(NivalisReturnToLobbyMessage msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (session.AttachedEntity is not { Valid: true } attached ||
            !HasComp<NivalisSpectatorComponent>(attached))
            return;

        GameTicker.Respawn(session);
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
        while (query.MoveNext(out _, out _, out _, out _))
        {
            MakeSpectator(ev.Player);
            ev.Handled = true;
            return;
        }
    }

    private void MakeSpectator(ICommonSession session)
    {
        var mind = _mind.GetOrCreateMind(session.UserId);
        _mind.SetUserId(mind, session.UserId);

        var isAdmin = _admin.HasAdminFlag(session, AdminFlags.Admin);

        var ghost = _ghost.SpawnGhost((mind.Owner, mind.Comp), canReturn: true);
        if (ghost is not { Valid: true } ghostUid)
            return;

        var ghostComp = Comp<GhostComponent>(ghostUid);
        _ghost.SetCanReturnToBody((ghostUid, ghostComp), false);
        _ghost.SetCanGhostInteract((ghostUid, ghostComp), isAdmin);

        if (!isAdmin)
        {
            _actions.RemoveAction(ghostUid, ghostComp.BooActionEntity);
            _actions.RemoveAction(ghostUid, ghostComp.ToggleGhostHearingActionEntity);
            _actions.RemoveAction(ghostUid, ghostComp.ToggleLightingActionEntity);
            _actions.RemoveAction(ghostUid, ghostComp.ToggleFoVActionEntity);
            _actions.RemoveAction(ghostUid, ghostComp.ToggleGhostsActionEntity);
        }

        var spect = EnsureComp<NivalisSpectatorComponent>(ghostUid);
        spect.Player = session.UserId;
        spect.IsAdmin = isAdmin;
        Dirty(ghostUid, spect);

        FollowSurvivor(ghostUid, spect);
    }

    private void OnSpectateCycle(NivalisSpectateCycleMessage msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (session.AttachedEntity is not { Valid: true } attached ||
            !TryComp<NivalisSpectatorComponent>(attached, out var spect))
            return;

        CycleSpectator(attached, spect, msg.Next);
    }

    private void CycleSpectator(EntityUid ghostUid, NivalisSpectatorComponent spect, bool next)
    {
        List<EntityUid> alive = new();
        var query = EntityQueryEnumerator<NivalisSurvivorComponent, TransformComponent, MobStateComponent>();
        while (query.MoveNext(out var survivor, out _, out _, out var mobState))
        {
            if (_mobState.IsAlive(survivor, mobState) && !Deleted(survivor))
                alive.Add(survivor);
        }

        if (alive.Count == 0)
            return;

        // Order by net entity id for a stable ordering that matches client display.
        alive.Sort((a, b) => GetNetEntity(a).CompareTo(GetNetEntity(b)));

        var index = alive.IndexOf(spect.FollowTarget);
        if (index < 0)
            index = -1;

        index += next ? 1 : -1;
        index = (index + alive.Count) % alive.Count;

        spect.FollowTarget = alive[index];
        Dirty(ghostUid, spect);
        _follower.StartFollowingEntity(ghostUid, spect.FollowTarget);
    }

    private void FollowSurvivor(EntityUid ghostUid, NivalisSpectatorComponent spect)
    {
        if (TryFindSpectateTarget(out var target) && target != null)
        {
            spect.FollowTarget = target.Value;
            _follower.StartFollowingEntity(ghostUid, target.Value);
        }
        else
        {
            spect.FollowTarget = EntityUid.Invalid;
            if (TryGetSpawnCoordinates(out var coords))
                _transformSystem.SetCoordinates(ghostUid, coords);
        }

        Dirty(ghostUid, spect);
    }

    private bool TryFindSpectateTarget(out EntityUid? target)
    {
        target = null;
        var query = EntityQueryEnumerator<NivalisSurvivorComponent, TransformComponent, MobStateComponent>();
        while (query.MoveNext(out var survivor, out _, out _, out var mobState))
        {
            if (_mobState.IsAlive(survivor, mobState))
            {
                target = survivor;
                return true;
            }
        }

        return false;
    }

    private void OnPhaseChanged(ref NivalisSurvivalPhaseChangedEvent args)
    {
        if (args.OldPhase == args.NewPhase)
            return;

        RespawnSpectators();
    }

    private void RespawnSpectators()
    {
        var query = EntityQueryEnumerator<NivalisSpectatorComponent>();
        while (query.MoveNext(out var uid, out var spect))
        {
            if (!_playerManager.TryGetSessionById(spect.Player, out var session))
            {
                QueueDel(uid);
                continue;
            }

            _mind.WipeMind(session);
            QueueDel(uid);

            var active = QueryActiveRules();
            while (active.MoveNext(out var ruleUid, out _, out var comp, out _))
            {
                var profile = GameTicker.GetPlayerProfile(session);
                var mob = SpawnAsSurvivor((ruleUid, comp), session, profile);
                if (mob != EntityUid.Invalid)
                {
                    GameTicker.PlayerJoinGame(session);
                    GameTicker.PlayersJoinedRoundNormally++;
                    RaiseLocalEvent(mob, new PlayerSpawnCompleteEvent(mob, session, null, true, false,
                        GameTicker.PlayersJoinedRoundNormally, EntityUid.Invalid, profile), true);
                }

                break;
            }
        }
    }

    private void ApplySelectedTraits(EntityUid mob, NetUserId userId)
    {
        if (!_pendingTraits.TryGetValue(userId, out var selected))
            return;

        var ent = (Entity<NivalisTraitComponent?>)mob;
        foreach (var traitId in selected)
        {
            _traits.AddTrait(ent, traitId);
        }
    }

    private void ApplySelectedPerk(EntityUid mob, NetUserId userId)
    {
        if (!_pendingPerk.TryGetValue(userId, out var perk) || perk == null)
            return;

        _perks.SetPerk((Entity<NivalisPerkComponent?>)mob, perk.Value);

        if (perk.Value.Id == NivalisExecutionerSystem.ExecutionerPerk)
            EnsureComp<NivalisExecutionerComponent>(mob);
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

        EnsureComp<NivalisTraitComponent>(mob);

        ApplySelectedTraits(mob, session.UserId);

        ApplySelectedPerk(mob, session.UserId);

        EnsureComp<NivalisSurvivalResourceComponent>(mob);
        EnsureComp<NivalisStaminaComponent>(mob);
        EnsureComp<NivalisMoraleComponent>(mob);

        EnsureComp<NivalisScrapComponent>(mob);

        var mover = EnsureComp<InputMoverComponent>(mob);
        mover.SprintInverted = true;
        Dirty(mob, mover);

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
