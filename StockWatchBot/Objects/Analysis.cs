using AlphaVantage.Net.Stocks;
using AlphaVantage.Net.Stocks.TimeSeries;
using StockWatchBot.API;
using StockWatchBot.ML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StockWatchBot.Objects
{
    class Analysis
    {
        //API stuff
        public AlphaVantageStocksClient APIObj;
        public StockTimeSeries StockObj;
        public string Ticker;
        public int DaysCount;

        //Lists
        public List<List<StockDataPoint>> APIData;
        public List<Day> Days;


        public Analysis(string ticker)
        {
            //Initialize API object
            Ticker = ticker;
            StockObj = null;
            Task.Run(async () => await InitAPIObj());
            while (StockObj == null) { Thread.Sleep(1000); }
            //DaysCount = StockObj.DataPoints.Count() / 390; //this assumes every trading day is a full 390 points, no early closures

            //Break API object in to days list
            APIData = new List<List<StockDataPoint>>();
            Days = new List<Day>();
            var dataPoints = StockObj.DataPoints;
            string tempDate = dataPoints.First().Time.Date.ToString();
            var tempList = new List<StockDataPoint>();
            foreach (var min in dataPoints)
            {
                if (min.Time.Date.ToString() != tempDate) //if a new day is reached, add the tempList and start a new one
                {
                    Days.Add(new Day(Ticker, tempList));
                    tempList = new List<StockDataPoint>();
                    tempDate = min.Time.Date.ToString();
                }

                tempList.Add(min);

                if (min == dataPoints.Last()) //handles adding the last day since it was cut off with it being in the first if statement
                    Days.Add(new Day(Ticker, tempList));
            }

            foreach (var day in Days)
            {
                day.DebugDump();
                day.SaveJSON();
            }
        }


        private async Task InitAPIObj()
        {
            APIObj = new AlphaVantageStocksClient(APIKey.key);
            StockObj = await APIObj.RequestIntradayTimeSeriesAsync(Ticker, AlphaVantage.Net.Stocks.TimeSeries.IntradayInterval.OneMin, AlphaVantage.Net.Stocks.TimeSeries.TimeSeriesSize.Full);
            
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //debugging, dump data to file
            /*string dumpFile = $"SPYDUMP_5-30-2020_11-02am.txt";
            if (!File.Exists(dumpFile))
            {
                using (StreamWriter f = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), dumpFile)))
                {
                    foreach (var sdp in StockObj.DataPoints)
                    {
                        string temp = $"Time:{sdp.Time}\tOpen:{sdp.OpeningPrice}\tClose:{sdp.ClosingPrice}\tVolume:{sdp.Volume}";
                        f.WriteLine(temp);
                    }
                }
            }*/
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        }

        public async Task CompareClosesToOpens()
        {
            var dict = new Dictionary<DateTime, decimal>();

            Days.Reverse(); //reverse so we start with oldest day
            decimal previousClose = 0;
            foreach (var day in Days)
            {
                if (previousClose == 0)
                    dict.Add(day.OpenTime, 0);
                else
                    dict.Add(day.OpenTime, (day.Open - previousClose));

                previousClose = day.Close;
            }
            Days.Reverse(); //reverse back for future use

            using (StreamWriter f = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), $"Analysis_{Ticker}_CompareClosesToOpens.txt")))
            {
                string header = $"{Ticker} - CompareClosesToOpens analysis\nDate\t\tDifference in open from last close\n";
                //Console.WriteLine(header);
                f.WriteLine(header);

                foreach (var key in dict.Keys)
                {
                    string temp = $"{key.Date.ToString("yyyy-MM-dd")}\t\t{dict[key]}";
                    //Console.WriteLine(temp);
                    f.WriteLine(temp);
                }
            }
        }

        public async Task StatTest()
        {
            var segList = new List<List<TimeSegment>>();

            foreach (var d in Days)
                segList.Add(d.Segments_5min);

            var test = new ML_Segment(Ticker, segList);
            test.ML_Analyze_Segments_Basic();
            test.Debug_Dump();
        }

        private async void CompareHighs()
        {
            //how close are each day's highest and lowest price, volume
            //percentage and average of how much usually drops during day and how much climbs (high/lows from days)
            var dict = new Dictionary<DateTime, decimal>();
        }

        private async void CompareLows()
        {
            //how close are each day's highest and lowest price, volume
            //percentage and average of how much usually drops during day and how much climbs (high/lows from days)

        }

        private async void CompareVolumes()
        {
            //how close are each day's highest and lowest price, volume

        }

        public async Task CompareOpenPositions()
        {
            
            //how many days open up, how many days open down, and percentages/chance for each, and average amount for both and for all opens together
            var dayObj = await APIObj.RequestDailyTimeSeriesAsync(Ticker, TimeSeriesSize.Full);
            var dayArray = dayObj.DataPoints.ToArray();
            
            //average last 10, 20, 50, 1000(4yrs) days
            int i = 0;
            decimal avg10, pct10, avg20, pct20, avg50, pct50, avg1000, pct1000;
            avg10 = pct10 = avg20 = pct20 = avg50 = pct50 = avg1000 = pct1000 = 0;
            int up10, down10, up20, down20, up50, down50, up1000, down1000;
            up10 = down10 = up20 = down20 = up50 = down50 = up1000 = down1000 = 0;
            
            while (i < 1000)
            {
                decimal diff = dayArray[i].OpeningPrice - dayArray[i + 1].ClosingPrice;
                
                if (i < 10)
                {
                    avg10 += diff;
                    if (diff < 0)
                        down10 += 1;
                    else if (diff > 0)
                        up10 += 1;
                }
                if (i < 20)
                {
                    avg20 += diff;
                    if (diff < 0)
                        down20 += 1;
                    else if (diff > 0)
                        up20 += 1;
                }
                if (i < 50)
                {
                    avg50 += diff;
                    if (diff < 0)
                        down50 += 1;
                    else if (diff > 0)
                        up50 += 1;
                }
                avg1000 += diff;
                if (diff < 0)
                    down1000 += 1;
                else if (diff > 0)
                    up1000 += 1;

                i++;
            }
            
            avg10 /= 10;
            avg20 /= 20;
            avg50 /= 50;
            avg1000 /= 1000;

            pct10 = up10 / 10;
            pct20 = up20 / 20;
            pct50 = up50 / 50;
            pct1000 = up1000 / 1000;
            
            using (StreamWriter f = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), $"Analysis_{Ticker}_CompareOpenPositions.txt")))
            {
                string header = $"{Ticker} - CompareOpenPositions analysis\n\n";
                //Console.WriteLine(header);
                f.WriteLine(header);

                f.WriteLine($"Avg last 10\nAVG:{avg10}\nUp:{up10}\tDown:{down10}\nPCT up:{pct10}\n");
                f.WriteLine($"Avg last 10\nAVG:{avg20}\nUp:{up20}\tDown:{down20}\nPCT up:{pct20}\n");
                f.WriteLine($"Avg last 10\nAVG:{avg50}\nUp:{up50}\tDown:{down50}\nPCT up:{pct50}\n");
                f.WriteLine($"Avg last 10\nAVG:{avg1000}\nUp:{up1000}\tDown:{down1000}\nPCT up:{pct1000}\n");
            }
            
        }

        private async void AvgLowHigh()
        {
            //average time/percent that the low and high are hit

        }

        private async void FormatSegments_30min()
        {
            //break down 30/60 minute segments in to up/down and pct for easier viewing/analysis (can print like excel doc, each day with hour segments on one line, then average at bottom)

        }


        private async void StrategyCloseToOpen()
        {
            //percentage/avg gain from buy close sell open strategy

        }

        private async void StrategyOpenToClose()
        {
            //percentage/avg gain from buy open sell close strategy

        }

        private async void StrategyLowToHigh()
        {
            //percentage/avg gain from buy low point sell high point (if possible to avg a time or pct to buy in and sell at) [im thinking if it hits a certain goal % down it "buys" then waits for certain gain percentage

        }






        //
        //TODO LIST
        //

        //simple, get SPY data (and any other interesting stocks, ZM, DIS, etc) at end of day and store in to json file for later
        //for API sake can make a list of tickers and then at end of day (ex 1:05pm, or when the notification part stops) run through the list with a 30 sec wait in between or something

        //then print to file all the info below

        //compare by days of week (esp patterns early monday and late friday), first few days of months vs last few days of month, day before or after holidays

        //data
        //break these down in to last 2 days, last 5 days, last 10 days, last 20 (if data for 20) [do this by storing for all days but letting me calculate/retrieve based off a set or custom number]

        //extra complicated stuff
        //predict where a day is heading?  look at if it opens up vs down if it closes up or down, look at volume for up/down segments, if up or down after open, if up or down right before close previous day, etc.

        //idea i really like
        //basic level, graph each day and add data points for high/low and time with diffs/%s etc.
        //high level, avg out each day (either by minute or segment) and add graph points like above
        //highest level?  keep a running average going after collecting data every day, so that I can look at both short term trends as well as long term (weeks, months, even years) trends like how much it grows on avg
        //  and if it does follow the small drop in morning, slow movement one direction during day, (tiny drop?) then pump near end of day or if that's just recent.  Graph results and have data like normal
    }
}
