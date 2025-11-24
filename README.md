# SystemConfiguratorUI

A WPF desktop JSON editor focused on navigating and editing large `configuration.json` files. The app is built on .NET 6 with the MVVM pattern, featuring:

- Tree-based navigation of objects and arrays.
- Property editing for primitive values with live synchronization to the raw JSON text.
- Raw JSON editor with formatting and validation actions.
- Global search plus search-and-replace across property names and string values.
- File operations for loading and saving formatted JSON.

### Building

```
dotnet restore
dotnet build
dotnet run
```

### Keyboard Shortcuts

- **Ctrl+O**: Open a JSON file
- **Ctrl+S**: Save the current file
- **Ctrl+F**: Find next search result
