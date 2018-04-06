using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uml.Robotics.Ros;

namespace Uml.Robotics.Ros.Samples
{
  class Program
  {
    private enum op
    {
      set,
      get,
      has,
      del,
      list
    }

    private int _result = -1;

    public int result()
    {
      return _result;
    }

    private Program( string[] args )
    {
      op OP = op.list;
      try
      {
        OP = (op)Enum.Parse( typeof( op ), args[0], true );
        if( args.Length == 0 )
        {
          ShowUsage( 0 );
          return;
        }
      }
      catch( Exception ex )
      {
        Console.WriteLine( "Caught exception: " );
        Console.WriteLine( ex.Message );
      }

      if( args.Length == 1 && OP != op.list )
      {
        ShowUsage( 1 );
        return;
      }
      switch( OP )
      {
        case op.del:
          if( !Param.Del( Names.Resolve( args[1] ) ) )
            Console.WriteLine( "Failed to delete " + args[1] );
          break;
        case op.get:
          {
            string s = null;
            Param.Get( args[1], out s );
            if( s != null )
              Console.WriteLine( s );
          }
          break;
        case op.list:
          {
            foreach( string s in Param.List() )
              Console.WriteLine( s );
          }
          break;
        case op.set:
          Param.Set( args[1], args[2] );
          break;
      }
    }

    private void ShowUsage( int p )
    {
      switch( p )
      {
        case 0:
          Console.WriteLine( "Valid operations:" );
          foreach( op o in (op[])Enum.GetValues( typeof( op ) ) )
            Console.WriteLine( "\t" + o.ToString() );
          break;
        case 1:
          Console.WriteLine( "You must specify a param name for this rosparam operation." );
          break;
      }
    }

    static void Main( string[] args )
    {
      IDictionary<string, string> remappings;
      RemappingHelper.GetRemappings( ref args, out remappings );
      Network.Init( remappings );
      Master.init( remappings );
      ThisNode.Init( "", remappings, (int)( InitOption.AnonymousName | InitOption.NoRousout ) );
      Param.Init( remappings );
      //ROS.Init(args, "");
      new Program( args ).result();

      // Demo how to get/set parameters directly
      Param.Set( "/test/string", "Hello" );
      Param.Set( "/test/number", 42 );
      string result;
      if( Param.Get( "/test/string", out result ) )
      {
        Console.WriteLine( $"Got {result}" );
      }
      else
      {
        Console.WriteLine( "Haven't got any value for /test/string" );
      }
    }
  }
}
