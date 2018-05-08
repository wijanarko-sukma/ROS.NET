using System;
using System.Collections.Generic;


namespace Uml.Robotics.Ros
{
  public class NodeHandle : IDisposable
  {
    private object gate = new object();
    private string Namespace = "";
    private string UnresolvedNamespace = "";
    private ICallbackQueue callbackQueue;
    private bool _ok = true;
    private NodeHandleBackingCollection collection = new NodeHandleBackingCollection();
    private int referenceCount;
    private bool initializedRos;
    private IDictionary<string, string> remappings = new Dictionary<string, string>();
    private IDictionary<string, string> unresolvedRemappings = new Dictionary<string, string>();

    /// <summary>
    ///     Creates a new node
    /// </summary>
    /// <param name="ns">Namespace of node</param>
    /// <param name="remappings">any remappings</param>
    public NodeHandle( string ns, IDictionary<string, string> remappings = null )
    {
      if( ns != "" && ns[0] == '~' )
        ns = Names.Resolve( ns );
      construct( ns, true );
      initRemappings( remappings );
    }

    /// <summary>
    ///     Create a new nodehandle that is a partial deep copy of another
    /// </summary>
    /// <param name="rhs">The nodehandle this new one aspires to be</param>
    public NodeHandle( NodeHandle rhs )
    {
      Callback = rhs.Callback;
      remappings = new Dictionary<string, string>( rhs.remappings );
      unresolvedRemappings = new Dictionary<string, string>( rhs.unresolvedRemappings );
      construct( rhs.Namespace, true );
      UnresolvedNamespace = rhs.UnresolvedNamespace;
    }

    /// <summary>
    ///     Creates a new child node
    /// </summary>
    /// <param name="parent">Parent node to attach</param>
    /// <param name="ns">Namespace of new node</param>
    public NodeHandle( NodeHandle parent, string ns )
    {
      Namespace = parent.Namespace;
      Callback = parent.Callback;
      remappings = new Dictionary<string, string>( parent.remappings );
      unresolvedRemappings = new Dictionary<string, string>( parent.unresolvedRemappings );
      construct( ns, false );
    }

    /// <summary>
    ///     Creates a new child node with remappings
    /// </summary>
    /// <param name="parent">Parent node to attach</param>
    /// <param name="ns">Namespace of new node</param>
    /// <param name="remappings">Remappings</param>
    public NodeHandle( NodeHandle parent, string ns, IDictionary<string, string> remappings )
    {
      Namespace = parent.Namespace;
      Callback = parent.Callback;
      this.remappings = new Dictionary<string, string>( remappings );
      construct( ns, false );
    }

    /// <summary>
    ///     Creates a new nodehandle using the default ROS callback queue
    /// </summary>
    public NodeHandle()
        : this( ThisNode.Namespace, null )
    {
    }

    /// <summary>
    ///     Creates a new nodehandle using the given callback queue
    /// </summary>
    public NodeHandle( ICallbackQueue callbackQueue )
        : this( ThisNode.Namespace, null )
    {
      Callback = callbackQueue;
    }

    /// <summary>
    ///     gets/sets this nodehandle's callbackqueue
    ///     get : if the private _callback is null it is set to ROS.GlobalCallbackQueue
    /// </summary>
    public ICallbackQueue Callback
    {
      get
      {
        if( callbackQueue == null )
        {
          callbackQueue = ROS.GlobalCallbackQueue;
        }

        return callbackQueue;
      }
      set { callbackQueue = value; }
    }

    /// <summary>
    ///     The conjunction of ROS.ok, and the ok-ness of this nodehandle
    /// </summary>
    public bool ok
    {
      get { return ROS.ok && _ok; }
      private set { _ok = value; }
    }

    /// <summary>
    ///     Unregister every subscriber and publisher in this node
    /// </summary>
    public void shutdown()
    {
      lock( gate )
      {
        _ok = false;

        foreach( ISubscriber sub in collection.Subscribers )
          sub.unsubscribe();
        foreach( IPublisher pub in collection.Publishers )
          pub.unadvertise();

        foreach( IServiceClient client in collection.Serviceclients )
          client.shutdown();
        foreach( ServiceServer srv in collection.Serviceservers )
          srv.shutdown();
        collection.ClearAll();
      }
      destruct();
    }

    /// <summary>
    ///     Creates a publisher
    /// </summary>
    /// <typeparam name="M">Type of topic</typeparam>
    /// <param name="topic">Name of topic</param>
    /// <param name="queueSize">How many messages to qeueue if asynchrinous</param>
    /// <returns>A publisher with the specified topic type, name and options</returns>
    public Publisher<M> advertise<M>( string topic, int queueSize ) where M : RosMessage, new()
    {
      return advertise<M>( topic, queueSize, false );
    }

