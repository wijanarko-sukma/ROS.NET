using System;
using System.Collections.Generic;
using System.Diagnostics;

using Uml.Robotics.XmlRpc;
using System.Threading.Tasks;
using System.Linq;

namespace Uml.Robotics.Ros
{
  public delegate void ParamDelegate( string key, XmlRpcValue value );
  public delegate void ParamStringDelegate( string key, string value );
  public delegate void ParamDoubleDelegate( string key, double value );
  public delegate void ParamIntDelegate( string key, int value );
  public delegate void ParamBoolDelegate( string key, bool value );

  public static class Param
  {
    static object gate = new object();
    static Dictionary<string, XmlRpcValue> cachedValues = new Dictionary<string, XmlRpcValue>();        // cache contains mapped keys
    static Dictionary<string, List<ParamDelegate>> subscriptions = new Dictionary<string, List<ParamDelegate>>();

    public static void Subscribe( string key, ParamBoolDelegate callback )
    {
      SubscribeInternal( key, ( name, value ) => callback( name, value.GetBool() ) );
    }

    public static void Subscribe( string key, ParamIntDelegate callback )
    {
      SubscribeInternal( key, ( name, value ) => callback( name, value.GetInt() ) );
    }

    public static void Subscribe( string key, ParamDoubleDelegate callback )
    {
      SubscribeInternal( key, ( name, value ) => callback( name, value.GetDouble() ) );
    }

    public static void Subscribe( string key, ParamStringDelegate callback )
    {
      SubscribeInternal( key, ( name, value ) => callback( name, value.GetString() ) );
    }

    public static void Subscribe( string key, ParamDelegate callback )
    {
      SubscribeInternal( key, callback );
    }

    private static void SubscribeInternal( string key, ParamDelegate callback )
    {
      string mappedKey = Names.Resolve( key );
      XmlRpcValue parm = new XmlRpcValue();
      parm.Set( 0, ThisNode.Name );
      parm.Set( 1, XmlRpcManager.Instance.Uri );
      parm.Set( 2, mappedKey );

      lock( gate )
      {
        var result = new XmlRpcValue();
        var payload = new XmlRpcValue();
        if( Master.execute( "subscribeParam", parm, result, payload, false ) )
        {
          if( !subscriptions.TryGetValue( key, out var list ) )
          {
            list = new List<ParamDelegate>();
            subscriptions.Add( key, list );
          }
          list.Add( callback );
        }
      }
      Update( key, GetParam( key, true ) );
    }

    /// <summary>
    ///     Sets the paramater on the parameter server
    /// </summary>
    /// <param name="mappedKey">Fully mapped name of the parameter to be set</param>
    /// <param name="val">Value of the paramter</param>
    private static void SetOnServer( string key, XmlRpcValue parm )
    {
      string mappedKey = Names.Resolve( key );
      parm.Set( 0, ThisNode.Name );
      parm.Set( 1, mappedKey );
      // parm.Set(2, ...), the value to be set on the parameter server was stored in parm by the calling function already )

      lock( gate )
      {
        var response = new XmlRpcValue();
        var payload = new XmlRpcValue();
        if( Master.execute( "setParam", parm, response, payload, true ) )
        {
          if( cachedValues.ContainsKey( mappedKey ) )
            cachedValues[mappedKey] = parm;
        }
        else
        {
          throw new RosException( "RPC call setParam for key " + mappedKey + " failed. " );
        }
      }
    }

    /// <summary>
    ///     Sets the paramater on the parameter server
    /// </summary>
    /// <param name="key">Name of the parameter</param>
    /// <param name="val">Value of the paramter</param>
    public static void Set( string key, XmlRpcValue val )
    {
      XmlRpcValue parm = new XmlRpcValue();
      parm.Set( 2, val );
      SetOnServer( key, parm );
    }

    /// <summary>
    ///     Sets the paramater on the parameter server
    /// </summary>
    /// <param name="key">Name of the parameter</param>
    /// <param name="val">Value of the paramter</param>
    public static void Set( string key, string val )
    {
      XmlRpcValue parm = new XmlRpcValue();
      parm.Set( 2, val );
      SetOnServer( key, parm );
    }

