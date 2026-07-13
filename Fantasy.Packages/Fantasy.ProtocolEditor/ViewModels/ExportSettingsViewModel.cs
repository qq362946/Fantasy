using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fantasy.ProtocolEditor.Services;
using Fantasy.ProtocolExportTool.Models;
using Fantasy.ProtocolExportTool.Services;

namespace Fantasy.ProtocolEditor.ViewModels;

public partial class ExportSettingsViewModel : ViewModelBase
{
    private bool _isLoadingSettings;

    /// <summary>
    /// 工作区路径（协议目录）- 只读显示
    /// </summary>
    [ObservableProperty]
    private string _workspacePath = string.Empty;

    /// <summary>
    /// 服务器输出目录
    /// </summary>
    [ObservableProperty]
    private string _serverOutputDirectory = string.Empty;

    /// <summary>
    /// 客户端输出目录
    /// </summary>
    [ObservableProperty]
    private string _clientOutputDirectory = string.Empty;

    /// <summary>
    /// 是否导出到服务器
    /// </summary>
    [ObservableProperty]
    private bool _exportToServer = true;

    /// <summary>
    /// 是否导出到客户端
    /// </summary>
    [ObservableProperty]
    private bool _exportToClient = true;

    /// <summary>
    /// 子包协议导出配置。
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PackageExportItemViewModel> _packageExports = new();

    /// <summary>
    /// 配置是否有效
    /// </summary>
    [ObservableProperty]
    private bool _isValid;

    /// <summary>
    /// 验证消息
    /// </summary>
    [ObservableProperty]
    private string _validationMessage = string.Empty;

    /// <summary>
    /// 配置是否已修改
    /// </summary>
    [ObservableProperty]
    private bool _isModified;

    /// <summary>
    /// 保存成功消息
    /// </summary>
    [ObservableProperty]
    private string _saveMessage = string.Empty;

    /// <summary>
    /// 加载配置
    /// </summary>
    public void LoadSettings(ExporterSettings settings)
    {
        _isLoadingSettings = true;
        try
        {
            var config = ConfigService.CreateProtocolExportConfig(settings);
            WorkspacePath = config.ProtocolDir;
            ServerOutputDirectory = settings.Export.NetworkProtocolServerDirectory.Value;
            ClientOutputDirectory = settings.Export.NetworkProtocolClientDirectory.Value;
            ExportToServer = settings.Export.ExportType.HasFlag(ProtocolExportType.Server);
            ExportToClient = settings.Export.ExportType.HasFlag(ProtocolExportType.Client);

            foreach (var package in PackageExports)
            {
                package.PropertyChanged -= OnPackageExportPropertyChanged;
            }

            PackageExports.Clear();
            foreach (var packageSettings in settings.Export.PackageExports)
            {
                var package = new PackageExportItemViewModel(packageSettings);
                package.PropertyChanged += OnPackageExportPropertyChanged;
                PackageExports.Add(package);
            }

            RefreshPackageDisplayNames();
        }
        finally
        {
            _isLoadingSettings = false;
        }

        IsModified = false;
        SaveMessage = string.Empty;
        ValidateSettings();
    }

