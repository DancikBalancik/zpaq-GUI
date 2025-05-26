# ZPAQ GUI for Windows

A modern graphical user interface for the [zpaq](http://mattmahoney.net/dc/zpaq.html) archiver, providing easy access to all advanced compression, extraction, and listing options on Windows.

## Features

- **Full zpaq support:** Exposes all major zpaq command-line options in a user-friendly Settings dialog.
- **Multi-threaded compression/extraction:** Select CPU core/thread count for optimal performance.
- **Advanced options:** Configure fragment size, streaming mode, method string, file attribute handling, forced operations, summary, repack, index, version selection, and more.
- **Encryption:** Enable password protection for archives.
- **Robust error handling:** User-friendly error messages and warnings for risky settings (e.g., Ultra+encryption thread limits).
- **Modern UI:** Material Design styling, drag-and-drop, progress bars, and responsive feedback.

## Requirements

- Windows 10/11
- [.NET 7.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
- `zpaq.exe` (included or place in the same directory as the GUI)

## Building

1. Open a terminal in the `GUI` directory.
2. Run:
   ```powershell
   dotnet build ZpaqGUI.csproj --configuration Release
   ```
3. The output will be in `bin/Release/net7.0-windows/`.

## Usage

1. Place `zpaq.exe` in the same folder as `ZpaqGUI.exe`.
2. Run `ZpaqGUI.exe`.
3. Use the gear icon to open Settings and configure advanced options.
4. Add files/folders, set compression level, enable encryption if desired, and create or extract archives.

## Settings Dialog Options

- **Threads:** Number of CPU threads to use.
- **No Attributes:** Ignore file attributes/permissions.
- **Force:** Force add/extract/list.
- **Test:** Test extraction only (no write).
- **Summary:** Show top N files or fragment IDs.
- **Fragment Size:** Set deduplication fragment size (2^N KiB).
- **Streaming:** Enable streaming mode (no deduplication).
- **Method String:** Advanced compression method string.
- **Index File:** Use an external index file.
- **Repack:** Repack to another archive (with optional password).
- **All Versions:** Extract/list all versions (digits).
- **To:** Rename extracted files/folders.
- **Not/Only:** Exclude/include patterns.

## Troubleshooting

- If you see errors about missing controls or settings, ensure you have built the project after any changes to `Settings.settings` or XAML files.
- For best results, always use the latest zpaq release and .NET runtime.

## Known Issues

- **Thread Limit:** Compression methods have their own thread limit. If you go above it, out-of-memory errors will be displayed.
- **Settings Persistence:** If you manually edit `Settings.settings` or XAML files, you must rebuild the project to regenerate code-behind and settings accessors.
- **File Attribute Handling:** The `-noattributes` option may not preserve all NTFS metadata. Use with caution if you require exact restoration.
- **UI Responsiveness:** Very large archives or file lists may cause the UI to become temporarily unresponsive during zpaq operations.
- **zpaq.exe Location:** The GUI requires `zpaq.exe` in the same directory. If not found, you will see a warning at startup.
- **.NET Version:** Only .NET 7.0 is officially supported. Other versions may not work as expected.
- **Error Reporting:** Some zpaq errors may only appear in the status bar or as a message box. Check the log file for details if an operation fails unexpectedly.

## License

See `COPYING` for license information.

## Credits

- [Matt Mahoney](http://mattmahoney.net/dc/zpaq.html) for zpaq
- Material Design in XAML Toolkit

---

For more information, see the zpaq documentation (`zpaq.pod`) or visit the [zpaq homepage](http://mattmahoney.net/dc/zpaq.html).
