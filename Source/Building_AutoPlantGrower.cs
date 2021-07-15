//Building_AutoPlantGrower
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AutoPonics {
    public class Building_AutoPlantGrower : Building, IPlantToGrowSettable
    {
        private ThingDef plantDefToGrow;

        private CompPowerTrader compPower;

        IEnumerable<IntVec3> IPlantToGrowSettable.Cells => this.OccupiedRect().Cells;

        public IEnumerable<Plant> PlantsOnMe
        {
            get
            {
                if (!base.Spawned)
                {
                    yield break;
                }
                CellRect.CellRectIterator cri = this.OccupiedRect().GetIterator();
                while (!cri.Done())
                {
                    List<Thing> thingList = base.Map.thingGrid.ThingsListAt(cri.Current);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        Plant p = thingList[i] as Plant;
                        if (p != null)
                        {
                            yield return p;
                        }
                        if (p == null) {
                            Thing thing = ThingMaker.MakeThing(plantDefToGrow);
                            if (cri.Current.GetPlant(Map) == null) {
                                GenPlace.TryPlaceThing(thing, cri.Current, Map, ThingPlaceMode.Direct, out Thing lastResultingThing);
                                yield return p;
                            }
                        }

                    }
                    cri.MoveNext();
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            yield return PlantToGrowSettableUtility.SetPlantToGrowCommand(this);
        }

        public override void PostMake()
        {
            base.PostMake();
            plantDefToGrow = def.building.defaultPlantToGrow;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compPower = GetComp<CompPowerTrader>();
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.GrowingFood, KnowledgeAmount.Total);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref plantDefToGrow, "plantDefToGrow");
        }

        public override void TickRare()
        {
            if (compPower != null && !compPower.PowerOn)
            {
                foreach (Plant item in PlantsOnMe)
                {
                    if (item != null) {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Rotting, 1f);
                        item.TakeDamage(dinfo);
                    }

                }
            }
            else if(compPower != null && compPower.PowerOn)
            {
                foreach (Plant item in PlantsOnMe)
                {
                    if (item != null && item.Growth == 1.0f)
                    {
                        //Log.Message("Should yield harvest!");
                        int num2 = item.YieldNow();
                        if (num2 > 0)
                        {
                            Thing thing = ThingMaker.MakeThing(item.def.plant.harvestedThingDef);
                            thing.stackCount = num2;
                            thing.SetForbidden(value: false);
                            GenPlace.TryPlaceThing(thing, item.Position, Map, ThingPlaceMode.Near);
                            item.Destroy();
                        }
                    }
                }
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            foreach (Plant item in PlantsOnMe.ToList())
            {
                if(item != null) {
                    item.Destroy();
                }
            }
            base.DeSpawn(mode);
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            if (base.Spawned)
            {
                text = ((!PlantUtility.GrowthSeasonNow(base.Position, base.Map, forSowing: true)) ? (text + "\n" + "CannotGrowBadSeasonTemperature".Translate()) : (text + "\n" + "GrowSeasonHereNow".Translate()));
            }
            return text;
        }

        public ThingDef GetPlantDefToGrow()
        {
            return plantDefToGrow;
        }

        public void SetPlantDefToGrow(ThingDef plantDef)
        {
            plantDefToGrow = plantDef;
        }

        public bool CanAcceptSowNow()
        {
            if (compPower != null && !compPower.PowerOn)
            {
                return false;
            }
            return true;
        }
    }

}
