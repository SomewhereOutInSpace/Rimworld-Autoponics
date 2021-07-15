// RimWorld.WorkGiver_GrowerSow
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AutoPonics {
    public class WorkGiver_AutoGrowerSow : WorkGiver_AutoGrower
    {
        protected static string CantSowCavePlantBecauseOfLightTrans;

        protected static string CantSowCavePlantBecauseUnroofedTrans;

        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public static void ResetStaticData()
        {
            CantSowCavePlantBecauseOfLightTrans = "CantSowCavePlantBecauseOfLight".Translate();
            CantSowCavePlantBecauseUnroofedTrans = "CantSowCavePlantBecauseUnroofed".Translate();
        }

        protected override bool ExtraRequirements(IPlantToGrowSettable settable, Pawn pawn)
        {
            if (!settable.CanAcceptSowNow())
            {
                return false;
            }
            Zone_Growing zone_Growing = settable as Zone_Growing;
            IntVec3 c;
            if (zone_Growing != null)
            {
                if (!zone_Growing.allowSow)
                {
                    return false;
                }
                c = zone_Growing.Cells[0];
            }
            else
            {
                c = ((Thing)settable).Position;
            }
            WorkGiver_AutoGrower.wantedPlantDef = WorkGiver_AutoGrower.CalculateWantedPlantDef(c, pawn.Map);
            if (WorkGiver_AutoGrower.wantedPlantDef == null)
            {
                return false;
            }
            return true;
        }

        public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            Map map = pawn.Map;
            if (c.IsForbidden(pawn))
            {
                return null;
            }
            if (!PlantUtility.GrowthSeasonNow(c, map, forSowing: true))
            {
                return null;
            }
            if (WorkGiver_AutoGrower.wantedPlantDef == null)
            {
                WorkGiver_AutoGrower.wantedPlantDef = WorkGiver_AutoGrower.CalculateWantedPlantDef(c, map);
                if (WorkGiver_AutoGrower.wantedPlantDef == null)
                {
                    return null;
                }
            }
            List<Thing> thingList = c.GetThingList(map);
            bool flag = false;
            for (int i = 0; i < thingList.Count; i++)
            {
                Thing thing = thingList[i];
                if (thing.def == WorkGiver_AutoGrower.wantedPlantDef)
                {
                    return null;
                }
                if ((thing is Blueprint || thing is Frame) && thing.Faction == pawn.Faction)
                {
                    flag = true;
                }
            }
            if (flag)
            {
                Thing edifice = c.GetEdifice(map);
                if (edifice == null || edifice.def.fertility < 0f)
                {
                    return null;
                }
            }
            if (WorkGiver_AutoGrower.wantedPlantDef.plant.cavePlant)
            {
                if (!c.Roofed(map))
                {
                    JobFailReason.Is(CantSowCavePlantBecauseUnroofedTrans);
                    return null;
                }
                if (map.glowGrid.GameGlowAt(c, ignoreCavePlants: true) > 0f)
                {
                    JobFailReason.Is(CantSowCavePlantBecauseOfLightTrans);
                    return null;
                }
            }
            if (WorkGiver_AutoGrower.wantedPlantDef.plant.interferesWithRoof && c.Roofed(pawn.Map))
            {
                return null;
            }
            Plant plant = c.GetPlant(map);
            if (plant != null && plant.def.plant.blockAdjacentSow)
            {
                Pawn p = pawn;
                LocalTargetInfo target = plant;
                bool ignoreOtherReservations = forced;
                if (!p.CanReserve(target, 1, -1, null, ignoreOtherReservations) || plant.IsForbidden(pawn))
                {
                    return null;
                }
                return new Job(JobDefOf.CutPlant, plant);
            }
            Thing thing2 = PlantUtility.AdjacentSowBlocker(WorkGiver_AutoGrower.wantedPlantDef, c, map);
            if (thing2 != null)
            {
                Plant plant2 = thing2 as Plant;
                if (plant2 != null)
                {
                    Pawn p = pawn;
                    LocalTargetInfo target = plant2;
                    bool ignoreOtherReservations = forced;
                    if (p.CanReserve(target, 1, -1, null, ignoreOtherReservations) && !plant2.IsForbidden(pawn))
                    {
                        IPlantToGrowSettable plantToGrowSettable = plant2.Position.GetPlantToGrowSettable(plant2.Map);
                        if (plantToGrowSettable == null || plantToGrowSettable.GetPlantDefToGrow() != plant2.def)
                        {
                            return new Job(JobDefOf.CutPlant, plant2);
                        }
                    }
                }
                return null;
            }
            if (WorkGiver_AutoGrower.wantedPlantDef.plant.sowMinSkill > 0 && pawn.skills != null && pawn.skills.GetSkill(SkillDefOf.Plants).Level < WorkGiver_AutoGrower.wantedPlantDef.plant.sowMinSkill)
            {
                return null;
            }
            for (int j = 0; j < thingList.Count; j++)
            {
                Thing thing3 = thingList[j];
                if (thing3.def.BlocksPlanting())
                {
                    Pawn p = pawn;
                    LocalTargetInfo target = thing3;
                    bool ignoreOtherReservations = forced;
                    if (!p.CanReserve(target, 1, -1, null, ignoreOtherReservations))
                    {
                        return null;
                    }
                    if (thing3.def.category == ThingCategory.Plant)
                    {
                        if (!thing3.IsForbidden(pawn))
                        {
                            return new Job(JobDefOf.CutPlant, thing3);
                        }
                        return null;
                    }
                    if (thing3.def.EverHaulable)
                    {
                        return HaulAIUtility.HaulAsideJobFor(pawn, thing3);
                    }
                    return null;
                }
                continue;
            }
            if (WorkGiver_AutoGrower.wantedPlantDef.CanEverPlantAt(c, map) && PlantUtility.GrowthSeasonNow(c, map, forSowing: true))
            {
                Pawn p = pawn;
                LocalTargetInfo target = c;
                bool ignoreOtherReservations = forced;
                if (p.CanReserve(target, 1, -1, null, ignoreOtherReservations))
                {
                    Job job = new Job(JobDefOf.Sow, c);
                    job.plantDefToSow = WorkGiver_AutoGrower.wantedPlantDef;
                    return job;
                }
            }
            return null;
        }
    }
}

