using System;
using System.Collections.Generic;
using XLib.XLeveling;

namespace XSkills
{
    public class XSkill : Skill
    {
        //Stores skills that hasn't received the configurations from the server.
        static protected List<string> MissingConfig;

        public XSkill(string name, string displayName = null, string group = null, int expBase = 200, float expMult = 1.33f, int maxLevel = 25) :
            base(name, displayName, group, expBase, expMult, maxLevel) 
        {
            if (MissingConfig is null) MissingConfig = new List<String>();
            foreach (string skillName in MissingConfig)
            {
                if (skillName == Name) return;
            }
            MissingConfig.Add(Name);
        }

        public override void OnConfigReceived()
        {
            MissingConfig.Remove(Name);
            if (MissingConfig.Count == 0)
            {
                XSkills.DoHarmonyPatch(XLeveling.Api);
            }
        }
    }
}
