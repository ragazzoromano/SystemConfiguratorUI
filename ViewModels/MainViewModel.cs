using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SystemConfiguratorUI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly string StateDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SystemConfiguratorUI");

    private static readonly string LastFileRecordPath = Path.Combine(StateDirectory, "last-file.txt");

    private JToken _jsonRoot = new JObject();
    private string? _currentFilePath;
    private bool _hasUnsavedChanges;
    private readonly List<JsonTreeNodeViewModel> _searchMatches = new();
    private int _searchIndex = -1;
    private bool _isUpdatingFromTree = false;

    public ObservableCollection<JsonTreeNodeViewModel> RootNodes { get; } = new();

    private JsonTreeNodeViewModel? _selectedNode;
    public JsonTreeNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            var previous = _selectedNode;

            if (SetProperty(ref _selectedNode, value))
            {
                if (previous != null && !ReferenceEquals(previous, _selectedNode))
                {
                    previous.IsSelected = false;
                }

                if (_selectedNode != null)
                {
                    _selectedNode.IsSelected = true;
                    ExpandAncestors(_selectedNode);
                }
            }
        }
    }

    private string _rawJsonText = "{}";
    public string RawJsonText
    {
        get => _rawJsonText;
        set => SetProperty(ref _rawJsonText, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                RecalculateSearchMatches();
            }
        }
    }

    private string _replaceText = string.Empty;
    public string ReplaceText
    {
        get => _replaceText;
        set => SetProperty(ref _replaceText, value);
    }

    private string _searchStatus = string.Empty;
    public string SearchStatus
    {
        get => _searchStatus;
        set => SetProperty(ref _searchStatus, value);
    }

    private bool _matchCase;
    public bool MatchCase
    {
        get => _matchCase;
        set
        {
            if (SetProperty(ref _matchCase, value))
            {
                RecalculateSearchMatches();
            }
        }
    }

    public string WindowTitle => BuildWindowTitle();

    public ICommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAsCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand FormatJsonCommand { get; }
    public ICommand ValidateJsonCommand { get; }
    public ICommand FindNextCommand { get; }
    public ICommand FindPreviousCommand { get; }
    public ICommand ReplaceCommand { get; }
    public ICommand ReplaceAllCommand { get; }
    public ICommand ExpandAllCommand { get; }
    public ICommand CollapseAllCommand { get; }
    public ICommand ExpandChildrenCommand { get; }
    public ICommand CollapseChildrenCommand { get; }

    public MainViewModel()
    {
        OpenCommand = new RelayCommand(OpenFile);
        SaveCommand = new RelayCommand(SaveFile);
        SaveAsCommand = new RelayCommand(SaveFileAs);
        ExitCommand = new RelayCommand(() => Application.Current.Shutdown());
        FormatJsonCommand = new RelayCommand(FormatJson);
        ValidateJsonCommand = new RelayCommand(ValidateRawEditor);
        FindNextCommand = new RelayCommand(FindNext);
        FindPreviousCommand = new RelayCommand(FindPrevious);
        ReplaceCommand = new RelayCommand(ReplaceCurrent);
        ReplaceAllCommand = new RelayCommand(ReplaceAll);
        ExpandAllCommand = new RelayCommand(ExpandAllNodes);
        CollapseAllCommand = new RelayCommand(CollapseAllNodes);
        ExpandChildrenCommand = new RelayCommand(ExpandChildren);
        CollapseChildrenCommand = new RelayCommand(CollapseChildren);

        if (!TryLoadLastOpenedFile())
        {
            LoadInitialDocument();
        }
    }

    private void LoadInitialDocument()
    {
        RawJsonText = _jsonRoot.ToString(Formatting.Indented);
        MarkUnsaved(false);
        RebuildTreeWithLineInfo();
    }

    private void OpenFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadFileFromPath(dialog.FileName);
        }
    }

    public bool LoadFileFromPath(string filePath)
    {
        if (!ConfirmDiscardChanges())
        {
            return false;
        }

        try
        {
            var text = File.ReadAllText(filePath);
            var parsed = ParseJsonWithLineInfo(text);
            var formatted = parsed.ToString(Formatting.Indented);
            _jsonRoot = ParseJsonWithLineInfo(formatted);
            _currentFilePath = filePath;
            RawJsonText = formatted;
            RebuildTree();
            MarkUnsaved(false);
            SaveLastFilePath(_currentFilePath);
            return true;
        }
        catch (JsonReaderException ex)
        {
            MessageBox.Show($"Could not parse JSON: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Could not read file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return false;
    }

    private void SaveFile()
    {
        if (string.IsNullOrWhiteSpace(_currentFilePath))
        {
            SaveFileAs();
            return;
        }

        TryWriteFile(_currentFilePath);
    }

    private void SaveFileAs()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = Path.GetFileName(_currentFilePath) ?? "configuration.json"
        };

        if (dialog.ShowDialog() == true)
        {
            _currentFilePath = dialog.FileName;
            TryWriteFile(dialog.FileName);
            SaveLastFilePath(_currentFilePath);
        }
    }

    private void TryWriteFile(string path)
    {
        try
        {
            FormatJson();
            File.WriteAllText(path, RawJsonText, Encoding.UTF8);
            MarkUnsaved(false);
            SaveLastFilePath(path);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Could not save file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (JsonReaderException ex)
        {
            MessageBox.Show($"JSON is invalid: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool TryLoadLastOpenedFile()
    {
        var lastFilePath = ReadLastFilePath();
        if (string.IsNullOrWhiteSpace(lastFilePath) || !File.Exists(lastFilePath))
        {
            return false;
        }

        return LoadFileFromPath(lastFilePath);
    }

    private static string? ReadLastFilePath()
    {
        try
        {
            if (File.Exists(LastFileRecordPath))
            {
                var content = File.ReadAllText(LastFileRecordPath).Trim();
                return string.IsNullOrWhiteSpace(content) ? null : content;
            }
        }
        catch
        {
            // Ignore errors reading persisted state and fall back to default document.
        }

        return null;
    }

    private static void SaveLastFilePath(string? path)
    {
        try
        {
            Directory.CreateDirectory(StateDirectory);

            if (string.IsNullOrWhiteSpace(path))
            {
                if (File.Exists(LastFileRecordPath))
                {
                    File.Delete(LastFileRecordPath);
                }

                return;
            }

            File.WriteAllText(LastFileRecordPath, path);
        }
        catch
        {
            // Ignore errors persisting the last file path; do not block app usage.
        }
    }

    public bool ConfirmClose() => ConfirmDiscardChanges();

    private bool ConfirmDiscardChanges()
    {
        if (!_hasUnsavedChanges)
        {
            return true;
        }

        var result = MessageBox.Show("You have unsaved changes. Continue?", "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }

    private void FormatJson()
    {
        try
        {
            var formatted = ParseJsonWithLineInfo(RawJsonText).ToString(Formatting.Indented);
            RawJsonText = formatted;
            _jsonRoot = ParseJsonWithLineInfo(formatted);
            RebuildTree();
            MarkUnsaved(true);
        }
        catch (JsonReaderException ex)
        {
            MessageBox.Show($"JSON is invalid: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ValidateRawEditor()
    {
        try
        {
            var parsed = ParseJsonWithLineInfo(RawJsonText);
            var formatted = parsed.ToString(Formatting.Indented);
            RawJsonText = formatted;
            _jsonRoot = ParseJsonWithLineInfo(formatted);
            RebuildTree();
            MarkUnsaved(true);
            MessageBox.Show("JSON is valid.", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (JsonReaderException ex)
        {
            MessageBox.Show($"JSON is invalid: {ex.Message}", "Validation", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RebuildTree()
    {
        RootNodes.Clear();
        foreach (var child in BuildNodes(_jsonRoot, null))
        {
            RootNodes.Add(child);
        }

        foreach (var node in RootNodes)
        {
            node.ValueChanged = HandleNodeUpdated;
        }

        RecalculateSearchMatches();
        OnPropertyChanged(nameof(WindowTitle));
    }

    private void RebuildTreeWithLineInfo()
    {
        _jsonRoot = ParseJsonWithLineInfo(RawJsonText);
        RebuildTree();
    }

    private IEnumerable<JsonTreeNodeViewModel> BuildNodes(JToken token, JsonTreeNodeViewModel? parent)
    {
        if (token is JObject obj)
        {
            foreach (var property in obj.Properties())
            {
                var node = new JsonTreeNodeViewModel(property.Name, property.Value, parent)
                {
                    ValueChanged = HandleNodeUpdated
                };
                node.Children = new ObservableCollection<JsonTreeNodeViewModel>(BuildNodes(property.Value, node));
                yield return node;
            }
        }
        else if (token is JArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                var name = $"[{i}]";
                var item = array[i];
                var node = new JsonTreeNodeViewModel(name, item, parent)
                {
                    ValueChanged = HandleNodeUpdated
                };
                node.Children = new ObservableCollection<JsonTreeNodeViewModel>(BuildNodes(item, node));
                yield return node;
            }
        }
        else
        {
            yield return new JsonTreeNodeViewModel(parent?.Name ?? "root", token, parent)
            {
                ValueChanged = HandleNodeUpdated
            };
        }
    }

    private void HandleNodeUpdated()
    {
        if (_isUpdatingFromTree) return;
        
        _isUpdatingFromTree = true;
        try
        {
            RawJsonText = _jsonRoot.ToString(Formatting.Indented);
            MarkUnsaved(true);
            RecalculateSearchMatches();
        }
        finally
        {
            _isUpdatingFromTree = false;
        }
    }

    private static void ExpandAncestors(JsonTreeNodeViewModel node)
    {
        var current = node.Parent;
        while (current != null)
        {
            current.IsExpanded = true;
            current = current.Parent;
        }
    }

    private void MarkUnsaved(bool value)
    {
        _hasUnsavedChanges = value;
        OnPropertyChanged(nameof(WindowTitle));
    }

    private static JToken ParseJsonWithLineInfo(string text)
    {
        return JToken.Parse(text, new JsonLoadSettings
        {
            LineInfoHandling = LineInfoHandling.Load
        });
    }

    private string BuildWindowTitle()
    {
        var fileName = string.IsNullOrWhiteSpace(_currentFilePath)
            ? "SystemConfiguratorUI"
            : $"{_currentFilePath} - SystemConfiguratorUI";

        if (_hasUnsavedChanges)
        {
            fileName += "*";
        }

        return fileName;
    }

    private void RecalculateSearchMatches()
    {
        _searchMatches.Clear();
        _searchIndex = -1;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SearchStatus = string.Empty;
            return;
        }

        var comparer = MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        foreach (var node in RootNodes.SelectMany(r => r.Flatten()))
        {
            if (node.Matches(SearchText, comparer))
            {
                _searchMatches.Add(node);
            }
        }

        UpdateSearchStatus();
    }

    private void UpdateSearchStatus()
    {
        if (_searchMatches.Count == 0)
        {
            SearchStatus = "0/0";
        }
        else if (_searchIndex < 0)
        {
            SearchStatus = $"0/{_searchMatches.Count}";
        }
        else
        {
            SearchStatus = $"{_searchIndex + 1}/{_searchMatches.Count}";
        }
    }

    private void FindNext()
    {
        if (_searchMatches.Count == 0)
        {
            return;
        }

        if (_searchIndex < 0)
        {
            _searchIndex = 0;
        }
        else
        {
            _searchIndex = (_searchIndex + 1) % _searchMatches.Count;
        }
        
        SelectedNode = _searchMatches[_searchIndex];
        UpdateSearchStatus();
    }

    private void FindPrevious()
    {
        if (_searchMatches.Count == 0)
        {
            return;
        }

        if (_searchIndex < 0)
        {
            _searchIndex = _searchMatches.Count - 1;
        }
        else
        {
            _searchIndex = (_searchIndex - 1 + _searchMatches.Count) % _searchMatches.Count;
        }
        
        SelectedNode = _searchMatches[_searchIndex];
        UpdateSearchStatus();
    }

    private void ReplaceCurrent()
    {
        if (_searchMatches.Count == 0 || _searchIndex < 0 || _searchIndex >= _searchMatches.Count)
        {
            return;
        }

        var comparer = MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var node = _searchMatches[_searchIndex];
        if (node.ReplaceValue(SearchText, ReplaceText, comparer))
        {
            HandleNodeUpdated();
        }
    }

    private void ReplaceAll()
    {
        if (string.IsNullOrEmpty(SearchText))
        {
            return;
        }

        var comparer = MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        bool changed = false;

        foreach (var node in RootNodes.SelectMany(n => n.Flatten()))
        {
            if (node.ReplaceValue(SearchText, ReplaceText, comparer))
            {
                changed = true;
            }
        }

        if (changed)
        {
            HandleNodeUpdated();
        }
    }

    private void ExpandAllNodes()
    {
        foreach (var node in RootNodes.SelectMany(n => n.Flatten()))
        {
            node.IsExpanded = true;
        }
    }

    private void CollapseAllNodes()
    {
        foreach (var node in RootNodes.SelectMany(n => n.Flatten()))
        {
            node.IsExpanded = false;
        }
    }

    private void ExpandChildren()
    {
        if (SelectedNode == null)
        {
            return;
        }

        SelectedNode.IsExpanded = true;
        foreach (var child in SelectedNode.Children.SelectMany(c => c.Flatten()))
        {
            child.IsExpanded = true;
        }
    }

    private void CollapseChildren()
    {
        if (SelectedNode == null)
        {
            return;
        }

        foreach (var child in SelectedNode.Children.SelectMany(c => c.Flatten()))
        {
            child.IsExpanded = false;
        }
    }
}
