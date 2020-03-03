// RimWorld.WorkGiver_Grower
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AutoPonics {
    public abstract class WorkGiver_AutoGrower : WorkGiver_Scanner
    {
        protected static ThingDef wantedPlantDef;

        public override bool AllowUnreachable => true;

        protected virtual bool ExtraRequirements(IPlantToGrowSettable settable, Pawn pawn)
        {
            return true;
        }

        public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
        {
            Danger maxDanger = pawn.NormalMaxDanger();
            List<Building> bList = pawn.Map.listerBuildings.allBuildingsColonist;
            for (int k = 0; k < bList.Count; k++)
            {
                Building_AutoPlantGrower b = bList[k] as Building_AutoPlantGrower;
                if (b != null && ExtraRequirements(b, pawn) && !b.IsForbidden(pawn) && pawn.CanReach(b, PathEndMode.OnCell, maxDanger) && !b.IsBurning())
                {
                    CellRect.CellRectIterator cri = b.OccupiedRect().GetIterator();
                    while (!cri.Done())
                    {
                        yield return cri.Current;
                        cri.MoveNext();
                    }
                    wantedPlantDef = null;
                }
            }
            wantedPlantDef = null;
            List<Zone> zonesList = pawn.Map.zoneManager.AllZones;
            for (int j = 0; j < zonesList.Count; j++)
            {
                Zone_Growing growZone = zonesList[j] as Zone_Growing;
                if (growZone == null)
                {
                    continue;
                }
                if (growZone.cells.Count == 0)
                {
                    Log.ErrorOnce("Grow zone has 0 cells: " + growZone, -563487);
                }
                else if (ExtraRequirements(growZone, pawn) && !growZone.ContainsStaticFire && pawn.CanReach(growZone.Cells[0], PathEndMode.OnCell, maxDanger))
                {
                    for (int i = 0; i < growZone.cells.Count; i++)
                    {
                        yield return growZone.cells[i];
                    }
                    wantedPlantDef = null;
                }
            }
            wantedPlantDef = null;
        }

        public static ThingDef CalculateWantedPlantDef(IntVec3 c, Map map)
        {
            return c.GetPlantToGrowSettable(map)?.GetPlantDefToGrow();
        }
    }
}

