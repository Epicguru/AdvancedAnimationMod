﻿using RimWorld;
using Verse;

namespace AAM
{
    [DefOf]
    public static class AAM_DefOf
    {
        static AAM_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AAM_DefOf));
        }

        public static JobDef AAM_InAnimation;
    }
}
