Wild Blue Tools

A KSP mod that provides common functionality for mods by Wild Blue Industries.

---INSTALLATION---

Copy the contents of the mod's GameData directory into your GameData folder.

1.10.0
New Props
- Holoscreen: This prop works the same way as the internal plasma screen, but you can toggle the screen on and off.

Bug Fixes & Enhancements
- WBIGeoLab now integrates into the Operations Manager.
- You can properly configure a part to be a battery by using the ConverterSkill.
- Fixed an issue with WBIHeatRadiator not showing up in the Operations Manager.
- Fixed an issue with IVAs spawning in the editor when inflating parts.
- You can now select the default image for the Plasma Screen in addition to screens in the Screenshots folder.
- Moved the kPad and plasma screens to the Utility tab.
- The experiment lab now accounts for the science multiplier difficulty setting when generating bonus science.

1.9.0
- Added WBINameTag, WBIGroundStabilizer, and WBIGeoLab.
- Added the Buckboard 6000

1.8.10
- KSP 1.2.2 update.

1.8.9
- Greenhouse fixes.

1.8.8
- Bug fixes & enhancements.

1.8.7
- Disabled angle snap.

1.8.6
- If the target docking port supports angle snap that you can turn on/off (all WBI docking ports do), and it's turned off, then it will be turned on if the active port's angle snap is turned on.

1.8.5

WBIDockingNodeHelper
- Added ability to enable/disable angle snap, and the ability to set the snap angle.

Other
- Cleaned up some logging issues related to missing part modules and textures when supported mods aren't installed.

1.8.4
- Updated to KSP 1.2.1
- Minor bug fixes with WBILight

1.8.3
- Fixed some welding issues.
- Greenhouses won't harvest crops if you run out of resources.

1.8.1
- You can now weld ports during eva.

1.8.0
- Added WBIConvertibleMPL. Use this when you want science labs with stock Mobile Processing Lab functionality to be able to switch to a different configuration.

1.7.3
- Fixed an issue where the greenhouse would freeze the game on load.

1.7.0
Updated to KSP 1.2. Expect additional patches as KSP is fixed and mods are updated.

1.6.5
- Growth time is no longer reduced based upon experienced Scientists. Yield is still affected by experience though.
- Greenhouses now show where they're at in the growth cycle and show up in the Ops Manager.

1.6.0
- Experiments can now be created in the field by some labs. To that end, experiments have the option to specify what resources they need and how much. If not specified, then a default value will be used that's equal to the experiment mass times 10 in the default resource, or a minimum amount of the default resource, whichever is greater.
- Labs have the ability to restrict the experiments they create based upon a list of tags. Hence experiments may list a set of tags as well. If an experiment has no tags that match the tags required by the lab then it won't show up in the list of experiments that it can create.
- Experiments can now require asteroids with a minimum mass.
NOTE: Basic and DeepFreeze experiments are now located in WildBlueTools; there is no effect to MOLE users.

1.5.0
- Bug fixes and new ice cream flavors.

1.4.2
- Minor fixes to the science system.

1.4.1
- Part mass is now correctly calculated.

1.4.0
- Added animation button prop to control external animations from the IVA.
- The cabin lights button prop can now control external light animations.
- Fixed an issue where resources required by experiments wouldn't be accumulated.

1.3.13
- Added template for Uraninite.

1.3.12
- You can now change the configration on tanks with symmetrical parts. In the SPH/VAB it will happen automatically when you select a new configuration. After launch, you'll have the option to change symmetrical tanks.

1.3.11
- Added WBISelfDestruct and WBIOmniDecouple.
- If fuel tanks are arrayed symmetrically, you'll no longer be able to reconfigure them. It's either that or let the game explode (ie nothing I can do about it except prevent players from changing symmetrical tanks).

1.3.10
- Fixed an issue where the greenhouse wasn't properly calculating the crop growth time.
- Fixed an NRE with lights
- Improved rendering performance for the Operations Manager.

1.3.9
- Fixed an issue with the CryoFuels MM support to avoid duplicate templates.

