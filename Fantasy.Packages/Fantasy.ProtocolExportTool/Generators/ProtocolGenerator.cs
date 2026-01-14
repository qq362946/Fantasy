using System.Threading;
using System.Threading.Tasks;
using Fantasy.ProtocolExportTool.Models;
using Spectre.Console;

namespace Fantasy.ProtocolExportTool.Generators;

public class ProtocolGenerator
{
    public async Task GenerateAsync(ProtocolExportConfig config, CancellationToken ct = default)
    {
        var createdSuccessfully = false;
        var protocolExporter = new CSharpExporter(
            config.ProtocolDir,
            config.ClientDir,
            config.ServerDir, config.ExportType);
        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            )
            .StartAsync(async ctx =>
            {
                var loadTask1 = ctx.AddTask("[green]解析RouteType配置...[/]");
                var loadTask2 = ctx.AddTask("[green]解析RoamingType配置...[/]");
                var loadTask3 = ctx.AddTask("[green]解析并验证Outer协议...[/]");
                var loadTask4 = ctx.AddTask("[green]解析并验证Inner协议...[/]");
                
                var runningLoadTask1 = Task.Run(() =>
                {
                    protocolExporter.ParseRouteTypeConfig();
                    loadTask1.Increment(100);
                }, ct);

                var runningLoadTask2 = Task.Run(() =>
                {
                    protocolExporter.ParseRoamingTypeConfig();
                    loadTask2.Increment(100);
                }, ct);

                var runningLoadTask3 = Task.Run(() =>
                {
                    protocolExporter.ParseAndValidateOuterProtocols();
                    loadTask3.Increment(100);
                }, ct);

                var runningLoadTask4 = Task.Run(() =>
                {
                    protocolExporter.ParseAndValidateInnerProtocols();
                    loadTask4.Increment(100);
                }, ct);

                await Task.WhenAll(runningLoadTask1, runningLoadTask2, runningLoadTask3, runningLoadTask4);
                
                if (protocolExporter.IsErrors())
                {
                    return;
                }
                
                var generateTask1 = ctx.AddTask("[green]生成RouteType代码...[/]");
                var generateTask2 = ctx.AddTask("[green]生成RoamingType代码...[/]");
                var generateTask3 = ctx.AddTask("[green]生成OuterOpcode代码...[/]");
                var generateTask4 = ctx.AddTask("[green]生成InnerOpcode代码...[/]");
                var generateTask5 = ctx.AddTask("[green]生成OuterMessage代码...[/]");
                var generateTask6 = ctx.AddTask("[green]生成InnerMessage代码...[/]");
                var generateTask7 = ctx.AddTask("[green]生成OuterMessageHelper代码...[/]");
                var generateTask8 = ctx.AddTask("[green]生成OuterEnum代码...[/]");
                var generateTask9 = ctx.AddTask("[green]生成InnerEnum代码...[/]");
                
                var runningGenerateTask1 = Task.Run(async () =>
                {
                    await protocolExporter.GenerateRouteTypeAsync();
                    generateTask1.Increment(100);
                }, ct);

                var runningGenerateTask2 = Task.Run(async () =>
                {
                    await protocolExporter.GenerateRoamingTypeAsync();
                    generateTask2.Increment(100);
                }, ct);

                var runningGenerateTask3 = Task.Run(async () =>
                {
                    await protocolExporter.GenerateOuterOpcodeAsync();
                    generateTask3.Increment(100);
                }, ct);

                var runningGenerateTask4 = Task.Run(async () =>
                {
                    await protocolExporter.GenerateInnerOpcodeAsync();
                    generateTask4.Increment(100);
                }, ct);
                
                var runningGenerateTask5 = Task.Run(async () =>
                {
                    await protocolExporter.GenerateOuterMessageAsync();
                    generateTask5.Increment(100);
                }, ct);
                
                var runningGenerateTask6 = Task.Run(async () =>
                {
                    await protocolExporter.GenerateInnerMessageAsync();
                    generateTask6.Increment(100);
                }, ct);
                
                var runningGenerateTask7 = Task.Run(async () =>
                {
                    await protocolExporter.GenerateOuterMessageHelperAsync();
                    generateTask7.Increment(100);
                }, ct);

                var runningGenerateTask8 = Task.Run(async () =>
                {
                    await protocolExporter.GenerateOuterEnumsAsync();
                    generateTask8.Increment(100);
                }, ct);

                var runningGenerateTask9 = Task.Run(async () =>
                {
                    await protocolExporter.GenerateInnerEnumsAsync();
                    generateTask9.Increment(100);
                }, ct);

                await Task.WhenAll(
                    runningGenerateTask1,
                    runningGenerateTask2,
                    runningGenerateTask3,
                    runningGenerateTask4,
                    runningGenerateTask5,
                    runningGenerateTask6,
                    runningGenerateTask7,
                    runningGenerateTask8,
                    runningGenerateTask9);
                
                createdSuccessfully = true;
            });

        if (createdSuccessfully)
        {
            // 输出已移至 ProtocolExportService 统一处理
        }
    }
}