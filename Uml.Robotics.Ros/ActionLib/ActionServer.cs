﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using Messages.std_msgs;
using Uml.Robotics.Ros.ActionLib.Interfaces;
using Messages;
using Messages.actionlib_msgs;
using System.Collections.Concurrent;

namespace Uml.Robotics.Ros.ActionLib
{
  public class ActionServer<TGoal, TResult, TFeedback> : IActionServer<TGoal, TResult, TFeedback>
      where TGoal : InnerActionMessage, new()
      where TResult : InnerActionMessage, new()
      where TFeedback : InnerActionMessage, new()
  {
    public int QueueSize { get; set; } = 50;
    public TimeSpan StatusListTimeout { get; private set; }

    private const string ACTIONLIB_STATUS_FREQUENCY = "actionlib_status_frequency";
    private const string STATUS_LIST_TIMEOUT = "status_list_timeout";
    private bool started;
    private ConcurrentDictionary<string, ServerGoalHandle<TGoal, TResult, TFeedback>> goalHandles;
    private NodeHandle nodeHandle;
    private DateTime lastCancel;
    private Action<ServerGoalHandle<TGoal, TResult, TFeedback>> goalCallback;
    private Action<ServerGoalHandle<TGoal, TResult, TFeedback>> cancelCallback;
    private Publisher<ResultActionMessage<TResult>> resultPublisher;
    private Publisher<FeedbackActionMessage<TFeedback>> feedbackPublisher;
    private Publisher<GoalStatusArray> goalStatusPublisher;
    private Subscriber goalSubscriber;
    private Subscriber cancelSubscriber;
    private TimeSpan statusInterval;
    private DateTime nextStatusPublishTime;
    private long spinCallbackId = 0;
    private Timer timer;
    //private object lockGoalHandles;

    public ActionServer( NodeHandle nodeHandle, string actionName )
    {
      this.goalHandles = new ConcurrentDictionary<string, ServerGoalHandle<TGoal, TResult, TFeedback>>();
      this.nodeHandle = new NodeHandle( nodeHandle, actionName );
      this.lastCancel = DateTime.UtcNow;
      this.started = false;
      //this.lockGoalHandles = new object();
    }


    public TFeedback CreateFeedback()
    {
      var feedback = new TFeedback();
      return feedback;
    }


    public TResult CreateResult()
    {
      var result = new TResult();
      return result;
    }


    public void RegisterCancelCallback( Action<ServerGoalHandle<TGoal, TResult, TFeedback>> cancelCallback )
    {
      this.cancelCallback = cancelCallback;
    }


    public void RegisterGoalCallback( Action<ServerGoalHandle<TGoal, TResult, TFeedback>> goalCallback )
    {
      this.goalCallback = goalCallback;
    }


    public void Shutdown()
    {
      if( spinCallbackId != 0 )
      {
        ROS.GlobalCallbackQueue.RemoveById( spinCallbackId );
        spinCallbackId = 0;
      }
      resultPublisher.shutdown();
      feedbackPublisher.shutdown();
      goalStatusPublisher.shutdown();
      goalSubscriber.shutdown();
      cancelSubscriber.shutdown();
    }


    public void Start()
    {
      if( started )
      {
        return;
      }

      // Emmitting topics
      resultPublisher = nodeHandle.advertise<ResultActionMessage<TResult>>( "result", QueueSize );
      feedbackPublisher = nodeHandle.advertise<FeedbackActionMessage<TFeedback>>( "feedback", QueueSize );
      goalStatusPublisher = nodeHandle.advertise<GoalStatusArray>( "status", QueueSize, true );

      // Read the frequency with which to publish status from the parameter server
      // If not specified locally explicitly, use search param to find actionlib_status_frequency
      Param.Get( ACTIONLIB_STATUS_FREQUENCY, out double statusFrequency, 5.0 ); //Hz
      timer = new Timer( SpinCallback, null, 0, (int)(( 1 / statusFrequency ) * 1000 ));
      
      double statusListTimeout;
      Param.Get( STATUS_LIST_TIMEOUT, out statusListTimeout, 5.0 ); //second
      var split = SplitSeconds( statusListTimeout );
      StatusListTimeout = new TimeSpan( 0, 0, 0, split.seconds, split.milliseconds );

      // Message consumers
      goalSubscriber = nodeHandle.subscribe<GoalActionMessage<TGoal>>( "goal", this.QueueSize, GoalCallback );
      cancelSubscriber = nodeHandle.subscribe<GoalID>( "cancel", this.QueueSize, CancelCallback );

      started = true;
      PublishStatus();
    }


    public void PublishFeedback( GoalStatus goalStatus, TFeedback feedback )
    {
      var newFeedback = new FeedbackActionMessage<TFeedback>();
      newFeedback.Header = new Messages.std_msgs.Header();
      newFeedback.Header.stamp = ROS.GetTime();
      newFeedback.GoalStatus = goalStatus;
      newFeedback.Feedback = feedback;
      ROS.Debug()( $"[{ThisNode.Name}] [actionlib] Publishing feedback for goal with id: {goalStatus.goal_id.id} and stamp: {new DateTimeOffset( ROS.GetTime( goalStatus.goal_id.stamp ) ).ToUnixTimeSeconds()}" );
      feedbackPublisher.publish( newFeedback );
    }


    public void PublishResult( GoalStatus goalStatus, TResult result )
    {
      var newResult = new ResultActionMessage<TResult>();
      newResult.Header = new Messages.std_msgs.Header();
      newResult.Header.stamp = ROS.GetTime();
      newResult.GoalStatus = goalStatus;
      if( result != null )
      {
        newResult.Result = result;
      }
      ROS.Debug()( $"[{ThisNode.Name}] [actionlib] Publishing result for goal with id: {goalStatus.goal_id.id} and stamp: {new DateTimeOffset( ROS.GetTime( goalStatus.goal_id.stamp ) ).ToUnixTimeSeconds()}" );
      resultPublisher.publish( newResult );
      PublishStatus();
    }


    //GoalStatusArray statusArray = new GoalStatusArray();
    List<GoalStatus> goalStatuses = new List<GoalStatus>();
    List<string> idsToBeRemoved = new List<string>();

    public void PublishStatus()
    {
      var now = DateTime.UtcNow;
      //if (this.statusArray == null)
      //{
      //  this.statusArray = new GoalStatusArray();
      //  this.statusArray.header = new Messages.std_msgs.Header();
      //}
      var statusArray = new GoalStatusArray
      {
        header = new Messages.std_msgs.Header()
      };
      statusArray.header.stamp = ROS.GetTime( now );

      goalStatuses.Clear();      
      idsToBeRemoved.Clear();
     
//lock( lockGoalHandles )
      //{
        foreach( var pair in goalHandles )
        {
          goalStatuses.Add( pair.Value.GoalStatus );

          if( ( pair.Value.DestructionTime != null ) && ( pair.Value.DestructionTime + StatusListTimeout < now ) )
          {
            ROS.Debug()( $"[{ThisNode.Name}] [actionlib] Removing server goal handle for goal id: {pair.Value.GoalId.id}" );
            idsToBeRemoved.Add( pair.Value.GoalId.id );
          }
        }

        statusArray.status_list = goalStatuses.ToArray();
        goalStatusPublisher.publish( statusArray );

        foreach( string id in idsToBeRemoved )
        {
          goalHandles.TryRemove( id, out var dummy );
        }
      //}
    }


    private void CancelCallback( GoalID goalId )
    {
      if( !started )
      {
        return;
      }

      ROS.Debug()( $"[{ThisNode.Name}] [actionlib] The action server has received a new cancel request" );

      if( goalId.id == null )
      {
        var timeZero = DateTime.UtcNow;

        foreach( var valuePair in goalHandles )
        {
          var goalHandle = valuePair.Value;
          if( ( ROS.GetTime( goalId.stamp ) == timeZero ) || ( ROS.GetTime( goalHandle.GoalId.stamp ) < ROS.GetTime( goalId.stamp ) ) )
          {
            if( goalHandle.SetCancelRequested() && ( cancelCallback != null ) )
            {
              cancelCallback( goalHandle );
            }
          }
        }
      }
      else
      {
        ServerGoalHandle<TGoal, TResult, TFeedback> goalHandle;
        var foundGoalHandle = goalHandles.TryGetValue( goalId.id, out goalHandle );
        if( foundGoalHandle )
        {
          if( goalHandle.SetCancelRequested() && ( cancelCallback != null ) )
          {
            cancelCallback( goalHandle );
          }
        }
        else
        {
          // We have not received the goal yet, prepare to cancel goal when it is received
          var goalStatus = new GoalStatus
          {
            status = GoalStatus.RECALLING
          };
          goalHandle = new ServerGoalHandle<TGoal, TResult, TFeedback>(this, goalId, goalStatus, null)
          {
            DestructionTime = ROS.GetTime(goalId.stamp)
          };
          //lock( lockGoalHandles )
          //{
          goalHandles[goalId.id] = goalHandle;
          //}
        }

      }

      // Make sure to set lastCancel based on the stamp associated with this cancel request
      if( ROS.GetTime( goalId.stamp ) > lastCancel )
      {
        lastCancel = ROS.GetTime( goalId.stamp );
      }
    }


    private void GoalCallback( GoalActionMessage<TGoal> goalAction )
    {
      if( !started )
      {
        return;
      }

      GoalID goalId = goalAction.GoalId;

      ROS.Debug()( $"[{ThisNode.Name}] [actionlib] The action server has received a new goal request" );
      ServerGoalHandle<TGoal, TResult, TFeedback> observedGoalHandle = null;
      if( goalHandles.ContainsKey( goalId.id ) )
      {
        observedGoalHandle = goalHandles[goalId.id];
      }

      if( observedGoalHandle != null )
      {
        // The goal could already be in a recalling state if a cancel came in before the goal
        if( observedGoalHandle.GoalStatus.status == GoalStatus.RECALLING )
        {
          observedGoalHandle.GoalStatus.status = GoalStatus.RECALLED;
          PublishResult( observedGoalHandle.GoalStatus, null ); // Empty result
        }
      }
      else
      {
        // Create and register new goal handle
        GoalStatus goalStatus = new GoalStatus();
        goalStatus.status = GoalStatus.PENDING;
        var newGoalHandle = new ServerGoalHandle<TGoal, TResult, TFeedback>( this, goalId,
            goalStatus, goalAction.Goal
        );
        newGoalHandle.DestructionTime = ROS.GetTime( goalId.stamp );
        //lock( lockGoalHandles )
        //{
          goalHandles[goalId.id] = newGoalHandle;
        //}
        goalCallback?.Invoke( newGoalHandle );
      }
    }


    private void SpinCallback( object state )
    {
      if( started )
        PublishStatus();
    }


    private (int seconds, int milliseconds) SplitSeconds( double exactSeconds )
    {
      int seconds = (int)exactSeconds;
      int milliseconds = (int)( ( exactSeconds - seconds ) * 1000 );

      return (seconds, milliseconds);
    }


    private class SpinCallbackImplementation : CallbackInterface
    {
      private Action callback;


      public SpinCallbackImplementation( Action callback )
      {
        this.callback = callback;
      }


      public override void AddToCallbackQueue( ISubscriptionCallbackHelper helper, RosMessage msg, bool nonconst_need_copy, ref bool was_full, TimeData receipt_time )
      {
        throw new NotImplementedException();
      }


      public override void Clear()
      {
        throw new NotImplementedException();
      }


      internal override CallResult Call()
      {
        callback();
        return CallResult.Success;
      }
    }
  }
}
