﻿using System;
using System.Collections.Generic;
using RogueElements;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;
using RogueEssence.Script;

namespace RogueEssence.Ground
{
    /// <summary>
    /// Entity representing a position on the map that can be used by the game logic for
    /// making entities and etc move to the marker's position.
    /// </summary>
    [Serializable]
    public class GroundMarker : GroundEntity
    {
        public override Color DevEntColoring => Color.OrangeRed;

        public GroundMarker(string name, Loc pos, Dir8 dir)
        {
            EntName = name;
            Position = pos;
            Direction = dir;
            Bounds = new Rect(Position.X, Position.Y, 8, 8); //Static size, so its easier to click on it!
            if (pos == null)
                pos = new Loc(-1,-1);
            if (name == null)
                name = "";
        }
        protected GroundMarker(GroundMarker other) : base(other)
        { }

        public override GroundEntity Clone() { return new GroundMarker(this); }

        public override EEntTypes GetEntityType()
        {
            return EEntTypes.Marker;
        }

        public override bool DevHasGraphics()
        {
            return false;
        }

        /// <summary>
        /// For markers this doesn't do anything
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<LuaEngine.EEntLuaEventTypes> ActiveLuaCallbacks()
        {
            return new List<LuaEngine.EEntLuaEventTypes>();
        }
    }
}
