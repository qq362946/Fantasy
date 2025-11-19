using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fantasy.ProtocolEditor.Models;
using Fantasy.ProtocolEditor.Services;

namespace Fantasy.ProtocolEditor.ViewModels;

public partial class ExportSettingsViewModel : ViewModelBase
{
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
    public void LoadSettings(WorkspaceConfig config)
    {
        WorkspacePath = config.WorkspacePath;
        ServerOutputDirectory = config.ServerOutputDirectory;
        ClientOutputDirectory = config.ClientOutputDirectory;
        ExportToServer = config.ExportToServer;
        ExportToClient = config.ExportToClient;

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
            var config = ConfigService.LoadConfig() ?? new WorkspaceConfig();

            // 更新工作区路径（确保不丢失）
            if (!string.IsNullOrEmpty(WorkspacePath))
            {
                config.WorkspacePath = WorkspacePath;
            }

            // 更新导出配置
            config.ServerOutputDirectory = ServerOutputDirectory;
            config.ClientOutputDirectory = ClientOutputDirectory;
            config.ExportToServer = ExportToServer;
            config.ExportToClient = ExportToClient;

            // 保存配置
            ConfigService.SaveConfig(config);

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

        IsValid = true;
        ValidationMessage = "✅ 配置有效，可以执行导出";
    }

    /// <summary>
    /// 当属性改变时自动验证并标记为已修改
    /// </summary>
    partial void OnServerOutputDirectoryChanged(string value)
    {
        IsModified = true;
        SaveMessage = string.Empty;
        ValidateSettings();
    }

    partial void OnClientOutputDirectoryChanged(string value)
    {
        IsModified = true;
        SaveMessage = string.Empty;
        ValidateSettings();
    }

    partial void OnExportToServerChanged(bool value)
    {
        IsModified = true;
        SaveMessage = string.Empty;
        ValidateSettings();
    }

    partial void OnExportToClientChanged(bool value)
    {
        IsModified = true;
        SaveMessage = string.Empty;
        ValidateSettings();
    }

    partial void OnWorkspacePathChanged(string value)
    {
        ValidateSettings();
    }
}
