# 🚀 HSPrint - Quick Reference Card

## Start Development

### Fastest Way to Start
Press **F5** → App runs + Swagger opens automatically

### Alternative Methods
```powershell
# Terminal
dotnet run

# Watch mode (hot reload)
Ctrl+Shift+P → "Run Task" → "watch"
```

---

## 🔥 Most Used Shortcuts

| Action | Shortcut |
|--------|----------|
| **Start Debug** | F5 |
| **Build** | Ctrl+Shift+B |
| **Quick Open File** | Ctrl+P |
| **Command Palette** | Ctrl+Shift+P |
| **Go to Definition** | F12 |
| **Find References** | Shift+F12 |
| **Rename Symbol** | F2 |
| **Format Document** | Shift+Alt+F |
| **Toggle Terminal** | Ctrl+` |
| **Toggle Sidebar** | Ctrl+B |

---

## 📍 Important URLs

- **API Base**: http://localhost:50246
- **Swagger UI**: http://localhost:50246/swagger
- **Health Check**: http://localhost:50246/api/health

---

## 🛠️ Common Tasks

### Build Project
```powershell
Ctrl+Shift+B
```

### Clean & Rebuild
```powershell
Ctrl+Shift+P → "Run Task" → "clean"
Ctrl+Shift+B  # Build again
```

### Add NuGet Package
```powershell
dotnet add package PackageName
```

### Restore Packages
```powershell
Ctrl+Shift+P → "Run Task" → "restore"
```

---

## 🐛 Debugging

- **Set Breakpoint**: Click gutter or F9
- **Step Over**: F10
- **Step Into**: F11
- **Continue**: F5
- **Stop**: Shift+F5

---

## 📂 Project Structure

```
HSPrint/
├── Controllers/     → API endpoints
├── Services/        → Business logic
├── Utils/          → Helper classes
└── Properties/     → Launch settings
```

---

## 💡 Pro Tips

1. **IntelliSense**: Type `.` after any object for suggestions
2. **Import Suggestions**: Hover over red squiggles → "Quick Fix"
3. **Multiple Cursors**: Alt+Click
4. **Duplicate Line**: Shift+Alt+↓
5. **Comment Toggle**: Ctrl+/

---

## 🔍 Search & Navigation

- **Search in Files**: Ctrl+Shift+F
- **Go to Symbol**: Ctrl+T
- **Go to Line**: Ctrl+G
- **Peek Definition**: Alt+F12

---

## 📦 Extensions Installed

- ✅ C# Dev Kit
- ✅ C# Extension
- 📋 Check `.vscode/extensions.json` for recommended extensions

---

## 🆘 Troubleshooting

### Build Fails?
```powershell
dotnet restore
dotnet clean
dotnet build
```

### IntelliSense Not Working?
```powershell
Ctrl+Shift+P → "Developer: Reload Window"
```

### Extension Issues?
```powershell
Ctrl+Shift+P → "Extensions: Show Recommended Extensions"
```

---

**Need More Help?** Check `.vscode/README.md`
