using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Uml.Robotics.Ros;


namespace Messages.std_msgs
{
  public class Header : RosMessage
  {
    public uint seq = new uint();
    public Time stamp = new Time();
    public string frame_id = "";

    public override string MD5Sum() { return "2176decaecbce78abc3b96ef049fabed"; }
    public override bool HasHeader() { return false; }
    public override bool IsMetaType() { return false; }
    public override string MessageDefinition() { return @"uint32 seq
time stamp
string frame_id"; }
    public override string MessageType { get { return "std_msgs/Header"; } }
    public override bool IsServiceComponent() { return false; }

    public Header()
    {
    }

    public Header( byte[] serializedMessage )
    {
      Deserialize( serializedMessage );
    }

    public Header( byte[] serializedMessage, ref int currentIndex )
    {
      Deserialize( serializedMessage, ref currentIndex );
    }

    public override void Deserialize( byte[] serializedMessage, ref int currentIndex )
    {
      int piecesize;
      IntPtr h;

      //seq
      piecesize = Marshal.SizeOf( typeof( uint ) );
      h = IntPtr.Zero;
      if( serializedMessage.Length - currentIndex != 0 )
      {
        h = Marshal.AllocHGlobal( piecesize );
        Marshal.Copy( serializedMessage, currentIndex, h, piecesize );
      }
      if( h == IntPtr.Zero )
        throw new Exception( "Memory allocation failed" );
      seq = (uint)Marshal.PtrToStructure( h, typeof( uint ) );
      Marshal.FreeHGlobal( h );
      currentIndex += piecesize;
      //stamp
      stamp = new Time( new TimeData(
              BitConverter.ToUInt32( serializedMessage, currentIndex ),
              BitConverter.ToUInt32( serializedMessage, currentIndex + Marshal.SizeOf( typeof( System.Int32 ) ) ) ) );
      currentIndex += 2 * Marshal.SizeOf( typeof( System.Int32 ) );
      //frame_id
      frame_id = "";
      piecesize = BitConverter.ToInt32( serializedMessage, currentIndex );
      currentIndex += 4;
      frame_id = Encoding.ASCII.GetString( serializedMessage, currentIndex, piecesize );
      currentIndex += piecesize;
    }

    public override byte[] Serialize( bool partofsomethingelse )
    {
      byte[] thischunk, scratch1, scratch2;
      List<byte[]> pieces = new List<byte[]>();
      GCHandle h;

      //seq
      scratch1 = new byte[Marshal.SizeOf( typeof( uint ) )];
      h = GCHandle.Alloc( scratch1, GCHandleType.Pinned );
      Marshal.StructureToPtr( seq, h.AddrOfPinnedObject(), false );
      h.Free();
      pieces.Add( scratch1 );
      //stamp
      pieces.Add( BitConverter.GetBytes( stamp.data.sec ) );
      pieces.Add( BitConverter.GetBytes( stamp.data.nsec ) );
      //frame_id
      if( frame_id == null )
        frame_id = "";
      scratch1 = Encoding.ASCII.GetBytes( (string)frame_id );
      thischunk = new byte[scratch1.Length + 4];
      scratch2 = BitConverter.GetBytes( scratch1.Length );
      Array.Copy( scratch1, 0, thischunk, 4, scratch1.Length );
      Array.Copy( scratch2, thischunk, 4 );
      pieces.Add( thischunk );
      // combine every array in pieces into one array and return it
      int __a_b__f = pieces.Sum( ( __a_b__c ) => __a_b__c.Length );
      int __a_b__e = 0;
      byte[] __a_b__d = new byte[__a_b__f];
      foreach( var __p__ in pieces )
      {
        Array.Copy( __p__, 0, __a_b__d, __a_b__e, __p__.Length );
        __a_b__e += __p__.Length;
      }
      return __a_b__d;
    }

    public override void Randomize()
    {
      Random rand = new Random();
      int strlength;
      byte[] strbuf;

      //seq
      seq = (uint)rand.Next();
      //stamp
      stamp = new Time( new TimeData(
              Convert.ToUInt32( rand.Next() ),
              Convert.ToUInt32( rand.Next() ) ) );
      //frame_id
      strlength = rand.Next( 100 ) + 1;
      strbuf = new byte[strlength];
      rand.NextBytes( strbuf );  //fill the whole buffer with random bytes
      for( int __x__ = 0; __x__ < strlength; __x__++ )
        if( strbuf[__x__] == 0 ) //replace null chars with non-null random ones
          strbuf[__x__] = (byte)( rand.Next( 254 ) + 1 );
      strbuf[strlength - 1] = 0; //null terminate
      frame_id = Encoding.ASCII.GetString( strbuf );
    }

    public override bool Equals( RosMessage ____other )
    {
      var other = ____other as Messages.std_msgs.Header;
      if( other == null )
        return false;

      bool ret = true;
      ret &= seq == other.seq;
      ret &= stamp.data.Equals( other.stamp.data );
      ret &= frame_id == other.frame_id;
      // for each SingleType st:
      //    ret &= {st.Name} == other.{st.Name};
      return ret;
    }
  }
}
