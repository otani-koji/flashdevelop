using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using PluginCore.DockPanelSuite;
using PluginCore.Helpers;
using PluginCore;

namespace WeifenLuo.WinFormsUI.Docking
{
    partial class DockPanel
    {
        private sealed class DockDragHandler : DragHandler
        {
            private class DockIndicator : DragForm
            {
                #region IHitTest
                private interface IHitTest
                {
                    DockStyle HitTest(Point pt);
                    DockStyle Status { get; set;    }
                }
                #endregion

                #region PanelIndicator
                private class PanelIndicator : PictureBox, IHitTest
                {
                    private static readonly Image _imagePanelLeft = ScaleHelper.Scale(Resources.DockIndicator_PanelLeft);
                    private static readonly Image _imagePanelRight = ScaleHelper.Scale(Resources.DockIndicator_PanelRight);
                    private static readonly Image _imagePanelTop = ScaleHelper.Scale(Resources.DockIndicator_PanelTop);
                    private static readonly Image _imagePanelBottom = ScaleHelper.Scale(Resources.DockIndicator_PanelBottom);
                    private static readonly Image _imagePanelFill = ScaleHelper.Scale(Resources.DockIndicator_PanelFill);
                    private static readonly Image _imagePanelLeftActive = ScaleHelper.Scale(Resources.DockIndicator_PanelLeft_Active);
                    private static readonly Image _imagePanelRightActive = ScaleHelper.Scale(Resources.DockIndicator_PanelRight_Active);
                    private static readonly Image _imagePanelTopActive = ScaleHelper.Scale(Resources.DockIndicator_PanelTop_Active);
                    private static readonly Image _imagePanelBottomActive = ScaleHelper.Scale(Resources.DockIndicator_PanelBottom_Active);
                    private static readonly Image _imagePanelFillActive = ScaleHelper.Scale(Resources.DockIndicator_PanelFill_Active);

                    public PanelIndicator(DockStyle dockStyle)
                    {
                        m_dockStyle = dockStyle;
                        SizeMode = PictureBoxSizeMode.AutoSize;
                        Image = ImageInactive;
                    }

                    private readonly DockStyle m_dockStyle;
                    private DockStyle DockStyle => m_dockStyle;

                    private DockStyle m_status;
                    public DockStyle Status
                    {
                        get => m_status;
                        set
                        {
                            if (value != DockStyle && value != DockStyle.None)
                                throw new InvalidEnumArgumentException();

                            if (m_status == value)
                                return;

                            m_status = value;
                            IsActivated = (m_status != DockStyle.None);
                        }
                    }

                    private Image ImageInactive
                    {
                        get
                        {
                            if (DockStyle == DockStyle.Left)
                                return _imagePanelLeft;
                            if (DockStyle == DockStyle.Right)
                                return _imagePanelRight;
                            if (DockStyle == DockStyle.Top)
                                return _imagePanelTop;
                            if (DockStyle == DockStyle.Bottom)
                                return _imagePanelBottom;
                            if (DockStyle == DockStyle.Fill)
                                return _imagePanelFill;
                            return null;
                        }
                    }

                    private Image ImageActive
                    {
                        get
                        {
                            if (DockStyle == DockStyle.Left)
                                return _imagePanelLeftActive;
                            if (DockStyle == DockStyle.Right)
                                return _imagePanelRightActive;
                            if (DockStyle == DockStyle.Top)
                                return _imagePanelTopActive;
                            if (DockStyle == DockStyle.Bottom)
                                return _imagePanelBottomActive;
                            if (DockStyle == DockStyle.Fill)
                                return _imagePanelFillActive;
                            return null;
                        }
                    }

                    private bool m_isActivated = false;
                    private bool IsActivated
                    {
                        get => m_isActivated;
                        set
                        {
                            m_isActivated = value;
                            Image = IsActivated ? ImageActive : ImageInactive;
                        }
                    }

