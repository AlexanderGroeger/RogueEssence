﻿using System;
using System.Collections.Generic;
using System.Text;
using RogueEssence.Content;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using System.Drawing;
using RogueElements;
using Avalonia.Controls;
using RogueEssence.Dev.Views;
using RogueEssence.Dev.ViewModels;

namespace RogueEssence.Dev
{
    public class SpawnRangeListEditor : Editor<ISpawnRangeList>
    {
        public override bool DefaultSubgroup => true;
        public override bool DefaultDecoration => false;

        public override void LoadWindowControls(StackPanel control, string name, Type type, object[] attributes, ISpawnRangeList member)
        {
            LoadLabelControl(control, name);

            SpawnRangeListBox lbxValue = new SpawnRangeListBox();
            lbxValue.MaxHeight = 260;
            SpawnRangeListBoxViewModel mv = new SpawnRangeListBoxViewModel();
            lbxValue.DataContext = mv;

            Type elementType = ReflectionExt.GetBaseTypeArg(typeof(ISpawnRangeList<>), type, 0);
            //lbxValue.StringConv = DataEditor.GetStringRep(elementType, new object[0] { });
            //add lambda expression for editing a single element
            mv.OnEditItem += (int index, object element, SpawnRangeListBoxViewModel.EditElementOp op) =>
            {
                DataEditForm frmData = new DataEditForm();
                if (element == null)
                    frmData.Title = name + "/" + "New " + elementType.Name;
                else
                    frmData.Title = name + "/" + element.ToString();

                DataEditor.LoadClassControls(frmData.ControlPanel, "(SpawnRangeList) " + name + "[" + index + "]", elementType, ReflectionExt.GetPassableAttributes(2, attributes), element, true);

                frmData.SelectedOKEvent += () =>
                {
                    element = DataEditor.SaveClassControls(frmData.ControlPanel, name, elementType, ReflectionExt.GetPassableAttributes(2, attributes), true);
                    op(index, element);
                    frmData.Close();
                };
                frmData.SelectedCancelEvent += () =>
                {
                    frmData.Close();
                };

                control.GetOwningForm().RegisterChild(frmData);
                frmData.Show();
            };

            mv.LoadFromList(member);
            control.Children.Add(lbxValue);
        }

        public override ISpawnRangeList SaveWindowControls(StackPanel control, string name, Type type, object[] attributes)
        {
            int controlIndex = 0;
            controlIndex++;
            SpawnRangeListBox lbxValue = (SpawnRangeListBox)control.Children[controlIndex];
            SpawnRangeListBoxViewModel mv = (SpawnRangeListBoxViewModel)lbxValue.DataContext;
            return mv.GetList(type);
        }
    }
}
