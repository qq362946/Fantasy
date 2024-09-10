using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SRDebugger.Editor
{
    public partial class SRDebugEditor
    {
        /// <summary>
        /// Sets compiler define <paramref name="define"/> to be enabled/disabled on all build targets.
        /// </summary>
        static void SetCompileDefine(string define, bool enabled)
        {
            foreach (BuildTargetGroup targetGroup in GetAllBuildTargetGroups())
            {
                // Use hash set to remove duplicates.
                List<string> defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';').ToList();

                bool alreadyExists = false;

                for (var i = 0; i < defines.Count; i++)
                {
                    if (string.Equals(define, defines[i], StringComparison.InvariantCultureIgnoreCase))
                    {
                        alreadyExists = true;
                        if (!enabled)
                        {
                            defines.RemoveAt(i);
                        }
                    }
                }

                if (!alreadyExists && enabled)
                {
                    defines.Add(define);
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", defines.ToArray()));
            }
        }
        static void ForceRecompile()
        {
            AssetDatabase.ImportAsset(SRInternalEditorUtil.GetAssetPath("StompyRobot.SRDebugger.asmdef"), ImportAssetOptions.ForceUpdate);
        }

        static IEnumerable<BuildTargetGroup> GetAllBuildTargetGroups()
        {
            Type enumType = typeof(BuildTargetGroup);
            string[] names = Enum.GetNames(enumType);
            Array values = Enum.GetValues(enumType);

            for (var i = 0; i < names.Length; i++)
            {
                string name = names[i];
                BuildTargetGroup value = (BuildTargetGroup)values.GetValue(i);

                if (value == BuildTargetGroup.Unknown) continue;

                MemberInfo[] member = enumType.GetMember(name);
                MemberInfo entry = member.FirstOrDefault(p => p.DeclaringType == enumType);

                if (entry == null)
                {
                    Debug.LogErrorFormat(
                        "[SRDebugger] Unhandled build target: {0}. SRDebugger disabled state may not be applied correctly to this platform.",
                        name);
                    continue;
                }

                if (entry.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length != 0)
                {
                    // obsolete, ignore.
                    continue;
                }

                yield return value;
            }
        }
    }
}