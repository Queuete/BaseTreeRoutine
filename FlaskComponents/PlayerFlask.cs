﻿using PoeHUD.Models.Enums;
using PoeHUD.Poe.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeRoutine.FlaskComponents
{
    public class PlayerFlask
    {
        public int Index { get; set; } = 0;
        public String Name { get; set; } = "None";
        public int TotalUses { get; set; } = 0;
        public String BuffString1 { get; set; } = "";
        //For Hybrid Flask as Hybrid flask have two buffs.
        public String BuffString2 { get; set; } = "";

        public FlaskActions Action1 { get; set; } = FlaskActions.None;
        public FlaskActions Action2 { get; set; } = FlaskActions.None;
        public Boolean Instant { get; set; } = false;
        public Mods Mods { get; set; } = null;
    }
}
