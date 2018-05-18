using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uml.Robotics.Ros
{
  public class TypeRegistryBase
  {
    public Dictionary<string, Type> TypeRegistry { get; } = new Dictionary<string, Type>();
    public List<string> PackageNames { get; } = new List<string>();

    protected TypeRegistryBase()
    {
    }

    public IEnumerable<string> GetTypeNames()
    {
      return TypeRegistry.Keys;
    }

    protected T Create<T>( string rosType ) where T : class, new()
    {
      T result = null;
      bool typeExist = TypeRegistry.TryGetValue( rosType, out Type type );
      if( typeExist )
      {
        //result = Activator.CreateInstance( type ) as T;
        result = type.GetInstance() as T;
      }

      return result;
    }
  }
}