                    public DockStyle HitTest(Point pt)
                    {
                        return this.Visible && ClientRectangle.Contains(PointToClient(pt)) ? DockStyle : DockStyle.None;
                    }
                }
                #endregion PanelIndicator

                #region PaneIndicator
                private class PaneIndicator : PictureBox, IHitTest
                {
                    private struct HotSpotIndex
                    {
                        public HotSpotIndex(int x, int y, DockStyle dockStyle)
                        {
                            m_x = x;
                            m_y = y;
                            m_dockStyle = dockStyle;
                        }

                        private readonly int m_x;
                        public int X => m_x;

                        private readonly int m_y;
                        public int Y => m_y;

                        private readonly DockStyle m_dockStyle;
                        public DockStyle DockStyle => m_dockStyle;
                    }

                    private static readonly Bitmap _bitmapPaneDiamond = ScaleHelper.Scale(Resources.DockIndicator_PaneDiamond);
                    private static readonly Bitmap _bitmapPaneDiamondLeft = ScaleHelper.Scale(Resources.DockIndicator_PaneDiamond_Left);
                    private static readonly Bitmap _bitmapPaneDiamondRight = ScaleHelper.Scale(Resources.DockIndicator_PaneDiamond_Right);
                    private static readonly Bitmap _bitmapPaneDiamondTop = ScaleHelper.Scale(Resources.DockIndicator_PaneDiamond_Top);
                    private static readonly Bitmap _bitmapPaneDiamondBottom = ScaleHelper.Scale(Resources.DockIndicator_PaneDiamond_Bottom);
                    private static readonly Bitmap _bitmapPaneDiamondFill = ScaleHelper.Scale(Resources.DockIndicator_PaneDiamond_Fill);
                    private static readonly Bitmap _bitmapPaneDiamondHotSpot = ScaleHelper.Scale(Resources.DockIndicator_PaneDiamond_Hotspot);
                    private static readonly Bitmap _bitmapPaneDiamondHotSpotIndex = Resources.DockIndicator_PaneDiamond_HotspotIndex;
                    private static readonly HotSpotIndex[] _hotSpots = new[]
            {
                new HotSpotIndex(1, 0, DockStyle.Top),
                new HotSpotIndex(0, 1, DockStyle.Left),
                new HotSpotIndex(1, 1, DockStyle.Fill),
                new HotSpotIndex(2, 1, DockStyle.Right),
                new HotSpotIndex(1, 2, DockStyle.Bottom)
            };
                    private static readonly GraphicsPath _displayingGraphicsPath = DrawHelper.CalculateGraphicsPathFromBitmap(_bitmapPaneDiamond);

                    public PaneIndicator()
                    {
                        SizeMode = PictureBoxSizeMode.AutoSize;
                        Image = _bitmapPaneDiamond;
                        Region = new Region(DisplayingGraphicsPath);
                    }

                    public static GraphicsPath DisplayingGraphicsPath => _displayingGraphicsPath;

                    public DockStyle HitTest(Point pt)
                    {
                        if (!Visible)
                            return DockStyle.None;

                        pt = PointToClient(pt);
                        if (!ClientRectangle.Contains(pt))
                            return DockStyle.None;

                        for (int i = _hotSpots.GetLowerBound(0); i <= _hotSpots.GetUpperBound(0); i++)
                        {
                            if (_bitmapPaneDiamondHotSpot.GetPixel(pt.X, pt.Y) == _bitmapPaneDiamondHotSpotIndex.GetPixel(_hotSpots[i].X, _hotSpots[i].Y))
                                return _hotSpots[i].DockStyle;
                        }

                        return DockStyle.None;
                    }

