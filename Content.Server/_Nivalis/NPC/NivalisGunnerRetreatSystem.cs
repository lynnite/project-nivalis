using System.Numerics;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Shared._Nivalis.Weapons;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
namespace Content.Server._Nivalis.NPC;

public sealed partial class NivalisGunnerRetreatSystem : EntitySystem
{
    [Dependency] private readonly NPCSteeringSystem _steering = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float RetreatDistance = 1.5f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NPCRangedCombatComponent, InputMoverComponent, TransformComponent>();
        while (query.MoveNext(out var npc, out var ranged, out _, out var npcXform))
        {
            if (ranged.Target == EntityUid.Invalid ||
                !Exists(ranged.Target) ||
                !TryComp(ranged.Target, out TransformComponent? targetXform))
            {
                continue;
            }

            var weaponDry = false;
            foreach (var item in _hands.EnumerateHeld(npc))
            {
                if (TryComp<NivalisAutoReloadComponent>(item, out _) &&
                    TryComp<BasicEntityAmmoProviderComponent>(item, out var ammo) &&
                    ammo.Count is not null && ammo.Count <= 0)
                {
                    weaponDry = true;
                    break;
                }
            }

            if (!weaponDry)
                continue;

            var npcPos = _transform.GetWorldPosition(npcXform);
            var targetPos = _transform.GetWorldPosition(targetXform);
            var direction = npcPos - targetPos;

            if (direction.LengthSquared() < 0.0001f)
                direction = new Vector2(1f, 0f);
            else
                direction = Vector2.Normalize(direction);

            var retreatPos = npcPos + direction * RetreatDistance;
            var coordinates = _transform.ToCoordinates(new MapCoordinates(retreatPos, npcXform.MapID));

            if (TryComp<NPCSteeringComponent>(npc, out var existing) &&
                existing.Status == SteeringStatus.InRange)
            {
                continue;
            }

            var steering = _steering.Register(npc, coordinates);
            steering.Range = 0.5f;
            steering.ForceMove = false;
            steering.ArriveOnLineOfSight = false;
        }
    }
}