    /// <summary>
    ///     Creates a publisher, specify latching
    /// </summary>
    /// <typeparam name="M">Type of topic</typeparam>
    /// <param name="topic">Name of topic</param>
    /// <param name="queueSize">How many messages to enqueue if asynchrinous</param>
    /// <param name="latch">Boolean determines whether the given publisher will latch or not</param>
    /// <returns>A publisher with the specified topic type, name and options</returns>
    public Publisher<M> advertise<M>( string topic, int queueSize, bool latch ) where M : RosMessage, new()
    {
      return advertise( new AdvertiseOptions<M>( topic, queueSize ) { latch = latch } );
    }

    /// <summary>
    ///     Creates a publisher with connect and disconnect callbacks
    /// </summary>
    /// <typeparam name="M">Type of topic</typeparam>
    /// <param name="topic">Name of topic</param>
    /// <param name="queueSize">How many messages to enqueue if asynchrinous</param>
    /// <param name="connectCallback">Callback to fire when this node connects</param>
    /// <param name="disconnectCallback">Callback to fire when this node disconnects</param>
    /// <returns>A publisher with the specified topic type, name and options</returns>
    public Publisher<M> advertise<M>( string topic, int queueSize, SubscriberStatusCallback connectCallback,
        SubscriberStatusCallback disconnectCallback )
        where M : RosMessage, new()
    {
      return advertise<M>( topic, queueSize, connectCallback, disconnectCallback, false );
    }

    /// <summary>
    ///     Creates a publisher with connect and disconnect callbacks, specify latching.
    /// </summary>
    /// <typeparam name="M">Type of topic</typeparam>
    /// <param name="topic">Name of topic</param>
    /// <param name="queueSize">How many messages to enqueue if asynchrinous</param>
    /// <param name="connectCallback">Callback to fire when this node connects</param>
    /// <param name="disconnectCallback">Callback to fire when this node disconnects</param>
    /// <param name="latch">Boolean determines whether the given publisher will latch or not</param>
    /// <returns>A publisher with the specified topic type, name and options</returns>
    public Publisher<M> advertise<M>( string topic, int queueSize, SubscriberStatusCallback connectCallback,
        SubscriberStatusCallback disconnectCallback, bool latch )
        where M : RosMessage, new()
    {
      return advertise( new AdvertiseOptions<M>( topic, queueSize, connectCallback, disconnectCallback ) { latch = latch } );
    }

    /// <summary>
    ///     Creates a publisher with the given advertise options
    /// </summary>
    /// <typeparam name="M">Type of topic</typeparam>
    /// <param name="ops">Advertise options</param>
    /// <returns>A publisher with the specified options</returns>
    public Publisher<M> advertise<M>( AdvertiseOptions<M> ops ) where M : RosMessage, new()
    {
      ops.topic = resolveName( ops.topic );
      if( ops.callbackQueue == null )
      {
        ops.callbackQueue = Callback;
      }
      var callbacks = new SubscriberCallbacks( ops.connectCB, ops.disconnectCB, ops.callbackQueue );
      if( TopicManager.Instance.advertise( ops, callbacks ) )
      {
        var pub = new Publisher<M>( ops.topic, ops.md5Sum, ops.dataType, this, callbacks );
        lock( gate )
        {
          collection.Publishers.Add( pub );
        }
        return pub;
      }
      ROS.Error()( $"[{ThisNode.Name}] Advertisement of publisher has failed" );
      return null;
    }


    /// <summary>
    ///     Creates a subscriber with the given topic name.
    /// </summary>
    /// <typeparam name="M">Type of the subscriber message</typeparam>
    /// <param name="topic">Topic name</param>
    /// <param name="queueSize">How many messages to qeueue</param>
    /// <param name="cb">Callback to fire when a message is receieved</param>
    /// <param name="allowConcurrentCallbacks">Probably breaks things when true</param>
    /// <returns>A subscriber</returns>
    public Subscriber subscribe<M>( string topic, int queueSize, CallbackDelegate<M> cb, bool allowConcurrentCallbacks = false ) where M : RosMessage, new()
    {
      return subscribe<M>( topic, queueSize, Ros.Callback.Create( cb ), allowConcurrentCallbacks );
    }

