using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;
using Messages;
using Messages.custom_msgs;
using Uml.Robotics.Ros;
using Messages.roscpp_tutorials;

namespace ServiceServerSample
{

  class Program
  {
    private static bool addition( TwoInts.Request req, ref TwoInts.Response resp )
    {
      ROS.Info()( "[ServiceServerSample] addition callback" );
      resp.sum = req.a + req.b;
      ROS.Info()( req.ToString() );
      ROS.Info()( resp.sum.ToString() );
      return true;
    }
    static void Main( string[] args )
    {
      NodeHandle nodeHandle;
      string NODE_NAME = "ServiceServerTest";
      ServiceServer server;

      try
      {
        ROS.Init( new string[0], NODE_NAME );
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
        nodeHandle = new NodeHandle();
        server = nodeHandle.advertiseService<TwoInts.Request, TwoInts.Response>( "/add_two_ints", addition );
        while( ROS.ok && server.IsValid )
        {
          Thread.Sleep( 10 );
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
