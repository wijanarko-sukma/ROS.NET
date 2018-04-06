using FauxMessages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace YAMLParser
{
  public class ActionFileParser
  {
    private List<MsgFileLocation> actionFileLocations;

    public ActionFileParser( List<MsgFileLocation> actionFileLocations )
    {
      this.actionFileLocations = actionFileLocations;
    }

    public List<ActionFile> GenerateRosMessageClasses()
    {
      var actionMessages = GenerateMessageFiles();

      return actionMessages;
    }


    private List<ActionFile> GenerateMessageFiles()
    {
      var result = new List<ActionFile>();

      // Generate message files
      foreach( var fileLocation in actionFileLocations )
      {
        result.Add( new ActionFile( fileLocation ) );
      }

      // Resolve type dependencies between message files
      Console.WriteLine( $"Start parsing action files" );
      foreach( var messageFile in result )
      {
        Console.WriteLine( $"Parse action file: {messageFile.Name}" );
        messageFile.ParseAndResolveTypes();
      }
      Console.WriteLine( $"Parsing of action files completed" );

      return result;
    }

  }
}
