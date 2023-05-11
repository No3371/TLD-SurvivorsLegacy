﻿using System.Collections;
using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.Scenes;
using MelonLoader;
using MelonLoader.TinyJSON;
using ModData;
using UnityEngine;

namespace SurvivorsLegacy
{
	internal class SurvivorsLegacy : MelonMod
    {
		internal static SurvivorsLegacy Instance { get; private set; }
		internal ModDataManager ModData { get; private set; } = new ModDataManager(nameof(SurvivorsLegacy));
		internal object LegacySender { get; set; }
		internal HttpClient LegacySenderHttp { get; set; }
		internal HttpClient LegacyReceiverHttp { get; set; }
		public override void OnInitializeMelon()
		{
			Instance = this;
			uConsole.RegisterCommand("sl_test", new Action(Test));
			uConsole.RegisterCommand("sl_test2", new Action(Test2));
			uConsole.RegisterCommand("sl_here", new Action(LegacyHere));
			uConsole.RegisterCommand("sl_l", new Action(Legacies));
		}
		void Test ()
		{
			InterfaceManager.GetPanel<Panel_HUD>().m_CollectibleNoteObjectText.text = Time.time.ToString();
			InterfaceManager.GetPanel<Panel_HUD>().m_CollectibleNoteObjectText.ProcessAndRequest();
			// InterfaceManager.GetPanel<Panel_HUD>().m_CollectibleNoteScrollView.gameObject.SetActive(true);
			InterfaceManager.GetPanel<Panel_HUD>().m_CollectibleNoteObject.gameObject.SetActive(true);
			
			// s[0] = "tset";
			// var table = Localization.s_CurrentLanguageStringTable;
			// foreach(var l in table.m_Languages)
			// 	MelonLogger.Msg(l);
			// if (table.DoesKeyExist("survivorslegacy_test"))
			// 	table.AddOrUpdateTableEntry("survivorslegacy_test", s, new int[] {0});
			// table.GetEntryFromKey()
		}
		void Test2 ()
		{
			// InterfaceManager.GetPanel<Panel_HUD>().m_CollectibleNoteScrollView.gameObject.SetActive(true);
			InterfaceManager.GetPanel<Panel_HUD>().m_CollectibleNoteObject.gameObject.SetActive(false);
		}
		void LegacyHere ()
		{
			var pos = MelonUtils.ParseJSONStringtoStruct<Vector3>(ModData.Load( $"{SceneLegacies.CurrentScene}.here" ));
			MelonLogger.Msg($"{ pos }");
			GameManager.m_PlayerManager.TeleportPlayer(pos, Quaternion.identity);
		}
		void Legacies ()
		{
			int.TryParse(ModData.Load("legacies"), out int legacies);
			ShowLegaciesMessage(legacies);
		}

        internal void ShowLegaciesMessage (int count)
		{
			var i = HUDMessage.CreateMessageInfo();
			i.m_DisplayTime = 5;
			i.m_IgnoreOverlayActive = true;
			i.m_Text = $"Surviving With {count} Legacies";
			HUDMessage.AddMessageToQueue(i);
			
		}

