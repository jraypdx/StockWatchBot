using AlphaVantage.Net.Core;
using AlphaVantage.Net.Stocks;
using AlphaVantage.Net.Stocks.TimeSeries;
using OxyPlot;
using StockWatchBot.API;
using StockWatchBot.Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StockWatchBot.Trackers
{
    class Stock
    {
        public string Ticker;
        public TimeSpan MarketOpen = new TimeSpan(6, 30, 0); //Pacific time (9:30am EST)
        public TimeSpan MarketClose = new TimeSpan(13, 0, 0); //Pacific time (4:00pm EST)
        public AlphaVantageStocksClient APIObj;
        public StockTimeSeries StockObj;

        public Stock(string tick)
        {
            Ticker = tick;
            APIObj = new AlphaVantageStocksClient(APIKey.key);
        }

        //don't use? save actual day object instead?
        public async Task UpdateDayFiles() //move to Stock.cs eventually
        {
            StockObj = await APIObj.RequestIntradayTimeSeriesAsync(Ticker, AlphaVantage.Net.Stocks.TimeSeries.IntradayInterval.OneMin, AlphaVantage.Net.Stocks.TimeSeries.TimeSeriesSize.Full);

            DateTime lastDate = DateTime.Now.Date;//StockObj.DataPoints.First().Time.Date;
            foreach (var sdp in StockObj.DataPoints)
            {
                if (sdp.Time.Date != lastDate)
                {
                    lastDate = sdp.Time.Date;
                    //if (JSONDay)
                }
            }
        }

        public async Task RunNotifier()
        {
            var notification = new NotificationServer();
            List<StockDataPoint> dataPoints = new List<StockDataPoint>();
            decimal alertOver = 290; //used to send notifications when passes certain price
            decimal alertUnder = 284;

            if (DateTime.Now.TimeOfDay < MarketOpen) // wait for market to open
            {
                var waitTime = MarketOpen - DateTime.Now.TimeOfDay;
                await Task.Delay(waitTime);
            }

            while (DateTime.Now.TimeOfDay < MarketClose)
            {
                var data = await APIObj.RequestDailyTimeSeriesAsync(Ticker);
                var latest = data.DataPoints.First();
                dataPoints.Add(latest);

                ///////////////////////////TESTING
                //notification.Publish("Test", "test notification");
                ///////////////////////////TESTING


                if (DateTime.Now.TimeOfDay.Minutes == 0)
                {
                    notification.Publish($"Hourly {Ticker} update", $"{latest.ClosingPrice}");
                }
                if (latest.ClosingPrice > alertOver)
                {
                    notification.Publish($"{Ticker} alert", $"{latest.ClosingPrice} has passed alertOver:{alertOver}");
                    alertOver = 999; //so that it gets ignored on future passes
                }
                if (latest.ClosingPrice < alertUnder)
                {
                    notification.Publish($"{Ticker} alert", $"{latest.ClosingPrice} has dropped below alertUnder:{alertUnder}");
                    alertUnder = 0; //so that it gets ignored on future passes
                }

                await Task.Delay(TimeSpan.FromSeconds(60));
            }

            //one last check for market closed
            var lastData = await APIObj.RequestDailyTimeSeriesAsync(Ticker);
            notification.Publish("Market closed", $"{Ticker} final price: {lastData.DataPoints.First().ClosingPrice}");
        }

        public async void PrintSample()
        {
            var test = new AlphaVantageStocksClient(APIKey.key);
            var data = await test.RequestIntradayTimeSeriesAsync(Ticker, AlphaVantage.Net.Stocks.TimeSeries.IntradayInterval.OneMin, AlphaVantage.Net.Stocks.TimeSeries.TimeSeriesSize.Full);
            
            using (StreamWriter f = new StreamWriter("SPY.txt"))
            {
                foreach (var d in data.DataPoints)
                {
                    Console.WriteLine($"{Ticker} H:{d.HighestPrice} L:{d.LowestPrice} Diff:{d.HighestPrice - d.LowestPrice} Volume: {d.Volume}");
                    //f.WriteLine($"{Ticker} {d.Time}  Open:{d.OpeningPrice}  H:{d.HighestPrice}  L:{d.LowestPrice}  Diff:{d.HighestPrice - d.LowestPrice}  Volume: {d.Volume}");
                }
            }
        }

        public async void SaveVolumeSample()
        {
            var test = new AlphaVantageStocksClient(APIKey.key);
            var data = await test.RequestIntradayTimeSeriesAsync(Ticker, AlphaVantage.Net.Stocks.TimeSeries.IntradayInterval.OneMin, AlphaVantage.Net.Stocks.TimeSeries.TimeSeriesSize.Full);

            using (StreamWriter f = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), "SPYvol.txt")))
            {
                foreach (var d in data.DataPoints)
                {
                    //Console.WriteLine($"{Ticker} H:{d.HighestPrice} L:{d.LowestPrice} Diff:{d.HighestPrice - d.LowestPrice} Volume: {d.Volume}");
                    f.WriteLine($"{d.Volume}");
                }
            }
        }

        public async void SaveMinOpenSample()
        {
            var test = new AlphaVantageStocksClient(APIKey.key);
            var data = await test.RequestIntradayTimeSeriesAsync(Ticker, AlphaVantage.Net.Stocks.TimeSeries.IntradayInterval.OneMin, AlphaVantage.Net.Stocks.TimeSeries.TimeSeriesSize.Full);
            
            using (StreamWriter f = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), "SPYopen.txt")))
            {
                foreach (var d in data.DataPoints)
                {
                    //Console.WriteLine($"{Ticker} H:{d.HighestPrice} L:{d.LowestPrice} Diff:{d.HighestPrice - d.LowestPrice} Volume: {d.Volume}");
                    f.WriteLine($"{d.OpeningPrice}");
                }
            }
        }


        public async Task<List<int>> GetVolumeSample() //Get the volume for the last 5 trading days by minute?  And reverse it so it's in order from oldest to newest
        {
            Console.WriteLine($"GetVolumeSample: {DateTime.Now}");
            var outList = new List<int>();
            var APIObj = new AlphaVantageStocksClient(APIKey.key);
            var data = await APIObj.RequestIntradayTimeSeriesAsync(Ticker, AlphaVantage.Net.Stocks.TimeSeries.IntradayInterval.OneMin, AlphaVantage.Net.Stocks.TimeSeries.TimeSeriesSize.Full);

            Console.WriteLine($"After data retrieved: {DateTime.Now}");

            //outList = data.DataPoints.Select(x => x.Volume);
            foreach (var min in data.DataPoints)
                outList.Add((int)min.Volume);

            Console.WriteLine($"After converted to list: {DateTime.Now}");

            outList.Reverse();

            Console.WriteLine($"After reversed: {DateTime.Now}");

            return outList;
        }
    }
}
