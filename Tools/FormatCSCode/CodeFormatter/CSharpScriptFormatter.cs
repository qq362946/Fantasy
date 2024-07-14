using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Formatter;

public class CSharpScriptFormatter
{
    public CSharpScriptFormatter(string filePath)
    {
        try
        {
            FileInfo fileInfo = new(filePath);
            
            string formattedCode;
            using (var fs = fileInfo.OpenRead())
            {
                using var sr = new StreamReader(fs);
                var content = sr.ReadToEnd();
                var workspace = new AdhocWorkspace();
                var options = workspace.Options;
                var syntaxTree = CSharpSyntaxTree.ParseText(content);
                var root = syntaxTree.GetRoot();
                var formattedRoot = Microsoft.CodeAnalysis.Formatting.Formatter.Format(root, workspace, options);
                formattedCode = formattedRoot.ToFullString();
                Console.WriteLine(formattedCode);
            }

            if (string.IsNullOrEmpty(formattedCode))
                return;
            
            File.WriteAllText(filePath, formattedCode);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            Environment.Exit(1);
        }
    }
}