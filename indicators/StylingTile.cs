// 
// Copyright (C) 2024, NinjaTrader LLC <ninjatrader@ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Gui;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Windows.Data;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
#endregion

public class StylingElement : DrawingTool
{
    public bool show;
    public string FullName;
    public string Name;
    private BitmapImage icon;
    public string DisplayName;
    private string iconBasePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaTrader 8/custom_resources/icons/");

    public StylingElement()
    {
        this.FullName = "DefaultFullName";
        this.Name = "DefaultName";
        this.DisplayName = "DefaultDisplayName";
    }

    public StylingElement(string FullName, string Name, string DisplayName, string fileName)
    {
        this.Name = Name;
        this.show = false;
        this.FullName = FullName;
        this.DisplayName = DisplayName;
        this.icon = new BitmapImage();
        this.icon.BeginInit();
        this.icon.UriSource = new Uri(iconBasePath + fileName);
        this.icon.EndInit();
    }

    public override object Icon
    {
        get
        {
            // Instantiate a Grid on which to place the image
            Grid myCanvas = new Grid { Height = 16, Width = 16 };

            // Instantiate an Image to place on the Grid
            Image image = new Image
            {
                Height = 16,
                Width = 16,
                Source = icon
            };

            // Add the image to the Grid
            myCanvas.Children.Add(image);

            return myCanvas;
        }
    }
}
namespace NinjaTrader.NinjaScript.Indicators
{
    [TypeConverter("NinjaTrader.NinjaScript.Indicators.StylingToolIndicatorTypeConverter")]
    [CategoryOrder(typeof(Custom.Resource), "NinjaScriptParameters", 1)]
    [CategoryOrder(typeof(Resource), "PropertyCategoryDataSeries", 2)]
    [CategoryOrder(typeof(Resource), "NinjaScriptSetup", 3)]
    //[CategoryOrder(typeof(Custom.Resource), "NinjaScriptDrawingTools", 4)]
    [CategoryOrder(typeof(Custom.Resource), "NinjaScriptIndicatorVisualGroup", 5)]
    //[CategoryExpanded(typeof(Custom.Resource), "NinjaScriptDrawingTools", false)]
    public class StylingTile : Indicator
    {
        private Border b;
        private Grid grid;
        private Thickness margin;
        private bool subscribedToSize;
        private Point startPoint;
        private DrawingTool selectedDrawing;
        public Dictionary<string, StylingElement> allActions;
        private ChartPanel chartPanel;
        private ChartScale chartScale;
        private ChartControl chartControl;
        protected override void OnBarUpdate()
        {
            if (!subscribedToSize && ChartPanel != null)
            {
                subscribedToSize = true;

                ChartPanel.SizeChanged += (o, e) =>
                {
                    if (grid == null || ChartPanel == null)
                        return;
                    if (grid.Margin.Left + grid.ActualWidth > ChartPanel.ActualWidth || grid.Margin.Top + grid.ActualHeight > ChartPanel.ActualHeight)
                    {
                        double left = Math.Max(0, Math.Min(grid.Margin.Left, ChartPanel.ActualWidth - grid.ActualWidth));
                        double top = Math.Max(0, Math.Min(grid.Margin.Top, ChartPanel.ActualHeight - grid.ActualHeight));
                        grid.Margin = new Thickness(left, top, 0, 0);
                        Left = left;
                        Top = top;
                    }
                };
            }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = @"Styling Tile";
                Description = Custom.Resource.DrawingToolIndicatorDescription;
                IsOverlay = true;
                IsChartOnly = true;
                DisplayInDataBox = false;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;
                SelectedTypes = new XElement("SelectedTypes");
                allActions = new Dictionary<string, StylingElement>(){
                    {"Template", new StylingElement("Template Selector", "Template", "Change Template", "template.png")},
                    {"Fill", new StylingElement("Fill Color Selector", "Fill", "Change fill color", "fill.png")},
                    {"BorderColor", new StylingElement("Border Color Selector", "BorderColor", "Change border color", "fill.png")},
                    {"BorderThickness", new StylingElement("Border thickness Selector", "BorderThickness", "Change border thickness", "fill.png")},
                    {"Midline", new StylingElement("Rectangles midline", "Midline", "Add rectangle midline", "midline.png")}
                };
                foreach (StylingElement se in new[]
                {
                    allActions["Template"],
                    allActions["Fill"],
                    allActions["Midline"]
                })
                {
                    XElement el = new XElement(se.Name);
                    el.Add(new XAttribute("Action", se.Name));
                    SelectedTypes.Add(el);
                }
                Left = 0;
                Top = -1;
                NumberOfRows = 1;


            }
            else if (State == State.DataLoaded)
            {
                ChartControl.MouseUp += OnChartMouseUp;
            }
            else if (State == State.Terminated)
            {
                ChartControl.MouseUp -= OnChartMouseUp;
            }
            else if (State == State.Historical)
            {
                if (IsVisible)
                {
                    if (ChartControl != null)
                    {
                        if (Top < 0)
                            Top = 40;

                        ChartControl.Dispatcher.InvokeAsync(() => { UserControlCollection.Add(CreateControl()); });
                    }
                }
            }
        }