    /// <summary>
    ///     Creates a subscriber
    /// </summary>
    /// <typeparam name="M">Topic type</typeparam>
    /// <param name="topic">Topic name</param>
    /// <param name="queueSize">How many messages to qeueue</param>
    /// <param name="cb">Function to fire when a message is recieved</param>
    /// <param name="allowConcurrentCallbacks">Probably breaks things when true</param>
    /// <returns>A subscriber</returns>
    public Subscriber subscribe<M>( string topic, int queueSize, CallbackInterface cb, bool allowConcurrentCallbacks )
        where M : RosMessage, new()
    {
      if( callbackQueue == null )
      {
        callbackQueue = ROS.GlobalCallbackQueue;
      }

      var ops = new SubscribeOptions<M>( topic, queueSize, cb.SendEvent )
      {
        callback_queue = callbackQueue,
        allow_concurrent_callbacks = allowConcurrentCallbacks
      };
      ops.callback_queue.AddCallback( cb );
      return subscribe( ops );
    }

    public Subscriber Subscribe( string topic, string messageType, int queueSize, CallbackDelegate<RosMessage> cb, bool allowConcurrentCallbacks = false )
    {
      return Subscribe( topic, messageType, queueSize, Ros.Callback.Create( cb ), allowConcurrentCallbacks );
    }

    public Subscriber Subscribe( string topic, string messageType, int queueSize, CallbackInterface cb, bool allowConcurrentCallbacks = false )
    {
      if( callbackQueue == null )
      {
        callbackQueue = ROS.GlobalCallbackQueue;
      }

      var message = RosMessage.Generate( messageType );
      var ops = new SubscribeOptions( topic, message.MessageType, message.MD5Sum(), queueSize, new SubscriptionCallbackHelper<RosMessage>( message.MessageType, cb.SendEvent ) )
      {
        callback_queue = callbackQueue,
        allow_concurrent_callbacks = allowConcurrentCallbacks
      };
      ops.callback_queue.AddCallback( cb );
      return subscribe( ops );
    }

    /// <summary>
    ///     Creates a subscriber with given subscriber options
    /// </summary>
    /// <param name="ops">Subscriber options</param>
    /// <returns>A subscriber</returns>
    public Subscriber subscribe( SubscribeOptions ops )
    {
      ops.topic = resolveName( ops.topic );
      if( ops.callback_queue == null )
      {
        ops.callback_queue = Callback;
      }

      TopicManager.Instance.subscribe( ops );

      var sub = new Subscriber( ops.topic, this, ops.helper );
      lock( gate )
      {
        collection.Subscribers.Add( sub );
      }
      return sub;
    }

    /// <summary>
    ///     Advertises a named ServiceServer
    /// </summary>
    /// <typeparam name="MReq">Request sub-srv type</typeparam>
    /// <typeparam name="MRes">Response sub-srv type</typeparam>
    /// <param name="service">The name of the service to advertise</param>
    /// <param name="srv_func">The handler for the service</param>
    /// <returns>The ServiceServer that will call the ServiceFunction on behalf of ServiceClients</returns>
    public ServiceServer advertiseService<MReq, MRes>( string service, ServiceFunction<MReq, MRes> srv_func )
        where MReq : RosMessage, new()
        where MRes : RosMessage, new()
    {
      return advertiseService( new AdvertiseServiceOptions<MReq, MRes>( service, srv_func ) );
    }

    /// <summary>
    ///     Advertises a ServiceServer with specified OPTIONS
    /// </summary>
    /// <typeparam name="MReq">Request sub-srv type</typeparam>
    /// <typeparam name="MRes">Response sub-srv type</typeparam>
    /// <param name="ops">isn't it obvious?</param>
    /// <returns>The ServiceServer that will call the ServiceFunction on behalf of ServiceClients</returns>
    public ServiceServer advertiseService<MReq, MRes>( AdvertiseServiceOptions<MReq, MRes> ops )
        where MReq : RosMessage, new()
        where MRes : RosMessage, new()
    {
      ops.service = resolveName( ops.service );
      if( ops.callback_queue == null )
      {
        ops.callback_queue = Callback;
      }
      if( ServiceManager.Instance.AdvertiseService( ops ) )
      {
        ServiceServer srv = new ServiceServer( ops.service, this );
        lock( gate )
        {
          collection.Serviceservers.Add( srv );
        }
        return srv;
      }
      throw new InvalidOperationException( "Could not advertise service" );
    }

    public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>( string service_name )
        where MReq : RosMessage, new()
        where MRes : RosMessage, new()
    {
      return serviceClient<MReq, MRes>( new ServiceClientOptions( service_name, false, null ) );
    }

    public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>( string service_name, bool persistent )
        where MReq : RosMessage, new()
        where MRes : RosMessage, new()
    {
      return serviceClient<MReq, MRes>( new ServiceClientOptions( service_name, persistent, null ) );
    }

