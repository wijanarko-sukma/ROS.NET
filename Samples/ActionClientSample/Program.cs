﻿using System;
using System.Threading;

using Uml.Robotics.Ros;
using Uml.Robotics.Ros.ActionLib;
using Messages.control_msgs;
using System.Collections.Generic;

namespace ActionClientSample
{
  class Program
  {
    /*static void Main(string[] args)
    {
        Console.WriteLine("Start ROS");
        ROS.Init(new string[0], "ActionClient");
        NodeHandle clientNodeHandle = new NodeHandle();

        Console.WriteLine("Create client");
        var actionClient = new ActionClient<SingleJointPositionGoal, SingleJointPositionResult,
            SingleJointPositionFeedback>("ActionTest", clientNodeHandle);

        Console.WriteLine("Wait for client and server to negotiate connection");
        bool started = actionClient.WaitForActionServerToStart(new TimeSpan(0, 0, 10));


        if (started || true)
        {
            var goal = new SingleJointPositionGoal();
            goal.position = 12.0;
            goal.max_velocity = 42.0;

            Console.WriteLine("Send goal from client");
            var transition = false;
            actionClient.SendGoal(goal,
                (goalHandle) => { Console.WriteLine($"Transition: {goalHandle}"); transition = true; },
                (goalHandle, feedback) => { Console.WriteLine($"Feedback: {feedback}"); });


            Console.WriteLine("Wait for action client to receive transition");
            while (!transition)
            {
                Thread.Sleep(1);
            }
        } else
        {
            Console.WriteLine("Negotiation with server failed!");
        }
    }*/


    static void Main( string[] args )
    {
      Environment.SetEnvironmentVariable("ROS_HOSTNAME", "localhost");
      Environment.SetEnvironmentVariable("ROS_IP", "127.0.0.1");
      Environment.SetEnvironmentVariable("ROS_MASTER_URI", "http://localhost:11311/");

      Console.WriteLine( "Start ROS" );
      ROS.Init( ref args, "ActionClient" );

      ICallbackQueue callbackQueue = new CallbackQueue();

      var asyncSpinner = new AsyncSpinner(callbackQueue);
      asyncSpinner.Start();

      NodeHandle clientNodeHandle = new NodeHandle(callbackQueue);  //ROS.GlobalNodeHandle;

      Console.WriteLine( "Create client" );
      var actionClient = new ActionClient<Messages.actionlib.TestGoal, Messages.actionlib.TestResult,
          Messages.actionlib.TestFeedback>( "test_action", clientNodeHandle );

      Console.WriteLine( "Wait for client and server to negotiate connection" );
      bool started = actionClient.WaitForActionServerToStart( new TimeSpan( 0, 0, 60 ) );


      if( started )
      {
        int counter = 0;
        var dict = new Dictionary<int, Messages.actionlib.TestGoal>();

        while( !Console.KeyAvailable )
        {
          var now = DateTime.UtcNow;
          Console.WriteLine( $"Waited: {DateTime.UtcNow - now}" );
          var goal = new Messages.actionlib.TestGoal
          {
            goal = counter
          };
          dict[counter] = goal;
          counter += 1;

          Console.WriteLine( $"------------------> Send goal {goal.goal} from client" );
          var cts = new CancellationTokenSource();
          var goalTask = actionClient.SendGoalAsync( goal,
              ( goalHandle ) =>
              {
                if( goalHandle.State == CommunicationState.DONE )
                {
                  int g = goalHandle.Goal.Goal.goal;
                  var resultt = goalHandle.Result;
                  if( resultt != null )
                  {
                    Console.WriteLine( $"------------------> Got Result for goal {g}: {resultt.result}" );
                  }
                  else
                  {
                    Console.WriteLine( $"Result for goal {g} is NULL!" );
                  }
                  dict.Remove( g );
                }
              },
              ( goalHandle, feedback ) =>
              {
                Console.WriteLine( $"------------------> Got Feedback: {feedback.Feedback.feedback}" );
              },
              cts.Token
          );

          var waiter = goalTask.GetAwaiter();
          goalTask.Wait();
          
          Console.ReadLine();

          while (!waiter.IsCompleted) Thread.Yield();
          var result = waiter.GetResult();
          break;
        }        

        Console.WriteLine( "Wait for 15s for open goals" );
        var timeOut = new TimeSpan( 0, 0, 15 );
        var start = DateTime.UtcNow;
        while( ( DateTime.UtcNow - start <= timeOut ) && ( dict.Count > 0 ) )
        {
          Thread.Sleep( 1 );
        }
        if( dict.Count == 0 )
        {
          Console.WriteLine( "All goals have been reached!" );
        }
        else
        {
          Console.WriteLine( "TIMEOUT: There are still open goals" );
        }        
      }
      else
      {
        Console.WriteLine( "Negotiation with server failed!" );
      }

      Console.WriteLine( "Shutdown ROS" );
      actionClient.Shutdown();
      clientNodeHandle.shutdown();
      ROS.shutdown();
    }
  }
}