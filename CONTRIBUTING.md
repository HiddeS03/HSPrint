# Contributing to HSPrint

Thank you for your interest in contributing to HSPrint! This document provides guidelines and instructions for contributing.

## ??? Project Structure

```
HSPrint/
??? Controllers/ # API endpoints
?   ??? HealthController.cs
?   ??? PrinterController.cs
? ??? PrintController.cs
??? Services/             # Business logic
?   ??? IPrinterService.cs
?   ??? PrinterService.cs
?   ??? IPrintService.cs
?   ??? PrintService.cs
??? Utils/  # Helper utilities
?   ??? RawPrinterHelper.cs
?   ??? Updater.cs
??? Program.cs    # Application startup
??? appsettings.json      # Configuration
```

## ??? Development Setup

1. **Prerequisites**
   - .NET 8 SDK
   - Visual Studio 2022 or VS Code
   - Git

2. **Clone and Setup**
   ```bash
   git clone https://github.com/yourusername/HSPrint.git
   cd HSPrint
   dotnet restore
   ```

3. **Run in Development Mode**
   ```bash
   dotnet watch run
   ```

## ?? Coding Guidelines

### Code Style
- Use C# naming conventions
- Follow .NET coding standards
- Use async/await for I/O operations
- Add XML documentation comments for public APIs

### Example
```csharp
/// <summary>
/// Prints a ZPL document to the specified printer
/// </summary>
/// <param name="printerName">Name of the printer</param>
/// <param name="zpl">ZPL content to print</param>
/// <returns>True if successful, false otherwise</returns>
public async Task<bool> PrintZpl(string printerName, string zpl)
{
    // Implementation
}
```

## ?? Testing

### Manual Testing
```powershell
# Run the test script
.\test-api.ps1
```

### Adding Unit Tests
Create tests in a new `HSPrint.Tests` project:
```bash
dotnet new xunit -n HSPrint.Tests
dotnet add HSPrint.Tests reference HSPrint
```

## ?? Reporting Bugs

When reporting bugs, please include:
- HSPrint version (`GET /health/version`)
- Windows version
- Printer model and driver
- Complete error message or log
- Steps to reproduce

## ? Feature Requests

We welcome feature requests! Please provide:
- Use case description
- Expected behavior
- Why this would be useful
- Any implementation ideas

## ?? Pull Request Process

1. **Fork the Repository**
   ```bash
   git clone https://github.com/yourusername/HSPrint.git
   cd HSPrint
 git checkout -b feature/your-feature-name
   ```

2. **Make Your Changes**
 - Write clean, documented code
- Follow existing patterns
   - Add logging where appropriate
   - Update README if needed

3. **Test Your Changes**
   ```bash
   dotnet build
   dotnet run
   # Test your changes manually
   ```

4. **Commit Your Changes**
   ```bash
   git add .
   git commit -m "feat: add your feature description"
   ```

 Commit message format:
   - `feat:` New feature
   - `fix:` Bug fix
   - `docs:` Documentation changes
   - `refactor:` Code refactoring
   - `test:` Adding tests
   - `chore:` Maintenance tasks

5. **Push and Create PR**
   ```bash
   git push origin feature/your-feature-name
   ```
   Then create a pull request on GitHub.

## ?? Priority Areas

We're especially interested in contributions for:

### High Priority
- [ ] Windows Service support
- [ ] API key authentication
- [ ] Print job queue and retry logic
- [ ] Comprehensive error handling

### Medium Priority
- [ ] Linux support (CUPS)
- [ ] Print job history/logging to database
- [ ] Webhook notifications
- [ ] Printer status monitoring

### Nice to Have
- [ ] macOS support
- [ ] Docker container
- [ ] Web-based management UI
- [ ] Multi-language support

## ?? Resources

- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Serilog Documentation](https://github.com/serilog/serilog)
- [ZPL Programming Guide](https://www.zebra.com/us/en/support-downloads/knowledge-articles/zpl-programming-guide.html)

## ?? Communication

- **Issues**: For bug reports and feature requests
- **Discussions**: For questions and general discussion
- **Email**: support@hssoftware.nl for private inquiries

## ?? License

By contributing, you agree that your contributions will be licensed under the MIT License.

## ?? Recognition

Contributors will be recognized in:
- README.md contributors section
- Release notes
- Project documentation

Thank you for making HSPrint better! ??
