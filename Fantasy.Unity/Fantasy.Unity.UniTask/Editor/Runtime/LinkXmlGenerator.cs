using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fantasy
{
    public class LinkXmlGenerator
    {
        private const string LinkPath = "Assets/link.xml";
        // 在Unity编辑器中运行该方法来生成link.xml文件
        [UnityEditor.MenuItem("Fantasy/Generate link.xml")]
        public static void GenerateLinkXml()
        {
            using (var writer = new StreamWriter("Assets/link.xml"))
            {
                writer.WriteLine("<linker>");
                GenerateLinkXml(writer, "Assembly-CSharp", LinkPath);
                Debug.Log("Assembly-CSharp Link generation completed");
                GenerateLinkXml(writer, "Fantasy.Unity", LinkPath);
                Debug.Log("Fantasy.Unity Link generation completed");
                foreach (var linkAssembly in FantasySettingsScriptableObject.Instance.linkAssemblyDefinitions)
                {
                    GenerateLinkXml(writer, linkAssembly.name, LinkPath);
                    Debug.Log($"{linkAssembly.name} Link generation completed");
                }
                writer.WriteLine("</linker>");
            }

            AssetDatabase.Refresh();
            Debug.Log("link.xml generated successfully!");
        }

        private static void GenerateLinkXml(StreamWriter writer, string assemblyName, string outputPath)
        {
            var assembly = System.Reflection.Assembly.Load(assemblyName);
            var types = assembly.GetTypes();
            writer.WriteLine($"  <assembly fullname=\"{assembly.GetName().Name}\">");
            foreach (var type in types)
            {
                var typeName = type.FullName.Replace('<', '+').Replace('>', '+');
                writer.WriteLine($"    <type fullname=\"{typeName}\" preserve=\"all\"/>");
            }
            writer.WriteLine("  </assembly>");
        }
    }
}