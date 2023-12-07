using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace SurvivorsLegacy
{
    [HarmonyPatch(typeof(ContainerInteraction), nameof(ContainerInteraction.InitializeInteraction))]
	internal static class PatchContainerInteraction
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
}
