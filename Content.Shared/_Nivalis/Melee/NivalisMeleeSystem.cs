using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared._Nivalis.Melee.Events;
using Content.Shared._Nivalis.Perks;
using Content.Shared._Nivalis.Stamina;
using Content.Shared._Nivalis.Status;
using Content.Shared._Nivalis.Traits;
using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.DoAfter;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._Nivalis.Melee;

public abstract partial class SharedNivalisMeleeSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] protected ActionBlockerSystem Blocker = default!;
    [Dependency] protected DamageableSystem Damageable = default!;
    [Dependency] protected SharedHandsSystem Hands = default!;
    [Dependency] protected SharedCombatModeSystem CombatMode = default!;
    [Dependency] protected SharedInteractionSystem Interaction = default!;
    [Dependency] protected SharedMapSystem Maps = default!;
    [Dependency] protected SharedPhysicsSystem Physics = default!;
    [Dependency] protected SharedPopupSystem PopupSystem = default!;
    [Dependency] protected SharedTransformSystem TransformSystem = default!;
    [Dependency] private SharedNivalisMeleeParrySystem _parry = default!;
    [Dependency] protected StatusEffectsSystem Status = default!;
    [Dependency] protected ThrowingSystem Throwing = default!;
    [Dependency] protected SharedDoAfterSystem DoAfter = default!;
    [Dependency] private NivalisFractureSystem _fracture = default!;

    private EntityQuery<DamageableComponent> _damageQuery = default!;

    private const int AttackMask = (int)(CollisionGroup.MobMask | CollisionGroup.Opaque);

    public const int MaxTargets = 5;
    public const float ShoveRange = 1.8f;

    public override void Initialize()
    {
        base.Initialize();

        _damageQuery = GetEntityQuery<DamageableComponent>();

        SubscribeAllEvent<NivalisLightAttackEvent>(OnLightAttack);
        SubscribeAllEvent<NivalisHeavyAttackEvent>(OnHeavyAttack);
        SubscribeAllEvent<NivalisStopAttackEvent>(OnStopAttack);
        SubscribeAllEvent<NivalisShoveEvent>(OnShove);
    }

    private void OnShove(NivalisShoveEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var target = GetEntity(msg.Target);
        PerformShove(user, target, GetCoordinates(msg.Coordinates), args.SenderSession);
    }

    public bool PerformShove(EntityUid user, EntityUid target, EntityCoordinates coordinates, ICommonSession? session = null)
    {
        if (Deleted(target) || user == target)
            return false;

        if (!CombatMode.IsInCombatMode(user) || !Blocker.CanAttack(user, target))
            return false;

        if (_fracture.HasArmFracture(user))
            return false;

        if (!InRange(user, target, ShoveRange, session))
            return false;

        if (TryComp<NivalisStaminaComponent>(user, out var shoveStamina) &&
            shoveStamina.Current < shoveStamina.ShoveCost)
        {
            return false;
        }

        var weaponUid = user;
        if (TryGetWeapon(user, out var wUid, out _))
            weaponUid = wUid;

        DoLungeAnimation(user, weaponUid, Angle.FromDegrees(45), TransformSystem.ToMapCoordinates(coordinates), ShoveRange, null);

        var swingSound = new SoundPathSpecifier("/Audio/Weapons/punchmiss.ogg")
        {
            Params = AudioParams.Default.AddVolume(-2f).WithVariation(0.05f),
        };
        if (_net.IsClient)
            Audio.PlayPredicted(swingSound, user, user);
        else
            Audio.PlayPvs(swingSound, user);

        var userPos = TransformSystem.GetWorldPosition(user);
        var targetPos = TransformSystem.GetWorldPosition(target);
        var dir = targetPos - userPos;
        if (dir.LengthSquared() > 0.0001f)
        {
            var pushDir = dir.Normalized();
            Throwing.TryThrow(target, pushDir * 0.5f, baseThrowSpeed: 2.5f, user: user, pushbackRatio: 0f, playSound: false, doSpin: false);
        }

        Status.TryAddStatusEffectDuration(target, SharedStunSystem.StunId, TimeSpan.FromSeconds(1.0));

        if (TryGetWeapon(target, out var targetWeaponUid, out var targetWeapon))
        {
            targetWeapon.Attacking = false;
            var curTime = Timing.CurTime;
            if (targetWeapon.NextAttack < curTime + TimeSpan.FromSeconds(1.0))
            {
                targetWeapon.NextAttack = curTime + TimeSpan.FromSeconds(1.0);
            }
            DirtyField(targetWeaponUid, targetWeapon, nameof(NivalisMeleeComponent.Attacking));
            DirtyField(targetWeaponUid, targetWeapon, nameof(NivalisMeleeComponent.NextAttack));
        }

        if (TryComp<NivalisMeleeParryComponent>(target, out var parryComp))
        {
            parryComp.Protecting = false;
            Dirty(target, parryComp);
        }

        if (TryComp<DoAfterComponent>(target, out var doAfterComp))
        {
            foreach (var doAfter in doAfterComp.DoAfters.Values)
            {
                DoAfter.Cancel(target, doAfter.Index, doAfterComp);
            }
        }

        if (_net.IsServer)
        {
            PopupSystem.PopupEntity(Loc.GetString("nivalis-shove-popup"), target, user);
            if (TryComp<NivalisStaminaComponent>(user, out var shoveDrain))
                DrainStamina(user, shoveDrain.ShoveCost);
        }

        return true;
    }

    private void OnStopAttack(NivalisStopAttackEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (user == null)
            return;

        if (!TryGetWeapon(user.Value, out var weaponUid, out var weapon) ||
            weaponUid != GetEntity(msg.Weapon))
        {
            return;
        }

        if (!weapon.Attacking)
            return;

        weapon.Attacking = false;
        DirtyField(weaponUid, weapon, nameof(NivalisMeleeComponent.Attacking));
    }

    private void OnLightAttack(NivalisLightAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        if (!TryGetWeapon(user, out var weaponUid, out var weapon) ||
            weaponUid != GetEntity(msg.Weapon))
        {
            return;
        }

        AttemptAttack(user, weaponUid, weapon, msg, args.SenderSession);
    }

    private void OnHeavyAttack(NivalisHeavyAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        if (!TryGetWeapon(user, out var weaponUid, out var weapon) ||
            weaponUid != GetEntity(msg.Weapon))
        {
            return;
        }

        AttemptAttack(user, weaponUid, weapon, msg, args.SenderSession);
    }

    public bool TryGetWeapon(EntityUid entity, out EntityUid weaponUid, [NotNullWhen(true)] out NivalisMeleeComponent? melee)
    {
        weaponUid = default;
        melee = null;

        if (Hands.TryGetActiveItem(entity, out var held))
        {
            if (TryComp<NivalisMeleeComponent>(held, out melee))
            {
                weaponUid = held.Value;
                return true;
            }
        }

        if (TryComp<NivalisMeleeComponent>(entity, out melee))
        {
            weaponUid = entity;
            return true;
        }

        return false;
    }

    public bool TryLightAttack(EntityUid user, EntityUid weaponUid, NivalisMeleeComponent weapon, EntityUid target)
    {
        if (!TryComp(target, out TransformComponent? targetXform))
            return false;

        return AttemptAttack(user, weaponUid, weapon,
            new NivalisLightAttackEvent(GetNetEntity(target), GetNetEntity(weaponUid), GetNetCoordinates(targetXform.Coordinates)),
            null);
    }

    public void TryLightAttackMiss(EntityUid user, EntityUid weaponUid, NivalisMeleeComponent weapon, EntityCoordinates coordinates)
    {
        AttemptAttack(user, weaponUid, weapon,
            new NivalisLightAttackEvent(null, GetNetEntity(weaponUid), GetNetCoordinates(coordinates)),
            null);
    }

    public bool TryHeavyAttack(EntityUid user, EntityUid weaponUid, NivalisMeleeComponent weapon, EntityCoordinates coordinates)
    {
        if (!TryComp(user, out TransformComponent? userXform))
            return false;

        var targetMap = TransformSystem.ToMapCoordinates(coordinates);
        if (targetMap.MapId != userXform.MapID)
            return false;

        var userPos = TransformSystem.GetWorldPosition(userXform);
        var direction = targetMap.Position - userPos;
        var distance = MathF.Min(weapon.Range, direction.Length());

        var entities = ArcRayCast(userPos, direction.ToWorldAngle(), weapon.Angle, distance, userXform.MapID, user).ToList();

        if (entities.Count > MaxTargets)
            entities.RemoveRange(MaxTargets, entities.Count - MaxTargets);

        var netEnts = new List<NetEntity>(entities.Count);
        foreach (var ent in entities)
            netEnts.Add(GetNetEntity(ent));

        return AttemptAttack(user, weaponUid, weapon,
            new NivalisHeavyAttackEvent(GetNetEntity(weaponUid), netEnts, GetNetCoordinates(coordinates)),
            null);
    }

    public bool CanStartAttack(EntityUid user, NivalisMeleeComponent weapon)
    {
        return weapon.NextAttack <= Timing.CurTime && CombatMode.IsInCombatMode(user);
    }

    private bool AttemptAttack(EntityUid user, EntityUid weaponUid, NivalisMeleeComponent weapon, NivalisAttackEvent attack, ICommonSession? session)
    {
        var curTime = Timing.CurTime;

        if (weapon.NextAttack > curTime)
            return false;

        if (!CombatMode.IsInCombatMode(user))
            return false;

        var lowStamina = !CanAffordStamina(user, weapon, attack is NivalisHeavyAttackEvent);

        EntityUid? target = null;
        switch (attack)
        {
            case NivalisLightAttackEvent light:
                if (light.Target != null && !TryGetEntity(light.Target, out target))
                {
                    return false;
                }

                if (!Blocker.CanAttack(user, target))
                    return false;

                if (weaponUid == target)
                    return false;

                break;
            default:
                if (!Blocker.CanAttack(user))
                    return false;
                break;
        }

        var isHeavy = attack is NivalisHeavyAttackEvent;
        if (!isHeavy)
        {
            if (weapon.ComboHits >= weapon.LightComboCount)
            {
                weapon.ComboHits = 0;
                DirtyField(weaponUid, weapon, nameof(NivalisMeleeComponent.ComboHits));
                return false;
            }

            weapon.ComboHits++;

            var cooldownSeconds = weapon.ComboHits >= weapon.LightComboCount
                ? weapon.LightComboRecovery
                : weapon.LightComboInterval;
            if (lowStamina)
                cooldownSeconds *= weapon.LowStaminaMultiplier;

            cooldownSeconds *= GetTraitFloat(user, c => c.LightSwingIntervalMult);
            cooldownSeconds *= GetPerkFloat(user, c => c.LightSwingIntervalMult);

            weapon.NextAttack = curTime + TimeSpan.FromSeconds(cooldownSeconds);

            DirtyField(weaponUid, weapon, nameof(NivalisMeleeComponent.NextAttack));
            DirtyField(weaponUid, weapon, nameof(NivalisMeleeComponent.ComboHits));
        }
        else
        {
            var fireRate = 1f / MathF.Max(0.1f, weapon.AttackRate);
            if (lowStamina)
                fireRate *= weapon.LowStaminaMultiplier;

            fireRate *= GetTraitFloat(user, c => c.HeavySwingIntervalMult);
            fireRate *= GetPerkFloat(user, c => c.HeavySwingIntervalMult);

            weapon.NextAttack = curTime + TimeSpan.FromSeconds(fireRate);
            DirtyField(weaponUid, weapon, nameof(NivalisMeleeComponent.NextAttack));
        }

        if (_net.IsServer)
        {
            var cost = isHeavy ? weapon.HeavyStaminaDamage : weapon.LightStaminaDamage;
            if (cost > 0f)
                DrainStamina(user, cost);
        }

        if (isHeavy)
            DoHeavyAttack(user, (NivalisHeavyAttackEvent)attack, weaponUid, weapon, session);
        else
            DoLightAttack(user, (NivalisLightAttackEvent)attack, weaponUid, weapon, session);

        DoLungeAnimation(user, weaponUid, weapon.Angle,
            TransformSystem.ToMapCoordinates(GetCoordinates(attack.Coordinates)), weapon.Range,
            isHeavy ? weapon.WideAnimation : weapon.Animation);

        weapon.Attacking = true;
        DirtyField(weaponUid, weapon, nameof(NivalisMeleeComponent.Attacking));

        return true;
    }

    private void DoLungeAnimation(EntityUid user, EntityUid weapon, Angle angle, MapCoordinates coordinates, float length, string? animation)
    {
        if (!TryComp(user, out TransformComponent? userXform))
            return;

        var invMatrix = TransformSystem.GetInvWorldMatrix(userXform);
        var localPos = Vector2.Transform(coordinates.Position, invMatrix);

        if (localPos.LengthSquared() <= 0f)
            return;

        localPos = userXform.LocalRotation.RotateVec(localPos);

        const float bufferLength = 0.2f;
        var visualLength = MathF.Max(0f, length - bufferLength);

        if (localPos.Length() > visualLength)
            localPos = localPos.Normalized() * visualLength;

        DoLunge(user, weapon, angle, localPos, animation);
    }

    protected bool CanAffordStamina(EntityUid user, NivalisMeleeComponent weapon, bool heavy)
    {
        if (!TryComp<NivalisStaminaComponent>(user, out var stamina))
            return true;

        var cost = heavy ? weapon.HeavyStaminaDamage : weapon.LightStaminaDamage;
        return cost <= 0f || stamina.Current >= cost;
    }

    private void DrainStamina(EntityUid user, float cost)
    {
        if (!TryComp<NivalisStaminaComponent>(user, out var stamina))
            return;

        stamina.Current = MathF.Max(0f, stamina.Current - cost);
        stamina.LastExertion = Timing.CurTime;
        Dirty(user, stamina);
    }

    protected float GetTraitFloat(EntityUid user, Func<NivalisTraitComponent, float> getter)
    {
        return TryComp<NivalisTraitComponent>(user, out var traits) ? getter(traits) : 1f;
    }

    protected float GetPerkFloat(EntityUid user, Func<NivalisPerkComponent, float> getter)
    {
        return TryComp<NivalisPerkComponent>(user, out var perk) ? getter(perk) : 1f;
    }

    private bool IsFistAttack(EntityUid user, EntityUid weaponUid)
    {
        return weaponUid == user;
    }

    private DamageSpecifier ApplyMeleeDamageMultipliers(EntityUid user, EntityUid weaponUid, DamageSpecifier damage)
    {
        var mult = 1f;
        if (TryComp<NivalisTraitComponent>(user, out var traits))
        {
            mult *= traits.MeleeDamageMult;
            if (IsFistAttack(user, weaponUid))
                mult *= traits.FistDamageMult;
        }
        if (TryComp<NivalisPerkComponent>(user, out var perks))
        {
            mult *= perks.MeleeDamageMult;
            if (IsFistAttack(user, weaponUid))
                mult *= perks.FistDamageMult;

            if (perks.MeleeDamageCap > 0f)
                mult = MathF.Min(mult, perks.MeleeDamageCap);
        }

        if (MathHelper.CloseTo(mult, 1f))
            return damage;

        return damage * mult;
    }

    protected abstract bool InRange(EntityUid user, EntityUid target, float range, ICommonSession? session);

    protected virtual void DoLightAttack(EntityUid user, NivalisLightAttackEvent ev, EntityUid meleeUid, NivalisMeleeComponent component, ICommonSession? session)
    {
        var damage = component.LightDamage;

        damage = ApplyMeleeDamageMultipliers(user, meleeUid, damage);

        var target = GetEntity(ev.Target);
        var resistanceBypass = false;

        if (Deleted(target) ||
            !HasComp<DamageableComponent>(target) ||
            !TryComp(target, out TransformComponent? targetXform) ||
            !InRange(user, target.Value, component.Range, session))
        {
            var missEvent = new NivalisMeleeHitEvent(new List<EntityUid>(), user, meleeUid, damage, null);
            RaiseLocalEvent(meleeUid, missEvent, broadcast: true);
            PlaySwingSound(user, meleeUid, component);
            return;
        }


        var hitEntities = new List<EntityUid>();
        hitEntities.Add(target.Value);
        var hitEvent = new NivalisMeleeHitEvent(hitEntities, user, meleeUid, damage, null);
        RaiseLocalEvent(meleeUid, hitEvent, broadcast: true);

        if (hitEvent.Handled)
            return;

        Interaction.DoContactInteraction(user, meleeUid);
        Interaction.DoContactInteraction(user, target);

        var attackedEvent = new NivalisAttackedEvent(meleeUid, user, targetXform.Coordinates);
        RaiseLocalEvent(target.Value, attackedEvent);

        if (_parry.TryParry(target.Value, user, meleeUid))
            return;

        var modifiedDamage = DamageSpecifier.ApplyModifierSets(damage + hitEvent.BonusDamage + attackedEvent.BonusDamage, hitEvent.ModifiersList);

        if (Damageable.TryChangeDamage(target.Value, modifiedDamage, out _, origin: user, ignoreResistances: resistanceBypass))
        {
            var targets = new List<EntityUid>();
            targets.Add(target.Value);
            DoDamageEffect(targets, user, targetXform);
        }

        PlayHitSound(target.Value, user, meleeUid, component);
    }

    private void DoHeavyAttack(EntityUid user, NivalisHeavyAttackEvent ev, EntityUid meleeUid, NivalisMeleeComponent component, ICommonSession? session)
    {
        if (!TryComp(user, out TransformComponent? userXform))
            return;

        var targetMap = TransformSystem.ToMapCoordinates(GetCoordinates(ev.Coordinates));

        if (targetMap.MapId != userXform.MapID)
            return;

        var userPos = TransformSystem.GetWorldPosition(userXform);
        var direction = targetMap.Position - userPos;
        var distance = MathF.Min(component.Range, direction.Length());

        var damage = component.HeavyDamage;

        damage = ApplyMeleeDamageMultipliers(user, meleeUid, damage);

        var entities = GetEntityList(ev.Entities);

        if (entities.Count == 0)
        {
            var missEvent = new NivalisMeleeHitEvent(new List<EntityUid>(), user, meleeUid, damage, direction);
            RaiseLocalEvent(meleeUid, missEvent, broadcast: true);
            PlaySwingSound(user, meleeUid, component);
            return;
        }

        if (entities.Count > MaxTargets)
            entities.RemoveRange(MaxTargets, entities.Count - MaxTargets);

        for (var i = entities.Count - 1; i >= 0; i--)
        {
            var entity = entities[i];

            if (TerminatingOrDeleted(entity))
            {
                entities.RemoveAt(i);
                continue;
            }

            if (!ArcRaySuccessful(entity, userPos, direction.ToWorldAngle(), component.Angle, distance, userXform.MapID, user, session))
                entities.RemoveAt(i);
        }

        var targets = new List<EntityUid>();
        foreach (var entity in entities)
        {
            if (entity == user || !_damageQuery.HasComponent(entity))
                continue;

            targets.Add(entity);
        }

        var hitEvent = new NivalisMeleeHitEvent(targets, user, meleeUid, damage, direction);
        RaiseLocalEvent(meleeUid, hitEvent, broadcast: true);

        if (hitEvent.Handled)
            return;

        Interaction.DoContactInteraction(user, meleeUid);
        foreach (var target in targets)
            Interaction.DoContactInteraction(user, target);

        var appliedDamage = new DamageSpecifier();
        for (var i = targets.Count - 1; i >= 0; i--)
        {
            var entity = targets[i];

            if (!Blocker.CanAttack(user, entity))
            {
                targets.RemoveAt(i);
                continue;
            }

            var attackedEvent = new NivalisAttackedEvent(meleeUid, user, GetCoordinates(ev.Coordinates));
            RaiseLocalEvent(entity, attackedEvent);

            if (_parry.TryParry(entity, user, meleeUid))
            {
                targets.RemoveAt(i);
                continue;
            }

            var modifiedDamage = DamageSpecifier.ApplyModifierSets(damage + hitEvent.BonusDamage + attackedEvent.BonusDamage, hitEvent.ModifiersList);
            var damageResult = Damageable.ChangeDamage(entity, modifiedDamage, origin: user);

            if (damageResult.GetTotal() > FixedPoint2.Zero)
                appliedDamage += damageResult;

            if (TerminatingOrDeleted(entity))
                targets.RemoveAt(i);
        }

        if (entities.Count > 0)
            PlayHitSound(entities[0], user, meleeUid, component);

        if (targets.Count > 0 && appliedDamage.GetTotal() > FixedPoint2.Zero)
            DoDamageEffect(targets, user, Transform(targets[0]));
    }

    protected HashSet<EntityUid> ArcRayCast(Vector2 position, Angle angle, Angle arcWidth, float range, MapId mapId, EntityUid ignore)
    {
        var widthRad = arcWidth;
        var increments = 1 + 35 * (int)Math.Ceiling(widthRad / (2 * Math.PI));
        var increment = widthRad / increments;
        var baseAngle = angle - widthRad / 2;

        var resSet = new HashSet<EntityUid>();

        for (var i = 0; i < increments; i++)
        {
            var castAngle = new Angle(baseAngle + increment * i);
            var res = Physics.IntersectRay(mapId,
                new CollisionRay(position,
                    castAngle.ToWorldVec(),
                    AttackMask),
                range,
                ignore,
                false)
                .ToList();

            if (res.Count != 0)
            {
                var resChecked = res.Where(x => x.Distance.Equals(res[0].Distance));
                foreach (var r in resChecked)
                {
                    if (Interaction.InRangeUnobstructed(ignore, r.HitEntity, range + 0.1f, overlapCheck: false))
                        resSet.Add(r.HitEntity);
                }
            }
        }

        return resSet;
    }

    protected virtual bool ArcRaySuccessful(EntityUid targetUid,
        Vector2 position,
        Angle angle,
        Angle arcWidth,
        float range,
        MapId mapId,
        EntityUid ignore,
        ICommonSession? session)
    {
        return true;
    }

    protected abstract void DoDamageEffect(List<EntityUid> targets, EntityUid? user, TransformComponent targetXform);

    public abstract void DoLunge(EntityUid user, EntityUid weapon, Angle angle, Vector2 localPos, string? animation, bool predicted = true);

    protected void PlaySwingSound(EntityUid user, EntityUid weapon, NivalisMeleeComponent component)
    {
        if (_net.IsClient)
            Audio.PlayPredicted(component.SwingSound, user, user);
        else
            Audio.PlayPvs(component.SwingSound, user);
    }

    protected void PlayHitSound(EntityUid target, EntityUid user, EntityUid weapon, NivalisMeleeComponent component)
    {
        if (component.HitSound != null)
        {
            if (_net.IsClient)
                Audio.PlayPredicted(component.HitSound, target, user);
            else
                Audio.PlayPvs(component.HitSound, target);
        }
    }
}

public sealed class NivalisMeleeHitEvent : EntityEventArgs
{
    public List<EntityUid> HitEntities = new();
    public EntityUid User;
    public EntityUid Weapon;
    public DamageSpecifier Damage;
    public Vector2? Direction;

    public DamageSpecifier BonusDamage = new();
    public List<DamageModifierSet> ModifiersList = new();

    public bool Handled;

    public bool IsHit => HitEntities.Count > 0;

    public NivalisMeleeHitEvent(List<EntityUid> hitEntities, EntityUid user, EntityUid weapon, DamageSpecifier damage, Vector2? direction)
    {
        HitEntities = hitEntities;
        User = user;
        Weapon = weapon;
        Damage = damage;
        Direction = direction;
    }
}