                    private DockStyle m_status = DockStyle.None;
                    public DockStyle Status
                    {
                        get => m_status;
                        set
                        {
                            m_status = value;
                            if (m_status == DockStyle.None)
                                Image = _bitmapPaneDiamond;
                            else if (m_status == DockStyle.Left)
                                Image = _bitmapPaneDiamondLeft;
                            else if (m_status == DockStyle.Right)
                                Image = _bitmapPaneDiamondRight;
                            else if (m_status == DockStyle.Top)
                                Image = _bitmapPaneDiamondTop;
                            else if (m_status == DockStyle.Bottom)
                                Image = _bitmapPaneDiamondBottom;
                            else if (m_status == DockStyle.Fill)
                                Image = _bitmapPaneDiamondFill;
                        }
                    }
                }
                #endregion PaneIndicator

                #region consts
                private readonly int _PanelIndicatorMargin = 10;
                #endregion

                private readonly DockDragHandler m_dragHandler;

                public DockIndicator(DockDragHandler dragHandler)
                {
                    m_dragHandler = dragHandler;
                    Controls.AddRange(new Control[] {
                        PaneDiamond,
                        PanelLeft,
                        PanelRight,
                        PanelTop,
                        PanelBottom,
                        PanelFill
                        });
                    Region = new Region(Rectangle.Empty);
                }

                private PaneIndicator m_paneDiamond = null;
                private PaneIndicator PaneDiamond
                {
                    get
                    {
                        if (m_paneDiamond is null)
                            m_paneDiamond = new PaneIndicator();

                        return m_paneDiamond;
                    }
                }

                private PanelIndicator m_panelLeft = null;
                private PanelIndicator PanelLeft
                {
                    get
                    {
                        if (m_panelLeft is null)
                            m_panelLeft = new PanelIndicator(DockStyle.Left);

                        return m_panelLeft;
                    }
                }

                private PanelIndicator m_panelRight = null;
                private PanelIndicator PanelRight
                {
                    get
                    {
                        if (m_panelRight is null)
                            m_panelRight = new PanelIndicator(DockStyle.Right);

                        return m_panelRight;
                    }
                }

                private PanelIndicator m_panelTop = null;
                private PanelIndicator PanelTop
                {
                    get
                    {
                        if (m_panelTop is null)
                            m_panelTop = new PanelIndicator(DockStyle.Top);

                        return m_panelTop;
                    }
                }

                private PanelIndicator m_panelBottom = null;
                private PanelIndicator PanelBottom
                {
                    get
                    {
                        if (m_panelBottom is null)
                            m_panelBottom = new PanelIndicator(DockStyle.Bottom);

                        return m_panelBottom;
                    }
                }

                private PanelIndicator m_panelFill = null;
                private PanelIndicator PanelFill
                {
                    get
                    {
                        if (m_panelFill is null)
                            m_panelFill = new PanelIndicator(DockStyle.Fill);

                        return m_panelFill;
                    }
                }

                private bool m_fullPanelEdge = false;
                public bool FullPanelEdge
                {
                    get => m_fullPanelEdge;
                    set
                    {
                        if (m_fullPanelEdge == value)
                            return;

                        m_fullPanelEdge = value;
                        RefreshChanges();
                    }
                }

                public DockDragHandler DragHandler => m_dragHandler;

                public DockPanel DockPanel => DragHandler.DockPanel;

                private DockPane m_dockPane = null;
                public DockPane DockPane
                {
                    get => m_dockPane;
                    internal set
                    {
                        if (m_dockPane == value)
                            return;

                        DockPane oldDisplayingPane = DisplayingPane;
                        m_dockPane = value;
                        if (oldDisplayingPane != DisplayingPane)
                            RefreshChanges();
                    }
                }

                private IHitTest m_hitTest = null;
                private IHitTest HitTestResult
                {
                    get => m_hitTest;
                    set
                    {
                        if (m_hitTest == value)
                            return;

                        if (m_hitTest != null)
                            m_hitTest.Status = DockStyle.None;

                        m_hitTest = value;
                    }
                }

                private DockPane DisplayingPane => ShouldPaneDiamondVisible() ? DockPane : null;

