﻿using TreeRoutine.FlaskComponents;
using PoeHUD.Models.Enums;
using PoeHUD.Poe;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.EntityComponents;
using PoeHUD.Poe.RemoteMemoryObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeRoutine.DefaultBehaviors.Helpers
{
    public class FlaskHelper<TSettings, TCache>
        where TSettings : BaseTreeSettings, new()
        where TCache : BaseTreeCache, new()
    {
        public BaseTreeRoutinePlugin<TSettings, TCache> Core { get; set; }

        public const String ChargeReductionModName = "flaskchargesused";

        public List<PlayerFlask> getAllFlaskInfo()
        {
            List<PlayerFlask> flaskList = new List<PlayerFlask>();
            for(int i=0;i<5;i++)
            {
                var flask = getFlaskInfo(i);
                if (flask != null)
                    flaskList.Add(flask);
            }
            return flaskList;
        }

        public PlayerFlask getFlaskInfo(int flaskIndex)
        {

            Entity currentFlask = Core.Cache.SavedIngameState.IngameUi.InventoryPanel[InventoryIndex.Flask][flaskIndex, 0, 5];
            if (currentFlask == null || currentFlask.Address == 0x00)
                return null;

            PlayerFlask simplePlayerFlask = new PlayerFlask();

            simplePlayerFlask.Index = flaskIndex;
            simplePlayerFlask.Name = Core.GameController.Files.BaseItemTypes.Translate(currentFlask.Path).BaseName;

            Charges flaskChargesStruct = currentFlask.GetComponent<Charges>();
            Mods flaskMods = currentFlask.GetComponent<Mods>();

            var useCharge = calculateUseCharges(flaskChargesStruct.ChargesPerUse, flaskMods.ItemMods);
            if (useCharge > 0)
                simplePlayerFlask.TotalUses = flaskChargesStruct.NumCharges / useCharge;

            //TreeRoutine.LogError("Flask: " + simplePlayerFlask.Name + "Num Charges: " + flaskChargesStruct.NumCharges + " Use Charges: " + useCharge + " Charges Per use: " + flaskChargesStruct.ChargesPerUse + " Total Uses: " + simplePlayerFlask.TotalUses, 5);


            var flaskBaseName = currentFlask.GetComponent<Base>().Name;
            String flaskBuffOut = null;
            if (!Core.Cache.MiscBuffInfo.flaskNameToBuffConversion.TryGetValue(
                flaskBaseName, out flaskBuffOut))
            {
                Core.LogErr("Cannot find Flask Buff for flask on slot " + (flaskIndex + 1), 5);
                return null;
            }

            simplePlayerFlask.BuffString1 = flaskBuffOut;

            // For Hybrid Flask as it have two buffs.
            if (!Core.Cache.MiscBuffInfo.flaskNameToBuffConversion2.TryGetValue(flaskBaseName, out flaskBuffOut))
                simplePlayerFlask.BuffString2 = "";
            else simplePlayerFlask.BuffString2 = flaskBuffOut;

            simplePlayerFlask.Mods = currentFlask.GetComponent<Mods>();

            handleFlaskMods(simplePlayerFlask);

            return simplePlayerFlask;
        }

        private int calculateUseCharges(float BaseUseCharges, List<ItemMod> flaskMods)
        {
            int totalChargeReduction = 0;
            if (!Core.GameController.EntityListWrapper.PlayerStats.TryGetValue(PlayerStats.FlaskChargesUsedPosPct, out totalChargeReduction))
                totalChargeReduction = 0;

            if (totalChargeReduction > 0)
                BaseUseCharges = ((100 + totalChargeReduction) / 100) * BaseUseCharges;
            foreach (var mod in flaskMods)
            {
                if (mod.Name.ToLower().Contains(ChargeReductionModName))
                    BaseUseCharges = ((100 + (float)mod.Value1) / 100) * BaseUseCharges;
            }
            return (int)Math.Floor(BaseUseCharges);
        }

        private void handleFlaskMods(PlayerFlask flask)
        {
            FlaskActions flaskActionOut;
            //Checking flask action based on flask name type.
            if (!Core.Cache.FlaskInfo.FlaskTypes.TryGetValue(flask.Name, out flaskActionOut))
                Core.LogErr("Error: " + flask.Name + " name not found. Report this error message.", Core.ErrmsgTime);
            else flask.Action1 = flaskActionOut;

            //Checking for unique flasks.
            if (flask.Mods.ItemRarity == ItemRarity.Unique)
            {
                flask.Name = flask.Mods.UniqueName;

                //Enabling Unique flask action 2.
                if (!Core.Cache.FlaskInfo.UniqueFlaskNames.TryGetValue(flask.Name, out flaskActionOut))
                    Core.LogErr("Error: " + flask.Name + " unique name not found. Report this error message.", Core.ErrmsgTime);
                else flask.Action2 = flaskActionOut;
            }

            //Checking flask mods.
            FlaskActions action2 = FlaskActions.Ignore;
            foreach (var mod in flask.Mods.ItemMods)
            {
                if (mod.Name.ToLower().Contains("instant"))
                    flask.Instant = true;

                // We have already decided action2 for unique flasks.
                if (flask.Mods.ItemRarity == ItemRarity.Unique)
                    continue;

                if (!Core.Cache.FlaskInfo.FlaskMods.TryGetValue(mod.Name, out action2))
                    Core.LogErr("Error: " + mod.Name + " mod not found. Is it unique flask? If not, report this error message.", Core.ErrmsgTime);
                else if (action2 != FlaskActions.Ignore)
                    flask.Action2 = action2;
            }
        }

        public Boolean canUsePotion(PlayerFlask flask, int reservedUses=0)
        {
            if (flask.TotalUses - reservedUses <= 0)
            {
                //TreeRoutine.LogMessage("Don't have enough uses on flask " + flask.Name + " to use.", 1);
                return false;
            }

            // Can't use a life flask if we are full hp
            if (flask.Action1 == FlaskActions.Life && !Core.PlayerHelper.isHealthBelowPercentage(100))
            {
                //TreeRoutine.LogMessage("Can't use life flask " + flask.Name + " at full health.", 1);
                return false;
            }

            if (flask.Action1 == FlaskActions.Mana && !Core.PlayerHelper.isManaBelowPercentage(100))
            {
                //TreeRoutine.LogMessage("Can't use mana flask " + flask.Name + " at full mana.", 1);
                return false;
            }

            if (flask.Action1 == FlaskActions.Hybrid && (!Core.PlayerHelper.isHealthBelowPercentage(100) || !Core.PlayerHelper.isManaBelowPercentage(100)))
            {
                //TreeRoutine.LogMessage("Can't use mana hybrid " + flask.Name + " at full health and mana.", 1);
                return false;
            }

            return true;
        }
    }
}
