using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Combat;

/// <summary>
///     Raised when a Nivalis grapple do-after completes or is cancelled.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class NivalisGrappleDoAfterEvent : SimpleDoAfterEvent
{
}
