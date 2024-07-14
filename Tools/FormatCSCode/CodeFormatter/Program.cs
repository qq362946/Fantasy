// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using Formatter;

var rootCommand = new RootCommand("This is an app for csharp script formatting");
var formatFileCommand = new Command("script", "format a script file");
var fileArgument = new Argument<string>("file path to format");

formatFileCommand.AddArgument(fileArgument);
formatFileCommand.SetHandler(filePath => _ = new CSharpScriptFormatter(filePath), fileArgument);
rootCommand.AddCommand(formatFileCommand);

var formatCodeCommand = new Command("code", "format a code string");
var codeArgument = new Argument<string>("code to format");
formatCodeCommand.AddArgument(codeArgument);
formatCodeCommand.SetHandler(code => _ = new CSharpCodeFormatter(code), codeArgument);
rootCommand.AddCommand(formatCodeCommand);

return await rootCommand.InvokeAsync(args);