using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json.Linq;

namespace SystemConfiguratorUI.ViewModels;

public class JsonTreeNodeViewModel : ObservableObject
{
    public JsonTreeNodeViewModel(string name, JToken token, JsonTreeNodeViewModel? parent)
    {
        Name = name;
        Token = token;
        Parent = parent;
        Children = new ObservableCollection<JsonTreeNodeViewModel>();
    }

    public string Name { get; }
    public JToken Token { get; }
    public JsonTreeNodeViewModel? Parent { get; }
    public ObservableCollection<JsonTreeNodeViewModel> Children { get; set; }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public Action? ValueChanged { get; set; }

    public string NodeType => Token.Type.ToString();

    public string DisplayName => Name;

    public string DisplayValue
    {
        get
        {
            if (Token is JValue value)
            {
                return value.Value?.ToString() ?? "null";
            }

            if (Token is JObject)
            {
                return "{ }";
            }

            if (Token is JArray array)
            {
                return $"[{array.Count}]";
            }

            return Token.Type.ToString();
        }
    }

    public bool IsPrimitive => Token is JValue;
    public bool IsBoolean => Token.Type == JTokenType.Boolean;
    public bool IsString => Token.Type == JTokenType.String;
    public bool IsNumber => Token.Type == JTokenType.Integer || Token.Type == JTokenType.Float;

    public string EditableValue
    {
        get => Token is JValue value ? value.Value?.ToString() ?? string.Empty : string.Empty;
        set
        {
            if (!IsPrimitive || IsBoolean)
            {
                return;
            }

            if (Token is JValue jValue)
            {
                object? newValue = value;
                if (IsNumber)
                {
                    if (double.TryParse(value, out var number))
                    {
                        newValue = number;
                    }
                    else
                    {
                        return;
                    }
                }

                jValue.Value = newValue;
                OnPropertyChanged(nameof(DisplayValue));
                ValueChanged?.Invoke();
            }
        }
    }

    public bool? BooleanValue
    {
        get => Token.Type == JTokenType.Boolean && Token is JValue value ? value.Value<bool?>() : null;
        set
        {
            if (Token is JValue jValue && Token.Type == JTokenType.Boolean && value.HasValue)
            {
                jValue.Value = value.Value;
                OnPropertyChanged(nameof(DisplayValue));
                ValueChanged?.Invoke();
            }
        }
    }

    public IEnumerable<JsonTreeNodeViewModel> Flatten()
    {
        yield return this;
        foreach (var child in Children)
        {
            foreach (var nested in child.Flatten())
            {
                yield return nested;
            }
        }
    }

    public bool Matches(string term, StringComparison comparison)
    {
        if (Name.IndexOf(term, comparison) >= 0)
        {
            return true;
        }

        if (IsString && Token is JValue value)
        {
            var text = value.Value?.ToString() ?? string.Empty;
            return text.IndexOf(term, comparison) >= 0;
        }

        return false;
    }

    public bool ReplaceValue(string find, string replace, StringComparison comparison)
    {
        if (!IsString || Token is not JValue value)
        {
            return false;
        }

        var text = value.Value?.ToString() ?? string.Empty;
        if (text.IndexOf(find, comparison) < 0)
        {
            return false;
        }

        var newValue = Replace(text, find, replace, comparison);
        value.Value = newValue;
        OnPropertyChanged(nameof(DisplayValue));
        ValueChanged?.Invoke();
        return true;
    }

    private static string Replace(string source, string find, string replace, StringComparison comparison)
    {
        if (string.IsNullOrEmpty(find))
        {
            return source;
        }

        var builder = new StringBuilder();
        int currentIndex = 0;
        int matchIndex = source.IndexOf(find, comparison);

        while (matchIndex >= 0)
        {
            builder.Append(source, currentIndex, matchIndex - currentIndex);
            builder.Append(replace);
            currentIndex = matchIndex + find.Length;
            matchIndex = source.IndexOf(find, currentIndex, comparison);
        }

        builder.Append(source, currentIndex, source.Length - currentIndex);
        return builder.ToString();
    }
}
