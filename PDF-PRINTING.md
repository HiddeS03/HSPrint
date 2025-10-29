# PDF Printing Setup

## Overview

The `PrintPdf` method has been updated to use external PDF utilities for reliable printing on Windows. This approach avoids the `Win32Exception` error that occurred with the previous shell-based printing method.

## Supported PDF Utilities

The service attempts to use PDF printing utilities in the following order:

### 1. **SumatraPDF** (Recommended) ?

SumatraPDF is a lightweight, free PDF viewer with excellent command-line printing support.

**Installation:**
- Download from: https://www.sumatrapdfreader.org/download-free-pdf-viewer
- Install to default location OR place `SumatraPDF.exe` in the HSPrint application directory

**Why SumatraPDF?**
- ? Lightweight (< 10MB)
- ? Free and open-source
- ? Excellent command-line support
- ? Silent printing (no UI)
- ? Fast and reliable
- ? No dependencies

**Checked Paths:**
```
C:\Program Files\SumatraPDF\SumatraPDF.exe
C:\Program Files (x86)\SumatraPDF\SumatraPDF.exe
[HSPrint Directory]\SumatraPDF.exe
```

### 2. **Adobe Acrobat Reader** (Fallback)

If SumatraPDF is not found, the service will attempt to use Adobe Acrobat Reader.

**Checked Paths:**
```
C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe
C:\Program Files (x86)\Adobe\Acrobat Reader DC\Reader\AcroRd32.exe
C:\Program Files\Adobe\Acrobat Reader DC\Reader\AcroRd32.exe
```

**Limitations:**
- ?? Slower than SumatraPDF
- ?? Larger installation size
- ?? May show UI briefly
- ?? Requires killing process after printing

### 3. **Windows Shell** (Last Resort)

If no PDF utility is found, falls back to Windows default handler.

**Limitations:**
- ? Unreliable
- ? May not work at all
- ? Platform-specific behavior
- ? Cannot specify printer

## Quick Setup (Recommended)

### Option A: Install SumatraPDF System-Wide

1. Download installer: https://www.sumatrapdfreader.org/download-free-pdf-viewer
2. Run installer (default location: `C:\Program Files\SumatraPDF\`)
3. Restart HSPrint service
4. Test PDF printing

### Option B: Portable SumatraPDF

1. Download portable version: https://www.sumatrapdfreader.org/download-free-pdf-viewer
2. Extract `SumatraPDF.exe` 
3. Copy to HSPrint application directory (same folder as `HSPrint.exe`)
4. Restart HSPrint service
5. Test PDF printing

## Testing

### Test PDF Printing

```powershell
# Create a test PDF (base64 encoded)
$pdfContent = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes("C:\path\to\test.pdf"))

# Send print request
Invoke-RestMethod -Method Post -Uri "http://localhost:50246/print/pdf" `
  -ContentType "application/json" `
  -Body (@{
    printerName = "Your Printer Name"
    base64Pdf = $pdfContent
  } | ConvertTo-Json)
```

### Check Logs

Look for these messages in `logs/hsprinteragent-YYYYMMDD.log`:

**Success with SumatraPDF:**
```
Successfully sent PDF to {PrinterName} using SumatraPDF
```

**Success with Adobe Reader:**
```
Successfully sent PDF to {PrinterName} using Adobe Reader
```

**Warning (No utility found):**
```
No PDF printer utility found (SumatraPDF or Adobe Reader). Using Windows shell - this may not work reliably.
For best results, install SumatraPDF from https://www.sumatrapdfreader.org/
```

## Troubleshooting

### "No PDF printer utility found" warning

**Solution:** Install SumatraPDF (see Quick Setup above)

### PDF not printing

1. **Check printer name is correct:**
   ```powershell
   Invoke-RestMethod http://localhost:50246/printer
   ```

2. **Verify PDF utility is installed:**
   - Check if `C:\Program Files\SumatraPDF\SumatraPDF.exe` exists
   - Or place portable version in HSPrint directory

3. **Check logs** for error details

4. **Test PDF manually:**
   ```cmd
   "C:\Program Files\SumatraPDF\SumatraPDF.exe" -print-to "Your Printer" "C:\path\to\test.pdf"
   ```

### Permissions issues

- Ensure HSPrint has permission to write to temp directory
- Ensure printer drivers are installed
- Run HSPrint as administrator if needed

### Adobe Reader stays open

This is expected behavior - the service kills the Reader process after a delay. If issues occur:
- Switch to SumatraPDF (no process killing needed)
- Increase delay in code if needed

## Command-Line Reference

### SumatraPDF
```bash
SumatraPDF.exe -print-to "Printer Name" -silent "file.pdf"
```

### Adobe Acrobat Reader
```bash
AcroRd32.exe /t "file.pdf" "Printer Name"
```

## Future Improvements

Potential enhancements for future versions:

- [ ] Bundle SumatraPDF with installer
- [ ] Add configuration option for custom PDF utility path
- [ ] Support for Linux (using `lp`, `lpr`, or CUPS)
- [ ] Support for macOS (using `lp`)
- [ ] Native PDF rendering library (no external dependencies)
- [ ] PDF to image conversion before printing

## Technical Details

**Why not use .NET built-in PDF printing?**
- `PrintDocument` doesn't natively support PDF
- `Process.Start` with "printto" verb is unreliable across Windows versions
- External utilities provide consistent, tested printing behavior

**Why SumatraPDF over Adobe?**
- Much smaller download (~10MB vs ~200MB)
- Faster startup and printing
- Better command-line support
- No license restrictions
- No need to kill process

**Temporary files:**
- PDFs are temporarily saved to `%TEMP%\hsprint_{guid}.pdf`
- Files are automatically deleted after 10 seconds
- Cleanup happens asynchronously to avoid blocking

## License & Attribution

- **SumatraPDF**: GPLv3 / Free (https://www.sumatrapdfreader.org/)
- **Adobe Acrobat Reader**: Proprietary / Free for personal use

---

**Need Help?**
- GitHub Issues: https://github.com/HiddeS03/HSPrint/issues
- Email: support@hssoftware.nl
