using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets
{
    public struct ConnectionTimeout
    {
        public static ConnectionTimeout FromTimeSpan(TimeSpan ts) => ts;
        public static ConnectionTimeout FromTicks(int ticks) => TimeSpan.FromTicks(ticks);
        public static ConnectionTimeout FromMilliseconds(int millis) => TimeSpan.FromMilliseconds(millis);
        public static ConnectionTimeout FromSeconds(int seconds) => TimeSpan.FromSeconds(seconds);
        public static ConnectionTimeout FromMinutes(int minutes) => TimeSpan.FromMinutes(minutes);
        public static ConnectionTimeout FromHours(int hours) => TimeSpan.FromHours(hours);
        public static ConnectionTimeout FromDays(int days) => TimeSpan.FromDays(days);
        public static ConnectionTimeout Zero => TimeSpan.Zero;
        public static ConnectionTimeout Infinity => TimeSpan.FromMilliseconds(-1);

        public int Ticks => (int)_timespan.Ticks;
        public int Milliseconds => _timespan.Milliseconds;
        public int Seconds => _timespan.Seconds;
        public int Minutes => _timespan.Minutes;
        public int Hours => _timespan.Hours;
        public int Days => _timespan.Days;

        public int TotalMilliseconds => (int)_timespan.TotalMilliseconds;
        public int TotalSeconds => (int)_timespan.TotalSeconds;
        public int TotalMinutes => (int)_timespan.TotalMinutes;
        public int TotalHours => (int)_timespan.TotalHours;
        public int TotalDays => (int)_timespan.TotalDays;

        public ConnectionTimeout Add(TimeSpan ts) => _timespan.Add(ts);
        public ConnectionTimeout Add(ConnectionTimeout timeout) => _timespan.Add(timeout._timespan);

        public ConnectionTimeout Subtract(TimeSpan ts) => _timespan.Subtract(ts);
        public ConnectionTimeout Subtract(ConnectionTimeout timeout) => _timespan.Subtract(timeout._timespan);


        TimeSpan _timespan;
        private ConnectionTimeout(TimeSpan ts)
        {
            if (ts.TotalMilliseconds < -1)
                throw new Exception($"Minimum Connetion Timeout Milliseconds is  -1 (Infinite)");
            if (ts.TotalMilliseconds > int.MaxValue)
                throw new Exception($"Maximum Connetion Timeout Milliseconds is {int.MaxValue}");
            _timespan = ts;
        }

        public static implicit operator ConnectionTimeout(TimeSpan ts)
        {
            return new ConnectionTimeout(ts);
        }

        public static implicit operator TimeSpan(ConnectionTimeout timeout)
        {
            return timeout._timespan;
        }

        public static bool operator ==(ConnectionTimeout x, ConnectionTimeout y)
        {
            return x._timespan == y._timespan;
        }

        public static bool operator !=(ConnectionTimeout x, ConnectionTimeout y)
        {
            return x._timespan != y._timespan;
        }

        public static bool operator >(ConnectionTimeout x, ConnectionTimeout y)
        {
            return x._timespan > y._timespan;
        }

        public static bool operator <(ConnectionTimeout x, ConnectionTimeout y)
        {
            return x._timespan < y._timespan;
        }

        public static bool operator >=(ConnectionTimeout x, ConnectionTimeout y)
        {
            return x._timespan >= y._timespan;
        }

        public static bool operator <=(ConnectionTimeout x, ConnectionTimeout y)
        {
            return x._timespan <= y._timespan;
        }

        public override bool Equals(object obj)
        {
            return _timespan.Equals(((ConnectionTimeout)obj)._timespan);
        }

        public override int GetHashCode()
        {
            return _timespan.GetHashCode();
        }
    }
}
