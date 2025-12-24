using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
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
        RowNumber = ExtractLineNumber(token);
    }

    public string Name { get; }
    public JToken Token { get; }
    public JsonTreeNodeViewModel? Parent { get; }
    public ObservableCollection<JsonTreeNodeViewModel> Children { get; set; }

    public int? RowNumber { get; }

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

            if (Token is JObject obj)
            {
                // Check for common identifier properties first
                var identifiers = new[] { "name", "id", "alias", "title", "key" };
                var identifierProp = obj.Properties()
                    .FirstOrDefault(p => identifiers.Contains(p.Name.ToLowerInvariant()));
                
                if (identifierProp != null && identifierProp.Value is JValue identifierValue)
                {
                    // Format the value appropriately based on type
                    string? formattedValue = null;
                    if (identifierValue.Type == JTokenType.String)
                    {
                        var idValue = identifierValue.Value?.ToString();
                        if (!string.IsNullOrEmpty(idValue))
                        {
                            formattedValue = $"\"{idValue}\"";
                        }
                        // Empty string - fall through to show property list
                    }
                    else if (identifierValue.Type == JTokenType.Boolean)
                    {
                        formattedValue = (identifierValue.Value?.ToString() ?? "false").ToLowerInvariant();
                    }
                    else if (identifierValue.Type == JTokenType.Null)
                    {
                        formattedValue = "null";
                    }
                    else
                    {
                        // Numbers and other types - no quotes
                        var idValue = identifierValue.Value;
                        if (idValue != null)
                        {
                            formattedValue = idValue.ToString();
                        }
                    }
                    
                    if (formattedValue != null)
                    {
                        // Show: { name: "John Doe" } or { id: 123 } etc.
                        return $"{{ {identifierProp.Name}: {formattedValue} }}";
                    }
                }
                
                // Fallback to property preview
                var properties = obj.Properties().ToList();
                var props = properties.Take(3).Select(p => p.Name);
                var preview = string.Join(", ", props);
                if (properties.Count > 3)
                    preview += "...";
                return properties.Count > 0 ? $"{{ {preview} }}" : "{ }";
            }

            if (Token is JArray array)
            {
                return $"[{array.Count}]";
            }

            return Token.Type.ToString();
        }
    }

    public string NodeTypeIcon
    {
        get
        {
            return Token.Type switch
            {
                JTokenType.Object => "\uE8F1",      // FolderFill
                JTokenType.Array => "\uE8FD",       // BulletedList
                JTokenType.String => "\uE8E9",      // Font
                JTokenType.Integer => "\uE8EF",     // Calculator
                JTokenType.Float => "\uE8EF",       // Calculator
                JTokenType.Boolean => "\uE8FB",     // CheckboxComposite
                _ => "\uE91F"                       // Dot
            };
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

    private static int? ExtractLineNumber(JToken token)
    {
        if (token is IJsonLineInfo lineInfo && lineInfo.HasLineInfo() && lineInfo.LineNumber > 0)
        {
            return lineInfo.LineNumber;
        }

        return null;
    }
}
