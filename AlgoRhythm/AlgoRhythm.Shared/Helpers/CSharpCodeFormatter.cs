namespace AlgoRhythm.Shared.Helpers
{
    /// <summary>
    /// Class that format the user's C# code 
    /// </summary>
    public class CSharpCodeFormatter 
    {
        public string CodeTemplate =>
             @"using System;
               using System.Linq;
               using System.Collections.Generic;
               using System.Collections; 
               using System.Numerics;
               using System.Text;
               using System.Threading.Tasks;              

               {0};";
        
        public string Format(string code)
        {
            return string.Format(CodeTemplate, code);
        }

    }
}
