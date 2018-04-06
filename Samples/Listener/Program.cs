using System;
using System.Diagnostics;
using Uml.Robotics.Ros;
using std_msgs = Messages.std_msgs;

namespace Listener
{
  public class Program
  {
    private static void chatterCallback( std_msgs.String s )
    {
      ROS.Info()( "RECEIVED: " + s.data );
      Console.WriteLine( $"Received: " + s.data );
    }
    private static void Main( string[] args )
    {
      Trace.Listeners.Add( new TextWriterTraceListener( Console.Out ) );
      ROS.Init( args, "Listener" );
      var spinner = new AsyncSpinner();
      spinner.Start();
      NodeHandle node = new NodeHandle();
      Subscriber Subscriber = node.subscribe<std_msgs.String>( "/chatter", 1, chatterCallback );
      ROS.waitForShutdown();
    }
  }
}
