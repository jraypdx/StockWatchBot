using StockWatchBot.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockWatchBot.ML
{
    //TODO
    //  - Create a way to load past data to add on to
    //  - Predict open and first bit of day based on end of last day
    //     - Then create a prediction model for the day (based on end of last day, and maybe first bit of morning)
    //     - Then update the model as the day progresses, trying to predict the next few minutes (each minute further, less accuracy)
    //  - For "full" prediction function...
    //     - Have it add to a list for each move what next move is, ex: (up5 next is up1, so find up5 and add an up1 to list)
    //     - Or even better look at next 3, 4, or even 5 points to predict what comes next (with higher wait the closer it is)
    //     - Ex 5 points has a list of objects each with 5 lists, and the objects in the lists have direction,strength,weight (10,8,6,4,2,etc.)

    class ML_Segment
    {
        public string Ticker;
        public List<List<TimeSegment>> Segments;
        public ML_Segment_Stats Stats;
        public List<List<ML_Segment_Node>> NodeList;

        public ML_Segment(string ticker, List<List<TimeSegment>> timeSegment)
        {
            Ticker = ticker;
            Segments = timeSegment;
            Stats = new ML_Segment_Stats();
            NodeList = new List<List<ML_Segment_Node>>();
        }

        public void ML_Analyze_Segments_Basic()
        {
            if (Segments.Count <= 1)
            {
                Console.WriteLine("Not enough data to process for ML_Analyze_Segments_Basic");
                return;
            }

            //Create a 5-chain-5 of the data points
            foreach (var segment in Segments)
            {
                var seg = segment.ToArray();
                int len = seg.Length;
                if (len < 20)
                {
                    Console.WriteLine("This method works best with at least 20 data points");
                    break;
                }

                var tempNodeList = new List<ML_Segment_Node>();

                for (int i = 0; i < len - 1; i++)
                {
                    ML_Segment_Node tempNode = new ML_Segment_Node(seg[i].SegmentDirection, Math.Abs(seg[i].SegmentDirectionStrength));
                    for (int a = 0; a < 5; a++)
                    {
                        ML_Segment_Node prev = null;
                        if (i - a > 0)
                        {
                            if (prev == null)
                            {
                                prev = tempNodeList.Last();
                            }
                            if (prev != null)
                            {
                                tempNode.Previous[a] = prev;
                                prev.Next[a] = tempNode;
                                prev = prev.GetFirstPrevious();
                            }
                        }
                    }
                    tempNodeList.Add(tempNode);
                }
                NodeList.Add(tempNodeList);
            }

            //Add to data for simple test calculations (just a weighted average)
            foreach(var nodeList in NodeList)
            {
                //Console.WriteLine($"nodeList");
                foreach (var node in nodeList)
                {
                    //Console.WriteLine($"\tnode: {node.Direction.ToString()} {Math.Abs(node.Strength)}");
                    ML_Segment_Vector tempVec;

                    if (node.Direction == Direction.Up)
                        tempVec = Stats.Up[Math.Abs(node.Strength)];
                    else if (node.Direction == Direction.Down)
                        tempVec = Stats.Down[Math.Abs(node.Strength)];
                    else //flat, none
                        tempVec = Stats.Flat;

                    int str = 5;
                    foreach (var n in node.Next)
                    {
                        if (n == null)
                            break;
                        
                        if (n.Direction == Direction.Up)
                            tempVec.Up[Math.Abs(n.Strength)] += str;
                        else if (n.Direction == Direction.Down)
                            tempVec.Down[Math.Abs(n.Strength)] += str;
                        else
                            tempVec.Flat += str;
                        tempVec.Count++;
                        str--;
                    }
                }
            }
        }

        public void ML_Analyze_Segments_Full()
        {

        }

        public void Debug_Dump()
        {
            using (StreamWriter f = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), $"Vector_Dump_{Ticker}_{DateTime.Now.ToFileTime()}.txt")))
            {
                string header = $"{Ticker} - ML_Segment debug dump\n";
                f.WriteLine(header);

                // UP
                for (int i = 1; i < 11; i++)
                {
                    try
                    {
                        f.WriteLine($"UP {i}");
                        ML_Segment_Vector tempVec = Stats.Up[i];

                        if (tempVec == null)
                            continue;

                        f.WriteLine($"\t{tempVec.Total_Average_Strength()}");
                    }
                    catch (Exception ex)
                    {
                        f.WriteLine($"{ex.Message}\n\t{ex.StackTrace}");
                    }
                }

                f.WriteLine("");

                // DOWN
                for (int i = 1; i < 11; i++)
                {
                    f.WriteLine($"DOWN {i}");
                    ML_Segment_Vector tempVec = Stats.Down[i];

                    if (tempVec == null)
                        continue;

                    f.WriteLine($"\t{tempVec.Total_Average_Strength()}");
                }

                f.WriteLine("");

                f.WriteLine($"FLAT\n\t{Stats.Flat.Total_Average_Strength()}");


                // UP
                /*for (int i = 0; i < 11; i++)
                {
                    f.WriteLine($"UP {i}");
                    ML_Segment_Vector tempVec = Stats.Up[i];

                    if (tempVec == null)
                        continue;

                    int total = tempVec.Total_Strength();
                    for (int a = 0; a < 11; a++)
                    {
                        var pct = tempVec.Up[a] / total;
                        f.WriteLine($"\tUP\t{a}\t{pct}");
                    }
                }*/

            }
        }
    }

    /// <summary>
    /// Used to gather stats for calculating from data points, only really useful if there are a lot of data points (over 20)
    /// </summary>
    class ML_Segment_Node
    {
        public Direction Direction;
        public int Strength;
        public ML_Segment_Node[] Previous;
        public ML_Segment_Node[] Next;

        public ML_Segment_Node GetFirstPrevious() { if (Previous.Length == 0) { return null; } else return Previous[0]; }
        public ML_Segment_Node GetFirstNext() { if (Next.Length == 0) { return null; } else return Next[0]; }

        public ML_Segment_Node(Direction direction, int strength)
        {
            Direction = direction;
            Strength = strength;
            Previous = new ML_Segment_Node[5];
            Next = new ML_Segment_Node[5];
        }

    }

    class ML_Segment_Stats
    {
        public ML_Segment_Vector[] Up; //This is direction, then the vector is a list of what comes next
        public ML_Segment_Vector[] Down; //no... need to record the current as well as the next... linked list for each 
        public ML_Segment_Vector Flat;

        public ML_Segment_Stats() 
        {
            Up = new ML_Segment_Vector[11];
            Down = new ML_Segment_Vector[11];
            Flat = new ML_Segment_Vector();
            for (int i = 0; i < 11; i++)
            {
                Up[i] = new ML_Segment_Vector();
                Down[i] = new ML_Segment_Vector();
            }
        }
    }

    class ML_Segment_Vector
    {
        public Direction Vector_Direction;
        public int Count;
        public int[] Up;
        public int[] Down;
        public int Flat;
        //public int Strength_Length() { int t = 0; foreach (var s in Strength) { t += s; } return t; }

        public ML_Segment_Vector()
        {
            Count = 0;
            Up = new int[11];
            Down = new int[11];
            Flat = 0;
        }

        public int Total_Strength()
        {
            int total = 0;
            foreach (int s in Up)
                total += s;
            foreach (int s in Down)
                total += s;
            total += Flat;
            return total;
        }

        public decimal Total_Average_Strength()
        {
            if (Count == 0)
                return 0;
            int total = 0;
            for (int i = 0; i < 11; i++)
                total += (Up[i] * i);
            for (int i = 0; i < 11; i++)
                total -= (Down[i] * i);
            return (decimal)total / (decimal)Count;
        }
    }


}
