
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Uml.Robotics.Ros
{
  public class ConnectionManager
  {
    private static Lazy<ConnectionManager> instance = new Lazy<ConnectionManager>( LazyThreadSafetyMode.ExecutionAndPublication );

    public static ConnectionManager Instance
    {
      get { return instance.Value; }
    }

    internal static void Terminate()
    {
      Instance.Shutdown();
    }

    internal static void Reset()
    {
      instance = new Lazy<ConnectionManager>( LazyThreadSafetyMode.ExecutionAndPublication );
    }
    
    private uint connection_id_counter;
    private object connection_id_counter_mutex = new object();
    private List<Connection> connections = new List<Connection>();
    private object connections_mutex = new object();
    private List<Connection> dropped_connections = new List<Connection>();
    private object dropped_connections_mutex = new object();
    private TcpListener listener;
    private WrappedTimer acceptor;

    public int TCPPort
    {
      get
      {
        if( listener == null || listener.LocalEndpoint == null )
          return -1;

        return ( (IPEndPoint)listener.LocalEndpoint ).Port;
      }
    }

    public uint GetNewConnectionId()
    {
      lock( connection_id_counter_mutex )
      {
        return connection_id_counter++;
      }
    }

    public void AddConnection( Connection connection )
    {
      lock( connections_mutex )
      {
        connections.Add( connection );
        connection.DroppedEvent += OnConnectionDropped;
      }
    }

    public void Clear( Connection.DropReason reason )
    {
      RemoveDroppedConnections();

      Connection[] localConnections = null;
      lock( connections_mutex )
      {
        localConnections = connections.ToArray();
        connections.Clear();
      }

      foreach( Connection c in localConnections )
      {
        if( !c.dropped )
          c.drop( reason );
      }

      lock( dropped_connections_mutex )
      {
        dropped_connections.Clear();
      }
    }

    public void Shutdown()
    {
      acceptor.Stop();

      if( listener != null )
      {
        listener.Stop();
        listener = null;
      }

      PollManager.Instance.RemovePollThreadListener( RemoveDroppedConnections );

      Clear( Connection.DropReason.Destructing );
    }

    public void TcpRosAcceptConnection( TcpTransport transport )
    {
      Connection conn = new Connection();
      AddConnection( conn );
      conn.initialize( transport, true, OnConnectionHeaderReceived );
    }


    public bool OnConnectionHeaderReceived( Connection conn, Header header )
    {
      bool ret = false;
      if( header.Values.ContainsKey( "topic" ) )
      {
        TransportSubscriberLink sub_link = new TransportSubscriberLink();
        ret = sub_link.Initialize( conn );
        ret &= sub_link.HandleHeader( header );
      }
      else if( header.Values.ContainsKey( "service" ) )
      {
        IServiceClientLink iscl = new IServiceClientLink();
        ret = iscl.initialize( conn );
        ret &= iscl.handleHeader( header );
      }
      else
      {
        ROS.Warn()( "Got a connection for a type other than topic or service from [" + conn.RemoteString +
                      "]." );
        return false;
      }
      //ROS.Debug()("CONNECTED [" + val + "]. WIN.");
      return ret;
    }

    public void CheckAndAccept( object nothing )
    {
      while( listener != null && listener.Pending() )
      {
        TcpRosAcceptConnection( new TcpTransport( listener.AcceptSocketAsync().Result, PollManager.Instance.poll_set ) );
      }
    }

    public void Start()
    {
      PollManager.Instance.AddPollThreadListener( RemoveDroppedConnections );

      listener = new TcpListener( IPAddress.Any, Network.TcpRosServerPort );
      listener.Start( 16 );
      acceptor = ROS.timerManager.StartTimer( CheckAndAccept, 100, 100 );
    }

    private void OnConnectionDropped( Connection conn, Connection.DropReason r )
    {
      lock( dropped_connections_mutex )
      {
        dropped_connections.Add( conn );
      }
    }

    private void RemoveDroppedConnections()
    {
      Connection[] localDroppedConnections = null;
      lock( dropped_connections_mutex )
      {
        localDroppedConnections = dropped_connections.ToArray();
        dropped_connections.Clear();
      }

      lock( connections_mutex )
      {
        foreach( Connection c in localDroppedConnections )
        {
          ROS.Debug()( "Removing dropped connection: " + c.CallerID );
          connections.Remove( c );
        }
      }
    }
  }
}