                private void RefreshChanges()
                {
                    Region region = new Region(Rectangle.Empty);
                    Rectangle rectDockArea = FullPanelEdge ? DockPanel.DockArea : DockPanel.DocumentWindowBounds;

                    rectDockArea = RectangleToClient(DockPanel.RectangleToScreen(rectDockArea));
                    if (ShouldPanelIndicatorVisible(DockState.DockLeft))
                    {
                        PanelLeft.Location = new Point(rectDockArea.X + _PanelIndicatorMargin, rectDockArea.Y + (rectDockArea.Height - PanelRight.Height) / 2);
                        PanelLeft.Visible = true;
                        region.Union(PanelLeft.Bounds);
                    }
                    else
                        PanelLeft.Visible = false;

                    if (ShouldPanelIndicatorVisible(DockState.DockRight))
                    {
                        PanelRight.Location = new Point(rectDockArea.X + rectDockArea.Width - PanelRight.Width - _PanelIndicatorMargin, rectDockArea.Y + (rectDockArea.Height - PanelRight.Height) / 2);
                        PanelRight.Visible = true;
                        region.Union(PanelRight.Bounds);
                    }
                    else
                        PanelRight.Visible = false;

                    if (ShouldPanelIndicatorVisible(DockState.DockTop))
                    {
                        PanelTop.Location = new Point(rectDockArea.X + (rectDockArea.Width - PanelTop.Width) / 2, rectDockArea.Y + _PanelIndicatorMargin);
                        PanelTop.Visible = true;
                        region.Union(PanelTop.Bounds);
                    }
                    else
                        PanelTop.Visible = false;

                    if (ShouldPanelIndicatorVisible(DockState.DockBottom))
                    {
                        PanelBottom.Location = new Point(rectDockArea.X + (rectDockArea.Width - PanelBottom.Width) / 2, rectDockArea.Y + rectDockArea.Height - PanelBottom.Height - _PanelIndicatorMargin);
                        PanelBottom.Visible = true;
                        region.Union(PanelBottom.Bounds);
                    }
                    else
                        PanelBottom.Visible = false;

                    if (ShouldPanelIndicatorVisible(DockState.Document))
                    {
                        Rectangle rectDocumentWindow = RectangleToClient(DockPanel.RectangleToScreen(DockPanel.DocumentWindowBounds));
                        PanelFill.Location = new Point(rectDocumentWindow.X + (rectDocumentWindow.Width - PanelFill.Width) / 2, rectDocumentWindow.Y + (rectDocumentWindow.Height - PanelFill.Height) / 2);
                        PanelFill.Visible = true;
                        region.Union(PanelFill.Bounds);
                    }
                    else
                        PanelFill.Visible = false;

                    if (ShouldPaneDiamondVisible())
                    {
                        Rectangle rect = RectangleToClient(DockPane.RectangleToScreen(DockPane.ClientRectangle));
                        PaneDiamond.Location = new Point(rect.Left + (rect.Width - PaneDiamond.Width) / 2, rect.Top + (rect.Height - PaneDiamond.Height) / 2);
                        PaneDiamond.Visible = true;
                        using (GraphicsPath graphicsPath = PaneIndicator.DisplayingGraphicsPath.Clone() as GraphicsPath)
                        {
                            Point[] pts = new[]
                        {
                            new Point(PaneDiamond.Left, PaneDiamond.Top),
                            new Point(PaneDiamond.Right, PaneDiamond.Top),
                            new Point(PaneDiamond.Left, PaneDiamond.Bottom)
                        };
                            using (Matrix matrix = new Matrix(PaneDiamond.ClientRectangle, pts))
                            {
                                graphicsPath.Transform(matrix);
                            }
                            region.Union(graphicsPath);
                        }
                    }
                    else
                        PaneDiamond.Visible = false;

                    Region = region;
                }

                private bool ShouldPanelIndicatorVisible(DockState dockState)
                {
                    if (!Visible)
                        return false;

                    if (DockPanel.DockWindows[dockState].Visible)
                        return false;

                    return DragHandler.DragSource.IsDockStateValid(dockState);
                }

