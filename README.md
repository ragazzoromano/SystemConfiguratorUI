# SystemConfiguratorUI

A WPF desktop JSON editor focused on navigating and editing large `configuration.json` files. The app is built on .NET 8 with the MVVM pattern and provides a consistent experience for working with nested JSON structures.

## Features

- Tree-based navigation of objects and arrays for quick traversal of deeply nested JSON.
- Property editing for primitive values with live synchronization to the raw JSON text.
- Raw JSON editor with formatting and validation actions.
- Global search plus search-and-replace across property names and string values.
- File operations for loading, formatting, and saving JSON to disk.
- Keyboard shortcuts for common actions (see [Keyboard Shortcuts](#keyboard-shortcuts)).

## Project Intent

SystemConfiguratorUI is intended to streamline work with sprawling configuration files where accuracy and clarity are critical. The UI helps surface nested keys quickly, keeps the visual and raw representations in sync, and provides guardrails (validation and formatting) so teams can reduce mistakes when editing shared JSON assets.

## Prerequisites

- `.NET 8.0` SDK or later installed on Windows.
- Ability to run WPF applications (Windows 10 or later recommended).

## Development Notes

- The MVVM structure keeps view logic minimal—new UI behaviors should be added through commands and view models for testability.
- Validation feedback appears in the raw editor; reformatting JSON can clear minor spacing issues before commits.
- Large files are best handled by using the tree view for navigation and the search panel for targeted updates instead of manual scrolling.

### Getting Started

1. Restore dependencies:
   ```
   dotnet restore
   ```
2. Build the application:
   ```
   dotnet build
   ```
3. Run the app:
   ```
   dotnet run
   ```

### Usage Tips

- Use the tree view to locate deeply nested keys quickly, then edit values in the detail panel.
- Switch to the raw editor to make broad text edits; formatting/validation can help tidy up spacing issues.
- Save frequently when working with large files to avoid reloading or revalidating long documents.

### Keyboard Shortcuts

- **Ctrl+O**: Open a JSON file
- **Ctrl+S**: Save the current file
- **Ctrl+F**: Find next search result
