using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Perks;

[Serializable, NetSerializable]
public sealed class NivalisArbiterFlashMessage : EntityEventArgs
{
    public NetEntity Source;

    public byte Red = 255;

    public byte Green = 255;

    public byte Blue = 255;

    public float HoldTime;

    public float FadeTime;

    public NivalisArbiterFlashMessage()
    {
    }

    public NivalisArbiterFlashMessage(NetEntity source, byte red, byte green, byte blue, float hold, float fade)
    {
        Source = source;
        Red = red;
        Green = green;
        Blue = blue;
        HoldTime = hold;
        FadeTime = fade;
    }
}
