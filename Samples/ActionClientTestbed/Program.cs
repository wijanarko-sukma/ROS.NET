using System;
using System.Collections.Generic;
using System.Threading;
using Uml.Robotics.Ros;
using Uml.Robotics.Ros.ActionLib;
using Messages.actionlib_msgs;
using System.Diagnostics;

namespace ActionClientTestbed
{
  class Program
  {
    private TestState testState;
    private int receivedFeedback = 0;
    private ActionClient<Messages.actionlib.TestGoal, Messages.actionlib.TestResult, Messages.actionlib.TestFeedback> actionClient;
    private List<TestParameters> TestParams = new List<TestParameters>();
    private SingleThreadSpinner spinner;

    static void Main( string[] args )
    {
      Console.WriteLine( "Start roscore and ActionServerTesbed.lua and press any key" );
      while( !Console.KeyAvailable )
      {
        Thread.Sleep( 1 );
      }

      Console.WriteLine( "Start ROS" );
      ROS.ROS_MASTER_URI = "http://rosvita:11311";
      ROS.Init( new string[0], "ActionClient" );

      //(new Program()).Start(1);
      ( new TestActionServerKill() ).Start( 5 );
      ROS.shutdown();
    }


    private void Start( int numberOfRuns )
    {
      NodeHandle clientNodeHandle = new NodeHandle();
      spinner = new SingleThreadSpinner( ROS.GlobalCallbackQueue );

      /*TestParams.Add(new TestParameters("Reject Goal", GoalStatus.REJECTED, 0, false, null));
      TestParams.Add(new TestParameters("Cancel not yet accepted goal", GoalStatus.RECALLED, 0, true, null));
      TestParams.Add(new TestParameters("Cancel accepted goal", GoalStatus.PREEMPTED, 0, true, null));
      TestParams.Add(new TestParameters("Abort Goal", GoalStatus.ABORTED, 100, false, null));*/
      TestParams.Add( new TestParameters( "Get Result 123", GoalStatus.SUCCEEDED, 100, false, 123 ) );
      var successfulTestCount = 0;
      var failedTestCount = 0;

      Console.WriteLine( "Create client" );
      actionClient = new ActionClient<Messages.actionlib.TestGoal, Messages.actionlib.TestResult,
          Messages.actionlib.TestFeedback>( "test_action", clientNodeHandle, 120 );

      Console.WriteLine( "Wait for client and server to negotiate connection" );
      bool started = actionClient.WaitForActionServerToStartSpinning( new TimeSpan( 0, 0, 3 ), spinner );

      var sw = Stopwatch.StartNew();
      DateTime? firstFail = null;
      var startTime = DateTime.Now;
      for( int i = 0; i < numberOfRuns; i++ )
      {
        bool testError = false;
        if( started )
        {
          Console.WriteLine( "Server connected, start tests" );
          foreach( var parameter in TestParams )
          {
            if( !TestCase( parameter.Name, parameter.ExpectedState, parameter.ExpectedFeedback,
                parameter.CancelGoal, parameter.ExpectedGoal ) )
            {
              testError = true;
              if( firstFail == null )
              {
                firstFail = DateTime.Now;
              }
              break;
            }
          }
        }
        else
        {
          ROS.Error()( "Could not connect to server" );
          testError = true;
        }

        if( testError )
        {
          ROS.Error()( "Errors ocured during testing!" );
          failedTestCount++;
        }
        else
        {
          successfulTestCount++;
          ROS.Info()( "Testbed completed successfully" );
        }
      }

      Console.WriteLine( "-----------" );
      Console.WriteLine( $"Test took {sw.Elapsed} StartTime: {startTime} Goals/s {numberOfRuns / sw.Elapsed.TotalSeconds}" );
      Console.WriteLine( $"Successful: {successfulTestCount} Failed: {failedTestCount} FirstFail: {firstFail}" );
      Console.WriteLine( "All done, press any key to exit" );
      Console.WriteLine( "-----------" );

      while( !Console.KeyAvailable )
      {
        Thread.Sleep( 1 );
      }

      Console.WriteLine( "Shutdown client" );
      actionClient.Shutdown();
      clientNodeHandle.shutdown();
    }