    public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>( string service_name, bool persistent,
        IDictionary<string, string> header_values )
        where MReq : RosMessage, new()
        where MRes : RosMessage, new()
    {
      return serviceClient<MReq, MRes>( new ServiceClientOptions( service_name, persistent, header_values ) );
    }

    public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>( ServiceClientOptions ops )
        where MReq : RosMessage, new()
        where MRes : RosMessage, new()
    {
      ops.service = resolveName( ops.service );
      ops.md5sum = new MReq().MD5Sum();
      return new ServiceClient<MReq, MRes>( ops.service, ops.persistent, ops.header_values, ops.md5sum );
    }

    public ServiceClient<MSrv> serviceClient<MSrv>( string service_name )
        where MSrv : RosService, new()
    {
      return serviceClient<MSrv>( new ServiceClientOptions( service_name, false, null ) );
    }

    public ServiceClient<MSrv> serviceClient<MSrv>( string service_name, bool persistent )
        where MSrv : RosService, new()
    {
      return serviceClient<MSrv>( new ServiceClientOptions( service_name, persistent, null ) );
    }

    public ServiceClient<MSrv> serviceClient<MSrv>( string service_name, bool persistent,
        IDictionary<string, string> header_values )
        where MSrv : RosService, new()
    {
      return serviceClient<MSrv>( new ServiceClientOptions( service_name, persistent, header_values ) );
    }

    public ServiceClient<MSrv> serviceClient<MSrv>( ServiceClientOptions ops )
        where MSrv : RosService, new()
    {
      ops.service = resolveName( ops.service );
      ops.md5sum = new MSrv().RequestMessage.MD5Sum();
      return new ServiceClient<MSrv>( ops.service, ops.persistent, ops.header_values, ops.md5sum );
    }

    private void construct( string ns, bool validate_name )
    {
      if( !ROS.initialized )
        throw new Exception( "You must call ROS.Init() before instantiating the first nodehandle" );

      collection = new NodeHandleBackingCollection();
      UnresolvedNamespace = ns;
      Namespace = validate_name ? resolveName( ns ) : resolveName( ns, true, true );

      ok = true;
      lock( gate )
      {
        if( referenceCount == 0 && !ROS.isStarted() )
        {
          initializedRos = true;
          ROS.Start();
        }
        ++referenceCount;
      }
    }

    private void destruct()
    {
      lock( gate )
      {
        --referenceCount;
      }
      callbackQueue = null;
      if( referenceCount == 0 && initializedRos )
        ROS.shutdown();
    }

    private void initRemappings( IDictionary<string, string> rms )
    {
      if( rms == null )
        return;

      foreach( string k in rms.Keys )
      {
        string left = k;
        string right = rms[k];
        if( left != "" && left[0] != '_' )
        {
          string resolved_left = resolveName( left, false );
          string resolved_right = resolveName( right, false );
          remappings[resolved_left] = resolved_right;
          unresolvedRemappings[left] = right;
        }
      }
    }

    private string remapName( string name )
    {
      string resolved = resolveName( name, false );
      if( resolved == null )
        resolved = "";
      else if( remappings.ContainsKey( resolved ) )
        return (string)remappings[resolved];
      return Names.Remap( resolved );
    }

    private string resolveName( string name )
    {
      return resolveName( name, true );
    }

    private string resolveName( string name, bool remap )
    {
      if( !Names.Validate( name, out string error ) )
        throw new InvalidNameException( error );
      return resolveName( name, remap, false );
    }

    private string resolveName( string name, bool remap, bool novalidate )
    {
      //ROS.Debug()( $"resolveName({name})" );
      if( name == "" )
        return Namespace;

      string final = name;
      if( final[0] == '~' )
        throw new InvalidNameException( "Node name must not start with a '~' (tilde) character." );
      else if( final[0] != '/' && Namespace != "" )
      {
        final = Names.Append( Namespace, final );
      }
      final = Names.Clean( final );
      if( remap )
      {
        final = remapName( final );
      }
      return Names.Resolve( final, false );
    }

    public void Dispose()
    {
      shutdown();
    }

    private class NodeHandleBackingCollection
    {
      public List<IPublisher> Publishers = new List<IPublisher>();
      public List<IServiceClient> Serviceclients = new List<IServiceClient>();
      public List<ServiceServer> Serviceservers = new List<ServiceServer>();
      public List<ISubscriber> Subscribers = new List<ISubscriber>();

      public void ClearAll()
      {
        Publishers.Clear();
        Subscribers.Clear();
        Serviceservers.Clear();
        Serviceclients.Clear();
      }
    }
  }
}
