# Application Icon Instructions

## Issue
The icon URLs from iconsdb.com don't allow direct programmatic downloads.

## Manual Download Instructions

### Option 1: Use Your Provided Icons (Recommended)
1. **PNG (512x512)**: Download from https://www.iconsdb.com/icons/download/black/radio-4-512.png
   - Save as: `src/RadioPlayer.WPF/Resources/app.png`

2. **ICO (512x512)**: Download from https://www.iconsdb.com/icons/download/black/radio-4-512.ico
   - Save as: `src/RadioPlayer.WPF/Resources/app.ico`

### Option 2: Alternative Free Icon Sources

#### Free Icon Websites:
1. **Icons8** - https://icons8.com/icons/set/radio (Free with attribution)
2. **Flaticon** - https://www.flaticon.com/search?word=radio (Free with attribution)
3. **Icon-icons.com** - https://icon-icons.com/search/icons/radio (Various licenses)
4. **IconArchive** - https://www.iconarchive.com/tag/radio (Check individual licenses)

#### How to Download:
1. Visit one of the websites above
2. Search for "radio" icons
3. Download in both PNG (512x512) and ICO formats
4. Save PNG as `app.png` and ICO as `app.ico` in the Resources folder

### Option 3: Create Icon from SVG
I've already downloaded a radio icon from Bootstrap Icons (MIT licensed):
- File: `src/RadioPlayer.WPF/Resources/radio.svg`

To convert to PNG/ICO:
1. Use an online converter like:
   - https://cloudconvert.com/svg-to-png
   - https://convertio.co/svg-ico/
2. Upload `radio.svg`
3. Set size to 512x512 pixels
4. Download and rename to `app.png` and `app.ico`

### Option 4: Use Windows Default Icon
The application will use a default system icon if no custom icon is provided.

## After Adding Icons

Once you have the icons in place:
1. PNG file: `src/RadioPlayer.WPF/Resources/app.png`
2. ICO file: `src/RadioPlayer.WPF/Resources/app.ico`

The project is already configured to use them:
- Application icon (taskbar, title bar): Uses `app.ico`
- System tray icon: Will use `app.ico`

No code changes needed - just place the files and rebuild!
