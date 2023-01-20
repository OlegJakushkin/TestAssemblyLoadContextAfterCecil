// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Mono.Cecil.Cil;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace Nethermind.Test.Utils;

public static class InsertionFactory
{
    //A cache to store modified assemblies latest paths
    public static Dictionary<string, string> AssemblyNameToPath = new();


    // Clears all known modification references
    public static void Clear()
    {
        AssemblyNameToPath = new();
    }
    public class CollectibleAssemblyLoadContext
        : AssemblyLoadContext
    {
        public CollectibleAssemblyLoadContext() : base(isCollectible: true)
        {
            Default.Resolving += (context, name) =>
            {
                return Load(name);
            };
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string path = "";
            if (AssemblyNameToPath.TryGetValue(assemblyName.FullName, out path))
            {
                return Assembly.LoadFile(path);
            }
            return null;
        }
    }



    // A getter for any type instance from modified Assemblies
    // Note - returns dynamic to get around System.Runtime.Loader.IndividualAssemblyLoadContext errors form (T) casts  
    public static object GetRaw<T>() where T : class
    {
        AssemblyLoadContext context = new CollectibleAssemblyLoadContext();
        var type = typeof(T);
        context.EnterContextualReflection();
        Assembly assembly = context.LoadFromAssemblyName(type.Assembly.GetName());

        Type programType = assembly.GetType(type.FullName);
        object result = Activator.CreateInstance(programType);

        return result;
    }

    public static void ModifyTypeConstructor<TtoInjectInto>(Type typeToInject, string methodToCallName)
    {
        //GetRaw arg real T type data
        Type typeToInjectInto = typeof(TtoInjectInto);
        ModifyTypeConstructor(typeToInjectInto, typeToInject, methodToCallName);
    }

    // Modify the constructor of a type by injecting a call to a void method, specified by its name,
    // from an external type.
    // Note: A type can be modified multiple times.
    // Note: If the method to call modifies a static variable, avoid placing a breakpoint on the line
    // where the static variable is copied to a temporary variable. Instead, place the breakpoint one
    // line below, as this will allow you to see the expected value as in release mode.
    // Note: Adding a reference to a given type may cause problems with looped references.
    // Example of usage:
    // ```
    // InsertionFactory.ModifyTypeConstructor<TestToBeModified>(typeof(TestInjection), 
    //      nameof(TestInjection.AddCounter)); // Modifies the constructor of TestToBeModified
    // var cntAfter0 = TestInjection.Counter; // Outputs 0
    // var test1 = InsertionFactory.GetRaw<TestToBeModified>(); // Obtains a new instance of TestToBeModified
    // var cntAfter1 = TestInjection.Counter; // Outputs 1
    // ```
    public static void ModifyTypeConstructor(Type typeToInjectInto, Type typeToInject, string methodToCallName)
    {

        // GetRaw the assembly path of the assembly to modify
        var assemblyPath = GetAssemblyPath(typeToInjectInto);

        // Read the assembly
        var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

        // Add the assembly reference
        assembly.MainModule.ImportReference(typeToInject);

        // Find the type to modify
        var targetType = assembly.MainModule.GetType(typeToInjectInto.FullName);
        if (targetType == null)
        {
            throw new ArgumentException($"Type {typeToInjectInto.FullName} not found in the assembly");
        }

        // Find the constructor of the type
        var targetConstructor = targetType.Methods.FirstOrDefault(x => x.IsConstructor);
        if (targetConstructor == null)
        {
            throw new ArgumentException($"Constructor for type {typeToInjectInto.FullName} not found");
        }

        // Import the method from the external type
        var methodToCall = assembly.MainModule.ImportReference(typeToInject.GetMethod(methodToCallName));

        // Insert the method call at the beginning of the constructor
        var ilProcessor = targetConstructor.Body.GetILProcessor();
        ilProcessor.InsertBefore(targetConstructor.Body.Instructions[0], ilProcessor.Create(OpCodes.Call, methodToCall));

        // Write the modified assembly to a new file
        var modifiedAssemblyPath = Path.Combine(Path.GetTempPath(), $"{typeToInjectInto.Name}_{Guid.NewGuid()}.dll");
        assembly.Write(modifiedAssemblyPath);

        //Add to cached list
        AssemblyNameToPath[assembly.Name.FullName] = modifiedAssemblyPath;
    }

    //GetRaw a patrh to Assembly from cache
    private static string GetAssemblyPath(Type typeToInjectInto)
    {
        return GetAssemblyPath(typeToInjectInto.Assembly);
    }

    private static string GetAssemblyPath(Assembly assembly)
    {
        string path = "";

        //GetRaw cached or default
        if (!AssemblyNameToPath.TryGetValue(assembly.GetName().FullName, out path))
        {
            path = assembly.Location;
        }
        return path;
    }
}
