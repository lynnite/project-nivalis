using System.Linq;
using Content.Client.Gameplay;
using Content.Shared._Nivalis.Melee;
using Content.Shared._Nivalis.Melee.Events;
using Content.Shared.CombatMode;
using Content.Shared.Effects;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;

using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client._Nivalis.Melee;

public sealed partial class NivalisMeleeSystem : SharedNivalisMeleeSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string MeleeLungeKey = "nivalis-melee-lunge";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<NivalisMeleeLungeEvent>(OnMeleeLunge);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        UpdateEffects();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var entityNull = _player.LocalEntity;
        if (entityNull == null)
            return;

        var entity = entityNull.Value;

        if (!TryGetWeapon(entity, out var weaponUid, out var weapon))
            return;

        if (!CombatMode.IsInCombatMode(entity) || !Blocker.CanAttack(entity))
        {
            weapon.Attacking = false;
            return;
        }

        var useDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
        var altDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.UseSecondary);

        if (weapon.Attacking && useDown != BoundKeyState.Down && altDown != BoundKeyState.Down)
        {
            RaisePredictiveEvent(new NivalisStopAttackEvent(GetNetEntity(weaponUid)));
        }

        if (weapon.NextAttack > Timing.CurTime)
            return;

        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
        if (mousePos.MapId == MapId.Nullspace)
            return;

        EntityCoordinates coordinates;
        if (Maps.TryFindGridAt(mousePos, out var gridUid, out _))
            coordinates = TransformSystem.ToCoordinates(gridUid, mousePos);
        else
            coordinates = TransformSystem.ToCoordinates(_map.GetMap(mousePos.MapId), mousePos);

        if (altDown == BoundKeyState.Down)
        {
            ClientHeavyAttack(entity, coordinates, weaponUid, weapon);
            return;
        }

        if (useDown == BoundKeyState.Down)
            ClientLightAttack(entity, mousePos, coordinates, weaponUid, weapon);
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
