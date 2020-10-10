using AlphaVantage.Net.Stocks;
using AlphaVantage.Net.Stocks.TimeSeries;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Cms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockWatchBot.Objects
{
    class Day //390 minutes in a trading day
    {
        //Prices
        //public decimal PreviousClose; //passed in
        public decimal Open; //from first/last
        public decimal Close;
        public decimal High; //calculated
        public decimal Low;
        public decimal DayAverage;
        public long Volume;

        //Comparisons - percentages
        //public decimal PreviousCloseOpenPct() { return ((Open / PreviousClose) - 1) * 100; }
        public decimal HighLowPct() { return ((High / Low) - 1) * 100; }
        public decimal HighOpenPct() { return ((High / Open) - 1) * 100; }
        public decimal HighClosePct() { return ((High / Close) - 1) * 100; }
        public decimal LowOpenPct() { return ((Low / Open) - 1) * 100; }
        public decimal LowClosePct() { return ((Low / Close) - 1) * 100; }

        //Comparisons - decimals
        //public decimal PreviousCloseOpenDiff() { return Open - PreviousClose; }
        public decimal HighLowDiff() { return High - Low; }
        public decimal HighOpenDiff() { return High - Open; }
        public decimal HighCloseDiff() { return High - Close; }
        public decimal LowOpenDiff() { return Low - Open; }
        public decimal LowCloseDiff() { return Low - Close; }
        public decimal OpenCloseDiff() { return Close - Open; }

        //Dates/times
        public DateTime OpenTime; //used to tell date
        public DateTime DayHighTime;
        public DateTime DayLowTime;

        //Lists
        public List<TimeSegment> Segments_5min; //78 total
        public List<TimeSegment> Segments_30min; //13 total
        public List<TimeSegment> Segments_65min; //6 total
        public List<TimeSegment> Segments_130min; //3 total
        //list of 12 highest and lowest volume 5 min segments

        //Stock objects
        public string Ticker;
        public List<StockDataPoint> DataPoints; //stores info for this day


        public Day(string ticker, List<StockDataPoint> dataPoints)
        {
            //set from passed in
            Ticker = ticker;
            DataPoints = dataPoints;
            //PreviousClose = previousClose;

            //set from DataPoints
            Open = DataPoints.Last().OpeningPrice;
            //if (PreviousClose <= 0) { PreviousClose = Open; } //if not passed in set to open, in future use API to get day before whatever is first day so that it can be used, or throw away first day??
            Close = DataPoints.First().ClosingPrice;
            OpenTime = DataPoints.Last().Time;

            //set with calculations
            decimal avg = 0;
            StockDataPoint high = DataPoints.Last();
            StockDataPoint low = DataPoints.Last();
            foreach (var sdp in DataPoints)
            {
                avg += sdp.ClosingPrice;
                Volume += sdp.Volume;
                if (sdp.ClosingPrice > high.ClosingPrice)
                    high = sdp;
                if (sdp.ClosingPrice < low.ClosingPrice)
                    low = sdp;

            }
            DayAverage = avg / DataPoints.Count;
            High = high.ClosingPrice;
            DayHighTime = high.Time;
            Low = low.ClosingPrice;
            DayLowTime = low.Time;

            /*
            //DEBUGGING output day to text file
            string dayFile = $"SPY_debug_dump_{OpenTime.ToString("yyyy-MM-dd")}.txt";
            Console.WriteLine(dayFile);
            if (!File.Exists(dayFile))
            {
                using (StreamWriter f = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), dayFile)))
                {
                    string header = $"{Ticker},  {OpenTime},  Total points:{DataPoints.Count}\n";
                    f.WriteLine(header);
                    foreach (var sdp in DataPoints)
                    {
                        string temp = $"Time:{sdp.Time}\tOpen:{sdp.OpeningPrice}\tClose:{sdp.ClosingPrice}\tVolume:{sdp.Volume}";
                        f.WriteLine(temp);
                    }
                }
            }
            */

            //set time segments with Datapoints
            Segments_130min = new List<TimeSegment>();
            Segments_65min = new List<TimeSegment>();
            Segments_30min = new List<TimeSegment>();
            Segments_5min = new List<TimeSegment>();
            var temp_130 = new List<TimeSlice>();
            var temp_65 = new List<TimeSlice>();
            var temp_30 = new List<TimeSlice>();
            var temp_5 = new List<TimeSlice>();
            TimeSlice temp;
            int i = 1;
            foreach (var sdp in DataPoints)
            {
                temp = new TimeSlice { Time = sdp.Time, Open = sdp.OpeningPrice, Close = sdp.ClosingPrice, Volume = sdp.Volume };
                temp_130.Add(temp);
                temp_65.Add(temp);
                temp_30.Add(temp);
                temp_5.Add(temp);

                if (i % 130 == 0 || sdp == DataPoints.Last())
                {
                    Segments_130min.Add(new TimeSegment(temp_130));
                    temp_130 = new List<TimeSlice>();
                }
                if (i % 65 == 0 || sdp == DataPoints.Last())
                {
                    Segments_65min.Add(new TimeSegment(temp_65));
                    temp_65 = new List<TimeSlice>();
                }
                if (i % 30 == 0 || sdp == DataPoints.Last())
                {
                    Segments_30min.Add(new TimeSegment(temp_30));
                    temp_30 = new List<TimeSlice>();
                }
                if (i % 5 == 0 || sdp == DataPoints.Last())
                {
                    Segments_5min.Add(new TimeSegment(temp_5));
                    temp_5 = new List<TimeSlice>();
                }

                i++;
            }
        }

        public void SaveJSON()
        {
            string JSONPath = @"C:\stockdata\" + Ticker + @"\days\";
            string JSONFile =  $"{Ticker}_{OpenTime.Date.ToString("yyyy-MM-dd")}.json";
            string filePath = Path.Combine(JSONPath, JSONFile);

            if (!Directory.Exists(JSONPath))
                Directory.CreateDirectory(JSONPath);

            if (!File.Exists(filePath))
                File.WriteAllText(filePath, JsonConvert.SerializeObject(this));
        }


        /// <summary>
        /// Converts hour:minute time in to stock market minutes (minutes open, minutes from 9:30am EST) - returns -1 for error
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns></returns>
        public int ConvertTimeToMinutes(int hour, int minutes)
        {
            if (hour < 9) //format to 24h for math
                hour += 12;

            int marketMinutes = ((hour - 9) * 60) + minutes - 30;

            if (marketMinutes < 0 || marketMinutes > 390) //Check that the time passed in is in market open bounds (9:30am - 4:00pm EST)
            {
                if (hour > 12) //format for output
                    hour -= 12;
                Console.WriteLine($"Invalid time given to ConvertTimeToMinutes.\n    {hour}:{minutes} is outside of market open hours (9:30am to 4:00pm EST)");
                return -1;
            }

            return marketMinutes;
        }

        /// <summary>
        /// Converts stock market minutes (minutes open) in to hour:minute time - returns "ERROR: message" for error
        /// </summary>
        /// <param name="minutes"></param>
        /// <returns></returns>
        public string ConvertMinutesToTime(int minutes)
        {
            if (minutes < 0 || minutes > 390) //Check that the time passed in is in market open bounds (9:30am - 4:00pm EST), 0 to 390 minutes
                return $"ERROR: {minutes} is an invalid number of market minutes and must be between 0 (open) and 390 (close).";

            int hour = 0;
            while (minutes >= 60) //Seperate the minutes in to hours and minutes
            {
                hour += 1;
                minutes -= 60;
            }

            hour += 9; //convert to base of 9:30am
            minutes += 30;

            if (minutes >= 60)
            {
                minutes -= 60;
                hour += 1;
            }
            if (hour > 12)
                hour -= 12;

            if (minutes < 10)
                return $"{hour}:0{minutes}"; //easier than looking up how to 0 pad lol
            return $"{hour}:{minutes}";
        }


        public void DebugDump()
        {
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //debugging, dump days info to text file
            string seg130 = "\n130 minute segments:\n";
            string seg65 = "65 minute segments:\n";
            string seg30 = "30 minute segments:\n";
            string seg5 = "5 minute segments:\n";
            foreach (var s in Segments_130min)
                seg130 += $"{s.Time}  \t{s.SegmentDirection},{s.SegmentDirectionStrength}\t\t{s.Change()}\t\t{s.ChangePct()}\n";
            foreach (var s in Segments_65min)
                seg65 += $"{s.Time}  \t{s.SegmentDirection},{s.SegmentDirectionStrength}\t\t{s.Change()}\t\t{s.ChangePct()}\n";
            foreach (var s in Segments_30min)
                seg30 += $"{s.Time}  \t{s.SegmentDirection},{s.SegmentDirectionStrength}\t\t{s.Change()}\t\t{s.ChangePct()}\n";
            foreach (var s in Segments_5min)
                seg5 += $"{s.Time}  \t{s.SegmentDirection},{s.SegmentDirectionStrength}\t\t{s.Change()}\t\t{s.ChangePct()}\n";

            string dumpFile = $"{Ticker}_{OpenTime.ToString("yyyy-MM-dd")}_dump.txt";
            using (StreamWriter f = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), dumpFile)))
            {
                string debugOutput = $"" +
                    $"{Ticker}\t{OpenTime.ToString("yyyy-MM-dd")}\n\n" +
                    $"Open: \t{Open}\nClose: \t{Close}\nHigh: \t{High} {DayHighTime}\nLow: \t{Low} {DayLowTime}\nAVG: \t{DayAverage}\nVol: \t{Volume}\n\n" +
                    $"HighOpenPct: \t{HighOpenPct()}\nLowOpenPct: \t{LowOpenPct()}\nHighClosePct: \t{HighClosePct()}\nLowClosePct: \t{LowClosePct()}\nHighLowPct: \t{HighLowPct()}\n\n" +
                    $"HighOpenDif: \t{HighOpenDiff()}\nLowOpenDif: \t{LowOpenDiff()}\nHighCloseDif: \t{HighCloseDiff()}\nLowCloseDif: \t{LowCloseDiff()}\nHgihLowDif: \t{HighLowDiff()}\nOpenCloseDiff: \t{OpenCloseDiff()}";

                f.WriteLine(debugOutput);
                f.WriteLine(seg130);
                f.WriteLine(seg65);
                f.WriteLine(seg30);
                f.WriteLine(seg5);
            }
            
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }

    }
}
/*
 * public decimal Open; //from first/last
        public decimal Close;
        public decimal High; //calculated
        public decimal Low;
        public decimal DayAverage;
        public long Volume;

        //Comparisons - percentages
        //public decimal PreviousCloseOpenPct() { return ((Open / PreviousClose) - 1) * 100; }
        public decimal HighOpenPct() { return ((High / Open) - 1) * 100; }
        public decimal HighClosePct() { return ((High / Close) - 1) * 100; }
        public decimal LowOpenPct() { return ((Low / Open) - 1) * 100; }
        public decimal LowClosePct() { return ((Low / Close) - 1) * 100; }

        //Comparisons - decimals
        //public decimal PreviousCloseOpenDiff() { return Open - PreviousClose; }
        public decimal HighLowDiff() { return High - Low; }
        public decimal HighOpenDiff() { return High - Open; }
        public decimal HighCloseDiff() { return High - Close; }
        public decimal LowOpenDiff() { return Low - Open; }
        public decimal LowCloseDiff() { return Low - Close; }

        public DateTime DayHighTime;
        public DateTime DayLowTime;
*/
