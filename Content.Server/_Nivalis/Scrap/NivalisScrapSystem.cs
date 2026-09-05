using Content.Shared._Nivalis.Scrap;
using Robust.Shared.GameObjects;

namespace Content.Server._Nivalis.Scrap;

public sealed partial class NivalisScrapSystem : SharedNivalisScrapSystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public void GrantScrap(EntityUid receiver, float amount)
    {
        if (!Exists(receiver) || amount <= 0f)
            return;

        ModifyScrap((receiver, null), amount);
    }
}

