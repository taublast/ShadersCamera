# ShadersCamera

**UNDER HEAVY CONSTRUCTION**

> Uses `DrawnUi.Maui.Camera` nuget for playing with camera

* Applying shaders to camera preview in real-time
* Saving captured photo with shader effect
* Saving EXIF and injecting custom metadata
* Can edit shaders code in standalone window when running on desktop

## TO FIX

make Ui appear faster, before cam is initialized

fix palette for toon
in cropped mode apply shader to image without borders
create antispam for taking previews
gallery
### Interaction:

* Tap on the screen to use current frame for bottom menu previews
* Tap/swipe bottom shader menu to select shaders
* Long pressing on shader preview in bottom menu would open shaders editor on desktop
* Other on-screen buttons: Power, Effect, Flash, Source, Capture Photo

Effect button is here temporarily: it's different basic color filters vs custom effect: shaders.
UI to be will changed much..

### Optional ToDo

* Apply shaders while saving in background
* Optional saving geolocation
* Enhanced shaders editor including mobile version