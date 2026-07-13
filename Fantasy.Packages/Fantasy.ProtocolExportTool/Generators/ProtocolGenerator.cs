using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fantasy.ProtocolExportTool.Generators.Parsers;
using Fantasy.ProtocolExportTool.Models;
using Fantasy.ProtocolExportTool.Services;
using Spectre.Console;

namespace Fantasy.ProtocolExportTool.Generators;

public class ProtocolGenerator
{
    public async Task GenerateAsync(ProtocolExportConfig config, CancellationToken ct = default)
    {
        var targets = CreateTargets(config);
        ValidateUniqueMessageNames(targets);
        var cachePath = string.IsNullOrWhiteSpace(config.OpCodeCacheFile)
            ? Path.Combine(Path.GetFullPath(config.ProtocolDir), "OpCode.Cache")
            : Path.GetFullPath(config.OpCodeCacheFile);

        using var opCodeCache = OpCodeCacheSession.Open(cachePath);
        var jobs = targets.ConvertAll(target => new ExportJob(
            target,
            new CSharpExporter(
                target.ProtocolDir,
                target.ClientDir,
                target.ServerDir,
                opCodeCache,
                target.ExportType)));

        foreach (var job in jobs)
        {
            ct.ThrowIfCancellationRequested();
            ParseTarget(job, ct);
        }

        var validationFailed = false;
        foreach (var job in jobs)
        {
            validationFailed |= job.Exporter.IsErrors();
        }

        if (validationFailed)
        {
            throw new InvalidOperationException("协议验证失败，未生成代码，也未更新 OpCode.Cache。");
        }

        foreach (var job in jobs)
        {
            ct.ThrowIfCancellationRequested();
            await GenerateTargetAsync(job, ct);
        }

        ct.ThrowIfCancellationRequested();
        opCodeCache.Commit();
    }

    private static List<ProtocolExportTarget> CreateTargets(ProtocolExportConfig config)
    {
        var targets = new List<ProtocolExportTarget>
        {
            new()
            {
                ProtocolDir = config.ProtocolDir,
                ServerDir = config.ServerDir,
                ClientDir = config.ClientDir,
                ExportType = config.ExportType
            }
        };

        targets.AddRange(config.PackageExports);
        return targets;
    }

    private static void ValidateUniqueMessageNames(IReadOnlyList<ProtocolExportTarget> targets)
    {
        var seenMessages = new Dictionary<string, MessageDefinition>(StringComparer.Ordinal);
        var duplicateErrors = new List<string>();

        foreach (var target in targets)
        {
            foreach (var protocol in new[] { "Outer", "Inner" })
            {
                var protocolPath = Path.Combine(target.ProtocolDir, protocol);
                if (!Directory.Exists(protocolPath))
                {
                    continue;
                }

                var files = Directory.GetFiles(protocolPath, "*.proto", SearchOption.AllDirectories)
                    .OrderBy(file => Path.GetRelativePath(protocolPath, file), StringComparer.Ordinal);

                foreach (var file in files)
                {
                    var parser = new ProtocolFileParser(file);
                    var parseResult = parser.Parse(File.ReadAllLines(file));

                    foreach (var message in parseResult.Messages.Values)
                    {
                        if (!message.HasOpCode)
                        {
                            continue;
                        }

                        if (seenMessages.TryGetValue(message.Name, out var previousMessage))
                        {
                            duplicateErrors.Add($"- {message.Name}: {previousMessage.SourceFilePath} line {previousMessage.SourceLineNumber} <-> {message.SourceFilePath} line {message.SourceLineNumber}");
                            continue;
                        }

                        seenMessages.Add(message.Name, message);
                    }
                }
            }
        }

        if (duplicateErrors.Count == 0)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("发现重名协议消息，已停止生成 OpCode。请先重命名以下消息后再导出:");
        foreach (var error in duplicateErrors)
        {
            builder.AppendLine(error);
        }

        throw new InvalidOperationException(builder.ToString().TrimEnd());
    }

