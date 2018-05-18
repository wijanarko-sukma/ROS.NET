﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace Uml.Robotics.XmlRpc
{
  public class XmlRpcDispatch
  {
    [Flags]
    public enum EventType
    {
      NoEvent = 0,
      ReadableEvent = 1,
      WritableEvent = 2,
      Exception = 4
    }

    private class DispatchRecord
    {
      public XmlRpcSource Client { get; set; }
      public EventType Mask { get; set; }
    }


    private List<DispatchRecord> sources = new List<DispatchRecord>();


    public void AddSource( XmlRpcSource source, EventType eventMask )
    {
      sources.Add( new DispatchRecord { Client = source, Mask = eventMask } );
    }


    public void RemoveSource( XmlRpcSource source )
    {
      sources.RemoveAll( x => x.Client == source );
    }


    public void SetSourceEvents( XmlRpcSource source, EventType eventMask )
    {
      foreach( var record in sources.Where( x => x.Client == source ) )
      {
        record.Mask |= eventMask;
      }
    }

    List<Socket> checkRead = new List<Socket>();
    List<Socket> checkWrite = new List<Socket>();
    List<Socket> checkError = new List<Socket>();


    private void CheckSources( IEnumerable<DispatchRecord> sources, TimeSpan timeout, List<XmlRpcSource> toRemove )
    {
      const EventType ALL_EVENTS = EventType.ReadableEvent | EventType.WritableEvent | EventType.Exception;

      checkRead.Clear();
      checkWrite.Clear();
      checkError.Clear();
      //int a = 0, b = 0, c = 0, d = sources.Count();
      foreach ( var src in sources )
      {
        Socket sock = src.Client.getSocket();
        if( sock == null )
          continue;

        //if (!sock.Connected)
        //continue;

        //bool isValidConnection = sock.Connected || (!sock.Connected && sock.LocalEndPoint.ToString().StartsWith("0.0.0.0"));
        //if (!isValidConnection)
        //  continue;

        
        var mask = src.Mask;
        if (mask.HasFlag(EventType.ReadableEvent))
        {
          checkRead.Add(sock);
          //a++;
        }
        if (mask.HasFlag(EventType.WritableEvent))
        {
          checkWrite.Add(sock);
          //b++;
        }
        if (mask.HasFlag(EventType.Exception))
        {
          checkError.Add(sock);
          //c++;
        }
      }

      // Check for events
      Socket.Select( checkRead, checkWrite, checkError, (int)( timeout.Milliseconds * 1000.0 ) );
      //Thread.Yield();
      //Console.WriteLine($"{d} - {a} - {b} - {c}");

      if( checkRead.Count + checkWrite.Count + checkError.Count == 0 )
        return;

      // Process events
      foreach( var record in sources )
      {
        XmlRpcSource src = record.Client;
        EventType newMask = ALL_EVENTS;
        Socket sock = src.getSocket();
        if( sock == null )
          continue;

        // if you select on multiple event types this could be ambiguous
        if( checkRead.Contains( sock ) )
          newMask &= src.HandleEvent( EventType.ReadableEvent );
        if( checkWrite.Contains( sock ) )
          newMask &= src.HandleEvent( EventType.WritableEvent );
        if( checkError.Contains( sock ) )
          newMask &= src.HandleEvent( EventType.Exception );

        if( newMask == EventType.NoEvent )
        {
          toRemove.Add( src );
        }
        else
        {
          record.Mask = newMask;
        }
      }
    }


    public void Work( TimeSpan timeSlice )
    {
      var endTime = DateTime.UtcNow.Add( timeSlice );
      var toRemove = new List<XmlRpcSource>();
      while ( sources.Count > 0 )
      {
        var sourcesCopy = sources.GetRange( 0, sources.Count );        
        CheckSources( sourcesCopy, timeSlice, toRemove );

        foreach( var src in toRemove )
        {
          RemoveSource( src );
          if( !src.KeepOpen )
            src.Close();
        }
        toRemove.Clear();

        // check whether end time has been passed
        if ( DateTime.UtcNow > endTime )
          break;        
      }
    }


    public void Clear()
    {
      sources.Clear();
    }
  }
}
