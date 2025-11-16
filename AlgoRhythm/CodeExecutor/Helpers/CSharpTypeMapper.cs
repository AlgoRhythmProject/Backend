namespace CodeExecutor.Helpers
{
    public static class CSharpTypeMapper
    {
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
