using AlgoRhythm.Shared.Models.CodeExecution;
using CodeExecutor.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Text.Json;

namespace CodeExecutor.Helpers
{
    /// <summary>
    /// Class that format the user's C# code 
    /// </summary>
    public class CSharpCodeFormatter : ICodeFormatter
    {
        public string CodeTemplate =>
             @"using System;
               using System.Linq;
               using System.Collections.Generic;
               using System.Collections; 
               using System.Numerics;
               using System.Text;

               {0};";
        
        public string Format(string code)
        {
            return string.Format(CodeTemplate, code);
        }

    }

    public static class ArgumentConverter
    {
        public static object?[] ConvertArgs(this List<FunctionParameter>? parameters, Dictionary<string, ITypeSymbol> parsedParams)
        {
            return parameters?
                .Where(p => p is not null)
                .Select(p =>
            {
                ITypeSymbol paramTypeSymbol = parsedParams[p.Name];

                string typeName = paramTypeSymbol.ToString() ?? string.Empty;
                Type? type = CSharpTypeMapper.StringToType(typeName);

                return Convert.ChangeType(p.Value,  type);
                
                
            }).ToArray() ?? [];
        }
    }
}
