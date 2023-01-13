using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using Upgradeables;
using KSP.UI.Screens;
using KSP.Localization;
using System.IO;

// Taken from Angel-125's Sandcastle
// https://github.com/Angel-125/Sandcastle

namespace BDB
{
    public class ModuleBdbStorablePart : ModuleCargoPart
    {
        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine(Localizer.Format("#LOC_BDB_storablePartDescription"));
            info.AppendLine(" ");
            info.AppendLine(Localizer.Format("#LOC_BDB_storablePartDryMass", new string[] { string.Format("{0:n3}", part.mass) }));
            info.AppendLine(Localizer.Format("#LOC_BDB_storablePartPackedVolume", new string[] { string.Format("{0:n1}", packedVolume) }));
            if (stackableQuantity > 1)
                info.AppendLine(Localizer.Format("#LOC_BDB_storablePartStackingCapacity", stackableQuantity.ToString()));

            return info.ToString();
        }
    }
    public class ModuleBdbStorableInventory : ModuleInventoryPart
    {
        //public override string GetInfo()
        //{
        //    StringBuilder info = new StringBuilder();

        //    info.AppendLine(Localizer.Format("#LOC_BDB_storablePartDescription"));
        //    info.AppendLine(" ");
        //    info.AppendLine(Localizer.Format("#LOC_BDB_storablePartDryMass", new string[] { string.Format("{0:n3}", part.mass) }));
        //    info.AppendLine(Localizer.Format("#LOC_BDB_storablePartPackedVolume", new string[] { string.Format("{0:n1}", packedVolume) }));
        //    if (stackableQuantity > 1)
        //        info.AppendLine(Localizer.Format("#LOC_BDB_storablePartStackingCapacity", stackableQuantity.ToString()));

        //    return info.ToString();
        //}
    }
}
