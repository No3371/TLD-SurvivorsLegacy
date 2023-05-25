using System.Collections;
using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.Gear;
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
			uConsole.RegisterCommand("sl_here", new Action(LegacyHere));
			uConsole.RegisterCommand("sl_l", new Action(Legacies));
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

	[HarmonyPatch(typeof(Condition), nameof(Condition.PlayerDeath))]
	internal static class SendLegacyOnDeath
	{
		static void Postfix ()
		{
			if (ContainerManager.m_CorpseContainers == null || ContainerManager.m_CorpseContainers.Count == 0) return;
            Il2CppSystem.Collections.Generic.List<Il2CppTLD.Gear.GearItemObject> items = GameManager.m_Inventory.m_Items;
			if (items.Count == 0) return;
			int rand1 = -1, rand2 = -1, rand3 = -1;
			rand1 = UnityEngine.Random.Range(0, items.Count);
			if (items.Count - rand1 > 1)
				rand2 = UnityEngine.Random.Range(rand1 + 1, items.Count);
			if (items.Count - rand2 > 1)
				rand3 = UnityEngine.Random.Range(rand2 + 1, items.Count);

			var itemNames = new List<string>();
			if (rand1 > -1) itemNames.Add(items[rand1].m_GearItemName);
			if (rand2 > -1) itemNames.Add(items[rand2].m_GearItemName);
			if (rand3 > -1) itemNames.Add(items[rand3].m_GearItemName);

			Record record;
			record.items = itemNames.ToArray();
			record.note = GameManager.m_Log.m_GeneralNotes;
			record.days = GameManager.m_TimeOfDay.m_DaysSurvivedLastFrame;
			var msg = MelonLoader.TinyJSON.Encoder.Encode(record);
			MelonLogger.Msg($"Legacy message prepared: {msg}");
			if (SurvivorsLegacy.Instance.LegacySender != null) MelonCoroutines.Stop(SurvivorsLegacy.Instance.LegacySender);
			SurvivorsLegacy.Instance.LegacySender = MelonCoroutines.Start(Send(SceneLegacies.CurrentScene, msg));
		}

		static IEnumerator Send (string sceneName, string msg)
		{
			if (SurvivorsLegacy.Instance.LegacySenderHttp == null)
			{
				SurvivorsLegacy.Instance.LegacySenderHttp = new HttpClient();
				SurvivorsLegacy.Instance.LegacySenderHttp.Timeout = Timeout.InfiniteTimeSpan;
			}

            HttpClient sender = SurvivorsLegacy.Instance.LegacySenderHttp;
            sender.CancelPendingRequests();
			msg = AesOperation.EncryptString(msg);
			var sceneNameEncrypted = AesOperation.EncryptString(sceneName);
			var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sceneNameEncrypted));
			var post = sender.PostAsync($"https://patchbay.pub/tld-survivorslegacy-{b64}", new StringContent(msg));
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
					post = sender.PostAsync($"https://patchbay.pub/tld-survivorslegacy-{b64}", new StringContent(msg));
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
			// MelonLogger.Msg($"{CurrentScene}");
			CurrentScenePopulated = false;
			CurrentScenePopulated = SurvivorsLegacy.Instance.ModData.Load(CurrentScene) == "Y";
			if (CurrentScenePopulated)
			{
				MelonLogger.Msg($"Legacy in {CurrentScene} is already populated.");
				return;
			}
			if (ContainerManager.m_CorpseContainers == null) return;
			// MelonLogger.Msg($"ContainerManager: {ContainerManager.m_CorpseContainers.Count}");
			List<Container> qualified = null;
			foreach  (Container container in ContainerManager.m_CorpseContainers)
			{
				if (!container.IsInspected() && container.isActiveAndEnabled)
				{
					if (qualified == null) qualified = new List<Container>();
					qualified.Add(container);
				}
			}

			// MelonLogger.Msg($"Qualified: {qualified.Count}");
			if (qualified == null) return;
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
			var sceneNameEncrypted = AesOperation.EncryptString(sceneName);
			var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sceneNameEncrypted));
			get = SurvivorsLegacy.Instance.LegacyReceiverHttp.GetAsync($"https://patchbay.pub/tld-survivorslegacy-{b64}");
			// MelonLogger.Msg($"Getting legacy from https://patchbay.pub/tld-survivorslegacy-{b64}");
			yield return new WaitForSeconds(1.5f);
			if (!get.IsCompletedSuccessfully)
			{
				// MelonLogger.Msg($"GET failed!");
				yield break;
			}
			var task = get.Result.Content.ReadAsStringAsync();
			if (!task.IsCompletedSuccessfully)
			{
				// MelonLogger.Msg($"Abort for failed read.");
				yield break;
			}
			var msg = task.Result;
			// MelonLogger.Msg($"Fetched record msg: {msg}");
			msg = AesOperation.DecryptString(msg);
			var record = Decode(msg);
			if (!record.HasValue)
			{
				// MelonLogger.Msg($"Abort for invalid msg.");
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

				if (container == null || container.IsInspected())
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
				MelonLogger.Msg($"Added survivor's legacy. Saved.");
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
					if (gi == null) continue;
					gi.RollGearCondition(true);
					// MelonLogger.Msg($"Gear condition rolled to " + gi.GetRoundedCondition());
					container.AddGear(gi);
					// MelonLogger.Msg($"Added {item} to {container.name} at {container.gameObject.transform.position}");
				}
		}

		internal static Record? Decode (string msg)
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
				var count = Mathf.Min(itemArr.Count, 3);
				items = new string[count];
				for (int i = 0; i < count; i++) items[i] = itemArr[i];
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
				panel_HUD.m_CollectibleNoteObjectText.text = $"Upon approaching, you noticed a note on the body:\n\n\n\n{text}\n\n\n\n(Survived for {days} days)";
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
