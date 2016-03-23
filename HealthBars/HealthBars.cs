﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Inheritance;
using StardewValley;
using StardewValley.Monsters;

namespace HealthBars
{
    public class HealthBars : Mod
    {
        public static SGame TheGame => Program.gamePtr;

        public static HealthBarConfig ModConfig { get; set; }
        public static List<Monster> monsters = new List<Monster>();

        public static RenderTarget2D RTarg { get; set; }

        Texture2D texBar;

        public override void Entry(params object[] objects)
        {
            ModConfig = new HealthBarConfig().InitializeConfig(BaseConfigPath);

            int innerBarWidth = ModConfig.BarWidth - ModConfig.BarBorderWidth * 2;
            int innerBarHeight = ModConfig.BarHeight - ModConfig.BarBorderHeight * 2;

            GameEvents.FirstUpdateTick += (sender, args) =>
            {
                texBar = new Texture2D(Game1.graphics.GraphicsDevice, innerBarWidth, innerBarHeight);
                var data = new uint[innerBarWidth * innerBarHeight];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = 0xffffffff;
                }
                texBar.SetData<uint>(data);
            };
            GraphicsEvents.DrawTick += GraphicsEvents_DrawTick;
            LocationEvents.CurrentLocationChanged += LocationEvents_CurrentLocationChanged;

            Log.Info("Health Bars by Zoryn => Initialized");
        }

        private void LocationEvents_CurrentLocationChanged(object sender, EventArgsCurrentLocationChanged e)
        {
            var gameLoc = Game1.currentLocation;
            if (gameLoc == null)
                return;

            try
            {
                monsters.Clear();
                foreach (var v in gameLoc.characters)
                {
                    if (v is Monster)
                    {
                        monsters.Add(v as Monster);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private void GraphicsEvents_DrawTick(object sender, EventArgs e)
        {
            if (monsters.Count < 1 || Game1.activeClickableMenu != null)
                return;

            var font = Game1.smallFont;
            var batch = Game1.spriteBatch;
            var viewport = Game1.viewport;


            if (!Game1.options.zoomLevel.Equals(1.0f))
            {
                if (TheGame.Screen.RenderTargetUsage == RenderTargetUsage.DiscardContents)
                {
                    TheGame.Screen = new RenderTarget2D(Game1.graphics.GraphicsDevice, Math.Min(4096, (int)((double)TheGame.Window.ClientBounds.Width * (1.0 / (double)Game1.options.zoomLevel))),
                     Math.Min(4096, (int)((double)TheGame.Window.ClientBounds.Height * (1.0 / (double)Game1.options.zoomLevel))),
                        false, SurfaceFormat.Color, DepthFormat.Depth16, 1, RenderTargetUsage.PreserveContents);
                }
                TheGame.GraphicsDevice.SetRenderTarget(TheGame.Screen);
            }

            batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

            foreach (var monster in monsters)
            {
                if (monster.maxHealth < monster.health)
                {
                    monster.maxHealth = monster.health;
                }

                if (monster.maxHealth == monster.health && !ModConfig.DisplayHealthWhenNotDamaged)
                    continue;

                var animSprite = monster.Sprite;

                var size = new Vector2(animSprite.spriteWidth, animSprite.spriteHeight) * Game1.pixelZoom;
                
                var screenLoc = monster.Position - new Vector2(viewport.X, viewport.Y);
                screenLoc.X += size.X / 2 - ModConfig.BarWidth / 2.0f;
                screenLoc.Y -= ModConfig.BarHeight;

                var fill = monster.health / (float)monster.maxHealth;

                batch.Draw(texBar, screenLoc + new Vector2(ModConfig.BarBorderWidth, ModConfig.BarBorderHeight), texBar.Bounds, Color.Lerp(ModConfig.LowHealthColor, ModConfig.HighHealthColor, fill), 0.0f, Vector2.Zero, new Vector2(fill, 1.0f), SpriteEffects.None, 0);

                if (ModConfig.DisplayCurrentHealthNumber)
                {
                    var textLeft = monster.health.ToString();
                    var textSizeL = font.MeasureString(textLeft);
                    if (ModConfig.DisplayTextBorder)
                        batch.DrawString(Game1.borderFont, textLeft, screenLoc - new Vector2(-1.0f, textSizeL.Y + 1.65f), ModConfig.TextBorderColor, 0.0f, Vector2.Zero, 0.66f, SpriteEffects.None, 0);
                    batch.DrawString(font, textLeft, screenLoc - new Vector2(0.0f, textSizeL.Y + 1.0f), ModConfig.TextColor, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
                }

                if (ModConfig.DisplayMaxHealthNumber)
                {
                    var textRight = monster.maxHealth.ToString();
                    var textSizeR = font.MeasureString(textRight);
                    if (ModConfig.DisplayTextBorder)
                        batch.DrawString(Game1.borderFont, textRight, screenLoc + new Vector2(ModConfig.BarWidth, 0.0f) - new Vector2(textSizeR.X - 1f, textSizeR.Y + 1.65f), ModConfig.TextBorderColor, 0.0f, Vector2.Zero, 0.66f, SpriteEffects.None, 0);
                    batch.DrawString(font, textRight, screenLoc + new Vector2(ModConfig.BarWidth, 0.0f) - new Vector2(textSizeR.X, textSizeR.Y + 1.0f), ModConfig.TextColor, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
                }
            }

            batch.End();

            if (!Game1.options.zoomLevel.Equals(1.0f))
            {
                TheGame.GraphicsDevice.SetRenderTarget((RenderTarget2D)null);
                Game1.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
                Game1.spriteBatch.Draw((Texture2D)TheGame.Screen, Vector2.Zero, new Microsoft.Xna.Framework.Rectangle?(TheGame.Screen.Bounds), Color.White, 0.0f, Vector2.Zero, Game1.options.zoomLevel, SpriteEffects.None, 1f);
                Game1.spriteBatch.End();
            }
        }
    }

    public class HealthBarConfig : Config
    {
        public bool DisplayHealthWhenNotDamaged { get; set; }

        public bool DisplayMaxHealthNumber { get; set; }
        public bool DisplayCurrentHealthNumber { get; set; }

        public bool DisplayTextBorder { get; set; }

        public Color TextColor { get; set; }
        public Color TextBorderColor { get; set; }

        public Color LowHealthColor { get; set; }
        public Color HighHealthColor { get; set; }

        public int BarWidth { get; set; }
        public int BarHeight { get; set; }

        public int BarBorderWidth { get; set; }
        public int BarBorderHeight { get; set; }

        public override T GenerateDefaultConfig<T>()
        {
            DisplayHealthWhenNotDamaged = false;

            DisplayMaxHealthNumber = true;
            DisplayCurrentHealthNumber = true;

            DisplayTextBorder = true;

            TextColor = Color.White;
            TextBorderColor = Color.Black;

            LowHealthColor = Color.DarkRed;
            HighHealthColor = Color.LimeGreen;

            BarWidth = 90;
            BarHeight = 15;

            BarBorderWidth = 2;
            BarBorderHeight = 2;
            return this as T;
        }
    }
}