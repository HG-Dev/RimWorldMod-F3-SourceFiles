//#define DEBUG
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HG.FFF
{
    public class CompUnloadableSpawner : ThingComp
    {
        public delegate bool TryNextProgress(int elapsedTicks, int totalTicksUntilSpawn, int spawnIndex);

        private CompProperties_UnloadableSpawner Props => (CompProperties_UnloadableSpawner)this.props;

        public const int DEFAULT_TOTAL_TICKS_UNTIL_SPAWN = 999999;
        private int totalTicksUntilSpawn = DEFAULT_TOTAL_TICKS_UNTIL_SPAWN;
        private int ticksElapsed;
        private int containedThingCount;
        private int totalThingsSpawned;
        private bool unloadingEnabled = true;

        [Unsaved]
        private Sustainer audio;

        public const string SIGNAL_WORKING = "UnloadableIsWorking";
        public const string SIGNAL_CANCEL_WORKING = "UnloadableStoppedWorking";
        public const string SIGNAL_UNLOAD_READY = "UnloadableIsReady";
        public const string SIGNAL_CANCEL_UNLOAD_READY = "UnloadableNoLongerReady";
        public const string SIGNAL_CAPACITY_FILLED = "UnloadableAtCapacity";
        public const string SIGNAL_CANCEL_CAPACITY_FILLED = "UnloadableCanSpawn";
        public const string SIGNAL_NEW_PRODUCT = "UnloadableProduced";

        public int CurrentContainedCount => containedThingCount;

        [Unsaved]
        private bool _sufficientAmountForUnload;
        public bool SufficientAmountForUnload
        {
            get
            {
                var value = containedThingCount >= UnityEngine.Mathf.Max(Props.minBeforeUnload, 1);
                if (_sufficientAmountForUnload != value)
                {
#if DEBUG
                    Log.Message("[HG] SufficientAmountForUnload changed. Contains " + containedThingCount);
#endif
                    if (value) parent.BroadcastCompSignal(SIGNAL_UNLOAD_READY);
                    else parent.BroadcastCompSignal(SIGNAL_CANCEL_UNLOAD_READY);
                }
                    
                _sufficientAmountForUnload = value;
                return value;
            }
        }

        [Unsaved]
        private bool _filledToCapacity;
        public bool FilledToCapacity
        {
            get
            {
                var value = containedThingCount >= Props.MaxContained;
                if (value != _filledToCapacity)
                {
#if DEBUG
                    Log.Message($"FTC change: {containedThingCount} vs. {Props.MaxContained}");
#endif
                    if (value) parent.BroadcastCompSignal(SIGNAL_CAPACITY_FILLED);
                    else parent.BroadcastCompSignal(SIGNAL_CANCEL_CAPACITY_FILLED);
                }
                _filledToCapacity = value;
                return value;
            }
        }

        [Unsaved]
        private bool _working;
        public bool Working
        {
            get
            {
                return _working;
            }
            private set
            {
                if (_working != value)
                {
                    parent.BroadcastCompSignal(value ? SIGNAL_WORKING : SIGNAL_CANCEL_WORKING);
                    _working = value;
                }    
            }
        }

        public int MaxContained => Props.MaxContained;
        public bool ShouldUnload => unloadingEnabled && SufficientAmountForUnload;
        public float ProgressToNextBatchSpawn => ticksElapsed / (float)totalTicksUntilSpawn;
        public int TotalThingsSpawned => totalThingsSpawned;

        public float LifetimeSlowdownFactor => Props.CalculateLifetimeSlowdownProgress(TotalThingsSpawned);

        public TryNextProgress StepValidator = CannotValidate;

        private static bool CannotValidate(int _, int __, int ___) => false;

        public override void PostSpawnSetup(bool respawningAfterLoad) => totalTicksUntilSpawn = respawningAfterLoad
            ? totalTicksUntilSpawn
            : Props.spawnIntervalRange.Lerped(LifetimeSlowdownFactor);


        public int TicksUntilItemComplete => totalTicksUntilSpawn - ticksElapsed;

        public override void CompTickRare()
        {
            Working = !FilledToCapacity 
                && StepValidator(ticksElapsed + GenTicks.TickRareInterval, totalTicksUntilSpawn, totalThingsSpawned);

            if (!Working)
                return;

            ticksElapsed += GenTicks.TickRareInterval;
            if (ticksElapsed >= totalTicksUntilSpawn)
            {
                if (TrySpawnBatch() > 0)
                {
                    parent.BroadcastCompSignal(SIGNAL_NEW_PRODUCT);
                    _ = SufficientAmountForUnload;
                }
                
                totalTicksUntilSpawn = Props.spawnIntervalRange.Lerped(LifetimeSlowdownFactor);
                ticksElapsed = 0;
                return;
            }

            if (audio == null || audio.Ended)
            {
                audio = Props.soundWorking.TrySpawnSustainer(SoundInfo.InMap(parent, MaintenanceType.PerTickRare));
            }
        }

        private void DevTrySpawnSingle()
        {
            if (!TrySpawnSingle()) return;

            parent.BroadcastCompSignal(SIGNAL_NEW_PRODUCT);
            _ = SufficientAmountForUnload;
        }
        public bool TrySpawnSingle(bool playSoundEffect = true)
        {
            if (containedThingCount >= Props.MaxContained)
                return false;

            if (playSoundEffect)
                SoundDefOf.CryptosleepCasket_Accept.PlayOneShot(SoundInfo.InMap(parent));

            containedThingCount++;
            totalThingsSpawned++;
            return true;
        }

        private void DevTrySpawnBatch()
        {
            if (TrySpawnBatch() <= 0) return;

            parent.BroadcastCompSignal(SIGNAL_NEW_PRODUCT);
            _ = SufficientAmountForUnload;
        }
        public int TrySpawnBatch(bool playSoundEffect = true)
        {
            if (containedThingCount + Props.spawnBatchCount > Props.MaxContained)
                return 0;

            if (playSoundEffect)
                SoundDefOf.CryptosleepCasket_Accept.PlayOneShot(SoundInfo.InMap(parent));

            containedThingCount += Props.spawnBatchCount;
            totalThingsSpawned += Props.spawnBatchCount;

            return Props.spawnBatchCount;
        }

        public List<Thing> ExtractContentStacks()
        {
            var contents = new List<Thing>();
            var itemsPerStack = Props.thingToSpawn.stackLimit;
            for (var stacksToEmit = Mathf.CeilToInt(containedThingCount / itemsPerStack); stacksToEmit > 0; stacksToEmit--)
            {
                var extractedCount = UnityEngine.Mathf.Min(containedThingCount, Props.thingToSpawn.stackLimit);
                var stack = ThingMaker.MakeThing(Props.thingToSpawn, Props.thingStuff);
                stack.stackCount = extractedCount;
                contents.Add(stack);
                containedThingCount -= extractedCount;
            }

            _ = SufficientAmountForUnload;
            _ = FilledToCapacity;
            return contents;
        }

        public List<Thing> EjectContentsNearby()
        {
            var stacks = ExtractContentStacks();
            
            if (stacks.Count == 0)
            {
                SoundDefOf.FloatMenu_Cancel.PlayOneShot(SoundInfo.InMap(parent));
#if DEBUG
                Log.Warning($"Attempted to eject contents of {nameof(CompUnloadableSpawner)} on {parent.def.defName}, but nothing could be extracted");
#endif
                return new List<Thing>() { ThingMaker.MakeThing(ThingDefOf.ChunkMechanoidSlag) };
            }
            
            SoundDefOf.CryptosleepCasket_Eject.PlayOneShot(SoundInfo.InMap(parent));

            foreach (var stack in stacks)
            {
#if DEBUG
                Log.Message($"Placing {stack.stackCount}-sized stack of {stack.def.defName} near {this.parent.Position}");
#endif
                GenPlace.TryPlaceThing(stack, this.parent.Position, this.parent.Map, ThingPlaceMode.Near);
            }
                
            return stacks;
        }

        public AcceptanceReport CanPawnUnload(Pawn pawn)
        {
            var reachable = pawn.CanReach(parent, Verse.AI.PathEndMode.Touch, Danger.Deadly);
            if (!reachable)
                return "CannotReach".Translate();

            if (SufficientAmountForUnload)
            {
                if (!unloadingEnabled)
                    return "ForbiddenLower".Translate();

                if (parent.IsBurning())
                    return "BurningLower".Translate();

                return true;
            }

            if (Working)
            {
                if (Props.minBeforeUnload > 1)
                    return "FFF.BatchInProgress".Translate(Props.minBeforeUnload);

                return "FermentationProgress".Translate(ProgressToNextBatchSpawn.ToStringPercent(),
                    Props.minBeforeUnload - containedThingCount);
            }

            return "InactiveFacility".Translate();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            audio?.End();
            base.PostDeSpawn(map, mode);
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<int>(ref this.totalTicksUntilSpawn, "tus", DEFAULT_TOTAL_TICKS_UNTIL_SPAWN, false);
            Scribe_Values.Look<int>(ref this.containedThingCount, "ctc", 0, false);
            Scribe_Values.Look<int>(ref this.totalThingsSpawned, "tts", 0, false);
            Scribe_Values.Look<bool>(ref this.unloadingEnabled, "ue", true, false);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Toggle
            {
                defaultLabel = "FFF.ToggleUnloading".Translate(),
                defaultDesc = "FFF.ToggleUnloadingDesc".Translate(),
                isActive = () => this.unloadingEnabled,
                toggleAction = () => this.unloadingEnabled = !this.unloadingEnabled,
                activateSound = SoundDefOf.Tick_Tiny,
                icon = unloadingEnabled ? TexCommand.ForbidOff : TexCommand.ForbidOn
            };
            if (this.containedThingCount >= 1)
            {
                yield return new Command_Action
                {
                    defaultLabel = "FFF.EjectContents".Translate(),
                    defaultDesc = "FFF.EjectContentsDesc".Translate(Find.ActiveLanguageWorker.Pluralize(Props.thingToSpawn.label, containedThingCount)),
                    action = () => EjectContentsNearby(),
                    Disabled = (this.containedThingCount < 1),
                    activateSound = SoundDefOf.Tick_Tiny,
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject", true)
            };
            }

            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: +1 " + this.Props.thingToSpawn.label,
                    icon = TexCommand.Install,
                    action = DevTrySpawnSingle
                };
                var batchCount = Props.spawnBatchCount;
                if (batchCount > 1)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = $"DEV: +{batchCount} {Props.thingToSpawn.label}",
                        icon = TexCommand.Install,
                        action = DevTrySpawnBatch
                    };
                }
            }
            yield break;
        }
    }
}
