﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using RogueEssence.Dungeon;
using RogueEssence.Content;

namespace RogueEssence.Dev
{
    public partial class TilePreview : UserControl
    {
        private static object drawLock = new object();
        private bool runningAnim;
        private Thread animThread;
        private int animFrame;

        private TileLayer chosenAnim;

        public TileLayer GetChosenAnim()
        {
            return new TileLayer(chosenAnim);
        }

        public void SetTileSize(int tileSize)
        {
            lock (drawLock)
            {
                Image img = new Bitmap(tileSize, tileSize);
                pictureBox.Image = img;
                pictureBox.Size = img.Size;
            }
        }

        public void SetChosenAnim(TileBrush brush)
        {
            SetChosenAnim(brush.Layer);
        }

        public void SetChosenAnim(TileLayer anim)
        {
            lock (drawLock)
            {
                chosenAnim = new TileLayer(anim);
                if (animFrame >= chosenAnim.Frames.Count)
                    animFrame = 0;
                UpdatePreviewTile();
            }
        }

        public event EventHandler TileClick
        {
            add { pictureBox.Click += value; }
            remove { pictureBox.Click -= value; }
        }

        public TilePreview()
        {
            InitializeComponent();

            chosenAnim = new TileLayer();
            animThread = new Thread(UpdatePreviewTileTimer);
        }

        protected override void OnLoad(EventArgs e)
        {
            runningAnim = true;
            UpdatePreviewTile();
            animThread.Start();
            
            base.OnLoad(e);
        }


        void UpdatePreviewTileTimer()
        {
            while (true)
            {
                lock (drawLock)
                {
                    if (!runningAnim || !GraphicsManager.Loaded)
                        return;

                    if (chosenAnim.Frames.Count > 1)
                    {
                        animFrame++;
                        if (animFrame >= chosenAnim.Frames.Count)
                            animFrame = 0;

                        UpdatePreviewTile();
                    }
                    else if (animFrame > 0)
                    {
                        animFrame = 0;
                        UpdatePreviewTile();
                    }
                }
                Thread.Sleep(chosenAnim.FrameLength * 1000 / 60);
            }
        }


        void UpdatePreviewTile()
        {
            Image endTileImage = new Bitmap(pictureBox.Width, pictureBox.Height);
            if (chosenAnim.Frames.Count > 0)
            {
                if (chosenAnim.Frames[animFrame] != TileFrame.Empty)
                {
                    using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(endTileImage))
                        graphics.DrawImage(DevTileManager.Instance.GetTile(chosenAnim.Frames[animFrame]), 0, 0);
                }
            }
            pictureBox.Image = endTileImage;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            //end animation thread
            lock (drawLock)
                runningAnim = false;

            base.OnHandleDestroyed(e);
        }
    }
}
