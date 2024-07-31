# NinjaTraderScripts

Repository with scripts created by me, to help my intraday trading. 

These scripts were created and tested for **NinjaTrader 8**. The scripts will be divided according to their NT subcategory (i.e: indicator, drawing tool, etc).

They also may contain bugs, as i'm not that worried about just reloading the scripting engine if they happen as they are not intended for production and/or a selling product.

All the elements, if they require time context, consider NewYork timezone as the default and in-use timezone.

## Need a custom, tailored element to be coded?

- if you need a custom NinjaTrader 8 script, contact me and we can discuss it! 🙂

## Already made elements

### Indicators

1. [Open prices](https://github.com/luisrodrigues154/NinjaTraderScripts/blob/master/indicators/OpenPrices.cs)
    - Draws a **dashed** dark-gray line from NY midnight open (12 AM)
    - Draws a **solid** dark-gray line from NY pre-market open (8:30 AM)
    - Allows to:
        - Toggle each draw
        - Show prior days (default off, to ease hardware)
2. [Prior day H/L](https://github.com/luisrodrigues154/NinjaTraderScripts/blob/master/indicators/PriorDayHL.cs)
    - Draws a **dashed** pink line for prior day **High** 
    - Draws a **solid** pink line for prior day **Low**
    - Allows to:
        - Show text alongside High/Low line (i.e: Mon H/Mon L)
        - Change plot settings
        - Toggle which to show
3. [Sessions H/L](https://github.com/luisrodrigues154/NinjaTraderScripts/blob/master/indicators/SessionsHL.cs)
    - Draws a **dashed** line for **Highs**
    - Draws a **solid** line for **Lows**
    - Sessions hardcoded coloring:
        - Asia: Yellow
        - London: Blue
        - NewYork: Green (high)/Crimsom (low) 
    - Allows to:
        - Defining sessions hours (Defaults: Asia 6:30PM - 3:30AM ; London: 03:30-9:30AM ; NewYork: 9:30AM-4PM)
        - Toggle which sessions
        - Show prior days (default off, to ease hardware)
        - Show text alongside Highs/Lows 
4. [Time macros](https://github.com/luisrodrigues154/NinjaTraderScripts/blob/master/indicators/TimeMacros.cs)
    - Draws vertical dashed lines at different times
    - Times available (NY time)
        - 12AM
        - 8:30AM
        - 9:30AM
        - 9:50 - 10:10AM
        - 10:50 - 11:10AM
        - 12AM - 1PM
        - 3PM  - 4PM
    - Allows to:
        - Toggle each time
        - Disable all (without unticking desired, for backtesting easiness)
        - Show prior days (default off, to ease hardware)

### Maybe next

These are elements that i might do later, because they seem to add quality of life, but not made until now due to not being extremelly necessary

1. In chart position trading similar to tradingview (long/short, with red and green areas for easier trade management)

## Potential tweaks to currently made elements

- Change indicators that use simple line drawing to plots, allowing for greater user configuration (such as lines)