using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fantasy
{
    public class FantasySettingsProvider : SettingsProvider
    {
        private SerializedObject _serializedObject;
        private SerializedProperty _autoCopyAssembly;
        private SerializedProperty _hotUpdatePath;
        private SerializedProperty _hotUpdateAssemblyDefinitions;
        private SerializedProperty _linkAssemblyDefinitions;
        private SerializedProperty _includeAssembly;
        private bool _showLinkXmlConfig = true; // 控制是否显示 Link.xml 配置
        public FantasySettingsProvider() : base("Project/Fantasy Settings", SettingsScope.Project) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            Init();
            base.OnActivate(searchContext, rootElement);
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            FantasySettingsScriptableObject.Save();
        }

        private void Init()
        {
            _serializedObject?.Dispose();
            _serializedObject = new SerializedObject(FantasySettingsScriptableObject.Instance);
            _autoCopyAssembly = _serializedObject.FindProperty("autoCopyAssembly");
            _hotUpdatePath = _serializedObject.FindProperty("hotUpdatePath");
            _hotUpdateAssemblyDefinitions = _serializedObject.FindProperty("hotUpdateAssemblyDefinitions");
            _linkAssemblyDefinitions = _serializedObject.FindProperty("linkAssemblyDefinitions");
            _includeAssembly = _serializedObject.FindProperty("includeAssembly");
        }

        public override void OnGUI(string searchContext)
        {
            if (_serializedObject == null || !_serializedObject.targetObject)
            {
                Init();
            }

            using (CreateSettingsWindowGUIScope())
            {
                _serializedObject!.Update();

                // ============ Fantasy 框架集成检测 ============
                DrawCscRspInstallationSection();
                DrawSectionDivider();
                EditorGUILayout.Space(10);
                // ============ Fantasy WebGL 支持检测 ============
                DrawWebGLInstallationSection();
                DrawSectionDivider();
                EditorGUILayout.Space(10);
                // ============ 程序集自动拷贝设置 ============
                EditorGUI.BeginChangeCheck();
                DrawAssemblyCopySection();
                DrawSectionDivider();
                // ============ Link.xml 生成设置 ============
                DrawLinkXmlSection();

                if (EditorGUI.EndChangeCheck())
                {
                    _serializedObject.ApplyModifiedProperties();
                    FantasySettingsScriptableObject.Save();
                    EditorApplication.RepaintHierarchyWindow();
                }

                base.OnGUI(searchContext);
            }
        }

        private IDisposable CreateSettingsWindowGUIScope()
        {
            var unityEditorAssembly = System.Reflection.Assembly.GetAssembly(typeof(EditorWindow));
            var type = unityEditorAssembly.GetType("UnityEditor.SettingsWindow+GUIScope");
            return Activator.CreateInstance(type) as IDisposable;
        }

        /// <summary>
        /// 绘制 FANTASY_UNITY 安装状态区域
        /// </summary>
        private void DrawCscRspInstallationSection()
        {
            bool isInstalled = CheckFantasyUnityDefineStatus();
            
            // 状态框
            if (isInstalled)
            {
                // 已安装 - 绿色背景框
                GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(0, 0, 5, 5)
                };

                Color originalColor = GUI.color;
                GUI.color = new Color(0.7f, 1f, 0.7f);  // 绿色背景

                EditorGUILayout.BeginVertical(boxStyle);
                GUI.color = originalColor;

                GUIStyle installedButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 35
                };

                Color originalBgColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.6f, 0.4f);  // 橙红色
                if (GUILayout.Button("✓ FANTASY_UNITY 已安装 - 点击卸载", installedButtonStyle))
                {
                    if (EditorUtility.DisplayDialog("确认卸载",
                        "确定要卸载 FANTASY_UNITY 预编译符号吗？\n\n卸载后需要重新编译项目才能生效。",
                        "确定卸载", "取消"))
                    {
                        UninstallFantasyUnityDefine();
                    }
                }
                GUI.backgroundColor = originalBgColor;

                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("编译器配置正确，框架功能已启用", MessageType.Info);

                EditorGUILayout.EndVertical();
            }
            else
            {
                // 未安装 - 橙黄色背景框
                GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(0, 0, 5, 5)
                };

                Color originalColor = GUI.color;
                GUI.color = Color.red;  // 橙黄色背景
                
                EditorGUILayout.BeginVertical(boxStyle);
                GUI.color = originalColor;

                EditorGUILayout.Space(8);

                // 醒目的大按钮
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 35
                };

                Color originalBgColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
                if (GUILayout.Button("点击安装 FANTASY_UNITY", buttonStyle))
                {
                    InstallFantasyUnityDefine();
                }
                GUI.backgroundColor = originalBgColor;

                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("安装后可能需要重新编译项目才能生效", MessageType.Info);

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 检测当前激活平台的 Scripting Define Symbols 是否包含 FANTASY_UNITY
        /// </summary>
        /// <returns>是否已正确安装</returns>
        private bool CheckFantasyUnityDefineStatus()
        {
            return HasScriptingDefineSymbol("FANTASY_UNITY");
        }

        /// <summary>
        /// 安装 FANTASY_UNITY 定义到当前激活平台的 Scripting Define Symbols
        /// </summary>
        private void InstallFantasyUnityDefine()
        {
            try
            {
                bool changed = AddScriptingDefineSymbol("FANTASY_UNITY");
                string activeGroup = GetActiveBuildTargetGroup().ToString();

                if (changed)
                {
                    EditorUtility.DisplayDialog("成功", $"FANTASY_UNITY 已安装到 {activeGroup} 的 Scripting Define Symbols。\n\nUnity 将自动重新编译。", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", $"{activeGroup} 的 Scripting Define Symbols 中已存在 FANTASY_UNITY。", "确定");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"安装 FANTASY_UNITY 失败: {ex.Message}");
                EditorUtility.DisplayDialog("错误", $"安装失败:\n{ex.Message}", "确定");
            }
        }

        /// <summary>
        /// 从当前激活平台的 Scripting Define Symbols 卸载 FANTASY_UNITY
        /// </summary>
        private void UninstallFantasyUnityDefine()
        {
            try
            {
                bool changed = RemoveScriptingDefineSymbol("FANTASY_UNITY");
                string activeGroup = GetActiveBuildTargetGroup().ToString();

                if (changed)
                {
                    EditorUtility.DisplayDialog("成功", $"FANTASY_UNITY 已从 {activeGroup} 的 Scripting Define Symbols 中卸载。\n\nUnity 将自动重新编译。", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", $"{activeGroup} 的 Scripting Define Symbols 中不存在 FANTASY_UNITY。", "确定");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"卸载 FANTASY_UNITY 失败: {ex.Message}");
                EditorUtility.DisplayDialog("错误", $"卸载失败:\n{ex.Message}", "确定");
            }
        }

        /// <summary>
        /// 绘制 FANTASY_WEBGL 安装状态区域
        /// </summary>
        private void DrawWebGLInstallationSection()
        {
            bool isInstalled = CheckWebGLDefineStatus(out bool fileExists, out bool hasDefine);

            // 状态框
            if (isInstalled)
            {
                // 已安装 - 绿色背景框
                GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(0, 0, 5, 5)
                };

                Color originalColor = GUI.color;
                GUI.color = new Color(0.7f, 1f, 0.7f);  // 绿色背景

                EditorGUILayout.BeginVertical(boxStyle);
                GUI.color = originalColor;

                GUIStyle installedButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 35
                };

                Color originalBgColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.6f, 0.4f);  // 橙红色
                if (GUILayout.Button("✓ FANTASY_WEBGL 已安装 - 点击卸载", installedButtonStyle))
                {
                    if (EditorUtility.DisplayDialog("确认卸载",
                        "确定要卸载 FANTASY_WEBGL 预编译符号吗？\n\n卸载后需要重新编译项目才能生效。",
                        "确定卸载", "取消"))
                    {
                        UninstallFantasyWebGLDefine();
                    }
                }
                GUI.backgroundColor = originalBgColor;

                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("WebGL 平台支持已启用", MessageType.Info);

                EditorGUILayout.EndVertical();
            }
            else
            {
                // 未安装 - 橙黄色背景框
                GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(0, 0, 5, 5)
                };

                Color originalColor = GUI.color;
                GUI.color = Color.red;  // 橙黄色背景

                EditorGUILayout.BeginVertical(boxStyle);
                GUI.color = originalColor;

                EditorGUILayout.Space(8);

                // 醒目的大按钮
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 35
                };

                Color originalBgColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
                if (GUILayout.Button("点击安装 FANTASY_WEBGL", buttonStyle))
                {
                    InstallFantasyWebGLDefine();
                }
                GUI.backgroundColor = originalBgColor;

                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("安装后可能需要重新编译项目才能生效", MessageType.Info);

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 检测当前激活平台的 Scripting Define Symbols 是否包含 FANTASY_WEBGL
        /// </summary>
        /// <returns>是否已正确安装</returns>
        private bool CheckWebGLDefineStatus(out bool fileExists, out bool hasDefine)
        {
            fileExists = true;
            hasDefine = HasScriptingDefineSymbol("FANTASY_WEBGL");
            return hasDefine;
        }

        /// <summary>
        /// 安装 FANTASY_WEBGL 定义到当前激活平台的 Scripting Define Symbols
        /// </summary>
        private void InstallFantasyWebGLDefine()
        {
            try
            {
                bool changed = AddScriptingDefineSymbol("FANTASY_WEBGL");
                string activeGroup = GetActiveBuildTargetGroup().ToString();

                if (changed)
                {
                    EditorUtility.DisplayDialog("成功", $"FANTASY_WEBGL 已安装到 {activeGroup} 的 Scripting Define Symbols。\n\nUnity 将自动重新编译。", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", $"{activeGroup} 的 Scripting Define Symbols 中已存在 FANTASY_WEBGL。", "确定");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"安装 FANTASY_WEBGL 失败: {ex.Message}");
                EditorUtility.DisplayDialog("错误", $"安装失败:\n{ex.Message}", "确定");
            }
        }

        /// <summary>
        /// 从当前激活平台的 Scripting Define Symbols 卸载 FANTASY_WEBGL
        /// </summary>
        private void UninstallFantasyWebGLDefine()
        {
            try
            {
                bool changed = RemoveScriptingDefineSymbol("FANTASY_WEBGL");
                string activeGroup = GetActiveBuildTargetGroup().ToString();

                if (changed)
                {
                    EditorUtility.DisplayDialog("成功", $"FANTASY_WEBGL 已从 {activeGroup} 的 Scripting Define Symbols 中卸载。\n\nUnity 将自动重新编译。", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", $"{activeGroup} 的 Scripting Define Symbols 中不存在 FANTASY_WEBGL。", "确定");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"卸载 FANTASY_WEBGL 失败: {ex.Message}");
                EditorUtility.DisplayDialog("错误", $"卸载失败:\n{ex.Message}", "确定");
            }
        }

        /// <summary>
        /// 绘制分隔线
        /// </summary>
        private void DrawSectionDivider()
        {
            EditorGUILayout.Space(5);

            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;

            // 绘制细线
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));

            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// 绘制程序集自动拷贝设置区域
        /// </summary>
        private void DrawAssemblyCopySection()
        {
            // 标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };
            EditorGUILayout.LabelField("程序集自动拷贝", titleStyle);
            EditorGUILayout.Space(5);

            // 功能说明
            GUIStyle descStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 11,
                normal = { textColor = Color.gray }
            };
            EditorGUILayout.LabelField("Unity 编译后自动将指定的程序集文件（DLL + PDB）复制到目标目录，用于热更新或其他用途", descStyle);
            EditorGUILayout.Space(8);

            // 主开关区域
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 12, 12),
                margin = new RectOffset(0, 0, 0, 8)
            };

            EditorGUILayout.BeginVertical(boxStyle);

            // 开关 - 更大更明显
            EditorGUILayout.BeginHorizontal();

            GUIStyle bigToggleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            bool newValue = EditorGUILayout.ToggleLeft("启用自动拷贝", _autoCopyAssembly.boolValue, bigToggleStyle);

            if (newValue != _autoCopyAssembly.boolValue)
            {
                _autoCopyAssembly.boolValue = newValue;
            }

            GUILayout.FlexibleSpace();

            if (_autoCopyAssembly.boolValue)
            {
                GUIStyle statusLabel = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(0f, 0.6f, 0f) }
                };
                EditorGUILayout.LabelField("● 已启用", statusLabel, GUILayout.Width(60));
            }
            else
            {
                GUIStyle statusLabel = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 11,
                    normal = { textColor = Color.gray }
                };
                EditorGUILayout.LabelField("○ 已禁用", statusLabel, GUILayout.Width(60));
            }
            EditorGUILayout.EndHorizontal();

            // 如果启用了自动拷贝，显示详细配置
            if (_autoCopyAssembly.boolValue)
            {
                EditorGUILayout.Space(15);

                // 步骤 1：设置输出目录
                DrawStepHeader("1", "设置输出目录");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(_hotUpdatePath, GUIContent.none);
                if (GUILayout.Button("选择文件夹", GUILayout.Width(80)))
                {
                    string path = EditorUtility.OpenFolderPanel("选择程序集输出目录", _hotUpdatePath.stringValue, "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        _hotUpdatePath.stringValue = path;
                    }
                }
                EditorGUILayout.EndHorizontal();

                // 路径状态提示
                if (string.IsNullOrEmpty(_hotUpdatePath.stringValue))
                {
                    EditorGUILayout.Space(3);
                    GUIStyle warningStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(0.8f, 0.4f, 0f) }
                    };
                    EditorGUILayout.LabelField("⚠ 请先设置输出目录", warningStyle);
                }
                else if (!Directory.Exists(_hotUpdatePath.stringValue))
                {
                    EditorGUILayout.Space(3);
                    GUIStyle warningStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(0.6f, 0.6f, 0f) }
                    };
                    EditorGUILayout.LabelField("ℹ 目录不存在，编译时将自动创建", warningStyle);
                }
                else
                {
                    EditorGUILayout.Space(3);
                    GUIStyle successStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(0f, 0.6f, 0f) }
                    };
                    EditorGUILayout.LabelField("✓ 目录已配置", successStyle);
                }

                EditorGUILayout.Space(15);

                // 步骤 2：选择要拷贝的程序集
                DrawStepHeader("2", "程序集列表");

                GUIStyle hintStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 10,
                    normal = { textColor = Color.gray }
                };
                EditorGUILayout.LabelField("选择需要自动拷贝的热更新程序集", hintStyle);
                EditorGUILayout.Space(10);

                // 程序集列表区域
                if (_hotUpdateAssemblyDefinitions.arraySize == 0)
                {
                    // 空状态提示框
                    Color originalColor = GUI.color;
                    GUI.color = new Color(0.95f, 0.95f, 0.95f);
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    GUI.color = originalColor;

                    EditorGUILayout.Space(10);

                    GUIStyle emptyIconStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 20,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                    };
                    EditorGUILayout.LabelField("📦", emptyIconStyle);

                    GUIStyle emptyTextStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.gray }
                    };
                    EditorGUILayout.LabelField("暂未添加任何程序集", emptyTextStyle);

                    EditorGUILayout.Space(5);

                    GUIStyle addHintStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 10,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = new Color(0.3f, 0.6f, 1f) }
                    };
                    EditorGUILayout.LabelField("点击下方的 + 按钮添加程序集", addHintStyle);

                    EditorGUILayout.Space(10);

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }

                // 程序集列表
                EditorGUILayout.PropertyField(_hotUpdateAssemblyDefinitions, new GUIContent(""), true);

                if (_hotUpdateAssemblyDefinitions.arraySize > 0)
                {
                    EditorGUILayout.Space(5);
                    GUIStyle successStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(0f, 0.6f, 0f) }
                    };
                    EditorGUILayout.LabelField($"✓ 已添加 {_hotUpdateAssemblyDefinitions.arraySize} 个程序集", successStyle);
                }

                // 配置完成状态总结
                bool isConfigured = !string.IsNullOrEmpty(_hotUpdatePath.stringValue) && _hotUpdateAssemblyDefinitions.arraySize > 0;

                if (isConfigured)
                {
                    Color originalColor = GUI.color;
                    GUI.color = new Color(0.8f, 1f, 0.8f);
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    GUI.color = originalColor;

                    GUIStyle summaryStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 11,
                        normal = { textColor = new Color(0f, 0.5f, 0f) }
                    };
                    EditorGUILayout.LabelField($"✓ 配置完成！编译后将自动拷贝 {_hotUpdateAssemblyDefinitions.arraySize} 个程序集到目标目录", summaryStyle);

                    EditorGUILayout.EndVertical();
                }
            }
            else
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox("启用后可在 Unity 编译完成时自动拷贝程序集文件", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制步骤标题
        /// </summary>
        private void DrawStepHeader(string stepNumber, string title)
        {
            EditorGUILayout.BeginHorizontal();

            // 步骤编号
            GUIStyle stepStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.3f, 0.6f, 1f) }
            };
            EditorGUILayout.LabelField($"步骤 {stepNumber}:", stepStyle, GUILayout.Width(60));

            // 步骤标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };
            EditorGUILayout.LabelField(title, titleStyle);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// 绘制 Link.xml 生成设置区域
        /// </summary>
        private void DrawLinkXmlSection()
        {
            // 标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };
            EditorGUILayout.LabelField("Link.xml 代码裁剪配置", titleStyle);
            EditorGUILayout.Space(5);

            // 功能说明
            GUIStyle descStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 11,
                normal = { textColor = Color.gray }
            };
            EditorGUILayout.LabelField("配置 Link.xml 文件以防止 IL2CPP 编译时过度裁剪反射或动态调用的代码，确保运行时正常", descStyle);
            EditorGUILayout.Space(8);

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 12, 12),
                margin = new RectOffset(0, 0, 0, 8)
            };

            EditorGUILayout.BeginVertical(boxStyle);

            // 显示配置开关
            EditorGUILayout.BeginHorizontal();

            GUIStyle bigToggleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            _showLinkXmlConfig = EditorGUILayout.ToggleLeft("显示配置", _showLinkXmlConfig, bigToggleStyle);

            GUILayout.FlexibleSpace();

            if (_showLinkXmlConfig)
            {
                GUIStyle statusLabel = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(0.3f, 0.6f, 1f) }
                };
                EditorGUILayout.LabelField("● 已展开", statusLabel, GUILayout.Width(60));
            }
            else
            {
                GUIStyle statusLabel = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 11,
                    normal = { textColor = Color.gray }
                };
                EditorGUILayout.LabelField("○ 已折叠", statusLabel, GUILayout.Width(60));
            }
            EditorGUILayout.EndHorizontal();

            // 如果显示配置，则展示详细内容
            if (_showLinkXmlConfig)
            {
                EditorGUILayout.Space(15);

                // 程序集定义配置标题
                GUIStyle sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13
                };
                EditorGUILayout.LabelField("需要保护的程序集", sectionTitleStyle);
                EditorGUILayout.Space(3);

                GUIStyle hintStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 10,
                    normal = { textColor = Color.gray }
                };
                EditorGUILayout.LabelField("选择需要在 IL2CPP 编译时防止代码裁剪的程序集", hintStyle);
                EditorGUILayout.Space(3);

                // 默认包含程序集说明
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(5);
                GUIStyle defaultInfoStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 10,
                    normal = { textColor = new Color(0.4f, 0.6f, 0.8f) }
                };
                EditorGUILayout.LabelField("ℹ 默认包含：Assembly-CSharp、Fantasy.Unity", defaultInfoStyle);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(10);

                // 程序集列表区域
                if (_linkAssemblyDefinitions.arraySize == 0)
                {
                    // 空状态提示框
                    Color originalColor = GUI.color;
                    GUI.color = new Color(0.95f, 0.95f, 0.95f);
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    GUI.color = originalColor;

                    EditorGUILayout.Space(10);

                    GUIStyle emptyIconStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 20,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                    };
                    EditorGUILayout.LabelField("📋", emptyIconStyle);

                    GUIStyle emptyTextStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.gray }
                    };
                    EditorGUILayout.LabelField("暂未添加任何程序集", emptyTextStyle);

                    EditorGUILayout.Space(5);

                    GUIStyle addHintStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 10,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = new Color(0.3f, 0.6f, 1f) }
                    };
                    EditorGUILayout.LabelField("点击下方的 + 按钮添加程序集", addHintStyle);

                    EditorGUILayout.Space(10);

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }

                // 程序集列表
                EditorGUILayout.PropertyField(_linkAssemblyDefinitions, new GUIContent(""), true);

                if (_linkAssemblyDefinitions.arraySize > 0)
                {
                    EditorGUILayout.Space(5);
                    GUIStyle successStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(0f, 0.6f, 0f) }
                    };
                    EditorGUILayout.LabelField($"✓ 已添加 {_linkAssemblyDefinitions.arraySize} 个程序集", successStyle);
                }

                EditorGUILayout.Space(15);

                // 生成按钮区域
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 35,
                    fixedWidth = 200
                };

                Color originalBgColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
                if (GUILayout.Button("生成 Link.xml 文件", buttonStyle))
                {
                    LinkXmlGenerator.GenerateLinkXml();
                    EditorUtility.DisplayDialog("操作成功", "Link.xml 文件已生成", "确定");
                }
                GUI.backgroundColor = originalBgColor;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox("点击上方开关展开配置", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        static FantasySettingsProvider _provider;

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            if (FantasySettingsScriptableObject.Instance && _provider == null)
            {
                _provider = new FantasySettingsProvider();
                using (var so = new SerializedObject(FantasySettingsScriptableObject.Instance))
                {
                    _provider.keywords = GetSearchKeywordsFromSerializedObject(so);
                }
            }
            return _provider;
        }

        #region Scripting Define Symbols Helpers

        /// <summary>
        /// 获取当前激活平台的 BuildTargetGroup
        /// </summary>
        private static BuildTargetGroup GetActiveBuildTargetGroup()
        {
            return EditorUserBuildSettings.selectedBuildTargetGroup;
        }

        /// <summary>
        /// 获取当前激活平台的 Scripting Define Symbols 列表
        /// </summary>
        private static HashSet<string> GetCurrentDefineSymbols(out string rawDefines)
        {
            var group = GetActiveBuildTargetGroup();
            rawDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var set = new HashSet<string>(StringComparer.Ordinal);

            if (!string.IsNullOrEmpty(rawDefines))
            {
                foreach (var symbol in rawDefines.Split(';'))
                {
                    var trimmed = symbol.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        set.Add(trimmed);
                    }
                }
            }

            return set;
        }

        /// <summary>
        /// 检测当前激活平台的 Scripting Define Symbols 是否包含指定符号
        /// </summary>
        private static bool HasScriptingDefineSymbol(string symbol)
        {
            var defines = GetCurrentDefineSymbols(out _);
            return defines.Contains(symbol);
        }

        /// <summary>
        /// 向当前激活平台的 Scripting Define Symbols 添加指定符号
        /// </summary>
        /// <returns>true 表示实际添加了新符号，false 表示已存在</returns>
        private static bool AddScriptingDefineSymbol(string symbol)
        {
            var defines = GetCurrentDefineSymbols(out _);

            if (defines.Contains(symbol))
            {
                return false;
            }

            defines.Add(symbol);
            var group = GetActiveBuildTargetGroup();
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
            return true;
        }

        /// <summary>
        /// 从当前激活平台的 Scripting Define Symbols 移除指定符号
        /// </summary>
        /// <returns>true 表示实际移除了符号，false 表示不存在</returns>
        private static bool RemoveScriptingDefineSymbol(string symbol)
        {
            var defines = GetCurrentDefineSymbols(out _);

            if (!defines.Remove(symbol))
            {
                return false;
            }

            var group = GetActiveBuildTargetGroup();
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
            return true;
        }

        #endregion
    }
}
