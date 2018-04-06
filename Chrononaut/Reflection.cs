using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Chrononaut
{
    class Reflection
    {
        // Use reflection to get a method of a potentially hidden class
        static object RunMethod(string assemblyName, string className, object instance, string methodName, object[] parameters)
        {
            // Start by getting the assembly.
            Assembly assembly = Assembly.LoadFile(assemblyName);

            // Get the assembly from the already loaded list instead of reading it from file
            /*
             * Not possible since assembly-csharp is not in the LoadedAssemblyList
            Assembly assembly = null;
            foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                Debug.Log("ass: " + loadedAssembly.name);
                if (loadedAssembly.name == assemblyName || loadedAssembly.name == "KSP")
                    assembly = loadedAssembly.assembly;
            }
            */

            if (assembly == null)
                return null;

            Debug.LogError("className: " + className);
            Type type = assembly.GetType(className);
            Debug.LogError("type: " + type);

            Debug.LogError("methodName: " + methodName);
            MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.LogError("methodInfo: " + methodInfo);

            // Run the method
            return methodInfo.Invoke(instance, parameters);
        }

        static object GetField(string assemblyName, string className, object instance, string fieldName)
        {
            // Start by getting the assembly.
            Debug.Log("assemblyName: " + assemblyName);
            Assembly assembly = Assembly.LoadFile(assemblyName);
            Debug.Log("assembly: " + assembly);
            Debug.Log("className: " + className);
            Type type = assembly.GetType(className);
            Debug.Log("type: " + type);
            Debug.Log("fieldName: " + fieldName);
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Log("fieldInfo: " + fieldInfo.Name);

            FieldInfo[] fieldInfos = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            Debug.Log("fieldInfos: " + fieldInfos.Count());
            foreach (FieldInfo fi in fieldInfos)
                Debug.Log("fi: " + fi.Name + " - " + (fi.GetValue(instance) == null ? "null" : fi.GetValue(instance)));

            // Run the method
            return fieldInfo.GetValue(instance);
        }
    }
}
