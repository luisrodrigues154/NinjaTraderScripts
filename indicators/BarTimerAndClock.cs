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
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class BarTimerAndClock : Indicator
    {
        private string errMsg = "BarTimeAndClock: Unable to process. (connection/data/time error?!)";
        TimeZoneInfo ninjaTraderTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        private DateTime now = Core.Globals.Now;
        private bool connected,
                                hasRealtimeData;
        private SessionIterator sessionIterator;

        private System.Windows.Threading.DispatcherTimer timer;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Adds current time to the already existing BarTimer indicator";
                Name = "BarTimerAndClock";
                Calculate = Calculate.OnEachTick;
                DrawOnPricePanel = false;
                IsChartOnly = true;
                IsOverlay = true;
                DisplayInDataBox = false;
                ShowClock = true;
                ShowClockSeconds = true;
                ShowBarTimer = true;
            }
            else if (State == State.Realtime)
            {
                if (timer == null && IsVisible)
                {
                    if (Bars.BarsType.IsTimeBased && Bars.BarsType.IsIntraday)
                    {
                        writeText(errMsg);
                    }
                    else
                        writeText("BarTimerAndClock: not intraday!");
                }
            }
            else if (State == State.Terminated)
            {
                if (timer == null)
                    return;

                timer.IsEnabled = false;
                timer = null;
            }
        }
        private void writeText(string text)
        {
            Draw.TextFixed(this, "NinjaScriptInfo", text, TextPosition.BottomRight, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
        }
        protected override void OnBarUpdate()
        {
            if (State == State.Realtime)
            {
                hasRealtimeData = true;
                connected = true;
            }
        }

        protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
        {
            if (connectionStatusUpdate.PriceStatus == ConnectionStatus.Connected
                && connectionStatusUpdate.Connection.InstrumentTypes.Contains(Instrument.MasterInstrument.InstrumentType)
                && Bars.BarsType.IsTimeBased
                && Bars.BarsType.IsIntraday)
            {
                connected = true;

                if (DisplayTime() && timer == null)
                {
                    ChartControl.Dispatcher.InvokeAsync(() =>
                    {
                        timer = new System.Windows.Threading.DispatcherTimer { Interval = new TimeSpan(0, 0, 1), IsEnabled = true };
                        timer.Tick += OnTimerTick;
                    });
                }
            }
            else if (connectionStatusUpdate.PriceStatus == ConnectionStatus.Disconnected)
                connected = false;
        }

        private bool DisplayTime()
        {
            return ChartControl != null
                    && Bars != null
                    && Bars.Instrument.MarketData != null
                    && IsVisible;
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            ForceRefresh();

            if (DisplayTime())
            {
                if (timer != null && !timer.IsEnabled)
                    timer.IsEnabled = true;


                if (connected)
                {
                    if (SessionIterator.IsInSession(Now, false, true))
                    {
                        if (hasRealtimeData)
                        {
                            string outputString = "";
                            if (ShowClock)
                            {

                                DateTime timeOffset = TimeZoneInfo.ConvertTime(DateTime.UtcNow, ninjaTraderTimeZone);
                                timeOffset = ninjaTraderTimeZone.IsDaylightSavingTime(timeOffset) ? timeOffset + new TimeSpan(01, 00, 00) : timeOffset;
                                if (ShowClockSeconds) outputString = string.Format("Clock: {0}", timeOffset.ToString("HH:mm:ss"));
                                else outputString = string.Format("Clock: {0}", timeOffset.ToString("HH:mm"));
                            }

                            writeText(outputString);
                        }
                        else
                            writeText(errMsg);
                    }
                    else
                        writeText(errMsg);
                }
                else
                {
                    writeText(errMsg);

                    if (timer != null)
                        timer.IsEnabled = false;
                }
            }
        }

        private SessionIterator SessionIterator
        {
            get
            {
                if (sessionIterator == null)
                    sessionIterator = new SessionIterator(Bars);
                return sessionIterator;
            }
        }

        private DateTime Now
        {
            get
            {
                now = (Cbi.Connection.PlaybackConnection != null ? Cbi.Connection.PlaybackConnection.Now : Core.Globals.Now);

                if (now.Millisecond > 0)
                    now = Core.Globals.MinDate.AddSeconds((long)Math.Floor(now.Subtract(Core.Globals.MinDate).TotalSeconds));

                return now;
            }
        }

        #region Properties

        [Display(ResourceType = typeof(Custom.Resource), Name = "Show BarTimer", GroupName = "NinjaScriptParameters", Order = 1)]
        public bool ShowBarTimer
        { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "Show Clock", GroupName = "NinjaScriptParameters", Order = 2)]
        public bool ShowClock
        { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "Show Seconds in clock", GroupName = "NinjaScriptParameters", Order = 3)]
        public bool ShowClockSeconds
        { get; set; }


        #endregion

    }
}
