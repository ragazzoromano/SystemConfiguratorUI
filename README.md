# SystemConfiguratorUI

A WPF desktop JSON editor focused on navigating and editing large `configuration.json` files. The app is built on .NET 6 with the MVVM pattern, featuring:

- Tree-based navigation of objects and arrays.
- Property editing for primitive values with live synchronization to the raw JSON text.
- Raw JSON editor with formatting and validation actions.
- Global search plus search-and-replace across property names and string values.
- File operations for loading and saving formatted JSON.

## Project Intent

SystemConfiguratorUI is intended to streamline work with sprawling configuration files where accuracy and clarity are critical. The UI helps surface nested keys quickly, keeps the visual and raw representations in sync, and provides guardrails (validation and formatting) so teams can reduce mistakes when editing shared JSON assets.

## Notes

- Designed for `.NET 6` on Windows with WPF; ensure the SDK is installed before building.
- The MVVM structure keeps view logic minimal—new UI behaviors should be added through commands and view models for testability.
- Validation feedback appears in the raw editor; reformatting JSON can clear minor spacing issues before commits.
- Large files are best handled by using the tree view for navigation and the search panel for targeted updates instead of manual scrolling.

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
