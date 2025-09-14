using BepInEx;
using HarmonyLib;
using UnityEngine.SceneManagement;
using TeamCherry.Localization;
using System.Collections.Generic;
using System.IO;

public static class TranslationLoader
{
    public static Dictionary<string, Dictionary<string, string>> Translations = [];

    public static void Load(string folderPath, BepInEx.Logging.ManualLogSource logger)
    {
        if (!Directory.Exists(folderPath))
        {
            logger.LogWarning($"Translation folder not found: {folderPath}");
            return;
        }

        foreach (var file in Directory.GetFiles(folderPath, "*.txt"))
        {
            string sheet = Path.GetFileNameWithoutExtension(file);
            var dict = new Dictionary<string, string>();

            foreach (var line in File.ReadAllLines(file))
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains(":")) continue;

                var parts = line.Split([':'], 2);
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                dict[key] = value;
            }

            Translations[sheet] = dict;
            logger.LogInfo($"Loaded {dict.Count} translations for sheet {sheet}");
        }
    }
}

namespace TranslationMod
{
    [BepInPlugin("com.codenat.translationmod", "Translation Mod", "1.0.0")]
    public class TranslationModPlugin : BaseUnityPlugin
    {
        private Harmony _harmony = null!;

        private void Awake()
        {
            Logger.LogInfo("Translation Mod loaded!");
            string folder = Path.Combine(Paths.PluginPath, "TranslationMod/Resources/en");
            TranslationLoader.Load(folder, Logger);
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnSceneChanged(Scene prev, Scene next)
        {
            if (next.name != "Menu_Title" && _harmony == null)
            {
                _harmony = new Harmony("com.codenat.translationmod");
                _harmony.PatchAll(typeof(LanguageGetPatch));
            }
        }
    }

    [HarmonyPatch(typeof(Language), nameof(Language.Get), [typeof(string), typeof(string)])]
    public static class LanguageGetPatch
    {
        [HarmonyPostfix]
        private static void ChangeText(string key, string sheetTitle, ref string __result)
        {
            if (TranslationLoader.Translations.TryGetValue(sheetTitle, out var sheetDict))
            {
                if (sheetDict.TryGetValue(key, out var newText))
                {
                    __result = newText;
                }
            }
        }
    }
}