using Content.Shared._Nivalis.Weapons;

namespace Content.Client._Nivalis.Weapons;

/// <summary>
///     Client-side concrete lookup for <see cref="SharedNivalisScatterSystem"/> so the
///     shared aim system's dependency can be resolved during client prediction. The actual
///     scatter/aim logic lives in the shared system.
/// </summary>
public sealed partial class NivalisScatterSystem : SharedNivalisScatterSystem
{
}
