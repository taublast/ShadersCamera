# Filters Camera

Open-source app for iOS, Mac Catalyst, Android and Windows applying hardware-accelerated shaders in real-time to camera preview and saved photos. 
Comes with built-in desktop [SKSL](https://skia.org/docs/user/sksl) editor.

SKSL shaders demonstrate: film grain, cubic splines, various image adjustments, lens distortion effects, edge detection and more.

Made with [DrawnUI for .NET MAUI](https://drawnui.net).

* Applying shaders to camera preview in real-time
* Switch mirror preview
* Switch full-screen/fit preview
* Saving captured photo with shader effect
* Saving EXIF and injecting custom metadata
* Select camera and photo quality/format
* Edit shaders code in standalone window when running on desktop

### How To Use

- Tap anywhere on the screen to set current frame as preview
- Choose your real-time filter from previews in drawer menu!
- Open settings for more!

For Windows users best with Photo Link app to instantly view your taken photos!

### Interaction:

- On-screen buttons:  View Captured, Settings, Flash, Source, Capture Photo
- User drawer menu to select filters
- Tap anywhere on the screen to use current frame for drawer previews
- Zoom with fingers
- On desktop long pressing shader preview opens SKSL editor

### On The Roadmap

* Save filter name to EXIF
* Add selection indicator for previews scroll
* Pass rendering scale as uniform for all shaders for full consistency between preview and large capture
* Localization and change language in settings

### Optional To-Do

* Apply shaders while saving in background
* Crop manual/presets
* Combine with lens shaders
* Save geolocation to EXIF
* Shaders editor for mobile version

### .NET MAUI Libs Stack

* [SkiaSharp](https://github.com/mono/SkiaSharp)
* [DrawnUi for .NET MAUI](https://github.com/taublast/DrawnUi)
* [FastPopups for .NET MAUI](https://github.com/taublast/FastPopups)