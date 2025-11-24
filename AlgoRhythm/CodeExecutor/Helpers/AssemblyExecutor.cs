using AlgoRhythm.Shared.Models.CodeExecution;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using System.Reflection;

namespace CodeExecutor.Helpers
{
    /// <summary>
    /// Executes methods from a compiled C# assembly loaded in memory.
    /// Supports both static and instance methods.
    /// </summary>
    public class AssemblyExecutor
    {
        /// <summary>
        /// Loads an assembly from a <see cref="MemoryStream"/> and invokes a specified method
        /// on a class with the given name, passing in the provided arguments.
        /// </summary>
        /// <param name="assemblyStream">The <see cref="MemoryStream"/> containing the compiled assembly.</param>
        /// <param name="className">The fully qualified name of the class containing the method to invoke.</param>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="args">The arguments to pass to the method. Can be empty or null for parameterless methods.</param>
        /// <returns>
        /// If result returned by the invoked method is the same as expected value (<c>null</c> if the method is <c>void</c>) + the value returned by method
        /// </returns>
        /// <exception cref="Exception">Thrown if the specified class or method cannot be found.</exception>
        public (bool? passed, object? returnedValue) Execute(MemoryStream assemblyStream, string className, string methodName, List<FunctionParameter>? args, string expectedValue)
        {
            Assembly assembly = Assembly.Load(assemblyStream.ToArray());
            Type? type = assembly.GetType(className);
            if (type == null) throw new Exception($"Class {className} not found.");

            // Needed for static classes
            bool isStatic = type.IsAbstract && type.IsSealed;
            object? instance = isStatic ? null : Activator.CreateInstance(type);

            MethodInfo? method = type.GetMethod(methodName);

            if (method == null)
                throw new Exception($"Method {methodName} not found in {className}.");

            ParameterInfo[] parametersInfo = method.GetParameters();
            object?[] parameters = args.ConvertArgs(parametersInfo);

            object? returnedValue = method.Invoke(instance, parameters);
            
            if (method.ReturnType == typeof(void))
            {
                return (true, returnedValue);
            }

            var expectedValueConverted = JsonConvert.DeserializeObject(expectedValue, method.ReturnType);

            return (Equals(expectedValueConverted, returnedValue), returnedValue);
        }
    }
}