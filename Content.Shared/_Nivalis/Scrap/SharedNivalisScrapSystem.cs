using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Scrap;

public abstract partial class SharedNivalisScrapSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NivalisScrapComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<NivalisScrapComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Scrap < 0f)
        {
            ent.Comp.Scrap = 0f;
            Dirty(ent);
        }
    }

    public bool HasScrap(Entity<NivalisScrapComponent?> ent, out float amount)
    {
        amount = 0f;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        amount = ent.Comp.Scrap;
        return true;
    }

    public void SetScrap(Entity<NivalisScrapComponent?> ent, float value)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            ent.Comp = AddComp<NivalisScrapComponent>(ent);

        ent.Comp.Scrap = Math.Max(0f, value);
        Dirty(ent);
    }

    public bool ModifyScrap(Entity<NivalisScrapComponent?> ent, float delta)
    {
        if (!Resolve(ent, ref ent.Comp, false))
        {
            if (delta <= 0f)
                return false;
            ent.Comp = AddComp<NivalisScrapComponent>(ent);
        }

        var next = ent.Comp.Scrap + delta;
        var wasSpent = delta < 0f;
        if (wasSpent && next < 0f)
            return false;

        SetScrap(ent, next);
        return true;
    }
}

