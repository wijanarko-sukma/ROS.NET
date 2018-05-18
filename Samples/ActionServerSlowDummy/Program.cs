using System;
using System.Threading;

using Uml.Robotics.Ros;
using Uml.Robotics.Ros.ActionLib;
using Messages.control_msgs;

namespace ActionServerSlowDummy
{
  class Program
  {
    static void Main(string[] args)
    {
      //#if (DEBUG)
      //      Environment.SetEnvironmentVariable("ROS_HOSTNAME", "");
      //      Environment.SetEnvironmentVariable("ROS_IP", "192.168.200.32");
      //      Environment.SetEnvironmentVariable("ROS_MASTER_URI", "http://192.168.200.231:11311/");
      //#endif
      Environment.SetEnvironmentVariable("ROS_HOSTNAME", "localhost");
      Environment.SetEnvironmentVariable("ROS_IP", "127.0.0.1");
      Environment.SetEnvironmentVariable("ROS_MASTER_URI", "http://localhost:11311/");
      Console.WriteLine("Start ROS");
      ROS.Init(ref args, "ActionServerSlowDummy");

      ICallbackQueue callbackQueue = new CallbackQueue();

      var asyncSpinner = new AsyncSpinner(callbackQueue);
      asyncSpinner.Start();

      //var spinner = new SingleThreadSpinner(callbackQueue);


      NodeHandle serverNodeHandle = new NodeHandle(callbackQueue);

      Console.WriteLine("Create server");
      var actionServer = new ActionServer<Messages.actionlib.TestGoal, Messages.actionlib.TestResult,
          Messages.actionlib.TestFeedback>(serverNodeHandle, "test_action_slow");
      Console.WriteLine("Start Server");
      Param.Set("status_list_timeout", 999.9);
      actionServer.Start();

      actionServer.RegisterGoalCallback((goalHandle) =>
      {
        Console.WriteLine($"Goal registered callback. Goal: {goalHandle.Goal.goal}");
        goalHandle.SetAccepted("accepted");

        new Thread(() =>
        {
          for (int x = 0; x < 300; x++)
          {
            var fb = new Messages.actionlib.TestFeedback
            {
              feedback = x
            };
            goalHandle.PublishFeedback(fb);
            Thread.Sleep(100);
          }

          var result = new Messages.actionlib.TestResult
          {
            result = 123
          };
          goalHandle.SetGoalStatus(Messages.actionlib_msgs.GoalStatus.SUCCEEDED, "done");
          actionServer.PublishResult(goalHandle.GoalStatus, result);
        }).Start();        
      });

      Console.ReadLine();
      
      actionServer.Shutdown();
      serverNodeHandle.shutdown();
      ROS.shutdown();
    }
  }
}