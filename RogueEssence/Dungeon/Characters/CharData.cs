﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NLua;
using RogueEssence.Data;

namespace RogueEssence.Dungeon
{

    [Serializable]
    public class CharData
    {

        public const int MAX_SKILL_SLOTS = 4;
        public const int MAX_INTRINSIC_SLOTS = 1;

        public string Nickname;
        public string BaseName
        {
            get
            {
                if (String.IsNullOrEmpty(Nickname))
                    return DataManager.Instance.GetMonster(BaseForm.Species).Name.ToLocal();
                else
                    return Nickname;
            }
        }

        public string OriginalUUID;
        public string OriginalTeam;

        public static string GetFullFormName(MonsterID id)
        {
            string name = DataManager.Instance.GetMonster(id.Species).Name.ToLocal();
            SkinData data = DataManager.Instance.GetSkin(id.Skin);
            if (data.Symbol != '\0')
                name = data.Symbol + name;
            if (id.Gender != Gender.Genderless)
            {
                char genderChar = (id.Gender == Gender.Male) ? '\u2642' : '\u2640';
                if (name[name.Length - 1] != genderChar)
                    name += genderChar;
            }
            return name;
        }


        public MonsterID BaseForm;

        public int Level;
        public int EXP;

        //permanent stat boosts
        public int MaxHPBonus;
        public int AtkBonus;
        public int DefBonus;
        public int MAtkBonus;
        public int MDefBonus;
        public int SpeedBonus;

        public List<SlotSkill> BaseSkills;

        public List<int> BaseIntrinsics;

        public List<bool> Relearnables;

        public int Discriminator;

        public string MetAt;
        //public int MetDungeon;
        //public StructMap MetFloor;
        public string DefeatAt;
        //public int DefeatDungeon;
        //public StructMap DefeatFloor;
        //cannot release founder
        public bool IsFounder;
        public bool IsFavorite;

        public string ScriptVars;
        [NonSerialized]
        public LuaTable LuaDataTable;


        public CharData()
        {
            Nickname = "";
            OriginalUUID = "";
            OriginalTeam = "";
            BaseSkills = new List<SlotSkill>();
            for (int ii = 0; ii < MAX_SKILL_SLOTS; ii++)
                BaseSkills.Add(new SlotSkill());
            BaseIntrinsics = new List<int>();
            for (int ii = 0; ii < MAX_INTRINSIC_SLOTS; ii++)
                BaseIntrinsics.Add(0);
            Relearnables = new List<bool>();

            MetAt = "";
            //MetDungeon = -1;
            //MetFloor = new StructMap(-1, -1);
            DefeatAt = "";
            //DefeatedDungeon = -1;
            //DefeatedFloor = new StructMap(-1,-1);
            LuaDataTable = Script.LuaEngine.Instance.RunString("return {}").First() as LuaTable;
        }

        public CharData(CharData other)
        {
            //clean variables
            Nickname = other.Nickname;
            BaseForm = other.BaseForm;
            Level = other.Level;

            MaxHPBonus = other.MaxHPBonus;
            AtkBonus = other.AtkBonus;
            DefBonus = other.DefBonus;
            MAtkBonus = other.MAtkBonus;
            MDefBonus = other.MDefBonus;
            SpeedBonus = other.SpeedBonus;

            BaseSkills = new List<SlotSkill>();
            foreach (SlotSkill skill in other.BaseSkills)
                BaseSkills.Add(new SlotSkill(skill));
            BaseIntrinsics = new List<int>();
            BaseIntrinsics.AddRange(other.BaseIntrinsics);
            Relearnables = new List<bool>();
            Relearnables.AddRange(other.Relearnables);

            OriginalUUID = other.OriginalUUID;
            OriginalTeam = other.OriginalTeam;
            MetAt = other.MetAt;
            //MetDungeon = other.MetDungeon;
            //MetFloor = other.MetFloor;
            DefeatAt = other.DefeatAt;
            //FaintDungeon = other.FaintDungeon;
            //FaintFloor = other.FaintFloor;
            IsFounder = other.IsFounder;
            IsFavorite = other.IsFavorite;
            Discriminator = other.Discriminator;

            //TODO: deep copy?
            LuaDataTable = other.LuaDataTable;
        }



        public virtual void Promote(MonsterID data)
        {
            MonsterData dex = DataManager.Instance.GetMonster(BaseForm.Species);
            BaseMonsterForm form = dex.Forms[BaseForm.Form];

            BaseForm = data;

            int prevIndex = 0;
            if (form.Intrinsic2 == BaseIntrinsics[0])
                prevIndex = 1;
            else if (form.Intrinsic3 == BaseIntrinsics[0])
                prevIndex = 2;

            BaseIntrinsics.Clear();
            
            MonsterData newDex = DataManager.Instance.GetMonster(BaseForm.Species);
            BaseMonsterForm newForm = newDex.Forms[BaseForm.Form];

            if (prevIndex == 2 && newForm.Intrinsic3 == 0)
                prevIndex = 0;
            if (prevIndex == 1 && newForm.Intrinsic2 == 0)
                prevIndex = 0;

            if (prevIndex == 0)
                BaseIntrinsics.Add(newForm.Intrinsic1);
            else if (prevIndex == 1)
                BaseIntrinsics.Add(newForm.Intrinsic2);
            else if (prevIndex == 2)
                BaseIntrinsics.Add(newForm.Intrinsic3);
        }

        public void SaveLua()
        {
            string serialized = Script.LuaEngine.Instance.SerializeLuaTable(LuaDataTable);
            if (serialized != null)
                ScriptVars = serialized;
            else
                DiagManager.Instance.LogInfo("CharData.OnSerializing(): Couldn't serialize lua data table!!");
        }

        public void LoadLua()
        {
            if (ScriptVars == null)
            {
                LuaDataTable = Script.LuaEngine.Instance.RunString("return {}").First() as LuaTable;
                return;
            }

            LuaDataTable = Script.LuaEngine.Instance.DeserializedLuaTable(ScriptVars);
            if (LuaDataTable == null)
            {
                //Make sure thers is at least a table in the data table when done deserializing.
                DiagManager.Instance.LogInfo(String.Format("CharData.OnDeserialized(): Couldn't deserialize LuaDataTable string '{0}'!", ScriptVars));
                LuaDataTable = Script.LuaEngine.Instance.RunString("return {}").First() as LuaTable;
            }
        }
    }
}
