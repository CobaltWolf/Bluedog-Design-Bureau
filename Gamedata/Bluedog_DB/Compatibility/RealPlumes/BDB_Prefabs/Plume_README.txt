//////Custom Real Plumes for BDB //////////
//////Authored by Zorg //////////

Readme v 0.5, July 7 2019

Since BDB "stock" plumes now use beautiful effects created by Jade Of Maar for Plume Party, a project was undertaken to replace the existing realplumes with custom ones referring these new effects.
In addition to looking very nice, in many cases these effects are a lot more efficient than the old realplume ones in that they can achieve a given look using few particles
Note that the Plume Party mod is not a dependency for these effects, particle FX originally designed for Plume Party are bundled within the BDB FX folder

Since mod authors may not always have time to maintain realplume configs, this readme will outline the method behind the madness of this project :P in case other community members wish to contribute in the future.

Note on RealPlume:
- Nearly everything RealPlume does is via the SmokeScreen plugin.
- It bundles a set of prefabricated plumes containing smokescreen configs
- These plumes reference particle effects and sound effects bundled WITHIN the RealPlume mod folder
- It also has module manager patch which removes stock effects globally for any engine with a realplume config prior to applying new effects

////////Plume prefabs///////////////
This project utilises custom smokescreen prefab configs located within Bluedog_DB/Compatibility/RealPlumes/BDB_Prefabs
Engine configs work pretty much the same as any other realPlume config except they reference these prefabs instead
Some plumes use effect names and parameters that might look unfamiliar to what you usually see in RealPlume [Plume, flare, plumeboundary etc]. You might see effects named [Stream, Blaze, fume etc] in BDB configs.
You can see the parameters required to configure an engine inside each particle effect node in the preFab. Though its easier to find an another engine config that uses the same plume and work from there

eg the below effect is configured by
Specific to this effect:
- blazePosition
- blazeScale

Global:
- localRotation
- energy
- speed
- emissionMult

MODEL_MULTI_SHURIKEN_PERSIST
{
    //Get the inputs from the other config.
    transformName = #$../../../PLUME[BDB_KeroloxLower_Blaze]/transformName$
    localRotation = #$../../../PLUME[BDB_KeroloxLower_Blaze]/localRotation[0]$,$../../../PLUME[BDB_KeroloxLower_Blaze]/localRotation[1]$,$../../../PLUME[BDB_KeroloxLower_Blaze]/localRotation[2]$
    localPosition = #$../../../PLUME[BDB_KeroloxLower_Blaze]/blazePosition[0]$,$../../../PLUME[BDB_KeroloxLower_Blaze]/blazePosition[1]$,$../../../PLUME[BDB_KeroloxLower_Blaze]/blazePosition[2]$
    fixedScale    = #$../../../PLUME[BDB_KeroloxLower_Blaze]/blazeScale$
    energy        = #$../../../PLUME[BDB_KeroloxLower_Blaze]/energy$
    speed         = #$../../../PLUME[BDB_KeroloxLower_Blaze]/speed$
    emissionMult  = #$../../../PLUME[BDB_KeroloxLower_Blaze]/emissionMult$

---snip---
}

Some plumes might require nearly every parameter such as speed, energy and emissionMult to be configured separately for each effect (eg blazeEnergy)
This is from the first set of messy configs created for this project. These are being phased out with much simplified prefabs that allow for cleaner and easier engine configs

/////// atmoshphere and power keys //////////
SmokeScreen allows particle behaviour to respond to a variety of parameters. For expanding realplumes, we only need to configure them against atmosphere curves (density) and throttle setting curves (power)
Standard reaplume prefabs use curves with keys defined such as the one below:

speed
{
  density = 1.0 6
  density = 0.7 5
  density = 0.1 4
  density = 0.01 3
  density = 0.0 1.5
  power = 1.0 1
  power = 0.0 1
}
The first number is the key and the second is the value. eg first line is For atmo density of 1 (sea level), set speed of particle to 6

For ease of use and for consistency BDB prefabs uses a variable for the key the value for which is defined in a seperate BDB_PLume_Keys.cfg

eg:

logGrow
{
density = #$@BDBPlume/atmosphereKeys/key0$ 0
density = #$@BDBPlume/atmosphereKeys/key1$ 3
density = #$@BDBPlume/atmosphereKeys/key2$ 3
density = #$@BDBPlume/atmosphereKeys/key3$ 3
density = #$@BDBPlume/atmosphereKeys/key4$ 3
density = #$@BDBPlume/atmosphereKeys/key5$ 4
}

Please refer to BDB_PLume_Keys.cfg for additional documentation.
This system faciliates better consistency across prefabs in expansion and throttle behaviour, furthermore I think it makes the prefab configs easier to read and work with

As of this writing the process to convert the BDB realplumes to this system is ongoing.

////////additional effects///////////////
Some engines have custom additional effects such as gas generator effects eg Delta II RS27. There isnt a dedicated gas generator transform to apply a plume to in most such cases.
Therefore the plume is set up within the engine config instead of via a prefab. The effects are added to an exisitng plume on the thrust transform and then offset using the positioning parameters to line up with the exhaust on the model


////////possible future/////////
BDB could in theory in the future have expanding smokescreen plumes without needing realplume. However this project is not at that state for the following reasons nor is it actually a goal, just mentioning the possibility here
- A trailing smoke config (particles whose behaviour is relative to the planet) has not been created of sufficient quality to replace the smoke effects in RealPlumes SRB configs. As such all BDB solids use standard RealPlume prefabs for now
- A few BDB prefabs (mainly kerolox) use a smoke particle effect from RealPlume

To make that transition
- Good SRB plumes with trailing smoke from plume party needs to be created
- Any effects referencing RealPlume particles within BDB prefabs need to be replaced with bundled BDB ones.
- All BDB realplume prefabs currently reference RealPlume sound FX. These would need to be replaced with bundled ones.
- A module manager patch needs to be created to remove stock effects for any engine with a BDB smokescreen config (currently taken care of by RealPlume)
- Some changes to the module manager patching to use only SmokeScreen as a dependency
