namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Plots price's extremes from prior day.
	/// </summary>
	public class PriorDayHL : Indicator
	{
		private DateTime currentDate = Core.Globals.MinDate;
		private DateTime previousDate = Core.Globals.MinDate;
		private double currentHigh = double.MinValue;
		private double currentLow = double.MinValue;
		private double priorDayHigh = double.MinValue;
		private double priorDayLow = double.MinValue;
		private bool skipFirst = true;
		private Data.SessionIterator sessionIterator;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"An indicator to draw prior day's Highs and Lows. Ideally this is to be used alongside 'Sessions HL' indicator. Defaults are to:  Dashed -> highs ; Solid -> lows . Reason: to avoid many colors when used in combination with said indicator";
				Name = "Prior day HL";
				IsAutoScale = false;
				IsOverlay = true;
				IsSuspendedWhileInactive = true;
				DrawOnPricePanel = false;
				ShowLow = true;
				ShowHigh = true;
				Calculate = Calculate.OnEachTick;

				AddPlot(new Stroke(Brushes.Plum, DashStyleHelper.Solid, 1), PlotStyle.Hash, "High");
				AddPlot(new Stroke(Brushes.Plum, DashStyleHelper.Solid, 1), PlotStyle.Square, "Low");

			}
			else if (State == State.Configure)
			{
				currentDate = Core.Globals.MinDate;
				previousDate = Core.Globals.MinDate;
				currentHigh = double.MinValue;
				currentLow = double.MinValue;
				priorDayHigh = double.MinValue;
				priorDayLow = double.MinValue;
				sessionIterator = null;
				skipFirst = true;
			}
			else if (State == State.DataLoaded)
			{
				sessionIterator = new Data.SessionIterator(Bars);
			}
			else if (State == State.Historical)
			{
				if (!Bars.BarsType.IsIntraday)
				{
					Draw.TextFixed(this, "NinjaScriptInfo", "ERR", TextPosition.BottomRight);
					Log("ERR", LogLevel.Error);
				}
			}
		}
		private void drawPreviousDayText(string tag, string text, double price)
		{
			if (previousDate == currentDate) return;
			Draw.Text(this, tag, text, 0, price);
		}
		protected override void OnBarUpdate()
		{
			if (!Bars.BarsType.IsIntraday)
				return;

			// If the current data is not the same date as the current bar then its a new session
			if (currentDate != sessionIterator.GetTradingDay(Time[0]) || currentLow == double.MinValue || currentHigh == double.MinValue)
			{
				// The current day OHLC values are now the prior days value so set
				// them to their respect indicator series for plotting
				priorDayHigh = currentHigh;
				priorDayLow = currentLow;

				// Initilize the current day settings to the new days data
				currentHigh = High[0];
				currentLow = Low[0];
				previousDate = currentDate;
				currentDate = sessionIterator.GetTradingDay(Time[0]);
				skipFirst = true;

			}
			else // The current day is the same day
			{
				// Set the current day HL values
				currentHigh = Math.Max(currentHigh, High[0]);
				currentLow = Math.Min(currentLow, Low[0]);
				if (skipFirst)
				{
					skipFirst = false;
					return;
				}
				if (ShowHigh)
				{
					string prevDayHighId = "high_" + previousDate.Date.ToString("yyyyMMdd");
					PriorHigh[0] = priorDayHigh;
					drawPreviousDayText(prevDayHighId, String.Format("{0} H", previousDate.Date.ToString("ddd")), priorDayHigh);
				}
				if (ShowLow)
				{
					string prevDayLowId = "Low_" + previousDate.Date.ToString("yyyyMMdd");
					PriorLow[0] = priorDayLow;
					drawPreviousDayText(prevDayLowId, String.Format("{0} L", previousDate.Date.ToString("ddd")), priorDayLow);
				}
			}
		}

		#region Properties

		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> PriorHigh
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> PriorLow
		{
			get { return Values[1]; }
		}

		[Display(ResourceType = typeof(Custom.Resource), Name = "Show High", GroupName = "NinjaScriptParameters", Order = 1)]
		public bool ShowHigh
		{ get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "Show Low", GroupName = "NinjaScriptParameters", Order = 2)]
		public bool ShowLow
		{ get; set; }
		#endregion

		public override string FormatPriceMarker(double price)
		{
			return Instrument.MasterInstrument.FormatPrice(price);
		}
	}
}
