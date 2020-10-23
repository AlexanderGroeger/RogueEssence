﻿using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Content;
using RogueEssence.Data;
using RogueEssence.Dungeon;
using RogueEssence.Network;

namespace RogueEssence.Menu
{
    public class OfferFeaturesMenu : InteractableMenu
    {
        public SpeakerPortrait Portrait;
        public MenuText Nickname;
        public MenuText Name;
        public MenuText Level;

        public MenuText Elements;

        public MenuDivider MainDiv;
        public MenuText[] Skills;

        public MenuDivider IntrinsicDiv;
        public MenuText Intrinsic;

        public CharData CurrentChar { get; private set; }

        private TradeTeamMenu baseMenu;

        public OfferFeaturesMenu(Rect bounds, TradeTeamMenu baseMenu)
        {
            Bounds = bounds;
            this.baseMenu = baseMenu;

            Portrait = new SpeakerPortrait(new MonsterID(), new EmoteStyle(0),
                Bounds.Start + new Loc(GraphicsManager.MenuBG.TileWidth * 2, GraphicsManager.MenuBG.TileHeight), false);

            Nickname = new MenuText("", Bounds.Start + new Loc(GraphicsManager.MenuBG.TileWidth * 2 + 48, GraphicsManager.MenuBG.TileHeight));
            Name = new MenuText("", Bounds.Start + new Loc(GraphicsManager.MenuBG.TileWidth * 2 + 48, GraphicsManager.MenuBG.TileHeight + VERT_SPACE));

            Level = new MenuText("", Bounds.Start + new Loc(GraphicsManager.MenuBG.TileWidth * 2 + 48, GraphicsManager.MenuBG.TileHeight + VERT_SPACE * 2));

            Elements = new MenuText("", Bounds.Start + new Loc(GraphicsManager.MenuBG.TileWidth * 2, GraphicsManager.MenuBG.TileHeight + VERT_SPACE * 3));

            MainDiv = new MenuDivider(Bounds.Start + new Loc(GraphicsManager.MenuBG.TileWidth, GraphicsManager.MenuBG.TileHeight + VERT_SPACE * 4 - 2), Bounds.End.X - Bounds.X - GraphicsManager.MenuBG.TileWidth * 2);

            Skills = new MenuText[CharData.MAX_SKILL_SLOTS];
            for (int ii = 0; ii < CharData.MAX_SKILL_SLOTS; ii++)
            {
                Skills[ii] = new MenuText("", Bounds.Start + new Loc(GraphicsManager.MenuBG.TileWidth * 2, GraphicsManager.MenuBG.TileHeight + VERT_SPACE * (ii + 4)));
            }

            IntrinsicDiv = new MenuDivider(Bounds.Start + new Loc(GraphicsManager.MenuBG.TileWidth, GraphicsManager.MenuBG.TileHeight + VERT_SPACE * 8 - 2), Bounds.End.X - Bounds.X - GraphicsManager.MenuBG.TileWidth * 2);

            Intrinsic = new MenuText("", Bounds.Start + new Loc(GraphicsManager.MenuBG.TileWidth * 2, GraphicsManager.MenuBG.TileHeight + VERT_SPACE * 8));
        }

        public void SetCurrentChar(CharData chara)
        {
            CurrentChar = chara;
            if (CurrentChar == null)
                return;

            Portrait.Speaker = CurrentChar.BaseForm;
            Nickname.Text = CurrentChar.BaseName;
            Name.Text = CurrentChar.FullFormName;

            BaseMonsterForm formData = DataManager.Instance.GetMonster(CurrentChar.BaseForm.Species).Forms[CurrentChar.BaseForm.Form];

            Level.Text = Text.FormatKey("MENU_TEAM_LEVEL_SHORT", CurrentChar.Level);
            ElementData element1 = DataManager.Instance.GetElement(formData.Element1);
            ElementData element2 = DataManager.Instance.GetElement(formData.Element2);
            string typeString = String.Format("{0}\u2060{1}", element1.Symbol, element1.Name.ToLocal());
            if (formData.Element2 != 00)
                typeString += "/" + String.Format("{0}\u2060{1}", element2.Symbol, element1.Name.ToLocal());

            Elements.Text = typeString;

            for (int ii = 0; ii < CharData.MAX_SKILL_SLOTS; ii++)
            {
                SlotSkill skill = CurrentChar.BaseSkills[ii];
                SkillData data = DataManager.Instance.GetSkill(skill.SkillNum);
                ElementData element = DataManager.Instance.GetElement(data.Data.Element);
                string skillString = "-----";
                if (skill.SkillNum > -1)
                    skillString = String.Format("{0}\u2060{1}", element.Symbol, data.Name.ToLocal());
                Skills[ii].Text = skillString;
            }

            IntrinsicData entry = DataManager.Instance.GetIntrinsic(CurrentChar.BaseIntrinsics[0]);
            Intrinsic.Text = entry.Name.ToLocal();
        }

        public override IEnumerable<IMenuElement> GetElements()
        {

            yield return Portrait;
            yield return Nickname;
            yield return Name;

            yield return Level;
            yield return Elements;

            yield return MainDiv;

            foreach (MenuText skill in Skills)
                yield return skill;

            yield return IntrinsicDiv;
            yield return Intrinsic;
        }

