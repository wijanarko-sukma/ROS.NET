using System;
using System.Diagnostics;
using System.Threading;
using Uml.Robotics.Ros;
using std_msgs = Messages.std_msgs;
using String = Messages.std_msgs.String;

namespace Talker
{
  public class Program
  {
    private static void Main( string[] args )
    {
      Trace.Listeners.Add( new TextWriterTraceListener( Console.Out ) );
      ROS.Init( args, "Talker" );
      var spinner = new SingleThreadSpinner();
      NodeHandle node = new NodeHandle();
      Publisher<std_msgs.String> Talker = node.advertise<std_msgs.String>( "/chatter", 1 );
      int count = 0;

      while( ROS.ok && !Console.KeyAvailable )
      {
        Console.WriteLine( "publishing message" );
        ROS.Info()( "Publishing a chatter message:    \"Blah blah blah " + count + "\"" );
        String pow = new String( "Blah blah blah " + ( count++ ) );

        Talker.publish( pow );
        spinner.SpinOnce();
        Thread.Sleep( 1000 );
      }

      ROS.shutdown();
    }
  }

}
