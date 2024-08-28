// 
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX.Direct2D1;
#endregion

//This namespace holds Drawing tools in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.DrawingTools
{
    /// <summary>
    /// Represents an interface that exposes information regarding a Risk Reward IDrawingTool.
    /// </summary>
    public class PlacePosition : DrawingTool
    {
        private const int cursorSensitivity = 15;
        private ChartAnchor editingAnchor;
        private double entryPrice;
        private bool needsRatioUpdate = true;
        private double ratio = 2;
        private double risk;
        private double reward;
        private double calculatedRR;
        private double stopPrice;
        private double targetPrice;
        private double textleftPoint;
        private double textRightPoint;

        private bool setInitialReward;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = Custom.Resource.NinjaScriptDrawingToolRiskRewardDescription;
                Name = "PositionPlacer";
                InitialRR = 2;
                AnchorLineStroke = new Stroke(Brushes.DarkGray, DashStyleHelper.Solid, 1f, 0);
                EntryLineStroke = new Stroke(Brushes.Goldenrod, DashStyleHelper.Solid, 2f);
                StopLineStroke = new Stroke(Brushes.Crimson, DashStyleHelper.Solid, 2f);
                TargetLineStroke = new Stroke(Brushes.SeaGreen, DashStyleHelper.Solid, 2f);
                EntryAnchor = new ChartAnchor { IsEditing = true, DrawingTool = this };
                RiskAnchor = new ChartAnchor { IsEditing = true, DrawingTool = this };
                RewardAnchor = new ChartAnchor { IsEditing = true, DrawingTool = this };
                EntryAnchor.DisplayName = Custom.Resource.NinjaScriptDrawingToolRiskRewardAnchorEntry;
                RiskAnchor.DisplayName = Custom.Resource.NinjaScriptDrawingToolRiskRewardAnchorRisk;
                RewardAnchor.DisplayName = Custom.Resource.NinjaScriptDrawingToolRiskRewardAnchorReward;
                ShowPoints = true;
                ShowRR = true;
                ShowCurrency = true;
                setInitialReward = false;
            }
            else if (State == State.Terminated)
                Dispose();
        }



        private void DrawShadedAreas(ChartPanel chartPanel, ChartControl chartControl, ChartScale chartScale)
        {
            Point riskP = RiskAnchor.GetPoint(chartControl, chartPanel, chartScale);
            Point rewardP = RewardAnchor.GetPoint(chartControl, chartPanel, chartScale);
            Point entryP = EntryAnchor.GetPoint(chartControl, chartPanel, chartScale);


            // draw risk 
            float minX = (float)Math.Min(Math.Min(riskP.X, entryP.X), rewardP.X);
            float maxX = (float)Math.Max(Math.Max(riskP.X, entryP.X), rewardP.X);
            float minY = (float)Math.Min(riskP.Y, entryP.Y);
            float maxY = (float)Math.Max(riskP.Y, entryP.Y);



            SharpDX.RectangleF rect = new SharpDX.RectangleF(minX, minY, maxX - minX, maxY - minY);
            // Create a SharpDX brush with the specified opacity

            SharpDX.Direct2D1.Brush crimsonBrush = Brushes.Crimson.ToDxBrush(RenderTarget);
            crimsonBrush.Opacity = 0.3f;
            RenderTarget.FillRectangle(rect, crimsonBrush);

            // draw reward 


            minY = (float)Math.Min(rewardP.Y, entryP.Y);
            maxY = (float)Math.Max(rewardP.Y, entryP.Y);

            // Create a SharpDX brush with the specified opacity

            SharpDX.Direct2D1.Brush seaGreenBrush = Brushes.SeaGreen.ToDxBrush(RenderTarget);
            seaGreenBrush.Opacity = 0.3f;
            RenderTarget.FillRectangle(new SharpDX.RectangleF(minX, minY, maxX - minX, maxY - minY), seaGreenBrush);


        }
        private void DrawPriceText(ChartAnchor anchor, Point point, double price, ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
        {
            if (TextAlignment == TextLocation.Off)
                return;

            string priceString;
            ChartBars chartBars = GetAttachedToChartBars();

            // bars can be null while chart is initializing
            if (chartBars == null)
                return;

            // NS can change ChartAnchor price via Draw method or directly meaning we needed to resync price before drawing
            if (!IsUserDrawn)
                price = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(anchor.Price);

            updateBaseData(false);
            priceString = GetPriceString(price, chartBars);

            Stroke color;
            textleftPoint = RiskAnchor.GetPoint(chartControl, chartPanel, chartScale).X;
            textRightPoint = EntryAnchor.GetPoint(chartControl, chartPanel, chartScale).X;

            if (anchor == RewardAnchor) color = TargetLineStroke;
            else if (anchor == RiskAnchor) color = StopLineStroke;
            else if (anchor == EntryAnchor) color = EntryLineStroke;
            else color = AnchorLineStroke;

            SimpleFont wpfFont = chartControl.Properties.LabelFont ?? new SimpleFont();
            SharpDX.DirectWrite.TextFormat textFormat = wpfFont.ToDirectWriteTextFormat();
            textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
            textFormat.WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap;
            SharpDX.DirectWrite.TextLayout textLayout = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory, priceString, textFormat, chartPanel.H, textFormat.FontSize);

            if (RiskAnchor.Time <= EntryAnchor.Time)
            {
                switch (TextAlignment)
                {
                    case TextLocation.InsideLeft: point.X = textleftPoint; break;
                    case TextLocation.InsideRight: point.X = textRightPoint - textLayout.Metrics.Width; break;
                    case TextLocation.ExtremeLeft: point.X = textleftPoint; break;
                    case TextLocation.ExtremeRight: point.X = textRightPoint - textLayout.Metrics.Width; break;
                }

            }
            else if (RiskAnchor.Time >= EntryAnchor.Time)
                switch (TextAlignment)
                {
                    case TextLocation.InsideLeft: point.X = textRightPoint; break;
                    case TextLocation.InsideRight: point.X = textleftPoint - textLayout.Metrics.Width; break;
                    case TextLocation.ExtremeLeft: point.X = textRightPoint; break;
                    case TextLocation.ExtremeRight: point.X = textleftPoint - textLayout.Metrics.Width; break;
                }

            RenderTarget.DrawTextLayout(new SharpDX.Vector2((float)point.X, (float)point.Y), textLayout, color.BrushDX, SharpDX.Direct2D1.DrawTextOptions.NoSnap);
        }

        public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
        {
            return Anchors.Select(anchor => new AlertConditionItem
            {
                Name = anchor.DisplayName,
                ShouldOnlyDisplayName = true,
                Tag = anchor
            });
        }

        public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
        {
            switch (DrawingState)
            {
                case DrawingState.Building: return Cursors.Pen;
                case DrawingState.Moving: return IsLocked ? Cursors.No : Cursors.SizeAll;
                case DrawingState.Editing: return IsLocked ? Cursors.No : (editingAnchor == EntryAnchor ? Cursors.SizeNESW : Cursors.SizeNWSE);
                default:
                    // draw move cursor if cursor is near line path anywhere
                    Point entryAnchorPixelPoint = EntryAnchor.GetPoint(chartControl, chartPanel, chartScale);

                    // see if we are near an anchor right away. this is is cheap so no big deal to do often
                    ChartAnchor closest = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);

                    if (closest != null)
                        return IsLocked ? Cursors.Arrow : (closest == EntryAnchor ? Cursors.SizeNESW : Cursors.SizeNWSE);

                    Point stopAnchorPixelPoint = RiskAnchor.GetPoint(chartControl, chartPanel, chartScale);
                    Vector anchorsVector = stopAnchorPixelPoint - entryAnchorPixelPoint;

                    // see if the mouse is along one of our lines for moving
                    if (MathHelper.IsPointAlongVector(point, entryAnchorPixelPoint, anchorsVector, cursorSensitivity))
                        return IsLocked ? Cursors.Arrow : Cursors.SizeAll;

                    if (!DrawTarget)
                        return null;

                    Point targetPoint = RewardAnchor.GetPoint(chartControl, chartPanel, chartScale);
                    Vector targetToEntryVector = targetPoint - entryAnchorPixelPoint;
                    return MathHelper.IsPointAlongVector(point, entryAnchorPixelPoint, targetToEntryVector, cursorSensitivity) ? (IsLocked ? Cursors.Arrow : Cursors.SizeAll) : null;
            }
        }

        private string GetPriceString(double price, ChartBars chartBars)
        {
            string priceString = chartBars.Bars.Instrument.MasterInstrument.FormatPrice(price);
            double yValueEntry = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EntryAnchor.Price);
            double tickSize = AttachedTo.Instrument.MasterInstrument.TickSize;
            double pointValue = AttachedTo.Instrument.MasterInstrument.PointValue;

            if (ShowRR && yValueEntry == price)
            {
                if (calculatedRR != -1)
                    priceString = string.Format("{0} (RR: {1})", priceString, calculatedRR);
                else
                    priceString = priceString + "(RR: INF)";
            }
            else
            {
                if (ShowCurrency && ShowPoints)
                {
                    double points = Math.Round(Math.Abs(yValueEntry - price), 2);
                    string cost;

                    if (AttachedTo.Instrument.MasterInstrument.InstrumentType == InstrumentType.Forex)
                        cost = Core.Globals.FormatCurrency(AttachedTo.Instrument.MasterInstrument.RoundToTickSize(points / tickSize * (tickSize * pointValue * Account.All[0].ForexLotSize)));
                    else
                        cost = Core.Globals.FormatCurrency(AttachedTo.Instrument.MasterInstrument.RoundToTickSize(points) / tickSize * (tickSize * pointValue));

                    priceString = string.Format("{0} ({1} ; {2}p)", priceString, cost, points);
                }
                else if (!ShowCurrency && ShowPoints)
                {
                    double points = Math.Round(Math.Abs(yValueEntry - price), 2);

                    priceString = string.Format("{0} ({1}p)", priceString, points);
                }
                else if (ShowCurrency)
                {
                    double points = Math.Round(Math.Abs(yValueEntry - price), 2);
                    string cost;

                    if (AttachedTo.Instrument.MasterInstrument.InstrumentType == InstrumentType.Forex)
                        cost = Core.Globals.FormatCurrency(AttachedTo.Instrument.MasterInstrument.RoundToTickSize(points / tickSize * (tickSize * pointValue * Account.All[0].ForexLotSize)));
                    else
                        cost = Core.Globals.FormatCurrency(AttachedTo.Instrument.MasterInstrument.RoundToTickSize(points) / tickSize * (tickSize * pointValue));

                    priceString = string.Format("{0} ({1})", priceString, cost);
                }


            }
            return priceString;
        }

        public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
        {
            ChartPanel chartPanel = chartControl.ChartPanels[chartScale.PanelIndex];
            Point entryPoint = EntryAnchor.GetPoint(chartControl, chartPanel, chartScale);
            Point stopPoint = RiskAnchor.GetPoint(chartControl, chartPanel, chartScale);

            if (!DrawTarget)
                return new[] { entryPoint, stopPoint };

            Point targetPoint = RewardAnchor.GetPoint(chartControl, chartPanel, chartScale);
            return new[] { entryPoint, stopPoint, targetPoint };
        }

        public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
        {
            // dig up which anchor we are running on to determine line
            ChartAnchor chartAnchor = conditionItem.Tag as ChartAnchor;
            if (chartAnchor == null)
                return false;

            ChartPanel chartPanel = chartControl.ChartPanels[PanelIndex];
            double alertY = chartScale.GetYByValue(chartAnchor.Price);
            Point entryPoint = EntryAnchor.GetPoint(chartControl, chartPanel, chartScale);
            Point stopPoint = RiskAnchor.GetPoint(chartControl, chartPanel, chartScale);
            Point targetPoint = RewardAnchor.GetPoint(chartControl, chartPanel, chartScale);
            double anchorMinX = DrawTarget ? new[] { entryPoint.X, stopPoint.X, targetPoint.X }.Min() : new[] { entryPoint.X, stopPoint.X }.Min();
            double anchorMaxX = DrawTarget ? new[] { entryPoint.X, stopPoint.X, targetPoint.X }.Max() : new[] { entryPoint.X, stopPoint.X }.Max();
            double lineStartX = anchorMinX;
            double lineEndX = anchorMaxX;

            // first thing, if our smallest x is greater than most recent bar, we have nothing to do yet.
            // do not try to check Y because lines could cross through stuff
            double firstBarX = chartControl.GetXByTime(values[0].Time);
            double firstBarY = chartScale.GetYByValue(values[0].Value);

            if (lineEndX < firstBarX) // bars passed our drawing tool
                return false;

            Point lineStartPoint = new Point(lineStartX, alertY);
            Point lineEndPoint = new Point(lineEndX, alertY);

            Point barPoint = new Point(firstBarX, firstBarY);
            // NOTE: 'left / right' is relative to if line was vertical. it can end up backwards too
            MathHelper.PointLineLocation pointLocation = MathHelper.GetPointLineLocation(lineStartPoint, lineEndPoint, barPoint);
            // for vertical things, think of a vertical line rotated 90 degrees to lay flat, where it's normal vector is 'up'
            switch (condition)
            {
                case Condition.Greater: return pointLocation == MathHelper.PointLineLocation.LeftOrAbove;
                case Condition.GreaterEqual: return pointLocation == MathHelper.PointLineLocation.LeftOrAbove || pointLocation == MathHelper.PointLineLocation.DirectlyOnLine;
                case Condition.Less: return pointLocation == MathHelper.PointLineLocation.RightOrBelow;
                case Condition.LessEqual: return pointLocation == MathHelper.PointLineLocation.RightOrBelow || pointLocation == MathHelper.PointLineLocation.DirectlyOnLine;
                case Condition.Equals: return pointLocation == MathHelper.PointLineLocation.DirectlyOnLine;
                case Condition.NotEqual: return pointLocation != MathHelper.PointLineLocation.DirectlyOnLine;
                case Condition.CrossAbove:
                case Condition.CrossBelow:
                    Predicate<ChartAlertValue> predicate = v =>
                    {
                        double barX = chartControl.GetXByTime(v.Time);
                        double barY = chartScale.GetYByValue(v.Value);
                        Point stepBarPoint = new Point(barX, barY);
                        // NOTE: 'left / right' is relative to if line was vertical. it can end up backwards too
                        MathHelper.PointLineLocation ptLocation = MathHelper.GetPointLineLocation(lineStartPoint, lineEndPoint, stepBarPoint);
                        if (condition == Condition.CrossAbove)
                            return ptLocation == MathHelper.PointLineLocation.LeftOrAbove;
                        return ptLocation == MathHelper.PointLineLocation.RightOrBelow;
                    };
                    return MathHelper.DidPredicateCross(values, predicate);
            }
            return false;
        }

        public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
        {
            return DrawingState == DrawingState.Building || Anchors.Any(a => a.Time >= firstTimeOnChart && a.Time <= lastTimeOnChart);
        }

        public override void OnCalculateMinMax()
        {
            // It is important to set MinValue and MaxValue to the min/max Y values your drawing tool uses if you want it to support auto scale
            MinValue = double.MaxValue;
            MaxValue = double.MinValue;

            if (!IsVisible)
                return;

            // return min/max values only if something has been actually drawn
            if (Anchors.Any(a => !a.IsEditing))
                foreach (ChartAnchor anchor in Anchors)
                {
                    if (anchor.DisplayName == RewardAnchor.DisplayName && !DrawTarget)
                        continue;

                    MinValue = Math.Min(anchor.Price, MinValue);
                    MaxValue = Math.Max(anchor.Price, MaxValue);
                }
        }

        private void updateBaseData(bool fromRiskToInitial)
        {
            if (Anchors == null || AttachedTo == null) return;

            entryPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EntryAnchor.Price);
            stopPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RiskAnchor.Price);
            targetPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RewardAnchor.Price);

            // risk anchor
            if (fromRiskToInitial && !setInitialReward)
            {
                reward = (entryPrice - stopPrice) * InitialRR;
                targetPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(entryPrice + reward);
                RewardAnchor.Price = targetPrice;
                setInitialReward = false;
                RewardAnchor.IsEditing = false;
            }
            else
            {
                risk = Math.Abs(entryPrice - stopPrice);
                reward = Math.Abs(entryPrice - targetPrice);
                try
                {
                    calculatedRR = reward / risk;
                    calculatedRR = Math.Round(calculatedRR, 2);
                }
                catch (Exception)
                {
                    calculatedRR = -1;
                }
            }
        }

        public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
        {
            switch (DrawingState)
            {
                case DrawingState.Building:

                    if (EntryAnchor.IsEditing)
                    {
                        dataPoint.CopyDataValues(EntryAnchor);
                        dataPoint.CopyDataValues(RiskAnchor);
                        dataPoint.CopyDataValues(RewardAnchor);
                        EntryAnchor.IsEditing = false;
                        entryPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EntryAnchor.Price);
                        updateBaseData(false);
                    }
                    else if (RiskAnchor.IsEditing)
                    {
                        dataPoint.CopyDataValues(RiskAnchor);
                        RiskAnchor.IsEditing = false;
                        stopPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RiskAnchor.Price);
                        updateBaseData(true);
                    }
                    else if (RewardAnchor.IsEditing)
                    {
                        dataPoint.CopyDataValues(RewardAnchor);
                        RewardAnchor.IsEditing = false;
                        targetPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RewardAnchor.Price);
                        updateBaseData(false);
                    }

                    if (!EntryAnchor.IsEditing && !RiskAnchor.IsEditing && !RewardAnchor.IsEditing)
                    {
                        DrawingState = DrawingState.Normal;
                        IsSelected = false;
                    };
                    // if the anchors are no longer being edited, set the drawing state to normal and unselect the object

                    break;
                case DrawingState.Normal:
                    Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale);
                    //find which anchor has been clicked relative to the mouse point and make whichever anchor now editable
                    editingAnchor = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);
                    if (editingAnchor != null)
                    {
                        editingAnchor.IsEditing = true;
                        DrawingState = DrawingState.Editing;
                    }
                    else if (GetCursor(chartControl, chartPanel, chartScale, point) == null)
                        IsSelected = false; // missed
                    else
                        // didnt click an anchor but on a line so start moving
                        DrawingState = DrawingState.Moving;
                    break;
            }
        }

        public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
        {
            if (IsLocked && DrawingState != DrawingState.Building || !IsVisible)
                return;

            if (DrawingState == DrawingState.Building)
            {
                if (EntryAnchor.IsEditing)
                    dataPoint.CopyDataValues(EntryAnchor);
                else if (RiskAnchor.IsEditing)
                    dataPoint.CopyDataValues(RiskAnchor);
                else if (RewardAnchor.IsEditing)
                    dataPoint.CopyDataValues(RewardAnchor);
            }
            else if (DrawingState == DrawingState.Editing && editingAnchor != null)
            {
                dataPoint.CopyDataValues(editingAnchor);
                entryPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EntryAnchor.Price);
                stopPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RiskAnchor.Price);
                targetPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RewardAnchor.Price);
                updateBaseData(false);

            }
            else if (DrawingState == DrawingState.Moving)
            {
                foreach (ChartAnchor anchor in Anchors)
                    anchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
            }

            entryPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EntryAnchor.Price);
            stopPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RiskAnchor.Price);
            targetPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RewardAnchor.Price);
        }

        public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
        {
            //don't set anchors until we're done drawing
            if (DrawingState == DrawingState.Building)
                return;

            //set the drawing state back to normal when mouse is relased
            if (DrawingState == DrawingState.Editing || DrawingState == DrawingState.Moving)
                DrawingState = DrawingState.Normal;
            if (editingAnchor != null)
            {
                updateBaseData(false);
                editingAnchor.IsEditing = false;
            }
            editingAnchor = null;
        }

        public override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (!IsVisible)
                return;
            if (Anchors.All(a => a.IsEditing))
                return;

            // this will be true right away to fix a restoral issue, so check if we really want to set reward
            if (needsRatioUpdate && DrawTarget)
                updateBaseData(false);

            ChartPanel chartPanel = chartControl.ChartPanels[PanelIndex];
            Point entryPoint = EntryAnchor.GetPoint(chartControl, chartPanel, chartScale);
            Point stopPoint = RiskAnchor.GetPoint(chartControl, chartPanel, chartScale);
            Point targetPoint = RewardAnchor.GetPoint(chartControl, chartPanel, chartScale);

            AnchorLineStroke.RenderTarget = RenderTarget;
            EntryLineStroke.RenderTarget = RenderTarget;
            StopLineStroke.RenderTarget = RenderTarget;

            // first of all, turn on anti-aliasing to smooth out our line
            RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
            RenderTarget.DrawLine(entryPoint.ToVector2(), stopPoint.ToVector2(), AnchorLineStroke.BrushDX, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);

            double anchorMinX = DrawTarget ? new[] { entryPoint.X, stopPoint.X, targetPoint.X }.Min() : new[] { entryPoint.X, stopPoint.X }.Min();
            double anchorMaxX = DrawTarget ? new[] { entryPoint.X, stopPoint.X, targetPoint.X }.Max() : new[] { entryPoint.X, stopPoint.X }.Max();
            double lineStartX = anchorMinX;
            double lineEndX = anchorMaxX;

            SharpDX.Vector2 entryStartVector = new SharpDX.Vector2((float)lineStartX, (float)entryPoint.Y);
            SharpDX.Vector2 entryEndVector = new SharpDX.Vector2((float)lineEndX, (float)entryPoint.Y);
            SharpDX.Vector2 stopStartVector = new SharpDX.Vector2((float)lineStartX, (float)stopPoint.Y);
            SharpDX.Vector2 stopEndVector = new SharpDX.Vector2((float)lineEndX, (float)stopPoint.Y);

            // don't try and draw the target stuff until we have calculated the target
            SharpDX.Direct2D1.Brush tmpBrush = IsInHitTest ? chartControl.SelectionBrush : AnchorLineStroke.BrushDX;

            if (DrawTarget)
            {
                AnchorLineStroke.RenderTarget = RenderTarget;
                RenderTarget.DrawLine(entryPoint.ToVector2(), targetPoint.ToVector2(), tmpBrush, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);

                TargetLineStroke.RenderTarget = RenderTarget;
                SharpDX.Vector2 targetStartVector = new SharpDX.Vector2((float)lineStartX, (float)targetPoint.Y);
                SharpDX.Vector2 targetEndVector = new SharpDX.Vector2((float)lineEndX, (float)targetPoint.Y);

                tmpBrush = IsInHitTest ? chartControl.SelectionBrush : TargetLineStroke.BrushDX;
                RenderTarget.DrawLine(targetStartVector, targetEndVector, tmpBrush, TargetLineStroke.Width, TargetLineStroke.StrokeStyle);

                DrawPriceText(RewardAnchor, targetPoint, targetPrice, chartControl, chartPanel, chartScale);

                // if can draw target, all have been drawn (because target is only drawn on first MouseUp)
                DrawShadedAreas(chartPanel, chartControl, chartScale);

            }

            tmpBrush = IsInHitTest ? chartControl.SelectionBrush : EntryLineStroke.BrushDX;
            RenderTarget.DrawLine(entryStartVector, entryEndVector, tmpBrush, EntryLineStroke.Width, EntryLineStroke.StrokeStyle);



            DrawPriceText(EntryAnchor, entryPoint, entryPrice, chartControl, chartPanel, chartScale);

            tmpBrush = IsInHitTest ? chartControl.SelectionBrush : StopLineStroke.BrushDX;
            RenderTarget.DrawLine(stopStartVector, stopEndVector, tmpBrush, StopLineStroke.Width, StopLineStroke.StrokeStyle);
            DrawPriceText(RiskAnchor, stopPoint, stopPrice, chartControl, chartPanel, chartScale);
        }

        [Browsable(false)]
        private bool DrawTarget { get { return (RiskAnchor != null && !RiskAnchor.IsEditing) || (RewardAnchor != null && !RewardAnchor.IsEditing); } }

        [Display(Order = 1)]
        public ChartAnchor EntryAnchor { get; set; }
        [Display(Order = 2)]
        public ChartAnchor RiskAnchor { get; set; }
        [Browsable(false)]
        public ChartAnchor RewardAnchor { get; set; }

        public override object Icon { get { return Icons.DrawRiskReward; } }

        [Range(0, double.MaxValue)]
        [NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Initial RR", GroupName = "NinjaScriptGeneral", Order = 1)]
        public double InitialRR
        {
            get { return ratio; }
            set
            {
                if (ratio.ApproxCompare(value) == 0)
                    return;
                ratio = value;
                needsRatioUpdate = true;
            }
        }

        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolAnchor", GroupName = "NinjaScriptLines", Order = 5)]
        public Stroke AnchorLineStroke { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolRiskRewardLineStrokeEntry", GroupName = "NinjaScriptLines", Order = 6)]
        public Stroke EntryLineStroke { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolRiskRewardLineStrokeRisk", GroupName = "NinjaScriptLines", Order = 7)]
        public Stroke StopLineStroke { get; set; }
        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolRiskRewardLineStrokeReward", GroupName = "NinjaScriptLines", Order = 8)]
        public Stroke TargetLineStroke { get; set; }

        public override IEnumerable<ChartAnchor> Anchors { get { return new[] { EntryAnchor, RiskAnchor, RewardAnchor }; } }

        [Display(ResourceType = typeof(Custom.Resource), Name = "Text Alignment", GroupName = "NinjaScriptLines", Order = 1)]
        public TextLocation TextAlignment { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "Display points", GroupName = "NinjaScriptLines", Order = 2)]
        public bool ShowPoints { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "Display Risk:Reward", GroupName = "NinjaScriptLines", Order = 3)]
        public bool ShowRR { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "Display Currency", GroupName = "NinjaScriptLines", Order = 4)]
        public bool ShowCurrency { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolRulerYValueDisplayUnit", GroupName = "NinjaScriptGeneral", Order = 2)]
        public ValueUnit DisplayUnit { get; set; }

        public override bool SupportsAlerts { get { return true; } }

    }


}