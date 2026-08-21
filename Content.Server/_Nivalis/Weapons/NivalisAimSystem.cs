using Content.Shared._Nivalis.Weapons;

namespace Content.Server._Nivalis.Weapons;

/// <summary>
///     Server half of the Nivalis aim system. The shared system handles the predictive
///     input; this class exists so the system is registered on the server.
/// </summary>
public sealed partial class NivalisAimSystem : SharedNivalisAimSystem;
