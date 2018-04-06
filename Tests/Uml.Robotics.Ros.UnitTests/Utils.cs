using System;
using System.IO;
using System.Reflection;

namespace Uml.Robotics.Ros.UnitTests
{
  public class Utils
  {
    public static void WaitForDebugger()
    {
      Console.WriteLine( "Unit Test is halted until the debugger is attached to following PID:" );
      Console.WriteLine( $"Process ID: {System.Diagnostics.Process.GetCurrentProcess().Id}" );
      while( !System.Diagnostics.Debugger.IsAttached )
      {
        System.Threading.Thread.Sleep( 1 );
      }
    }

    public static string DataPath
    {
      get
      {
        var basePath = Path.GetDirectoryName( typeof( Utils ).GetTypeInfo().Assembly.Location );

        var path = "TestData";
        for( int i = 0; i < 4; i++ )
        {
          var fullPath = Path.Combine( basePath, path );
          if( Directory.Exists( fullPath ) )
            return fullPath;
          path = Path.Combine( "..", path );
        }

        throw new Exception( "TestData directory not found" );
      }
    }
  }
}
