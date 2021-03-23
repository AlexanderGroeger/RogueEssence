﻿using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.LevelGen;
using RogueEssence.Ground;
using RogueEssence.Data;
using RogueEssence.Script;

namespace RogueEssence.Dungeon
{
    //A zone acts as a single dungeon, with both mapped and generated floors, ground or dungeon mode.
    [Serializable]
    public class Zone
    {
        public LocalText Name;

        public bool NoEXP;
        public int Level;
        public bool LevelCap;
        public bool TeamRestrict;
        public int TeamSize;
        public bool MoneyRestrict;
        public int BagRestrict;
        public int BagSize;

        //we want to be able to load and save maps in both ground and dungeon mode
        //we want to be able to create new maps and save them to a file
        //we want ground and dungeon mode to both be accessible via a structloc, whatever data that structloc may contain

        public List<ZoneSegmentBase> Structures;

        public List<string> GroundMaps;

        private ReRandom rand;

        protected Dictionary<int, ZoneGenContext> structureContexts;
        protected Dictionary<SegLoc, Map> maps;

        public int MapCount { get { return maps.Count; } }

        private int zoneIndex;
        public SegLoc CurrentMapID { get; private set; }
        public Map CurrentMap { get; private set; }

        //include a current groundmap, with moveto methods included
        public GroundMap CurrentGround { get; private set; }

        public List<MapStatus> CarryOver;

        /// <summary>
        /// For containing entire dungeon-related events. (Since we can't handle some of those things inside the dungeon floors themselves)
        /// </summary>
        private Dictionary<LuaEngine.EZoneCallbacks, ScriptEvent> ScriptEvents;

        public Zone(ulong seed, int zoneIndex)
        {
            DiagManager.Instance.LogInfo("Zone Seed: " + seed);
            rand = new ReRandom(seed);

            this.zoneIndex = zoneIndex;
            Name = new LocalText();

            CurrentMapID = SegLoc.Invalid;

            Level = -1;
            TeamSize = -1;
            BagRestrict = -1;
            BagSize = -1;

            structureContexts = new Dictionary<int, ZoneGenContext>();
            maps = new Dictionary<SegLoc, Map>();
            Structures = new List<ZoneSegmentBase>();

            CarryOver = new List<MapStatus>();

            ScriptEvents = new Dictionary<LuaEngine.EZoneCallbacks, ScriptEvent>();
        }

        public void LoadScriptEvents(Dictionary<LuaEngine.EZoneCallbacks, ScriptEvent> scriptEvents)
        {
            ScriptEvents = scriptEvents;
        }

        public void LuaEngineReload()
        {
            foreach (ScriptEvent scriptEvent in ScriptEvents.Values)
                scriptEvent.LuaEngineReload();
            if (CurrentMap != null)
                CurrentMap.LuaEngineReload();
            if (CurrentGround != null)
                CurrentGround.LuaEngineReload();
        }
        public void SaveLua()
        {
            if (CurrentMap != null)
                CurrentMap.SaveLua();
            if (CurrentGround != null)
                CurrentGround.SaveLua();
        }
        public void LoadLua()
        {
            if (CurrentMap != null)
                CurrentMap.LoadLua();
            if (CurrentGround != null)
                CurrentGround.LoadLua();
        }

        private void exitMap()
        {
            if (CurrentMap != null && CurrentMapID.IsValid())//only clean up maps that are valid (aka, not from editor mode)
                CurrentMap.DoCleanup();
            CurrentMap = null;
            if (CurrentGround != null && CurrentMapID.IsValid())//only clean up maps that are valid (aka, not from editor mode)
                CurrentGround.DoCleanup();
            CurrentGround = null;
        }

        public void SetCurrentMap(SegLoc map)
        {
            exitMap();

            if (map.IsValid())
            {
                if (map.Segment > -1)
                    CurrentMap = GetMap(map);
                else
                    CurrentGround = GetGround(map);
            }
            CurrentMapID = map;
        }

