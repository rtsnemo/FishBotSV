using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FishBotSV
{
    public class AutoFishingMod
    {
        private readonly IModHelper helper;
        private readonly IMonitor monitor;
        private bool isActive = false;
        private bool autoCast = true;
        private bool autoHit = true;
        private bool autoFish = true;
        private int lastFishingTime = 0;
        private const int FISHING_COOLDOWN = 2000;
        private bool wasFishCaught = false;
        private int fishCaughtTimer = 0;
        private const int FISH_CAUGHT_DELAY = 60; // 60 ticks = 1 second

        public bool IsActive => isActive;

        public AutoFishingMod(IModHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;
        }

        public void ToggleAutoFishing()
        {
            isActive = !isActive;
            if (isActive)
            {
                ShowAchievement("Auto Fishing Activated!", "Auto fishing is now active!");
                monitor.Log("Auto fishing activated!", LogLevel.Info);
            }
            else
            {
                ShowAchievement("Auto Fishing Deactivated!", "Auto fishing is now disabled!");
                monitor.Log("Auto fishing deactivated!", LogLevel.Info);
            }
        }

        public void Update()
        {
            if (!Context.IsWorldReady || Game1.currentLocation == null || !isActive)
                return;

            if (Game1.player != null && Game1.player.CurrentTool is StardewValley.Tools.FishingRod fishingRod)
            {
                bool currentFishCaught = fishingRod.fishCaught;
                if (currentFishCaught && !wasFishCaught)
                {
                    fishCaughtTimer = FISH_CAUGHT_DELAY;
                }
                wasFishCaught = currentFishCaught;
                if (fishCaughtTimer > 0)
                {
                    fishCaughtTimer--;
                    if (fishCaughtTimer <= 0)
                    {
                        ForceCloseFishPopup(fishingRod);
                    }
                }
            }
            if (Game1.ticks - lastFishingTime < FISHING_COOLDOWN)
                return;

            AutoCast();
            AutoHit();
            if (autoFish)
            {
                GetFish();
            }
        }

        private void AutoCast()
        {
            if (Game1.player?.CurrentTool is FishingRod rod && autoCast && CanAutoCast(rod))
            {
                rod.beginUsing(Game1.currentLocation, (int)Game1.player.Tile.X * 64, 
                              (int)Game1.player.Tile.Y * 64, Game1.player);
                rod.castingPower = 1.0f;
                lastFishingTime = Game1.ticks;
            }
        }

        private void AutoHit()
        {
            if (Game1.player?.CurrentTool is FishingRod rod && autoHit && CanAutoHook(rod))
            {
                rod.timePerBobberBob = 1f;
                rod.timeUntilFishingNibbleDone = FishingRod.maxTimeToNibble;
                rod.DoFunction(Game1.player.currentLocation, (int)rod.bobber.X, (int)rod.bobber.Y, 1, Game1.player);
            }
        }

        private void GetFish()
        {
            if (Game1.activeClickableMenu is BobberBar bobberBar)
            {
                if (Game1.player?.CurrentTool is FishingRod rod)
                {
                    try
                    {
                        string whichFish = helper.Reflection.GetField<string>(bobberBar, "whichFish", true).GetValue();
                        int fishSize = helper.Reflection.GetField<int>(bobberBar, "fishSize", true).GetValue();
                        int fishQuality = helper.Reflection.GetField<int>(bobberBar, "fishQuality", true).GetValue();
                        float difficulty = helper.Reflection.GetField<float>(bobberBar, "difficulty", true).GetValue();
                        bool treasure = helper.Reflection.GetField<bool>(bobberBar, "treasure", true).GetValue();
                        bool perfect = helper.Reflection.GetField<bool>(bobberBar, "perfect", true).GetValue();
                        bool fromFishPond = helper.Reflection.GetField<bool>(bobberBar, "fromFishPond", true).GetValue();
                        string setFlagOnCatch = helper.Reflection.GetField<string>(bobberBar, "setFlagOnCatch", true).GetValue();
                        bool bossFish = helper.Reflection.GetField<bool>(bobberBar, "bossFish", true).GetValue();

                        rod.pullFishFromWater(whichFish, fishSize, fishQuality, (int)difficulty, treasure, perfect, fromFishPond, setFlagOnCatch, bossFish, 1);
                        Game1.exitActiveMenu();
                        rod.resetState();
                        rod.doneHoldingFish(Game1.player);
                        lastFishingTime = 0;
                    }
                    catch (Exception ex)
                    {
                        Game1.exitActiveMenu();
                        if (Game1.player?.CurrentTool is FishingRod fishingRod)
                        {
                            fishingRod.resetState();
                        }
                    }
                }
            }
        }

        private bool CanAutoCast(FishingRod tool)
        {
            return Context.CanPlayerMove && 
                   Game1.activeClickableMenu == null && 
                   !tool.castedButBobberStillInAir && 
                   !tool.hit && 
                   !tool.inUse() && 
                   !tool.isCasting && 
                   !tool.isFishing && 
                   !tool.isNibbling && 
                   !tool.isReeling && 
                   !tool.isTimingCast && 
                   !tool.pullingOutOfWater;
        }

        private bool CanAutoHook(FishingRod tool)
        {
            return tool.isNibbling && 
                   !tool.isReeling && 
                   !tool.hit && 
                   !tool.pullingOutOfWater && 
                   !tool.fishCaught;
        }

        private void ShowAchievement(string title, string description)
        {
            Game1.addHUDMessage(new HUDMessage(title, HUDMessage.achievement_type)
            {
                message = description,
                timeLeft = 3000
            });
        }

        private void ForceCloseFishPopup(FishingRod rod)
        {
            try
            {
                rod.doneHoldingFish(Game1.player);
            }
            catch (Exception ex)
            {
                monitor.Log($"[DEBUG] Ошибка при закрытии попапа: {ex.Message}", LogLevel.Alert);
            }
        }
    }
} 