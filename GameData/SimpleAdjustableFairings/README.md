# SimpleAdjustableFairings

A  Kerbal Space Program plugin enabling easy to use, visually appealing fairings made from modular sections

This plugin adds no parts by itself, it is designed to be used with a specific pack of fairing parts.

## Requirements

This plugin is designed to work with a specific version of KSP, any others may not work:

* KSP Version: 1.11.2

## License

Code and plugin are distributed under the [GNU Lesser General Public License](https://www.gnu.org/licenses/lgpl-3.0.en.html).  Please see that link or the included `README` file for the full license terms.

## Code

[Available on Github](https://github.com/blowfishpro/SimpleAdjustableFairings/)

## Changelog

### v1.12.0

* Recompile against KSP 1.11.2

### v1.11.0

* Recompile against KSP 1.10.1

### v1.10.1

* Fix transparency not being set correctly on disabled objects (which might become enabled by switching)
* Fix fairing parts not re-initializing when new data is pushed by another module before the fairing is built

### v1.10.0

* Don't modify drag cubes/FAR/colliders if deployed (fixes an exception when loading a craft with a deployed fairing)
* Group all of the fairing's fields and events in the part action window:
  * Adds two new fields to `ModuleSimpleAdjustableFairing`:
    * `uiGroupName` - unique identifier for the group, defaults to `fairing`
    * `uiGroupDisplayName` - name of the group to display in the UI, defaults to `Fairing`

### v1.9.1

* Pre-render both drag cubes rather than re-rendering at deployment
  * Fix exceptions and potential physics weirdness at deployment

### v1.9.0

* Recompile against KSP 1.9.1

### v1.8.0

* Add `enabled` attribute to model data (default `true`)
  * Used for part switching to disable a particular piece of the fairing (since eliminating the node wouldn't do anything)
* Send `OnPartModelChanged` event so other modules can respond to changes in the model
* Listen for `ModuleDataChanged` to rebuild fairing
  * Use `requestNotifyFARToRevoxelize` and `requestRecalculateDragCubes` attributes of event details if present rather than recalculating aero properties ourself, that ensures it's only done once per cycle
* Use better method of recalculating drag cubes
* Send/listen for `DragCubesWereRecalculated` and `FarWasNotifiedToRevoxelize` to make sure actions are only done once per cycle

### v1.7.2

* Fix invisible prefabs counting for cargo bay occlusion tests

### v1.7.1

* Fix root part fairings when loading an existing vessel

### v1.7.0

* Wall now optional (but it won't be adjustable if not present)
* Add wall base that there will only ever be one of and will stay at the bottom of the fairing
  * `WALL_BASE` in the config
* Add cap which will only be attached to the first segment (and move with the nose)
  * `CAP` in the config

### v1.6.0

* Recompile against KSP 1.8.1

### v1.5.1

* Recompile against KSP 1.7.3

### v1.5.0

* Recompile against KSP 1.7.0

### v1.4.0

* Recompile against KSP 1.6.1

### v1.3.1

* Recompile against KSP 1.5.1

### v1.3.0

* Recompile for KSP 1.5

### v1.2.0

* Recompile for KSP 1.4.5
* Add Deploy action for action groups

### v1.1.1

* Recompile for KSP 1.4.4

### v1.1.0

* Recompile for KSP 1.4.3
* `maxSegments` now sets the maximum number of wall segments on the fairing (previously it was always 10)
* Fix icon setup not working correctly

### v1.0.1

* Recompile for KSP 1.3

### v1.0.0

* Bump version for a non-preview release

### v0.1

* Initial release
