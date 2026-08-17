using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server._Nivalis.Traits;
using Content.Shared._Nivalis.GameTicking.Components;
using Content.Shared._Nivalis.Perks;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.GameTicking.Rules;

/// <summary>
///     Scavenge/storm cycle
///     The phase advances between <see cref="NivalisSurvivalPhase.Scavenge"/> and
///     <see cref="NivalisSurvivalPhase.Storm"/>, announcing the transition and raising a
///     <see cref="NivalisSurvivalPhaseChangedEvent"/> for other systems to react to.
///     When a storm begins, every mapped <see cref="NivalisStormSpawnerComponent"/> releases
///     the horde. The storm persists until every living
///     <see cref="NivalisStormEnemyComponent"/> has been killed
/// </summary>
public sealed partial class NivalisSurvivalCycleSystem : GameRuleSystem<NivalisSurvivalCycleComponent>
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NivalisTraitDraftSystem _traitDraft = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Started(EntityUid uid, NivalisSurvivalCycleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.Phase = NivalisSurvivalPhase.Scavenge;
        component.NextPhaseChange = Timing.CurTime + TimeSpan.FromSeconds(component.ScavengeDuration);
        component.PhaseEndTime = component.NextPhaseChange;
        Dirty(uid, component);
    }

    protected override void ActiveTick(EntityUid uid, NivalisSurvivalCycleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        switch (component.Phase)
        {
            case NivalisSurvivalPhase.Scavenge:
                if (component.NextPhaseChange == TimeSpan.Zero || Timing.CurTime < component.NextPhaseChange)
                    break;
                AdvancePhase(uid, component);
                break;

            case NivalisSurvivalPhase.Storm:
                if (component.NextPhaseChange != TimeSpan.Zero && Timing.CurTime >= component.NextPhaseChange)
                {
                    AdvancePhase(uid, component);
                    break;
                }

                if (!AnyStormEnemiesAlive())
                    AdvancePhase(uid, component);
                break;
        }
    }

    private bool AnyStormEnemiesAlive()
    {
        var query = EntityQueryEnumerator<NivalisStormEnemyComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out var mobState))
        {
            if (_mobState.IsAlive(uid, mobState))
                return true;
        }

        return false;
    }

    private void SpawnStormEnemies(NivalisSurvivalCycleComponent component)
    {
        var spawned = 0;
        var query = EntityQueryEnumerator<NivalisStormSpawnerComponent, TransformComponent>();
        while (query.MoveNext(out _, out var spawner, out var xform))
        {
            var count = spawner.SpawnCount + component.WavesCleared * spawner.ThreatRampPerWave;
            for (var i = 0; i < count; i++)
            {
                var coords = xform.Coordinates;
                if (spawner.SpawnJitter > 0f)
                {
                    var offset = new Angle(_random.NextDouble() * Math.Tau).ToWorldVec() * (_random.NextFloat() * spawner.SpawnJitter);
                    coords = coords.Offset(offset);
                }

                EntityManager.SpawnEntity(spawner.SpawnPrototype, coords);
                spawned++;
            }
        }

        if (spawned > 0)
        {
            _chat.DispatchGlobalAnnouncement(
                Loc.GetString("nivalis-storm-sensitive", ("count", spawned)),
                colorOverride: Color.Crimson);
        }
    }
    private void OfferWaveTraits()
    {
        var query = EntityQueryEnumerator<NivalisPerkComponent>();
        while (query.MoveNext(out var uid, out var perk))
        {
            if (perk.Perks.Count >= perk.MaxPerks)
                continue;

            _traitDraft.OpenTraitDraft(uid, perk, 3);
        }
    }

    private void AdvancePhase(EntityUid uid, NivalisSurvivalCycleComponent component)
    {
        var oldPhase = component.Phase;

        switch (component.Phase)
        {
            case NivalisSurvivalPhase.Scavenge:
                component.Phase = NivalisSurvivalPhase.Storm;
                component.NextPhaseChange = Timing.CurTime + TimeSpan.FromSeconds(component.StormDuration);
                component.PhaseEndTime = component.NextPhaseChange;
                _chat.DispatchGlobalAnnouncement(Loc.GetString("nivalis-cycle-storm-start"), colorOverride: Color.DeepSkyBlue);
                SpawnStormEnemies(component);
                break;

            case NivalisSurvivalPhase.Storm:
                component.Phase = NivalisSurvivalPhase.Scavenge;
                component.NextPhaseChange = Timing.CurTime + TimeSpan.FromSeconds(component.ScavengeDuration);
                component.PhaseEndTime = component.NextPhaseChange;
                component.WavesCleared++;
                Dirty(uid, component);
                _chat.DispatchGlobalAnnouncement(Loc.GetString("nivalis-cycle-scavenge-start"), colorOverride: Color.Wheat);
                OfferWaveTraits();
                break;

            default:
                component.Phase = NivalisSurvivalPhase.Scavenge;
                component.NextPhaseChange = Timing.CurTime + TimeSpan.FromSeconds(component.ScavengeDuration);
                break;
        }

        Dirty(uid, component);

        var changeEv = new NivalisSurvivalPhaseChangedEvent(uid, oldPhase, component.Phase);
        RaiseLocalEvent(ref changeEv);
    }
}
