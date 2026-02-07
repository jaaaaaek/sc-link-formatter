# Implementation Plan: Avalonia UI for SoundCloud Downloader

## Context

This project transforms the existing console-based SoundCloud downloader into a modern, cross-platform desktop application using Avalonia UI. The current implementation ([Program.cs](c:\Users\jhiba\source\repos\sc-link-formatter\LinkFormatter\Program.cs)) generates yt-dlp commands but requires manual copy-paste execution. Users must manage URLs through text files (NewDownloads.txt, ExistingDownloads.txt) and configure auth tokens via App.config.

The new GUI will automate the entire workflow: users can paste URLs directly, select output formats (MP3 or WAV), view real-time download progress, manage a download queue, and browse downloaded files—all from a modern, dark-themed interface with Claude orange (#D97757) accents.

**Key Requirements:**
- Avalonia UI with dark theme + orange accents
- Automated yt-dlp execution with real-time progress
- Settings persistence (output folder, auth token, preferences)
- Download queue with status tracking (Pending → Downloading → Completed/Failed)
- URL validation and duplicate prevention
- Pause/cancel functionality
- First-run welcome screen with auth token setup
- Single .exe distribution

## Implementation Approach

### Architecture: MVVM Pattern with Service Layer

**Project Structure:**
```
LinkFormatter/
├── Models/              (Data models: DownloadItem, AppSettings, Enums)
├── Services/            (Business logic: DownloadService, SettingsService, UrlValidator, FileService, FFmpegService)
├── ViewModels/          (Presentation logic: MainWindowViewModel, WelcomeViewModel, etc.)
├── Views/               (UI: MainWindow.axaml, WelcomeView.axaml, etc.)
├── Styles/              (DarkTheme.axaml with Claude orange)
└── Music/               (yt-dlp.exe bundled; ffmpeg.exe auto-downloaded on first run)
```

### Core Components

#### 1. **Settings Management** (JSON-based)
- **Replace App.config** with `appsettings.json` stored in `%APPDATA%\SoundCloudDownloader\`
- **Schema:**
  ```json
  {
    "OutputFolder": "C:\\Users\\...\\Music",
    "SoundCloudToken": "",
    "PreferredFormat": "MP3",
    "WindowWidth": 1200,
    "WindowHeight": 800,
    "DownloadedUrls": ["url1", "url2", ...],
    "IsFirstRun": true
  }
  ```
- **Service:** `SettingsService.cs` handles JSON serialization, atomic writes, and defaults
- **Migration:** Replace ExistingDownloads.txt with `DownloadedUrls` array in JSON

#### 2. **Download Service** (Process-based yt-dlp execution)
- **Execution Strategy:**
  - Spawn `yt-dlp.exe` process from Music folder
  - Redirect StandardOutput/StandardError for progress tracking
  - Parse output for download percentage: `[download]  45.2% of ...`
  - Report progress via `IProgress<DownloadProgress>`
  - Handle cancellation via `CancellationToken` + `Process.Kill(entireProcessTree: true)`

- **Command Generation:**
  ```csharp
  // MP3 (no auth required)
  yt-dlp -f ba --extract-audio --audio-format mp3 {url} -o "{outputFolder}\%(title)s.%(ext)s" --newline --extractor-retries 10 --retry-sleep extractor:300

  // WAV (requires auth token)
  yt-dlp -f ba --extract-audio --audio-format wav {url} -o "{outputFolder}\%(title)s.%(ext)s" --add-header "Authorization: OAuth {token}" --newline --extractor-retries 10 --retry-sleep extractor:300
  ```

- **Queue Processing:** Sequential downloads (one at a time) to avoid overwhelming system
- **State Tracking:** Dictionary of active Process instances keyed by download Guid

#### 3. **URL Validation**
- **Validator Service:** `UrlValidator.cs` checks:
  - Valid URI format
  - Domain is `soundcloud.com`
  - Skips URLs with exactly 3 slashes (malformed)
  - Skips URLs containing `/you/` (user profile pages)
  - Regex: `^https?://soundcloud\.com/[\w-]+/[\w-]+` or `^https?://soundcloud\.com/[\w-]+/sets/[\w-]+`

#### 4. **FFmpeg Service** (Auto-download on first run)
- **Why:** FFmpeg.exe is ~147MB and exceeds GitHub's 100MB file size limit
- **Solution:** Download automatically on first run from trusted source
- **FFmpeg Service:** `FFmpegService.cs` handles:
  - Check if ffmpeg.exe exists in Music folder
  - If missing, download from https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip (~80MB)
  - Extract ffmpeg.exe from the archive to Music folder
  - Report download progress via `IProgress<DownloadProgress>`
  - Handle errors gracefully with retry options
- **First-Run Integration:** Welcome screen checks for FFmpeg and downloads if needed
- **Fallback:** If download fails, show error with manual download link

#### 5. **Download Queue System**
- **Model:** `DownloadItem.cs`
  ```csharp
  public class DownloadItem {
      public Guid Id { get; set; }
      public string Url { get; set; }
      public DownloadStatus Status { get; set; }  // Pending, Downloading, Completed, Failed, Cancelled
      public AudioFormat Format { get; set; }     // MP3, WAV
      public double Progress { get; set; }        // 0-100
      public string ErrorMessage { get; set; }
      public string OutputFileName { get; set; }
  }
  ```
- **ViewModel:** `DownloadQueueViewModel.cs` manages `ObservableCollection<DownloadItemViewModel>`
- **Operations:** Add, Remove, Reorder, Clear Completed

#### 6. **UI Layout** (1200x800 main window)

**Structure:**
```
┌─────────────────────────────────────────────────┐
│  SETTINGS (Left Sidebar 250px)                  │
│  - Output Folder selection                      │
│  - Auth Token input                             │
│  - Default Format (MP3/WAV)                     │
├─────────────────────────────────────────────────┤
│  MAIN CONTENT (Right Panel)                     │
│  ┌───────────────────────────────────────────┐  │
│  │ URL INPUT (paste, format selector, add)  │  │
│  ├───────────────────────────────────────────┤  │
│  │ DOWNLOAD QUEUE (list with status icons)  │  │
│  │ [Start] [Pause] [Stop All] [Clear Done]  │  │
│  ├───────────────────────────────────────────┤  │
│  │ PROGRESS CONSOLE (scrolling output)      │  │
│  ├───────────────────────────────────────────┤  │
│  │ DOWNLOADED FILES (list with Open btn)    │  │
│  └───────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

**First-Run Welcome Dialog:**
- Modal dialog on `IsFirstRun == true`
- **FFmpeg check and download:** Automatically check for FFmpeg and download if missing (with progress bar)
- Auth token input (optional)
- Link to README instructions
- Checkbox: "I only want MP3 downloads (no token needed)"
- Show download progress for FFmpeg if needed (~80MB, may take 1-2 minutes)

**Theme Colors:**
- Background: `#1E1E1E`
- Surface: `#2D2D2D`
- Primary: `#D97757` (Claude orange)
- Text: `#FFFFFF`
- Success: `#4CAF50`, Error: `#F44336`

### Data Flow

**Download Workflow:**
1. User pastes URL → `UrlInputViewModel` validates → Creates `DownloadItem` (Status: Pending)
2. Item added to `DownloadQueueViewModel.Items` (ObservableCollection)
3. User clicks "Start Downloads" → `MainWindowViewModel` processes queue sequentially
4. For each item:
   - Set Status = Downloading
   - `DownloadService.DownloadAsync()` spawns yt-dlp process
   - Parse stdout → Update `Progress` and `ProgressConsoleViewModel`
   - On completion: Status = Completed, add URL to `DownloadedUrls`, save settings, refresh file list
5. Duplicate prevention: Check if URL in `DownloadedUrls` before adding to queue

**Settings Persistence:**
- Load on app start → populate ViewModels
- Save on settings change (debounced 500ms)
- Save on app close (window state)
- Save after each successful download (append URL)

## Critical Files to Create/Modify

### Phase 1: Foundation (Models & Services)

1. **LinkFormatter.csproj** - Add Avalonia NuGet packages
   ```xml
   <PackageReference Include="Avalonia" Version="11.2.0" />
   <PackageReference Include="Avalonia.Desktop" Version="11.2.0" />
   <PackageReference Include="Avalonia.Themes.Fluent" Version="11.2.0" />
   <PackageReference Include="System.Text.Json" Version="9.0.0" />
   <OutputType>WinExe</OutputType>  <!-- Change from Exe -->
   ```

2. **Models/DownloadItem.cs** - Core data model for downloads
3. **Models/DownloadStatus.cs** - Enum: Pending, Downloading, Completed, Failed, Cancelled
4. **Models/AudioFormat.cs** - Enum: MP3, WAV
5. **Models/AppSettings.cs** - Settings schema for JSON persistence

6. **Services/ISettingsService.cs** & **Services/SettingsService.cs**
   - Load/save JSON from `%APPDATA%\SoundCloudDownloader\appsettings.json`
   - Use System.Text.Json for serialization
   - Atomic writes (write to temp, then move)

7. **Services/IUrlValidator.cs** & **Services/UrlValidator.cs**
   - Validate SoundCloud URLs via regex
   - Implement skip logic (3 slashes, /you/)

8. **Services/IFFmpegService.cs** & **Services/FFmpegService.cs** ⭐ CRITICAL
   - Check if ffmpeg.exe exists in Music folder
   - Download FFmpeg from https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip (~80MB)
   - Extract ffmpeg.exe from ZIP archive
   - Report download progress via `IProgress<DownloadProgress>`
   - Handle download errors with fallback options
   - Called during first-run welcome screen

9. **Services/IDownloadService.cs** & **Services/DownloadService.cs** ⭐ CRITICAL
   - Execute yt-dlp.exe as Process
   - Build command arguments based on format (MP3 vs WAV)
   - Parse stdout for progress: `\[download\]\s+(\d+\.?\d*)%`
   - Report via `IProgress<DownloadProgress>`
   - Handle cancellation via `Process.Kill(entireProcessTree: true)`
   - Track active processes in `Dictionary<Guid, Process>`

10. **Services/IFileService.cs** & **Services/FileService.cs**
    - Get downloaded files from output folder
    - Open file location: `Process.Start("explorer.exe", $"/select,\"{path}\"")`

### Phase 2: ViewModels

11. **ViewModels/ViewModelBase.cs** - INotifyPropertyChanged base class
12. **ViewModels/WelcomeViewModel.cs** - First-run setup, FFmpeg check/download, auth token input
13. **ViewModels/SettingsPanelViewModel.cs** - Output folder, auth token, format selection
14. **ViewModels/UrlInputViewModel.cs** - URL paste, validation, add to queue
15. **ViewModels/DownloadItemViewModel.cs** - Wrapper for DownloadItem with UI properties
16. **ViewModels/DownloadQueueViewModel.cs** - ObservableCollection of items, queue management
17. **ViewModels/ProgressConsoleViewModel.cs** - Scrolling console output (max 500 lines)
18. **ViewModels/FilesListViewModel.cs** - Downloaded files browser
19. **ViewModels/MainWindowViewModel.cs** ⭐ CRITICAL - Orchestrates all child ViewModels, processes download queue

### Phase 3: Views

20. **App.axaml** & **App.axaml.cs** - Avalonia application root, DI setup
21. **Styles/DarkTheme.axaml** - Dark theme with Claude orange (#D97757)

22. **Views/WelcomeView.axaml** - First-run welcome dialog with FFmpeg download progress
23. **Views/SettingsPanelView.axaml** - Left sidebar settings panel
24. **Views/UrlInputView.axaml** - URL input section
25. **Views/DownloadQueueView.axaml** - Queue list with controls
26. **Views/ProgressConsoleView.axaml** - Console output viewer
27. **Views/FilesListView.axaml** - Downloaded files list
28. **Views/MainWindow.axaml** ⭐ CRITICAL - Main window layout composing all views

29. **Program.cs** - Replace Main() with Avalonia bootstrapper:
   ```csharp
   public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
   public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
   ```

### Phase 4: Integration & Polish

30. **App.axaml.cs** - Wire up dependency injection or manual service instantiation
31. Command bindings in ViewModels (ICommand implementations)
32. Error handling, validation messages, loading states
33. Keyboard shortcuts (Ctrl+V to paste, Enter to add URL, Delete to remove from queue)

## Reusable Existing Code

- **yt-dlp command structure** from [Program.cs:130](c:\Users\jhiba\source\repos\sc-link-formatter\LinkFormatter\Program.cs#L130)
- **URL skip logic** from [Program.cs:126-128](c:\Users\jhiba\source\repos\sc-link-formatter\LinkFormatter\Program.cs#L126-L128)
- **ExistingDownloads.txt parsing logic** from [Program.cs:137-145](c:\Users\jhiba\source\repos\sc-link-formatter\LinkFormatter\Program.cs#L137-L145) (adapt to JSON)
- **Music folder path resolution** from current working directory logic

## Implementation Sequence

1. **Install Avalonia** - Add NuGet packages, update .csproj
2. **Create Models** - DownloadItem, enums, AppSettings
3. **Build Services** - SettingsService → UrlValidator → FFmpegService ⭐ → FileService → DownloadService ⭐
4. **Build ViewModels** - ViewModelBase → Child VMs → MainWindowViewModel ⭐
5. **Create Theme** - DarkTheme.axaml with orange accents
6. **Build Views** - Individual views → WelcomeView (with FFmpeg download) → MainWindow ⭐
7. **Wire Up App** - App.axaml.cs DI, Program.cs bootstrapper
8. **Test FFmpeg Download** - First run → FFmpeg auto-download → Verify ffmpeg.exe exists
9. **Test Download Workflow** - Add URL → Validate → Queue → Download → Progress → Completion
10. **Polish** - Error handling, keyboard shortcuts, animations

## Verification & Testing

### End-to-End Test Scenarios

1. **First Run (with FFmpeg Download):**
   - Delete `%APPDATA%\SoundCloudDownloader\appsettings.json`
   - Delete `Music\ffmpeg.exe` if it exists
   - Launch app → Welcome screen should appear
   - FFmpeg download should start automatically (progress bar visible)
   - Wait for FFmpeg download to complete (~80MB, 1-2 minutes)
   - Verify `Music\ffmpeg.exe` exists after download
   - Enter auth token (optional) → Click Continue
   - Main window should open with empty queue

1a. **First Run (FFmpeg Already Exists):**
   - Delete `%APPDATA%\SoundCloudDownloader\appsettings.json`
   - Ensure `Music\ffmpeg.exe` exists
   - Launch app → Welcome screen should appear
   - FFmpeg check should complete instantly (no download)
   - Enter auth token (optional) → Click Continue
   - Main window should open

2. **Settings Persistence:**
   - Set output folder to `C:\Music`
   - Set auth token, select WAV format
   - Close app, reopen → Settings should be restored

3. **URL Validation:**
   - Paste invalid URL (e.g., `https://google.com`) → Should show error
   - Paste valid SoundCloud URL → Should add to queue
   - Paste URL with `/you/` → Should skip with warning
   - Paste duplicate URL → Should show "Already downloaded" warning

4. **Download Workflow (MP3):**
   - Add valid SoundCloud URL
   - Select MP3 format
   - Click "Start Downloads"
   - Progress should update in real-time
   - Console should show yt-dlp output
   - On completion: Status = Completed, file appears in Downloaded Files list
   - URL added to DownloadedUrls in settings

5. **Download Workflow (WAV with Auth):**
   - Ensure auth token is set
   - Add valid URL, select WAV format
   - Start download → Should include `--add-header` in yt-dlp command
   - Verify download completes successfully

6. **Pause/Cancel:**
   - Start download
   - Click Cancel on item → Process should terminate, Status = Cancelled
   - Partial file may remain in output folder

7. **Queue Management:**
   - Add 3 URLs
   - Remove middle item → Queue should update
   - Reorder items (move up/down) → Queue should reflect changes
   - Clear completed downloads → Only completed items removed

8. **File Viewer:**
   - Click "Open" on downloaded file → Explorer should open with file selected
   - Click "Refresh" → File list should update with latest files in output folder

9. **Single-File Distribution:**
   - Build with `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`
   - Copy .exe + Music folder to different machine
   - Run .exe → Should launch GUI, download should work

### Success Criteria

✅ App launches as GUI (not console)
✅ First-run welcome screen appears on clean install
✅ **FFmpeg auto-downloads on first run if missing** (~80MB, with progress bar)
✅ Settings persist between sessions
✅ URLs validate correctly (accept SoundCloud, reject others)
✅ Duplicate prevention works (checks DownloadedUrls)
✅ Downloads execute with real-time progress updates
✅ MP3 downloads work without auth token
✅ WAV downloads work with auth token
✅ Pause/cancel terminates yt-dlp process
✅ Downloaded files list updates after completion
✅ Dark theme with Claude orange accents applied
✅ Single .exe distribution works on clean Windows machine (no ffmpeg.exe in repo)

## Technical Notes

- **yt-dlp output parsing:** Use `--newline` flag for consistent line-by-line output
- **Process cleanup:** Use `.NET 5+ Process.Kill(entireProcessTree: true)` to terminate ffmpeg subprocess
- **Console memory:** Limit ProgressConsoleViewModel to 500 lines max
- **Settings corruption:** Try-catch on load, fall back to defaults if JSON invalid
- **Cross-platform:** Avalonia supports Windows, macOS, Linux (yt-dlp/ffmpeg must be bundled per platform)

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| yt-dlp output format changes | Use `--newline` for consistent parsing, regex with fallbacks |
| ffmpeg subprocess not killed on cancel | Use `Process.Kill(entireProcessTree: true)` |
| Large console output causes memory issues | Limit to 500 lines, trim oldest entries |
| Settings file corruption | Try-catch on load, atomic writes, default fallback |
| First-time users confused by auth token | Clear welcome screen, link to README, "MP3 only" option |