    /// <summary>
    /// 保存配置命令
    /// </summary>
    [RelayCommand]
    private void SaveConfig()
    {
        try
        {
            var settings = ConfigService.LoadExporterSettings();

            // 更新导出配置
            settings.Export.NetworkProtocolServerDirectory.Value = ServerOutputDirectory;
            settings.Export.NetworkProtocolClientDirectory.Value = ClientOutputDirectory;
            settings.Export.ExportType = GetExportType();
            settings.Export.PackageExports = PackageExports
                .Select(package => package.ToSettings())
                .ToList();

            // 保存配置
            ConfigService.SaveExporterSettings(settings);

            IsModified = false;
            SaveMessage = "✅ 配置已保存";
        }
        catch (Exception ex)
        {
            SaveMessage = $"❌ 保存失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 验证设置
    /// </summary>
    [RelayCommand]
    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(WorkspacePath))
        {
            IsValid = false;
            ValidationMessage = "❌ 请先打开工作区文件夹";
            return;
        }

        if (!Directory.Exists(WorkspacePath))
        {
            IsValid = false;
            ValidationMessage = $"❌ 工作区目录不存在: {WorkspacePath}";
            return;
        }

        if (!ExportToServer && !ExportToClient)
        {
            IsValid = false;
            ValidationMessage = "❌ 请至少选择一个导出目标（服务器或客户端）";
            return;
        }

        if (ExportToServer && string.IsNullOrWhiteSpace(ServerOutputDirectory))
        {
            IsValid = false;
            ValidationMessage = "❌ 已启用服务器导出，但未配置服务器输出目录";
            return;
        }

        if (ExportToClient && string.IsNullOrWhiteSpace(ClientOutputDirectory))
        {
            IsValid = false;
            ValidationMessage = "❌ 已启用客户端导出，但未配置客户端输出目录";
            return;
        }

        for (var i = 0; i < PackageExports.Count; i++)
        {
            var package = PackageExports[i];
            var packageName = $"子包 {i + 1}";

            if (string.IsNullOrWhiteSpace(package.ProtocolDirectory))
            {
                IsValid = false;
                ValidationMessage = $"❌ {packageName}未配置协议目录";
                return;
            }

            var resolvedProtocolDirectory = ResolveOutputPath(package.ProtocolDirectory);
            if (!Directory.Exists(resolvedProtocolDirectory))
            {
                IsValid = false;
                ValidationMessage = $"❌ {packageName}协议目录不存在: {resolvedProtocolDirectory}";
                return;
            }

            if (!package.ExportToServer && !package.ExportToClient)
            {
                IsValid = false;
                ValidationMessage = $"❌ {packageName}请至少选择一个导出目标";
                return;
            }

            if (package.ExportToServer && string.IsNullOrWhiteSpace(package.ServerOutputDirectory))
            {
                IsValid = false;
                ValidationMessage = $"❌ {packageName}已启用服务器导出，但未配置服务器输出目录";
                return;
            }

            if (package.ExportToClient && string.IsNullOrWhiteSpace(package.ClientOutputDirectory))
            {
                IsValid = false;
                ValidationMessage = $"❌ {packageName}已启用客户端导出，但未配置客户端输出目录";
                return;
            }
        }

        IsValid = true;
        
        // 如果是相对路径，在提示中显示解析后的绝对路径，方便用户确认
        var serverHint = ExportToServer && !Path.IsPathRooted(ServerOutputDirectory)
            ? $"（解析为: {ResolveOutputPath(ServerOutputDirectory)}）"
            : string.Empty;
        var clientHint = ExportToClient && !Path.IsPathRooted(ClientOutputDirectory)
            ? $"（解析为: {ResolveOutputPath(ClientOutputDirectory)}）"
            : string.Empty;
        
        if (!string.IsNullOrEmpty(serverHint) || !string.IsNullOrEmpty(clientHint))
        {
            ValidationMessage = $"✅ 配置有效，可以执行导出 {serverHint}{clientHint}";
        }
        else
        {
            ValidationMessage = "✅ 配置有效，可以执行导出";
        }
    }

    /// <summary>
    /// 将输出路径解析为绝对路径。
    /// 若 outputPath 是相对路径，则以 ExporterSettings.json 所在目录为基准进行解析。
    /// </summary>
    private static string ResolveOutputPath(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return outputPath;
        }

