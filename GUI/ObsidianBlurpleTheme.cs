#pragma warning disable WFO1000

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OrbKit_Quest_Completer.GUI
{
    // Name: Obsidian Blurple Theme
    // Target: .NET 10 / WinForms
    // Site: https://voidtechstudios.us/
    // Copyright © 2026 Void Tech Studios

    public static class FDPalette
    {
        public static readonly Color BgDeep = Color.FromArgb(13, 14, 20);
        public static readonly Color SurfaceDark = Color.FromArgb(21, 22, 32);
        public static readonly Color SurfaceCard = Color.FromArgb(29, 31, 45);
        public static readonly Color SurfaceHover = Color.FromArgb(46, 49, 88);
        public static readonly Color SurfaceInput = Color.FromArgb(17, 18, 26);
        public static readonly Color BorderSubtle = Color.FromArgb(46, 49, 68);
        public static readonly Color BorderFocus = Color.FromArgb(88, 101, 242);
        public static readonly Color AccentBrurple = Color.FromArgb(114, 128, 255);
        public static readonly Color AccentBlurple = Color.FromArgb(88, 101, 242);
        public static readonly Color AccentViolet = Color.FromArgb(147, 92, 246);
        public static readonly Color AccentOrange = Color.FromArgb(240, 178, 50);
        public static readonly Color AccentCrimson = Color.FromArgb(237, 66, 69);
        public static readonly Color HudGreen = Color.FromArgb(35, 165, 90);
        public static readonly Color TextPrimary = Color.FromArgb(242, 243, 245);
        public static readonly Color TextSecondary = Color.FromArgb(181, 186, 193);
        public static readonly Color TextMuted = Color.FromArgb(120, 125, 138);
    }

    public abstract class ThemeModule : ContainerControl
    {
        internal static Graphics? G;
        public static readonly Bitmap TextBitmap = new(1, 1);
        public static readonly Graphics TextGraphics = Graphics.FromImage(TextBitmap);

        public static GraphicsPath GetResponsiveRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new();
            if (rect.Width <= 0 || rect.Height <= 0)
                return path;

            int safeRadius = Math.Max(1, Math.Min(radius, Math.Min(rect.Width / 2, rect.Height / 2)));
            int curveSize = safeRadius * 2;

            path.AddArc(rect.X, rect.Y, curveSize, curveSize, 180f, 90f);
            path.AddArc(rect.Right - curveSize, rect.Y, curveSize, curveSize, 270f, 90f);
            path.AddArc(rect.Right - curveSize, rect.Bottom - curveSize, curveSize, curveSize, 0f, 90f);
            path.AddArc(rect.X, rect.Bottom - curveSize, curveSize, curveSize, 90f, 90f);
            path.CloseFigure();

            return path;
        }

        internal static SizeF MeasureString(string text, Font font) =>
            TextGraphics.MeasureString(text, font);

        internal static SizeF MeasureString(string text, Font font, int width) =>
            TextGraphics.MeasureString(text, font, width, StringFormat.GenericTypographic);

        internal static GraphicsPath CreateRound(int x, int y, int width, int height, int slope) =>
            CreateRound(new Rectangle(x, y, width, height), slope);

        internal static GraphicsPath CreateRound(Rectangle r, int slope)
        {
            GraphicsPath path = new(FillMode.Winding);
            if (r.Width <= 0 || r.Height <= 0) return path;

            int safeSlope = Math.Max(1, Math.Min(slope, Math.Min(r.Width, r.Height)));
            path.AddArc(r.X, r.Y, safeSlope, safeSlope, 180.0f, 90.0f);
            path.AddArc(r.Right - safeSlope, r.Y, safeSlope, safeSlope, 270.0f, 90.0f);
            path.AddArc(r.Right - safeSlope, r.Bottom - safeSlope, safeSlope, safeSlope, 0.0f, 90.0f);
            path.AddArc(r.X, r.Bottom - safeSlope, safeSlope, safeSlope, 90.0f, 90.0f);
            path.CloseFigure();
            return path;
        }
    }

    public class ColorTable : ProfessionalColorTable
    {
        private readonly Color _backColor = FDPalette.SurfaceCard;
        private readonly Color _hoverColor = FDPalette.SurfaceHover;
        private readonly Color _borderColor = FDPalette.BorderSubtle;

        public override Color ToolStripDropDownBackground => _backColor;
        public override Color ImageMarginGradientBegin => _backColor;
        public override Color ImageMarginGradientMiddle => _backColor;
        public override Color ImageMarginGradientEnd => _backColor;
        public override Color MenuItemSelected => _hoverColor;
        public override Color MenuItemBorder => FDPalette.AccentBlurple;
        public override Color MenuItemPressedGradientBegin => _backColor;
        public override Color MenuItemPressedGradientEnd => _backColor;
        public override Color MenuItemPressedGradientMiddle => _backColor;
        public override Color MenuBorder => _borderColor;
        public override Color SeparatorDark => _borderColor;
        public override Color SeparatorLight => _backColor;
    }

    [DefaultEvent("Click")]
    public class FDActionCard : Control
    {
        private string _title = "Quest Action";
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Title { get => _title; set { _title = value; Invalidate(); } }

        private string _subtitle = "Configure session details and run game.";
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Subtitle { get => _subtitle; set { _subtitle = value; Invalidate(); } }

        private Image? _icon;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Image? Icon { get => _icon; set { _icon = value; Invalidate(); } }

        private Color _accentColor = FDPalette.AccentBlurple;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color AccentColor { get => _accentColor; set { _accentColor = value; Invalidate(); } }

        private bool _isSelected;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool Selected { get => _isSelected; set { _isSelected = value; Invalidate(); } }

        private bool _isHovered;

        public FDActionCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(260, 80);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 6 || Height <= 6) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new(0, 0, Width - 1, Height - 1);
            Color fill = _isSelected ? Color.FromArgb(35, 37, 64) : (_isHovered ? FDPalette.SurfaceHover : FDPalette.SurfaceCard);
            Color border = _isSelected ? _accentColor : (_isHovered ? Color.FromArgb(140, _accentColor) : FDPalette.BorderSubtle);

            using (GraphicsPath path = ThemeModule.GetResponsiveRoundedPath(rect, 12))
            using (SolidBrush brush = new(fill))
            using (Pen pen = new(border, _isSelected ? 1.5f : 1f))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            int badgeSize = (int)(Height * 0.6f);
            int pad = (Height - badgeSize) / 2;
            Rectangle badgeRect = new(pad, pad, badgeSize, badgeSize);

            using (GraphicsPath bPath = ThemeModule.GetResponsiveRoundedPath(badgeRect, badgeSize / 2))
            using (SolidBrush bBrush = new(Color.FromArgb(40, _accentColor)))
            {
                g.FillPath(bBrush, bPath);
            }

            if (_icon != null)
            {
                int imgSz = (int)(badgeSize * 0.65f);
                int off = (badgeSize - imgSz) / 2;
                g.DrawImage(_icon, new Rectangle(badgeRect.X + off, badgeRect.Y + off, imgSz, imgSz));
            }
            else
            {
                using Font glyphFont = new("Segoe UI Symbol", badgeSize * 0.34f, FontStyle.Bold);
                TextRenderer.DrawText(g, "◆", glyphFont, badgeRect, _accentColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            int textX = badgeRect.Right + 12;
            int textW = Width - textX - 12;
            if (textW <= 10) return;

            using Font titleFont = new("Segoe UI", 9.5f, FontStyle.Bold);
            using Font subFont = new("Segoe UI", 8.25f, FontStyle.Regular);
            TextRenderer.DrawText(g, _title, titleFont, new Rectangle(textX, pad, textW, 20), FDPalette.TextPrimary, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, _subtitle, subFont, new Rectangle(textX, pad + 20, textW, Height - pad - 24), FDPalette.TextSecondary, TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }
    }

    public class FDListView : ListView
    {
        private const int LVM_FIRST = 0x1000;
        private const int LVM_GETHEADER = LVM_FIRST + 31;
        private const int LVM_SCROLL = LVM_FIRST + 20;
        private const int SB_HORZ = 0;
        private const int SB_VERT = 1;
        private const int GWL_STYLE = -16;
        private const int WS_HSCROLL = 0x00100000;
        private const int WS_VSCROLL = 0x00200000;
        private const int WM_PAINT = 0x000F;
        private const int WM_SIZE = 0x0005;
        private const int WM_WINDOWPOSCHANGED = 0x0047;
        private const int WM_HSCROLL = 0x0114;
        private const int WM_VSCROLL = 0x0115;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E;
        private const int WM_KEYDOWN = 0x0100;
        private const uint SIF_ALL = 0x17;

        [StructLayout(LayoutKind.Sequential)]
        private struct SCROLLINFO
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(nint hWnd, string pszSubAppName, string? pszSubIdList);

        [DllImport("user32.dll")]
        private static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);

        [DllImport("user32.dll")]
        private static extern bool GetScrollInfo(nint hWnd, int nBar, ref SCROLLINFO lpScrollInfo);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(nint hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

        private DarkHeaderSubClass? _headerSubClass;
        private FDScrollBarOverlay? _vScrollOverlay;
        private FDScrollBarOverlay? _hScrollOverlay;
        private bool _fittingColumns;

        private int _lastVPos = -1, _lastVMax = -1, _lastVPage = -1;
        private bool _lastVVisible;
        private int _lastHPos = -1, _lastHMax = -1, _lastHPage = -1;
        private bool _lastHVisible;

        public FDListView()
        {
            DoubleBuffered = true;
            OwnerDraw = true;
            FullRowSelect = true;
            View = View.Details;
            BackColor = FDPalette.SurfaceInput;
            ForeColor = FDPalette.TextPrimary;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SetWindowTheme(Handle, "DarkMode_Explorer", null);

            nint headerHandle = SendMessage(Handle, LVM_GETHEADER, nint.Zero, nint.Zero);
            if (headerHandle != nint.Zero)
            {
                _headerSubClass?.ReleaseHandle();
                _headerSubClass = new DarkHeaderSubClass(headerHandle);
            }

            if (!DesignMode)
            {
                EnsureOverlays();
                FitLastColumn();
                SyncOverlays(forceRepaint: true);
            }
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (!DesignMode && IsHandleCreated)
            {
                EnsureOverlays();
                SyncOverlays(forceRepaint: true);
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible)
            {
                if (_vScrollOverlay != null) _vScrollOverlay.Visible = false;
                if (_hScrollOverlay != null) _hScrollOverlay.Visible = false;
            }
            else if (!DesignMode && IsHandleCreated)
            {
                SyncOverlays(forceRepaint: true);
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (!DesignMode && IsHandleCreated)
            {
                FitLastColumn();
                SyncOverlays(forceRepaint: true);
            }
        }

        protected override void OnColumnWidthChanged(ColumnWidthChangedEventArgs e)
        {
            base.OnColumnWidthChanged(e);
            if (!DesignMode && IsHandleCreated && !_fittingColumns)
                SyncOverlays(forceRepaint: true);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (!DesignMode && IsHandleCreated &&
                m.Msg is WM_VSCROLL or WM_HSCROLL or WM_MOUSEWHEEL or WM_MOUSEHWHEEL or WM_KEYDOWN or WM_SIZE or WM_WINDOWPOSCHANGED or WM_PAINT)
            {
                SyncOverlays(forceRepaint: m.Msg is WM_SIZE or WM_WINDOWPOSCHANGED);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _headerSubClass?.ReleaseHandle();
                _headerSubClass = null;

                DisposeOverlay(ref _vScrollOverlay);
                DisposeOverlay(ref _hScrollOverlay);
            }
            base.Dispose(disposing);
        }

        private static void DisposeOverlay(ref FDScrollBarOverlay? overlay)
        {
            if (overlay == null) return;
            overlay.Parent?.Controls.Remove(overlay);
            overlay.Dispose();
            overlay = null;
        }

        private void EnsureOverlays()
        {
            if (Parent == null) return;
            EnsureSingleOverlay(ref _vScrollOverlay, isHorizontal: false);
            EnsureSingleOverlay(ref _hScrollOverlay, isHorizontal: true);
        }

        private void EnsureSingleOverlay(ref FDScrollBarOverlay? overlay, bool isHorizontal)
        {
            if (Parent == null) return;

            if (overlay == null)
            {
                overlay = new FDScrollBarOverlay(this, isHorizontal);
                Parent.Controls.Add(overlay);
                overlay.BringToFront();
            }
            else if (overlay.Parent != Parent)
            {
                overlay.Parent?.Controls.Remove(overlay);
                Parent.Controls.Add(overlay);
                overlay.BringToFront();
            }
        }

        private void FitLastColumn()
        {
            if (_fittingColumns || Columns.Count == 0 || ClientSize.Width <= 0) return;

            _fittingColumns = true;
            try
            {
                int used = 0;
                for (int i = 0; i < Columns.Count - 1; i++)
                    used += Columns[i].Width;

                int target = Math.Max(90, ClientSize.Width - used);
                if (Math.Abs(Columns[^1].Width - target) > 1)
                    Columns[^1].Width = target;
            }
            finally
            {
                _fittingColumns = false;
            }
        }

        private int GetHeaderHeight()
        {
            nint headerHandle = SendMessage(Handle, LVM_GETHEADER, nint.Zero, nint.Zero);
            if (headerHandle != nint.Zero && GetWindowRect(headerHandle, out RECT rc))
                return Math.Max(20, rc.Bottom - rc.Top);
            return 25;
        }

        internal void ScrollToRow(int targetPos)
        {
            if (!IsHandleCreated || Items.Count == 0) return;

            var si = new SCROLLINFO { cbSize = (uint)Marshal.SizeOf<SCROLLINFO>(), fMask = SIF_ALL };
            if (!GetScrollInfo(Handle, SB_VERT, ref si)) return;

            int maxPos = Math.Max(0, si.nMax - (int)si.nPage + 1);
            int clamped = Math.Clamp(targetPos, 0, maxPos);
            int deltaRows = clamped - si.nPos;
            if (deltaRows == 0) return;

            int rowHeight = Items[0].Bounds.Height;
            if (rowHeight <= 0) rowHeight = 20;

            SendMessage(Handle, LVM_SCROLL, nint.Zero, deltaRows * rowHeight);
            SyncOverlays(forceRepaint: true);
        }

        internal void ScrollToHorizontal(int targetPos)
        {
            if (!IsHandleCreated) return;

            var si = new SCROLLINFO { cbSize = (uint)Marshal.SizeOf<SCROLLINFO>(), fMask = SIF_ALL };
            if (!GetScrollInfo(Handle, SB_HORZ, ref si)) return;

            int maxPos = Math.Max(0, si.nMax - (int)si.nPage + 1);
            int clamped = Math.Clamp(targetPos, 0, maxPos);
            int deltaPx = clamped - si.nPos;
            if (deltaPx == 0) return;

            SendMessage(Handle, LVM_SCROLL, deltaPx, nint.Zero);
            SyncOverlays(forceRepaint: true);
        }

        internal void ForwardWheel(int delta)
        {
            if (!IsHandleCreated) return;
            SendMessage(Handle, WM_MOUSEWHEEL, (nint)(delta << 16), nint.Zero);
            SyncOverlays(forceRepaint: true);
        }

        private void SyncOverlays(bool forceRepaint)
        {
            if (_vScrollOverlay == null || _hScrollOverlay == null || Parent == null) return;

            int style = GetWindowLong(Handle, GWL_STYLE);
            int sbW = SystemInformation.VerticalScrollBarWidth;
            int sbH = SystemInformation.HorizontalScrollBarHeight;
            int border = BorderStyle == BorderStyle.FixedSingle ? 1 : 0;
            bool hasV = (style & WS_VSCROLL) != 0;
            var vSi = new SCROLLINFO { cbSize = (uint)Marshal.SizeOf<SCROLLINFO>(), fMask = SIF_ALL };
            bool gotV = hasV && GetScrollInfo(Handle, SB_VERT, ref vSi);
            int maxV = gotV ? Math.Max(0, vSi.nMax - (int)vSi.nPage + 1) : 0;
            bool showV = Visible && gotV && maxV > 0;
            bool hasH = (style & WS_HSCROLL) != 0;
            var hSi = new SCROLLINFO { cbSize = (uint)Marshal.SizeOf<SCROLLINFO>(), fMask = SIF_ALL };
            bool gotH = hasH && GetScrollInfo(Handle, SB_HORZ, ref hSi);
            int maxH = gotH ? Math.Max(0, hSi.nMax - (int)hSi.nPage + 1) : 0;
            bool showH = Visible && gotH && maxH > 0;

            bool vChanged = forceRepaint || showV != _lastVVisible || showH != _lastHVisible ||
                            vSi.nPos != _lastVPos || vSi.nMax != _lastVMax || (int)vSi.nPage != _lastVPage;

            bool hChanged = forceRepaint || showH != _lastHVisible || showV != _lastVVisible ||
                            hSi.nPos != _lastHPos || hSi.nMax != _lastHMax || (int)hSi.nPage != _lastHPage;

            _lastVVisible = showV; _lastVPos = vSi.nPos; _lastVMax = vSi.nMax; _lastVPage = (int)vSi.nPage;
            _lastHVisible = showH; _lastHPos = hSi.nPos; _lastHMax = hSi.nMax; _lastHPage = (int)hSi.nPage;

            if (vChanged)
            {
                if (!showV)
                {
                    if (_vScrollOverlay.Visible) _vScrollOverlay.Visible = false;
                }
                else
                {
                    int headerH = GetHeaderHeight();
                    int bottomTrim = showH ? sbH : 0;
                    Rectangle vBounds = new(
                        Right - sbW - border,
                        Top + headerH + border,
                        sbW,
                        Math.Max(0, Height - headerH - border * 2 - bottomTrim));

                    if (_vScrollOverlay.Bounds != vBounds)
                        _vScrollOverlay.Bounds = vBounds;

                    _vScrollOverlay.UpdateMetrics(vSi.nPos, maxV, (int)vSi.nPage, vSi.nMax + 1);
                    if (!_vScrollOverlay.Visible) _vScrollOverlay.Visible = true;
                    _vScrollOverlay.BringToFront();
                }
            }

            if (hChanged)
            {
                if (!showH)
                {
                    if (_hScrollOverlay.Visible) _hScrollOverlay.Visible = false;
                }
                else
                {
                    Rectangle hBounds = new(
                        Left + border,
                        Bottom - sbH - border,
                        Math.Max(0, Width - border * 2),
                        sbH);

                    if (_hScrollOverlay.Bounds != hBounds)
                        _hScrollOverlay.Bounds = hBounds;

                    _hScrollOverlay.UpdateMetrics(hSi.nPos, maxH, (int)hSi.nPage, hSi.nMax + 1, endPadding: showV ? sbW : 0);
                    if (!_hScrollOverlay.Visible) _hScrollOverlay.Visible = true;
                    _hScrollOverlay.BringToFront();
                }
            }
        }

        private sealed class DarkHeaderSubClass : NativeWindow
        {
            private const int WM_PAINT = 0x000F;
            private const int WM_ERASEBKGND = 0x0014;
            private const int HDM_GETITEMCOUNT = 0x1200;
            private const int HDM_GETITEMRECT = 0x1207;

            [DllImport("user32.dll")]
            private static extern bool GetClientRect(nint hWnd, out RECT lpRect);

            [DllImport("user32.dll")]
            private static extern nint SendMessage(nint hWnd, int msg, nint wParam, ref RECT lParam);

            public DarkHeaderSubClass(nint headerHandle) => AssignHandle(headerHandle);

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_ERASEBKGND && m.WParam != nint.Zero)
                {
                    if (GetClientRect(Handle, out RECT rc))
                    {
                        using Graphics g = Graphics.FromHdc(m.WParam);
                        FillHeaderRect(g, 0, rc.Right - rc.Left, rc.Bottom - rc.Top);
                    }
                    m.Result = (nint)1;
                    return;
                }

                base.WndProc(ref m);

                if (m.Msg == WM_PAINT)
                    PaintEmptyTail();
            }

            private void PaintEmptyTail()
            {
                if (!GetClientRect(Handle, out RECT rc)) return;

                int headerW = rc.Right - rc.Left;
                int headerH = rc.Bottom - rc.Top;
                if (headerW <= 0 || headerH <= 0) return;

                int count = (int)FDListView.SendMessage(Handle, HDM_GETITEMCOUNT, nint.Zero, nint.Zero);
                int lastRight = 0;

                for (int i = 0; i < count; i++)
                {
                    if (SendMessage(Handle, HDM_GETITEMRECT, i, ref rc) != nint.Zero)
                        lastRight = Math.Max(lastRight, rc.Right);
                }

                if (lastRight < headerW)
                {
                    using Graphics g = Graphics.FromHwnd(Handle);
                    FillHeaderRect(g, lastRight, headerW - lastRight, headerH);
                }
            }

            private static void FillHeaderRect(Graphics g, int x, int width, int height)
            {
                using SolidBrush bg = new(FDPalette.SurfaceCard);
                using Pen border = new(FDPalette.BorderSubtle, 1f);
                g.FillRectangle(bg, x, 0, width, height);
                g.DrawLine(border, x, height - 1, x + width, height - 1);
            }
        }

        private sealed class FDScrollBarOverlay : Control
        {
            private readonly FDListView _owner;
            private readonly bool _isHorizontal;
            private int _pos;
            private int _maxPos;
            private int _page;
            private int _total;
            private int _endPadding;
            private bool _hoverThumb;
            private bool _dragging;
            private int _dragStartCoord;
            private int _dragStartPos;
            private Rectangle _trackRect;
            private Rectangle _thumbRect;

            public FDScrollBarOverlay(FDListView owner, bool isHorizontal)
            {
                _owner = owner;
                _isHorizontal = isHorizontal;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                SetStyle(ControlStyles.Selectable, false);
                BackColor = FDPalette.SurfaceInput;
                Visible = false;
            }

            public void UpdateMetrics(int pos, int maxPos, int page, int total, int endPadding = 0)
            {
                _pos = pos;
                _maxPos = Math.Max(1, maxPos);
                _page = Math.Max(1, page);
                _total = Math.Max(1, total);
                _endPadding = Math.Max(0, endPadding);
                Invalidate();
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                if (_isHorizontal)
                    _owner.ScrollToHorizontal(_pos + (e.Delta > 0 ? -48 : 48));
                else
                    _owner.ForwardWheel(e.Delta);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button != MouseButtons.Left) return;

                if (_thumbRect.Contains(e.Location))
                {
                    _dragging = true;
                    _dragStartCoord = _isHorizontal ? e.X : e.Y;
                    _dragStartPos = _pos;
                    Invalidate();
                }
                else if (_isHorizontal && _trackRect.Width > 0 && e.X <= _trackRect.Right)
                {
                    float ratio = Math.Clamp((e.X - _trackRect.X) / (float)_trackRect.Width, 0f, 1f);
                    _owner.ScrollToHorizontal((int)Math.Round(ratio * _maxPos));
                }
                else if (!_isHorizontal && _trackRect.Height > 0)
                {
                    float ratio = Math.Clamp((e.Y - _trackRect.Y) / (float)_trackRect.Height, 0f, 1f);
                    _owner.ScrollToRow((int)Math.Round(ratio * _maxPos));
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (_dragging)
                {
                    if (_isHorizontal)
                    {
                        int travel = Math.Max(1, _trackRect.Width - _thumbRect.Width);
                        int dx = e.X - _dragStartCoord;
                        _owner.ScrollToHorizontal(_dragStartPos + (int)Math.Round((dx / (double)travel) * _maxPos));
                    }
                    else
                    {
                        int travel = Math.Max(1, _trackRect.Height - _thumbRect.Height);
                        int dy = e.Y - _dragStartCoord;
                        _owner.ScrollToRow(_dragStartPos + (int)Math.Round((dy / (double)travel) * _maxPos));
                    }
                    return;
                }

                bool over = _thumbRect.Contains(e.Location);
                if (over != _hoverThumb)
                {
                    _hoverThumb = over;
                    Cursor = over ? Cursors.Hand : Cursors.Default;
                    Invalidate();
                }
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (_dragging) { _dragging = false; Invalidate(); }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                if (_hoverThumb && !_dragging) { _hoverThumb = false; Cursor = Cursors.Default; Invalidate(); }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(FDPalette.SurfaceInput);

                float viewRatio = Math.Clamp(_page / (float)_total, 0.08f, 1f);

                if (_isHorizontal)
                {
                    int availW = Width - 12 - _endPadding;
                    if (availW <= 16) return;

                    _trackRect = new Rectangle(6, (Height - 4) / 2, availW, 4);
                    int thumbW = Math.Max(22, (int)(_trackRect.Width * viewRatio));
                    int maxTravel = Math.Max(1, _trackRect.Width - thumbW);
                    int thumbX = _trackRect.X + (int)((_pos / (float)_maxPos) * maxTravel);
                    _thumbRect = new Rectangle(thumbX, _trackRect.Y, thumbW, _trackRect.Height);
                }
                else
                {
                    if (Height <= 16) return;

                    _trackRect = new Rectangle((Width - 4) / 2, 6, 4, Height - 12);
                    int thumbH = Math.Max(22, (int)(_trackRect.Height * viewRatio));
                    int maxTravel = Math.Max(1, _trackRect.Height - thumbH);
                    int thumbY = _trackRect.Y + (int)((_pos / (float)_maxPos) * maxTravel);
                    _thumbRect = new Rectangle(_trackRect.X, thumbY, _trackRect.Width, thumbH);
                }

                Color thumbCol = (_dragging || _hoverThumb) ? FDPalette.AccentBlurple : FDPalette.BorderSubtle;
                using GraphicsPath thumbPath = ThemeModule.GetResponsiveRoundedPath(_thumbRect, 2);
                using SolidBrush thumbBrush = new(thumbCol);
                g.FillPath(thumbBrush, thumbPath);
            }
        }
    }

    public class FDConsoleBox : Control
    {
        private readonly List<(string Timestamp, string Message, Color Color)> _entries = [];
        private const int MaxEntries = 500;

        private int _scrollOffset;
        private int _maxScroll;
        private bool _autoScroll = true;
        private bool _isDraggingThumb;
        private int _dragStartY;
        private int _dragStartScroll;
        private Rectangle _thumbRect;
        private Rectangle _trackRect;
        private bool _hoverThumb;

        public FDConsoleBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            ForeColor = FDPalette.TextPrimary;
            Font = new Font("Consolas", 8.5f, FontStyle.Regular);
            Size = new Size(318, 140);
        }

        public void AppendLog(string timestamp, string message, Color color)
        {
            bool wasAtBottom = _autoScroll || (_maxScroll - _scrollOffset <= 12);

            _entries.Add((timestamp, message, color));
            if (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);

            _autoScroll = wasAtBottom;
            Invalidate();
        }

        public void Clear()
        {
            _entries.Clear();
            _scrollOffset = 0;
            _maxScroll = 0;
            _autoScroll = true;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Control && e.KeyCode == Keys.C && _entries.Count > 0)
            {
                string all = string.Join(Environment.NewLine, _entries.Select(x => $"{x.Timestamp}{x.Message}"));
                try { Clipboard.SetText(all); } catch { }
            }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); Focus(); }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_maxScroll <= 0) return;

            _scrollOffset = Math.Clamp(_scrollOffset + (e.Delta > 0 ? -24 : 24), 0, _maxScroll);
            _autoScroll = _scrollOffset >= _maxScroll - 4;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button == MouseButtons.Left && _maxScroll > 0)
            {
                if (_thumbRect.Contains(e.Location))
                {
                    _isDraggingThumb = true;
                    _dragStartY = e.Y;
                    _dragStartScroll = _scrollOffset;
                    Invalidate();
                }
                else if (_trackRect.Contains(e.Location))
                {
                    float ratio = Math.Clamp((e.Y - _trackRect.Y) / (float)Math.Max(1, _trackRect.Height), 0f, 1f);
                    _scrollOffset = Math.Clamp((int)(ratio * _maxScroll), 0, _maxScroll);
                    _autoScroll = _scrollOffset >= _maxScroll - 4;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDraggingThumb && _maxScroll > 0)
            {
                int trackScrollable = Math.Max(1, _trackRect.Height - _thumbRect.Height);
                _scrollOffset = Math.Clamp(_dragStartScroll + (int)Math.Round(((e.Y - _dragStartY) / (double)trackScrollable) * _maxScroll), 0, _maxScroll);
                _autoScroll = _scrollOffset >= _maxScroll - 4;
                Invalidate();
                return;
            }

            bool overThumb = _maxScroll > 0 && _thumbRect.Contains(e.Location);
            if (overThumb != _hoverThumb)
            {
                _hoverThumb = overThumb;
                Cursor = overThumb ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isDraggingThumb) { _isDraggingThumb = false; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 10 || Height <= 10) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle outerRect = new(0, 0, Width - 1, Height - 1);
            using GraphicsPath outerPath = ThemeModule.GetResponsiveRoundedPath(outerRect, 8);
            using (SolidBrush bgBrush = new(FDPalette.SurfaceInput))
                g.FillPath(bgBrush, outerPath);

            Rectangle viewport = new(8, 8, Width - 22, Height - 16);
            if (viewport.Width <= 10 || viewport.Height <= 10) return;

            int textMaxW = Math.Max(20, viewport.Width - 2);
            int totalH = 4;
            var measured = new List<(string Timestamp, string Message, Color Color, int Height, int TsWidth)>(_entries.Count);

            foreach (var entry in _entries)
            {
                string full = entry.Timestamp + entry.Message;
                SizeF sz = g.MeasureString(full, Font, textMaxW);
                int tsW = (int)Math.Ceiling(g.MeasureString(entry.Timestamp, Font).Width);
                int rowH = Math.Max(15, (int)Math.Ceiling(sz.Height) + 2);
                measured.Add((entry.Timestamp, entry.Message, entry.Color, rowH, tsW));
                totalH += rowH + 3;
            }

            _maxScroll = Math.Max(0, totalH - viewport.Height);
            _scrollOffset = _autoScroll ? _maxScroll : Math.Clamp(_scrollOffset, 0, _maxScroll);

            GraphicsState state = g.Save();
            g.SetClip(viewport);

            using SolidBrush tsBrush = new(FDPalette.TextMuted);
            int curY = viewport.Y - _scrollOffset;

            foreach (var item in measured)
            {
                if (curY + item.Height >= viewport.Top && curY <= viewport.Bottom)
                {
                    g.DrawString(item.Timestamp, Font, tsBrush, new PointF(viewport.X, curY));

                    using SolidBrush msgBrush = new(item.Color);
                    string pad = new(' ', item.Timestamp.Length);
                    g.DrawString(pad + item.Message, Font, msgBrush,
                        new RectangleF(viewport.X, curY, textMaxW, item.Height + 4));
                }
                curY += item.Height + 3;
            }

            g.Restore(state);

            using (Pen borderPen = new(FDPalette.BorderSubtle, 1f))
                g.DrawPath(borderPen, outerPath);

            if (_maxScroll > 0)
            {
                _trackRect = new Rectangle(Width - 9, 8, 4, Height - 16);
                float viewRatio = (float)viewport.Height / Math.Max(1, totalH);
                int thumbH = Math.Max(22, (int)(_trackRect.Height * viewRatio));
                int thumbY = _trackRect.Y + (int)((_scrollOffset / (float)_maxScroll) * Math.Max(1, _trackRect.Height - thumbH));
                _thumbRect = new Rectangle(_trackRect.X, thumbY, _trackRect.Width, thumbH);

                using GraphicsPath thumbPath = ThemeModule.GetResponsiveRoundedPath(_thumbRect, 2);
                using SolidBrush thumbBrush = new((_isDraggingThumb || _hoverThumb) ? FDPalette.AccentBlurple : FDPalette.BorderSubtle);
                g.FillPath(thumbBrush, thumbPath);
            }
            else
            {
                _thumbRect = Rectangle.Empty;
                _trackRect = Rectangle.Empty;
            }
        }
    }

    [DefaultEvent("ValueChanged")]
    public class FDSlider : Control
    {
        public event EventHandler? ValueChanged;

        private decimal _minimum = 0.00m;
        [Category("Behavior Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public decimal Minimum
        {
            get => _minimum;
            set { _minimum = value; if (_value < _minimum) _value = _minimum; Invalidate(); }
        }

        private decimal _maximum = 100.00m;
        [Category("Behavior Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public decimal Maximum
        {
            get => _maximum;
            set { _maximum = Math.Max(_minimum + 0.01m, value); if (_value > _maximum) _value = _maximum; Invalidate(); }
        }

        private decimal _value = 15.00m;
        [Category("Behavior Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public decimal Value
        {
            get => _value;
            set
            {
                decimal clamped = Math.Clamp(Math.Round(value, 2), _minimum, _maximum);
                if (_value == clamped) return;
                _value = clamped;
                ValueChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        private Color _accentColor = FDPalette.AccentBlurple;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color AccentColor { get => _accentColor; set { _accentColor = value; Invalidate(); } }

        private bool _isDragging;
        private bool _isHovered;

        public FDSlider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(180, 22);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left && Enabled) { _isDragging = true; UpdateValueFromMouse(e.X); } }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (_isDragging && Enabled) UpdateValueFromMouse(e.X); }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _isDragging = false; Invalidate(); }

        private void UpdateValueFromMouse(int mouseX)
        {
            int pad = 9;
            int trackWidth = Math.Max(1, Width - pad * 2);
            double ratio = Math.Clamp((mouseX - pad) / (double)trackWidth, 0.0, 1.0);
            Value = _minimum + (decimal)ratio * (_maximum - _minimum);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int pad = 9;
            int trackH = 6;
            int trackY = Height / 2 - trackH / 2;
            int trackWidth = Math.Max(4, Width - pad * 2);
            Rectangle trackRect = new(pad, trackY, trackWidth, trackH);

            using (GraphicsPath trackPath = ThemeModule.GetResponsiveRoundedPath(trackRect, 3))
            using (SolidBrush trackBrush = new(FDPalette.SurfaceInput))
            using (Pen borderPen = new(FDPalette.BorderSubtle, 1f))
            {
                g.FillPath(trackBrush, trackPath);
                g.DrawPath(borderPen, trackPath);
            }

            double range = (double)Math.Max(0.01m, _maximum - _minimum);
            double ratio = Math.Clamp((double)(_value - _minimum) / range, 0.0, 1.0);
            int fillW = (int)Math.Round(ratio * trackWidth);

            Color activeStart = Enabled ? _accentColor : FDPalette.TextMuted;
            Color activeEnd = Enabled ? FDPalette.AccentViolet : FDPalette.TextMuted;

            if (fillW > 2)
            {
                Rectangle fillRect = new(pad, trackY, fillW, trackH);
                using GraphicsPath fillPath = ThemeModule.GetResponsiveRoundedPath(fillRect, 3);
                using LinearGradientBrush fillBrush = new(fillRect, activeStart, activeEnd, 0f);
                g.FillPath(fillBrush, fillPath);
            }

            int thumbX = pad + fillW;
            int thumbY = Height / 2;
            int radius = (_isDragging || _isHovered) && Enabled ? 7 : 6;

            if ((_isDragging || _isHovered) && Enabled)
            {
                using SolidBrush halo = new(Color.FromArgb(65, activeStart));
                g.FillEllipse(halo, thumbX - radius - 3, thumbY - radius - 3, (radius + 3) * 2, (radius + 3) * 2);
            }

            Rectangle thumbRect = new(thumbX - radius, thumbY - radius, radius * 2, radius * 2);
            using (SolidBrush thumbBrush = new(Enabled ? Color.White : FDPalette.TextMuted))
            using (Pen thumbRing = new(activeStart, 2f))
            {
                g.FillEllipse(thumbBrush, thumbRect);
                g.DrawEllipse(thumbRing, thumbRect);
            }
        }
    }

    public sealed class ControllerRemapEventArgs : EventArgs
    {
        public string Domain { get; }
        public string SlotKey { get; }
        public string Concept { get; }
        public bool StackAction { get; }
        public string? SwapTargetSlotKey { get; }
        public bool CopyToOtherVehicle { get; }
        public bool ResetToDefault { get; }

        public ControllerRemapEventArgs(string domain, string slotKey, string concept = "", bool stackAction = false,
            string? swapTargetSlotKey = null, bool copyToOtherVehicle = false, bool resetToDefault = false)
        {
            Domain = domain; SlotKey = slotKey; Concept = concept; StackAction = stackAction;
            SwapTargetSlotKey = swapTargetSlotKey; CopyToOtherVehicle = copyToOtherVehicle; ResetToDefault = resetToDefault;
        }
    }

    [DefaultEvent("Click")]
    public class FDModernButton : Control
    {
        private Image? _icon;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Image? Icon { get => _icon; set { _icon = value; Invalidate(); } }

        private int _cornerRadius = 8;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int CornerRadius { get => _cornerRadius; set { _cornerRadius = Math.Max(2, value); Invalidate(); } }

        private Color _accentColor = FDPalette.AccentBlurple;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color AccentColor { get => _accentColor; set { _accentColor = value; Invalidate(); } }

        private bool _showAccentBar;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool ShowAccentBar { get => _showAccentBar; set { _showAccentBar = value; Invalidate(); } }

        private bool _isPrimaryAction;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool IsPrimaryAction { get => _isPrimaryAction; set { _isPrimaryAction = value; Invalidate(); } }

        private bool _showIconBox;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool ShowIconBox { get => _showIconBox; set { _showIconBox = value; Invalidate(); } }

        private bool _centerText = true;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool CenterText { get => _centerText; set { _centerText = value; Invalidate(); } }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title { get => base.Text; set { base.Text = value; Invalidate(); } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text { get => base.Text; set { base.Text = value; Invalidate(); } }

        private bool _isHovered;
        private bool _isMouseDown;

        public FDModernButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, false);
            Size = new Size(140, 34);
            Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            ForeColor = FDPalette.TextPrimary;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _isHovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _isHovered = false; _isMouseDown = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { _isMouseDown = true; Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _isMouseDown = false; Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 4 || Height <= 4) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new(0, 0, Width - 1, Height - 1);
            bool isDanger = _accentColor.R > 190 && _accentColor.G < 110;

            Color fill, border, textCol;
            if (!Enabled)
            {
                fill = Color.FromArgb(24, 25, 35);
                border = FDPalette.BorderSubtle;
                textCol = FDPalette.TextMuted;
            }
            else if (_isPrimaryAction)
            {
                fill = _isMouseDown ? Color.FromArgb(68, 79, 200) : (_isHovered ? FDPalette.AccentBrurple : _accentColor);
                border = fill;
                textCol = Color.White;
            }
            else if (isDanger)
            {
                fill = _isMouseDown ? Color.FromArgb(175, 42, 46) : (_isHovered ? FDPalette.AccentCrimson : Color.FromArgb(48, 24, 32));
                border = _isHovered ? FDPalette.AccentCrimson : Color.FromArgb(120, 44, 52);
                textCol = _isHovered ? Color.White : Color.FromArgb(255, 148, 152);
            }
            else
            {
                fill = _isMouseDown ? FDPalette.SurfaceDark : (_isHovered ? FDPalette.SurfaceHover : FDPalette.SurfaceCard);
                border = _isHovered ? _accentColor : FDPalette.BorderSubtle;
                textCol = ForeColor;
            }

            using (GraphicsPath path = ThemeModule.GetResponsiveRoundedPath(rect, _cornerRadius))
            using (SolidBrush bg = new(fill))
            using (Pen pen = new(border, 1f))
            {
                g.FillPath(bg, path);
                g.DrawPath(pen, path);
            }

            int pressY = (_isMouseDown && Enabled) ? 1 : 0;
            Rectangle textBounds = new(6, pressY, Width - 12, Height);

            if (_icon != null)
            {
                int iconSize = Math.Min(18, Height - 12);
                int iconY = (Height - iconSize) / 2 + pressY;
                g.DrawImage(_icon, new Rectangle(10, iconY, iconSize, iconSize));
                textBounds = new Rectangle(10 + iconSize + 6, pressY, Width - iconSize - 22, Height);
            }

            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            flags |= _centerText ? TextFormatFlags.HorizontalCenter : TextFormatFlags.Left;
            TextRenderer.DrawText(g, Text, Font, textBounds, textCol, flags);
        }
    }

    [DefaultEvent("Click")]
    public class FDLaunchButton : Control
    {
        private bool _isHovered;
        private bool _isPressed;

        private string _subText = "GAME PROCESS RUNNER";
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string SubText { get => _subText; set { _subText = value; Invalidate(); } }

        private Image? _icon;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Image? Icon { get => _icon; set { _icon = value; Invalidate(); } }

        private Color _accentColor = FDPalette.AccentBlurple;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color AccentColor { get => _accentColor; set { _accentColor = value; Invalidate(); } }

        private int _cornerRadius = 10;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int CornerRadius { get => _cornerRadius; set { _cornerRadius = Math.Max(4, value); Invalidate(); } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text { get => base.Text; set { base.Text = value; Invalidate(); } }

        public FDLaunchButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, false);
            Size = new Size(260, 48);
            BackColor = Color.Transparent;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            Text = "START SESSION";
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHovered = false; _isPressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _isPressed = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _isPressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 6 || Height <= 6) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle outer = new(0, 0, Width - 1, Height - 1);
            using GraphicsPath path = ThemeModule.GetResponsiveRoundedPath(outer, _cornerRadius);

            if (!Enabled)
            {
                using SolidBrush bg = new(Color.FromArgb(28, 29, 40));
                using Pen border = new(FDPalette.BorderSubtle, 1f);
                g.FillPath(bg, path);
                g.DrawPath(border, path);
                TextRenderer.DrawText(g, Text, Font, outer, FDPalette.TextMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            Color leftCol = _isPressed ? Color.FromArgb(68, 80, 204) : (_isHovered ? FDPalette.AccentBrurple : _accentColor);
            Color rightCol = _isPressed ? Color.FromArgb(116, 72, 206) : (_isHovered ? Color.FromArgb(166, 118, 255) : FDPalette.AccentViolet);

            using (LinearGradientBrush grad = new(outer, leftCol, rightCol, 0f))
            {
                g.FillPath(grad, path);
            }

            Rectangle inner = new(1, 1, Width - 3, Height - 3);
            using (GraphicsPath innerPath = ThemeModule.GetResponsiveRoundedPath(inner, Math.Max(2, _cornerRadius - 1)))
            using (Pen sheen = new(Color.FromArgb(_isHovered ? 80 : 42, 255, 255, 255), 1f))
            {
                g.DrawPath(sheen, innerPath);
            }

            int pressY = _isPressed ? 1 : 0;
            if (!string.IsNullOrWhiteSpace(_subText) && Height >= 42)
            {
                using Font subFont = new("Segoe UI", 7.25f, FontStyle.Bold);
                int titleH = TextRenderer.MeasureText(Text, Font).Height;
                int subH = TextRenderer.MeasureText(_subText, subFont).Height;
                int topY = (Height - (titleH + subH - 2)) / 2 + pressY;

                TextRenderer.DrawText(g, Text, Font, new Rectangle(10, topY, Width - 20, titleH), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(g, _subText, subFont, new Rectangle(10, topY + titleH - 2, Width - 20, subH), Color.FromArgb(222, 226, 255),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            else
            {
                TextRenderer.DrawText(g, Text, Font, new Rectangle(10, pressY, Width - 20, Height), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }
    }

    [DefaultEvent("CheckedChanged")]
    public class FDToggle : Control
    {
        private bool _checked;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool Checked
        {
            get => _checked;
            set { if (_checked == value) return; _checked = value; CheckedChanged?.Invoke(this, EventArgs.Empty); Invalidate(); }
        }

        public event EventHandler? CheckedChanged;

        public FDToggle()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(44, 22);
            Cursor = Cursors.Hand;
        }

        protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 4 || Height <= 4) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = ThemeModule.GetResponsiveRoundedPath(rect, Height / 2))
            using (SolidBrush brush = new(_checked ? FDPalette.HudGreen : FDPalette.SurfaceHover))
            {
                g.FillPath(brush, path);
            }

            int pad = 3;
            int thumbSz = Height - pad * 2;
            int thumbX = _checked ? (Width - thumbSz - pad) : pad;
            using SolidBrush thumb = new(Color.White);
            g.FillEllipse(thumb, thumbX, pad, thumbSz, thumbSz);
        }
    }

    [DefaultEvent("Click")]
    public class FDSidebarItem : Control
    {
        private bool _isHovered;
        private bool _isSelected;

        [Category("Behavior Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Control? LinkedPanel { get; set; }

        [Category("Behavior Quest")]
        [Description("Optional secondary panel/group (e.g. right-rail controls) shown when this tab is active.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Control? LinkedSecondaryPanel { get; set; }

        private Image? _icon;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Image? Icon { get => _icon; set { _icon = value; Invalidate(); } }

        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool Selected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;

                if (_isSelected)
                    ActivateLinkedPanels();

                Invalidate();
            }
        }

        public FDSidebarItem()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            Size = new Size(130, 32);
            Cursor = Cursors.Hand;
        }

        private void ActivateLinkedPanels()
        {
            if (Parent != null)
            {
                foreach (Control ctrl in Parent.Controls)
                {
                    if (ctrl is FDSidebarItem other && other != this)
                    {
                        if (other._isSelected)
                        {
                            other._isSelected = false;
                            other.Invalidate();
                        }

                        if (other.LinkedPanel != null && other.LinkedPanel != LinkedPanel)
                            other.LinkedPanel.Visible = false;

                        if (other.LinkedSecondaryPanel != null && other.LinkedSecondaryPanel != LinkedSecondaryPanel)
                            other.LinkedSecondaryPanel.Visible = false;
                    }
                }
            }

            if (LinkedPanel != null)
            {
                LinkedPanel.Visible = true;
                LinkedPanel.BringToFront();
            }

            if (LinkedSecondaryPanel != null)
            {
                LinkedSecondaryPanel.Visible = true;
                LinkedSecondaryPanel.BringToFront();
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { _isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnClick(EventArgs e)
        {
            Selected = true;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 6 || Height <= 6) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new(1, 1, Width - 3, Height - 3);

            Color bg = _isSelected
                ? Color.FromArgb(50, 56, 112)
                : (_isHovered ? Color.FromArgb(37, 40, 60) : FDPalette.SurfaceCard);

            Color border = _isSelected
                ? FDPalette.AccentBrurple
                : (_isHovered ? FDPalette.AccentBlurple : FDPalette.BorderSubtle);

            float borderWidth = _isSelected ? 1.5f : 1f;

            using (GraphicsPath path = ThemeModule.GetResponsiveRoundedPath(rect, rect.Height / 2))
            using (SolidBrush bgBrush = new(bg))
            using (Pen borderPen = new(border, borderWidth))
            {
                g.FillPath(bgBrush, path);
                g.DrawPath(borderPen, path);
            }

            if (_isSelected)
            {
                int dotY = Height / 2 - 3;
                using SolidBrush halo = new(Color.FromArgb(70, FDPalette.AccentBrurple));
                using SolidBrush dot = new(FDPalette.AccentBrurple);
                g.FillEllipse(halo, 10, dotY - 2, 10, 10);
                g.FillEllipse(dot, 12, dotY, 6, 6);
            }

            Rectangle textRect = _isSelected
                ? new Rectangle(24, 0, Width - 30, Height)
                : new Rectangle(8, 0, Width - 16, Height);

            Color textCol = _isSelected || _isHovered ? FDPalette.TextPrimary : FDPalette.TextSecondary;
            TextRenderer.DrawText(g, Text, Font, textRect, textCol,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    public class FDStatPill : Control
    {
        private string _prefix = "STATUS";
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Prefix { get => _prefix; set { _prefix = value; Invalidate(); } }

        public FDStatPill()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(175, 30);
            Text = "Ready";
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        private Color ResolveStatusDotColor()
        {
            string val = (Text ?? string.Empty).Trim();
            if (val.Equals("Ready", StringComparison.OrdinalIgnoreCase) ||
                val.Equals("Connected", StringComparison.OrdinalIgnoreCase))
                return FDPalette.HudGreen;

            if (val.Equals("Missing", StringComparison.OrdinalIgnoreCase) ||
                val.Contains("Error", StringComparison.OrdinalIgnoreCase))
                return FDPalette.AccentCrimson;

            if (val.Contains("Check", StringComparison.OrdinalIgnoreCase))
                return FDPalette.AccentOrange;

            if (val.Equals("Disconnected", StringComparison.OrdinalIgnoreCase) ||
                val == "-" || val == "-")
                return FDPalette.TextMuted;

            if (_prefix.Contains("ORB", StringComparison.OrdinalIgnoreCase))
                return FDPalette.AccentViolet;

            return FDPalette.AccentBrurple;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 6 || Height <= 6) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = ThemeModule.GetResponsiveRoundedPath(rect, Height / 2))
            using (SolidBrush brush = new(FDPalette.SurfaceDark))
            using (Pen borderPen = new(FDPalette.BorderSubtle, 1f))
            {
                g.FillPath(brush, path);
                g.DrawPath(borderPen, path);
            }

            Color dotCol = ResolveStatusDotColor();
            int dotY = Height / 2 - 4;
            using (SolidBrush halo = new(Color.FromArgb(60, dotCol)))
            using (SolidBrush dot = new(dotCol))
            {
                g.FillEllipse(halo, 9, dotY - 2, 12, 12);
                g.FillEllipse(dot, 11, dotY, 8, 8);
            }

            using Font prefixFont = new("Segoe UI", 8f, FontStyle.Bold);
            using Font valFont = new("Segoe UI", 8.5f, FontStyle.Bold);

            int textX = 25;
            Size pSize = TextRenderer.MeasureText(_prefix, prefixFont);
            TextRenderer.DrawText(g, _prefix, prefixFont, new Rectangle(textX, 0, pSize.Width, Height), FDPalette.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            int valX = textX + pSize.Width;
            TextRenderer.DrawText(g, Text, valFont, new Rectangle(valX, 0, Math.Max(10, Width - valX - 10), Height), FDPalette.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    public class AboutLinkTile : Control
    {
        private string _title = "Website";
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Title { get => _title; set { _title = value; Invalidate(); } }

        [Category("Behavior Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Url { get; set; } = "";

        private Image? _icon;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Image? Icon { get => _icon; set { _icon = value; Invalidate(); } }

        private bool _isHovered;

        public AboutLinkTile()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(210, 42);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (!string.IsNullOrWhiteSpace(Url))
            {
                try { Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true }); }
                catch (Exception ex) { MessageBox.Show($"Could not open link: {ex.Message}", "Quest Completer", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 6 || Height <= 6) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = ThemeModule.GetResponsiveRoundedPath(rect, 10))
            using (SolidBrush bg = new(_isHovered ? FDPalette.SurfaceHover : FDPalette.SurfaceCard))
            using (Pen pen = new(_isHovered ? FDPalette.AccentBlurple : FDPalette.BorderSubtle, 1f))
            {
                g.FillPath(bg, path);
                g.DrawPath(pen, path);
            }

            int textX = 14;
            if (_icon != null)
            {
                int sz = Math.Min(22, Height - 14);
                g.DrawImage(_icon, new Rectangle(12, (Height - sz) / 2, sz, sz));
                textX = 12 + sz + 8;
            }

            using Font font = new("Segoe UI", 9f, FontStyle.Bold);
            TextRenderer.DrawText(g, _title, font, new Rectangle(textX, 0, Width - textX - 28, Height), FDPalette.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            TextRenderer.DrawText(g, "↗", font, new Rectangle(Width - 28, 0, 20, Height),
                _isHovered ? FDPalette.AccentBrurple : FDPalette.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    public class FDPatchNotesBox : Control
    {
        private string _title = "NOTES";
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Title { get => _title; set { _title = value; Invalidate(); } }

        private Color _accentColor = FDPalette.AccentBlurple;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color AccentColor { get => _accentColor; set { _accentColor = value; Invalidate(); } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text { get => base.Text; set { base.Text = value; _scrollOffset = 0; Invalidate(); } }

        private int _scrollOffset;
        private int _maxScroll;
        private bool _isDraggingThumb;
        private int _dragStartY;
        private int _dragStartScroll;
        private Rectangle _thumbRect;
        private Rectangle _trackRect;
        private bool _hoverThumb;

        public FDPatchNotesBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            ForeColor = FDPalette.TextPrimary;
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            Size = new Size(448, 160);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); Focus(); }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_maxScroll <= 0) return;
            _scrollOffset = Math.Clamp(_scrollOffset + (e.Delta > 0 ? -24 : 24), 0, _maxScroll);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button == MouseButtons.Left && _maxScroll > 0)
            {
                if (_thumbRect.Contains(e.Location))
                {
                    _isDraggingThumb = true;
                    _dragStartY = e.Y;
                    _dragStartScroll = _scrollOffset;
                    Invalidate();
                }
                else if (_trackRect.Contains(e.Location))
                {
                    float ratio = (e.Y - _trackRect.Y) / (float)Math.Max(1, _trackRect.Height);
                    _scrollOffset = Math.Clamp((int)(ratio * _maxScroll), 0, _maxScroll);
                    Invalidate();
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDraggingThumb && _maxScroll > 0)
            {
                int trackScrollable = Math.Max(1, _trackRect.Height - _thumbRect.Height);
                _scrollOffset = Math.Clamp(_dragStartScroll + (int)Math.Round(((e.Y - _dragStartY) / (double)trackScrollable) * _maxScroll), 0, _maxScroll);
                Invalidate();
                return;
            }
            bool overThumb = _maxScroll > 0 && _thumbRect.Contains(e.Location);
            if (overThumb != _hoverThumb) { _hoverThumb = overThumb; Cursor = overThumb ? Cursors.Hand : Cursors.Default; Invalidate(); }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isDraggingThumb) { _isDraggingThumb = false; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 10 || Height <= 10) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle outerRect = new(0, 0, Width - 1, Height - 1);
            using GraphicsPath outerPath = ThemeModule.GetResponsiveRoundedPath(outerRect, 10);
            using (SolidBrush bgBrush = new(FDPalette.SurfaceInput))
            {
                g.FillPath(bgBrush, outerPath);
            }

            Rectangle viewport = new(12, 10, Width - 28, Height - 20);
            if (viewport.Width <= 10 || viewport.Height <= 10) return;

            string[] lines = (Text ?? "").Replace("\r\n", "\n").Split('\n');
            using Font regularFont = new(Font.FontFamily, Font.Size, FontStyle.Regular);
            using Font boldFont = new(Font.FontFamily, Font.Size, FontStyle.Bold);

            int textMaxW = Math.Max(20, viewport.Width - 6);
            int totalContentHeight = 6;
            var parsedLines = new List<(string Text, bool IsBullet, bool IsHeader, int Height, int AvailW)>(lines.Length);

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) { parsedLines.Add(("", false, false, 10, textMaxW)); totalContentHeight += 10; continue; }

                bool isBullet = line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("• ");
                bool isHeader = !isBullet && (line.StartsWith('#') || line.EndsWith(':') || (line.StartsWith('[') && line.EndsWith(']')));

                string cleanText = isBullet ? line[2..].Trim() : line.TrimStart('#', '[', ' ').TrimEnd(']');
                int availW = isBullet ? Math.Max(20, textMaxW - 16) : textMaxW;
                int rowH = Math.Max(18, (int)Math.Ceiling(g.MeasureString(cleanText, isHeader ? boldFont : regularFont, availW).Height) + 2);

                parsedLines.Add((cleanText, isBullet, isHeader, rowH, availW));
                totalContentHeight += rowH + 6;
            }

            _maxScroll = Math.Max(0, totalContentHeight - viewport.Height);
            _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScroll);

            GraphicsState state = g.Save();
            g.SetClip(viewport);

            using SolidBrush primaryBrush = new(FDPalette.TextPrimary);
            using SolidBrush secondaryBrush = new(FDPalette.TextSecondary);
            using SolidBrush accentBrush = new(FDPalette.AccentBrurple);

            int curY = viewport.Y - _scrollOffset;
            foreach (var item in parsedLines)
            {
                if (string.IsNullOrEmpty(item.Text)) { curY += item.Height; continue; }
                if (curY + item.Height >= viewport.Top && curY <= viewport.Bottom)
                {
                    if (item.IsBullet)
                    {
                        g.FillEllipse(accentBrush, new Rectangle(viewport.X + 2, curY + 6, 5, 5));
                        g.DrawString(item.Text, regularFont, primaryBrush, new RectangleF(viewport.X + 14, curY, item.AvailW, item.Height + 4));
                    }
                    else if (item.IsHeader)
                    {
                        g.DrawString(item.Text, boldFont, accentBrush, new RectangleF(viewport.X, curY, item.AvailW, item.Height + 4));
                    }
                    else
                    {
                        g.DrawString(item.Text, regularFont, secondaryBrush, new RectangleF(viewport.X, curY, item.AvailW, item.Height + 4));
                    }
                }
                curY += item.Height + 6;
            }

            g.Restore(state);

            using (Pen borderPen = new(FDPalette.BorderSubtle, 1f))
            {
                g.DrawPath(borderPen, outerPath);
            }

            if (_maxScroll > 0)
            {
                _trackRect = new Rectangle(Width - 10, 10, 4, Height - 20);
                float viewRatio = (float)viewport.Height / Math.Max(1, totalContentHeight);
                int thumbH = Math.Max(22, (int)(_trackRect.Height * viewRatio));
                int thumbY = _trackRect.Y + (int)((_scrollOffset / (float)_maxScroll) * Math.Max(1, _trackRect.Height - thumbH));
                _thumbRect = new Rectangle(_trackRect.X, thumbY, _trackRect.Width, thumbH);

                using GraphicsPath thumbPath = ThemeModule.GetResponsiveRoundedPath(_thumbRect, 2);
                using SolidBrush thumbBrush = new((_isDraggingThumb || _hoverThumb) ? _accentColor : FDPalette.BorderSubtle);
                g.FillPath(thumbBrush, thumbPath);
            }
            else
            {
                _thumbRect = Rectangle.Empty;
                _trackRect = Rectangle.Empty;
            }
        }
    }

    [DefaultEvent("TextChanged")]
    public class FDTextBox : Control
    {
        public readonly TextBox Base;
        private readonly int _borderRadius = 8;

        [DllImport("user32.dll")]
        private static extern nint SendMessage(nint hWnd, int wMsg, nint wParam, nint lParam);
        private const int WM_VSCROLL = 0x115;
        private const int SB_BOTTOM = 7;

        protected override Size DefaultSize => new(240, 34);

        public FDTextBox()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint |
         ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
         ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;

            Base = new TextBox
            {
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                BorderStyle = BorderStyle.None,
                ForeColor = FDPalette.TextPrimary,
                BackColor = FDPalette.SurfaceInput,
                Location = new Point(11, 8)
            };

            Controls.Add(Base);
            Base.TextChanged += (_, _) => { base.Text = Base.Text; };
            Base.KeyDown += (s, e) => { if (e.Control && e.KeyCode == Keys.A) { Base.SelectAll(); e.SuppressKeyPress = true; } };
            Base.Enter += (_, _) => Invalidate();
            Base.Leave += (_, _) => Invalidate();

            UpdateBaseSize();
        }

        public void ScrollToBottom() { if (Base.IsHandleCreated) SendMessage(Base.Handle, WM_VSCROLL, SB_BOTTOM, nint.Zero); }
        public void AppendText(string text) => Base.AppendText(text);
        public void Clear() => Base.Clear();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public HorizontalAlignment TextAlign { get => Base.TextAlign; set => Base.TextAlign = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int MaxLength { get => Base.MaxLength; set => Base.MaxLength = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool ReadOnly { get => Base.ReadOnly; set => Base.ReadOnly = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool UseSystemPasswordChar { get => Base.UseSystemPasswordChar; set => Base.UseSystemPasswordChar = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool Multiline
        {
            get => Base.Multiline;
            set { Base.Multiline = value; Base.WordWrap = value; Base.ScrollBars = value ? ScrollBars.Vertical : ScrollBars.None; UpdateBaseSize(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text { get => base.Text; set { base.Text = value; Base.Text = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override Font Font { get => base.Font; set { base.Font = value; Base.Font = value; UpdateBaseSize(); } }

        private void UpdateBaseSize()
        {
            if (Base == null) return;
            Base.Width = Math.Max(10, Width - 22);
            if (!Multiline)
            {
                Height = Math.Max(32, Base.Height + 14);
                Base.Top = Math.Max(4, (Height - Base.Height) / 2);
            }
            else
            {
                Base.Top = 8;
                Base.Height = Math.Max(10, Height - 16);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateBaseSize();
            Invalidate();
            base.OnResize(e);
        }
        protected override void OnMouseDown(MouseEventArgs e) { Base.Focus(); base.OnMouseDown(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 4 || Height <= 4) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle mainRect = new(0, 0, Width - 1, Height - 1);
            using GraphicsPath path = ThemeModule.GetResponsiveRoundedPath(mainRect, _borderRadius);
            using SolidBrush bgBrush = new(Base.BackColor);
            using Pen borderPen = new(Base.Focused ? FDPalette.BorderFocus : FDPalette.BorderSubtle, Base.Focused ? 1.5f : 1f);
            e.Graphics.FillPath(bgBrush, path);
            e.Graphics.DrawPath(borderPen, path);
        }
    }

    public class FDButton : Control
    {
        private bool _isHovered;
        private bool _isPressed;

        protected override Size DefaultSize => new(125, 34);

        public FDButton()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { _isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _isPressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _isPressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 4 || Height <= 4) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new(0, 0, Width - 1, Height - 1);
            Color fill = !Enabled ? FDPalette.SurfaceDark : (_isPressed ? FDPalette.SurfaceInput : (_isHovered ? FDPalette.SurfaceHover : FDPalette.SurfaceCard));
            Color border = (_isHovered && Enabled) ? FDPalette.AccentBlurple : FDPalette.BorderSubtle;

            using GraphicsPath path = ThemeModule.GetResponsiveRoundedPath(rect, 8);
            using SolidBrush bg = new(fill);
            using Pen pen = new(border, 1f);
            e.Graphics.FillPath(bg, path);
            e.Graphics.DrawPath(pen, path);

            TextRenderer.DrawText(e.Graphics, Text, Font, rect, Enabled ? FDPalette.TextPrimary : FDPalette.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    public class FDGroupBox : ContainerControl
    {
        private string _title = "SECTION";
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Title { get => _title; set { _title = value; Invalidate(); } }

        private string _subTitle = "";
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string SubTitle { get => _subTitle; set { _subTitle = value; Invalidate(); } }

        protected override Size DefaultSize => new(280, 140);

        public FDGroupBox()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            Padding = new Padding(14, 46, 14, 14);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 6 || Height <= 6) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new(0, 0, Width - 1, Height - 1);
            using GraphicsPath cardPath = ThemeModule.GetResponsiveRoundedPath(rect, 12);

            using (SolidBrush bg = new(FDPalette.SurfaceDark))
            {
                g.FillPath(bg, cardPath);
            }

            int headerH = 38;
            GraphicsState state = g.Save();
            g.SetClip(cardPath);
            using (SolidBrush hdrBg = new(FDPalette.SurfaceCard))
            using (Pen hdrDivider = new(FDPalette.BorderSubtle, 1f))
            {
                g.FillRectangle(hdrBg, 0, 0, Width, headerH);
                g.DrawLine(hdrDivider, 0, headerH, Width, headerH);
            }
            g.Restore(state);

            using (Font titleFont = new("Segoe UI", 8.75f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, _title, titleFont,
                    new Rectangle(14, 0, Width - 28, headerH), FDPalette.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            if (!string.IsNullOrWhiteSpace(_subTitle))
            {
                using Font subFont = new("Segoe UI", 8f, FontStyle.Regular);
                TextRenderer.DrawText(g, _subTitle, subFont,
                    new Rectangle(14, 0, Width - 28, headerH), FDPalette.TextMuted,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            using (Pen borderPen = new(FDPalette.BorderSubtle, 1f))
            {
                g.DrawPath(borderPen, cardPath);
            }
        }
    }

    public class FDMenuContainer : ContainerControl
    {
        private string _title = "Module";
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Title { get => _title; set { _title = value; Invalidate(); } }

        private Image? _icon;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Image? Icon { get => _icon; set { _icon = value; Invalidate(); } }

        public FDMenuContainer()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
            Padding = new Padding(12, 42, 12, 12);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 6 || Height <= 6) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new(0, 0, Width - 1, Height - 1);
            using GraphicsPath path = ThemeModule.GetResponsiveRoundedPath(rect, 12);
            using SolidBrush bg = new(FDPalette.SurfaceDark);
            using Pen border = new(FDPalette.BorderSubtle, 1f);
            e.Graphics.FillPath(bg, path);
            e.Graphics.DrawPath(border, path);

            using Font f = new("Segoe UI", 10f, FontStyle.Bold);
            TextRenderer.DrawText(e.Graphics, _title, f, new Rectangle(14, 10, Width - 28, 24), FDPalette.TextPrimary);
        }
    }

    public class FDComboBox : ComboBox
    {
        public FDComboBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            BackColor = FDPalette.SurfaceCard;
            ForeColor = FDPalette.TextPrimary;
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            ItemHeight = 24;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle mainRect = new(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = ThemeModule.GetResponsiveRoundedPath(mainRect, 8))
            using (SolidBrush bgBrush = new(FDPalette.SurfaceInput))
            using (Pen borderPen = new(Focused ? FDPalette.AccentBlurple : FDPalette.BorderSubtle, 1f))
            {
                e.Graphics.FillPath(bgBrush, path);
                e.Graphics.DrawPath(borderPen, path);
            }

            if (SelectedIndex != -1)
            {
                Rectangle textRect = new(10, 0, Math.Max(10, Width - 34), Height);
                TextRenderer.DrawText(e.Graphics, Text, Font, textRect, FDPalette.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            using Pen arrowPen = new(FDPalette.AccentBrurple, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            int arrowX = Width - 18;
            int arrowY = Height / 2 - 2;
            e.Graphics.DrawLine(arrowPen, arrowX, arrowY, arrowX + 4, arrowY + 4);
            e.Graphics.DrawLine(arrowPen, arrowX + 4, arrowY + 4, arrowX + 8, arrowY);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (SolidBrush bgBrush = new(isSelected ? FDPalette.AccentBlurple : FDPalette.SurfaceCard))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            string itemText = Items[e.Index]?.ToString() ?? string.Empty;
            Rectangle textRect = new(e.Bounds.X + 10, e.Bounds.Y, Math.Max(10, e.Bounds.Width - 16), e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, itemText, Font, textRect, isSelected ? Color.White : FDPalette.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    [DefaultEvent("CheckedChanged")]
    public class FDCheckBox : Control
    {
        public event EventHandler? CheckedChanged;
        private bool _checked;

        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool Checked
        {
            get => _checked;
            set { if (_checked == value) return; _checked = value; CheckedChanged?.Invoke(this, EventArgs.Empty); Invalidate(); }
        }

        public FDCheckBox()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9.25f);
            Size = new Size(180, 24);
            Cursor = Cursors.Hand;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }
        protected override void OnMouseDown(MouseEventArgs e) { Checked = !Checked; base.OnMouseDown(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle boxRect = new(0, (Height - 18) / 2, 18, 18);

            using (GraphicsPath boxPath = ThemeModule.GetResponsiveRoundedPath(boxRect, 5))
            using (SolidBrush bgBrush = new(_checked ? FDPalette.AccentBlurple : FDPalette.SurfaceInput))
            using (Pen borderPen = new(_checked ? FDPalette.AccentBlurple : FDPalette.BorderSubtle, 1f))
            {
                e.Graphics.FillPath(bgBrush, boxPath);
                e.Graphics.DrawPath(borderPen, boxPath);
            }

            if (_checked)
            {
                using Pen checkPen = new(Color.White, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                e.Graphics.DrawLine(checkPen, boxRect.X + 4, boxRect.Y + 9, boxRect.X + 8, boxRect.Bottom - 5);
                e.Graphics.DrawLine(checkPen, boxRect.X + 8, boxRect.Bottom - 5, boxRect.Right - 4, boxRect.Y + 5);
            }

            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(26, 0, Width - 26, Height), FDPalette.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }

    [DefaultEvent("Click")]
    public class LaunchButton : FDLaunchButton { }

    public class FDProgressBar : Control
    {
        private int _minimum;
        [Category("Behavior Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Minimum
        {
            get => _minimum;
            set { _minimum = Math.Max(0, value); if (_value < _minimum) _value = _minimum; Invalidate(); }
        }

        private int _maximum = 100;
        [Category("Behavior Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Maximum
        {
            get => _maximum;
            set { _maximum = Math.Max(_minimum + 1, value); if (_value > _maximum) _value = _maximum; Invalidate(); }
        }

        private int _value;
        [Category("Behavior Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Value
        {
            get => _value;
            set { _value = Math.Clamp(value, _minimum, _maximum); Invalidate(); }
        }

        public void Increment(int amount) => Value += amount;

        public FDProgressBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Color.Transparent;
            Size = new Size(200, 12);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 6 || Height <= 4) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle outer = new(0, 0, Width - 1, Height - 1);
            int radius = Height / 2;

            using GraphicsPath gpOuter = ThemeModule.GetResponsiveRoundedPath(outer, radius);
            using (SolidBrush trackBrush = new(FDPalette.SurfaceInput))
            {
                g.FillPath(trackBrush, gpOuter);
            }

            double range = Math.Max(1, _maximum - _minimum);
            int fillWidth = (int)Math.Round((_value - _minimum) / range * (Width - 2));

            if (fillWidth > 3)
            {
                Rectangle fillRect = new(1, 1, fillWidth, Height - 2);
                using GraphicsPath gpFill = ThemeModule.GetResponsiveRoundedPath(fillRect, Math.Max(1, radius - 1));
                using LinearGradientBrush fillBrush = new(fillRect, FDPalette.AccentBlurple, FDPalette.HudGreen, 0f);
                g.FillPath(fillBrush, gpFill);
            }

            using Pen borderPen = new(FDPalette.BorderSubtle, 1f);
            g.DrawPath(borderPen, gpOuter);
        }
    }

    public class FDLabel : Control
    {
        private string _value1 = "VOID";
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Value1 { get => _value1; set { _value1 = value; Invalidate(); } }

        private string _value2 = "TECH";
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Value2 { get => _value2; set { _value2 = value; Invalidate(); } }

        public FDLabel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            Size = new Size(140, 24);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            SizeF sz1 = g.MeasureString(_value1, Font, Width, StringFormat.GenericTypographic);
            float y = Height / 2f - sz1.Height / 2f;
            using SolidBrush pBrush = new(FDPalette.TextPrimary);
            using SolidBrush aBrush = new(FDPalette.AccentBrurple);
            g.DrawString(_value1, Font, pBrush, 0f, y);
            g.DrawString(_value2, Font, aBrush, sz1.Width + 2f, y);
        }
    }

    public class FDTabControl : TabControl
    {
        public FDTabControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
            SizeMode = TabSizeMode.Fixed;
            ItemSize = new Size(125, 34);
            DrawMode = TabDrawMode.OwnerDrawFixed;
            Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(FDPalette.BgDeep);
            for (int i = 0; i < TabCount; i++)
            {
                Rectangle r = GetTabRect(i);
                bool sel = SelectedIndex == i;
                using GraphicsPath p = ThemeModule.GetResponsiveRoundedPath(r, 8);
                using SolidBrush b = new(sel ? Color.FromArgb(44, 48, 92) : FDPalette.SurfaceDark);
                e.Graphics.FillPath(b, p);
                TextRenderer.DrawText(e.Graphics, TabPages[i].Text, Font, r, sel ? FDPalette.TextPrimary : FDPalette.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    [DefaultEvent("CheckedChanged")]
    public class FDOnOffBox : Control
    {
        public delegate void CheckedChangedEventHandler(object sender);
        public event CheckedChangedEventHandler? CheckedChanged;

        private bool _checked;
        [Category("Appearance Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool Checked
        {
            get => _checked;
            set { if (_checked == value) return; _checked = value; CheckedChanged?.Invoke(this); Invalidate(); }
        }

        public FDOnOffBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            Size = new Size(68, 26);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseDown(MouseEventArgs e) { Checked = !Checked; base.OnMouseDown(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle outer = new(0, 0, Width - 1, Height - 1);
            using GraphicsPath path = ThemeModule.GetResponsiveRoundedPath(outer, Height / 2);
            using SolidBrush bg = new(_checked ? FDPalette.AccentBlurple : FDPalette.SurfaceCard);
            using Pen border = new(_checked ? FDPalette.AccentBlurple : FDPalette.BorderSubtle, 1f);
            g.FillPath(bg, path);
            g.DrawPath(border, path);

            int knob = Height - 6;
            int kx = _checked ? Width - knob - 3 : 3;
            using SolidBrush kb = new(Color.White);
            g.FillEllipse(kb, kx, 3, knob, knob);
        }
    }

    public class FDSeperator : Control
    {
        public FDSeperator()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Height = 8;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using Pen p = new(FDPalette.BorderSubtle, 1f);
            e.Graphics.DrawLine(p, 0, Height / 2, Width, Height / 2);
        }
    }

    public class FDKeyboard : Control
    {
        private Control? _target;
        [Category("Behavior Quest")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Control? Target { get => _target; set => _target = value; }
    }
}