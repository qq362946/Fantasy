using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Fantasy
{
    public class ButtonCodeBinding : IBindingCodeGenerator
    {
        public bool ValidType(Type type) => typeof(Button) == type;

        public List<(Type, string)> FunctionParams => new();
        public int Priority => 0;

        public string CodeXmlSurround => "~~~button onClick~~~";


        public string GetFunctionName(string fieldName)
        {
            var name = char.ToUpper(fieldName[0]) + fieldName[1..];
            return $"Click{name}";
        }

        public List<string> GetFunctionCode(string fieldName) => new()
        {
            $"public void {GetFunctionName(fieldName)}()",
            "{",
            "}"
        };

        public List<string> GetBindingCode(string targetField, string functionName) =>
            new()
            {
                $"{targetField}.onClick.RemoveAllListeners();",
                $"{targetField}.onClick.AddListener(self.{functionName});",
            };

        public List<string> GetUnBindingCode(string targetField, string functionName) =>
            new()
            {
                $"{targetField}.onClick.RemoveAllListeners();"
            };
    }
}