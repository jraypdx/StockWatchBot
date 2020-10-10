using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace StockWatchBot.Objects
{
    class TimeSlice
    {
        public DateTime Time;
        public decimal Open;
        public decimal Close;
        public long Volume;

        public decimal Change() { return Close - Open; }
        public decimal ChangePct() { return ((Close / Open) - 1) * 100; }
    }


    class TimeSegment : TimeSlice
    {
        public decimal Average;
        public int Length; //usually should be minutes, but in future might want to use hours or something
        public Direction SegmentDirection;
        public int SegmentDirectionStrength; //1 to 10


        public TimeSegment(List<TimeSlice> slices)
        {
            this.Length = slices.Count();
            this.Open = slices.Last().Open;
            this.Close = slices.First().Close;
            this.Time = slices.Last().Time;

            foreach (var s in slices)
            {
                this.Average += s.Close;
                this.Volume += s.Volume;
            }
            this.Average = this.Average / Length;

            SetDirection();
        }

        private void SetDirection()
        {
            if (Open == Close) //if no change
            {
                SegmentDirection = Direction.None;
                SegmentDirectionStrength = 0;
            }
            else
            {
                //find and set strength
                int str = (int)Equation1(ChangePct());
                if (str == 0)
                {
                    SegmentDirection = Direction.Flat;
                    SegmentDirectionStrength = 0;
                }
                //else if (str >= 10)
                //    SegmentDirectionStrength = 10; //gonna just take this off, don't think it will ever get to or above strength 10 (1% change in 5 min) but if it does I probably want to know about it...
                //else if (str <= -10)
                //    SegmentDirectionStrength = -10;
                else
                    SegmentDirectionStrength = str;

                //find and set direction if not 0
                if (str > 0)
                    SegmentDirection = Direction.Up;
                else if (str < 0)
                    SegmentDirection = Direction.Down;
            }
        }

        private double Equation1(decimal input) //y = sqrt(100x) will give ranking based off pct passed in, then if it's greater/less than 10/-10 stop it at 10/-10 [inverse of 1/100x^2]
        {
            bool isNegative = false;
            if (input < 0)
            {
                isNegative = true;
                input *= -1;
            }

            input = input * 100;
            var toUse = Convert.ToDouble(input);
            var ret = Math.Sqrt(toUse);

            if (isNegative)
                return ret * -1;
            else
                return ret;
        }
    }

    public enum Direction
    {
        Down,
        Flat, //basically down/up with strength 0
        None, //exact same
        Up
    }

}
