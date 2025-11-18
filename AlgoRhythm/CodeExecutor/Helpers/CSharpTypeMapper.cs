namespace CodeExecutor.Helpers
{
    /// <summary>
    /// Provides mapping between C# type names (as strings) and <see cref="System.Type"/> instances,
    /// and converts <see cref="System.Type"/> back to C# type syntax.
    /// Supports primitive types, arrays, nullable types, and generic types.
    /// </summary>
    public static class CSharpTypeMapper
    {
        /// <summary>
        /// Converts a C# type name string to a <see cref="System.Type"/> instance.
        /// Returns <see cref="object"/> if the type name is unknown.
        /// </summary>
        /// <param name="typeName">The C# type name (e.g., "int", "string").</param>
        /// <returns>The corresponding <see cref="System.Type"/> instance.</returns>
        public static Type StringToType(string typeName)
        {
            return typeName switch
            {
                "int" => typeof(int),
                "string" => typeof(string),
                "double" => typeof(double),
                "float" => typeof(float),
                "bool" => typeof(bool),
                "long" => typeof(long),
                "short" => typeof(short),
                "decimal" => typeof(decimal),
                _ => Type.GetType(typeName) ?? typeof(object)
            };
        }

        private static readonly Dictionary<Type, string> PrimitiveMap = new()
        {
            { typeof(void), "void" },
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(char), "char" },
            { typeof(string), "string" },
            { typeof(object), "object" }
        };

        /// <summary>
        /// Converts a <see cref="System.Type"/> instance to a C# type syntax string.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> to convert.</param>
        /// <returns>A string representing the type in C# syntax.</returns>
        public static string ToCSharp(Type type)
        {
            // Primitive keyword?
            if (PrimitiveMap.TryGetValue(type, out string? keyword))
                return keyword;

            // Nullable<T> ?
            if (Nullable.GetUnderlyingType(type) is Type underlying)
                return $"{ToCSharp(underlying)}?";

            // Array?
            if (type.IsArray)
            {
                string element = ToCSharp(type.GetElementType()!);
                string ranks = new string(',', type.GetArrayRank() - 1);
                return $"{element}[{ranks}]";
            }

            // Generic type?
            if (type.IsGenericType)
            {
                string name = type.Name[..type.Name.IndexOf('`')];
                var args = type.GetGenericArguments().Select(ToCSharp);
                return $"{name}<{string.Join(", ", args)}>";
            }

            // Fallback: use the clean name
            return type.Name;
        }
    }

}
