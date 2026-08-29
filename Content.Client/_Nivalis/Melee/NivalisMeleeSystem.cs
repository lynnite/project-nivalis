using System.Linq;
using System.Numerics;
using Content.Client.Gameplay;
using Content.Shared._Nivalis.Melee;
using Content.Shared._Nivalis.Melee.Events;
using Content.Shared._Nivalis.Stamina;
using Content.Shared.CombatMode;
using Content.Shared.Effects;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;

using Content.Shared.Input;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client._Nivalis.Melee;

public sealed partial class NivalisMeleeSystem : SharedNivalisMeleeSystem
{
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IStateManager _stateManager = default!;
    [Dependency] private AnimationPlayerSystem _animation = default!;
    [Dependency] private InputSystem _inputSystem = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private MapSystem _map = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private const string MeleeLungeKey = "nivalis-melee-lunge";
    private TimeSpan _nextShoveTime = TimeSpan.Zero;

    private EntityUid? _heavyChargingUser;
    private TimeSpan _heavyChargeStart = TimeSpan.Zero;
    private bool _isChargingHeavy;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<NivalisMeleeLungeEvent>(OnMeleeLunge);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.NivalisShove,
                new PointerInputCmdHandler(OnShoveCmd, outsidePrediction: false))
            .Register<NivalisMeleeSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<NivalisMeleeSystem>();
    }

    private bool OnShoveCmd(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.State != BoundKeyState.Down)
            return false;

        var entityNull = _player.LocalEntity;
        if (entityNull == null)
            return false;

        var user = entityNull.Value;

        if (!CombatMode.IsInCombatMode(user) || !Blocker.CanAttack(user))
            return false;

        if (Timing.CurTime < _nextShoveTime)
            return false;

        if (TryComp<NivalisStaminaComponent>(user, out var stamina) && stamina.Current < stamina.ShoveCost)
            return false;

        var hasWeapon = TryGetWeapon(user, out var weaponUid, out var weapon);
        if (hasWeapon && weapon!.NextAttack > Timing.CurTime)
            return false;

        var target = args.EntityUid;
        if (!target.IsValid() || !Exists(target) || target == user)
        {
            var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
            if (mousePos.MapId != MapId.Nullspace && _stateManager.CurrentState is GameplayStateBase screen)
            {
                var clicked = screen.GetClickedEntity(mousePos);
                if (clicked != null)
                    target = clicked.Value;
            }
        }

        if (target.IsValid() && target != user && Exists(target) && InRange(user, target, ShoveRange, null))
        {
            ClientShove(user, target, args.Coordinates, hasWeapon ? weaponUid : user, weapon);
            return true;
        }

        Hands.TryDrop(user);
        if (hasWeapon && weapon != null)
        {
            weapon.NextAttack = Timing.CurTime + TimeSpan.FromSeconds(0.3);
        }
        return false;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        UpdateEffects();
        UpdateHeavyChargeShake();
    }

    private void UpdateHeavyChargeShake()
    {
        if (!_isChargingHeavy || _heavyChargingUser == null || !Exists(_heavyChargingUser.Value))
            return;

        var user = _heavyChargingUser.Value;
        if (!TryComp<SpriteComponent>(user, out var sprite))
            return;

        var time = (float)Timing.CurTime.TotalSeconds;
        var shakeX = MathF.Sin(time * 40f) * 0.025f;
        var shakeY = MathF.Cos(time * 30f) * 0.025f;

        sprite.Offset = new Vector2(shakeX, shakeY);
    }

    private void ResetHeavyChargeShake(EntityUid? user)
    {
        if (user != null && Exists(user.Value) && TryComp<SpriteComponent>(user.Value, out var sprite))
        {
            sprite.Offset = Vector2.Zero;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var entityNull = _player.LocalEntity;
        if (entityNull == null)
        {
            if (_isChargingHeavy)
            {
                _isChargingHeavy = false;
                ResetHeavyChargeShake(_heavyChargingUser);
            }
            return;
        }

        var entity = entityNull.Value;

        if (!TryGetWeapon(entity, out var weaponUid, out var weapon))
        {
            if (_isChargingHeavy)
            {
                _isChargingHeavy = false;
                ResetHeavyChargeShake(_heavyChargingUser);
            }
            return;
        }

        if (!CombatMode.IsInCombatMode(entity) || !Blocker.CanAttack(entity))
        {
            if (_isChargingHeavy)
            {
                _isChargingHeavy = false;
                ResetHeavyChargeShake(_heavyChargingUser);
            }
            weapon.Attacking = false;
            return;
        }

        var useDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
        var altDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.UseSecondary);

        if (weapon.Attacking && useDown != BoundKeyState.Down && altDown != BoundKeyState.Down)
        {
            RaisePredictiveEvent(new NivalisStopAttackEvent(GetNetEntity(weaponUid)));
        }

        if (altDown == BoundKeyState.Down)
        {
            if (weapon.NextAttack <= Timing.CurTime)
            {
                if (weapon.HeavyWindupTime > 0f)
                {
                    if (!_isChargingHeavy)
                    {
                        if (CanAffordStamina(entity, weapon, true))
                        {
                            _isChargingHeavy = true;
                            _heavyChargingUser = entity;
                            _heavyChargeStart = Timing.CurTime;
                        }
                    }
                    else
                    {
                        var elapsed = (Timing.CurTime - _heavyChargeStart).TotalSeconds;
                        if (elapsed >= weapon.HeavyWindupTime)
                        {
                            _isChargingHeavy = false;
                            ResetHeavyChargeShake(entity);

                            var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
                            if (mousePos.MapId != MapId.Nullspace)
                            {
                                EntityCoordinates coordinates;
                                if (Maps.TryFindGridAt(mousePos, out var gridUid, out _))
                                    coordinates = TransformSystem.ToCoordinates(gridUid, mousePos);
                                else
                                    coordinates = TransformSystem.ToCoordinates(_map.GetMap(mousePos.MapId), mousePos);

                                ClientHeavyAttack(entity, coordinates, weaponUid, weapon);
                            }
                            return;
                        }
                    }
                }
                else
                {
                    var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
                    if (mousePos.MapId != MapId.Nullspace)
                    {
                        EntityCoordinates coordinates;
                        if (Maps.TryFindGridAt(mousePos, out var gridUid, out _))
                            coordinates = TransformSystem.ToCoordinates(gridUid, mousePos);
                        else
                            coordinates = TransformSystem.ToCoordinates(_map.GetMap(mousePos.MapId), mousePos);

                        ClientHeavyAttack(entity, coordinates, weaponUid, weapon);
                        return;
                    }
                }
            }
            return;
        }
        else
        {
            if (_isChargingHeavy)
            {
                _isChargingHeavy = false;
                ResetHeavyChargeShake(_heavyChargingUser);
            }
        }

        if (weapon.NextAttack > Timing.CurTime)
            return;

        var mousePosLight = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
        if (mousePosLight.MapId == MapId.Nullspace)
            return;

        EntityCoordinates coordinatesLight;
        if (Maps.TryFindGridAt(mousePosLight, out var gridUidLight, out _))
            coordinatesLight = TransformSystem.ToCoordinates(gridUidLight, mousePosLight);
        else
            coordinatesLight = TransformSystem.ToCoordinates(_map.GetMap(mousePosLight.MapId), mousePosLight);

        if (useDown == BoundKeyState.Down)
            ClientLightAttack(entity, mousePosLight, coordinatesLight, weaponUid, weapon);
    }

    private void ClientShove(EntityUid user, EntityUid target, EntityCoordinates coordinates, EntityUid weaponUid, NivalisMeleeComponent? weapon)
    {
        _nextShoveTime = Timing.CurTime + TimeSpan.FromSeconds(1.5);

        if (weapon != null)
        {
            weapon.NextAttack = Timing.CurTime + TimeSpan.FromSeconds(0.8);
            DirtyField(weaponUid, weapon, nameof(NivalisMeleeComponent.NextAttack));
        }

        RaiseNetworkEvent(new NivalisShoveEvent(GetNetEntity(target), GetNetCoordinates(coordinates)));
        PerformShove(user, target, coordinates);
    }

    protected override bool InRange(EntityUid user, EntityUid target, float range, ICommonSession? session)
    {
        var xform = Transform(target);
        var targetCoordinates = xform.Coordinates;
        var targetLocalAngle = xform.LocalRotation;

        return Interaction.InRangeUnobstructed(user, target, targetCoordinates, targetLocalAngle, range, overlapCheck: false);
    }

    protected override void DoDamageEffect(List<EntityUid> targets, EntityUid? user, TransformComponent targetXform)
    {
        _color.RaiseEffect(Color.Red, targets, Filter.Local());
    }

    private void ClientLightAttack(EntityUid attacker, MapCoordinates mousePos, EntityCoordinates coordinates, EntityUid weaponUid, NivalisMeleeComponent meleeComponent)
    {
        var attackerPos = TransformSystem.GetMapCoordinates(attacker);

        if (mousePos.MapId != attackerPos.MapId || (attackerPos.Position - mousePos.Position).Length() > meleeComponent.Range)
            return;

        EntityUid? target = null;
        if (_stateManager.CurrentState is GameplayStateBase screen)
            target = screen.GetClickedEntity(mousePos);

        if (Interaction.CombatModeCanHandInteract(attacker, target))
            return;

        RaisePredictiveEvent(new NivalisLightAttackEvent(GetNetEntity(target), GetNetEntity(weaponUid), GetNetCoordinates(coordinates)));
    }

    private void ClientHeavyAttack(EntityUid user, EntityCoordinates coordinates, EntityUid meleeUid, NivalisMeleeComponent component)
    {
        if (!TryComp(user, out TransformComponent? userXform) ||
            !Timing.IsFirstTimePredicted)
        {
            return;
        }

        var targetMap = TransformSystem.ToMapCoordinates(coordinates);
        if (targetMap.MapId != userXform.MapID)
            return;

        var userPos = TransformSystem.GetWorldPosition(userXform);
        var direction = targetMap.Position - userPos;
        var distance = MathF.Min(component.Range, direction.Length());

        var entities = GetNetEntityList(ArcRayCast(userPos, direction.ToWorldAngle(), component.Angle, distance, userXform.MapID, user).ToList());
        RaisePredictiveEvent(new NivalisHeavyAttackEvent(GetNetEntity(meleeUid), entities.GetRange(0, Math.Min(MaxTargets, entities.Count)), GetNetCoordinates(coordinates)));
    }

    private void OnMeleeLunge(NivalisMeleeLungeEvent ev)
    {
        var ent = GetEntity(ev.Entity);
        var entWeapon = GetEntity(ev.Weapon);

        if (Exists(ent) && Exists(entWeapon))
            DoLunge(ent, entWeapon, ev.Angle, ev.LocalPos, ev.Animation);
    }
}
