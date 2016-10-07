using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace BDB
{
    public class BdbCustomParams : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "Bluedog Design Bureau Options"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Bluedog Design Bureau"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }
        [GameParameters.CustomParameterUI("Cryogenic Boiloff Enabled?", toolTip = "Set to enable boiloff of cryogenic fuel (liquid hydrogen).")]
        public bool boiloffEnabled = true;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            Debug.Log("Setting difficulty preset");
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    boiloffEnabled = false;
                    break;

                case GameParameters.Preset.Normal:
                    boiloffEnabled = true;
                    break;

                case GameParameters.Preset.Moderate:
                    boiloffEnabled = true;
                    break;

                case GameParameters.Preset.Hard:
                    boiloffEnabled = true;
                    break;
            }
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            return true;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }
}
