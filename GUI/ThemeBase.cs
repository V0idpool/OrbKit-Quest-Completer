#pragma warning disable WFO1000

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OrbKit_Quest_Completer.GUI
{
    // Name: Obsidian Blurple Theme
    // Target: .NET 10 / WinForms
    // Site: https://voidtechstudios.us/
    // Copyright © 2026 Void Tech Studios

    public abstract class VTSThemeContainer : ContainerControl
    {
        #region Initialization

        protected Graphics G = null!;
        protected Bitmap? B;

        public VTSThemeContainer()
        {
            SetStyle((ControlStyles)139270, true);

            _ImageSize = Size.Empty;
            Font = new Font("Segoe UI", 9f);

            MeasureBitmap = new Bitmap(1, 1);
            MeasureGraphics = Graphics.FromImage(MeasureBitmap);

            DrawRadialPath = new GraphicsPath();

            InvalidateCustimization();
        }

        protected sealed override void OnHandleCreated(EventArgs e)
        {
            if (DoneCreation)
                InitializeMessages();

            InvalidateCustimization();
            ColorHook();

            if (_LockWidth != 0)
                Width = _LockWidth;
            if (_LockHeight != 0)
                Height = _LockHeight;
            if (!_ControlMode)
                base.Dock = DockStyle.Fill;

            Transparent = _Transparent;
            if (_Transparent && _BackColor)
                BackColor = Color.Transparent;

            base.OnHandleCreated(e);
        }

        private bool DoneCreation;
        protected sealed override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);

            if (Parent is null)
                return;
            _IsParentForm = Parent is Form;

            if (!_ControlMode)
            {
                InitializeMessages();

                if (_IsParentForm && ParentForm is not null && !DesignMode)
                {
                    ParentForm.FormBorderStyle = _BorderStyle;
                    if (_TransparencyKey != Color.Empty)
                        ParentForm.TransparencyKey = _TransparencyKey;

                    ParentForm.Shown += FormShown;
                }

                Parent.BackColor = BackColor;
            }

            OnCreation();
            DoneCreation = true;
            InvalidateTimer();
        }

        #endregion

        private void DoAnimation(bool i)
        {
            OnAnimation();
            if (i)
                Invalidate();
        }

        protected sealed override void OnPaint(PaintEventArgs e)
        {
            if (Width == 0 || Height == 0)
                return;

            if (_Transparent && _ControlMode)
            {
                PaintHook();
                if (B is not null)
                    e.Graphics.DrawImage(B, 0, 0);
            }
            else
            {
                G = e.Graphics;
                PaintHook();
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            ThemeShare.RemoveAnimationCallback(DoAnimation);
            base.OnHandleDestroyed(e);
        }

        private bool HasShown;
        private void FormShown(object? sender, EventArgs e)
        {
            if (_ControlMode || HasShown || ParentForm is null)
                return;

            if (_StartPosition == FormStartPosition.CenterParent || _StartPosition == FormStartPosition.CenterScreen)
            {
                Rectangle sb = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
                Rectangle cb = ParentForm.Bounds;
                ParentForm.Location = new Point(sb.Width / 2 - cb.Width / 2, sb.Height / 2 - cb.Height / 2);
            }

            HasShown = true;
        }

        #region Size Handling

        private Rectangle Frame;
        protected sealed override void OnSizeChanged(EventArgs e)
        {
            if (_Movable && !_ControlMode)
            {
                Frame = new Rectangle(7, 7, Width - 14, _Header - 7);
            }

            InvalidateBitmap();
            Invalidate();

            base.OnSizeChanged(e);
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if (_LockWidth != 0)
                width = _LockWidth;
            if (_LockHeight != 0)
                height = _LockHeight;
            base.SetBoundsCore(x, y, width, height, specified);
        }

        #endregion

        #region State Handling

        protected MouseState State;
        private void SetState(MouseState current)
        {
            State = current;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!(_IsParentForm && ParentForm?.WindowState == FormWindowState.Maximized))
            {
                if (_Sizable && !_ControlMode)
                    InvalidateMouse();
            }

            base.OnMouseMove(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            SetState(Enabled ? MouseState.None : MouseState.Block);
            base.OnEnabledChanged(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            SetState(MouseState.Over);
            base.OnMouseEnter(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            SetState(MouseState.Over);
            base.OnMouseUp(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            SetState(MouseState.None);

            if (GetChildAtPoint(PointToClient(MousePosition)) is not null)
            {
                if (_Sizable && !_ControlMode)
                {
                    Cursor = Cursors.Default;
                    Previous = 0;
                }
            }

            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                SetState(MouseState.Down);

            if (!(_IsParentForm && ParentForm?.WindowState == FormWindowState.Maximized || _ControlMode))
            {
                if (_Movable && Frame.Contains(e.Location))
                {
                    Capture = false;
                    WM_LMBUTTONDOWN = true;
                    DefWndProc(ref Messages[0]);
                }
                else if (_Sizable && Previous != 0)
                {
                    Capture = false;
                    WM_LMBUTTONDOWN = true;
                    DefWndProc(ref Messages[Previous]);
                }
            }

            base.OnMouseDown(e);
        }

        private bool WM_LMBUTTONDOWN;
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (WM_LMBUTTONDOWN && m.Msg == 513)
            {
                WM_LMBUTTONDOWN = false;

                SetState(MouseState.Over);
                if (!_SmartBounds || Parent is null)
                    return;

                if (IsParentMdi && Parent.Parent is not null)
                {
                    CorrectBounds(new Rectangle(Point.Empty, Parent.Parent.Size));
                }
                else
                {
                    CorrectBounds(Screen.FromControl(Parent).WorkingArea);
                }
            }
        }

        private Point GetIndexPoint;
        private bool B1, B2, B3, B4;
        private int GetIndex()
        {
            GetIndexPoint = PointToClient(MousePosition);
            B1 = GetIndexPoint.X < 7;
            B2 = GetIndexPoint.X > Width - 7;
            B3 = GetIndexPoint.Y < 7;
            B4 = GetIndexPoint.Y > Height - 7;

            if (B1 && B3) return 4;
            if (B1 && B4) return 7;
            if (B2 && B3) return 5;
            if (B2 && B4) return 8;
            if (B1) return 1;
            if (B2) return 2;
            if (B3) return 3;
            if (B4) return 6;
            return 0;
        }

        private int Current, Previous;
        private void InvalidateMouse()
        {
            Current = GetIndex();
            if (Current == Previous)
                return;

            Previous = Current;
            Cursor = Previous switch
            {
                0 => Cursors.Default,
                1 or 2 => Cursors.SizeWE,
                3 or 6 => Cursors.SizeNS,
                4 or 8 => Cursors.SizeNWSE,
                5 or 7 => Cursors.SizeNESW,
                _ => Cursors.Default
            };
        }

        private readonly Message[] Messages = new Message[9];
        private void InitializeMessages()
        {
            if (Parent is null) return;
            Messages[0] = Message.Create(Parent.Handle, 161, new nint(2), nint.Zero);
            for (int i = 1; i <= 8; i++)
                Messages[i] = Message.Create(Parent.Handle, 161, new nint(i + 9), nint.Zero);
        }

        private void CorrectBounds(Rectangle bounds)
        {
            if (Parent is null) return;

            if (Parent.Width > bounds.Width)
                Parent.Width = bounds.Width;
            if (Parent.Height > bounds.Height)
                Parent.Height = bounds.Height;

            int x = Parent.Location.X;
            int y = Parent.Location.Y;

            if (x < bounds.X) x = bounds.X;
            if (y < bounds.Y) y = bounds.Y;

            int maxRight = bounds.X + bounds.Width;
            int maxBottom = bounds.Y + bounds.Height;

            if (x + Parent.Width > maxRight) x = maxRight - Parent.Width;
            if (y + Parent.Height > maxBottom) y = maxBottom - Parent.Height;

            Parent.Location = new Point(x, y);
        }

        #endregion

        #region Base Properties

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override DockStyle Dock
        {
            get => base.Dock;
            set
            {
                if (!_ControlMode) return;
                base.Dock = value;
            }
        }

        private bool _BackColor;
        [Category("Misc")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override Color BackColor
        {
            get => base.BackColor;
            set
            {
                if (value == base.BackColor) return;

                if (!IsHandleCreated && _ControlMode && value == Color.Transparent)
                {
                    _BackColor = true;
                    return;
                }

                base.BackColor = value;
                if (Parent is not null)
                {
                    if (!_ControlMode)
                        Parent.BackColor = value;
                    ColorHook();
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override Size MinimumSize
        {
            get => base.MinimumSize;
            set
            {
                base.MinimumSize = value;
                if (Parent is not null)
                    Parent.MinimumSize = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override Size MaximumSize
        {
            get => base.MaximumSize;
            set
            {
                base.MaximumSize = value;
                if (Parent is not null)
                    Parent.MaximumSize = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get => base.Text;
            set { base.Text = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override Font Font
        {
            get => base.Font;
            set { base.Font = value; Invalidate(); }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override Color ForeColor
        {
            get => Color.Empty;
            set { }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override Image? BackgroundImage
        {
            get => null;
            set { }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override ImageLayout BackgroundImageLayout
        {
            get => ImageLayout.None;
            set { }
        }

        #endregion

        #region Public Properties

        private bool _SmartBounds = true;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool SmartBounds
        {
            get => _SmartBounds;
            set => _SmartBounds = value;
        }

        private bool _Movable = true;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool Movable
        {
            get => _Movable;
            set => _Movable = value;
        }

        private bool _Sizable = true;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool Sizable
        {
            get => _Sizable;
            set => _Sizable = value;
        }

        private Color _TransparencyKey;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color TransparencyKey
        {
            get => (_IsParentForm && !_ControlMode && ParentForm is not null) ? ParentForm.TransparencyKey : _TransparencyKey;
            set
            {
                if (value == _TransparencyKey) return;
                _TransparencyKey = value;

                if (_IsParentForm && !_ControlMode && ParentForm is not null)
                {
                    ParentForm.TransparencyKey = value;
                    ColorHook();
                }
            }
        }

        private FormBorderStyle _BorderStyle;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public FormBorderStyle BorderStyle
        {
            get => (_IsParentForm && !_ControlMode && ParentForm is not null) ? ParentForm.FormBorderStyle : _BorderStyle;
            set
            {
                _BorderStyle = value;

                if (_IsParentForm && !_ControlMode && ParentForm is not null)
                {
                    ParentForm.FormBorderStyle = value;
                    if (value != FormBorderStyle.None)
                    {
                        Movable = false;
                        Sizable = false;
                    }
                }
            }
        }

        private FormStartPosition _StartPosition;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public FormStartPosition StartPosition
        {
            get => (_IsParentForm && !_ControlMode && ParentForm is not null) ? ParentForm.StartPosition : _StartPosition;
            set
            {
                _StartPosition = value;
                if (_IsParentForm && !_ControlMode && ParentForm is not null)
                    ParentForm.StartPosition = value;
            }
        }

        private bool _NoRounding;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool NoRounding
        {
            get => _NoRounding;
            set { _NoRounding = value; Invalidate(); }
        }

        private Image? _Image;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Image? Image
        {
            get => _Image;
            set
            {
                _ImageSize = value?.Size ?? Size.Empty;
                _Image = value;
                Invalidate();
            }
        }

        private readonly Dictionary<string, Color> Items = new();
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Bloom[] Colors
        {
            get
            {
                var list = new List<Bloom>();
                foreach (var kvp in Items)
                    list.Add(new Bloom(kvp.Key, kvp.Value));
                return list.ToArray();
            }
            set
            {
                foreach (Bloom b in value)
                {
                    if (Items.ContainsKey(b.Name))
                        Items[b.Name] = b.Value;
                }

                InvalidateCustimization();
                ColorHook();
                Invalidate();
            }
        }

        private string _Customization = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Customization
        {
            get => _Customization;
            set
            {
                if (value == _Customization) return;

                Bloom[] currentItems = Colors;
                try
                {
                    byte[] data = Convert.FromBase64String(value);
                    for (int i = 0; i < currentItems.Length; i++)
                        currentItems[i].Value = Color.FromArgb(BitConverter.ToInt32(data, i * 4));
                }
                catch
                {
                    return;
                }

                _Customization = value;
                Colors = currentItems;
                ColorHook();
                Invalidate();
            }
        }

        private bool _Transparent;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool Transparent
        {
            get => _Transparent;
            set
            {
                _Transparent = value;
                if (!(IsHandleCreated || _ControlMode))
                    return;

                if (!value && BackColor.A != 255)
                    throw new InvalidOperationException("Unable to change value to false while a transparent BackColor is in use.");

                SetStyle(ControlStyles.Opaque, !value);
                SetStyle(ControlStyles.SupportsTransparentBackColor, value);

                InvalidateBitmap();
                Invalidate();
            }
        }

        #endregion

        #region Private Properties

        private Size _ImageSize;
        protected Size ImageSize => _ImageSize;

        private bool _IsParentForm;
        protected bool IsParentForm => _IsParentForm;

        protected bool IsParentMdi => Parent?.Parent is not null;

        private int _LockWidth;
        protected int LockWidth
        {
            get => _LockWidth;
            set
            {
                _LockWidth = value;
                if (LockWidth != 0 && IsHandleCreated)
                    Width = LockWidth;
            }
        }

        private int _LockHeight;
        protected int LockHeight
        {
            get => _LockHeight;
            set
            {
                _LockHeight = value;
                if (LockHeight != 0 && IsHandleCreated)
                    Height = LockHeight;
            }
        }

        private int _Header = 34;
        protected int Header
        {
            get => _Header;
            set
            {
                _Header = value;
                if (!_ControlMode)
                {
                    Frame = new Rectangle(7, 7, Width - 14, value - 7);
                    Invalidate();
                }
            }
        }

        private bool _ControlMode;
        protected bool ControlMode
        {
            get => _ControlMode;
            set
            {
                _ControlMode = value;
                Transparent = _Transparent;
                if (_Transparent && _BackColor)
                    BackColor = Color.Transparent;

                InvalidateBitmap();
                Invalidate();
            }
        }

        private bool _IsAnimated;
        protected bool IsAnimated
        {
            get => _IsAnimated;
            set
            {
                _IsAnimated = value;
                InvalidateTimer();
            }
        }

        #endregion

        #region Property Helpers

        protected Pen GetPen(string name) => new(Items[name]);
        protected Pen GetPen(string name, float width) => new(Items[name], width);
        protected SolidBrush GetBrush(string name) => new(Items[name]);
        protected Color GetColor(string name) => Items[name];

        protected void SetColor(string name, Color value) => Items[name] = value;
        protected void SetColor(string name, byte r, byte g, byte b) => SetColor(name, Color.FromArgb(r, g, b));
        protected void SetColor(string name, byte a, byte r, byte g, byte b) => SetColor(name, Color.FromArgb(a, r, g, b));
        protected void SetColor(string name, byte a, Color value) => SetColor(name, Color.FromArgb(a, value));

        private void InvalidateBitmap()
        {
            B?.Dispose();
            if (_Transparent && _ControlMode)
            {
                if (Width == 0 || Height == 0) return;
                B = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
                G = Graphics.FromImage(B);
            }
            else
            {
                B = null;
            }
        }

        private void InvalidateCustimization()
        {
            using var ms = new MemoryStream(Items.Count * 4);
            foreach (Bloom b in Colors)
                ms.Write(BitConverter.GetBytes(b.Value.ToArgb()), 0, 4);
            _Customization = Convert.ToBase64String(ms.ToArray());
        }

        private void InvalidateTimer()
        {
            if (DesignMode || !DoneCreation) return;

            if (_IsAnimated)
                ThemeShare.AddAnimationCallback(DoAnimation);
            else
                ThemeShare.RemoveAnimationCallback(DoAnimation);
        }

        #endregion

        #region User Hooks

        protected abstract void ColorHook();
        protected abstract void PaintHook();
        protected virtual void OnCreation() { }
        protected virtual void OnAnimation() { }

        #endregion

        #region Offset & Center

        protected Rectangle Offset(Rectangle r, int amount) =>
            new(r.X + amount, r.Y + amount, r.Width - amount * 2, r.Height - amount * 2);

        protected Size Offset(Size s, int amount) =>
            new(s.Width + amount, s.Height + amount);

        protected Point Offset(Point p, int amount) =>
            new(p.X + amount, p.Y + amount);

        protected Point Center(Rectangle p, Rectangle c) =>
            new(p.Width / 2 - c.Width / 2 + p.X + c.X, p.Height / 2 - c.Height / 2 + p.Y + c.Y);

        protected Point Center(Rectangle p, Size c) =>
            new(p.Width / 2 - c.Width / 2 + p.X, p.Height / 2 - c.Height / 2 + p.Y);

        protected Point Center(Rectangle child) => Center(Width, Height, child.Width, child.Height);
        protected Point Center(Size child) => Center(Width, Height, child.Width, child.Height);
        protected Point Center(int childWidth, int childHeight) => Center(Width, Height, childWidth, childHeight);
        protected Point Center(Size p, Size c) => Center(p.Width, p.Height, c.Width, c.Height);
        protected Point Center(int pWidth, int pHeight, int cWidth, int cHeight) =>
            new(pWidth / 2 - cWidth / 2, pHeight / 2 - cHeight / 2);

        #endregion

        #region Measure

        private readonly Bitmap MeasureBitmap;
        private readonly Graphics MeasureGraphics;

        protected Size Measure()
        {
            lock (MeasureGraphics)
                return MeasureGraphics.MeasureString(Text, Font, Width).ToSize();
        }

        protected Size Measure(string text)
        {
            lock (MeasureGraphics)
                return MeasureGraphics.MeasureString(text, Font, Width).ToSize();
        }

        #endregion

        #region Drawing Helpers

        protected void DrawPixel(Color c1, int x, int y)
        {
            if (_Transparent && B is not null)
            {
                B.SetPixel(x, y, c1);
            }
            else
            {
                using SolidBrush brush = new(c1);
                G.FillRectangle(brush, x, y, 1, 1);
            }
        }

        protected void DrawCorners(Color c1, int offset) => DrawCorners(c1, 0, 0, Width, Height, offset);
        protected void DrawCorners(Color c1, Rectangle r1, int offset) => DrawCorners(c1, r1.X, r1.Y, r1.Width, r1.Height, offset);
        protected void DrawCorners(Color c1, int x, int y, int width, int height, int offset) =>
            DrawCorners(c1, x + offset, y + offset, width - offset * 2, height - offset * 2);

        protected void DrawCorners(Color c1) => DrawCorners(c1, 0, 0, Width, Height);
        protected void DrawCorners(Color c1, Rectangle r1) => DrawCorners(c1, r1.X, r1.Y, r1.Width, r1.Height);
        protected void DrawCorners(Color c1, int x, int y, int width, int height)
        {
            if (_NoRounding) return;

            if (_Transparent && B is not null)
            {
                B.SetPixel(x, y, c1);
                B.SetPixel(x + (width - 1), y, c1);
                B.SetPixel(x, y + (height - 1), c1);
                B.SetPixel(x + (width - 1), y + (height - 1), c1);
            }
            else
            {
                using SolidBrush brush = new(c1);
                G.FillRectangle(brush, x, y, 1, 1);
                G.FillRectangle(brush, x + (width - 1), y, 1, 1);
                G.FillRectangle(brush, x, y + (height - 1), 1, 1);
                G.FillRectangle(brush, x + (width - 1), y + (height - 1), 1, 1);
            }
        }

        protected void DrawBorders(Pen p1, int offset) => DrawBorders(p1, 0, 0, Width, Height, offset);
        protected void DrawBorders(Pen p1, Rectangle r, int offset) => DrawBorders(p1, r.X, r.Y, r.Width, r.Height, offset);
        protected void DrawBorders(Pen p1, int x, int y, int width, int height, int offset) =>
            DrawBorders(p1, x + offset, y + offset, width - offset * 2, height - offset * 2);

        protected void DrawBorders(Pen p1) => DrawBorders(p1, 0, 0, Width, Height);
        protected void DrawBorders(Pen p1, Rectangle r) => DrawBorders(p1, r.X, r.Y, r.Width, r.Height);
        protected void DrawBorders(Pen p1, int x, int y, int width, int height) =>
            G.DrawRectangle(p1, x, y, width - 1, height - 1);

        protected void DrawText(Brush b1, HorizontalAlignment a, int x, int y) => DrawText(b1, Text, a, x, y);
        protected void DrawText(Brush b1, string text, HorizontalAlignment a, int x, int y)
        {
            if (string.IsNullOrEmpty(text)) return;

            Size textSize = Measure(text);
            Point centerPt = new(Width / 2 - textSize.Width / 2, Header / 2 - textSize.Height / 2);

            switch (a)
            {
                case HorizontalAlignment.Left:
                    G.DrawString(text, Font, b1, x, centerPt.Y + y);
                    break;
                case HorizontalAlignment.Center:
                    G.DrawString(text, Font, b1, centerPt.X + x, centerPt.Y + y);
                    break;
                case HorizontalAlignment.Right:
                    G.DrawString(text, Font, b1, Width - textSize.Width - x, centerPt.Y + y);
                    break;
            }
        }

        protected void DrawText(Brush b1, Point p1)
        {
            if (!string.IsNullOrEmpty(Text)) G.DrawString(Text, Font, b1, p1);
        }

        protected void DrawText(Brush b1, int x, int y)
        {
            if (!string.IsNullOrEmpty(Text)) G.DrawString(Text, Font, b1, x, y);
        }

        protected void DrawImage(HorizontalAlignment a, int x, int y) => DrawImage(_Image, a, x, y);
        protected void DrawImage(Image? image, HorizontalAlignment a, int x, int y)
        {
            if (image is null) return;
            Point pt = new(Width / 2 - image.Width / 2, Header / 2 - image.Height / 2);

            switch (a)
            {
                case HorizontalAlignment.Left:
                    G.DrawImage(image, x, pt.Y + y, image.Width, image.Height);
                    break;
                case HorizontalAlignment.Center:
                    G.DrawImage(image, pt.X + x, pt.Y + y, image.Width, image.Height);
                    break;
                case HorizontalAlignment.Right:
                    G.DrawImage(image, Width - image.Width - x, pt.Y + y, image.Width, image.Height);
                    break;
            }
        }

        protected void DrawImage(Point p1) => DrawImage(_Image, p1.X, p1.Y);
        protected void DrawImage(int x, int y) => DrawImage(_Image, x, y);
        protected void DrawImage(Image? image, Point p1) => DrawImage(image, p1.X, p1.Y);
        protected void DrawImage(Image? image, int x, int y)
        {
            if (image is not null)
                G.DrawImage(image, x, y, image.Width, image.Height);
        }

        protected void DrawGradient(ColorBlend blend, int x, int y, int width, int height) =>
            DrawGradient(blend, new Rectangle(x, y, width, height));

        protected void DrawGradient(ColorBlend blend, int x, int y, int width, int height, float angle) =>
            DrawGradient(blend, new Rectangle(x, y, width, height), angle);

        protected void DrawGradient(ColorBlend blend, Rectangle r) => DrawGradient(blend, r, 90.0f);
        protected void DrawGradient(ColorBlend blend, Rectangle r, float angle)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using LinearGradientBrush brush = new(r, Color.Empty, Color.Empty, angle) { InterpolationColors = blend };
            G.FillRectangle(brush, r);
        }

        protected void DrawGradient(Color c1, Color c2, int x, int y, int width, int height) =>
            DrawGradient(c1, c2, new Rectangle(x, y, width, height));

        protected void DrawGradient(Color c1, Color c2, int x, int y, int width, int height, float angle) =>
            DrawGradient(c1, c2, new Rectangle(x, y, width, height), angle);

        protected void DrawGradient(Color c1, Color c2, Rectangle r) => DrawGradient(c1, c2, r, 90.0f);
        protected void DrawGradient(Color c1, Color c2, Rectangle r, float angle)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using LinearGradientBrush brush = new(r, c1, c2, angle);
            G.FillRectangle(brush, r);
        }

        private readonly GraphicsPath DrawRadialPath;

        public void DrawRadial(ColorBlend blend, int x, int y, int width, int height) =>
            DrawRadial(blend, new Rectangle(x, y, width, height), width / 2, height / 2);

        public void DrawRadial(ColorBlend blend, int x, int y, int width, int height, Point center) =>
            DrawRadial(blend, new Rectangle(x, y, width, height), center.X, center.Y);

        public void DrawRadial(ColorBlend blend, int x, int y, int width, int height, int cx, int cy) =>
            DrawRadial(blend, new Rectangle(x, y, width, height), cx, cy);

        public void DrawRadial(ColorBlend blend, Rectangle r) => DrawRadial(blend, r, r.Width / 2, r.Height / 2);
        public void DrawRadial(ColorBlend blend, Rectangle r, Point center) => DrawRadial(blend, r, center.X, center.Y);
        public void DrawRadial(ColorBlend blend, Rectangle r, int cx, int cy)
        {
            if (r.Width <= 1 || r.Height <= 1) return;
            DrawRadialPath.Reset();
            DrawRadialPath.AddEllipse(r.X, r.Y, r.Width - 1, r.Height - 1);

            using PathGradientBrush brush = new(DrawRadialPath)
            {
                CenterPoint = new Point(r.X + cx, r.Y + cy),
                InterpolationColors = blend
            };

            if (G.SmoothingMode == SmoothingMode.AntiAlias)
                G.FillEllipse(brush, r.X + 1, r.Y + 1, r.Width - 3, r.Height - 3);
            else
                G.FillEllipse(brush, r);
        }

        protected void DrawRadial(Color c1, Color c2, int x, int y, int width, int height) =>
            DrawRadial(c1, c2, new Rectangle(x, y, width, height));

        protected void DrawRadial(Color c1, Color c2, int x, int y, int width, int height, float angle) =>
            DrawRadial(c1, c2, new Rectangle(x, y, width, height), angle);

        protected void DrawRadial(Color c1, Color c2, Rectangle r) => DrawRadial(c1, c2, r, 90.0f);
        protected void DrawRadial(Color c1, Color c2, Rectangle r, float angle)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using LinearGradientBrush brush = new(r, c1, c2, angle);
            G.FillEllipse(brush, r);
        }

        public GraphicsPath CreateRound(int x, int y, int width, int height, int slope) =>
            ThemeModule.CreateRound(x, y, width, height, slope);

        public GraphicsPath CreateRound(Rectangle r, int slope) =>
            ThemeModule.CreateRound(r, slope);

        #endregion
    }

    public abstract class ThemeControl154 : Control
    {
        #region Initialization

        protected Graphics G = null!;
        protected Bitmap? B;

        public ThemeControl154()
        {
            SetStyle((ControlStyles)139270, true);

            _ImageSize = Size.Empty;
            Font = new Font("Segoe UI", 9f);

            MeasureBitmap = new Bitmap(1, 1);
            MeasureGraphics = Graphics.FromImage(MeasureBitmap);

            DrawRadialPath = new GraphicsPath();

            InvalidateCustimization();
        }

        protected sealed override void OnHandleCreated(EventArgs e)
        {
            InvalidateCustimization();
            ColorHook();

            if (_LockWidth != 0) Width = _LockWidth;
            if (_LockHeight != 0) Height = _LockHeight;

            Transparent = _Transparent;
            if (_Transparent && _BackColor)
                BackColor = Color.Transparent;

            base.OnHandleCreated(e);
        }

        private bool DoneCreation;
        protected sealed override void OnParentChanged(EventArgs e)
        {
            if (Parent is not null)
            {
                OnCreation();
                DoneCreation = true;
                InvalidateTimer();
            }

            base.OnParentChanged(e);
        }

        #endregion

        private void DoAnimation(bool i)
        {
            OnAnimation();
            if (i) Invalidate();
        }

        protected sealed override void OnPaint(PaintEventArgs e)
        {
            if (Width == 0 || Height == 0) return;

            if (_Transparent)
            {
                PaintHook();
                if (B is not null)
                    e.Graphics.DrawImage(B, 0, 0);
            }
            else
            {
                G = e.Graphics;
                PaintHook();
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            ThemeShare.RemoveAnimationCallback(DoAnimation);
            base.OnHandleDestroyed(e);
        }

        #region Size & State Handling

        protected sealed override void OnSizeChanged(EventArgs e)
        {
            if (_Transparent) InvalidateBitmap();
            Invalidate();
            base.OnSizeChanged(e);
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if (_LockWidth != 0) width = _LockWidth;
            if (_LockHeight != 0) height = _LockHeight;
            base.SetBoundsCore(x, y, width, height, specified);
        }

        private bool InPosition;
        protected override void OnMouseEnter(EventArgs e)
        {
            InPosition = true;
            SetState(MouseState.Over);
            base.OnMouseEnter(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (InPosition) SetState(MouseState.Over);
            base.OnMouseUp(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) SetState(MouseState.Down);
            base.OnMouseDown(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            InPosition = false;
            SetState(MouseState.None);
            base.OnMouseLeave(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            SetState(Enabled ? MouseState.None : MouseState.Block);
            base.OnEnabledChanged(e);
        }

        protected MouseState State;
        private void SetState(MouseState current)
        {
            State = current;
            Invalidate();
        }

        #endregion

        #region Properties

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override Color ForeColor
        {
            get => Color.Empty;
            set { }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override Image? BackgroundImage
        {
            get => null;
            set { }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override ImageLayout BackgroundImageLayout
        {
            get => ImageLayout.None;
            set { }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get => base.Text;
            set { base.Text = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override Font Font
        {
            get => base.Font;
            set { base.Font = value; Invalidate(); }
        }

        private bool _BackColor;
        [Category("Misc")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override Color BackColor
        {
            get => base.BackColor;
            set
            {
                if (!IsHandleCreated && value == Color.Transparent)
                {
                    _BackColor = true;
                    return;
                }

                base.BackColor = value;
                if (Parent is not null) ColorHook();
            }
        }

        private bool _NoRounding;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool NoRounding
        {
            get => _NoRounding;
            set { _NoRounding = value; Invalidate(); }
        }

        private Image? _Image;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Image? Image
        {
            get => _Image;
            set
            {
                _ImageSize = value?.Size ?? Size.Empty;
                _Image = value;
                Invalidate();
            }
        }

        private bool _Transparent;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool Transparent
        {
            get => _Transparent;
            set
            {
                _Transparent = value;
                if (!IsHandleCreated) return;

                if (!value && BackColor.A != 255)
                    throw new InvalidOperationException("Unable to change value to false while a transparent BackColor is in use.");

                SetStyle(ControlStyles.Opaque, !value);
                SetStyle(ControlStyles.SupportsTransparentBackColor, value);

                if (value) InvalidateBitmap();
                else B = null;
                Invalidate();
            }
        }

        private readonly Dictionary<string, Color> Items = new();
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Bloom[] Colors
        {
            get
            {
                var list = new List<Bloom>();
                foreach (var kvp in Items)
                    list.Add(new Bloom(kvp.Key, kvp.Value));
                return list.ToArray();
            }
            set
            {
                foreach (Bloom b in value)
                {
                    if (Items.ContainsKey(b.Name))
                        Items[b.Name] = b.Value;
                }

                InvalidateCustimization();
                ColorHook();
                Invalidate();
            }
        }

        private string _Customization = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Customization
        {
            get => _Customization;
            set
            {
                if (value == _Customization) return;

                Bloom[] currentItems = Colors;
                try
                {
                    byte[] data = Convert.FromBase64String(value);
                    for (int i = 0; i < currentItems.Length; i++)
                        currentItems[i].Value = Color.FromArgb(BitConverter.ToInt32(data, i * 4));
                }
                catch
                {
                    return;
                }

                _Customization = value;
                Colors = currentItems;
                ColorHook();
                Invalidate();
            }
        }

        private Size _ImageSize;
        protected Size ImageSize => _ImageSize;

        private int _LockWidth;
        protected int LockWidth
        {
            get => _LockWidth;
            set
            {
                _LockWidth = value;
                if (LockWidth != 0 && IsHandleCreated) Width = LockWidth;
            }
        }

        private int _LockHeight;
        protected int LockHeight
        {
            get => _LockHeight;
            set
            {
                _LockHeight = value;
                if (LockHeight != 0 && IsHandleCreated) Height = LockHeight;
            }
        }

        private bool _IsAnimated;
        protected bool IsAnimated
        {
            get => _IsAnimated;
            set { _IsAnimated = value; InvalidateTimer(); }
        }

        #endregion

        #region Helpers & Hooks

        protected Pen GetPen(string name) => new(Items[name]);
        protected Pen GetPen(string name, float width) => new(Items[name], width);
        protected SolidBrush GetBrush(string name) => new(Items[name]);
        protected Color GetColor(string name) => Items[name];

        protected void SetColor(string name, Color value) => Items[name] = value;
        protected void SetColor(string name, byte r, byte g, byte b) => SetColor(name, Color.FromArgb(r, g, b));
        protected void SetColor(string name, byte a, byte r, byte g, byte b) => SetColor(name, Color.FromArgb(a, r, g, b));
        protected void SetColor(string name, byte a, Color value) => SetColor(name, Color.FromArgb(a, value));

        private void InvalidateBitmap()
        {
            B?.Dispose();
            if (Width == 0 || Height == 0) return;
            B = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
            G = Graphics.FromImage(B);
        }

        private void InvalidateCustimization()
        {
            using var ms = new MemoryStream(Items.Count * 4);
            foreach (Bloom b in Colors)
                ms.Write(BitConverter.GetBytes(b.Value.ToArgb()), 0, 4);
            _Customization = Convert.ToBase64String(ms.ToArray());
        }

        private void InvalidateTimer()
        {
            if (DesignMode || !DoneCreation) return;

            if (_IsAnimated) ThemeShare.AddAnimationCallback(DoAnimation);
            else ThemeShare.RemoveAnimationCallback(DoAnimation);
        }

        protected abstract void ColorHook();
        protected abstract void PaintHook();
        protected virtual void OnCreation() { }
        protected virtual void OnAnimation() { }

        protected Rectangle Offset(Rectangle r, int amount) =>
            new(r.X + amount, r.Y + amount, r.Width - amount * 2, r.Height - amount * 2);

        protected Size Offset(Size s, int amount) => new(s.Width + amount, s.Height + amount);
        protected Point Offset(Point p, int amount) => new(p.X + amount, p.Y + amount);

        protected Point Center(Rectangle p, Rectangle c) =>
            new(p.Width / 2 - c.Width / 2 + p.X + c.X, p.Height / 2 - c.Height / 2 + p.Y + c.Y);

        protected Point Center(Rectangle p, Size c) =>
            new(p.Width / 2 - c.Width / 2 + p.X, p.Height / 2 - c.Height / 2 + p.Y);

        protected Point Center(Rectangle child) => Center(Width, Height, child.Width, child.Height);
        protected Point Center(Size child) => Center(Width, Height, child.Width, child.Height);
        protected Point Center(int childWidth, int childHeight) => Center(Width, Height, childWidth, childHeight);
        protected Point Center(Size p, Size c) => Center(p.Width, p.Height, c.Width, c.Height);
        protected Point Center(int pWidth, int pHeight, int cWidth, int cHeight) =>
            new(pWidth / 2 - cWidth / 2, pHeight / 2 - cHeight / 2);

        private readonly Bitmap MeasureBitmap;
        private readonly Graphics MeasureGraphics;

        protected Size Measure()
        {
            lock (MeasureGraphics)
                return MeasureGraphics.MeasureString(Text, Font, Width).ToSize();
        }

        protected Size Measure(string text)
        {
            lock (MeasureGraphics)
                return MeasureGraphics.MeasureString(text, Font, Width).ToSize();
        }

        protected void DrawPixel(Color c1, int x, int y)
        {
            if (_Transparent && B is not null)
                B.SetPixel(x, y, c1);
            else
            {
                using SolidBrush b = new(c1);
                G.FillRectangle(b, x, y, 1, 1);
            }
        }

        protected void DrawCorners(Color c1, int offset) => DrawCorners(c1, 0, 0, Width, Height, offset);
        protected void DrawCorners(Color c1, Rectangle r1, int offset) => DrawCorners(c1, r1.X, r1.Y, r1.Width, r1.Height, offset);
        protected void DrawCorners(Color c1, int x, int y, int width, int height, int offset) =>
            DrawCorners(c1, x + offset, y + offset, width - offset * 2, height - offset * 2);

        protected void DrawCorners(Color c1) => DrawCorners(c1, 0, 0, Width, Height);
        protected void DrawCorners(Color c1, Rectangle r1) => DrawCorners(c1, r1.X, r1.Y, r1.Width, r1.Height);
        protected void DrawCorners(Color c1, int x, int y, int width, int height)
        {
            if (_NoRounding) return;

            if (_Transparent && B is not null)
            {
                B.SetPixel(x, y, c1);
                B.SetPixel(x + (width - 1), y, c1);
                B.SetPixel(x, y + (height - 1), c1);
                B.SetPixel(x + (width - 1), y + (height - 1), c1);
            }
            else
            {
                using SolidBrush b = new(c1);
                G.FillRectangle(b, x, y, 1, 1);
                G.FillRectangle(b, x + (width - 1), y, 1, 1);
                G.FillRectangle(b, x, y + (height - 1), 1, 1);
                G.FillRectangle(b, x + (width - 1), y + (height - 1), 1, 1);
            }
        }

        protected void DrawBorders(Pen p1, int offset) => DrawBorders(p1, 0, 0, Width, Height, offset);
        protected void DrawBorders(Pen p1, Rectangle r, int offset) => DrawBorders(p1, r.X, r.Y, r.Width, r.Height, offset);
        protected void DrawBorders(Pen p1, int x, int y, int width, int height, int offset) =>
            DrawBorders(p1, x + offset, y + offset, width - offset * 2, height - offset * 2);

        protected void DrawBorders(Pen p1) => DrawBorders(p1, 0, 0, Width, Height);
        protected void DrawBorders(Pen p1, Rectangle r) => DrawBorders(p1, r.X, r.Y, r.Width, r.Height);
        protected void DrawBorders(Pen p1, int x, int y, int width, int height) =>
            G.DrawRectangle(p1, x, y, width - 1, height - 1);

        protected void DrawText(Brush b1, HorizontalAlignment a, int x, int y) => DrawText(b1, Text, a, x, y);
        protected void DrawText(Brush b1, string text, HorizontalAlignment a, int x, int y)
        {
            if (string.IsNullOrEmpty(text)) return;

            Size textSize = Measure(text);
            Point pt = Center(textSize);

            switch (a)
            {
                case HorizontalAlignment.Left:
                    G.DrawString(text, Font, b1, x, pt.Y + y);
                    break;
                case HorizontalAlignment.Center:
                    G.DrawString(text, Font, b1, pt.X + x, pt.Y + y);
                    break;
                case HorizontalAlignment.Right:
                    G.DrawString(text, Font, b1, Width - textSize.Width - x, pt.Y + y);
                    break;
            }
        }

        protected void DrawText(Brush b1, Point p1)
        {
            if (!string.IsNullOrEmpty(Text)) G.DrawString(Text, Font, b1, p1);
        }

        protected void DrawText(Brush b1, int x, int y)
        {
            if (!string.IsNullOrEmpty(Text)) G.DrawString(Text, Font, b1, x, y);
        }

        protected void DrawImage(HorizontalAlignment a, int x, int y) => DrawImage(_Image, a, x, y);
        protected void DrawImage(Image? image, HorizontalAlignment a, int x, int y)
        {
            if (image is null) return;
            Point pt = Center(image.Size);

            switch (a)
            {
                case HorizontalAlignment.Left:
                    G.DrawImage(image, x, pt.Y + y, image.Width, image.Height);
                    break;
                case HorizontalAlignment.Center:
                    G.DrawImage(image, pt.X + x, pt.Y + y, image.Width, image.Height);
                    break;
                case HorizontalAlignment.Right:
                    G.DrawImage(image, Width - image.Width - x, pt.Y + y, image.Width, image.Height);
                    break;
            }
        }

        protected void DrawImage(Point p1) => DrawImage(_Image, p1.X, p1.Y);
        protected void DrawImage(int x, int y) => DrawImage(_Image, x, y);
        protected void DrawImage(Image? image, Point p1) => DrawImage(image, p1.X, p1.Y);
        protected void DrawImage(Image? image, int x, int y)
        {
            if (image is not null)
                G.DrawImage(image, x, y, image.Width, image.Height);
        }

        protected void DrawGradient(ColorBlend blend, int x, int y, int width, int height) =>
            DrawGradient(blend, new Rectangle(x, y, width, height));

        protected void DrawGradient(ColorBlend blend, int x, int y, int width, int height, float angle) =>
            DrawGradient(blend, new Rectangle(x, y, width, height), angle);

        protected void DrawGradient(ColorBlend blend, Rectangle r) => DrawGradient(blend, r, 90.0f);
        protected void DrawGradient(ColorBlend blend, Rectangle r, float angle)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using LinearGradientBrush brush = new(r, Color.Empty, Color.Empty, angle) { InterpolationColors = blend };
            G.FillRectangle(brush, r);
        }

        protected void DrawGradient(Color c1, Color c2, int x, int y, int width, int height) =>
            DrawGradient(c1, c2, new Rectangle(x, y, width, height));

        protected void DrawGradient(Color c1, Color c2, int x, int y, int width, int height, float angle) =>
            DrawGradient(c1, c2, new Rectangle(x, y, width, height), angle);

        protected void DrawGradient(Color c1, Color c2, Rectangle r) => DrawGradient(c1, c2, r, 90.0f);
        protected void DrawGradient(Color c1, Color c2, Rectangle r, float angle)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using LinearGradientBrush brush = new(r, c1, c2, angle);
            G.FillRectangle(brush, r);
        }

        private readonly GraphicsPath DrawRadialPath;

        public void DrawRadial(ColorBlend blend, int x, int y, int width, int height) =>
            DrawRadial(blend, new Rectangle(x, y, width, height), width / 2, height / 2);

        public void DrawRadial(ColorBlend blend, int x, int y, int width, int height, Point center) =>
            DrawRadial(blend, new Rectangle(x, y, width, height), center.X, center.Y);

        public void DrawRadial(ColorBlend blend, int x, int y, int width, int height, int cx, int cy) =>
            DrawRadial(blend, new Rectangle(x, y, width, height), cx, cy);

        public void DrawRadial(ColorBlend blend, Rectangle r) => DrawRadial(blend, r, r.Width / 2, r.Height / 2);
        public void DrawRadial(ColorBlend blend, Rectangle r, Point center) => DrawRadial(blend, r, center.X, center.Y);
        public void DrawRadial(ColorBlend blend, Rectangle r, int cx, int cy)
        {
            if (r.Width <= 1 || r.Height <= 1) return;
            DrawRadialPath.Reset();
            DrawRadialPath.AddEllipse(r.X, r.Y, r.Width - 1, r.Height - 1);

            using PathGradientBrush brush = new(DrawRadialPath)
            {
                CenterPoint = new Point(r.X + cx, r.Y + cy),
                InterpolationColors = blend
            };

            if (G.SmoothingMode == SmoothingMode.AntiAlias)
                G.FillEllipse(brush, r.X + 1, r.Y + 1, r.Width - 3, r.Height - 3);
            else
                G.FillEllipse(brush, r);
        }

        protected void DrawRadial(Color c1, Color c2, int x, int y, int width, int height) =>
            DrawRadial(c1, c2, new Rectangle(x, y, width, height));

        protected void DrawRadial(Color c1, Color c2, int x, int y, int width, int height, float angle) =>
            DrawRadial(c1, c2, new Rectangle(x, y, width, height), angle);

        protected void DrawRadial(Color c1, Color c2, Rectangle r) => DrawRadial(c1, c2, r, 90.0f);
        protected void DrawRadial(Color c1, Color c2, Rectangle r, float angle)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using LinearGradientBrush brush = new(r, c1, c2, angle);
            G.FillEllipse(brush, r);
        }

        public GraphicsPath CreateRound(int x, int y, int width, int height, int slope) =>
            ThemeModule.CreateRound(x, y, width, height, slope);

        public GraphicsPath CreateRound(Rectangle r, int slope) =>
            ThemeModule.CreateRound(r, slope);

        #endregion
    }

    public static class ThemeShare
    {
        private static int Frames;
        private static bool Invalidate;
        public static readonly PrecisionTimer ThemeTimer = new();

        private const int FPS = 50;
        private const int Rate = 10;

        public delegate void AnimationDelegate(bool invalidate);
        private static readonly List<AnimationDelegate> Callbacks = new();

        private static void HandleCallbacks(nint state, bool reserve)
        {
            Invalidate = Frames >= FPS;
            if (Invalidate) Frames = 0;

            lock (Callbacks)
            {
                for (int i = 0; i < Callbacks.Count; i++)
                    Callbacks[i].Invoke(Invalidate);
            }

            Frames += Rate;
        }

        private static void InvalidateThemeTimer()
        {
            if (Callbacks.Count == 0)
                ThemeTimer.Delete();
            else
                ThemeTimer.Create(0U, Rate, HandleCallbacks);
        }

        public static void AddAnimationCallback(AnimationDelegate callback)
        {
            lock (Callbacks)
            {
                if (Callbacks.Contains(callback)) return;
                Callbacks.Add(callback);
                InvalidateThemeTimer();
            }
        }

        public static void RemoveAnimationCallback(AnimationDelegate callback)
        {
            lock (Callbacks)
            {
                if (!Callbacks.Contains(callback)) return;
                Callbacks.Remove(callback);
                InvalidateThemeTimer();
            }
        }
    }

    public enum MouseState : byte
    {
        None = 0,
        Over = 1,
        Down = 2,
        Block = 3
    }

    public struct Bloom
    {
        public string _Name;
        public readonly string Name => _Name;

        private Color _Value;
        public Color Value
        {
            readonly get => _Value;
            set => _Value = value;
        }

        public string ValueHex
        {
            readonly get => $"#{_Value.R:X2}{_Value.G:X2}{_Value.B:X2}";
            set
            {
                try { _Value = ColorTranslator.FromHtml(value); }
                catch { }
            }
        }

        public Bloom(string name, Color value)
        {
            _Name = name;
            _Value = value;
        }
    }

    public sealed class PrecisionTimer : IDisposable
    {
        private bool _Enabled;
        public bool Enabled => _Enabled;

        private nint Handle;
        private TimerDelegate? TimerCallback;

        [DllImport("kernel32.dll", EntryPoint = "CreateTimerQueueTimer")]
        private static extern bool CreateTimerQueueTimer(ref nint handle, nint queue, TimerDelegate callback, nint state, uint dueTime, uint period, uint flags);

        [DllImport("kernel32.dll", EntryPoint = "DeleteTimerQueueTimer")]
        private static extern bool DeleteTimerQueueTimer(nint queue, nint handle, nint callback);

        public delegate void TimerDelegate(nint r1, bool r2);

        public void Create(uint dueTime, uint period, TimerDelegate callback)
        {
            if (_Enabled) return;

            TimerCallback = callback;
            bool success = CreateTimerQueueTimer(ref Handle, nint.Zero, TimerCallback, nint.Zero, dueTime, period, 0U);
            if (!success) ThrowNewException("CreateTimerQueueTimer");
            _Enabled = success;
        }

        public void Delete()
        {
            if (!_Enabled) return;
            bool success = DeleteTimerQueueTimer(nint.Zero, Handle, nint.Zero);
            if (!success && Marshal.GetLastWin32Error() != 997)
                ThrowNewException("DeleteTimerQueueTimer");

            _Enabled = !success;
        }

        private static void ThrowNewException(string name) =>
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"{name} failed.");

        public void Dispose() => Delete();
    }

    public class FDThemeContainer : VTSThemeContainer
    {
        private string _subHeader = "DISCORD GAME PROCESS RUNNER";
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string SubHeader
        {
            get => _subHeader;
            set { _subHeader = value; Invalidate(); }
        }

        private int _headerIconSize = 28;
        [Category("Appearance Quest")]
        [Description("Pixel size (width & height) of the header icon drawn from the Image property.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int HeaderIconSize
        {
            get => _headerIconSize;
            set { _headerIconSize = Math.Clamp(value, 16, Header - 8); Invalidate(); }
        }

        private bool _showIconBackdrop;
        [Category("Appearance Quest")]
        [Description("Draws the rounded blurple-to-violet badge behind the header icon.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool ShowIconBackdrop
        {
            get => _showIconBackdrop;
            set { _showIconBackdrop = value; Invalidate(); }
        }

        public FDThemeContainer()
        {
            Header = 54;
            BackColor = FDPalette.BgDeep;
            Font = new Font("Segoe UI", 10.25f, FontStyle.Bold);
            Text = "QUEST COMPLETER";
            Padding = new Padding(14, 62, 14, 14);

            SetColor("Background", FDPalette.BgDeep);
            SetColor("Header", FDPalette.SurfaceDark);
            SetColor("Border", FDPalette.BorderSubtle);
            SetColor("Accent", FDPalette.AccentBlurple);
            SetColor("Text", FDPalette.TextPrimary);
        }

        protected override void ColorHook() { }

        protected override void PaintHook()
        {
            G.SmoothingMode = SmoothingMode.AntiAlias;
            G.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            G.InterpolationMode = InterpolationMode.HighQualityBicubic;
            G.PixelOffsetMode = PixelOffsetMode.HighQuality;
            G.Clear(GetColor("Background"));

            Rectangle headerRect = new(0, 0, Width, Header);
            using (SolidBrush hdrBg = new(GetColor("Header")))
            {
                G.FillRectangle(hdrBg, headerRect);
            }

            using (LinearGradientBrush topRibbon = new(new Rectangle(0, 0, Math.Max(1, Width), 2), FDPalette.AccentBlurple, FDPalette.AccentViolet, 0f))
            {
                G.FillRectangle(topRibbon, 0, 0, Width, 2);
            }

            using (Pen divider = new(GetColor("Border"), 1f))
            {
                G.DrawLine(divider, 0, Header - 1, Width, Header - 1);
            }

            int titleX = 16;

            if (Image is not null)
            {
                int iconSize = Math.Clamp(_headerIconSize, 16, Header - 8);
                int iconY = (Header - iconSize) / 2;
                Rectangle iconRect = new(16, iconY, iconSize, iconSize);

                if (_showIconBackdrop)
                {
                    using GraphicsPath badgePath = ThemeModule.GetResponsiveRoundedPath(iconRect, 8);
                    using LinearGradientBrush badgeBrush = new(iconRect, FDPalette.AccentBlurple, FDPalette.AccentViolet, 135f);
                    G.FillPath(badgeBrush, badgePath);

                    int pad = Math.Max(3, iconSize / 7);
                    Rectangle innerRect = new(iconRect.X + pad, iconRect.Y + pad, iconSize - pad * 2, iconSize - pad * 2);
                    G.DrawImage(Image, innerRect);
                }
                else
                {
                    G.DrawImage(Image, iconRect);
                }

                titleX = iconRect.Right + 10;
            }

            TextRenderer.DrawText(G, Text, Font, new Point(titleX, 10), GetColor("Text"));
            using (Font subFont = new("Segoe UI", 7.75f, FontStyle.Regular))
            {
                TextRenderer.DrawText(G, _subHeader, subFont, new Point(titleX + 1, 30), FDPalette.TextMuted);
            }

            using Pen borderPen = GetPen("Border");
            DrawBorders(borderPen);
        }
    }
}