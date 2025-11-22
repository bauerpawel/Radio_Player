# libFLAC Native Library

This directory should contain the native libFLAC library (DLL) required for OGG/FLAC streaming support.

## Required File

- **Windows x64**: `libFLAC.dll` (from FLAC 1.5.0 or newer)

## Download Instructions

### Option 1: Official FLAC Release (Recommended)

1. Download FLAC 1.5.0 Windows binaries:
   - URL: https://ftp.osuosl.org/pub/xiph/releases/flac/flac-1.5.0-win.zip
   - Or from: https://xiph.org/flac/download.html

2. Extract the ZIP file

3. Copy the appropriate DLL to this directory:
   - For x64: Copy `bin/win64/libFLAC.dll` → `libs/flac/libFLAC.dll`
   - For x86: Copy `bin/win32/libFLAC.dll` → `libs/flac/libFLAC.dll`

### Option 2: vcpkg

```bash
vcpkg install flac:x64-windows
```

Then copy the DLL from vcpkg's installed directory.

### Option 3: Chocolatey

```bash
choco install flac
```

Then copy `libFLAC.dll` from the installation directory.

## Build Integration

The project is configured to automatically copy `libFLAC.dll` from this directory to the output folder during build.

If the DLL is not found, OGG/FLAC streams will fail with a `DllNotFoundException`.

## License

libFLAC is distributed under the Xiph.Org BSD-like license.
See: https://github.com/xiph/flac/blob/master/COPYING.Xiph

## Version Information

- **Required Version**: 1.5.0 or newer
- **Architecture**: Match your application (x64 or x86)
- **File Size**: ~300-400 KB (approximately)
