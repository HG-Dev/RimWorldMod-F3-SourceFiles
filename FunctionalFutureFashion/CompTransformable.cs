using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HG.FFF
{
    public class CompTransformable : ThingComp
    {
        [Serializable]
        public struct SignalPair
        {
            public string signal;
            public ThingDef thingToBecome;
        }

        public class Bill_Upgrade : Bill_ProductionWithUft
        {
            public Bill_Upgrade()
            {
            }
            
            public Bill_Upgrade(RecipeDef recipe, Precept_ThingStyle precept = null)
                : base(recipe, precept)
            {
            }
            
            public Action onComplete;

            public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
            {
                base.Notify_IterationCompleted(billDoer, ingredients);
                onComplete?.Invoke();
            }
        }
        
        private CompProperties_Transformable Props { get; set; }
        private const StringComparison IgnoreCase = StringComparison.InvariantCultureIgnoreCase;

        public IReadOnlyList<RecipeDefExt> PotentialTransformations { get; private set; }
        private Bill_Upgrade _activeTransformationBill = null;
        private int _activeTransformationIndex = -1;

        public bool HasActiveTransformationBill(out Bill_Upgrade bill, out int index)
        {
            bill = _activeTransformationBill;
            index = _activeTransformationIndex;
            return index >= 0 && bill is { deleted: false };
        }
        
        public override void Initialize(CompProperties iprops)
        {
            base.Initialize(iprops);
            Props = (CompProperties_Transformable)iprops;
            List<RecipeDefExt> list = new(Props.recipes.Count);
            foreach (var recipe in Props.recipes)
            {
                if (!recipe.HasModExtension<RecipeTransformableExtension>())
                {
                    Log.Warning($"{nameof(CompTransformable)} -- Recipe {recipe.defName} has no {nameof(RecipeTransformableExtension)} to specify def to become");
                    continue;
                }
                
                list.Add(new RecipeDefExt(recipe));
            }

            PotentialTransformations = list;
            
            if (Props.AnySignalTransform(CompProperties_Transformable.TRANSFORM_ON_INIT, out var thingDef))
            {
                Log.Warning($"{nameof(CompTransformable)} -- scheduling {parent.ThingID} to become {thingDef.defName}");
                LongEventHandler.QueueLongEvent(() => this.ReceiveCompSignal(CompProperties_Transformable.TRANSFORM_ON_INIT), parent.ThingID, false,
                    exception => { });
            }

            if (parent is not IBillGiver billGiver)
                return;
            
            foreach (var bill in billGiver.BillStack)
            {
                if (bill is Bill_Upgrade upgrade && Props.recipes.Contains(bill.recipe))
                {
                    _activeTransformationBill = upgrade;
                    _activeTransformationIndex =
                        PotentialTransformations.FirstIndexOf(rde => rde.Recipe == bill.recipe);
                    upgrade.onComplete = TransformUsingActiveBill;
                }
            }
        }

        public void DeleteTransformationBills()
        {
            if (parent is not IBillGiver billGiver)
                return;
            
            foreach (var bill in billGiver.BillStack)
                bill.suspended = false;

            if (HasActiveTransformationBill(out var activeBill, out _))
                billGiver.BillStack.Delete(activeBill);

            _activeTransformationBill = null;
            _activeTransformationIndex = -1;
                
            var billsToDelete = billGiver.BillStack.Bills.Where(bill => Props.recipes.Contains(bill.recipe)).ToArray();
            foreach (var toDelete in billsToDelete)
                billGiver.BillStack.Delete(toDelete);
        }
        
        public void CreateTransformationBill(int listIndex)
        {
            if (listIndex < 0 || listIndex >= PotentialTransformations.Count)
            {
                Log.Warning("Invalid transformation index");
                return;
            }

            if (parent is not IBillGiver billGiver)
                return;

            foreach (var bill in billGiver.BillStack)
                bill.suspended = true;
            
            if (parent is Building_WorkTableAutonomous autoTable)
            {
                autoTable.EjectContents();
            }
            if (parent.TryGetComp<CompPowerTrader>(out var power))
            {
                power.PowerOn = false;
                power.PowerOutput = 0;
            }
            
            _activeTransformationIndex = listIndex;
            _activeTransformationBill =
                new Bill_Upgrade(PotentialTransformations[listIndex].Recipe, parent.GetStyleSourcePrecept())
                {
                    onComplete = TransformUsingActiveBill
                };
            _activeTransformationBill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
            billGiver.BillStack.AddBill(_activeTransformationBill);
        }

        public override void ReceiveCompSignal(string signal)
        {
            if (parent.DestroyedOrNull() || !Props.AnySignalTransform(signal, out var thingToBecome))
                return;
            
            Transform(thingToBecome, parent.Stuff);
        }

        protected void TransformUsingActiveBill()
        {
            if (!HasActiveTransformationBill(out var activeBill, out _))
            {
                Log.Error("ActiveTransformBill finished, but it was marked null before information could be obtained");
                return;
            }
            var info = activeBill.recipe.GetModExtension<RecipeTransformableExtension>();
            // TODO: Figure out how stuff is derived from bill so upgrades can change stuff
            Transform(info.thingToBecome, parent.Stuff);
        }

        protected void Transform(ThingDef thingToBecome, ThingDef stuff)
        {
            var map = parent.MapHeld;
            var container = parent.TryGetInnerInteractableThingOwner();
            var thing = ThingMaker.MakeThing(thingToBecome, stuff);
            thing.SetFactionDirect(parent.Faction);
            var nextContainer = thing.TryGetInnerInteractableThingOwner();
            if (nextContainer != null)
            {
                container?.TryTransferAllToContainer(nextContainer);
            }
            else
            {
                container?.TryDropAll(parent.Position, map, ThingPlaceMode.Near);
            }
            
            parent.Destroy(DestroyMode.WillReplace);
            thing = GenSpawn.Spawn(thing, parent.Position, map, parent.Rotation);
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                pawn.pather.NotifyThingTransformed(parent, thing);
            }
        }
    }
}