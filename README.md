# SXL-Map-Tools

Collection of tools for making custom maps for Skater XL.

If you'd like a full template project with example scenes and everything set up and ready to start making maps, check out [SXL-Template-Project](https://github.com/billowper/SXL-Template-Project)

## How to Install

1. Open the Unity Package Manager under `Window/Package Manager`
2. Click the + button at the top left and chose "Add package from git URL..."
3. Paste this URL `https://github.com/billowper/SXL-Map-Tools.git`
4. Wait for install

![Add Package](https://i.imgur.com/m8oimus.jpg)

## Features

![ToolsWindow](https://i.imgur.com/ME4r345.jpg)

### Export Tool
Builds your open scene to an AssetBundle and copies the file to the Skater XL custom maps directory in My Documents, ready to play! You can also override the export name and delete previous versions in the maps directory using the tools window.

![ExportTool](https://i.imgur.com/ocOVFNQ.jpg) 

### GrindSurface
This component aims to streamline the creation of grindable objects, including generation of primitive colliders. 

* Add a GrindSurface component to any object, then use the buttons in the inspector to draw or manually add GrindSplines as children. When drawing splines, colliders are automatically generated on confirmation.

![GrindSurface](https://i.imgur.com/AQvQhhk.jpg)

###  GrindSpline
This component makes setting up grindable objects a little easier, with visual gizmos and some extra steps during the export process, allowing for more flexible scene setups. 

* Add a GridSpline component to a GameObject, use the "Add Point" button to create points for the grind spline
* The GrindSpline component will automatically update the name of the object so that it gets picked up correctly by the importer
* When Exporting, all objects with a GrindSpline component are moved into a "Grinds" object in the root of the scene, in order for the importer to detect and process them correctly.

![GrindSpline](https://i.imgur.com/svtbzpe.jpeg)

###  GrindSurface Spline Generation (Experimental)
GrindSurface and the accompnaying Grind Surface Generator tool window now allow you to generate splines for the selected GrindSurface.

* Select 1 or more objects in your scene and hit `CTRL + G` to add GrindSurfaces to them, which then automatically generates splines.
* Open the Grind Surface Generator window under the SXL menu item for settings and options, plus a button to a selected box collider's offset (useful when some edges don't get nicely det

![GrindSurfaceGenreation](https://i.imgur.com/VVaL715.jpg)

### Player Scale Reference Tool
Pressing `Shift + G` will place a scale reference player model at your mouse position in the scene view. The model is Editor Only, so don't worry about him showing up in your exported map!

![ScaleRef](https://i.imgur.com/N5B9wUk.jpg)
