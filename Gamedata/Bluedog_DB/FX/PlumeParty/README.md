# PlumeParty

Fancy KSP particle pack for mod makers

The "Plume Party" is a collection of plumes for parts in KSP, made for mod makers to use. Many of the provided plumes are actually pairs designed to blend seamlessly and create an even more visually appealing composite plume. The included configs "cfg_xyz.txt" should not be allowed to persist as ".cfg" files in GameData. They are only to be used as source to add to part configs in other mods. You, the mod maker, are welcome to bundle the plumes you use into your own mod. This pack is currently not to be treated as a dependency and currently will not be released on the KSP forum, SpaceDock, CurseForge or other such places.

The intricate work is already done. You just need to do the following:

* Copy and paste
* Change the `transform` key's values when necessary
* Add `localScale = 1, 1, 1` and `localPosition = 0, 0, 0` (default values shown) within `MODEL_MULTI_PARTICLE {}` and change their values when necessary to fit a plume to the desired part. Plume pairs will behave as long as they receive identical settings.
* Remove the ModuleManager PASS codes `@whatever` and `@PART[]` and so on.

Have fun and fly safe™
