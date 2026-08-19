using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Health;

/// <summary>
///     When attached to a mob, said mob will never enter the <see cref="MobState.Critical"/>
///     state. Instead it goes straight to death once its damage passes its death threshold.
/// </summary>
[RegisterComponent, NetworkedComponent]
[EntityCategory("Mobs")]
public sealed partial class NivalisNoCriticalComponent : Component
{
}
