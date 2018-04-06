using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;
using Messages.roscpp_tutorials;
using Uml.Robotics.Ros;

namespace ServiceClientSample
{
  class Program
  {
    static void Main( string[] args )
    {
      string NODE_NAME = "ServiceClientTest";

      try
      {
        ROS.Init( new string[0], NODE_NAME + DateTime.Now.Ticks );
        var spinner = new AsyncSpinner();
        spinner.Start();
      }
      catch( RosException e )
      {
        ROS.Critical()( "ROS.Init failed, shutting down: {0}", e.Message );
        ROS.shutdown();
        ROS.waitForShutdown();
        return;
      }

      try
      {
        var nodeHandle = new NodeHandle();
        while( ROS.ok )
        {

          Random r = new Random();
          TwoInts.Request req = new TwoInts.Request() { a = r.Next( 100 ), b = r.Next( 100 ) };
          TwoInts.Response resp = new TwoInts.Response();
          DateTime before = DateTime.Now;
          bool res = nodeHandle.serviceClient<TwoInts.Request, TwoInts.Response>( "/add_two_ints" ).call( req, ref resp );
          TimeSpan dif = DateTime.Now.Subtract( before );

          string str = "";
          if( res )
            str = "" + req.a + " + " + req.b + " = " + resp.sum + "\n";
          else
            str = "call failed after ";

          str += Math.Round( dif.TotalMilliseconds, 2 ) + " ms";
          ROS.Info()( str );
          Thread.Sleep( 1000 );
        }
      }
      catch( RosException e )
      {
        ROS.Critical()( "Shutting down: {0}", e.Message );
      }

      ROS.shutdown();
      ROS.waitForShutdown();

    }
  }
}
