using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages;
using Messages.std_msgs;
using Xunit;
using Uml.Robotics.Ros;
using tf = Uml.Robotics.Ros.Transforms;


namespace Uml.Robotics.Ros.UnitTests
{
  [Collection( RosFixture.ROS_COLLECTION )]
  public class TransformerFacts
  {
    private tf.Transformer transformer;
    private Time when;
    private RosFixture rosFixture;


    public TransformerFacts( RosFixture rosFixture )
    {
      when = ROS.GetTime();
      transformer = new tf.Transformer();
    }


    [Fact]
    public void Should_AddThreeTransforms()
    {
      tf.Transform a2b = new tf.Transform(
          new tf.Quaternion(),
          new tf.Vector3( 0.0, 0.0, 1.0 ),//1.0, 0.0, 0.5),
          when,
          "a",
          "b" );
      tf.Transform b2c = new tf.Transform(
          new tf.Quaternion(),
          new tf.Vector3( 0.0, 0.0, -0.5 ),//-1.0, -0.5, 1.0),
          when,
          "b",
          "c" );
      tf.Transform c2d = new tf.Transform(
          tf.Quaternion.FromRPY( new tf.Vector3( 0.0, 0.0, Math.PI / 4.0 ) ),
          new tf.Vector3( 1.0, 0.0, 0.0 ),
          when,
          "c",
          "d" );
      bool setsuccess = transformer.setTransform( a2b ) && transformer.setTransform( b2c ) &&
                        transformer.setTransform( c2d );

      Assert.Equal( true, setsuccess );
      Console.WriteLine( "This test is not fully implemented. Further testing should be done with the Transforms" );
    }
  }
}
