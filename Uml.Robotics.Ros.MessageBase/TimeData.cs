using System;

namespace Uml.Robotics.Ros
{
  public struct TimeData
  {
    public static readonly TimeData Zero = new TimeData( 0, 0 );

    public uint sec;
    public uint nsec;

    public TimeData( uint s, uint ns )
    {
      sec = s;
      nsec = ns;
    }

    public bool Equals( TimeData timer )
    {
      return ( sec == timer.sec && nsec == timer.nsec );
    }

    public long Ticks
    {
      get { return sec * TimeSpan.TicksPerSecond + (long)Math.Floor( nsec / 100.0 ); }
    }

    public TimeSpan ToTimeSpan()
    {
      return new TimeSpan( this.Ticks );
    }

    public static TimeData FromTicks( long ticks )
    {
      return FromTicks( (ulong)ticks );
    }

    public static TimeData FromTicks( ulong ticks )
    {
      ulong seconds = ( ( (ulong)Math.Floor( 1.0 * ticks / TimeSpan.TicksPerSecond ) ) );
      ulong nanoseconds = 100 * ( ticks % TimeSpan.TicksPerSecond );
      return new TimeData( (uint)seconds, (uint)nanoseconds );
    }

    public static TimeData FromTimeSpan( TimeSpan value )
    {
      return FromTicks( value.Ticks );
    }
  }
}