1.3.8
- Fixed an issue with crew transfers not working after changing a part's crew capacity during a template switch.

1.3.7
- Fixed an issue with WBILight throwing NREs in the VAB/SPH.

1.3.6
- Minor bug fixes

1.3.5
- Fixed an issue where lab experimetns would be completed before even being started.

1.3.4
- Fixed an issue where the "Bonus Science" tab would break the operations manager in the VAB/SPH.

1.3.3 Science Overhaul

- Experiment Manifest and transfer screens now list the part they're associated with.
- Fixed an issue where experiment info wasn't showing up in the VAB/SPH.
- The Load Experiment window now appears slightly offset from the Manifest window to make it easier to distinguish that you're now loading experiments into the part.
- The Transfer Experiment button now makes it more clear that it is a transfer experiment button.
- In the VAB/SPH, the Experiment Manifest will show a new "Load Experiment" button.
- You can now run/pause individual experiments.
- Changed how experiments check for and consume resources; they now go vessel-wide.
- The Experiment Lab will no longer stop if it, say, runs out of resources or the part doesn't have enough crew.
- Improved rendering performance of the experiment windows.
- Fixed an issue where experiments wouldn't show up after reloading a craft.

1.3.2
- New props

1.3.1
- Added Local Operations Manager window to enable controlling all PartModules on all vessels within physics range that implement IOpsView. This is used by mods such as Pathfinder.
- Added Slag and Konkrete resources, templates, and icons.
- Added WBIProspector and WBIAsteroidProcessor. These PartModules can convert non-ElectricCharge resources into all the resources available in a biome/asteroid, and can produce a byproduct resource. A typical use is to convert Ore/Rock into the locally available resources and Slag.
- The Rockhound template now uses the new WBIAsteroidProcessor to convert Ore and/ore Rock.

1.3.0
- Updated to KSP 1.1.3
- Introduced IOpsView to enable command and control of parts from the Operations Manager.
- Refactored the WBIMultiConverter to use a template selector similar to the WBIConvertibleStorage.
- WBIConvertibleStorage/WBIMulticonverter will show you all the templates that a part can use, but templates that you haven't researched yet will be grayed out.

1.2.9
- Removed Dirt from the USI LifeSupport template.
- Added icons for USI-LS templates.
- Added the WBIModuleDecoupler part module that can switch between a decoupler and a separator.

1.2.8
- Fixed an issue where converter text and experiment manifest text wasn't showing up in the VAB/SPH.
- Fixed an issue where you'd see crew portraits in a nearby vessel even though you're focused upon a different vessel.
- Fixed an issue where a science experiment would be run when transferring the experiment out of a lab, even though it hasn't met all the requirements.
- Fixed an issue where a science lab could not transmit data back to KSC when RemoteTech is installed. NOTE: This is a pretty simplistic fix; future updates will account for packet transmission rates etc.

1.2.7
- Fixed issues with USI-LS.

1.2.6
- Added new props

1.2.5
- Improved GUI for selecting resources
- You can now click on the laptop prop's monitor to change the image.

1.2.4
- More Input is NULL error fixes.

1.2.3
- More Input is NULL error fixes.

1.2.2
- Fixed NREs and Input Is NULL errors.

1.2.1
- Minor bug fixes

1.2.0

Science System
- Added a new science system that lets you load experiments into the Coach containers, fly them to your stations and transfer them into a MOLE lab, and once completed, load them back into a Coach for transport back to Kerbin. The experiments have little to no transmit value, encouraging you to bring them home (or if you prefer, load them into an MPL). The new experiments can have many requirements such as orbiting specific planets at specific altitudes, various resources, minimum crew, and required parts. To give players an added challenge, you can optionally specify the percentage chance that an experiment will succeed. You even have the ability to run a specific part module once an experiment has met the prerequisites- that gives you the ability to provide custom benefits. Consult the wiki for more details.

1.1.5
- Adjusted Ore and XenonGas capacities to reflect stock resource volumes.

---LICENSE---
Art Assets, including .mu, .mbm, and .dds files are copyright 2014-2016 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2014-2016 by Michael Billard (Angel-125)

    This source code is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.