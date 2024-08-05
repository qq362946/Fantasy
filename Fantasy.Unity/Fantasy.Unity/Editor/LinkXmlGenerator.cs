using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Fantasy
{
    public class LinkXmlGenerator
    {
        // 在Unity编辑器中运行该方法来生成link.xml文件
        [UnityEditor.MenuItem("Fantasy/Generate link.xml")]
        public static void GenerateLinkXml()
        {
            using (var writer = new StreamWriter("Assets/link.xml"))
            {
                writer.WriteLine("<linker>");
                GenerateLinkXml(writer, "Assembly-CSharp", "Assets/link.xml");
                GenerateLinkXml(writer, "Fantasy.Unity", "Assets/link.xml");
                writer.WriteLine("</linker>");
            }

            AssetDatabase.Refresh();
            Debug.Log("link.xml generated successfully!");
        }

        private static void GenerateLinkXml(StreamWriter writer, string assemblyName, string outputPath)
        {
            var assembly = Assembly.Load(assemblyName);
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