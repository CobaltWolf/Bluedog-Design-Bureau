# B9PartSwitch

A Kerbal Space Program plugin designed to implement switching of part meshes, resources, and nodes

This mod will not change anything by itself, but is designed to be used by other mods to enable part subtype switching

## Forum Thread

http://forum.kerbalspaceprogram.com/index.php?showtopic=140541

## Requirements

* KSP version 1.2.2 (build 1622) is the only supported KSP version
* [ModuleManager](http://forum.kerbalspaceprogram.com/index.php?showtopic=50533) is required.

## Installation

* Remove any previous installation of B9PartSwitch
* Make sure the latest version of ModuleManager is installed
* Copy the B9PartSwitch directory to your KSP GameData directory.

## Contributors

* [blowfish](http://forum.kerbalspaceprogram.com/index.php?/profile/119688-blowfish/) - primary developer
* [bac9](http://forum.kerbalspaceprogram.com/index.php?/profile/57757-bac9/) - author of [PartSubtypeSwitcher](https://bitbucket.org/bac9/ksp_plugins), on which this plugin is heavily based

## Source

The source can be found at [Github](https://github.com/blowfishpro/B9PartSwitch)

## License

This plugin is distributed under [LGPL v3.0](http://www.gnu.org/licenses/lgpl-3.0.en.html)

## Changelog


### v1.7.1

* Fix an occasional NRE when building part info

### v1.7.0

* Allow "child" part switch modules to modify volume of "parent" module
* Allow multiple modules to manage the same transform or node, only enable it if they all agree

### v1.6.1

* Switch percentFilled priority to resource -> subtype -> tank type -> 100% since resources can be overridden on individual subtypes now

### v1.6.0

* Allow tanks to be partially filled - percentFilled can be defined on the subtype, resource, or tank type (in decreasing order of priority), defaulting to completely full
* Allow toggling resource tweakability in the editor - resourcesTweakable can be defined on the subtype or tank type (subtype takes priority), default is whatever the standard is for that resource
* Allow RESOURCE nodes directly on the subtype
  * If the resource already exists on the tank, values defined here will override what is already on the tank (won't affect other subtypes using the same tank)
  * If it isn't already on the tank, it will be added (won't affect other subtypes using the same tank)
* Add ModuleB9DisableTransform to remove unused transforms on models
* Major internal changes

### v1.5.3

* Recompile against KSP 1.2.2
* Remove useless warnings in the log
* A few internal changes

### v1.5.2

* Recompile against KSP 1.2.1

### v1.5.1

* Fix resource amounts displaying incorrectly in part tooltip
* Reformat module title in part list tooltip a bit
* Hopefully reduce GC some more

### v1.5.0

* Update for KSP 1.2
* Add CoMOffset, CoPOffset, CoLOffset, CenterOfBuoyancy, CenterOfDisplacement to editable part fields
* Hopefully reduce GC allocation a little bit

### v1.4.3

* Recompile against KSP 1.1.3
* Remove some code which is unnecessary in KSP 1.1.3

### v1.4.2

* Fix TweakScale interaction - resource amounts did not account for scaling (broken since v1.4.0)

### v1.4.1

* Fix bug where we were setting maxTemp when we should have been setting skinMaxTemp or crashTolerance

### v1.4.0

* Find best subtype intelligently
  * If subtype name was previously set, use it to find the correct subtype (allows subtypes to be reordered without breaking craft)
  * If name was not previously set or not found, but index was, use it (this allows transitioning from current setup and renaming subtypes if necessary)
  * If index was not previously set, try to infer subtype based on part's resources (this allows easy transitioning from a non-switching setup)
  * Finally, just use first subtype
* Add unit testing for subtype finding
* Get rid of some unnecessary logging in debug mode
* Refactor part switching a bit

### v1.3.1

* Fix bug where having ModuleB9PartInfo on a root part would cause physics to break due to an exception (really a stock issue but no sense waiting for a fix)

### v1.3.0

* Do not destroy incompatible fuel switchers.  Instead, disable fuel switching
* Allow part's crash tolerance to be edited
* Add info module to display changes to part in the info window.  Only displays things that can be changed.
* Various internal changes

### v1.2.0

* Support TweakScale integration
* Allow plural switcher description (in part catalog) to be edited)
* Disable changing surface attach node size (problematic with Tweakscale)

### v1.1.4

* Don't remove FSfuelSwitch or InterstellarFuelSwitch if ModuleB9PartSwitch doesn't manage resources
* Defer rendering drag cubes until part has been attached (fixes flickering in editor)
* Avoid firing events multiple times
* Various internal changes

### v1.1.3

* Recompile against KSP 1.1.2
* Simplify part list info a bit
* Hopefully make some error messages clearer
* Various internal refactors and simplifications

### v1.1.2

* Remove FSmeshSwitch and InterstellarMeshSwitch from incompatible modules
* Recompile against KSP 1.1.1

### v1.1.1

* Fix resource cost not accounting for units per volume on tank type

### v1.1

* KSP 1.1 compatibility
* Fixed bug where having part switching on the root part would cause physics to break
* Moved UI controls to UI_ChooseOption
* Adjust default Monopropellant tank type to be closer to (new) stock values
* Use stock part mass modification
* Hopefully fix incompatible module checking
* Various refactors and simplifications which might improve performance a bit

### v1.0.1

* Fix NRE in flight scene

### v1.0.0

* Initial release
