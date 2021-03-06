﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Uml.Robotics.Ros
{
  public static class Service
  {
    public static bool exists( string serviceName, bool logFailureReason = false )
    {
      string mappedName = Names.Resolve( serviceName );

      string host = "";
      int port = 0;

      if( ServiceManager.Instance.LookUpService( mappedName, ref host, ref port ) )
      {
        var transport = new TcpTransport();
        if( transport.connect( host, port ) )
        {
          var m = new Dictionary<string, string>
                    {
                        { "probe", "1" },
                        { "md5sum", "*" },
                        { "callerid", ThisNode.Name },
                        { "service", mappedName }
                    };

          var h = new Header();
          h.Write( m, out byte[] headerbuf, out int size );

          byte[] sizebuf = BitConverter.GetBytes( size );

          transport.write( sizebuf, 0, sizebuf.Length );
          transport.write( headerbuf, 0, size );

          return true;
        }
        if( logFailureReason )
        {
          ROS.Info()( $"[{ThisNode.Name}] waitForService: Service[{mappedName}] could not connect to host [{host}:{port}], waiting..." );
        }
      }
      else if( logFailureReason )
      {
        ROS.Info()( $"[{ThisNode.Name}] waitForService: Service[{mappedName}] has not been advertised, waiting..." );
      }
      return false;
    }

    public static bool waitForService( string serviceName, TimeSpan timeout )
    {
      string mapped_name = Names.Resolve( serviceName );
      DateTime start_time = DateTime.UtcNow;
      bool printed = false;
      while( ROS.ok )
      {
        if( exists( serviceName, !printed ) )
        {
          break;
        }
        printed = true;
        if( timeout >= TimeSpan.Zero )
        {
          if( DateTime.UtcNow.Subtract( start_time ) > timeout )
            return false;
        }
        Thread.Sleep( ROS.WallDuration );
      }

      if( printed && ROS.ok )
      {
        ROS.Info()( $"[{ThisNode.Name}] waitForService: Service[{mapped_name}] is now available." );
      }
      return true;
    }

    public static bool waitForService( string service_name, int timeout )
    {
      return waitForService( service_name, TimeSpan.FromMilliseconds( timeout ) );
    }
  }
}
