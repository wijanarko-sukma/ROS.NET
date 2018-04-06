using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using sensor_msgs = Messages.sensor_msgs;

namespace Uml.Robotics.Ros.UnitTests
{
  public class MessageSerializationFacts
  {
    Random r = new Random();

    public MessageSerializationFacts()
    {
      MessageTypeRegistry.Default.ParseAssemblyAndRegisterRosMessages( typeof( RosMessage ).GetTypeInfo().Assembly );
      MessageTypeRegistry.Default.ParseAssemblyAndRegisterRosMessages( typeof( sensor_msgs.Image ).GetTypeInfo().Assembly );
      ServiceTypeRegistry.Default.ParseAssemblyAndRegisterRosServices( typeof( sensor_msgs.Image ).GetTypeInfo().Assembly );
    }

    [Fact]
    public void DeserializeRandomizedMessagesAndCompareRecursivelyToOriginals()
    {
      // randomize messages of all known types, and serialize one of each of them
      var messageTypes = MessageTypeRegistry.Default.GetTypeNames().ToList();
      foreach( var messageType in messageTypes )
      {
        var original = RosMessage.Generate( messageType );
        Assert.NotNull( original );
        original.Randomize();
        var originalSerialized = original.Serialize();
        Assert.NotNull( originalSerialized );

        RosMessage msg = RosMessage.Generate( messageType );
        Assert.NotNull( msg );

        msg.Deserialize( originalSerialized );
        Assert.Equal( original, msg );
      }
    }

    static Dictionary<string, string> LoadSums( string fileName )
    {
      var lines = File.ReadAllLines( Path.Combine( Utils.DataPath, fileName ) );
      return lines.Select( s => s.Split( ' ' ) ).ToDictionary( x => x[0], x => x[1] );
    }

    Lazy<Dictionary<string, string>> msgSumsLazy = new Lazy<Dictionary<string, string>>( () => LoadSums( "msg_sums.txt" ) );
    Lazy<Dictionary<string, string>> srvSumsLazy = new Lazy<Dictionary<string, string>>( () => LoadSums( "srv_sums.txt" ) );

    [Fact]
    public void CheckMsgMD5()
    {
      var msgSums = msgSumsLazy.Value;
      var typeRegistry = MessageTypeRegistry.Default.TypeRegistry;
      foreach( var key in msgSums.Keys.Where( typeRegistry.ContainsKey ) )
      {
        var msg = RosMessage.Generate( key );
        string desiredSum = msgSums[key];
        string actualSum = msg.MD5Sum();
        Assert.Equal( desiredSum, actualSum );
      }
    }

    [Fact]
    public void CheckSrvMD5()
    {
      var srvSums = srvSumsLazy.Value;
      var typeRegistry = ServiceTypeRegistry.Default.TypeRegistry;
      foreach( var key in srvSums.Keys.Where( typeRegistry.ContainsKey ) )
      {
        var srv = RosService.Generate( key );
        string desiredSum = srvSums[key];
        string actualSum = srv.MD5Sum();
        Assert.Equal( desiredSum, actualSum );
      }
    }
  }
}
