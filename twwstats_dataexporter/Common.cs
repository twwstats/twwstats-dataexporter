using System;
using System.Collections.Generic;
using twwstats_dataexporter.Services;

namespace twwstats_dataexporter
{
    public static class Common
    {
        /// <summary>
        /// List of the games to export. Each Tuple is made of:
        /// - The Game version / Mod
        /// - A human readable version name (this is no longer used by the code but makes maintenance of the version much easier)
        /// - The folder name for the data of that version (note that TWW and TWW2 data folders must be created inside a /tww and /tww2 parent folder respectively)
        /// </summary>
        public static List<Tuple<Game, String, String>> GameVersions = new List<Tuple<Game, String, String>>
            {
                // === LUCKY ===
                //new Tuple<Game, String, String>(Game.LSO, "Lucky", "lso436"),
                // === RADIOUS ===
                // --- 1149634389 Radious Part 1 ---
                // --- 1149634634 Radious Part 2 ---
                new Tuple<Game, String, String>(Game.RADIOUS, "Radious", "radious"),
                // === CTT ===
                // --- 1214959308 Boyz will be Boyz - CTT
                // --- 1233427148 Boyz will be Boyz - CTT - Units
                // --- 1158737832 Southern Realms 2 (Don't think this is CTT specific but it's compatible)
                // --- 1213746272 Southern Realms 2 Assets (Don't think this is CTT specific but it's compatible)
                // --- 1181220751 Kraka Drak (The Norse Dwarfs) (Don't think this is CTT specific but it's compatible)
                // --- 1192780327 Mixus Mousillon (Don't think this is CTT specific but it's compatible)
                // --- 1194588998 Mixus Legendary Lords 1 (Don't think this is CTT specific but it's compatible)
                // --- 1158791019 Mixus Legendary Lords 2 (Don't think this is CTT specific but it's compatible)
                // --- 1243153086 Mixus Tabletop Lords
                new Tuple<Game, String, String>(Game.CTT, "Boys Will Be Boyz", "ctt"),
                // === SFO ===
                // --- 1149625355 SFO 2 ---
                // --- 1303874898 Regiments of Renown SubMod
                // --- 1365953249 Southern Realms SubMod
                // --- 1366245525 Kraka Drak (The Norse Dwarfs) SubMod
                new Tuple<Game, String, String>(Game.SFO, "Steel Faith Overhaul", "sfo"),
                // === TWW 2 - BETAS ===
                new Tuple<Game, String, String>(Game.TWW2, "tww2_beta", "tww2_beta"),

                // === TWW 2 ===
                new Tuple<Game, String, String>(Game.TWW2, "The Warden & The Paunch v2", "5304167787849256707"),
                new Tuple<Game, String, String>(Game.TWW2, "The Warden & The Paunch", "5370115782236907066"),
                new Tuple<Game, String, String>(Game.TWW2, "The Kings Shilling", "570375842370499671"),
                new Tuple<Game, String, String>(Game.TWW2, "The Shadow & The Blade v3", "3175851157777132217"),
                //new Tuple<Game, String, String>(Game.TWW2, "The Shadow & The Blade v2", "1040626998566270501"),
                //new Tuple<Game, String, String>(Game.TWW2, "The Shadow & The Blade", "2636476295671744220"),
                // new Tuple<Game, String,String>(Game.TWW2, "The Hunter & The Beast v3", "2078602271960950043"),
                //new Tuple<Game, String,String>(Game.TWW2, "The Hunter & The Beast v2", "8355880322201813386"),
                //new Tuple<Game, String,String>(Game.TWW2, "The Hunter & The Beast", "341786206353968576"),
                //new Tuple<Game, String,String>(Game.TWW2, "The Amethyst Update", "4692632701925136852"),
                //new Tuple<Game, String,String>(Game.TWW2, "The Prophet & The Warlock", "6412932268177720920"),
                //new Tuple<Game, String,String>(Game.TWW2, "The Festag Update", "5140091117063796828"),
                //new Tuple<Game, String,String>(Game.TWW2, "The Aye Aye Patch", "8344960722990796442"),
                //new Tuple<Game, String,String>(Game.TWW2, "The Kharibdyss Update", "1470639714768346673"),
                //new Tuple<Game, String,String>(Game.TWW2, "The Resurgent Update", "7552225570622186211"),
                //new Tuple<Game, String,String>(Game.TWW2, "MWNL 2018", "5566353893085820817"),
                new Tuple<Game, String,String>(Game.TWW2, "Vanilla", "4513092812684888368"),

                // === TWW 1 ===
                //new Tuple<Game, String,String>(Game.TWW, "Latest", "133707410635705034"),
                //new Tuple<Game, String,String>(Game.TWW, "Vanilla", "8016606766435871385"),
            };

        public static String DataPath { get
            {
                return $"D:/code/twwstats_data";
            }
        }
    }
}   