    private static void ParseTarget(ExportJob job, CancellationToken ct)
    {
        AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            )
            .Start(ctx =>
            {
                var targetLabel = job.Target.ProtocolDir;
                var loadTask1 = ctx.AddTask($"[green]解析RouteType配置 ({targetLabel})...[/]");
                var loadTask2 = ctx.AddTask($"[green]解析RoamingType配置 ({targetLabel})...[/]");
                var loadTask3 = ctx.AddTask($"[green]解析并验证Outer协议 ({targetLabel})...[/]");
                var loadTask4 = ctx.AddTask($"[green]解析并验证Inner协议 ({targetLabel})...[/]");

                ct.ThrowIfCancellationRequested();
                job.Exporter.ParseRouteTypeConfig();
                loadTask1.Increment(100);

                ct.ThrowIfCancellationRequested();
                job.Exporter.ParseRoamingTypeConfig();
                loadTask2.Increment(100);

                ct.ThrowIfCancellationRequested();
                job.Exporter.ParseAndValidateOuterProtocols();
                loadTask3.Increment(100);

                ct.ThrowIfCancellationRequested();
                job.Exporter.ParseAndValidateInnerProtocols();
                loadTask4.Increment(100);
            });
    }

    private static async Task GenerateTargetAsync(ExportJob job, CancellationToken ct)
    {
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
                var targetLabel = job.Target.ProtocolDir;
                var generateTask1 = ctx.AddTask($"[green]生成RouteType代码 ({targetLabel})...[/]");
                var generateTask2 = ctx.AddTask($"[green]生成RoamingType代码 ({targetLabel})...[/]");
                var generateTask3 = ctx.AddTask($"[green]生成OuterOpcode代码 ({targetLabel})...[/]");
                var generateTask4 = ctx.AddTask($"[green]生成InnerOpcode代码 ({targetLabel})...[/]");
                var generateTask5 = ctx.AddTask($"[green]生成OuterMessage代码 ({targetLabel})...[/]");
                var generateTask6 = ctx.AddTask($"[green]生成InnerMessage代码 ({targetLabel})...[/]");
                var generateTask7 = ctx.AddTask($"[green]生成OuterMessageHelper代码 ({targetLabel})...[/]");
                var generateTask8 = ctx.AddTask($"[green]生成OuterEnum代码 ({targetLabel})...[/]");
                var generateTask9 = ctx.AddTask($"[green]生成InnerEnum代码 ({targetLabel})...[/]");

                ct.ThrowIfCancellationRequested();
                await job.Exporter.GenerateRouteTypeAsync();
                generateTask1.Increment(100);

                ct.ThrowIfCancellationRequested();
                await job.Exporter.GenerateRoamingTypeAsync();
                generateTask2.Increment(100);

                ct.ThrowIfCancellationRequested();
                await job.Exporter.GenerateOuterOpcodeAsync();
                generateTask3.Increment(100);

                ct.ThrowIfCancellationRequested();
                await job.Exporter.GenerateInnerOpcodeAsync();
                generateTask4.Increment(100);

                ct.ThrowIfCancellationRequested();
                await job.Exporter.GenerateOuterMessageAsync();
                generateTask5.Increment(100);

                ct.ThrowIfCancellationRequested();
                await job.Exporter.GenerateInnerMessageAsync();
                generateTask6.Increment(100);

                ct.ThrowIfCancellationRequested();
                await job.Exporter.GenerateOuterMessageHelperAsync();
                generateTask7.Increment(100);

                ct.ThrowIfCancellationRequested();
                await job.Exporter.GenerateOuterEnumsAsync();
                generateTask8.Increment(100);

                ct.ThrowIfCancellationRequested();
                await job.Exporter.GenerateInnerEnumsAsync();
                generateTask9.Increment(100);
            });
    }

    private sealed record ExportJob(ProtocolExportTarget Target, CSharpExporter Exporter);
}
