using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Fantasy
{
    public class ToggleCodeBinding : IBindingCodeGenerator
    {
        public bool ValidType(Type type) => type == typeof(Toggle);

        public List<(Type, string)> FunctionParams => new() { (typeof(bool), "value") };
        public int Priority => 2;
        public string CodeXmlSurround => "~~~toggle onValueChanged~~~";

        public string GetFunctionName(string fieldName)
        {
            var name = char.ToUpper(fieldName[0]) + fieldName[1..];
            return $"Toggle{name}";
        }

        public List<string> GetFunctionCode(string fieldName) => new()
        {
            $"public void {GetFunctionName(fieldName)}(bool value)",
            "{",
            "}"
        };

        public List<string> GetBindingCode(string targetField, string functionName) =>
            new()
            {
                $"{targetField}.onValueChanged.RemoveAllListeners();",
                $"{targetField}.onValueChanged.AddListener(self.{functionName});",
            };

        public List<string> GetUnBindingCode(string targetField, string functionName) =>
            new()
            {
                $"{targetField}.onValueChanged.RemoveAllListeners();",
            };
    }
}