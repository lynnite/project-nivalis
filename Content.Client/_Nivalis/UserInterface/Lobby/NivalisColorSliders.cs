using System;
using System.Globalization;
using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client._Nivalis.UserInterface.Lobby;

public sealed class NivalisColorSliders : BoxContainer
{
    public Action<Color>? OnColorChanged;

    private enum Mode
    {
        Hsv,
        Rgb,
    }

    private Mode _mode = Mode.Hsv;

    private Color _color = Color.White;

    private readonly NivalisSlider[] _sliders = new NivalisSlider[3];
    private readonly LineEdit[] _fields = new LineEdit[3];
    private readonly Label[] _labels = new Label[3];
    private readonly ScrollingScanlineStyleBox[] _fills = new ScrollingScanlineStyleBox[3];

    private NivalisDropdown? _modeDropdown;

    private bool _updating;

    private readonly Font _labelFont;

    private Texture? _scanlineTexture;

    public NivalisColorSliders()
    {
        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 4;
        HorizontalExpand = true;

        var cache = IoCManager.Resolve<IResourceCache>();
        _labelFont = cache.GetFont("/Fonts/Orbitron/Orbitron-Regular.ttf", 11);

        var modeRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 8,
        };
        modeRow.AddChild(new Label
        {
            Text = "MODE",
            MinWidth = 90,
            VerticalAlignment = VAlignment.Center,
            FontOverride = _labelFont,
            FontColorOverride = Color.FromHex("#8FA3B8"),
        });
        modeRow.AddChild(new Control { HorizontalExpand = true });

        _modeDropdown = new NivalisDropdown
        {
            HorizontalAlignment = HAlignment.Right,
            VerticalAlignment = VAlignment.Center,
            MinSize = new Vector2(110, 26),
            FontOverride = _labelFont,
        };
        _modeDropdown.AddItem("HSV", (int)Mode.Hsv);
        _modeDropdown.AddItem("RGB", (int)Mode.Rgb);
        _modeDropdown.SelectId((int)Mode.Hsv);
        _modeDropdown.OnItemSelected += args =>
        {
            _modeDropdown.SelectId(args.Id);
            SetMode((Mode)args.Id);
        };
        modeRow.AddChild(_modeDropdown);
        AddChild(modeRow);

        for (var i = 0; i < 3; i++)
        {
            var index = i;
            var fill = new ScrollingScanlineStyleBox();
            _fills[i] = fill;

            var (slider, field, label) = MakeRow(fill);
            _sliders[i] = slider;
            _fields[i] = field;
            _labels[i] = label;

            slider.OnValueChanged += _ => OnSliderDragged();
            slider.OnValueCommitted += _ => OnSliderCommitted();

            field.OnTextChanged += args => OnFieldFocusedChanged(index, args.Text);
            field.OnFocusExit += _ => CommitField(index);
            field.OnTextEntered += _ => CommitField(index);
        }

