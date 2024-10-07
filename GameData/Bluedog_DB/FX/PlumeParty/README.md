# PlumeParty

Fancy KSP particle pack for mod makers

The "Plume Party" is a collection of engine particles for parts in KSP, made for mod makers to use. Plume Party uses the stock methods for plumes and stands in the gap between the stock library and RealPlume. Rocket engine plumes are provided with variants for sea level and vacuum. Plume Party aims to provide for many popular sorts of engines: 
* [x] RCS thrusters
* [ ] Jet engines
  * [x] Turbofan
  * [x] Turbojet
  * [x] Nuclear
  * [ ] Scramjet
* [ ] Rockets (liquid)
  * [x] LFO, Hydrolox, Methalox (Raptor/Tundra)
  * [x] LFO, Hydrolox, Methalox (Blue Origin)
  * [ ] LFO, Hydrolox, Methalox (Generic)
  * [x] Nuclear
  * [x] Hypergolic (partial to BDB)
  * [x] Toroidal/annular aerospikes
  * [ ] TEA/TEB flash for engage event
  * [ ] Trail smoke for sea-level engines only
* [ ] Rockets (solid)
  * [x] Standard
  * [ ] Alternate
* [ ] Ion engines
  * [ ] Xenon
  * [ ] Argon

Many of the provided plumes are actually sets of 2 or more, designed to blend seamlessly and create an even more visually appealing composite plume. The included configs "cfg_xyz.txt" should not be allowed to persist as ".cfg" files in GameData. They, and the active files for the stock engines, ending in ".cfg" are for you to use as source to add to part configs in other mods. You, the mod maker, are welcome to bundle the plumes you use into your own mod. This pack is currently not to be treated as a dependency and currently will not be released on the KSP forum, SpaceDock, CurseForge or other such places. This may change at some point, once it reaches a level of completeness.

The intricate work is already done. You just need to do the following:

* Copy and paste.
* Change any `transform` keys' values when necessary.
* Add `localScale = 1, 1, 1` and `localPosition = 0, 0, 0` (default values shown) within `MODEL_MULTI_PARTICLE {}` and change their values when necessary to fit a plume to the desired part. Plume pairs will behave as long as they receive identical settings.
* Remove the ModuleManager PASS codes `@whatever` and `@PART[]` and so on.

Have fun and fly safe™

## Warning
Be aware that faults have been confirmed in the `localScale` function. Plume scaling breaks at the 4th effect transform on an engine and at the 6th effect transform on an RCS thruster.


