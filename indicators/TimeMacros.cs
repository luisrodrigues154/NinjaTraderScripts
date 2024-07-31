namespace NinjaTrader.NinjaScript.Indicators
{
	public class ICTMacros : Indicator
	{
		
		private DateTime currentDate;
		private string currentDateAsString;
		private	Data.SessionIterator	sessionIterator;
		private List<TimeSpan> timeMacros;            
		private bool currentDayDrawn;
		private bool showBacktest;
		
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"**Requires NY Timezone**. Draws a vertical line at NY session relevant time macros. All times are NY timezone";
				Name										= "Time Macros";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= false;
				
				IsSuspendedWhileInactive					= true;
				currentDate 								= DateTime.Today;
				currentDayDrawn 							= false;
				showBacktest 								= false;
				ShowBacktestInput 							= false;
				ShowMidnightInput 							= false;
				ShowOpenMarketInput						 	= false;
				ShowOpenEquitiesInput 						= true;
				ShowMacro1Input 							= true;
                ShowMacro2Input 							= true;
                ShowLunchInput                              = true;
				ShowAfternoonInput							= true;

			}
			else if (State == State.Configure){
				showBacktest = ShowBacktestInput;
				timeMacros = new List<TimeSpan>();
				if (!SwitchAllOff) {
					if (ShowMidnightInput) timeMacros.Add(new TimeSpan (0, 0, 0));
					if (ShowOpenMarketInput) timeMacros.Add(new TimeSpan (8, 30, 0));
					if (ShowOpenEquitiesInput) timeMacros.Add(new TimeSpan (9, 30, 0));
					if (ShowMacro1Input) { timeMacros.Add(new TimeSpan (9, 50, 0)); timeMacros.Add(new TimeSpan (10, 10, 0)); }
					if (ShowMacro2Input) { timeMacros.Add(new TimeSpan (10, 50, 0)); timeMacros.Add(new TimeSpan (11, 10, 0)); } 
					if (ShowLunchInput) { timeMacros.Add(new TimeSpan (12, 0, 0)); timeMacros.Add(new TimeSpan (13, 00, 0)); }
					if (ShowAfternoonInput) { timeMacros.Add(new TimeSpan (15, 0, 0)); timeMacros.Add(new TimeSpan (16, 00, 0)); }
				}
				
			}else if (State == State.DataLoaded)
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

		private void drawMacros(DateTime date) {
			for (ushort i = 0; i < timeMacros.Count; i++)
            {
				
				Draw.VerticalLine(this, string.Format("macro{0}_{1}", i, currentDateAsString ), currentDate.Add(timeMacros[i]), Brushes.DarkGray, DashStyleHelper.Dash, 1, false);
            }	
			currentDayDrawn = true;
		}
		protected override void OnBarUpdate(){
			if (!Bars.BarsType.IsIntraday)
				return;
			if(SwitchAllOff) return;
			
			if (showBacktest){
				if (currentDate != sessionIterator.GetTradingDay(Time[0])){
					// different day 
					currentDate = sessionIterator.GetTradingDay(Time[0]).Date;
					currentDateAsString = currentDate.Date.ToString("yyyyMMdd");
					currentDayDrawn = false;
				}
			}else {
				currentDate = DateTime.Today.Date;
				currentDateAsString = currentDate.Date.ToString("yyyyMMdd");
				
			}
			if(!currentDayDrawn) drawMacros(currentDate);
			
		}
	#region Properties
	
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show prior days", GroupName = "Parameters", Order = 1)]
		public bool ShowBacktestInput
		{ get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show 12 AM", GroupName = "Parameters", Order = 2)]
		public bool ShowMidnightInput
		{ get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show 8:30 AM", GroupName = "Parameters", Order = 3)]
		public bool ShowOpenMarketInput
		{ get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show 9:30 AM", GroupName = "Parameters", Order = 4)]
		public bool ShowOpenEquitiesInput
		{ get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show 9:50-10:10 AM", GroupName = "Parameters", Order = 5)]
		public bool ShowMacro1Input
		{ get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show 10:50-11:10 AM", GroupName = "Parameters", Order = 6)]
		public bool ShowMacro2Input
		{ get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show 12 AM - 1 PM", GroupName = "Parameters", Order = 7)]
		public bool ShowLunchInput
		{ get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show 3 PM - 4 PM", GroupName = "Parameters", Order = 8)]
		public bool ShowAfternoonInput
		{ get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Disable all", GroupName = "Parameters", Order = 9)]
		public bool SwitchAllOff
		{ get; set; }

	#endregion
}
