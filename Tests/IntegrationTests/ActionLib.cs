using System;
using Xunit;

using Uml.Robotics.Ros;
using Uml.Robotics.Ros.ActionLib;

using Messages.control_msgs;
using Messages.actionlib_msgs;
using System.Threading;

namespace Uml.Robotics.Ros.IntegrationTests
{
  [Collection( RosFixture.ROS_COLLECTION )]
  public class ActionLib
  {
    private RosFixture rosFixture;


    public ActionLib( RosFixture rosFixture )
    {
      this.rosFixture = rosFixture;
    }


    [Fact]
    public void Should_CreateGoalAndGetItDone()
    {
      NodeHandle serverNodeHandle = new NodeHandle();
      NodeHandle clientNodeHandle = new NodeHandle();

      Console.WriteLine( "Create server" );
      var actionServer = new ActionServer<FollowJointTrajectoryGoal, FollowJointTrajectoryResult,
          FollowJointTrajectoryFeedback>( serverNodeHandle, "SimpleTest" );

      Console.WriteLine( "Create client" );
      var actionClient = new ActionClient<FollowJointTrajectoryGoal, FollowJointTrajectoryResult,
          FollowJointTrajectoryFeedback>( "SimpleTest", clientNodeHandle );

      Console.WriteLine( "Start Server" );
      actionServer.Start();

      bool goalRegistered = false;
      actionServer.RegisterGoalCallback( ( goalHandle ) =>
      {
        Console.WriteLine( $"Goal registered callback. Joint Name {goalHandle.Goal.trajectory.joint_names[0]}" );
        goalRegistered = true;
      } );

      Console.WriteLine( "Wait for client and server to negotiate connection" );
      bool started = actionClient.WaitForActionServerToStart( new TimeSpan( 0, 0, 20 ) );
      Assert.Equal( true, started );

      var goal = new FollowJointTrajectoryGoal();
      goal.trajectory = new Messages.trajectory_msgs.JointTrajectory();
      goal.trajectory.joint_names = new string[] { "Hallo Welt!" };

      Console.WriteLine( "Send goal from client" );
      var cts = new CancellationTokenSource();
      actionClient.SendGoalAsync( goal, cts.Token ).GetAwaiter().GetResult();

      Console.WriteLine( "Wait for action server receiving the goal" );
      while( !goalRegistered )
      {
        Thread.Sleep( 1 );
      }
    }
  }
}
