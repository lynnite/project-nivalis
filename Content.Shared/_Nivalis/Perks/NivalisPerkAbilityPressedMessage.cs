using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Perks;

/// <summary>
///     Sent from the client lobby-in-play loop when the player activates their equipped perk
///     ability (F key). The server enforces the perk cooldown before dispatching its effect.
/// </summary>
[Serializable, NetSerializable]
public sealed class NivalisPerkAbilityPressedMessage : EntityEventArgs
{
}
