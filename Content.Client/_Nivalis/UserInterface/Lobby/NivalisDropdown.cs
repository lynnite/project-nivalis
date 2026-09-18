using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client._Nivalis.UserInterface.Lobby;

public sealed class NivalisDropdown : BoxContainer
{
    public event Action<ItemEventArgs>? OnItemSelected;

    public sealed class ItemEventArgs : EventArgs
    {
        public int Id { get; }

        public ItemEventArgs(int id)
        {
            Id = id;
        }
    }

    private sealed class DropdownButton : ContainerButton
    {
        public string DisplayText = string.Empty;
        public Font? DisplayFont;
        public Color DisplayColor = Color.White;

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (string.IsNullOrEmpty(DisplayText))
                return;

            var font = DisplayFont ?? UserInterfaceManager.ThemeDefaults.DefaultFont;
            var dims = handle.GetDimensions(font, DisplayText, 1f);
            var pos = new Vector2((Width - dims.X) / 2f, (Height - dims.Y) / 2f);
            handle.DrawString(font, pos, DisplayText, DisplayColor);
        }
    }

    private static readonly Color ChromeBackground = Color.FromHex("#0d1a2c").WithAlpha(0.96f);
    private static readonly Color ChromeBorder = Color.FromHex("#2a4a70").WithAlpha(0.9f);

    private const float IdleScrollSpeed = 9f;

    private readonly DropdownButton _button;
    private readonly Popup _popup;
    private readonly BoxContainer _list;

    private readonly List<(string Text, int Id, DropdownButton Button)> _items = new();

    private int _selectedId = -1;

    private Texture? _scanlineTexture;
    private Font? _font;

    private ScrollingScanlineStyleBox _buttonStyle = null!;

    public NivalisDropdown()
    {
        Orientation = LayoutOrientation.Horizontal;
        HorizontalExpand = true;
        MinSize = new Vector2(150, 30);

        _button = new DropdownButton
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            MinSize = new Vector2(150, 30),
        };
        _buttonStyle = MakeChrome();
        _button.StyleBoxOverride = _buttonStyle;
        _button.DisplayColor = Color.FromHex("#EAF3FF");
        _button.OnPressed += _ => TogglePopup();
        AddChild(_button);

        _list = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
            Margin = new Thickness(4),
        };

        _popup = new Popup
        {
            Children = { _list },
        };
    }

    public Font? FontOverride
    {
        set
        {
            _font = value;
            _button.DisplayFont = value;
            foreach (var (_, _, button) in _items)
                button.DisplayFont = value;
        }
    }

    public Texture? ScanlineTexture
    {
        get => _scanlineTexture;
        set
        {
            _scanlineTexture = value;
            _buttonStyle.Texture = value;

            foreach (var (_, _, button) in _items)
                RefreshItemStyle(button, false);
        }
    }

    private ScrollingScanlineStyleBox MakeChrome()
    {
        return new ScrollingScanlineStyleBox
        {
            Texture = _scanlineTexture,
            BackgroundColor = ChromeBackground,
            BorderColor = ChromeBorder,
            BorderThickness = 1.25f,
            InwardLean = 0f,
            SlantPixels = 0f,
        };
    }

    public void Clear()
    {
        _items.Clear();
        _selectedId = -1;
        _button.DisplayText = string.Empty;
        _list.RemoveAllChildren();
    }

    public void AddItem(string text, int id)
    {
        var item = new DropdownButton
        {
            HorizontalExpand = true,
            MinSize = new Vector2(150, 26),
            DisplayText = text,
            DisplayFont = _font,
            DisplayColor = Color.FromHex("#EAF3FF"),
        };

        item.StyleBoxOverride = MakeItemStyle(false);
        item.OnMouseEntered += _ => RefreshItemStyle(item, true);
        item.OnMouseExited += _ => RefreshItemStyle(item, false);
        item.OnPressed += _ =>
        {
            SelectId(id);
            _popup.Close();
            OnItemSelected?.Invoke(new ItemEventArgs(id));
        };
        _list.AddChild(item);

        _items.Add((text, id, item));

        if (_selectedId == -1)
            SelectId(id);
    }

    private ScrollingScanlineStyleBox MakeItemStyle(bool hovered)
    {
        return new ScrollingScanlineStyleBox
        {
            Texture = _scanlineTexture,
            BackgroundColor = hovered ? Color.FromHex("#16273f").WithAlpha(0.98f) : ChromeBackground,
            BorderColor = hovered ? Color.FromHex("#3E7FB0") : ChromeBorder,
            BorderThickness = hovered ? 1.5f : 1f,
            InwardLean = 0f,
            SlantPixels = 0f,
        };
    }

    private void RefreshItemStyle(DropdownButton button, bool hovered)
    {
        button.StyleBoxOverride = MakeItemStyle(hovered);
    }

    public void SelectId(int id)
    {
        _selectedId = id;
        foreach (var (text, itemId, _) in _items)
        {
            if (itemId != id)
                continue;

            _button.DisplayText = text;
            return;
        }
    }

    public int SelectedId => _selectedId;

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _buttonStyle.ScrollOffset += args.DeltaSeconds * IdleScrollSpeed;
    }

    private void TogglePopup()
    {
        if (_popup.Visible)
        {
            _popup.Close();
            return;
        }

        if (_popup.Parent == null)
            UserInterfaceManager.ModalRoot.AddChild(_popup);

        _list.MaxSize = new Vector2(320, 320);
        _popup.Open(UIBox2.FromDimensions(
            _button.GlobalPosition + new Vector2(0, _button.Height + 2),
            new Vector2(Math.Max(_button.Width, 180), _list.DesiredSize.Y + 8)));
    }
}
