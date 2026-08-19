using System.Threading;
using System.Threading.Tasks;
using Content.Server._Nivalis.NPC;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.CombatMode;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server._Nivalis.NPC.HTN.PrimitiveTasks.Operators.Combat.Melee;

/// <summary>
///     Attacks the specified blackboard target in Nivalis melee combat, delegating the actual
///     light/heavy swings to <see cref="NivalisNPCCombatSystem"/>.
///     Mirrors the classic <c>MeleeOperator</c> but works with Nivalis melee weapons.
/// </summary>
public sealed partial class NivalisMeleeOperator : HTNOperator, IHtnConditionalShutdown
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    [DataField("targetKey", required: true)]
    public string TargetKey = default!;

    [DataField("targetState")]
    public MobState TargetState = MobState.Alive;

    [DataField("heavyChance")]
    public float HeavyChance = 0f;

    [DataField("canParry")]
    public bool CanParry = false;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        // HeavyChance/CanParry are configured on the entity's prototype NivalisMeleeCombat
        // component, so (re)creating the component restores them. We only set the target here.
        var combat = _entManager.EnsureComponent<NivalisMeleeCombatComponent>(owner);
        combat.Target = blackboard.GetValue<EntityUid>(TargetKey);
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
            return (false, null);

        if (_entManager.TryGetComponent<MobStateComponent>(target, out var mobState) &&
            mobState.CurrentState > TargetState)
        {
            return (false, null);
        }

        return (true, null);
    }

    public void ConditionalShutdown(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (_entManager.HasComponent<NivalisMeleeCombatComponent>(owner))
            _entManager.RemoveComponent<NivalisMeleeCombatComponent>(owner);
        blackboard.Remove<EntityUid>(TargetKey);
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        ConditionalShutdown(blackboard);
    }

    public override void PlanShutdown(NPCBlackboard blackboard)
    {
        base.PlanShutdown(blackboard);
        ConditionalShutdown(blackboard);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        base.Update(blackboard, frameTime);

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        HTNOperatorStatus status;

        if (_entManager.TryGetComponent<NivalisMeleeCombatComponent>(owner, out var combat) &&
            blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) &&
            target != EntityUid.Invalid)
        {
            combat.Target = target;

            if (_entManager.TryGetComponent<MobStateComponent>(target, out var mobState) &&
                mobState.CurrentState > TargetState)
            {
                status = HTNOperatorStatus.Finished;
            }
            else
            {
                switch (combat.Status)
                {
                    case NivalisCombatStatus.TargetOutOfRange:
                    case NivalisCombatStatus.Normal:
                        status = HTNOperatorStatus.Continuing;
                        break;
                    default:
                        status = HTNOperatorStatus.Failed;
                        break;
                }
            }
        }
        else
        {
            status = HTNOperatorStatus.Failed;
        }

        if (status == HTNOperatorStatus.Continuing && ShutdownState == HTNPlanState.PlanFinished)
        {
            status = HTNOperatorStatus.Finished;
        }

        return status;
    }
}
