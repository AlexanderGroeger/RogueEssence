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
using System.Collections;
using Avalonia;
using System.Reactive.Subjects;
using RogueEssence.Dev.ViewModels;

namespace RogueEssence.Dev
{
    public class ArrayEditor : Editor<Array>
    {
        public override bool DefaultSubgroup => true;
        public override bool DefaultDecoration => false;

        public override void LoadWindowControls(StackPanel control, string name, Type type, object[] attributes, Array member)
        {
            LoadLabelControl(control, name);

            CollectionBox lbxValue = new CollectionBox();
            lbxValue.MaxHeight = 180;
            CollectionBoxViewModel mv = new CollectionBoxViewModel();
            lbxValue.DataContext = mv;

            Type elementType = type.GetElementType();
            //lbxValue.StringConv = GetStringRep(elementType, ReflectionExt.GetPassableAttributes(1, attributes));
            //add lambda expression for editing a single element
            mv.OnEditItem += (int index, object element, CollectionBoxViewModel.EditElementOp op) =>
            {
                DataEditForm frmData = new DataEditForm();
                if (element == null)
                    frmData.Title = name + "/" + "New " + elementType.Name;
                else
                    frmData.Title = name + "/" + element.ToString();

                DataEditor.LoadClassControls(frmData.ControlPanel, "(Array) " + name + "[" + index + "]", elementType, ReflectionExt.GetPassableAttributes(0, attributes), element, true);

                frmData.SelectedOKEvent += () =>
                {
                    element = DataEditor.SaveClassControls(frmData.ControlPanel, name, elementType, ReflectionExt.GetPassableAttributes(0, attributes), true);
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


            List<object> objList = new List<object>();
            for (int ii = 0; ii < member.Length; ii++)
                objList.Add(member.GetValue(ii));

            mv.LoadFromList(objList);
            control.Children.Add(lbxValue);
        }


        public override Array SaveWindowControls(StackPanel control, string name, Type type, object[] attributes)
        {
            int controlIndex = 0;
            //TODO: 2D array grid support
            //if (type.GetElementType().IsArray)

            controlIndex++;
            CollectionBox lbxValue = (CollectionBox)control.Children[controlIndex];
            CollectionBoxViewModel mv = (CollectionBoxViewModel)lbxValue.DataContext;
            List<object> objList = (List<object>)mv.GetList(typeof(List<object>));

            Array array = Array.CreateInstance(type.GetElementType(), objList.Count);
            for (int ii = 0; ii < objList.Count; ii++)
                array.SetValue(objList[ii], ii);

            return array;
        }
    }
}