                private bool ShouldPaneDiamondVisible()
                {
                    if (DockPane is null)
                        return false;

                    if (!DockPanel.AllowEndUserNestedDocking)
                        return false;

                    return DragHandler.DragSource.CanDockTo(DockPane);
                }

                public override void Show(bool bActivate)
                {
                    base.Show(bActivate);
                    Bounds = SystemInformation.VirtualScreen;
                    RefreshChanges();
                }

                public void TestDrop()
                {
                    Point pt = Control.MousePosition;
                    DockPane = DockHelper.PaneAtPoint(pt, DockPanel);

                    if (TestDrop(PanelLeft, pt) != DockStyle.None)
                        HitTestResult = PanelLeft;
                    else if (TestDrop(PanelRight, pt) != DockStyle.None)
                        HitTestResult = PanelRight;
                    else if (TestDrop(PanelTop, pt) != DockStyle.None)
                        HitTestResult = PanelTop;
                    else if (TestDrop(PanelBottom, pt) != DockStyle.None)
                        HitTestResult = PanelBottom;
                    else if (TestDrop(PanelFill, pt) != DockStyle.None)
                        HitTestResult = PanelFill;
                    else if (TestDrop(PaneDiamond, pt) != DockStyle.None)
                        HitTestResult = PaneDiamond;
                    else
                        HitTestResult = null;

                    if (HitTestResult != null)
                    {
                        if (HitTestResult is PaneIndicator)
                            DragHandler.Outline.Show(DockPane, HitTestResult.Status);
                        else
                            DragHandler.Outline.Show(DockPanel, HitTestResult.Status, FullPanelEdge);
                    }
                }

                private static DockStyle TestDrop(IHitTest hitTest, Point pt)
                {
                    return hitTest.Status = hitTest.HitTest(pt);
                }

                private static Rectangle GetAllScreenBounds()
                {
                    Rectangle rect = new Rectangle(0, 0, 0, 0);
                    foreach (Screen screen in Screen.AllScreens)
                    {
                        Rectangle rectScreen = screen.Bounds;
                        if (rectScreen.Left < rect.Left)
                        {
                            rect.Width += (rect.Left - rectScreen.Left);
                            rect.X = rectScreen.X;
                        }
                        if (rectScreen.Right > rect.Right)
                            rect.Width += (rectScreen.Right - rect.Right);
                        if (rectScreen.Top < rect.Top)
                        {
                            rect.Height += (rect.Top - rectScreen.Top);
                            rect.Y = rectScreen.Y;
                        }
                        if (rectScreen.Bottom > rect.Bottom)
                            rect.Height += (rectScreen.Bottom - rect.Bottom);
                    }

                    return rect;
                }
            }

            private class DockOutline : DockOutlineBase
            {
                public DockOutline()
                {
                    m_dragForm = new DragForm();
                    SetDragForm(Rectangle.Empty);
                    DragForm.BackColor = SystemColors.ActiveCaption;
                    DragForm.Opacity = 0.5;
                    DragForm.Show(false);
                }

                readonly DragForm m_dragForm;
                private DragForm DragForm => m_dragForm;

                protected override void OnShow()
                {
                    CalculateRegion();
                }

                protected override void OnClose()
                {
                    DragForm.Close();
                }

                private void CalculateRegion()
                {
                    if (SameAsOldValue)
                        return;

                    if (!FloatWindowBounds.IsEmpty)
                        SetOutline(FloatWindowBounds);
                    else if (DockTo is DockPanel)
                        SetOutline(DockTo as DockPanel, Dock, (ContentIndex != 0));
                    else if (DockTo is DockPane)
                        SetOutline(DockTo as DockPane, Dock, ContentIndex);
                    else
                        SetOutline();
                }

                private void SetOutline()
                {
                    SetDragForm(Rectangle.Empty);
                }