    private bool TestCase( string testName, byte successState, int expectedNumberOfFeedback, bool cancelGoal, int? expectedGoal )
    {
      testState = TestState.Unknown;
      receivedFeedback = 0;
      Console.WriteLine( "" );
      Console.WriteLine( testName );

      var goal = new Messages.actionlib.TestGoal();
      goal.goal = 42;

      var cts = new CancellationTokenSource();
      var clientHandle = actionClient.SendGoalAsync( goal,
          ( goalHandle ) => {
            Console.WriteLine( $"Transition: {goalHandle.State} {goalHandle.LatestGoalStatus.status} {goalHandle.Result?.result}" );

            if( goalHandle.State == CommunicationState.DONE )
            {
              if( goalHandle.LatestGoalStatus.status == successState )
              {
                if( expectedGoal == null )
                {
                  testState = TestState.Succeeded;
                }
                else
                {
                  if( goalHandle.Result?.result == expectedGoal )
                  {
                    Console.WriteLine( $"Received expected Goal Result: {goalHandle.Result?.result}" );
                    testState = TestState.Succeeded;
                  }
                  else
                  {
                    Console.WriteLine( $"Received unexpected Goal Result: {goalHandle.Result?.result}" );
                    testState = TestState.Failed;
                  }
                }
              }
              else
              {
                testState = TestState.Failed;
              }
            }
          },
          ( goalHandle, feedback ) => {
                  //Console.WriteLine($"Feedback {feedback.Feedback.feedback} {feedback.GoalStatus.status}");
                  receivedFeedback += 1;
          },
          cts.Token
      );
      Console.WriteLine( $"Sent goal {clientHandle.Id}" );

      if( cancelGoal )
      {
        Thread.Sleep( 100 );
        Console.WriteLine( "Canceling goal" );
        cts.Cancel();
      }

      WaitForSuccessWithTimeOut( 5, expectedNumberOfFeedback, expectedGoal );
      var testResult = ( testState == TestState.Succeeded );
      var feedbackResult = ( receivedFeedback == expectedNumberOfFeedback );
      var result = testResult;
      Console.WriteLine( result ? "SUCCESS" : $"FAIL transistion: {testResult} feedback: {feedbackResult}: {receivedFeedback}/{expectedNumberOfFeedback}" );
      Console.WriteLine( "" );
      return result;
    }


    private void WaitForSuccessWithTimeOut( int timeOutInSeconds, int expectedFeedback, int? expectedGoal )
    {
      var timeSpan = new TimeSpan( 0, 0, timeOutInSeconds );
      var start = DateTime.UtcNow;
      while( ( DateTime.UtcNow - start < timeSpan ) && ROS.ok )
      {
        if( ( testState == TestState.Succeeded ) )
        {
          break;
        }
        Thread.Sleep( 0 );
        spinner.SpinOnce();
      }
    }


    private enum TestState
    {
      Unknown,
      Succeeded,
      Failed
    }

  }

  // thanks: https://loune.net/2017/06/running-shell-bash-commands-in-net-core/
  public static class ShellHelper
  {
    public static string Bash( this string cmd )
    {
      var escapedArgs = cmd.Replace( "\"", "\\\"" );

      var process = new Process()
      {
        StartInfo = new ProcessStartInfo
        {
          FileName = "/bin/bash",
          Arguments = $"-c \"{escapedArgs}\"",
          RedirectStandardOutput = true,
          UseShellExecute = false,
          CreateNoWindow = true,
        }
      };
      process.Start();
      //string result = process.StandardOutput.ReadToEnd();
      //process.WaitForExit();
      return ""; //result;
    }
  }

  public class TestActionServerKill
  {
    public void Start( int numberOfTries )
    {
      NodeHandle clientNodeHandle = new NodeHandle();
      var spinner = new AsyncSpinner();
      spinner.Start();
      //var spinner = new SingleThreadSpinner(ROS.GlobalCallbackQueue);

      /*Console.WriteLine("Start Action Server");
      ShellHelper.Bash("screen -dmS selfkill th SelfKillingActionServer.th");
      Console.WriteLine("Ok");
      Thread.Sleep(300000);*/

      Console.WriteLine( "Start Action Client" );
      var actionClient = new ActionClient<Messages.actionlib.TestGoal, Messages.actionlib.TestResult,
          Messages.actionlib.TestFeedback>( "self_killing_action", clientNodeHandle, 10 );
      int numberReceived = 0;
      int numberTimeout = 0;
      for( int i = 0; i < numberOfTries; i++ )
      {
        bool started = actionClient.WaitForActionServerToStart( TimeSpan.FromSeconds( 10 ) );

        if( started )
        {
          var g = new Messages.actionlib.TestGoal();
          g.goal = 12;
          Console.WriteLine( "Sent" );
          var c = new CancellationTokenSource();
          //c.CancelAfter(6000);
          var gh = actionClient.SendGoalAsync( g, ( cgh ) => { }, ( cgh, fb ) => { }, c.Token );
          bool received = false;
          var start = DateTime.UtcNow;
          while( true )
          {
            if( gh.IsCompleted )
            {
              if( gh.IsCanceled )
              {
                break;
              }
              else if( gh.IsFaulted )
              {
                break;
              }
              else if( gh.Result.result == 123 )
              {
                received = true;
                break;
              }
            }
            Thread.Sleep( 0 );
            //spinner.SpinOnce();
          }
          if( received )
          {
            Console.WriteLine( "Received" );
            numberReceived += 1;
          }
          else
          {
            Console.WriteLine( "Timeout" );
            numberTimeout += 1;
          }
        }
        else
        {
          Console.WriteLine( "Could not connect to Action Server" );
        }
      }

      Console.WriteLine( $"Done Received: {numberReceived} / Timeout: {numberTimeout}" );
    }
  }

  public class TestParameters
  {
    public String Name { get; set; }
    public int ExpectedFeedback { get; set; }
    public byte ExpectedState { get; set; }
    public bool CancelGoal { get; set; }
    public int? ExpectedGoal { get; set; }

    public TestParameters( string name, byte state, int feedback, bool cancel, int? goal )
    {
      Name = name;
      ExpectedState = state;
      ExpectedFeedback = feedback;
      CancelGoal = cancel;
      ExpectedGoal = goal;
    }
  }
}