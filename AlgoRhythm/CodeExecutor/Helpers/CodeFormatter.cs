using AlgoRhythm.Shared.Models.CodeExecution;
using CodeExecutor.Interfaces;
using Microsoft.CodeAnalysis;
using System.Reflection;


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

}