        /// <summary>
        /// Finds the mapname in this zone's map list, and loads it.
        /// </summary>
        /// <param name="mapname"></param>
        public void SetCurrentGround(string mapname)
        {
            exitMap();

            int index = GroundMaps.FindIndex((str) => (str == mapname));

            if (index > -1)
                CurrentGround = GetGround(new SegLoc(-1, index));
            else
                throw new Exception(String.Format("Cannot find ground map of name {0} in {1}.", mapname, Name.DefaultText));

            CurrentMapID = new SegLoc(-1, index);
        }


        /// <summary>
        /// Creates a new map of the specified name into the current zone (temporarily) for dev usage.
        /// </summary>
        public void DevNewMap()
        {
            exitMap();

            CurrentMap = new Map();
            CurrentMap.CreateNew(10, 10);
            CurrentMap.EntryPoints.Add(new LocRay8(new Loc(CurrentMap.Width / 2, CurrentMap.Height / 2), Dir8.Down));

            CurrentMapID = new SegLoc(0, -1);
        }


        /// <summary>
        /// Loads a new map of the specified name into the current zone (temporarily) for dev usage.
        /// </summary>
        /// <param name="mapname"></param>
        public void DevLoadMap(string mapname)
        {
            exitMap();

            CurrentMap = DataManager.Instance.GetMap(mapname);
            CurrentMapID = new SegLoc(0, -1);
        }


        /// <summary>
        /// Loads a new ground map of the specified name into the current zone (temporarily) for dev usage.
        /// </summary>
        /// <param name="mapname"></param>
        public void DevLoadGround(string mapname)
        {
            exitMap();

            CurrentGround = DataManager.Instance.GetGround(mapname);
            CurrentMapID = new SegLoc(-1, -1);
        }

        /// <summary>
        /// Creates a new ground map of the specified name into the current zone (temporarily) for dev usage.
        /// </summary>
        public void DevNewGround()
        {
            exitMap();

            CurrentGround = new GroundMap();
            CurrentGround.CreateNew(16, 16, Content.GraphicsManager.DungeonTexSize);
            CurrentMapID = new SegLoc(-1, -1);
        }


        public Map GetMap(SegLoc id)
        {
            if (!maps.ContainsKey(id))
            {
                //NOTE: with the way this is currently done, the random numbers used by the maps end up being related to the random numbers used by the postprocs
                //not that anyone would really notice...
                ReRandom totalRand = new ReRandom(rand.FirstSeed);
                for (int ii = 0; ii < id.Segment; ii++)
                    totalRand.NextUInt64();
                ulong structSeed = totalRand.NextUInt64();
                DiagManager.Instance.LogInfo("Struct Seed: " + structSeed);
                ReRandom structRand = new ReRandom(structSeed);
                for (int ii = 0; ii < id.ID; ii++)
                    structRand.NextUInt64();

                ulong mapSeed = structRand.NextUInt64();

                //load the struct context if it isn't present yet
                if (!structureContexts.ContainsKey(id.Segment))
                {
                    ReRandom initRand = new ReRandom(structSeed);
                    ZoneGenContext newContext = new ZoneGenContext();
                    newContext.CurrentZone = zoneIndex;
                    newContext.CurrentSegment = id.Segment;
                    foreach (ZonePostProc zoneStep in Structures[id.Segment].PostProcessingSteps)
                    {
                        //TODO: find a better way to feed ZonePostProcs into full structures.
                        //Is there a way for them to be stateless?
                        //Additionally, the ZonePostProcs themselves sometimes hold IGenSteps that are copied over to the layouts.
                        //Is that really OK? (I would guess yes because there is no chance by design for them to be mutated when generating...)
                        ZonePostProc newStep = zoneStep.Instantiate(initRand.NextUInt64());
                        newContext.ZoneSteps.Add(newStep);
                    }
                    structureContexts[id.Segment] = newContext;
                }
                ZoneGenContext zoneContext = structureContexts[id.Segment];
                zoneContext.CurrentID = id.ID;
                zoneContext.Seed = mapSeed;

                //TODO: remove the need for this explicit cast
                //make a parameterized version of zonestructure and then make zonestructure itself put in basemapgencontext as the parameter
                Map map = ((BaseMapGenContext)Structures[id.Segment].GetMap(zoneContext)).Map;

                //uncomment this to cache the state of every map after its generation.  it's not nice on memory though...
                //maps.Add(id, map);
                return map;
            }
            return maps[id];
        }
        public GroundMap GetGround(SegLoc id)
        {
            return DataManager.Instance.GetGround(GroundMaps[id.ID]);
        }

