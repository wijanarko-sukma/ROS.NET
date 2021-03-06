﻿using System;
using System.Threading;

using Uml.Robotics.Ros;
using Uml.Robotics.Ros.ActionLib;
using Messages.control_msgs;
using System.Collections.Generic;
using Messages.actionlib;

namespace ActionServerSample
{
  class Program
  {
    static void Main(string[] args)
    {
#if (DEBUG)
      Environment.SetEnvironmentVariable("ROS_MASTER_URI", "http://localhost:11311/");
#endif
      Console.WriteLine("Start ROS");
      ROS.Init(ref args, "ActionServerClientSlowDummy");

      ICallbackQueue callbackQueue = new CallbackQueue();

      var asyncSpinner = new AsyncSpinner(callbackQueue);
      asyncSpinner.Start();

      NodeHandle nodeHandle = new NodeHandle(callbackQueue);

      ActionClient<Messages.actionlib.TestGoal, Messages.actionlib.TestResult,
          Messages.actionlib.TestFeedback> actionClient = null;

      // setup action server start
      Console.WriteLine("Create server");
      var actionServer = new ActionServer<Messages.actionlib.TestGoal, Messages.actionlib.TestResult,
          Messages.actionlib.TestFeedback>(nodeHandle, "test_action");
      Param.Set("status_list_timeout", 999.9);
      actionServer.RegisterGoalCallback((sgoalHandle) =>
      {
        Thread thread = new Thread(() => serverGoalCallback(sgoalHandle, actionServer, actionClient));
        thread.Start();
      });
      Console.WriteLine("Start Server");
      actionServer.Start();
      Console.WriteLine("Server Started");
      // setup action server finish


      // setup client
      actionClient = new ActionClient<Messages.actionlib.TestGoal, Messages.actionlib.TestResult,
          Messages.actionlib.TestFeedback>("test_action_slow", nodeHandle);
      // send action request to serverslowdummy 
      Console.WriteLine("Wait for client and server to negotiate connection");
      bool started = actionClient.WaitForActionServerToStart(new TimeSpan(0, 0, 3));

      if (!started)
      {
        Console.WriteLine("Negotiation with server failed!");
      }

      Console.ReadLine();

      actionServer.Shutdown();
      nodeHandle.shutdown();
      ROS.shutdown();
    }

    private static void serverGoalCallback(ServerGoalHandle<TestGoal, TestResult, TestFeedback> sgoalHandle,
      ActionServer<TestGoal, TestResult, TestFeedback> actionServer,
      ActionClient<TestGoal, TestResult, TestFeedback> actionClient)
    {
      // got goal to reach from clientsample
      Console.WriteLine($"Goal registered callback. Goal: {sgoalHandle.Goal.goal}");

      var goal = new Messages.actionlib.TestGoal();
      Console.WriteLine($"Send goal {goal.goal} from client");
      var cts = new CancellationTokenSource();
      actionClient.SendGoalAsync(goal,
          (cgoalHandle) =>
          {
            if (cgoalHandle.State == CommunicationState.DONE)
            {
              int g = cgoalHandle.Goal.Goal.goal;
              var result = cgoalHandle.Result;
              if (result != null)
              {
                Console.WriteLine($"Got Result for goal {g}: {cgoalHandle.Result.result}");
                var aresult = new Messages.actionlib.TestResult
                {
                  result = 999
                };
                sgoalHandle.SetGoalStatus(Messages.actionlib_msgs.GoalStatus.SUCCEEDED, "done");
                actionServer.PublishResult(sgoalHandle.GoalStatus, result);
              }
              else
              {
                Console.WriteLine($"Result for goal {g} is NULL!");
              }
            }
          },
          (cgoalHandle, feedback) =>
          {
            Console.WriteLine($"Feedback: {feedback}");
            var fb = new Messages.actionlib.TestFeedback
            {
              feedback = feedback.Feedback.feedback
            };
            sgoalHandle.PublishFeedback(fb);
          },
          cts.Token
      ).GetAwaiter().GetResult();
    }
  }
}