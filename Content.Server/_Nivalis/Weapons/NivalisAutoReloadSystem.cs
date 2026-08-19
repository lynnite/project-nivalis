using Content.Shared._Nivalis.Weapons;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Weapons;

/// <summary>
///     Handles whole-magazine self-reload for scavenger guns carrying
///     <see cref="NivalisAutoReloadComponent"/>. Only runs server-side.
/// </summary>
public sealed partial class NivalisAutoReloadSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NivalisAutoReloadComponent, BasicEntityAmmoProviderComponent>();
        while (query.MoveNext(out var uid, out var reload, out var ammo))
        {
            // Already full: nothing to do.
            if (ammo.Count >= ammo.Capacity)
            {
                if (reload.Reloading)
                {
                    reload.Reloading = false;
                    Dirty(uid, reload);
                }
                continue;
            }

            // Not empty yet: wait until the magazine is fully spent before reloading.
            if (ammo.Count > 0 || reload.Reloading == false)
            {
                if (ammo.Count == 0 && !reload.Reloading)
                {
                    // Magazine just ran dry - start the reload.
                    reload.Reloading = true;
                    reload.NextReload = _timing.CurTime + TimeSpan.FromSeconds(reload.ReloadDelay);
                    Dirty(uid, reload);

                    if (reload.SoundMagOut != null)
                        _audio.PlayPvs(reload.SoundMagOut, uid);
                }
                continue;
            }

            // Reloading and the delay has elapsed: refill the whole magazine.
            if (_timing.CurTime < reload.NextReload)
                continue;

            _gun.UpdateBasicEntityAmmoCount((uid, ammo), ammo.Capacity ?? 0);
            reload.Reloading = false;
            Dirty(uid, reload);

            if (reload.SoundMagIn != null)
                _audio.PlayPvs(reload.SoundMagIn, uid);
        }
    }
}
