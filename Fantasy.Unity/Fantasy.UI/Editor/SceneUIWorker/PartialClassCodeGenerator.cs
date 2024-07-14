using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fantasy
{
    public static class PartialClassCodeGenerator
    {
        private const string SurroundXml = "---UIEvent---";
        private const string OutSurroundXml = "---PartialClass---";

        public static bool CheckExistOldCode(string declarationName, List<string> oldCode, out int[] partialClassIdx)
        {
            partialClassIdx = new[] { -1, -1 };
            for (var i = 0; i < oldCode.Count; i++)
            {
                var oldScriptLine = oldCode[i];
                if (partialClassIdx[0] == -1 && Regex.IsMatch(oldScriptLine, @$"//\s*<{OutSurroundXml} class='{declarationName}'>"))
                    partialClassIdx[0] = i;
                if (partialClassIdx[0] != -1 && partialClassIdx[1] == -1 && Regex.IsMatch(oldScriptLine, @$"//\s*<{OutSurroundXml}/>"))
                    partialClassIdx[1] = i;
            }

            return partialClassIdx[0] > -1 && partialClassIdx[1] > partialClassIdx[0];
        }

        public static List<string> GenCode(string declarationName, List<string> contentCode)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            List<string> newCode = new();
            newCode.Add($"// <{OutSurroundXml} class='{declarationName}'>");
            newCode.Add($"public partial class {declarationName}");
            newCode.Add("{");
            newCode.AddRange(contentCode);
            newCode.Add("}");
            newCode.Add($"// <{OutSurroundXml}/>");
            return newCode;
        }

        public static List<string> GenBindingCode(List<IBindingCodeGenerator> bindingCodeGenerators, List<(Type, string)> propList,
            Dictionary<IBindingCodeGenerator, Dictionary<string, List<string>>> oldPropBindings)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            List<string> newCode = new();
            newCode.Add($"// <{SurroundXml}>");
            foreach (var bindingCodeGenerator in bindingCodeGenerators)
            {
                var fieldNameSet = propList.Where(tuple => bindingCodeGenerator.ValidType(tuple.Item1)).Select(tuple => tuple.Item2).ToHashSet();
                if (fieldNameSet.Count == 0)
                    continue;
                newCode.Add($"// <{bindingCodeGenerator.CodeXmlSurround}>");
                // 保留旧代码
                if (oldPropBindings.TryGetValue(bindingCodeGenerator, out var oldProps))
                {
                    foreach (var (oldFieldName, oldCode) in oldProps)
                    {
                        if (!fieldNameSet.Contains(oldFieldName))
                            continue;
                        fieldNameSet.Remove(oldFieldName);
                        newCode.Add($"// <{oldFieldName}>");
                        newCode.AddRange(oldCode);
                        newCode.Add($"// <{oldFieldName}/>");
                    }
                }

                // 新代码
                foreach (var fieldName in fieldNameSet)
                {
                    var code = bindingCodeGenerator.GetFunctionCode(fieldName);
                    if (code.Count == 0)
                        continue;
                    newCode.Add($"// <{fieldName}>");
                    newCode.AddRange(code);
                    newCode.Add($"// <{fieldName}/>");
                }

                newCode.Add($"// <{bindingCodeGenerator.CodeXmlSurround}/>");
            }

            newCode.Add($"// <{SurroundXml}/>");
            return newCode;
        }

        public static Dictionary<IBindingCodeGenerator, Dictionary<string, List<string>>> ParseOldCode(List<IBindingCodeGenerator> bindingCodeGenerators, List<string> oldCode)
        {
            Dictionary<IBindingCodeGenerator, Dictionary<string, List<string>>> oldPropBindings = new();
            if (!ValidateOldCode(oldCode, out var genIdx))
                return oldPropBindings;
            oldCode = oldCode.GetRange(genIdx[0] + 1, genIdx[1] - genIdx[0] - 1);
            foreach (var bindingCodeGenerator in bindingCodeGenerators)
            {
                if (ParseOldCode(bindingCodeGenerator, oldCode, out var oldProp))
                {
                    oldPropBindings.Add(bindingCodeGenerator, oldProp);
                }
            }

            return oldPropBindings;
        }

        public static bool ValidateOldCode(List<string> oldCode, out int[] genIdx)
        {
            genIdx = new[] { -1, -1 };

            if (oldCode.Count == 0)
                return false;

            for (int i = 0; i < oldCode.Count; i++)
            {
                var line = oldCode[i];
                if (genIdx[0] == -1 && Regex.IsMatch(line, @$"//\s*<{SurroundXml}>"))
                    genIdx[0] = i;
                if (genIdx[0] > -1 && genIdx[1] == -1 && Regex.IsMatch(line, @$"//\s*<{SurroundXml}/>"))
                    genIdx[1] = i;
            }

            return genIdx[0] != -1 && genIdx[1] > genIdx[0];
        }

        private static bool ParseOldCode(IBindingCodeGenerator bindingCodeGenerator, List<string> oldCode, [NotNullWhen(true)] out Dictionary<string, List<string>> oldProp)
        {
            oldProp = null;

            var surround = bindingCodeGenerator.CodeXmlSurround;
            int[] domainIdx = { -1, -1 };
            for (int i = 0; i < oldCode.Count; i++)
            {
                var line = oldCode[i];
                if (domainIdx[0] == -1 && Regex.IsMatch(line, @$"//\s*<{surround}>"))
                    domainIdx[0] = i;
                if (domainIdx[0] > -1 && domainIdx[1] == -1 && Regex.IsMatch(line, @$"//\s*<{surround}/>"))
                    domainIdx[1] = i;
            }

            if (domainIdx[0] == -1 || domainIdx[1] <= domainIdx[0])
                return false;
            var domain = oldCode.GetRange(domainIdx[0] + 1, domainIdx[1] - domainIdx[0] - 1);

            bool catching = false;
            string catchingFieldName = default;
            Regex openRegex = new Regex(@"//\s*<([\w]+)>");
            Regex closeRegex = new Regex(@"//\s*<([\w]+)/>");
            foreach (var line in domain)
            {
                if (!catching)
                {
                    var match = openRegex.Match(line);
                    if (!match.Success)
                        continue;
                    catching = true;
                    var group = match.Groups[1];
                    catchingFieldName = group.Value;
                }
                else
                {
                    var match = closeRegex.Match(line);
                    if (match.Success)
                    {
                        catching = false;
                    }
                    else
                    {
                        oldProp ??= new Dictionary<string, List<string>>();
                        oldProp.TryAdd(catchingFieldName, new List<string>());
                        oldProp[catchingFieldName].Add(line);
                    }
                }
            }

            return oldProp != null;
        }
    }
}