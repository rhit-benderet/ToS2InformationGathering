using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using BMG.Pooling;
using Cinematics.Players;
using Game.Chat;
using Game.Interface;
using Game.Scrolls;
using Game.Simulation;
using HarmonyLib;
using Home.HomeScene;
using Home.Services;
using Home.Shared;
using Server.Shared.Extensions;
using Server.Shared.Info;
using Server.Shared.Messages;
using Server.Shared.State;
using Server.Shared.State.Chat;
using Services;
using SML;
using UnityEngine;

public class Patches
{
    public static Role myRole = (Role)0;
    public static List<ScrollInfoMine> equippedScrolls = new List<ScrollInfoMine>();
    public static Role tomeRole = (Role)0;
    public static int tomeLots = 0;
    [HarmonyPatch(typeof(HudLobbyPreviousGamePanel), "OnPreviousGameInfoChanged")]
	public class SaveFile
	{
		public static List<PreviousGameEntry> ux = new List<PreviousGameEntry>();

		public static List<string> gameData = new List<string>();
		[HarmonyPostfix]
		public static async void Postfix(PreviousGameData previousGameData)
		{

            var type = AccessTools.TypeByName("Pepper");
            var getCurrentGameMode = AccessTools.Method(type, "GetCurrentGameMode");
            var getMyPosition = AccessTools.Method(type, "GetMyPosition");
            GameType gameType = ((GameMode)getCurrentGameMode.Invoke(null, new object[] { /* parameters */ })).gameType;
			string filename = string.Format("{0}/SalemModLoader/ModFolders/InformationGathering/{1}{2}-{3:yyyy-MM-dd}-{4}.csv", Path.GetDirectoryName(Application.dataPath), gameType, RoleExtensions.GetRoleData(myRole).roleName, DateTime.Now, DateTime.Now.ToString("t").Replace(":", "-"));
			string tobewritten = "";
            FactionType winningFaction = previousGameData.winningFaction;
            string roleName = RoleExtensions.GetRoleData(myRole).roleName;
            FactionType myFaction = RoleExtensions.GetRoleData(myRole).factionType;
            int myPosition = (int)getMyPosition.Invoke(null, new object[] { /* parameters */ });
			if (myPosition >= previousGameData.entries.Count || myPosition < 0)
			{
				return;
			}
			bool didWin = previousGameData.entries[myPosition].won;
            if (winningFaction == FactionType.NONE && myFaction == FactionType.NONE && roleName == "MissingRole")
            {
                return;
            }
            tobewritten += winningFaction.ToDisplayString();
            tobewritten += "\n";
            tobewritten += myFaction.ToDisplayString();
            tobewritten += "\n";
            tobewritten += roleName;
            tobewritten += "\n";
            tobewritten += didWin.ToString();
            tobewritten += "\n";
            tobewritten += tomeRole.ToDisplayString();
            tobewritten += ",";
            tobewritten += tomeLots.ToString();
            for (int i = 0; i < equippedScrolls.Count; i++)
            {
                ScrollInfoMine scroll = equippedScrolls[i];
                tobewritten += "\n" + scroll.role.ToDisplayString() + "," + scroll.nlots + "," + scroll.increase;
            }
            foreach (PreviousGameEntry entry in previousGameData.entries)
            {
                string gameName = entry.gameName;
                string accountName = ((entry.accountName != "") ? entry.accountName : ucpskasp[entry.gameName]);
				string ogRoleName = RoleExtensions.GetRoleData(entry.originalRole).roleName;
                bool won = entry.won;
                tobewritten += "\n" + gameName + "," + accountName + "," + ogRoleName + "," + won;
            }
            // Console.WriteLine(string.IsNullOrEmpty(tobewritten));
			await File.WriteAllTextAsync(filename, tobewritten, default(CancellationToken));
		}
	}
    public static Dictionary<string, string> ucpskasp = new Dictionary<string, string>();
    [HarmonyPatch(typeof(RoleRevealCinematicPlayer), "HandleOnMyIdentityChanged")]
	public class SavePlayers
	{
		[HarmonyPostfix]
		public static void Postfix(RoleRevealCinematicPlayer __instance)
		{
            var type = AccessTools.TypeByName("Pepper");
            var getMyRole = AccessTools.Method(type, "GetMyRole");
            ucpskasp.Clear();
			foreach (DiscussionPlayerObservation discussionPlayer in Service.Game.Sim.info.discussionPlayers)
			{
				ucpskasp[((Observation<DiscussionPlayerState>)(object)discussionPlayer).Data.gameName] = ((Observation<DiscussionPlayerState>)(object)discussionPlayer).Data.accountName;
			}

			myRole = (Role)getMyRole.Invoke(null, new object[] { /* parameters */ });
            
            var equippedScrollsA = Service.Home.UserService.Inventory.GetEquippedScrolls();
            for (int i = 0; i < equippedScrollsA.Count; i++)
            {
                Scroll scroll = equippedScrollsA[i];
                HomeScrollService service = Service.Home.Scrolls;
                ScrollInfoMine scrollInfo = GetScrollInfo(service, scroll.m_id);
                equippedScrolls.Add(scrollInfo);
            }
            Trinket tome = Service.Home.UserService.Inventory.GetTrinket();
            if (tome != null)
            {
                tomeRole = (Role)tome.RoleID;
                tomeLots = tome.CalculateLots();
            }
		}
        private static ScrollInfoMine GetScrollInfo(HomeScrollService service, int id)
        {
            var type = AccessTools.TypeByName("Home.Services.HomeScrollService");
            var catalogOfScrolls = AccessTools.Field(type, "catalogOfScrolls_");
            CatalogOfScrolls lookup = catalogOfScrolls.GetValue(service) as CatalogOfScrolls;
            for (int i = 0; i < lookup.scrolls.Count; i++)
            {
                GameScroll gameScroll = lookup.scrolls[i];
                if (gameScroll.gameScrollInfo.id == id)
                {
                    return new ScrollInfoMine
                    {
                        role = gameScroll.gameScrollInfo.role,
                        nlots = gameScroll.gameScrollInfo.lots,
                        increase = gameScroll.gameScrollInfo.scrollType != ScrollType.Decrease
                    };
                }
            }
			
			return null;
        }
        public static bool TryGetValue(IDictionary dict, object key, out object value)
        {
            if (dict.Contains(key))
            {
                value = dict[key];
                return true;
            }

            value = null;
            return false;
        }
	}

    public class ScrollInfoMine
    {
        public Role role;
        public int nlots;
        public bool increase;
    }
}