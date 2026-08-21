using Content.Shared.Hands.EntitySystems;

namespace Content.Shared._Nivalis.Weapons;

/// <summary>
///     Shared system that receives <see cref="NivalisAimEvent"/> input and updates the
///     held gun's <see cref="NivalisAimComponent.Aiming"/> state, tightening scatter.
/// </summary>
public abstract partial class SharedNivalisAimSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedNivalisScatterSystem _scatter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<NivalisAimEvent>(OnAimInput);
    }

    private void OnAimInput(NivalisAimEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (user == null)
            return;

        var held = _hands.GetActiveItem((user.Value, null));
        if (held == null || !TryComp<NivalisAimComponent>(held, out var aim))
            return;

        _scatter.SetAiming(held.Value, msg.Active);
    }
}