        private void OnChartMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Check each drawing object on the chart after mouse up

            foreach (DrawingTool drawing in DrawObjects.ToList())
            {
                if (drawing.IsSelected && drawing != selectedDrawing)
                {
                    selectedDrawing = drawing;
                    grid.Visibility = Visibility.Visible;
                    try
                    {

                        foreach (ChartAnchor a in selectedDrawing.Anchors)
                        {
                            grid.Margin = new Thickness(this.chartControl.GetXByTime(a.Time), this.chartScale.GetYByValue(a.Price) - grid.ActualHeight - 5, 0, 0);
                            break;
                        }
                    }
                    catch (Exception es) { Print("Err: " + es.Message); }

                }
                else if (!drawing.IsSelected && drawing == selectedDrawing)
                {
                    selectedDrawing = null;
                    grid.Visibility = Visibility.Hidden;
                }
            }
        }
        private FrameworkElement CreateControl()
        {
            if (grid != null)
                return grid;

            grid = new Grid { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(Left, Top, 0, 0) };
            grid.Visibility = Visibility.Hidden;
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength() });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength() });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength() });

            Brush background = Application.Current.FindResource("BackgroundMainWindow") as Brush ?? Brushes.White;
            Brush borderBrush = Application.Current.FindResource("BorderThinBrush") as Brush ?? Brushes.Black;

            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });

            for (int r = 0; r < g.RowDefinitions.Count; r++)
            {
                System.Windows.Shapes.Ellipse e = new System.Windows.Shapes.Ellipse
                {
                    Width = 3,
                    Height = 3,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Fill = borderBrush
                };
                Grid.SetRow(e, r);
                g.Children.Add(e);
            }

            b = new Border
            {
                VerticalAlignment = VerticalAlignment.Top,
                BorderThickness = new Thickness(0, 1, 1, 1),
                BorderBrush = borderBrush,
                Background = background,
                Width = 12,
                Height = 24,
                Cursor = System.Windows.Input.Cursors.Hand,
                Child = g
            };

            b.MouseDown += (o, e) =>
            {
                startPoint = e.GetPosition(ChartPanel);
                margin = grid.Margin;
                if (e.ClickCount > 1)
                {
                    b.ReleaseMouseCapture();
                    ChartControl.OnIndicatorsHotKey(this, null);
                }
                else
                    b.CaptureMouse();
            };

            b.MouseUp += (o, e) => { b.ReleaseMouseCapture(); };

            b.MouseMove += (o, e) =>
            {
                if (!b.IsMouseCaptured || grid == null || ChartPanel == null)
                    return;

                Point newPoint = e.GetPosition(ChartPanel);
                grid.Margin = new Thickness
                {
                    Left = Math.Max(0, Math.Min(margin.Left + (newPoint.X - startPoint.X), ChartPanel.ActualWidth - grid.ActualWidth)),
                    Top = Math.Max(0, Math.Min(margin.Top + (newPoint.Y - startPoint.Y), ChartPanel.ActualHeight - grid.ActualHeight))
                };

                Left = grid.Margin.Left;
                Top = grid.Margin.Top;
            };

            Grid.SetColumn(b, 1);

            grid.Children.Add(b);

            Grid contentGrid = new Grid();
            List<XElement> elements = SortElements(XElement.Parse(SelectedTypes.ToString()));


            int column = 0;
            int count = 0;
            FontFamily fontFamily = Application.Current.Resources["IconsFamily"] as FontFamily;
            Style style = Application.Current.Resources["LinkButtonStyle"] as Style;
            Type type = Core.Globals.AssemblyRegistry.GetType("StylingElement");
            while (count < elements.Count)
            {
                if (contentGrid.ColumnDefinitions.Count <= column)
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                for (int j = 0; j < NumberOfRows && count < elements.Count; j++)
                {
                    if (contentGrid.RowDefinitions.Count <= j)
                        contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });


                    XElement element = elements[count];

                    try
                    {
                        DrawingTools.DrawingTool dt = type.Assembly.CreateInstance(type.FullName) as DrawingTools.DrawingTool;
                        StylingElement se = allActions[element.Attribute("Action").Value];
                        if (dt != null)
                        {
                            Button bb = new Button
                            {
                                Content = se.Icon ?? Gui.Tools.Icons.DrawPencil,
                                ToolTip = se.DisplayName,
                                Style = style,
                                FontFamily = fontFamily,
                                FontSize = 16,
                                FontStyle = FontStyles.Normal,
                                Margin = new Thickness(3),
                                Padding = new Thickness(3)
                            };

                            Grid.SetRow(bb, j);
                            Grid.SetColumn(bb, column);

                            bb.Click += (s, e) => OnStylingTileButtonClick(s, e, se);

                            contentGrid.Children.Add(bb);
                            //tb.Items.Add(bb);
                            count++;
                        }
                        else
                        {
                            elements.RemoveAt(j);
                            j--;
                        }
                    }
                    catch (Exception e)
                    {
                        elements.RemoveAt(j);
                        j--;
                        Cbi.Log.Process(typeof(Custom.Resource), "NinjaScriptTileError", new object[] { element.Name.ToString(), e }, LogLevel.Error, LogCategories.NinjaScript);
                    }
                }
                column++;
            }

            Border tileHolder = new Border
            {
                Cursor = System.Windows.Input.Cursors.Arrow,
                Background = Application.Current.FindResource("BackgroundMainWindow") as Brush,
                BorderThickness = new Thickness((double)(Application.Current.FindResource("BorderThinThickness") ?? 1)),
                BorderBrush = Application.Current.FindResource("BorderThinBrush") as Brush,
                Child = contentGrid
            };

            grid.Children.Add(tileHolder);

            if (IsVisibleOnlyFocused)
            {
                Binding binding = new Binding("IsActive") { Source = ChartControl.OwnerChart, Converter = Application.Current.FindResource("BoolToVisConverter") as IValueConverter };
                grid.SetBinding(UIElement.VisibilityProperty, binding);
            }

            return grid;
        }
        private void OnStylingTileButtonClick(object sender, RoutedEventArgs e, StylingElement se)
        {
            ContextMenu contextMenu = new ContextMenu();
            switch (se.Name)
            {
                case "Fill":
                    // Initialize the context menu
                    MenuItem header = new MenuItem { Header = "Fill color" };
                    // Add color picker menu items
                    MenuItem crimson = new MenuItem { Header = "Crimson" };
                    crimson.Click += (s, e) => processFinalAction(se, "Crimson");

                    MenuItem seaGreen = new MenuItem { Header = "SeaGreen" };
                    seaGreen.Click += (s, e) => processFinalAction(se, "SeaGreen");

                    MenuItem silver = new MenuItem { Header = "Silver" };
                    silver.Click += (s, e) => processFinalAction(se, "Silver");


                    // Add the color items to the context menu
                    contextMenu.Items.Add(header);
                    contextMenu.Items.Add(new Separator()); // Optional separator
                    contextMenu.Items.Add(crimson);
                    contextMenu.Items.Add(seaGreen);
                    contextMenu.Items.Add(silver);
                    ((Button)sender).ContextMenu = contextMenu;
                    contextMenu.IsOpen = true;
                    break;
                case "Template":
                    break;
                case "Midline":

                    if (!(selectedDrawing is Rectangle)) return;
                    // Draw.Line(chartPanel, selectedDrawing.Tag + "Line", 
                    DateTime startTime = DateTime.Now;
                    DateTime endTime = DateTime.Now;
                    double topPrice = 0;
                    double botPrice = 0;
                    int barsAgo = 0;
                    int i = 0;
                    foreach (ChartAnchor a in selectedDrawing.Anchors)
                    {
                        if (i++ == 0)
                        {
                            startTime = a.Time;
                            topPrice = a.Price;
                        }
                        else
                        {
                            endTime = a.Time;
                            botPrice = a.Price;
                            break;
                        }
                    }
                    // Draw.Line(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, string templateName)
                    double price = (topPrice + botPrice) / 2;
                    processFinalAction(se, new List<object>() { startTime, endTime, price });
                    break;
            }
        }

        private void processFinalAction(StylingElement se, object value)
        {
            switch (se.Name)
            {
                case "Fill":
                    string[] propertyNames = { "AreaBrush", "Fill", "StrokeBrush" };
                    if (selectedDrawing == null) return;
                    Type type = selectedDrawing.GetType();
                    foreach (var propertyName in propertyNames)
                    {
                        var property = type.GetProperty(propertyName);

                        if (property != null && property.CanWrite)
                        {
                            try
                            {
                                property.SetValue(selectedDrawing, new SolidColorBrush((Color)ColorConverter.ConvertFromString((string)value)));
                            }
                            catch (Exception ex)
                            {
                                Print("Exception: " + ex.Message);
                            }
                        }
                    }
                    break;
                case "Midline":
                    List<object> args = (List<object>)value;
                    double price = (double)args[2];

                    Draw.Line(this, selectedDrawing.Tag + "Line", false, (DateTime)args[0], price, (DateTime)args[1], price, Brushes.White, DashStyleHelper.Dash, 1);
                    break;
            }
            ChartControl.InvalidateVisual();
        }
        public override void CopyTo(NinjaScript ninjaScript)
        {
            StylingTile dti = ninjaScript as StylingTile;
            if (dti != null)
            {
                dti.Left = Left;
                dti.Top = Top;
            }
            base.CopyTo(ninjaScript);
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            this.chartScale = chartScale;
            this.chartPanel = chartPanel;
            this.chartControl = chartControl;


        }

        private List<XElement> SortElements(XElement elements)
        {
            string[] ordered =  {

                                };

            List<XElement> ret = new List<XElement>();
            foreach (string s in ordered)
            {
                XElement c = elements.Element(s);
                if (c != null)
                {
                    ret.Add(XElement.Parse(c.ToString()));
                    c.Remove();
                }
            }

            foreach (XElement element in elements.Elements())
                ret.Add(element);

            return ret;
        }

        #region Properties

        [Browsable(false)]
        public double Top { get; set; }
        [Browsable(false)]
        public double Left { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptIsVisibleOnlyFocused", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 499)]
        public bool IsVisibleOnlyFocused { get; set; }

        public XElement SelectedTypes { get; set; }
        [Range(1, int.MaxValue)]
        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptNumberOfRows", GroupName = "NinjaScriptParameters", Order = 0)]
        public int NumberOfRows { get; set; }
        #endregion
    }

    public class StylingToolPropertyDescriptor : PropertyDescriptor
    {
        private readonly string displayName;
        private readonly string name;
        private readonly int order;
        private readonly Type type;
        public override AttributeCollection Attributes
        {
            get
            {
                Attribute[] attr = new Attribute[1];
                attr[0] = new DisplayAttribute { Name = DisplayName, GroupName = Custom.Resource.NinjaScriptDrawingTools, Order = order };

                return new AttributeCollection(attr);
            }
        }

        public StylingToolPropertyDescriptor(string name, string displayName, int order, Type type) : base(name, null)
        {
            this.name = name;
            this.displayName = displayName;
            this.order = order;
            this.type = type;
        }

        public override Type ComponentType { get { return typeof(StylingTile); } }
        public override string DisplayName { get { return displayName; } }
        public override bool IsReadOnly { get { return false; } }
        public override string Name { get { return name; } }
        public override Type PropertyType { get { return typeof(bool); } }

        public override bool CanResetValue(object component) { return true; }
        public override bool ShouldSerializeValue(object component) { return true; }

        public override object GetValue(object component)
        {
            StylingTile c = component as StylingTile;

            return c != null && c.SelectedTypes.Element(Name) != null;
        }

        public override void ResetValue(object component) { }

        public override void SetValue(object component, object value)
        {
            StylingTile c = component as StylingTile;
            if (c == null)
                return;
            bool val = (bool)value;

            if (val && c.SelectedTypes.Element(Name) == null)
            {
                XElement toAdd = new XElement(Name);
                toAdd.Add(new XAttribute("Action", Name));
                c.SelectedTypes.Add(toAdd);
            }
            else if (!val && c.SelectedTypes.Element(Name) != null)
                c.SelectedTypes.Element(Name).Remove();
        }
    }

    public class StylingToolIndicatorTypeConverter : TypeConverter
    {
        public override bool GetPropertiesSupported(ITypeDescriptorContext context) { return true; }

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object component, Attribute[] attrs)
        {
            TypeConverter tc = component is IndicatorBase ? TypeDescriptor.GetConverter(typeof(IndicatorBase)) : TypeDescriptor.GetConverter(typeof(DrawingTools.DrawingTool));
            PropertyDescriptorCollection propertyDescriptorCollection = tc.GetProperties(context, component, attrs);

            if (propertyDescriptorCollection == null)
                return null;

            PropertyDescriptorCollection properties = new PropertyDescriptorCollection(null);

            foreach (PropertyDescriptor pd in propertyDescriptorCollection)
            {
                if (!pd.IsBrowsable || pd.IsReadOnly) continue;

                if (pd.Name == "IsAutoScale" || pd.Name == "DisplayInDataBox" || pd.Name == "MaximumBarsLookBack" || pd.Name == "Calculate"
                                                || pd.Name == "PaintPriceMarkers" || pd.Name == "Displacement" || pd.Name == "ScaleJustification")
                    continue;

                if (pd.Name == "SelectedTypes")
                {
                    int i = 1;
                    Type type = Core.Globals.AssemblyRegistry.GetType("StylingElement");
                    DrawingTools.DrawingTool tool = type.Assembly.CreateInstance(type.FullName) as DrawingTools.DrawingTool;
                    foreach (StylingElement se in new StylingElement[] {
                            new StylingElement("Template Selector", "Template", "Change Template", "template.png"),
                            new StylingElement("Fill Color Selector", "Fill", "Change fill color", "fill.png"),
                            new StylingElement("Rectangles midline", "Midline", "Add rectangle midline", "midline.png")
                            }
                    )
                    {

                        try
                        {
                            StylingToolPropertyDescriptor descriptor = new StylingToolPropertyDescriptor(se.Name, se.FullName, i, type);
                            properties.Add(descriptor);
                            i++;
                        }
                        catch (Exception es) { }
                    }

                    continue;
                }

                properties.Add(pd);
            }
            return properties;
        }
    }
}