        public override void Update(InputManager input)
        {
            Visible = true;


            NetworkManager.Instance.Update();
            if (NetworkManager.Instance.Status == OnlineStatus.Offline)
            {
                //give offline message in a dialogue
                MenuManager.Instance.RemoveMenu();
                MenuManager.Instance.RemoveMenu();
                MenuManager.Instance.AddMenu(MenuManager.Instance.CreateDialogue(NetworkManager.Instance.ExitMsg), false);
            }
            else
            {
                ActivityTradeTeam tradeTeam = NetworkManager.Instance.Activity as ActivityTradeTeam;

                if (baseMenu.CurrentState == ExchangeState.Viewing)
                {
                    if (input.JustPressed(FrameInput.InputType.Confirm))
                    {
                        baseMenu.CurrentState = ExchangeState.Ready;

                        tradeTeam.SetReady(baseMenu.CurrentState);

                    }
                    else if (input.JustPressed(FrameInput.InputType.Cancel))
                    {
                        GameManager.Instance.SE("Menu/Cancel");
                        MenuManager.Instance.RemoveMenu();

                        baseMenu.CurrentState = ExchangeState.Selecting;

                        tradeTeam.OfferChar(null);
                        tradeTeam.SetReady(baseMenu.CurrentState);
                    }
                }
                else if (baseMenu.CurrentState == ExchangeState.Ready)
                {
                    if (tradeTeam.CurrentState == ExchangeState.Ready)
                    {
                        QuestionDialog dialog = MenuManager.Instance.CreateQuestion(Text.FormatKey("DLG_TRADE_TEAM_ASK", CurrentChar.BaseName, tradeTeam.OfferedChar.BaseName), () =>
                        {
                            baseMenu.CurrentState = ExchangeState.Exchange;
                            tradeTeam.SetReady(baseMenu.CurrentState);
                        }, () =>
                        {
                            baseMenu.CurrentState = ExchangeState.Viewing;
                            tradeTeam.SetReady(baseMenu.CurrentState);
                        });
                        MenuManager.Instance.AddMenu(dialog, true);
                    }
                    else
                    {
                        if (input.JustPressed(FrameInput.InputType.Cancel))
                        {
                            GameManager.Instance.SE("Menu/Cancel");

                            baseMenu.CurrentState = ExchangeState.Viewing;
                            tradeTeam.SetReady(baseMenu.CurrentState);
                        }
                    }
                }
                else if (baseMenu.CurrentState == ExchangeState.Exchange)
                {
                    if (tradeTeam.CurrentState == ExchangeState.Exchange || tradeTeam.CurrentState == ExchangeState.PostTradeWait)
                    {
                        int chosenIndex = baseMenu.CurrentPage * baseMenu.SpacesPerPage + baseMenu.CurrentChoice;

                        DataManager.Instance.Save.ActiveTeam.Assembly[chosenIndex] = new Character(tradeTeam.OfferedChar, DataManager.Instance.Save.ActiveTeam);
                        DataManager.Instance.Save.RegisterMonster(DataManager.Instance.Save.ActiveTeam.Assembly[chosenIndex].BaseForm.Species);

                        baseMenu.CurrentState = ExchangeState.PostTradeWait;
                        tradeTeam.SetReady(baseMenu.CurrentState);
                    }
                    else if (tradeTeam.CurrentState != ExchangeState.Ready)
                    {
                        MenuManager.Instance.AddMenu(MenuManager.Instance.CreateDialogue(Text.FormatKey("DLG_TRADE_CANCELED")), true);

                        baseMenu.CurrentState = ExchangeState.Viewing;
                        tradeTeam.SetReady(baseMenu.CurrentState);
                    }
                }
                else if (baseMenu.CurrentState == ExchangeState.PostTradeWait)
                {
                    if (tradeTeam.CurrentState != ExchangeState.Exchange)
                    {
                        DataManager.Instance.SaveMainGameState();

                        int chosenIndex = baseMenu.CurrentPage * baseMenu.SpacesPerPage + baseMenu.CurrentChoice;

                        bool sendBack = CurrentChar.OriginalUUID == tradeTeam.TargetInfo.UUID;
                        bool receiveBack = tradeTeam.OfferedChar.OriginalUUID == DataManager.Instance.Save.UUID;
                        string sendString = Text.FormatKey(sendBack ? "DLG_TRADE_TEAM_SENT_BACK" : "DLG_TRADE_TEAM_SENT", CurrentChar.BaseName, tradeTeam.TargetInfo.Data.TeamName);
                        string receiveString = Text.FormatKey(receiveBack ? "DLG_TRADE_TEAM_RECEIVED_BACK" : "DLG_TRADE_TEAM_RECEIVED", tradeTeam.OfferedChar.BaseName, tradeTeam.TargetInfo.Data.TeamName);

                        tradeTeam.OfferChar(null);

                        MenuManager.Instance.RemoveMenu();
                        MenuManager.Instance.ReplaceMenu(new TradeTeamMenu(chosenIndex));

                        GameManager.Instance.Fanfare("Fanfare/Note");
                        MenuManager.Instance.AddMenu(MenuManager.Instance.CreateDialogue(
                            () =>
                            {
                                MenuManager.Instance.AddMenu(MenuManager.Instance.CreateDialogue(
                            () => { GameManager.Instance.Fanfare("Fanfare/JoinTeam"); MenuManager.Instance.AddMenu(MenuManager.Instance.CreateDialogue(receiveString), false); },
                            Text.FormatKey("DLG_TRADE_TEAM_AND")), false);
                            },
                            sendString), false);

                        tradeTeam.SetReady(ExchangeState.Selecting);
                    }
                }

                baseMenu.UpdateStatus();
            }
        }
    }
}
