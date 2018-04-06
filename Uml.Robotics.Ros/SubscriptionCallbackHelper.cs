using System;


namespace Uml.Robotics.Ros
{
  public class SubscriptionCallbackHelper<M>
      : ISubscriptionCallbackHelper where M : RosMessage, new()
  {
    public SubscriptionCallbackHelper( string t, CallbackDelegate<M> cb )
        : this( Ros.Callback.Create( cb ) )
    {
      type = t;
    }

    public SubscriptionCallbackHelper( string t )
    {
      type = t;
    }

    public SubscriptionCallbackHelper( CallbackInterface q )
        : base( q )
    {
    }
  }

  public class ISubscriptionCallbackHelper
  {
    public CallbackInterface Callback { protected set; get; }

    public string type;

    protected ISubscriptionCallbackHelper()
    {
    }

    public ISubscriptionCallbackHelper( string type )
    {
      this.type = type;
    }

    protected ISubscriptionCallbackHelper( CallbackInterface Callback )
    {
      this.Callback = Callback;
    }


    public virtual void call( RosMessage msg )
    {
      Callback.SendEvent( msg );
    }
  }
}