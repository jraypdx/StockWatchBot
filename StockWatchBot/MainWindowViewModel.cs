using OxyPlot;
using OxyPlot.Series;
using StockWatchBot.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StockWatchBot
{
    class MainWindowViewModel
    {
        //public LineSeries VolumeModel { get; private set; }
        public List<DataPoint> Points { get; private set; }
        public string Title { get; private set; }
        public List<DataPoint> Points2 { get; private set; }
        public string Title2 { get; private set; }

        public string Message { get; set; }

        public MainWindowViewModel()
        {
            var analysis = new Analysis("SPY");
            //Task.Run(async () => await analysis.CompareOpenPositions());
            Task.Run(async () => await analysis.StatTest());

            //////////////// putting this on hold to work on analysis //////////////////////
            /*
            var spyTracker = new Trackers.Stock("SPY");
            spyTracker.RunNotifier();
            Message = "Sending notifications on channel: stock-notifications";
            */
            ////////////////////////////////////////////////////////////////////////////////

            //Console.WriteLine($"Start: {DateTime.Now}");
            //Points = new List<DataPoint>();
            //Points2 = new List<DataPoint>();
            //Title = "SPY";
            //var stock = new Trackers.Stock("SPY");
            //stock.SaveMinOpenSample();
            //Thread.Sleep(10000);
            // var task = stock.GetVolumeSample();
            //task.Wait();
            //var volumeList = task.Result;
            //stock.SaveVolumeSample();
            //var volData = File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), "SPYvol.txt"));
            //volData.Reverse();

            //var openData = File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), "SPYopen.txt"));
            //openData.Reverse();


            //Console.WriteLine($"After stock: {DateTime.Now}");

            //double count = 0;
            //foreach (var vd in volData)
            //{
            //    Points.Add(new DataPoint(count, Convert.ToDouble(vd)));
            //    count++;
            //}
            //count = 0;
            //foreach (var od in openData)
            //{
            //    Points2.Add(new DataPoint(count, Convert.ToDouble(od)));
            //    count++;
            //}

            //VolumeModel = new LineSeries { Title = "SPY Volume" };
            //int count = 0;
            //foreach (var v in volumeList)
            //{
            //    VolumeModel.Points.Add(new DataPoint(count, v));
            //    count++;
            //}

            //Console.WriteLine($"After plotting: {DateTime.Now}");
        }
    }
}