    /// <summary>
    ///     Sets the paramater on the parameter server
    /// </summary>
    /// <param name="key">Name of the parameter</param>
    /// <param name="val">Value of the paramter</param>
    public static void Set( string key, double val )
    {
      XmlRpcValue parm = new XmlRpcValue();
      parm.Set( 2, val );
      SetOnServer( key, parm );
    }

    /// <summary>
    ///     Sets the paramater on the parameter server
    /// </summary>
    /// <param name="key">Name of the parameter</param>
    /// <param name="val">Value of the paramter</param>
    public static void Set( string key, int val )
    {
      XmlRpcValue parm = new XmlRpcValue();
      parm.Set( 2, val );
      SetOnServer( key, parm );
    }

    /// <summary>
    ///     Sets the paramater on the parameter server
    /// </summary>
    /// <param name="key">Name of the parameter</param>
    /// <param name="val">Value of the paramter</param>
    public static void Set( string key, bool val )
    {
      XmlRpcValue parm = new XmlRpcValue();
      parm.Set( 2, val );
      SetOnServer( key, parm );
    }

    /// <summary>
    ///     Gets the parameter from the parameter server
    /// </summary>
    /// <param name="key">Name of the parameter</param>
    /// <returns></returns>
    internal static XmlRpcValue GetParam( string key, bool useCache = false )
    {
      string mappedKey = Names.Resolve( key );
      if( !GetImpl( mappedKey, out XmlRpcValue payload, useCache ) )
        payload = null;
      return payload;
    }

    private static bool SafeGet<T>( string key, out T dest, T def = default( T ) )
    {
      try
      {
        XmlRpcValue v = GetParam( key );
        if( v == null || !v.IsEmpty )
        {
          if( def == null )
          {
            dest = default( T );
            return false;
          }
          dest = def;
          return true;
        }

        if( typeof( T ) == typeof( double ) )
        {
          dest = (T)(object)v.GetDouble();
        }
        else if( typeof( T ) == typeof( string ) )
        {
          dest = (T)(object)v.GetString();
        }
        else if( typeof( T ) == typeof( bool ) )
        {
          dest = (T)(object)v.GetBool();
        }
        else if( typeof( T ) == typeof( int ) )
        {
          dest = (T)(object)v.GetInt();
        }
        else if( typeof( T ) == typeof( XmlRpcValue ) )
        {
          dest = (T)(object)v;
        }
        else
        {
          // unsupported type
          dest = default( T );
          return false;
        }

        return true;
      }
      catch
      {
        dest = default( T );
        return false;
      }
    }

    private static async Task<XmlRpcValue> GetParamTypeCheckedAsync( string key, XmlRpcType expectedType, bool useCache = false )
    {
      var result = new XmlRpcValue();
      if( !await GetParamAsync( key, result, useCache ) )
        throw new RosException( $"Getting ROS param '{key}' failed." );

      if( result.Type != expectedType )
        throw new XmlRpcException( $"{expectedType} response expected" );
      return result;
    }

    public static async Task<int> GetIntAsync( string key )
    {
      var rpcResult = await GetParamTypeCheckedAsync( key, XmlRpcType.Int );
      return rpcResult.GetInt();
    }

    public static async Task<bool> GetBoolAsync( string key )
    {
      var rpcResult = await GetParamTypeCheckedAsync( key, XmlRpcType.Boolean );
      return rpcResult.GetBool();
    }

    public static async Task<double> GetDoubleAsync( string key )
    {
      var rpcResult = await GetParamTypeCheckedAsync( key, XmlRpcType.Double );
      return rpcResult.GetDouble();
    }

    public static async Task<string> GetStringAsync( string key )
    {
      var rpcResult = await GetParamTypeCheckedAsync( key, XmlRpcType.String );
      return rpcResult.GetString();
    }

    public static async Task<DateTime> GetDateTimeAsync( string key )
    {
      var rpcResult = await GetParamTypeCheckedAsync( key, XmlRpcType.DateTime );
      return rpcResult.GetDateTime();
    }

    public static async Task<byte[]> GetBinaryAsync( string key )
    {
      var rpcResult = await GetParamTypeCheckedAsync( key, XmlRpcType.Base64 );
      return rpcResult.GetBinary();
    }

