using Xunit;
using System.Collections.Generic;
using YAMLParser;
using Xunit.Extensions;
using System;
using System.IO;
using FauxMessages;

namespace Uml.Robotics.Ros.UnitTests
{
  public class StaticResolverFixture : IDisposable
  {
    public StaticResolverFixture()
    {
      // Add stubs of standard messages to resolver
      var msgsStub = new MsgFile( new MsgFileLocation( @"common_msgs\std_msgs\msg\Header.msg", "." ),
          new List<string> { "uint32 seq", "time stamp", "string frame_id" }, "" );
      msgsStub.ParseAndResolveTypes();

      msgsStub = new MsgFile( new MsgFileLocation( @"common_msgs\std_msgs\msg\String.msg", "." ),
          new List<string> { "string data" }, "" );
      msgsStub.ParseAndResolveTypes();

      msgsStub = new MsgFile( new MsgFileLocation( @"common_msgs\std_msgs\msg\Duration.msg", "." ),
          new List<string> { "duration data" }, "" );
      msgsStub.ParseAndResolveTypes();

      msgsStub = new MsgFile( new MsgFileLocation( @"common_msgs\trajectory_msgs\msg\JointTrajectoryPoint.msg", "." ),
          new List<string> { "float64[] positions", "float64[] velocities", "float64[] accelerations", "float64[] effort",
                "duration time_from_start"}, "" );
      msgsStub.ParseAndResolveTypes();

      msgsStub = new MsgFile( new MsgFileLocation( @"common_msgs\trajectory_msgs\msg\JointTrajectory.msg", "." ),
          new List<string> { "std_msgs/Header header", "string[] joint_names", "JointTrajectoryPoint[] points" }, "" );
      msgsStub.ParseAndResolveTypes();

      msgsStub = new MsgFile( new MsgFileLocation( @"common_msgs\control_msgs\msg\JointTolerance.msg", "." ),
          new List<string> { "string name", "float64 position", "float64 velocity", "float64 acceleration" }, "" );
      msgsStub.ParseAndResolveTypes();

      msgsStub = new MsgFile( new MsgFileLocation( @"common_msgs\actionlib_msgs\GoalID.msg", "." ),
          new List<string> { "time stamp", "string id" }, "" );
      msgsStub.ParseAndResolveTypes();

      msgsStub = new MsgFile( new MsgFileLocation( @"common_msgs\actionlib_msgs\GoalStatus.msg", "." ),
          new List<string> { "uint8 PENDING         = 0", "uint8 ACTIVE          = 1",
                "uint8 PREEMPTED       = 2", "uint8 SUCCEEDED       = 3", "uint8 ABORTED         = 4 ",
                "uint8 REJECTED        = 5", "uint8 PREEMPTING      = 6 ", "uint8 RECALLING       = 7 ",
                "uint8 RECALLED        = 8", "uint8 LOST            = 9", "GoalID goal_id", "uint8 status", "string text" }, "" );
      msgsStub.ParseAndResolveTypes();

      msgsStub = new MsgFile( new MsgFileLocation( @"common_msgs\actionlib_msgs\GoalStatusArray.msg", "." ),
          new List<string> { "Header header", "GoalStatus[] status_list" }, "" );
      msgsStub.ParseAndResolveTypes();
    }


    public void Dispose()
    {
    }
  }

  public class ActionFileParserTheories : IClassFixture<StaticResolverFixture>
  {
    StaticResolverFixture resolver;

    public ActionFileParserTheories( StaticResolverFixture resolver )
    {
      this.resolver = resolver;
    }


    [Theory, MemberData( nameof( ActionFileLocations ) )]
    public void Should_ProduceCorrectMD5Sums_When_GivenRosDefaultActionFiles( MsgFileLocation stubFile, string[] lines,
        Dictionary<string, string> md5sums )
    {
      // Parse action file
      var actionFile = new ActionFile( stubFile, lines );
      actionFile.ParseAndResolveTypes();

      // Compare MD5 sums
      Assert.Equal( md5sums["Goal"], MD5.Sum( actionFile.GoalMessage ) );
      Assert.Equal( md5sums["ActionGoal"], MD5.Sum( actionFile.GoalActionMessage ) );
      Assert.Equal( md5sums["Result"], MD5.Sum( actionFile.ResultMessage ) );
      Assert.Equal( md5sums["ActionResult"], MD5.Sum( actionFile.ResultActionMessage ) );
      Assert.Equal( md5sums["Feedback"], MD5.Sum( actionFile.FeedbackMessage ) );
      Assert.Equal( md5sums["ActionFeedback"], MD5.Sum( actionFile.FeedbackActionMessage ) );
    }


