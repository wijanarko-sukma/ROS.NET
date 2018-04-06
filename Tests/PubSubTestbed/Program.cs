using System;
using System.Threading;
using System.Threading.Tasks;
using Uml.Robotics.Ros;

namespace Testbed
{
  class Program
  {
    private AsyncSpinner spinner;

    static void Main( string[] args )
    {
      var testbed = new Program();


      ROS.Info()( "Start testing of init and shutdown of ROS" );
      var result = testbed.TestStartStopOfRos( 10 );
      ROS.Info()( $"Test finish. Successful: {result.Item1}, Failed: {result.Item2}" );
    }


    private (int, int) TestStartStopOfRos( int numberOfTestRuns )
    {
      int succesfulRuns = 0;
      int failedRuns = 0;
      for( int i = 0; i < numberOfTestRuns; i++ )
      {
        StartRos( i );
        try
        {
          WaitForLatchedMessage( TimeSpan.FromSeconds( 10 ) );
          succesfulRuns += 1;
        }
        catch( TimeoutException te )
        {
          ROS.Error()( $"Run #{i} timed out!" );
          failedRuns += 1;
        }
        finally
        {
          StopRos( i );
        }
      }

      return (succesfulRuns, failedRuns);
    }


    private void StartRos( int runNumber )
    {
      ROS.Info()( $"Start ROS #{runNumber}" );
      ROS.Init( new string[] { }, "PubSubTestbed" );
      spinner = new AsyncSpinner();
      spinner.Start();
      ROS.Info()( "Started" );
    }


    private void StopRos( int runNumber )
    {
      ROS.Info()( $"Stop ROS #{runNumber}" );
      spinner.Stop();
      var shutdownTask = ROS.shutdown();
      shutdownTask.Wait();
      ROS.Info()( "Stopped" );
    }


    private void WaitForLatchedMessage( TimeSpan timeOut )
    {
      var receivedMessage = false;
      var s = ROS.GlobalNodeHandle.subscribe<Messages.std_msgs.String>( "/PubSubTestbed", 10, ( message ) =>
      {
        ROS.Info()( "--- Received message" );
        receivedMessage = true;
      } );

      var startTime = DateTime.UtcNow;
      while( !receivedMessage )
      {
        Thread.Sleep( 1 );
        if( DateTime.UtcNow - startTime > timeOut )
        {
          throw new TimeoutException( $"Now message retrieved withhin {timeOut}" );
        }
      }
    }


    /* private void SubscribeFirstThenPublish(int numberOfMessages)
     {
         Console.WriteLine("Init ROS");
         ROS.ROS_MASTER_URI = "http://192.168.0.134:11311";
         ROS.Init(new string[] { }, "SubscribeFirst");
         var spinner = new AsyncSpinner();
         spinner.Start();

         var nodeHandle = new NodeHandle();
         Console.WriteLine("Subscribe");
         //var subscriber = ROS.GlobalNodeHandle.subscribe<Messages.xamla_msgs.StressTest>("/StressTest", 10, (msg) =>
         //{
         //    Console.WriteLine($"Message #{msg.messageNumber} Took: {(DateTime.UtcNow - ROS.GetTime(msg.sentTime)).Ticks} ticks");
         //});

         Thread.Sleep(1000);

         var clientConnected = true;
         Console.WriteLine("Start Publisher");
         var publisher = nodeHandle.advertise<Messages.xamla_msgs.StressTest>("/StressTest", 10, (singlePublisher) =>
         {
             Console.WriteLine("Client connected");
             clientConnected = true;
         }, null);

         Task.Run(() =>
         {
             var messageNumber = 0;
             while (!clientConnected)
             {
                 Thread.Sleep(1);
             }
             Console.WriteLine("Publishing");
             while (messageNumber < numberOfMessages)
             {
                 var msg = new Messages.xamla_msgs.StressTest();
                 msg.messageNumber = (uint)messageNumber;
                 msg.sentTime = ROS.GetTime();
                 publisher.publish(msg);
                 Console.WriteLine($"Sent message #{messageNumber}");
                 Thread.Sleep(1000);
                 messageNumber += 1;
             }
         });

         ROS.waitForShutdown();
     }*/
  }
}