    private static XmlRpcValue GetParamTypeChecked( string key, XmlRpcType expectedType, bool useCache = false )
    {
      var result = GetParam( key, useCache );
      if( result == null )
        throw new RosException( $"Getting ROS param '{key}' failed." );

      if( result.Type != expectedType )
        throw new XmlRpcException( $"{expectedType} response expected" );
      return result;
    }

    public static int GetInt( string key )
    {
      var rpcResult = GetParamTypeChecked( key, XmlRpcType.Int );
      return rpcResult.GetInt();
    }

    public static bool GetBool( string key )
    {
      var rpcResult = GetParamTypeChecked( key, XmlRpcType.Boolean );
      return rpcResult.GetBool();
    }

    public static double GetDouble( string key )
    {
      var rpcResult = GetParamTypeChecked( key, XmlRpcType.Double );
      return rpcResult.GetDouble();
    }

    public static string GetString( string key )
    {
      var rpcResult = GetParamTypeChecked( key, XmlRpcType.String );
      return rpcResult.GetString();
    }

    public static DateTime GetDateTime( string key )
    {
      var rpcResult = GetParamTypeChecked( key, XmlRpcType.DateTime );
      return rpcResult.GetDateTime();
    }

    public static byte[] GetBinary( string key )
    {
      var rpcResult = GetParamTypeChecked( key, XmlRpcType.Base64 );
      return rpcResult.GetBinary();
    }

    public static bool Get( string key, out XmlRpcValue dest )
    {
      return SafeGet( key, out dest );
    }

    public static bool Get( string key, out bool dest )
    {
      return SafeGet( key, out dest );
    }

    public static bool Get( string key, out bool dest, bool def )
    {
      return SafeGet( key, out dest, def );
    }

    public static bool Get( string key, out int dest )
    {
      return SafeGet( key, out dest );
    }

    public static bool Get( string key, out int dest, int def )
    {
      return SafeGet( key, out dest, def );
    }

    public static bool Get( string key, out double dest )
    {
      return SafeGet( key, out dest );
    }

    public static bool Get( string key, out double dest, double def )
    {
      return SafeGet( key, out dest, def );
    }

    public static bool Get( string key, out string dest, string def = null )
    {
      return SafeGet( key, out dest, def );
    }

    public static List<string> List()
    {
      var ret = new List<string>();
      var parm = new XmlRpcValue();
      var result = new XmlRpcValue();
      var payload = new XmlRpcValue();
      parm.Set( 0, ThisNode.Name );
      if( !Master.execute( "getParamNames", parm, result, payload, false ) )
        return ret;
      if( result.Count != 3 || result[0].GetInt() != 1 || result[2].Type != XmlRpcType.Array )
      {
        ROS.Warn()( "Expected a return code, a description, and a list!" );
        return ret;
      }
      for( int i = 0; i < payload.Count; i++ )
      {
        ret.Add( payload[i].GetString() );
      }
      return ret;
    }

    /// <summary>
    ///     Checks if the paramter exists.
    /// </summary>
    /// <param name="key">Name of the paramerer</param>
    /// <returns></returns>
    public static bool Has( string key )
    {
      var parm = new XmlRpcValue();
      var result = new XmlRpcValue();
      var payload = new XmlRpcValue();
      parm.Set( 0, ThisNode.Name );
      parm.Set( 1, Names.Resolve( key ) );
      if( !Master.execute( "hasParam", parm, result, payload, false ) )
        return false;
      if( result.Count != 3 || result[0].GetInt() != 1 || result[2].Type != XmlRpcType.Boolean )
        return false;
      return result[2].GetBool();
    }

    /// <summary>
    ///     Deletes a parameter from the parameter server.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static bool Del( string key )
    {
      string mappedKey = Names.Resolve( key );

      XmlRpcValue parm = new XmlRpcValue(), result = new XmlRpcValue(), payload = new XmlRpcValue();
      parm.Set( 0, ThisNode.Name );
      parm.Set( 1, mappedKey );
      if( !Master.execute( "deleteParam", parm, result, payload, false ) )
        return false;

      lock( gate )
      {
        subscriptions.Remove( mappedKey );
        cachedValues.Remove( mappedKey );
      }

      return true;
    }