                private void SetOutline(Rectangle floatWindowBounds)
                {
                    SetDragForm(floatWindowBounds);
                }

                private void SetOutline(DockPanel dockPanel, DockStyle dock, bool fullPanelEdge)
                {
                    Rectangle rect = fullPanelEdge ? dockPanel.DockArea : dockPanel.DocumentWindowBounds;
                    rect.Location = dockPanel.PointToScreen(rect.Location);
                    if (dock == DockStyle.Top)
                    {
                        int height = dockPanel.GetDockWindowSize(DockState.DockTop);
                        rect = new Rectangle(rect.X, rect.Y, rect.Width, height);
                    }
                    else if (dock == DockStyle.Bottom)
                    {
                        int height = dockPanel.GetDockWindowSize(DockState.DockBottom);
                        rect = new Rectangle(rect.X, rect.Bottom - height, rect.Width, height);
                    }
                    else if (dock == DockStyle.Left)
                    {
                        int width = dockPanel.GetDockWindowSize(DockState.DockLeft);
                        rect = new Rectangle(rect.X, rect.Y, width, rect.Height);
                    }
                    else if (dock == DockStyle.Right)
                    {
                        int width = dockPanel.GetDockWindowSize(DockState.DockRight);
                        rect = new Rectangle(rect.Right - width, rect.Y, width, rect.Height);
                    }
                    else if (dock == DockStyle.Fill)
                    {
                        rect = dockPanel.DocumentWindowBounds;
                        rect.Location = dockPanel.PointToScreen(rect.Location);
                    }

                    SetDragForm(rect);
                }

                private void SetOutline(DockPane pane, DockStyle dock, int contentIndex)
                {
                    if (dock != DockStyle.Fill)
                    {
                        Rectangle rect = pane.DisplayingRectangle;
                        if (dock == DockStyle.Right)
                            rect.X += rect.Width / 2;
                        if (dock == DockStyle.Bottom)
                            rect.Y += rect.Height / 2;
                        if (dock == DockStyle.Left || dock == DockStyle.Right)
                            rect.Width -= rect.Width / 2;
                        if (dock == DockStyle.Top || dock == DockStyle.Bottom)
                            rect.Height -= rect.Height / 2;
                        rect.Location = pane.PointToScreen(rect.Location);

                        SetDragForm(rect);
                    }
                    else if (contentIndex == -1)
                    {
                        Rectangle rect = pane.DisplayingRectangle;
                        rect.Location = pane.PointToScreen(rect.Location);
                        SetDragForm(rect);
                    }
                    else
                    {
                        using (GraphicsPath path = pane.TabStripControl.GetOutline(contentIndex))
                        {
                            RectangleF rectF = path.GetBounds();
                            Rectangle rect = new Rectangle((int)rectF.X, (int)rectF.Y, (int)rectF.Width, (int)rectF.Height);
                            using (Matrix matrix = new Matrix(rect, new[] { new Point(0, 0), new Point(rect.Width, 0), new Point(0, rect.Height) }))
                            {
                                path.Transform(matrix);
                            }
                            Region region = new Region(path);
                            SetDragForm(rect, region);
                        }
                    }
                }

                private void SetDragForm(Rectangle rect)
                {
                    DragForm.Bounds = rect;
                    if (rect == Rectangle.Empty)
                        DragForm.Region = new Region(Rectangle.Empty);
                    else if (DragForm.Region != null)
                        DragForm.Region = null;
                }

                private void SetDragForm(Rectangle rect, Region region)
                {
                    DragForm.Bounds = rect;
                    DragForm.Region = region;
                }
            }

            public DockDragHandler(DockPanel panel)
                : base(panel)
            {
            }

            public new IDockDragSource DragSource
            {
                get => base.DragSource as IDockDragSource;
                set => base.DragSource = value;
            }

            private DockOutlineBase m_outline;
            public DockOutlineBase Outline
            {
                get => m_outline;
                private set => m_outline = value;
            }

