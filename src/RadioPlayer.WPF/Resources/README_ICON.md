# Application Icon Setup

The application is configured to use a custom icon (`app.ico`), but it's currently disabled to allow compilation without the icon file.

## How to Add an Icon

### Step 1: Download a Radio Icon

Choose one of these free icon sources:

1. **Icon-icons.com** (Recommended - Direct ICO support)
   - Visit: https://icon-icons.com/search/icons/radio
   - Filter by "ICO" format
   - Download your favorite radio icon
   - Suggested: Look for "radio", "fm radio", "antenna" designs

2. **Iconscout** (Multiple formats including ICO)
   - Visit: https://iconscout.com/icons/radio
   - Download in ICO format (or PNG to convert)

3. **Flaticon** (May require conversion to ICO)
   - Visit: https://www.flaticon.com/free-icons/radio
   - Download SVG/PNG and convert to ICO

### Step 2: Prepare the Icon File

- **File format:** `.ico` (Windows Icon)
- **Recommended sizes:** 16x16, 32x32, 48x48, 256x256 (multi-resolution ICO)
- **File name:** `app.ico`

**If you downloaded PNG/SVG:**
- Use an online converter like:
  - https://convertio.co/png-ico/
  - https://www.icoconverter.com/
  - https://cloudconvert.com/png-to-ico

### Step 3: Save the Icon

1. Save the icon file as: `src/RadioPlayer.WPF/Resources/app.ico`
2. Make sure the file is named exactly `app.ico` (case-sensitive on some systems)

### Step 4: Enable the Icon in the Project

1. Open `src/RadioPlayer.WPF/RadioPlayer.WPF.csproj`
2. Find this line (around line 10):
   ```xml
   <!-- <ApplicationIcon>Resources\app.ico</ApplicationIcon> -->
   ```
3. Uncomment it by removing `<!--` and `-->`:
   ```xml
   <ApplicationIcon>Resources\app.ico</ApplicationIcon>
   ```
4. Save the file

### Step 5: Rebuild the Project

```bash
dotnet build RadioPlayer.sln --configuration Release
```

The icon will now appear:
- In the Windows taskbar when the app is running
- In the title bar of the application window
- In Windows Explorer for the .exe file
- In the Windows Start menu

## Icon Design Recommendations

For a Radio Player app, consider icons featuring:
- 📻 Classic radio receiver
- 📡 Radio tower/antenna
- 🎵 Music waves
- 🔊 Sound waves
- ⚡ Radio signal waves

**Color scheme:** Purple/violet tones to match the Material Design theme (DeepPurple primary color)

## License Considerations

- Ensure the icon you download is free for commercial use
- Check the license terms on the icon website
- Most free icon sites (Flaticon, Icons8, Icon-icons) offer free icons with attribution or under Creative Commons licenses
- For commercial projects, verify the license allows commercial use

## Troubleshooting

**Error: "Could not find file app.ico"**
- Verify the file exists at: `src/RadioPlayer.WPF/Resources/app.ico`
- Check the file name is exactly `app.ico` (lowercase)
- Ensure the file is a valid ICO format

**Icon doesn't appear after building:**
- Clean and rebuild: `dotnet clean && dotnet build`
- Check that ApplicationIcon line is uncommented in .csproj
- Verify the ICO file is valid (try opening it in an image viewer)

---

**Current Status:** Icon feature is disabled to allow compilation. Follow the steps above to enable it.