    /// <summary>
    /// Sample data for ActionFile parse with MD5 sums retrieved from torch-ros
    /// </summary>
    public static IEnumerable<object[]> ActionFileLocations
    {
      get
      {
        #region SingleJointPosition.Action
        var singleJointPosition = new MsgFileLocation( @"common_msgs\control_msgs\action\SingleJointPosition.action", "." );
        string singleJointPositionContent = @"float64 position
duration min_duration
float64 max_velocity
---
---
Header header
float64 position
float64 velocity
float64 error";
        var singleJointPositionLines = singleJointPositionContent.Split( '\n' );
        var singleJointPositionMd5 = new Dictionary<string, string> {
                    { "Goal", "fbaaa562a23a013fd5053e5f72cbb35c"},
                    { "ActionGoal",  "4b0d3d091471663e17749c1d0db90f61"},
                    { "Result", "d41d8cd98f00b204e9800998ecf8427e"},
                    { "ActionResult", "1eb06eeff08fa7ea874431638cb52332"},
                    { "Feedback", "8cee65610a3d08e0a1bded82f146f1fd"},
                    { "ActionFeedback", "3503b7cf8972f90d245850a5d8796cfa"}
                };
        #endregion

        #region FollowJointTrajectory.Action
        var followJointTrajectory = new MsgFileLocation( @"common_msgs\control_msgs\action\FollowJointTrajectory.action", "." );
        string followJointTrajectoryContent = @"# The joint trajectory to follow
trajectory_msgs/JointTrajectory trajectory

# Tolerances for the trajectory.  If the measured joint values fall
# outside the tolerances the trajectory goal is aborted.  Any
# tolerances that are not specified (by being omitted or set to 0) are
# set to the defaults for the action server (often taken from the
# parameter server).

# Tolerances applied to the joints as the trajectory is executed.  If
# violated, the goal aborts with error_code set to
# PATH_TOLERANCE_VIOLATED.
JointTolerance[] path_tolerance

# To report success, the joints must be within goal_tolerance of the
# final trajectory value.  The goal must be achieved by time the
# trajectory ends plus goal_time_tolerance.  (goal_time_tolerance
# allows some leeway in time, so that the trajectory goal can still
# succeed even if the joints reach the goal some time after the
# precise end time of the trajectory).
#
# If the joints are not within goal_tolerance after ""trajectory finish
# time"" + goal_time_tolerance, the goal aborts with error_code set to
# GOAL_TOLERANCE_VIOLATED
JointTolerance[] goal_tolerance
duration goal_time_tolerance

---
int32 error_code
int32 SUCCESSFUL = 0
int32 INVALID_GOAL = -1
int32 INVALID_JOINTS = -2
int32 OLD_HEADER_TIMESTAMP = -3
int32 PATH_TOLERANCE_VIOLATED = -4
int32 GOAL_TOLERANCE_VIOLATED = -5

# Human readable description of the error code. Contains complementary
# information that is especially useful when execution fails, for instance:
# - INVALID_GOAL: The reason for the invalid goal (e.g., the requested
#   trajectory is in the past).
# - INVALID_JOINTS: The mismatch between the expected controller joints
#   and those provided in the goal.
# - PATH_TOLERANCE_VIOLATED and GOAL_TOLERANCE_VIOLATED: Which joint
#   violated which tolerance, and by how much.
string error_string

---
Header header
string[] joint_names
trajectory_msgs/JointTrajectoryPoint desired
trajectory_msgs/JointTrajectoryPoint actual
trajectory_msgs/JointTrajectoryPoint error";

        var followJointTrajectoryLines = followJointTrajectoryContent.Split( '\n' );
        var followJointTrajectoryMd5 = new Dictionary<string, string> {
                    { "Goal", "69636787b6ecbde4d61d711979bc7ecb"},
                    { "ActionGoal",  "cff5c1d533bf2f82dd0138d57f4304bb"},
                    { "Result", "493383b18409bfb604b4e26c676401d2"},
                    { "ActionResult", "c4fb3b000dc9da4fd99699380efcc5d9"},
                    { "Feedback", "10817c60c2486ef6b33e97dcd87f4474"},
                    { "ActionFeedback", "d8920dc4eae9fc107e00999cce4be641"}
                };
        #endregion

        return new[]
        {
          // Action File with empty result
          new object[] { singleJointPosition, singleJointPositionLines, singleJointPositionMd5 },
          // Complete Action File
          new object[] { followJointTrajectory, followJointTrajectoryLines, followJointTrajectoryMd5 },
        };
      }
    }

  }
}