            private DockIndicator m_indicator;
            private DockIndicator Indicator
            {
                get => m_indicator;
                set => m_indicator = value;
            }

            private Rectangle m_floatOutlineBounds;
            private Rectangle FloatOutlineBounds
            {
                get => m_floatOutlineBounds;
                set => m_floatOutlineBounds = value;
            }

            public void BeginDrag(IDockDragSource dragSource)
            {
                DragSource = dragSource;

                if (!BeginDrag())
                {
                    DragSource = null;
                    return;
                }

                Outline = new DockOutline();
                Indicator = new DockIndicator(this);
                Indicator.Show(false);

                FloatOutlineBounds = DragSource.BeginDrag(StartMousePosition);
            }

            protected override void OnDragging()
            {
                TestDrop();
            }

            protected override void OnEndDrag(bool abort)
            {
                DockPanel.SuspendLayout(true);

                Outline.Close();
                Indicator.Close();

                EndDrag(abort);

                DockPanel.ResumeLayout(true, true);

                DragSource = null;
            }

            private void TestDrop()
            {
                Outline.FlagTestDrop = false;

                Indicator.FullPanelEdge = ((Control.ModifierKeys & Keys.Shift) != 0);

                if ((Control.ModifierKeys & Keys.Control) == 0)
                {
                    Indicator.TestDrop();

                    if (!Outline.FlagTestDrop)
                    {
                        DockPane pane = DockHelper.PaneAtPoint(Control.MousePosition, DockPanel);
                        if (pane != null && DragSource.IsDockStateValid(pane.DockState))
                            pane.TestDrop(DragSource, Outline);
                    }

                    if (!Outline.FlagTestDrop && DragSource.IsDockStateValid(DockState.Float))
                    {
                        FloatWindow floatWindow = DockHelper.FloatWindowAtPoint(Control.MousePosition, DockPanel);
                        floatWindow?.TestDrop(DragSource, Outline);
                    }
                }
                else
                    Indicator.DockPane = DockHelper.PaneAtPoint(Control.MousePosition, DockPanel);

                if (!Outline.FlagTestDrop)
                {
                    if (DragSource.IsDockStateValid(DockState.Float))
                    {
                        Rectangle rect = FloatOutlineBounds;
                        rect.Offset(Control.MousePosition.X - StartMousePosition.X, Control.MousePosition.Y - StartMousePosition.Y);
                        Outline.Show(rect);
                    }
                }

                if (!Outline.FlagTestDrop)
                {
                    Cursor.Current = Cursors.No;
                    Outline.Show();
                }
                else
                    Cursor.Current = DragControl.Cursor;
            }

            private void EndDrag(bool abort)
            {
                if (abort)
                    return;

                if (!Outline.FloatWindowBounds.IsEmpty)
                    DragSource.FloatAt(Outline.FloatWindowBounds);
                else if (Outline.DockTo is DockPane)
                {
                    DockPane pane = Outline.DockTo as DockPane;
                    DragSource.DockTo(pane, Outline.Dock, Outline.ContentIndex);
                }
                else if (Outline.DockTo is DockPanel)
                {
                    DockPanel panel = Outline.DockTo as DockPanel;
                    panel.UpdateDockWindowZOrder(Outline.Dock, Outline.FlagFullEdge);
                    DragSource.DockTo(panel, Outline.Dock);
                }
                if ((DragSource as DockContentHandler)?.Content is ITabbedDocument)
                {
                    ((DockContentHandler)DragSource).Content.DockHandler.Activate();
                }
            }
        }

        private DockDragHandler m_dockDragHandler = null;
        private DockDragHandler GetDockDragHandler()
        {
            if (m_dockDragHandler is null)
                m_dockDragHandler = new DockDragHandler(this);
            return m_dockDragHandler;
        }

        internal void BeginDrag(IDockDragSource dragSource)
        {
            GetDockDragHandler().BeginDrag(dragSource);
        }
    }
}
