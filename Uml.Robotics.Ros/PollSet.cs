using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Uml.Robotics.Ros
{
  public class PollSet : PollSignal
  {
    const int SELECT_TIMEOUT = 5 * 1000;    // poll thread select timeout in microseconds (interval checking for new sockets)

    public delegate void SocketUpdateFunc( int stufftodo );

    private static ConcurrentDictionary<int, Socket> sockets = new ConcurrentDictionary<int, Socket>();

    public PollSet()
        : base( null )
    {
      base.Op = Update;
    }

    public new void Dispose()
    {
      base.Dispose();
      if( DisposingEvent != null )
        DisposingEvent.Invoke();
    }

    public delegate void DisposingDelegate();

    public event DisposingDelegate DisposingEvent;

    public bool AddSocket( Socket socket, SocketUpdateFunc updateFunc )
    {
      return AddSocket( socket, updateFunc, null );
    }

    public bool AddSocket( Socket socket, SocketUpdateFunc updateFunc, TcpTransport transport )
    {
      socket.Info = new Socket.SocketInfo { Func = updateFunc, Transport = transport };

      var socketHash = socket.GetHashCode();
      if (sockets.ContainsKey(socketHash))
        sockets[socketHash].Dispose();
      sockets[socketHash] = socket;
      return true;
    }

    public bool RemoveSocket( Socket socket )
    {
      sockets.TryRemove( socket.GetHashCode(), out var dummy );
      socket.Dispose();
      return true;
    }

    public bool AddEvents( Socket socket, int events )
    {
      if( socket != null && socket.Info != null )
        socket.Info.Events |= events;
      return true;
    }

    public bool RemoveEvents( Socket socket, int events )
    {
      if( socket != null && socket.Info != null )
        socket.Info.Events &= ~events;
      return true;
    }

    List<System.Net.Sockets.Socket> checkWrite = new List<System.Net.Sockets.Socket>();
    List<System.Net.Sockets.Socket> checkRead = new List<System.Net.Sockets.Socket>();
    List<System.Net.Sockets.Socket> checkError = new List<System.Net.Sockets.Socket>();
    List<Uml.Robotics.Ros.Socket> lsocks = new List<Uml.Robotics.Ros.Socket>();

    public void Update()
    {
      checkWrite.Clear();
      checkRead.Clear();
      checkError.Clear();
      lsocks.Clear();

      lock ( sockets )
      {
        foreach( Socket s in sockets.Values )
        {
          lsocks.Add( s );
          if( ( s.Info.Events & Socket.POLLIN ) != 0 )
            checkRead.Add( s.realSocket );
          if( ( s.Info.Events & Socket.POLLOUT ) != 0 )
            checkWrite.Add( s.realSocket );
          if( ( s.Info.Events & ( Socket.POLLERR | Socket.POLLHUP | Socket.POLLNVAL ) ) != 0 )
            checkError.Add( s.realSocket );
        }
      }

      if( lsocks.Count == 0 || ( checkRead.Count == 0 && checkWrite.Count == 0 && checkError.Count == 0 ) )
        return;

      try
      {
        System.Net.Sockets.Socket.Select( checkRead, checkWrite, checkError, SELECT_TIMEOUT );
      }
      catch
      {
        return;
      }

      int nEvents = checkRead.Count + checkWrite.Count + checkError.Count;
      if( nEvents == 0 )
        return;

      // Process events
      foreach( var record in lsocks )
      {
        int newMask = 0;
        if( checkRead.Contains( record.realSocket ) )
          newMask |= Socket.POLLIN;
        if( checkWrite.Contains( record.realSocket ) )
          newMask |= Socket.POLLOUT;
        if( checkError.Contains( record.realSocket ) )
          newMask |= Socket.POLLERR;
        record._poll( newMask );
      }
    }
  }
}
