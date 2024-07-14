using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Fantasy
{
    public static class CodeGenerator
    {
        private static string[] UsingCode => new[] { "using UnityEngine;", "using UnityEngine.UI;", "using Fantasy;", "" };

        private static List<IBindingCodeGenerator> GetBindingGenerator()
        {
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var targetAssembly = Assembly.GetAssembly(typeof(CodeGenerator));
            var assemblies = allAssemblies.Where(assembly => assembly.GetReferencedAssemblies().Any(refAssembly => refAssembly.FullName == targetAssembly.FullName)).ToList();
            assemblies.Add(targetAssembly);
            List<IBindingCodeGenerator> bindingCodeGenerators = new();
            foreach (var types in assemblies.Select(assembly => assembly.GetTypes().Where(type => !type.IsInterface && type.GetInterfaces().Contains(typeof(IBindingCodeGenerator))).ToList()))
            {
                bindingCodeGenerators.AddRange(types.Select(Activator.CreateInstance).Cast<IBindingCodeGenerator>());
            }

            return bindingCodeGenerators;
        }

        public static bool GenCode(string namespaceName, string declarationName, List<(Type, string)> propList, string oldScriptPath)
        {
            // 获取并实例化所有继承IBindingCodeGenerator的对象实例
            var bindingCodeGenerators = GetBindingGenerator();
            bindingCodeGenerators.Sort((a, b) => a.Priority - b.Priority);

            // 获取旧脚本数据
            List<string> oldCode = new();
            if (File.Exists(oldScriptPath))
            {
                oldCode = File.ReadAllLines(oldScriptPath).Select(s => s.Trim()).ToList();
            }

            bool isNewCode = oldCode.Count == 0;
            // 新脚本要加using和命名空间
            if (isNewCode)
            {
                oldCode.AddRange(UsingCode);
                oldCode.Add($"namespace {namespaceName}");
                oldCode.Add("{");
            }

            // AwakeSystem
            bool existAwakeSystem = AwakeSystemCodeGenerator.CheckExistOldCode(declarationName, oldCode, out var awakeSystem);
            var oldAwakeSystemCode = existAwakeSystem ? oldCode.GetRange(awakeSystem[0] + 1, awakeSystem[1] - awakeSystem[0] - 1) : new List<string>();
            var oldAwakeBindingProps = AwakeSystemCodeGenerator.ParseOldCode(bindingCodeGenerators, oldAwakeSystemCode);
            var awakeContentCode = AwakeSystemCodeGenerator.GenBindingCode(bindingCodeGenerators, propList, oldAwakeBindingProps);
            if (existAwakeSystem && AwakeSystemCodeGenerator.ValidateOldCode(oldAwakeSystemCode, out var awakeIdx))
            {
                // 改造旧AwakeSystem代码
                oldAwakeSystemCode.RemoveRange(awakeIdx[0], awakeIdx[1] - awakeIdx[0] + 1);
                oldAwakeSystemCode.InsertRange(awakeIdx[0], awakeContentCode);
                // 替换旧AwakeSystem代码
                oldCode.RemoveRange(awakeSystem[0] + 1, awakeSystem[1] - awakeSystem[0] - 1);
                oldCode.InsertRange(awakeSystem[0] + 1, oldAwakeSystemCode);
            }
            else
            {
                var newAwakeSystemCode = AwakeSystemCodeGenerator.GenCode(declarationName, awakeContentCode);
                oldCode.AddRange(newAwakeSystemCode);
            }

            // DestroySystem
            bool existDestroySystem = DestroySystemCodeGenerator.CheckExistOldCode(declarationName, oldCode, out var destroySystem);
            var oldDestroySystemCode = existDestroySystem ? oldCode.GetRange(destroySystem[0] + 1, destroySystem[1] - destroySystem[0] - 1) : new List<string>();
            var oldDestroyBindingProps = DestroySystemCodeGenerator.ParseOldCode(bindingCodeGenerators, oldDestroySystemCode);
            var destroyContentCode = DestroySystemCodeGenerator.GenBindingCode(bindingCodeGenerators, propList, oldDestroyBindingProps);
            if (existDestroySystem && DestroySystemCodeGenerator.ValidateOldCode(oldDestroySystemCode, out var destroyIdx))
            {
                // 改造旧DestroySystem代码
                oldDestroySystemCode.RemoveRange(destroyIdx[0], destroyIdx[1] - destroyIdx[0] + 1);
                oldDestroySystemCode.InsertRange(destroyIdx[0], destroyContentCode);
                // 替换旧DestroySystem代码
                oldCode.RemoveRange(destroySystem[0] + 1, destroySystem[1] - destroySystem[0] - 1);
                oldCode.InsertRange(destroySystem[0] + 1, oldDestroySystemCode);
            }
            else
            {
                var newDestroySystemCode = DestroySystemCodeGenerator.GenCode(declarationName, destroyContentCode);
                oldCode.AddRange(newDestroySystemCode);
            }

            // PartialClass
            bool existPartialClass = PartialClassCodeGenerator.CheckExistOldCode(declarationName, oldCode, out var partialClass);
            var oldPartialClassCode = existPartialClass ? oldCode.GetRange(partialClass[0] + 1, partialClass[1] - partialClass[0] - 1) : new List<string>();
            var oldPartialClassGenProps = PartialClassCodeGenerator.ParseOldCode(bindingCodeGenerators, oldPartialClassCode);
            var partialClassContentCode = PartialClassCodeGenerator.GenBindingCode(bindingCodeGenerators, propList, oldPartialClassGenProps);
            if (existPartialClass && PartialClassCodeGenerator.ValidateOldCode(oldPartialClassCode, out var genIdx))
            {
                // 改造旧PartialClass代码
                oldPartialClassCode.RemoveRange(genIdx[0], genIdx[1] - genIdx[0] + 1);
                oldPartialClassCode.InsertRange(genIdx[0], partialClassContentCode);
                // 替换旧PartialClass代码
                oldCode.RemoveRange(partialClass[0] + 1, partialClass[1] - partialClass[0] - 1);
                oldCode.InsertRange(partialClass[0] + 1, oldPartialClassCode);
            }
            else
            {
                var newPartialClassCode = PartialClassCodeGenerator.GenCode(declarationName, partialClassContentCode);
                oldCode.AddRange(newPartialClassCode);
            }

            // 命名空间括号
            if (isNewCode)
                oldCode.Add("}");

            StringBuilder sb = new();
            oldCode.ForEach(line => sb.AppendLine(line));
            File.WriteAllText(oldScriptPath, sb.ToString());
            CSharpFormatProgramLauncher launcher = new(FantasyUISettingsScriptableObject.Instance.formatCodeTool);
            if (launcher.LaunchFormatFile(oldScriptPath))
                return true;
            UnityEngine.Debug.LogError(launcher.ErrorMsg);
            return false;
        }
    }
}