        public void UnloadMap(SegLoc id)
        {
            if (maps.ContainsKey(id))
                maps.Remove(id);
        }

        public IEnumerator<YieldInstruction> OnInit()
        {
            string assetName = "zone_" + ZoneManager.Instance.CurrentZoneID;

            DiagManager.Instance.LogInfo("Zone.OnInit(): Initializing the zone..");
            if (assetName != "")
                LuaEngine.Instance.RunZoneScript(assetName);

            //Reload the map events
            foreach (var ev in ScriptEvents)
                ev.Value.ReloadEvent();

            //Do script event
            yield return CoroutineManager.Instance.StartCoroutine(RunScriptEvent(LuaEngine.EZoneCallbacks.Init, this));

            //Notify script engine
            LuaEngine.Instance.OnZoneInit(/*assetName, this*/);
        }

        public IEnumerator<YieldInstruction> OnEnterSegment()
        {
            string assetName = "zone_" + ZoneManager.Instance.CurrentZoneID;

            //Do script event
            yield return CoroutineManager.Instance.StartCoroutine(RunScriptEvent(LuaEngine.EZoneCallbacks.EnterSegment, this, CurrentMapID.Segment, CurrentMapID.ID));

            //Notify script engine
            LuaEngine.Instance.OnZoneSegmentStart(/*assetName, this*/);
        }

        public IEnumerator<YieldInstruction> OnExitSegment(GameProgress.ResultType result, bool rescuing)
        {
            string assetName = "zone_" + ZoneManager.Instance.CurrentZoneID;

            //Do script event
            yield return CoroutineManager.Instance.StartCoroutine(RunScriptEvent(LuaEngine.EZoneCallbacks.ExitSegment, this, result, rescuing, CurrentMapID.Segment, CurrentMapID.ID));

            //Notify script engine
            LuaEngine.Instance.OnZoneSegmentEnd(/*assetName, this*/);
        }

        public IEnumerator<YieldInstruction> OnAllyInteract(Character chara, Character target)
        {
            string assetName = "zone_" + ZoneManager.Instance.CurrentZoneID;

            //Do script event
            yield return CoroutineManager.Instance.StartCoroutine(RunScriptEvent(LuaEngine.EZoneCallbacks.AllyInteract, chara, target, this));

        }

        public IEnumerator<YieldInstruction> OnRescued(SOSMail mail)
        {
            string assetName = "zone_" + ZoneManager.Instance.CurrentZoneID;

            //Do script event
            yield return CoroutineManager.Instance.StartCoroutine(RunScriptEvent(LuaEngine.EZoneCallbacks.Rescued, this, mail));

        }


        public IEnumerator<YieldInstruction> RunScriptEvent(LuaEngine.EZoneCallbacks ev, params object[] parms)
        {
            if (ScriptEvents.ContainsKey(ev))
                yield return CoroutineManager.Instance.StartCoroutine(ScriptEvents[ev].Apply(parms));
        }


        public void DoCleanup()
        {
            exitMap();

            string assetName = "zone_" + zoneIndex;
            DiagManager.Instance.LogInfo(String.Format("Zone.~Zone(): Finalizing {0}..", assetName));

            LuaEngine.Instance.CleanZoneScript(assetName);
        }
    }
}
