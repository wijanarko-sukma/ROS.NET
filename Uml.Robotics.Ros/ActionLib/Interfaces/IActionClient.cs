using Messages.actionlib_msgs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Uml.Robotics.Ros.ActionLib
{
  public interface IActionClient<TGoal, TResult, TFeedback>
      where TGoal : InnerActionMessage, new()
      where TResult : InnerActionMessage, new()
      where TFeedback : InnerActionMessage, new()
  {
    string Name { get; }
    Publisher<GoalActionMessage<TGoal>> GoalPublisher { get; }
    Publisher<GoalID> CancelPublisher { get; }
    void TransitionToState( ClientGoalHandle<TGoal, TResult, TFeedback> goalHandle, CommunicationState state );
    void ProcessLost( ClientGoalHandle<TGoal, TResult, TFeedback> goalHandle );
    TGoal CreateGoal();
    void Shutdown();
    bool WaitForActionServerToStart( TimeSpan? timeout = null );
    bool WaitForActionServerToStartSpinning( TimeSpan? timeout, SingleThreadSpinner spinner );
    bool IsServerConnected();
    int? PreemptTimeout { get; }
    Task<TResult> SendGoalAsync(
       TGoal goal,
       Action<ClientGoalHandle<TGoal, TResult, TFeedback>> OnTransistionCallback = null,
       Action<ClientGoalHandle<TGoal, TResult, TFeedback>, FeedbackActionMessage<TFeedback>> OnFeedbackCallback = null,
       CancellationToken cancel = default( CancellationToken )
    );
    Task<TResult> SendGoalAsync(
       TGoal goal,
       CancellationToken cancel = default( CancellationToken )
    );
    Task<TResult> SendGoalAsync(
       TGoal goal,
       Action<ClientGoalHandle<TGoal, TResult, TFeedback>> OnTransistionCallback = null,
       CancellationToken cancel = default( CancellationToken )
    );
    Task<TResult> SendGoalAsync(
       TGoal goal,
       Action<ClientGoalHandle<TGoal, TResult, TFeedback>, FeedbackActionMessage<TFeedback>> OnFeedbackCallback = null,
       CancellationToken cancel = default( CancellationToken )
    );
  }

  public class ActionFailedExeption
      : Exception
  {
    private static readonly List<string> GoalStates = new List<string>() { "PENDING", "ACTIVE", "PREEMPTED", "SUCCEEDED", "ABORTED", "REJECTED", "PREEMPTING", "RECALLING", "RECALLED", "LOST" };

    public static string GetGoalStatusString( GoalStatus goalStatus )
    {
      if( goalStatus == null )
        return "INVALID GOAL STATUS 'null'";

      if( goalStatus.status >= 0 && goalStatus.status < GoalStates.Count )
      {
        return GoalStates[goalStatus.status];
      }

      return $"INVALID GOAL STATUS {goalStatus.status}";
    }

    public ActionFailedExeption( string actionName, Messages.actionlib_msgs.GoalStatus goalStatus )
        : base( $"The action '{actionName}' failed with final goal status '{GetGoalStatusString( goalStatus )}': {goalStatus?.text}" )
    {
      this.ActionName = actionName;
      this.FinalGoalStatus = ( goalStatus )?.status ?? GoalStatus.LOST;
      this.StatusText = goalStatus?.text;
    }

    public string ActionName { get; }
    public byte FinalGoalStatus { get; }
    public string StatusText { get; }
  }
}
