# HSPrint Logo Plaatsingen

Dit document beschrijft waar je logo's kunt plaatsen voor de HSPrint applicatie en installer.

## Logo Bestanden

Plaats je logo bestanden in de volgende locatie:
```
assets/
  ├── icon.ico          # Windows icoon (voor applicaties)
  ├── installer-icon.ico # MSI installer icoon
  └── logo.png          # PNG logo (voor about dialogs, etc.)
```

## 1. Config Tool Applicatie Icoon

**Locatie**: `HSPrint.ConfigTool/HSPrint.ConfigTool.csproj`

**Formaat**: `.ico` bestand (aanbevolen maten: 16x16, 32x32, 48x48, 256x256)

**Toevoegen**:
```xml
<PropertyGroup>
  <ApplicationIcon>..\assets\icon.ico</ApplicationIcon>
</PropertyGroup>
```

**Code aanpassing** in `HSPrint.ConfigTool/ConfigForm.cs`:
```csharp
// In constructor, vervang:
notifyIcon.Icon = SystemIcons.Application;

// Door:
notifyIcon.Icon = new Icon("icon.ico");
this.Icon = new Icon("icon.ico");
```

---

## 2. MSI Installer Icoon

**Locatie**: `HSPrint.Installer/Product.wxs`

**Formaat**: `.ico` bestand (32x32 of 48x48 pixels aanbevolen)

**Toevoegen**:
```xml
<Package>
  ...
  <Icon Id="ProductIcon" SourceFile="..\assets\installer-icon.ico" />
  <Property Id="ARPPRODUCTICON" Value="ProductIcon" />
</Package>
```

Dit zorgt ervoor dat:
- Het icoon verschijnt in "Programs and Features"
- Het icoon gebruikt wordt voor shortcuts
- Het icoon zichtbaar is tijdens installatie dialogs

---

## 3. Start Menu Shortcut Icoon

**Locatie**: Automatisch overgenomen van Config Tool executable

Als je een apart icoon wilt voor de Start Menu shortcut, pas aan in `HSPrint.Installer/Product.wxs`:

```xml
<Shortcut Id="StartMenuShortcut"
          Name="HSPrint"
          Target="[INSTALLFOLDER]HSPrint.ConfigTool.exe"
          WorkingDirectory="INSTALLFOLDER"
          Icon="ProductIcon"
          IconIndex="0" />
```

---

## 4. About Dialog Logo (optioneel)

Als je een About dialog wilt toevoegen aan de Config Tool:

**Formaat**: `.png` bestand (aanbevolen: 128x128 of 256x256 pixels)

**Code toevoegen** in `HSPrint.ConfigTool/ConfigForm.cs`:
```csharp
private void ShowAboutDialog()
{
    var aboutForm = new Form
    {
        Text = "About HSPrint",
        Size = new Size(400, 300),
        FormBorderStyle = FormBorderStyle.FixedDialog,
        StartPosition = FormStartPosition.CenterParent,
        MaximizeBox = false,
        MinimizeBox = false
    };

    var pictureBox = new PictureBox
    {
        Image = Image.FromFile("logo.png"),
        SizeMode = PictureBoxSizeMode.Zoom,
        Size = new Size(128, 128),
        Location = new Point(136, 20)
    };

    var label = new Label
    {
        Text = "HSPrint Printer Agent\nVersion 1.0.0\n\n© 2024 HS Software",
        AutoSize = true,
        Location = new Point(100, 160),
        TextAlign = ContentAlignment.MiddleCenter
    };

    aboutForm.Controls.Add(pictureBox);
    aboutForm.Controls.Add(label);
    aboutForm.ShowDialog();
}
```

---

## Aanbevolen Logo Specificaties

### .ICO Bestanden
- **Formaat**: Windows Icon (.ico)
- **Resoluties**: Meerdere maten in één bestand:
  - 16x16 (voor taskbar)
  - 32x32 (voor desktop shortcuts)
  - 48x48 (voor Verkenner)
  - 256x256 (voor Windows 7+)
- **Kleurdiepte**: 32-bit met alpha channel (transparantie)
- **Tool**: Je kunt online converters gebruiken of tools zoals GIMP, Photoshop, of online: https://convertio.co/png-ico/

### .PNG Bestanden (voor About dialogs)
- **Formaat**: PNG met transparantie
- **Resolutie**: 128x128 of 256x256 pixels
- **Kleurdiepte**: 32-bit RGBA

---

## Snelle Setup Instructies

1. **Plaats je logo bestanden** in de `assets/` folder
2. **Update HSPrint.ConfigTool.csproj**:
   ```xml
   <ApplicationIcon>..\assets\icon.ico</ApplicationIcon>
   ```
3. **Update HSPrint.Installer/Product.wxs**:
   ```xml
   <Icon Id="ProductIcon" SourceFile="..\assets\installer-icon.ico" />
   <Property Id="ARPPRODUCTICON" Value="ProductIcon" />
   ```
4. **Update ConfigForm.cs** om het icoon te laden:
   ```csharp
   notifyIcon.Icon = new Icon(@"..\..\..\..\assets\icon.ico");
   this.Icon = notifyIcon.Icon;
   ```
5. **Rebuild de applicatie**: `.\build-installer.ps1`

---

## Testen

Na het toevoegen van de logo's, test het volgende:

- [ ] Config Tool toont het juiste icoon in de taskbar
- [ ] System tray icoon is zichtbaar en correct
- [ ] MSI installer toont het logo tijdens installatie
- [ ] "Programs and Features" toont het juiste icoon
- [ ] Start Menu shortcut heeft het correcte icoon
- [ ] Desktop shortcut (indien gemaakt) heeft het juiste icoon

---

## Troubleshooting

**Icoon wordt niet weergegeven na rebuild?**
- Clear de icon cache: `ie4uinit.exe -show`
- Herstart Windows Verkenner
- Rebuild het project volledig

**MSI installer toont geen icoon?**
- Controleer dat het .ico bestand bestaat op de opgegeven locatie
- Gebruik relatieve paden vanuit de .wixproj locatie
- Rebuild de installer

**System tray icoon is wazig?**
- Zorg dat je .ico bestand de juiste resoluties bevat (minimaal 16x16 en 32x32)
- Gebruik een hoogwaardige source afbeelding

---

Succes met het toevoegen van je logo's! 🎨
