using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Shared._Nivalis.Recycling;

[RegisterComponent]
public sealed partial class NivalisRecyclerComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public bool RequireRecyclableTag = true;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BaseScrapReward = 50f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;

    [DataField]
    public SoundSpecifier? CrushSound = new SoundPathSpecifier("/Audio/Effects/metal_scrape2.ogg");
}

