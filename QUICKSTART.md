# HSPrint - Quick Reference

## 🚀 Start Development
```bash
dotnet watch run
# or
start-dev.bat
```
Open: http://localhost:50246

## 📦 Build for Production
```powershell
.\build.ps1
```
Output: `./publish/HSPrint.exe`

## 🧪 Test API
```powershell
.\test-api.ps1
```

## 📡 API Endpoints Quick Reference

### Health
- `GET /health` - Health check
- `GET /health/version` - Get version
- `GET /health/update` - Check for updates

### Printers
- `GET /printer` - List all printers

### Print Jobs
- `POST /print/zpl` - Print ZPL to local printer
  ```json
  { "printerName": "Zebra", "zpl": "^XA..." }
  ```

- `POST /print/zpl/tcp` - Print ZPL via TCP/IP
  ```json
  { "ip": "192.168.1.100", "port": 9100, "zpl": "^XA..." }
  ```

- `POST /print/image` - Print PNG image
  ```json
  { "printerName": "EPSON", "base64Png": "iVBOR..." }
  ```

- `POST /print/pdf` - Print PDF document
  ```json
  { "printerName": "PDF Printer", "base64Pdf": "JVBER..." }
  ```

## 🔧 Configuration Files
- `appsettings.json` - Main configuration
- `version.txt` - Current version
- `logs/` - Application logs

## 📝 Common Tasks

### Change Port
Edit `appsettings.json`:
```json
"Urls": "http://localhost:YOUR_PORT"
```

### Add CORS Origin
Edit `appsettings.json`:
```json
"Cors": {
  "AllowedOrigins": ["http://localhost:3001", "https://yoursite.com"]
}
```

### Update Version
Edit `version.txt` and rebuild

### View Logs
Check `logs/hsprinteragent-YYYYMMDD.log`

## 🐛 Troubleshooting

### Port Already in Use
```powershell
netstat -ano | findstr :50246
taskkill /PID <PID> /F
```

### Printer Not Found
```powershell
# List printers via PowerShell
Get-Printer | Select-Object Name
```

### Check if Running
```powershell
Invoke-RestMethod http://localhost:50246/health
```

## 📚 Documentation
- Full docs: README.md
- Swagger UI: http://localhost:50246 (when running)
- GitHub: https://github.com/yourusername/HSPrint
