using System.Reflection;
using System.Runtime.Loader;

namespace Nethermind.Test.Utils;

public class AssemblyLoadContextThatUsesInsertionFactoryLibs
    : AssemblyLoadContext
{
    public AssemblyLoadContextThatUsesInsertionFactoryLibs() : base(isCollectible: true)
    { }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        string path = "";
        if (InsertionFactory.AssemblyNameToPath.TryGetValue(assemblyName.FullName, out path))
        {
            return Assembly.LoadFile(path);
        }
        return null;
    }
}