        public override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
			LegacyReceiverHttp?.Dispose();
			LegacySenderHttp?.Dispose();
        }
    }

	[HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadSceneWithLoadingScreen))]
	internal static class ResolverStopper
	{
		static void Postfix (string sceneName)
		{
			MelonLogger.Msg($"GameManager.LoadSceneWithLoadingScreen: {SceneLegacies.CurrentScene} -> {sceneName}");
			SceneLegacies.CurrentScene = null;
			if (SceneLegacies.resolver != null) MelonCoroutines.Stop(SceneLegacies.resolver);
		}
	}

	// [HarmonyPatch(typeof(Container), nameof(Container.InstantiateContents))]
	// internal static class CorpsePatch
	// {
	// 	static Task<HttpResponseMessage> get;
	// 	static void Prefix (Container __instance)
	// 	{
	// 		if (!__instance.m_IsCorpse || __instance.IsInspected() || SceneTracker.CurrentScenePopulated) return;
	// 		if (SurvivorsLegacy.Instance.resolver == null) SurvivorsLegacy.Instance.resolver = MelonCoroutines.Start(QueueResolver(__instance));
	// 	}

	// 	static IEnumerator QueueResolver (Container container)
	// 	{
	// 		var http = new HttpClient();
	// 		http.Timeout = TimeSpan.FromSeconds(1);
	// 		for (int i = 0; i < 1 ; i++)
	// 		{
	// 			MelonLogger.Msg($"Getting legacy for corpse {container.name} at {container.gameObject.transform.position}");

	// 			get = http.GetAsync($"https://patchbay.hub/tld-survivorslegacy-{sceneName}");
	// 			MelonLogger.Msg($"Getting from https://patchbay.hub/tld-survivorslegacy-{sceneName}");
	// 			yield return new WaitForSeconds(1.5f);
	// 			if (!get.IsCompletedSuccessfully)
	// 			{
	// 				MelonLogger.Msg($"GET failed!");
	// 				break;
	// 			}

	// 			var msg = get.Result.Content.ReadAsStringAsync().Result;
	// 			MelonLogger.Msg($"Fetched record msg: {msg}");
	// 			var record = Decode(msg);
	// 			if (!record.HasValue) continue;
	// 			MelonLogger.Msg($"Decoded.");
	// 			if (container == null) continue;
	// 			if (SceneTracker.CurrentScene != sceneName)
	// 			{
	// 				MelonLogger.Msg($"Abort for scene changed.");
	// 				break;
	// 			}
	// 			ApplyRecordToContainer(record.Value, container);
	// 			SceneTracker.CurrentScenePopulated = true;
	// 			SurvivorsLegacy.Instance.ModData.Save("Y", sceneName);
	// 			break;
	// 		}
	// 		SurvivorsLegacy.Instance.resolver = null;
	// 	}

	// 	static Record? Decode (string msg)
	// 	{
	// 		var proxy = MelonLoader.TinyJSON.Decoder.Decode(msg) as MelonLoader.TinyJSON.ProxyObject;
	// 		if (proxy == null) return null;

	// 		if (!proxy.TryGetValue("days", out var days)
	// 		 || proxy["items"] == null)
	// 			return null;

	// 		var itemArr = proxy["items"] as ProxyArray;
	// 		string[]? items = null;
	// 		if (itemArr != null)
	// 		{
	// 			items = new string[itemArr.Count];
	// 			for (int i = 0; i < itemArr.Count; i++) items[i] = itemArr[i];
	// 		}
	// 		else return null;

	// 		Record record;
	// 		record.days = days;
	// 		record.items = items;
	// 		return record;

	// 	}

	// 	static void ApplyRecordToContainer (Record record, Container container)
	// 	{
	// 		if (record.items != null)
	// 			foreach (var item in record.items)
	// 			{
	// 				var gi = GearItem.InstantiateGearItem(item);
	// 				gi.RollGearCondition(false);
	// 				container.AddGear(gi);
	// 				MelonLogger.Msg($"Added {item} to {container.name} at {container.gameObject.transform.position}");
	// 			}
	// 	}
	// }

	[HarmonyPatch(typeof(Condition), nameof(Condition.PlayerDeath))]
	internal static class SendLegacyOnDeath
	{
		static void Postfix ()
		{
            Il2CppSystem.Collections.Generic.List<Il2CppTLD.Gear.GearItemObject> items = GameManager.m_Inventory.m_Items;
			if (items.Count == 0) return;
			int rand1 = -1, rand2 = -1, rand3 = -1;
			rand1 = UnityEngine.Random.Range(0, items.Count);
			if (items.Count - rand1 > 1)
				rand2 = UnityEngine.Random.Range(rand1 + 1, items.Count);
			if (items.Count - rand2 > 1)
				rand3 = UnityEngine.Random.Range(rand2 + 1, items.Count);

			var itemNames = new List<string>();
			if (rand1 >= -1) itemNames.Add(items[rand1].m_GearItemName);
			if (rand2 >= -1) itemNames.Add(items[rand2].m_GearItemName);
			if (rand3 >= -1) itemNames.Add(items[rand3].m_GearItemName);

			Record record;
			record.items = itemNames.ToArray();
			record.note = GameManager.m_Log.m_GeneralNotes;
			record.days = GameManager.m_TimeOfDay.m_DaysSurvivedLastFrame;
			var msg = MelonLoader.TinyJSON.Encoder.Encode(record);
			MelonLogger.Msg($"Legacy message prepared: {msg}");
			if (SurvivorsLegacy.Instance.LegacySender != null) MelonCoroutines.Stop(SurvivorsLegacy.Instance.LegacySender);
			SurvivorsLegacy.Instance.LegacySender = MelonCoroutines.Start(Send(SceneLegacies.CurrentScene, msg));
		}

		static IEnumerator Send (string scene, string msg)
		{
			if (SurvivorsLegacy.Instance.LegacySenderHttp == null)
			{
				SurvivorsLegacy.Instance.LegacySenderHttp = new HttpClient();
				SurvivorsLegacy.Instance.LegacySenderHttp.Timeout = Timeout.InfiniteTimeSpan;
			}

            HttpClient sender = SurvivorsLegacy.Instance.LegacySenderHttp;
            sender.CancelPendingRequests();
			var post = sender.PostAsync($"https://patchbay.pub/tld-survivorslegacy-{scene}", new StringContent(msg));
			MelonLogger.Msg($"Posting legacy...");
			var w = new WaitForSecondsRealtime(60);
			while (true)
			{
				yield return w;
				if (post.IsCompletedSuccessfully) break;
				if (post.IsFaulted)
				{
					MelonLogger.Msg($"Restarting POST...");
					post.Dispose();
					post = sender.PostAsync($"https://patchbay.pub/tld-survivorslegacy-{scene}", new StringContent(msg));
				}
			}
			MelonLogger.Msg($"Legacy sent!");
			post.Dispose();
		}
	}
	
	[HarmonyPatch(typeof(QualitySettingsManager), nameof(QualitySettingsManager.ApplyCurrentQualitySettings))]
	internal static class SceneLegacies
	{
		internal static string CurrentScene { get; set; }
		internal static bool CurrentScenePopulated { get; set; }
		internal static Record fetched;
		static Task<HttpResponseMessage> get;
		// internal static List<Container> queue;
		internal static object resolver;
		static void Postfix ()
		{
			if (resolver != null) MelonCoroutines.Stop(resolver);
			CurrentScene = null;
			CurrentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
			if (CurrentScene.StartsWith("MainMenu") || CurrentScene.StartsWith("Boot") || CurrentScene.StartsWith("empty"))
				return;
			MelonLogger.Msg($"{CurrentScene}");
			CurrentScenePopulated = false;
			CurrentScenePopulated = SurvivorsLegacy.Instance.ModData.Load(CurrentScene) == "Y";
			if (CurrentScenePopulated)
			{
				MelonLogger.Msg($"Legacy in {CurrentScene} is already populated.");
				return;
			}
			// MelonLogger.Msg($"ContainerManager: {ContainerManager.m_CorpseContainers.Count}");
			List<Container> qualified = null;
			foreach  (Container container in ContainerManager.m_CorpseContainers)
				if (!container.IsInspected() && container.isActiveAndEnabled)
				{
					if (qualified == null) qualified = new List<Container>();
					qualified.Add(container);
				}

			// MelonLogger.Msg($"Qualified: {qualified.Count}");
			if (qualified.Count > 0)
				MelonCoroutines.Start(Fetch());
		}

		static IEnumerator Fetch ()
		{
			yield return new WaitForSeconds(1.5f);
			if (SurvivorsLegacy.Instance.LegacyReceiverHttp == null)
			{
				SurvivorsLegacy.Instance.LegacyReceiverHttp = new HttpClient();
				SurvivorsLegacy.Instance.LegacyReceiverHttp.Timeout = TimeSpan.FromSeconds(3);
				SurvivorsLegacy.Instance.LegacyReceiverHttp.MaxResponseContentBufferSize = 8192;
			}
			SurvivorsLegacy.Instance.LegacyReceiverHttp.CancelPendingRequests();
			var sceneName = SceneLegacies.CurrentScene;
			get = SurvivorsLegacy.Instance.LegacyReceiverHttp.GetAsync($"https://patchbay.pub/tld-survivorslegacy-{sceneName}");
			// MelonLogger.Msg($"Getting legacy from https://patchbay.pub/tld-survivorslegacy-{sceneName}");
			yield return new WaitForSeconds(1.5f);
			if (!get.IsCompletedSuccessfully)
			{
				MelonLogger.Msg($"GET failed!");
				yield break;
			}
			var task = get.Result.Content.ReadAsStringAsync();
			if (!task.IsCompletedSuccessfully)
			{
				MelonLogger.Msg($"Abort for failed read.");
				yield break;
			}
			var msg = task.Result;
			// MelonLogger.Msg($"Fetched record msg: {msg}");
			var record = Decode(msg);
			if (!record.HasValue)
			{
				MelonLogger.Msg($"Abort for invalid msg.");
				yield break;
			}
			// MelonLogger.Msg($"Decoded.");
			fetched = record.Value;
			resolver = MelonCoroutines.Start(Populater(sceneName));
			get.Dispose();
		}

		static IEnumerator Populater (string sceneName)
		{
			yield return new WaitForSecondsRealtime(10);
			// if (queue == null || queue.Count < 1)
			// {
			// 	MelonLogger.Msg($"Abort no container queued.");
			// 	yield break;
			// }
			// MelonLogger.Msg(queue.Count);
			int retries = 0;
			var wait = new WaitForSecondsRealtime(1);
			for (int i = UnityEngine.Random.Range(0, ContainerManager.m_CorpseContainers.Count); retries < 10 && i < ContainerManager.m_CorpseContainers.Count; i = UnityEngine.Random.Range(0, ContainerManager.m_CorpseContainers.Count))
			{
				yield return wait;
                Container container = ContainerManager.m_CorpseContainers[i];
				// MelonLogger.Msg("A");
				if (SceneLegacies.CurrentScene != sceneName)
				{
					MelonLogger.Msg($"Abort for scene changed {SceneLegacies.CurrentScene} / {sceneName}");
					break;
				}

				if (container == null)
				{
                	MelonLogger.Msg($"Skipping invalid container.");
					retries++;
					continue;
				}
				if (!container.isActiveAndEnabled)
				{
                	MelonLogger.Msg($"Skipping inactive container.");
					retries++;
					continue;
				}
				var go  = container.gameObject;
				if (go == null)
				{
                	MelonLogger.Msg($"Skipping invalid container (go).");
					retries++;
					continue;
				}
				var transform = go.transform;
				if (transform == null)
				{
                	MelonLogger.Msg($"Skipping invalid container (transform).");
					retries++;
					continue;
				}
				// MelonLogger.Msg("B");
				// MelonLogger.Msg($"Checking out corpse#{i}: {container?.gameObject?.name} at {container?.gameObject?.transform?.position}, polulated: {!container?.m_NotPopulated}, enabled: {container?.enabled}, active: {container?.gameObject?.active}, rolled: {container?.m_RolledSpawnChance}");
                AddLegacyToContainer(fetched, container);
				// MelonLogger.Msg("C");
				GameManager.SaveGame();
				// MelonLogger.Msg("D");
				SceneLegacies.CurrentScenePopulated = true;
				SurvivorsLegacy.Instance.ModData.Save("Y", sceneName);
                SurvivorsLegacy.Instance.ModData.Save(container?.name, $"{sceneName}.corpse");
                SurvivorsLegacy.Instance.ModData.Save(MelonLoader.TinyJSON.Encoder.Encode(transform.position), $"{sceneName}.here");
                SurvivorsLegacy.Instance.ModData.Save(fetched.days.ToString(), $"{sceneName}.days");
				// MelonLogger.Msg($"Added sruvivor's legacy. Saving...");
				MelonLogger.Msg($"Added sruvivor's legacy. Saved.");
				if (!string.IsNullOrWhiteSpace(fetched.note)) SurvivorsLegacy.Instance.ModData.Save(fetched.note, $"{sceneName}.note");
				// MelonLogger.Msg($"Added sruvivor's legacy to {SurvivorsLegacy.Instance.ModData.Load($"{sceneName}.corpse")} in {SceneLegacies.CurrentScene} / {sceneName}.");
				break;
			}
			resolver = null;
		}

		static void AddLegacyToContainer (Record record, Container container)
		{
			if (record.items != null)
				foreach (var item in record.items)
				{
					var gi = GearItem.InstantiateGearItem(item);
					gi.RollGearCondition(true);
					// MelonLogger.Msg($"Gear condition rolled to " + gi.GetRoundedCondition());
					container.AddGear(gi);
					// MelonLogger.Msg($"Added {item} to {container.name} at {container.gameObject.transform.position}");
				}
		}

		static Record? Decode (string msg)
		{
			var proxy = MelonLoader.TinyJSON.Decoder.Decode(msg) as MelonLoader.TinyJSON.ProxyObject;
			if (proxy == null) return null;

			if (!proxy.TryGetValue("days", out var days)
			 || !proxy.TryGetValue("note", out var note)
			 || proxy["items"] == null)
				return null;

			var itemArr = proxy["items"] as ProxyArray;
			string[]? items = null;
			if (itemArr != null)
			{
				items = new string[itemArr.Count];
				for (int i = 0; i < itemArr.Count; i++) items[i] = itemArr[i];
			}
			else return null;

			Record record;
			record.days = days;
			record.items = items;
			record.note = note;
			return record;

		}
	}

	[HarmonyPatch(typeof(ContainerInteraction), nameof(ContainerInteraction.InitializeInteraction))]
	internal static class Note
	{
		internal static bool reading;
		static void Prefix (ContainerInteraction __instance)
		{
			bool searched = SurvivorsLegacy.Instance.ModData.Load($"{SceneLegacies.CurrentScene}.searched") == "Y";
			if (!searched)
			{
				Container c = __instance.m_Container;
				string cName = c?.name;
				if (cName == null) cName = c?.name;
				if (cName == null) cName = c?.name;
				// MelonLogger.Msg($"Reading note on {cName}... ? {SurvivorsLegacy.Instance.ModData.Load($"{SceneLegacies.CurrentScene}.corpse")}");
				if (!c.m_IsCorpse || cName != SurvivorsLegacy.Instance.ModData.Load($"{SceneLegacies.CurrentScene}.corpse")) return;
				var text = SurvivorsLegacy.Instance.ModData.Load($"{SceneLegacies.CurrentScene}.note");
				var days =  SurvivorsLegacy.Instance.ModData.Load($"{SceneLegacies.CurrentScene}.days");
				MelonLogger.Msg($"reading for {SceneLegacies.CurrentScene}.note : {text}");
				if (string.IsNullOrWhiteSpace(text)) text = "Take my stuff...\n\nSurvive.";
				Panel_HUD panel_HUD = InterfaceManager.GetPanel<Panel_HUD>();
				panel_HUD.m_CollectibleNoteObjectTitle.text = "Survivor's Legacy";
				panel_HUD.m_CollectibleNoteObjectTitle.ProcessAndRequest();
				panel_HUD.m_CollectibleNoteObjectText.text = $"When you get close, you noticed a note on the body:\n\n\n\n{text}\n\n\n\n(Survived for {days} days)";
				panel_HUD.m_CollectibleNoteObjectText.ProcessAndRequest();
				panel_HUD.m_CollectibleNoteObject.gameObject.SetActive(true);
				reading = true;
				SurvivorsLegacy.Instance.ModData.Save("Y", $"{SceneLegacies.CurrentScene}.searched");
				int.TryParse(SurvivorsLegacy.Instance.ModData.Load("legacies"), out int legacies);
				legacies += 1;
				SurvivorsLegacy.Instance.ModData.Save(legacies.ToString(), "legacies");
				SurvivorsLegacy.Instance.ShowLegaciesMessage(legacies);
				GameManager.SaveGame();
				return;
			}
		}
	}


	public struct Record
	{
		public int days;
		public string[] items;
		public string note;
	}
}
