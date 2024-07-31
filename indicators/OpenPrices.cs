namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Plots the NY 12AM and 8:30AM open prices
	/// </summary>
	public class OpenPrices : Indicator
	{
		private DateTime currentDate = Core.Globals.MinDate;
		private TimeSpan openTime12 = new TimeSpan(0, 0, 00);
		private TimeSpan openTime830 = new TimeSpan(8, 30, 00);
		private double open12AMValue = double.MinValue;
		private double open830AMValue = double.MinValue;
		private double priorOpen12AMValue = double.MinValue;
		private double priorOpen830AMValue = double.MinValue;
		private DateTime lastDate = Core.Globals.MinDate;
		private SessionIterator sessionIterator;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Plots NY timezone opening prices for each day (12 AM and/or 8:30 AM)";
				Name = "Opening Prices (NY)";
				IsAutoScale = false;
				DrawOnPricePanel = false;
				IsOverlay = true;
				IsSuspendedWhileInactive = true;
				Calculate = Calculate.OnEachTick;
				Show12AM = true;
				Show8_30AM = true;
				BarsRequiredToPlot = 0;

				AddPlot(new Stroke(Brushes.Silver, DashStyleHelper.Solid, 1), PlotStyle.Hash, "12 AM");
				AddPlot(new Stroke(Brushes.Silver, DashStyleHelper.Solid, 1), PlotStyle.Square, "8:30 AM");

			}
			else if (State == State.Configure)
			{
				openTime12 = new TimeSpan(0, 0, 00);
				openTime830 = new TimeSpan(8, 30, 00);
				currentDate = Core.Globals.MinDate;
				open12AMValue = double.MinValue;
				open830AMValue = double.MinValue;
				priorOpen12AMValue = double.MinValue;
				priorOpen830AMValue = double.MinValue;
				lastDate = Core.Globals.MinDate;
				sessionIterator = null;
			}
			else if (State == State.DataLoaded)
			{
				sessionIterator = new SessionIterator(Bars);
			}
			else if (State == State.Historical)
			{
				if (!Bars.BarsType.IsIntraday)
				{
					Draw.TextFixed(this, "NinjaScriptInfo", "Error", TextPosition.BottomRight);
					Log("Error", LogLevel.Error);
				}
			}
		}

		protected override void OnBarUpdate()
		{
			if (!Bars.BarsType.IsIntraday) return;

			if (Time[0].TimeOfDay == openTime12) open12AMValue = Open[0];
			if (Time[0].TimeOfDay == openTime830) open830AMValue = Open[0];

			if (currentDate != sessionIterator.GetTradingDay(Time[0]))
			{
				open12AMValue = double.MinValue;
				open830AMValue = double.MinValue;
				currentDate = sessionIterator.GetTradingDay(Time[0]);
			}
			if (!ShowPriorDays && currentDate != DateTime.Today) return;
			if (Show8_30AM && (open830AMValue != double.MinValue)) Open8_30AM[0] = open830AMValue;
			if (Show12AM && (open12AMValue != double.MinValue)) Open12AM[0] = open12AMValue;

		}

		#region Properties
		[Browsable(false)]  // this line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore()]       // this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public Series<double> Open12AM
		{
			get { return Values[0]; }
		}
		[Browsable(false)]  // this line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore()]       // this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public Series<double> Open8_30AM
		{
			get { return Values[1]; }
		}


		[Display(ResourceType = typeof(Custom.Resource), Name = "Show 12AM open", GroupName = "NinjaScriptParameters", Order = 1)]
		public bool Show12AM
		{ get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "Show 8:30AM open", GroupName = "NinjaScriptParameters", Order = 2)]
		public bool Show8_30AM
		{ get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "Show prior days", GroupName = "NinjaScriptParameters", Order = 3)]
		public bool ShowPriorDays
		{ get; set; }

		#endregion

		public override string FormatPriceMarker(double price)
		{
			return Instrument.MasterInstrument.FormatPrice(price);
		}
	}
}