    public static void Init( IDictionary<string, string> remappingArgs )
    {
      foreach( string name in remappingArgs.Keys )
      {
        string param = remappingArgs[name];
        if( name.Length < 2 )
          continue;
        if( name[0] == '_' && name[1] != '_' )
        {
          string localName = "~" + name.Substring( 1 );
          bool success = int.TryParse( param, out int i );
          if( success )
          {
            Set( Names.Resolve( localName ), i );
            continue;
          }
          success = double.TryParse( param, out double d );
          if( success )
          {
            Set( Names.Resolve( localName ), d );
            continue;
          }
          success = bool.TryParse( param.ToLower(), out bool b );
          if( success )
          {
            Set( Names.Resolve( localName ), b );
            continue;
          }
          Set( Names.Resolve( localName ), param );
        }
      }
      XmlRpcManager.Instance.Bind( "paramUpdate", ParamUpdateCallback );
    }

    /// <summary>
    ///     Manually update the value of a parameter
    /// </summary>
    /// <param name="key">Name of parameter</param>
    /// <param name="value">Value to update param to</param>
    public static void Update( string key, XmlRpcValue value )
    {
      if( value == null )
        return;

      key = Names.Clean( key );

      List<ParamDelegate> callbacks = null;

      lock( gate )
      {
        if( !cachedValues.ContainsKey( key ) )
          cachedValues.Add( key, value );
        else
          cachedValues[key] = value;

        if( !subscriptions.TryGetValue( key, out callbacks ) )
          return;

        callbacks = new List<ParamDelegate>( callbacks );     // create isolation copy to execute callbacks outside lock
      }

      if( callbacks != null )
      {
        foreach( var cb in callbacks )
          cb( key, value );
      }
    }

    /// <summary>
    ///     Fired when a parameter gets updated
    /// </summary>
    /// <param name="parm">Name of parameter</param>
    /// <param name="result">New value of parameter</param>
    public static void ParamUpdateCallback( XmlRpcValue val, XmlRpcValue result )
    {
      val.Set( 0, 1 );
      val.Set( 1, "" );
      val.Set( 2, 0 );
      //update(XmlRpcValue.LookUp(parm)[1].Get<string>(), XmlRpcValue.LookUp(parm)[2]);
      /// TODO: check carefully this stuff. It looks strange
      Update( val[1].GetString(), val[2] );
    }

    private static async Task<bool> GetParamAsync( string key, XmlRpcValue resultValue, bool useCache )
    {
      string mappepKey = Names.Resolve( key );

      if( useCache )
      {
        lock( gate )
        {
          if( cachedValues.TryGetValue( mappepKey, out var cachedValue ) && !cachedValue.IsEmpty )
          {
            resultValue.Copy( cachedValue );
            return true;
          }
        }
      }

      var parm = new XmlRpcValue();
      var result = new XmlRpcValue();
      parm.Set( 0, ThisNode.Name );
      parm.Set( 1, mappepKey );
      resultValue.SetArray( 0 );

      bool ret = await Master.ExecuteAsync( "getParam", parm, result, resultValue, false );
      if( ret && useCache )
      {
        lock( gate )
        {
          cachedValues.Add( mappepKey, resultValue.Clone() );
        }
      }

      return ret;
    }

    public static bool GetImpl( string key, out XmlRpcValue value, bool useCache )
    {
      string mappepKey = Names.Resolve( key );
      value = new XmlRpcValue();

      if( useCache )
      {
        lock( gate )
        {
          if( cachedValues.TryGetValue( mappepKey, out var cachedValue ) && !cachedValue.IsEmpty )
          {
            value = cachedValue;
            return true;
          }
        }
      }

      XmlRpcValue parm2 = new XmlRpcValue(), result2 = new XmlRpcValue();
      parm2.Set( 0, ThisNode.Name );
      parm2.Set( 1, mappepKey );
      value.SetArray( 0 );

      bool ret = Master.execute( "getParam", parm2, result2, value, false );
      if( ret && useCache )
      {
        lock( gate )
        {
          cachedValues.Add( mappepKey, value );
        }
      }

      return ret;
    }

    internal static void Reset()
    {
      cachedValues.Clear();
      subscriptions.Clear();
    }

    internal static void Terminate()
    {
      XmlRpcManager.Instance.Unbind( "paramUpdate" );
    }
  }
}
