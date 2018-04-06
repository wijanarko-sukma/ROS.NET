using System;
using Uml.Robotics.XmlRpc;
using Xunit;

namespace Uml.Robotics.Ros.UnitTests
{
  public class RpcValueFacts
  {
    [Fact]
    public void CheckSimpleArrayValuesRoundTrip()
    {
      var now = DateTime.UtcNow;
      var v = new XmlRpcValue();
      v[0].Set( -456 );
      v[1].Set( true );
      v[2].Set( 123.0 );
      v[3].Set( new byte[] { 0, 9, 8, 7, 6, 5, 4, 3, 2, 1 } );
      v[4].Set( now );
      v[5].Set( "Test String" );

      var xml = v.ToXml();

      var w = new XmlRpcValue();
      w.FromXml( xml );

      Assert.Equal( XmlRpcType.Int, w[0].Type );
      Assert.Equal( v[0].GetInt(), w[0].GetInt() );
      Assert.Equal( -456, w[0].GetInt() );

      Assert.Equal( XmlRpcType.Boolean, w[1].Type );
      Assert.Equal( v[1].GetBool(), w[1].GetBool() );
      Assert.Equal( true, w[1].GetBool() );

      Assert.Equal( XmlRpcType.Double, w[2].Type );
      Assert.Equal( v[2].GetDouble(), w[2].GetDouble(), 2 );
      Assert.Equal( 123.0, w[2].GetDouble(), 2 );

      Assert.Equal( XmlRpcType.Base64, w[3].Type );
      Assert.Equal( v[3].GetBinary(), w[3].GetBinary() );
      Assert.Equal( new byte[] { 0, 9, 8, 7, 6, 5, 4, 3, 2, 1 }, w[3].GetBinary() );

      Assert.Equal( XmlRpcType.DateTime, w[4].Type );
      Assert.True( Math.Abs( ( v[4].GetDateTime() - w[4].GetDateTime() ).TotalSeconds ) < 1 );
      Assert.True( Math.Abs( ( now - w[4].GetDateTime() ).TotalSeconds ) < 1 );

      Assert.Equal( XmlRpcType.String, w[5].Type );
      Assert.Equal( v[5].GetString(), w[5].GetString() );
      Assert.Equal( "Test String", w[5].GetString() );
    }

    [Fact]
    public void CheckStructRoundTrip()
    {
      var today = DateTime.Today;
      var v = new XmlRpcValue();
      v.Set( "memberInt", 789 );
      v.Set( "memberBool", true );
      v.Set( "memberDouble", 765.678 );
      v.Set( "memberBinary", new byte[] { 0, 2, 4, 6, 8, 10, 12 } );
      v.Set( "memberString", "qwerty" );
      v.Set( "memberDate", today );

      var innerArray = new XmlRpcValue( 1, 2.0, "three", today );
      v.Set( "memberArray", innerArray );

      var innerStruct = new XmlRpcValue();
      innerStruct.Copy( v );
      v.Set( "memberStruct", innerStruct );

      var xml = v.ToXml();

      var w = new XmlRpcValue();
      w.FromXml( xml );
      Assert.Equal( 8, w.Count );

      Assert.Equal( XmlRpcType.Struct, w.Type );
      Assert.True( w.HasMember( "memberInt" ) );
      Assert.True( w.HasMember( "memberBool" ) );
      Assert.True( w.HasMember( "memberDouble" ) );
      Assert.True( w.HasMember( "memberBinary" ) );
      Assert.True( w.HasMember( "memberString" ) );
      Assert.True( w.HasMember( "memberDate" ) );
      Assert.True( w.HasMember( "memberArray" ) );
      Assert.True( w.HasMember( "memberStruct" ) );

      Assert.Equal( XmlRpcType.Int, w["memberInt"].Type );
      Assert.Equal( XmlRpcType.Boolean, w["memberBool"].Type );
      Assert.Equal( XmlRpcType.Double, w["memberDouble"].Type );
      Assert.Equal( XmlRpcType.Base64, w["memberBinary"].Type );
      Assert.Equal( XmlRpcType.String, w["memberString"].Type );
      Assert.Equal( XmlRpcType.DateTime, w["memberDate"].Type );
      Assert.Equal( XmlRpcType.Array, w["memberArray"].Type );
      Assert.Equal( XmlRpcType.Struct, w["memberStruct"].Type );
      Assert.Equal( 4, w["memberArray"].Count );
      Assert.Equal( 7, w["memberStruct"].Count );

      Action<XmlRpcValue> checkValueOneLevel = ( XmlRpcValue value ) =>
      {
        Assert.Equal( 789, value["memberInt"].GetInt() );
        Assert.Equal( true, value["memberBool"].GetBool() );
        Assert.Equal( 765.678, value["memberDouble"].GetDouble(), 3 );
        Assert.Equal( new byte[] { 0, 2, 4, 6, 8, 10, 12 }, value["memberBinary"].GetBinary() );
        Assert.Equal( "qwerty", value["memberString"].GetString() );
        Assert.True( Math.Abs( ( today - value["memberDate"].GetDateTime() ).TotalSeconds ) < 1 );

        var a = value["memberArray"];
        Assert.Equal( 4, a.Count );
        Assert.Equal( XmlRpcType.Array, a.Type );
        Assert.Equal( 1, a[0].GetInt() );
        Assert.Equal( 2.0, a[1].GetDouble() );
        Assert.Equal( "three", a[2].GetString() );
        Assert.True( Math.Abs( ( today - a[3].GetDateTime() ).TotalSeconds ) < 1 );
      };

      checkValueOneLevel( w );
      checkValueOneLevel( w["memberStruct"] );
    }
  }
}
