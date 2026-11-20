# ChatApp Mobile Client (.NET MAUI 10)

This directory contains the cross-platform mobile client for **ChatApp**, built on .NET MAUI targeting Android, iOS, macOS (Mac Catalyst), and Windows.

---

## 1. Architectural Highlights

- **Framework:** .NET 10 (.NET MAUI with `MauiXamlInflator=SourceGen` compile-time XAML generation)
- **MVVM Architecture:** CommunityToolkit.Mvvm with modern C# 13/14 Partial Properties (`[ObservableProperty] public partial ...`) ensuring full Native AOT compatibility.
- **UI Toolkit:** CommunityToolkit.Maui (v16.0.0 tuned for .NET 10).
- **Navigation:** Centralized `INavigationService` abstracting Shell route-based navigation.
- **Theme Infrastructure:** Dynamic on-the-fly toggling between Light and Dark modes (`ThemeManager.ToggleTheme()`).
- **Typography & Localization:** Dual-direction layout support: Arabic Cairo font (RTL) and English OpenSans/Inter font (LTR).

---

## 2. Directory Structure

```
Mobile/
├── App.xaml / App.xaml.cs
├── AppShell.xaml / AppShell.xaml.cs
├── MauiProgram.cs
├── Features/
│   ├── Common/              # BaseViewModel, ThemeManager, DesignSystemPage & ViewModel
│   ├── Authentication/      # Login, Register, Profile Setup (Phase 5)
│   ├── Conversations/       # Inbox & User Search (Phase 6)
│   ├── Chat/                # Real-Time Chat (Phase 7)
│   ├── Profile/
│   └── Stickers/
├── Controls/                # Reusable luxury controls
│   ├── AvatarView.xaml      # User avatar with deterministic gradient initials and presence badge
│   ├── BadgeIndicator.xaml  # Unread badge with 99+ auto-formatting
│   ├── ChatButton.xaml      # Luxury button with spinner state (IsLoading) and haptic feedback
│   ├── ChatEntry.xaml       # Modern input with label, password reveal, and error feedback
│   └── StateContainer.xaml  # Content/Loading/Empty/Error state switcher
├── Converters/              # Value converters (Initials, Colors, Visibility, InverseBool)
├── Navigation/              # INavigationService, AppRoutes
└── Resources/
    ├── Colors/              # Colors.xaml
    ├── Styles/              # Typography.xaml, Styles.xaml
    └── Themes/              # LightTheme.xaml, DarkTheme.xaml
```

---

## 3. Design Tokens & Styling System

### 3.1 Color Palette
- **Brand Primary:** `#0D9488` (Vibrant Deep Teal)
- **Brand Secondary:** `#0284C7` (Electric Cyan)
- **Status Online:** `#10B981` (Emerald Green)
- **Status Error:** `#EF4444` (Coral Red)
- **Themes:**
  - Light Mode: Background `#F8FAFC`, Surface Card `#FFFFFF`, Text `#0F172A`
  - Dark Mode: Deep Space `#0B0F19`, Surface Card `#151D2F`, Text `#F8FAFC`

### 3.2 Spacing Scale
- `SpacingMicro`: 2dp
- `SpacingTiny`: 4dp
- `SpacingSmall`: 8dp
- `SpacingMedium`: 16dp
- `SpacingLarge`: 24dp
- `SpacingXLarge`: 32dp
- `SpacingXXLarge`: 48dp

---

## 4. How to Build & Run

### 4.1 Build for Windows
```powershell
dotnet build ChatApp.Mobile.csproj -f net10.0-windows10.0.19041.0
```

### 4.2 Build for Android
```powershell
dotnet build ChatApp.Mobile.csproj -f net10.0-android
```

### 4.3 Build for iOS
```powershell
dotnet build ChatApp.Mobile.csproj -f net10.0-ios
```
