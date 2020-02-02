﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace BDB
{
    public class BdbCustomParams : GameParameters.CustomParameterNode
    {
        public override string Title { get { return Localizer.Format("#BDB_Fileds_Setting_Title"); } }//"Bluedog Design Bureau Options"
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return Localizer.Format("#BDB_Fileds_Setting_SectionName"); } }//"Bluedog Design Bureau"
        public override string DisplaySection { get { return Section; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }
        [GameParameters.CustomParameterUI("#BDB_Fileds_Setting_boiloffEnabled", toolTip = "#BDB_Fileds_Setting_boiloffEnabled_toolTip")]//Cryogenic Boiloff Enabled?__Set to enable boiloff of cryogenic fuel (liquid hydrogen).
        public bool boiloffEnabled = true;
        [GameParameters.CustomFloatParameterUI("#BDB_Fileds_Setting_boiloffMultiplier", toolTip = "#BDB_Fileds_Setting_boiloffMultiplier_toolTip", asPercentage = true, displayFormat = "N1", minValue = 0.0f, maxValue = 1.0f)]//Boiloff Rate__Boiloff Rate Percentage
        public float boiloffMultiplier = 0.5f;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            Debug.Log("Setting difficulty preset");
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    boiloffEnabled = true;
                    boiloffMultiplier = 0.25f;
                    break;

                case GameParameters.Preset.Normal:
                    boiloffEnabled = true;
                    boiloffMultiplier = 0.5f;
                    break;

                case GameParameters.Preset.Moderate:
                    boiloffEnabled = true;
                    boiloffMultiplier = 0.75f;
                    break;

                case GameParameters.Preset.Hard:
                    boiloffEnabled = true;
                    boiloffMultiplier = 1.0f;
                    break;
            }
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (HighLogic.fetch != null)
            {
                if (HighLogic.LoadedScene == GameScenes.MAINMENU || HighLogic.LoadedScene == GameScenes.SETTINGS || HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    return (member.Name == "boiloffEnabled" || boiloffEnabled);
                }
            }
            return false;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }
}
