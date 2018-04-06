using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Uml.Robotics.Ros;
using System.Collections.ObjectModel;
using System;

namespace Uml.Robotics.Ros.Samples
{
  public class Program
  {
    static void Main( string[] args )
    {
      var options = new CommandLineApplication( throwOnUnexpectedArg: false );
      CommandOption levelOption = options.Option( "-l | --level <level>",
          "Defines the verbosity level for displaying messages 0-4; default: 0 - very verbose",
          CommandOptionType.SingleValue
      );
      CommandOption filterOption = options.Option( "-f | --filter <ignorestring>",
          "Semicolon sepperated list of strings that should filter out messages; default: ''",
          CommandOptionType.SingleValue
      );
      options.HelpOption( "-h | --help" );

      options.OnExecute( () =>
      {
        int level;
        try
        {
          level = int.Parse( levelOption.Value() );
        }
        catch( Exception e )
        {
          level = 0;
        }
        Console.WriteLine( "Using verbose level " + level );
        var rosoutDebug = new RosoutDebug( level, args );

        if( filterOption.HasValue() )
        {
          Console.WriteLine( "Using filter string: " + filterOption.Value() );
          rosoutDebug.Filter = filterOption.Value();
        }
        return 0;
      } );

      options.Execute( args );
    }
  }


  public class RosoutDebug
  {
    Subscriber subscriber;
    private NodeHandle nodeHandle;
    private int verboseLevel;

    // Right now, these are split from a semicolon-delimited string, and matching could be improved
    private List<string> filter = new List<string>();


    public RosoutDebug( int verboseLevel, string[] args )
    {
      this.verboseLevel = verboseLevel;
      ROS.Init( args, "RosoutDebug" );
      var asyncSpinner = new AsyncSpinner();
      asyncSpinner.Start();
      nodeHandle = new NodeHandle();
      Init();
    }


    /// <summary>
    /// A semicolon-delimited list of substrings that, when found in a concatenation of any rosout msgs fields,
    /// will not display that message
    /// </summary>
    public string Filter
    {
      get { return filter.ToString(); }
      set
      {
        filter.Clear();
        filter.AddRange( value.Split( ';' ) );
      }
    }


    public void Shutdown()
    {
      if( subscriber != null )
      {
        subscriber.shutdown();
        subscriber = null;
      }
      if( nodeHandle != null )
      {
        nodeHandle.shutdown();
        nodeHandle = null;
      }
      ROS.shutdown();
    }


    private void Init()
    {
      while( !ROS.isStarted() )
      {
        Thread.Sleep( 100 );
      }
      if( nodeHandle == null )
      {
        nodeHandle = new NodeHandle();
      }
      if( subscriber == null )
      {
        subscriber = nodeHandle.subscribe<Messages.rosgraph_msgs.Log>( "/rosout_agg", 100, Callback );
      }
    }


    private void Callback( Messages.rosgraph_msgs.Log msg )
    {
      string teststring = string.Format( "{0}\n{1}\n{2}\n{3}\n{4}\n{5}",
          msg.level,
          msg.msg,
          msg.name,
          msg.file,
          msg.function, msg.line
      );

      bool containesFilteredString = filter.Count > 0 && filter.Any( teststring.Contains );
      bool isFilteredByVerboseLevel = Math.Pow( 2, this.verboseLevel ) > msg.level;
      if( containesFilteredString || isFilteredByVerboseLevel )
      {
        return;
      }

      RosoutString rss = new RosoutString(
          ( 1.0 * msg.header.stamp.data.sec + ( 1.0 * msg.header.stamp.data.nsec ) / 1000000000.0 ),
          msg.level,
          msg.msg,
          msg.name,
          msg.file,
          msg.function,
          "" + msg.line
      );

      Console.WriteLine( rss.ToString() );
    }
  }


  public class RosoutString
  {
    //converts the int warning value from msg to a meaningful string
    private string ConvertVerbosityLevel( int level )
    {
      switch( level )
      {
        case 1:
          return "DEBUG";
        case 2:
          return "INFO ";
        case 4:
          return "WARN ";
        case 8:
          return "ERROR";
        case 16:
          return "FATAL";
        default:
          return "NONE " + level;
      }
    }

    public string Timestamp { get; set; }
    public string Level { get; set; }
    public string Msgdata { get; set; }
    public string Msgname { get; set; }
    public string Filename { get; set; }
    public string Functionname { get; set; }
    public string Lineno { get; set; }
    public double Stamp { get; set; }
    public int LevelNr = 0;


    public RosoutString( double stamp, int level, string data, string name, string filename, string function,
        string lineno )
    {
      this.Stamp = stamp;
      this.Timestamp = "" + stamp;
      this.LevelNr = level;
      this.Level = ConvertVerbosityLevel( level );
      this.Msgdata = data;
      this.Msgname = name;
      this.Filename = filename;
      this.Functionname = function;
      this.Lineno = lineno;
    }


    public override string ToString()
    {
      var result = $"[{Timestamp}] [{Level}] <{Filename}:{Lineno}::{Functionname}> \"{Msgname}\" - {Msgdata}";
      return result;
    }
  }
}