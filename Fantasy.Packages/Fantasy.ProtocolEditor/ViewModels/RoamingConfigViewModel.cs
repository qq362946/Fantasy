using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fantasy.ProtocolEditor.Models;
using Fantasy.ProtocolEditor.Services;

namespace Fantasy.ProtocolEditor.ViewModels;

/// <summary>
/// RoamingType.Config 配置编辑 ViewModel
/// </summary>
public partial class RoamingConfigViewModel : ViewModelBase
{
    /// <summary>
    /// 文件路径
    /// </summary>
    [ObservableProperty]
    private string _filePath = string.Empty;

    /// <summary>
    /// Roaming 类型条目列表（所有条目）
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<RoamingTypeEntry> _entries = new();

    /// <summary>
    /// 过滤后的条目列表（用于显示）
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<RoamingTypeEntry> _filteredEntries = new();

    /// <summary>
    /// 搜索文本
    /// </summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// 新条目的名称
    /// </summary>
    [ObservableProperty]
    private string _newEntryName = string.Empty;

    /// <summary>
    /// 新条目的值
    /// </summary>
    [ObservableProperty]
    private int _newEntryValue = 10000;

    /// <summary>
    /// 错误信息
    /// </summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// 是否已修改
    /// </summary>
    [ObservableProperty]
    private bool _isModified;

    /// <summary>
    /// 当搜索文本改变时触发
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        FilterEntries();
    }

    /// <summary>
    /// 过滤条目
    /// </summary>
    private void FilterEntries()
    {
        FilteredEntries.Clear();

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            // 没有搜索文本，显示所有条目
            foreach (var entry in Entries)
            {
                FilteredEntries.Add(entry);
            }
        }
        else
        {
            // 根据搜索文本过滤（名称或值）
            var searchLower = SearchText.ToLower();
            foreach (var entry in Entries)
            {
                if (entry.Name.ToLower().Contains(searchLower) ||
                    entry.Value.ToString().Contains(searchLower))
                {
                    FilteredEntries.Add(entry);
                }
            }
        }
    }

    /// <summary>
    /// 加载配置文件
    /// </summary>
    public void LoadConfig(string filePath)
    {
        FilePath = filePath;
        ErrorMessage = string.Empty;

        try
        {
            var entries = ConfigParser.ParseRoamingConfig(filePath);
            Entries.Clear();

            foreach (var entry in entries)
            {
                Entries.Add(entry);
            }

            IsModified = false;

            // 自动计算下一个可用的值
            if (Entries.Any())
            {
                NewEntryValue = Entries.Max(e => e.Value) + 1;
            }
            else
            {
                NewEntryValue = 10000;
            }

            // 更新过滤后的列表
            FilterEntries();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载配置文件失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 保存配置文件
    /// </summary>
    [RelayCommand]
    private void SaveConfig()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            ErrorMessage = "文件路径为空";
            return;
        }

        ErrorMessage = string.Empty;

        // 验证重复值
        var duplicateValues = ConfigParser.FindDuplicateValues(Entries);
        if (duplicateValues.Any())
        {
            var duplicateInfo = string.Join(", ", duplicateValues.Select(kv =>
                $"值 {kv.Key} 重复: {string.Join(", ", kv.Value)}"));
            ErrorMessage = $"发现重复的值: {duplicateInfo}";
            return;
        }

        // 验证重复名称
        var duplicateNames = ConfigParser.FindDuplicateNames(Entries);
        if (duplicateNames.Any())
        {
            var duplicateInfo = string.Join(", ", duplicateNames.Select(kv =>
                $"名称 {kv.Key} 重复"));
            ErrorMessage = $"发现重复的名称: {duplicateInfo}";
            return;
        }

        // 验证所有条目
        var invalidEntries = Entries.Where(e => !e.IsValid()).ToList();
        if (invalidEntries.Any())
        {
            ErrorMessage = $"存在无效条目: {string.Join(", ", invalidEntries.Select(e => e.Name))}";
            return;
        }

        try
        {
            ConfigParser.SaveRoamingConfig(FilePath, Entries);
            IsModified = false;
            ErrorMessage = "保存成功!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"保存失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 添加新条目
    /// </summary>
    [RelayCommand]
    private void AddEntry()
    {
        ErrorMessage = string.Empty;

        // 验证名称
        if (string.IsNullOrWhiteSpace(NewEntryName))
        {
            ErrorMessage = "名称不能为空";
            return;
        }

        if (!NewEntryName.EndsWith("RoamingType"))
        {
            ErrorMessage = "名称必须以 RoamingType 结尾";
            return;
        }

        // 验证值
        if (NewEntryValue < 10000)
        {
            ErrorMessage = "值必须大于等于 10000 (10000以内框架预留)";
            return;
        }

        // 检查名称是否已存在
        if (Entries.Any(e => string.Equals(e.Name, NewEntryName, StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = $"名称 {NewEntryName} 已存在";
            return;
        }

        // 检查值是否已存在
        if (Entries.Any(e => e.Value == NewEntryValue))
        {
            ErrorMessage = $"值 {NewEntryValue} 已存在";
            return;
        }

        // 添加新条目
        var newEntry = new RoamingTypeEntry
        {
            Name = NewEntryName,
            Value = NewEntryValue
        };

        Entries.Add(newEntry);
        IsModified = true;

        // 清空输入框并自动递增值
        NewEntryName = string.Empty;
        NewEntryValue = Entries.Max(e => e.Value) + 1;

        // 更新过滤后的列表
        FilterEntries();
    }

    /// <summary>
    /// 删除条目
    /// </summary>
    [RelayCommand]
    private void DeleteEntry(RoamingTypeEntry entry)
    {
        if (entry != null && Entries.Contains(entry))
        {
            Entries.Remove(entry);
            IsModified = true;
            ErrorMessage = string.Empty;

            // 更新过滤后的列表
            FilterEntries();
        }
    }

    /// <summary>
    /// 编辑条目
    /// </summary>
    [RelayCommand]
    private void EditEntry(RoamingTypeEntry entry)
    {
        if (entry != null)
        {
            // 取消其他条目的编辑状态
            foreach (var e in Entries.Where(e => e != entry))
            {
                e.IsEditing = false;
            }

            entry.IsEditing = true;
        }
    }

    /// <summary>
    /// 确认编辑
    /// </summary>
    [RelayCommand]
    private void ConfirmEdit(RoamingTypeEntry entry)
    {
        if (entry == null)
            return;

        ErrorMessage = string.Empty;

        // 验证条目
        var errorMsg = entry.GetErrorMessage();
        if (errorMsg != null)
        {
            ErrorMessage = errorMsg;
            return;
        }

        // 检查名称重复（排除自己）
        if (Entries.Any(e => e != entry && string.Equals(e.Name, entry.Name, StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = $"名称 {entry.Name} 已存在";
            return;
        }

        // 检查值重复（排除自己）
        if (Entries.Any(e => e != entry && e.Value == entry.Value))
        {
            ErrorMessage = $"值 {entry.Value} 已存在";
            return;
        }

        entry.IsEditing = false;
        IsModified = true;
    }

    /// <summary>
    /// 取消编辑
    /// </summary>
    [RelayCommand]
    private void CancelEdit(RoamingTypeEntry entry)
    {
        if (entry != null)
        {
            entry.IsEditing = false;
        }
    }
}