        return ExporterSettingsService.ResolvePath(outputPath, ConfigService.CurrentWorkspaceDirectory);
    }

    private ProtocolExportType GetExportType()
    {
        var exportType = (ProtocolExportType)0;
        if (ExportToServer)
        {
            exportType |= ProtocolExportType.Server;
        }

        if (ExportToClient)
        {
            exportType |= ProtocolExportType.Client;
        }

        return exportType;
    }

    [RelayCommand]
    private void AddPackageExport()
    {
        var package = new PackageExportItemViewModel();
        package.PropertyChanged += OnPackageExportPropertyChanged;
        PackageExports.Add(package);
        RefreshPackageDisplayNames();
        MarkAsModified();
    }

    [RelayCommand]
    private void RemovePackageExport(PackageExportItemViewModel package)
    {
        if (!PackageExports.Remove(package))
        {
            return;
        }

        package.PropertyChanged -= OnPackageExportPropertyChanged;
        RefreshPackageDisplayNames();
        MarkAsModified();
    }

    private void OnPackageExportPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PackageExportItemViewModel.DisplayName))
        {
            return;
        }

        MarkAsModified();
    }

    private void RefreshPackageDisplayNames()
    {
        for (var i = 0; i < PackageExports.Count; i++)
        {
            PackageExports[i].DisplayName = $"子包协议 {i + 1}";
        }
    }

    private void MarkAsModified()
    {
        if (_isLoadingSettings)
        {
            return;
        }

        IsModified = true;
        SaveMessage = string.Empty;
        ValidateSettings();
    }

    /// <summary>
    /// 当属性改变时自动验证并标记为已修改
    /// </summary>
    partial void OnServerOutputDirectoryChanged(string value)
    {
        MarkAsModified();
    }

    partial void OnClientOutputDirectoryChanged(string value)
    {
        MarkAsModified();
    }

    partial void OnExportToServerChanged(bool value)
    {
        MarkAsModified();
    }

    partial void OnExportToClientChanged(bool value)
    {
        MarkAsModified();
    }

    partial void OnWorkspacePathChanged(string value)
    {
        ValidateSettings();
    }
}

public partial class PackageExportItemViewModel : ViewModelBase
{
    private string _protocolDirectoryComment = "子包ProtoBuf文件所在的文件夹位置";
    private string _serverDirectoryComment = "子包ProtoBuf生成到服务端的文件夹位置";
    private string _clientDirectoryComment = "子包ProtoBuf生成到客户端的文件夹位置";

    [ObservableProperty]
    private string _displayName = "子包协议";

    [ObservableProperty]
    private string _protocolDirectory = string.Empty;

    [ObservableProperty]
    private string _serverOutputDirectory = string.Empty;

    [ObservableProperty]
    private string _clientOutputDirectory = string.Empty;

    [ObservableProperty]
    private bool _exportToServer = true;

    [ObservableProperty]
    private bool _exportToClient = true;

    public PackageExportItemViewModel()
    {
    }

    public PackageExportItemViewModel(PackageExportSettings settings)
    {
        ProtocolDirectory = settings.NetworkProtocolDirectory.Value;
        ServerOutputDirectory = settings.NetworkProtocolServerDirectory.Value;
        ClientOutputDirectory = settings.NetworkProtocolClientDirectory.Value;
        ExportToServer = settings.ExportType.HasFlag(ProtocolExportType.Server);
        ExportToClient = settings.ExportType.HasFlag(ProtocolExportType.Client);
        _protocolDirectoryComment = settings.NetworkProtocolDirectory.Comment;
        _serverDirectoryComment = settings.NetworkProtocolServerDirectory.Comment;
        _clientDirectoryComment = settings.NetworkProtocolClientDirectory.Comment;
    }

    public PackageExportSettings ToSettings()
    {
        var exportType = (ProtocolExportType)0;
        if (ExportToServer)
        {
            exportType |= ProtocolExportType.Server;
        }

        if (ExportToClient)
        {
            exportType |= ProtocolExportType.Client;
        }

        return new PackageExportSettings
        {
            NetworkProtocolDirectory = new SettingItem
            {
                Value = ProtocolDirectory,
                Comment = _protocolDirectoryComment
            },
            NetworkProtocolServerDirectory = new SettingItem
            {
                Value = ServerOutputDirectory,
                Comment = _serverDirectoryComment
            },
            NetworkProtocolClientDirectory = new SettingItem
            {
                Value = ClientOutputDirectory,
                Comment = _clientDirectoryComment
            },
            ExportType = exportType
        };
    }
}
