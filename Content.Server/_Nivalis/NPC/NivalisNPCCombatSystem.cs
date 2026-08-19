using System.Numerics;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Server._Nivalis.Combat;
using Content.Server._Nivalis.Melee;
using Content.Shared._Nivalis.Combat;
using Content.Shared._Nivalis.Melee;
using Content.Shared.CombatMode;
using Content.Shared.NPC;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.NPC;

/// <summary>
///     Drives Nivalis-melee combat for NPCs. Mirrors the classic
///     <see cref="NPCCombatSystem"/> melee loop but works against weapons using
///     <see cref="NivalisMeleeComponent"/> (light combos, heavy sweeps, parry).
/// </summary>
public sealed partial class NivalisNPCCombatSystem : EntitySystem
{
    private const float TargetMeleeLostRange = 16f;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly NPCSteeringSystem _steering = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly NivalisMeleeSystem _melee = default!;
    [Dependency] private readonly NivalisMeleeParrySystem _parry = default!;
    [Dependency] private readonly NivalisGrappleSystem _grapple = default!;

    private EntityQuery<CombatModeComponent> _combatQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _combatQuery = GetEntityQuery<CombatModeComponent>();

        SubscribeLocalEvent<NivalisMeleeCombatComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NivalisMeleeCombatComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, NivalisMeleeCombatComponent component, ComponentStartup args)
    {
        if (TryComp<CombatModeComponent>(uid, out var combatMode))
        {
            _combat.SetInCombatMode(uid, true, combatMode);
        }
    }

    private void OnShutdown(EntityUid uid, NivalisMeleeCombatComponent component, ComponentShutdown args)
    {
        if (TryComp<CombatModeComponent>(uid, out var combatMode))
        {
            _combat.SetInCombatMode(uid, false, combatMode);
        }

        _steering.Unregister(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<NivalisMeleeCombatComponent, ActiveNPCComponent>();

        while (query.MoveNext(out var uid, out var comp, out _))
        {
            if (!_combatQuery.TryGetComponent(uid, out var combat) || !combat.IsInCombatMode)
            {
                RemComp<NivalisMeleeCombatComponent>(uid);
                continue;
            }

            Attack(uid, comp, curTime);
        }
    }

    private void Attack(EntityUid uid, NivalisMeleeCombatComponent component, TimeSpan curTime)
    {
        component.Status = NivalisCombatStatus.Normal;

        if (component.Target == EntityUid.Invalid || !Exists(component.Target))
        {
            component.Status = NivalisCombatStatus.TargetUnreachable;
            return;
        }

        if (!_melee.TryGetWeapon(uid, out var weaponUid, out var weapon))
        {
            component.Status = NivalisCombatStatus.NoWeapon;
            return;
        }

        if (!TryComp(uid, out TransformComponent? xform) ||
            !TryComp(component.Target, out TransformComponent? targetXform))
        {
            component.Status = NivalisCombatStatus.TargetUnreachable;
            return;
        }

        if (!xform.Coordinates.TryDistance(EntityManager, targetXform.Coordinates, out var distance))
        {
            component.Status = NivalisCombatStatus.TargetUnreachable;
            return;
        }

        if (distance > TargetMeleeLostRange)
        {
            component.Status = NivalisCombatStatus.TargetUnreachable;
            return;
        }

        if (TryComp<NPCSteeringComponent>(uid, out var steering) &&
            steering.Status == SteeringStatus.NoPath)
        {
            component.Status = NivalisCombatStatus.TargetUnreachable;
            return;
        }

        _steering.Register(uid, new EntityCoordinates(component.Target, Vector2.Zero), steering);

        // Give the target a chance to react before swinging whenever the NPC is
        // physically able to reach them.
        if (distance > weapon.Range)
        {
            component.Status = NivalisCombatStatus.TargetOutOfRange;
            return;
        }

        // Parry handling: a parry-capable enemy will raise its guard when the
        // target closes into melee range.
        if (component.CanParry)
        {
            TryParry(uid, weaponUid, weapon, component, curTime);
        }

        // Grapple handling: a grapple-capable enemy locks onto the target.
        if (HasComp<NivalisGrappleComponent>(uid) && _grapple.TryStartGrapple(uid, component.Target))
            return;

        if (weapon.NextAttack > curTime)
            return;

        // Randomly perform a heavy sweeping attack.
        if (component.HeavyChance > 0f && _random.Prob(component.HeavyChance))
        {
            if (_melee.CanStartAttack(uid, weapon))
            {
                _melee.TryHeavyAttack(uid, weaponUid, weapon, targetXform.Coordinates);
                return;
            }
        }

        _melee.TryLightAttack(uid, weaponUid, weapon, component.Target);
    }

    private void TryParry(EntityUid uid, EntityUid weaponUid, NivalisMeleeComponent weapon,
        NivalisMeleeCombatComponent comp, TimeSpan curTime)
    {
        // Only parry if the weapon supports it and the parry is off cooldown.
        if (!_parry.HasParryWeapon(uid))
            return;

        if (comp.ParriedCooldownUntil > curTime)
            return;

        if (!_parry.TryStartParry(uid))
            return;

        comp.ParriedCooldownUntil = curTime + TimeSpan.FromSeconds(comp.ParryCooldown);
    }
}
