using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Formatter;

public class CSharpCodeFormatter
{
    public CSharpCodeFormatter(string code)
    {
        try
        {
            var workspace = new AdhocWorkspace();
            var options = workspace.Options;
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var root = syntaxTree.GetRoot();
            var formattedRoot = Microsoft.CodeAnalysis.Formatting.Formatter.Format(root, workspace, options);
            string formattedCode = formattedRoot.ToFullString();
            Console.WriteLine(formattedCode);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            Environment.Exit(1);
        }
    }
    
}