        SetMode(Mode.Hsv);
    }

    private (NivalisSlider Slider, LineEdit Field, Label Label) MakeRow(ScrollingScanlineStyleBox fill)
    {
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 8,
        };

        var text = new Label
        {
            Text = string.Empty,
            MinWidth = 90,
            VerticalAlignment = VAlignment.Center,
            FontOverride = _labelFont,
            FontColorOverride = Color.FromHex("#8FA3B8"),
        };

        var slider = new NivalisSlider
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            ScanlineTexture = _scanlineTexture,
            FillOverride = fill,
        };

        var field = new LineEdit
        {
            MinSize = new Vector2(52, 0),
            VerticalAlignment = VAlignment.Center,
            HorizontalAlignment = HAlignment.Right,
        };

        row.AddChild(text);
        row.AddChild(slider);
        row.AddChild(field);
        AddChild(row);

        return (slider, field, text);
    }

    public bool RgbMode => _mode == Mode.Rgb;

    public Texture? ScanlineTexture
    {
        get => _scanlineTexture;
        set
        {
            _scanlineTexture = value;
            foreach (var slider in _sliders)
                slider.ScanlineTexture = value;
            foreach (var fill in _fills)
                fill.Texture = value;
            if (_modeDropdown != null)
                _modeDropdown.ScanlineTexture = value;
            RefreshChannelVisuals();
        }
    }

    public Color Color
    {
        get => _color;
        set
        {
            _color = value;
            UpdateFromColor();
        }
    }

    private void SetMode(Mode mode)
    {
        _mode = mode;

        _labels[0].Text = _mode == Mode.Rgb ? "RED" : Loc.GetString("color-selector-sliders-hue");
        _labels[1].Text = _mode == Mode.Rgb ? "GREEN" : Loc.GetString("color-selector-sliders-saturation");
        _labels[2].Text = _mode == Mode.Rgb ? "BLUE" : Loc.GetString("color-selector-sliders-value");

        foreach (var slider in _sliders)
        {
            slider.MinValue = 0f;
            slider.MaxValue = _mode == Mode.Rgb ? 255f : 1f;
        }

        UpdateFromColor();
        OnColorChanged?.Invoke(_color);
    }

    private void UpdateFromColor()
    {
        _updating = true;

        if (_mode == Mode.Rgb)
        {
            SetSlider(0, _color.RByte);
            SetSlider(1, _color.GByte);
            SetSlider(2, _color.BByte);
        }
        else
        {
            var hsv = Color.ToHsv(_color);
            SetSlider(0, hsv.X);
            SetSlider(1, hsv.Y);
            SetSlider(2, hsv.Z);
        }

        _updating = false;
        RefreshChannelVisuals();
    }

    private void SetSlider(int index, float value)
    {
        _sliders[index].SetValueSilently(value);
        UpdateField(index, value);
    }

    private void UpdateField(int index, float value)
    {
        if (_fields[index].HasKeyboardFocus())
            return;

        var text = _mode == Mode.Rgb
            ? ((int)MathF.Round(value)).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.###", CultureInfo.InvariantCulture);
        _fields[index].Text = text;
    }

    private void OnSliderDragged()
    {
        if (_updating)
            return;

        _updating = true;
        for (var i = 0; i < 3; i++)
            UpdateField(i, _sliders[i].Value);
        _updating = false;
        RefreshChannelVisuals();
    }

    private void OnSliderCommitted()
    {
        if (_updating)
            return;

        CommitSlidersToColor();
    }

    private void CommitSlidersToColor()
    {
        _color = ReadColorFromSliders();
        OnColorChanged?.Invoke(_color);
        RefreshChannelVisuals();
    }

    private Color ReadColorFromSliders()
    {
        if (_mode == Mode.Rgb)
        {
            var r = (byte)Math.Clamp((int)MathF.Round(_sliders[0].Value), 0, 255);
            var g = (byte)Math.Clamp((int)MathF.Round(_sliders[1].Value), 0, 255);
            var b = (byte)Math.Clamp((int)MathF.Round(_sliders[2].Value), 0, 255);
            return new Color(r, g, b);
        }

        return Color.FromHsv(new Vector4(_sliders[0].Value, _sliders[1].Value, _sliders[2].Value, 1f));
    }

    private void OnFieldFocusedChanged(int index, string text)
    {
        if (_updating)
            return;

        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return;

        _sliders[index].SetValueSilently(parsed);
    }

    private void CommitField(int index)
    {
        if (!float.TryParse(_fields[index].Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return;

        _sliders[index].SetValueSilently(parsed);
        CommitSlidersToColor();
    }

    private void RefreshChannelVisuals()
    {
        if (_mode == Mode.Rgb)
        {
            var r = (byte)Math.Clamp((int)MathF.Round(_sliders[0].Value), 0, 255);
            var g = (byte)Math.Clamp((int)MathF.Round(_sliders[1].Value), 0, 255);
            var b = (byte)Math.Clamp((int)MathF.Round(_sliders[2].Value), 0, 255);

            _fills[0].BackgroundColor = new Color(r, 0, 0);
            _fills[1].BackgroundColor = new Color(0, g, 0);
            _fills[2].BackgroundColor = new Color(0, 0, b);
            return;
        }

        var h = _sliders[0].Value;
        var s = _sliders[1].Value;
        var v = _sliders[2].Value;

        _fills[0].BackgroundColor = Color.FromHsv(new Vector4(h, 1f, 1f, 1f));
        _fills[1].BackgroundColor = Color.FromHsv(new Vector4(h, s, 1f, 1f));
        _fills[2].BackgroundColor = Color.FromHsv(new Vector4(h, s, v, 1f));
    }
}
