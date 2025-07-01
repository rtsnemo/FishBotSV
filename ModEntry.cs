using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;

namespace FishBotSV
{
    public class ModEntry : Mod
    {
        private AutoFishingMod autoFishingMod;
        public static ModEntry Instance { get; private set; }

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            autoFishingMod = new AutoFishingMod(helper, Monitor);
            
            // Subscribe to events
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            
            Monitor.Log("Auto Fishing Bot mod loaded successfully!", LogLevel.Info);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // Activate/deactivate auto fishing with ':' key
            if (e.Button == SButton.OemSemicolon && Context.IsWorldReady)
            {
                autoFishingMod.ToggleAutoFishing();
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (Context.IsWorldReady && autoFishingMod.IsActive)
            {
                autoFishingMod.Update();
            }
        }
    }
} 