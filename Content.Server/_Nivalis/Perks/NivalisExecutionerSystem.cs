using System;
using Content.Server._Nivalis.Scrap;
using Content.Shared._Nivalis.Perks;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Perks;

public sealed partial class NivalisExecutionerSystem : EntitySystem
{
    public const string ExecutionerPerk = "Executioner";

    [Dependency] private NivalisScrapSystem _scrap = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<NivalisPerkAbilityPressedMessage>(OnAbilityPressed);
        SubscribeLocalEvent<NivalisPerkComponent, DamageChangedEvent>(OnPerkDamageTaken);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private bool TryGetExecutioner(EntityUid uid, out Entity<NivalisExecutionerComponent?> exec)
    {
        exec = default;
        if (!TryComp<NivalisPerkComponent>(uid, out var perk) ||
            perk.Perk?.Id != ExecutionerPerk)
            return false;

        exec = (uid, EnsureComp<NivalisExecutionerComponent>(uid));
        return true;
    }

    private void OnAbilityPressed(NivalisPerkAbilityPressedMessage msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid)
            return;

        if (!TryGetExecutioner(uid, out var exec) || exec.Comp is null)
            return;

        if (exec.Comp.Live)
            Stow(exec);
        else
            Don(exec);
    }

    private void Don(Entity<NivalisExecutionerComponent?> exec)
    {
        if (exec.Comp!.Broken && exec.Comp.Durability < exec.Comp.RequipThreshold)
            return;

        exec.Comp.Live = true;
        exec.Comp.Durability = MathF.Min(exec.Comp.Durability, exec.Comp.MaxDurability);
        Dirty(exec.Owner, exec.Comp);

    }

    private void Stow(Entity<NivalisExecutionerComponent?> exec)
    {
        exec.Comp!.Live = false;

        if (exec.Comp.BountyCount > 0)
        {
            var stacks = exec.Comp.BountyCount;
            _scrap.GrantScrap(exec.Owner, stacks * exec.Comp.ScrapPerBounty);
            exec.Comp.BountyCount = 0;
        }

        Dirty(exec.Owner, exec.Comp);
    }

    private void OnPerkDamageTaken(Entity<NivalisPerkComponent> perk, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        if (perk.Comp.Perk?.Id != ExecutionerPerk)
            return;

        if (!TryGetExecutioner(perk.Owner, out var exec) || exec.Comp is null)
            return;

        if (!exec.Comp.Live || exec.Comp.Broken)
            return;

        var amount = args.DamageDelta.GetTotal().Float();
        if (amount <= 0f || amount < exec.Comp.DamageThreshold)
            return;

        var cost = MathF.Min(exec.Comp.AbsorbPerHit + amount * 0.15f, exec.Comp.MaxDurability);
        exec.Comp.Durability = MathF.Max(0f, exec.Comp.Durability - cost);

        if (exec.Comp.Durability <= 0f)
        {
            exec.Comp.Broken = true;
        }

        Dirty(perk.Owner, exec.Comp);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.NewMobState != MobState.Dead || ev.Origin is not { } killer)
            return;

        if (!TryGetExecutioner(killer, out var exec) || exec.Comp is null)
            return;

        if (!exec.Comp.Live || exec.Comp.Broken)
            return;

        if (ev.Target == killer)
            return;

        exec.Comp.BountyCount++;
        Dirty(killer, exec.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NivalisExecutionerComponent>();
        while (query.MoveNext(out var uid, out var exec))
        {
            if (exec.Live)
                continue;

            if (exec.Durability >= exec.MaxDurability)
            {
                if (exec.Broken)
                {
                    exec.Broken = false;
                    Dirty(uid, exec);
                }
                continue;
            }

            var before = exec.Durability;
            exec.Durability = MathF.Min(exec.MaxDurability, exec.Durability + exec.RegenPerSecond * frameTime);
            if (!MathHelper.CloseTo(before, exec.Durability))
                Dirty(uid, exec);
        }
    }
}
