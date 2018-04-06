using Xunit;
using Uml.Robotics.Ros;
using tf = Uml.Robotics.Ros.Transforms;
using System.Collections.Generic;
using Xunit.Extensions;
using System;

namespace Uml.Robotics.Ros.UnitTests
{
  public class QuaternionTheories
  {
    [Theory, MemberData( nameof( QuaternionData ) )]
    public void Should_QuaternionToRPYToQuaternion( tf.Quaternion quaternion, tf.Vector3 expectedResult = null )
    {
      //Utils.WaitForDebugger();

      Console.WriteLine( "" );
      Console.WriteLine( "Test: QuaternionToRPYToQuaternion" );
      Console.WriteLine( "=================================" );
      var rpy = quaternion.getRPY();
      var resultQuaternion = tf.Quaternion.FromRPY( rpy );
      Console.WriteLine( "Input quaternion:" );
      Console.WriteLine( quaternion.ToString() );
      Console.WriteLine( "Roll, Pitch, Yaw:" );
      Console.WriteLine( rpy.ToString() );
      Console.WriteLine( "Output quaternion:" );
      Console.WriteLine( resultQuaternion.ToString() );

      // The output of conversion to rpy and back is a unit quaternion.
      // Hence, for comparison of input and output quaternion, 
      // the input quaternion has to be normalized.
      var unitQuaternion = quaternion / quaternion.abs;
      Console.WriteLine( "Normalized input quaternion:" );
      Console.WriteLine( unitQuaternion.ToString() );
      var tolerance = 1E-05;
      Assert.InRange( Math.Abs( unitQuaternion.x - resultQuaternion.x ), 0.0, tolerance );
      Assert.InRange( Math.Abs( unitQuaternion.y - resultQuaternion.y ), 0.0, tolerance );
      Assert.InRange( Math.Abs( unitQuaternion.z - resultQuaternion.z ), 0.0, tolerance );
      Assert.InRange( Math.Abs( unitQuaternion.w - resultQuaternion.w ), 0.0, tolerance );
      if( expectedResult != null )
      {
        Assert.InRange( Math.Abs( rpy.x - expectedResult.x ), 0.0, tolerance );
        Assert.InRange( Math.Abs( rpy.y - expectedResult.y ), 0.0, tolerance );
        Assert.InRange( Math.Abs( rpy.z - expectedResult.z ), 0.0, tolerance );
      }
    }

    public static IEnumerable<object[]> QuaternionData
    {
      get
      {
        return new[]
        {
          new object[] {new tf.Quaternion(0, 0, 0, 1)},
          new object[] {new tf.Quaternion(2, 2, 2, 1)},
          new object[] {new tf.Quaternion(3, 2, 4, 1)},
          // The following quaternion should rotate a vector around the y-axis with angle pi/2.
          // Hence, RPY should be (0,pi/2,0). 
          // See: https://en.wikipedia.org/wiki/Quaternions_and_spatial_rotation
          // and http://run.usc.edu/cs520-s12/quaternions/quaternions-cs520.pdf, slide 18.
          new object[] {new tf.Quaternion(0, Math.Sqrt(2.0)/2.0, 0, Math.Sqrt(2.0)/2.0), new tf.Vector3(0, Math.PI/2.0, 0)},
          new object[] {new tf.Quaternion(Math.Sqrt(2.0)/2.0, 0, 0, Math.Sqrt(2.0)/2.0), new tf.Vector3(Math.PI/2.0, 0, 0)},
          new object[] {new tf.Quaternion(0, 0, Math.Sqrt(2.0)/2.0, Math.Sqrt(2.0)/2.0), new tf.Vector3(0, 0, Math.PI/2.0)},
          new object[] {new tf.Quaternion(0, -Math.Sqrt(2.0)/2.0, 0, Math.Sqrt(2.0)/2.0), new tf.Vector3(0, -Math.PI/2.0, 0)},
          new object[] {new tf.Quaternion(-Math.Sqrt(2.0)/2.0, 0, 0, Math.Sqrt(2.0)/2.0), new tf.Vector3(-Math.PI/2.0, 0, 0)},
          new object[] {new tf.Quaternion(0, 0, -Math.Sqrt(2.0)/2.0, Math.Sqrt(2.0)/2.0), new tf.Vector3(0, 0, -Math.PI/2.0)}
        };
      }
    }
  }
}