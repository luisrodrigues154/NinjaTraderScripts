namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Plots sessions (Asia ; London ; New York) HL
	/// </summary>
	public class SessionsHL : Indicator
	{
		private DateTime currentDate;
		private string currentDayTagAsString;
		private TimeSpan lastCheckedTime;
		private TimeSpan AsiaOpen;
		private TimeSpan MidnightTime;
		private TimeSpan AsiaClose;
		private TimeSpan LondonOpen;
		private TimeSpan LondonClose;
		private TimeSpan NewYorkOpen;
		private TimeSpan NewYorkClose;
		private double currentHigh;
		private double currentLow;
		private Data.SessionIterator sessionIterator;
		private int barsAgoSession;
		private short currentSession;
		private bool newDay;


		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"An indicator to draw Sessions (Asia ; London ; New York) Highs and Lows. Ideally this is to be used alongside 'Prior Day HL' indicator. Defaults are to:  Dashed -> highs ; Solid -> lows . Reason: to avoid many colors when used in combination with said indicator";
				Name = "Sessions HL";
				IsAutoScale = false;
				IsOverlay = true;
				IsSuspendedWhileInactive = true;
				DrawOnPricePanel = true;
				ShowAsia = true;
				ShowLondon = true;
				ShowNewYork = true;
				Calculate = Calculate.OnBarClose;
				AsiaOpenInput = new TimeSpan(18, 30, 00);
				AsiaCloseInput = new TimeSpan(3, 30, 00);
				LondonOpenInput = new TimeSpan(3, 30, 00);
				LondonCloseInput = new TimeSpan(9, 30, 00);
				NewYorkOpenInput = new TimeSpan(9, 30, 00);
				NewYorkCloseInput = new TimeSpan(16, 00, 00);
				MidnightTime = new TimeSpan(0, 0, 0);
				currentHigh = double.MinValue;
				currentLow = double.MinValue;
				currentDate = Core.Globals.MinDate;
				ShowText = false;
				newDay = false;
				currentSession = 0;
				barsAgoSession = 0;
				sessionIterator = null;
			}
			else if (State == State.Configure)
			{
				// retrieve sessions' limits from user indicator panel
				if (AsiaOpenInput == MidnightTime && AsiaCloseInput == MidnightTime && LondonOpenInput == MidnightTime && LondonCloseInput == MidnightTime && NewYorkOpenInput == MidnightTime && NewYorkCloseInput == MidnightTime)
				{
					AsiaOpenInput = new TimeSpan(18, 30, 00);
					AsiaCloseInput = new TimeSpan(3, 30, 00);
					LondonOpenInput = new TimeSpan(3, 30, 00);
					LondonCloseInput = new TimeSpan(9, 30, 00);
					NewYorkOpenInput = new TimeSpan(9, 30, 00);
					NewYorkCloseInput = new TimeSpan(16, 00, 00);

				}
				else
				{
					AsiaOpen = AsiaOpenInput;
					AsiaClose = AsiaCloseInput;
					LondonOpen = LondonOpenInput;
					LondonClose = LondonCloseInput;
					NewYorkOpen = NewYorkOpenInput;
					NewYorkClose = NewYorkCloseInput;
				}


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
		private void drawLineAndText(string tagLine, double price, int barsAgo, Brush color, DashStyleHelper style, string text)
		{
			Draw.Line(this, tagLine, false, barsAgo, price, 0, price, color, style, 1, true);
			if (ShowText) Draw.Text(this, tagLine + "_text", text, 1, price);
		}
		protected override void OnBarUpdate()
		{
			if (!Bars.BarsType.IsIntraday)
				return;

			// If the current data is not the same date as the current bar then its a new session
			if (currentDate != sessionIterator.GetTradingDay(Time[0]))
			{
				// Initilize the current day settings to the new days data
				currentLow = Low[0];
				currentHigh = High[0];
				// new trading day starting
				currentDate = sessionIterator.GetTradingDay(Time[0]);
				currentDayTagAsString = currentDate.Date.ToString("yyyyMMdd");
				// set as new day, currentSession might have either 3 or 0, so its ok skipping init
				newDay = true;
			}
			else // The current day is the same day
			{
				currentHigh = Math.Max(currentHigh, High[0]);
				currentLow = Math.Min(currentLow, Low[0]);

				TimeSpan currentTime = Time[0].TimeOfDay;

				// this ensures OnEachTick correctness otherwise would count bars without new bars, duh
				if (currentTime != lastCheckedTime) barsAgoSession++;
				if (!ShowPriorDays && currentDate != DateTime.Today) return;

				if (currentTime >= AsiaOpen || (currentTime <= AsiaClose && currentTime >= MidnightTime) && ShowAsia)
				{
					if (currentSession != 1 || newDay)
					{
						currentSession = 1;
						// needs 0 otherwise starts a candle before
						barsAgoSession = 0;
						// Resets HL from previous day pass for the first candle of the session
						currentHigh = High[0];
						currentLow = Low[0];
						newDay = false;
					}

					string lineIdHigh = "AsiaHigh_" + currentDayTagAsString;
					drawLineAndText(lineIdHigh, currentHigh, barsAgoSession, Brushes.Yellow, DashStyleHelper.Dash, "Asia H");
					string lineIdLow = "AsiaLow_" + currentDayTagAsString;
					drawLineAndText(lineIdLow, currentLow, barsAgoSession, Brushes.Yellow, DashStyleHelper.Solid, "Asia L");


				}
				else if (currentTime >= LondonOpen && currentTime <= LondonClose && ShowLondon)
				{
					if (currentSession != 2)
					{
						currentSession = 2;
						barsAgoSession = 0;
						currentHigh = High[0];
						currentLow = Low[0];
					}

					string lineIdHigh = "LondonHigh_" + currentDayTagAsString;
					drawLineAndText(lineIdHigh, currentHigh, barsAgoSession, Brushes.LightSkyBlue, DashStyleHelper.Dash, "London H");
					string lineIdLow = "LondonLow_" + currentDayTagAsString;
					drawLineAndText(lineIdLow, currentLow, barsAgoSession, Brushes.LightSkyBlue, DashStyleHelper.Solid, "London L");


				}
				else if (currentTime >= NewYorkOpen && currentTime <= NewYorkClose && ShowNewYork)
				{
					if (currentSession != 3)
					{
						currentSession = 3;
						barsAgoSession = 0;
						currentHigh = High[0];
						currentLow = Low[0];
					}

					string lineIdHigh = "NewYorkHigh_" + currentDayTagAsString;
					drawLineAndText(lineIdHigh, currentHigh, barsAgoSession, Brushes.DarkCyan, DashStyleHelper.Dash, "NewYork H");
					string lineIdLow = "NewYorkLow_" + currentDayTagAsString;
					drawLineAndText(lineIdLow, currentLow, barsAgoSession, Brushes.Crimson, DashStyleHelper.Solid, "NewYorkL");
				}

				// storing for nextTick check
				lastCheckedTime = currentTime;
			}
		}

		#region Properties


		[Display(ResourceType = typeof(Custom.Resource), Name = "Show Asia", GroupName = "Parameters", Order = 1)]
		public bool ShowAsia
		{ get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "Asia open (HH:mm)", Description = "Asia session open time", GroupName = "Parameters", Order = 2)]
		public TimeSpan AsiaOpenInput { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "Asia close (HH:mm)", Description = "Asia session close time", GroupName = "Parameters", Order = 3)]
		public TimeSpan AsiaCloseInput { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "Show London", GroupName = "Parameters", Order = 4)]
		public bool ShowLondon
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Lodon open (HH:mm)", Description = "London session open time", GroupName = "Parameters", Order = 5)]
		public TimeSpan LondonOpenInput { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "London close (HH:mm)", Description = "London session close time", GroupName = "Parameters", Order = 6)]
		public TimeSpan LondonCloseInput { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "Show New York", GroupName = "Parameters", Order = 7)]
		public bool ShowNewYork
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "New York open (HH:mm)", Description = "New York session open time", GroupName = "Parameters", Order = 8)]
		public TimeSpan NewYorkOpenInput { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "New York close (HH:mm)", Description = "New York session close time", GroupName = "Parameters", Order = 9)]
		public TimeSpan NewYorkCloseInput { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "Show prior days", GroupName = "Parameters", Order = 10)]
		public bool ShowPriorDays
		{ get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "Show text on HL", GroupName = "Parameters", Order = 11)]
		public bool ShowText
		{ get; set; }
		#endregion

		public override string FormatPriceMarker(double price)
		{
			return Instrument.MasterInstrument.FormatPrice(price);
		}
	}
}