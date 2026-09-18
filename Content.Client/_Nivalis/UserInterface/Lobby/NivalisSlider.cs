using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client._Nivalis.UserInterface.Lobby;

public sealed class NivalisSlider : Control
{
    public event Action<NivalisSlider>? OnValueChanged;

    public event Action<NivalisSlider>? OnValueCommitted;

    private float _minValue;
    private float _maxValue = 100f;
    private float _value;

    private bool _dragging;
    private bool _hovered;
    private bool _changedWhileDragging;

    private readonly ScrollingScanlineStyleBox _trackStyle = new();
    private readonly ScrollingScanlineStyleBox _fillStyle = new();
    private readonly ScrollingScanlineStyleBox _handleStyle = new();

    private const float TrackHeight = 16f;
    private const float HandleWidth = 14f;

    public NivalisSlider()
    {
        MinSize = new Vector2(120, TrackHeight + 6);
        MouseFilter = MouseFilterMode.Stop;
        DefaultCursorShape = CursorShape.Pointer;

        _trackStyle.BackgroundColor = new Color(20, 28, 40, 230);
        _trackStyle.BorderColor = Color.FromHex("#58C6E8").WithAlpha(0.5f);
        _trackStyle.BorderThickness = 1f;
        _trackStyle.InwardLean = 0.06f;

        _fillStyle.BackgroundColor = Color.FromHex("#4a82bf");
        _fillStyle.BorderColor = Color.FromHex("#bfe3ff").WithAlpha(0.7f);
        _fillStyle.BorderThickness = 1f;
        _fillStyle.InwardLean = 0.06f;

        _handleStyle.BackgroundColor = Color.FromHex("#eefbff");
        _handleStyle.BorderColor = Color.White;
        _handleStyle.BorderThickness = 1.5f;
        _handleStyle.InwardLean = 0.1f;
    }

    public float MinValue
    {
        get => _minValue;
        set
        {
            _minValue = value;
            Value = Math.Clamp(_value, _minValue, _maxValue);
        }
    }

    public float MaxValue
    {
        get => _maxValue;
        set
        {
            _maxValue = value;
            Value = Math.Clamp(_value, _minValue, _maxValue);
        }
    }

    public float Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, _minValue, _maxValue);
            if (MathHelper.CloseTo(clamped, _value))
                return;

            _value = clamped;
            OnValueChanged?.Invoke(this);
        }
    }

    private float Fraction => _maxValue - _minValue <= 0f
        ? 0f
        : Math.Clamp((_value - _minValue) / (_maxValue - _minValue), 0f, 1f);

    public void SetValueSilently(float value)
    {
        _value = Math.Clamp(value, _minValue, _maxValue);
    }

    public Texture? ScanlineTexture
    {
        set
        {
            _trackStyle.Texture = value;
            _fillStyle.Texture = value;
            _handleStyle.Texture = value;
        }
    }

    public ScrollingScanlineStyleBox? FillOverride
    {
        get => _fillOverride;
        set
        {
            _fillOverride = value;
            if (value != null)
                value.Texture = _trackStyle.Texture;
        }
    }

    private ScrollingScanlineStyleBox? _fillOverride;

    protected override void MouseEntered()
    {
        base.MouseEntered();
        _hovered = true;
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        _hovered = false;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _dragging = true;
            args.Handle();
            UpdateFromMouse(args.RelativePosition.X);
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            var wasDragging = _dragging;
            _dragging = false;
            args.Handle();

            if (wasDragging && _changedWhileDragging)
            {
                _changedWhileDragging = false;
                OnValueCommitted?.Invoke(this);
            }
        }
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (_dragging)
            UpdateFromMouse(args.RelativePosition.X);
    }

    private void UpdateFromMouse(float x)
    {
        var usable = Math.Max(Width - HandleWidth, 1f);
        var frac = Math.Clamp((x - HandleWidth / 2f) / usable, 0f, 1f);
        var newValue = _minValue + frac * (_maxValue - _minValue);
        if (!MathHelper.CloseTo(newValue, _value))
            _changedWhileDragging = true;
        Value = newValue;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var midY = Height / 2f;
        var track = UIBox2.FromDimensions(new Vector2(0, midY - TrackHeight / 2f), new Vector2(Width, TrackHeight));
        _trackStyle.Draw(handle, track, UIScale);

        var frac = Fraction;
        var fillWidth = frac * (Width - HandleWidth) + HandleWidth;
        if (fillWidth > 1f)
        {
            var fill = UIBox2.FromDimensions(new Vector2(0, midY - TrackHeight / 2f), new Vector2(fillWidth, TrackHeight));
            (_fillOverride ?? _fillStyle).Draw(handle, fill, UIScale);
        }

        var handleX = frac * (Width - HandleWidth);
        var handleBox = UIBox2.FromDimensions(new Vector2(handleX, midY - TrackHeight / 2f - 2f),
            new Vector2(HandleWidth, TrackHeight + 4f));

        if (_hovered || _dragging)
            _handleStyle.Modulate = Color.White;
        else
            _handleStyle.Modulate = new Color(0.9f, 0.9f, 0.9f);

        _handleStyle.Draw(handle, handleBox, UIScale);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _trackStyle.ScrollOffset += args.DeltaSeconds * 9f;
        _fillStyle.ScrollOffset += args.DeltaSeconds * 9f;
    }
}
