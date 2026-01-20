using AlgoRhythm.Shared.Models.CodeExecution;
using Newtonsoft.Json;
using System.Reflection;

namespace CodeExecutor.Helpers
{
    /// <summary>
    /// Provides helper methods for converting function parameters 
    /// into typed argument arrays.
    /// </summary>
    public static class ArgumentConverter
    {
        public static object?[] ConvertArgs(this List<FunctionParameter>? parameters, ParameterInfo[] parameterInfos)
        {
            if (parameterInfos.Length != (parameters?.Count ?? 0)) throw new ArgumentException("Bad arguments, function can't be invoked");

            object?[] result = new object[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                result[i] = JsonConvert.DeserializeObject(parameters![i].Value, parameterInfos[i].ParameterType);
            }

            return result;
        }
    }
}
