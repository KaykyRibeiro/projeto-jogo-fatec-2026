using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

namespace ValeDasReliquias.Editor
{
    public static class GameAssetValidation
    {
        static readonly List<string> checks = new List<string>();
        static void Require(bool value, string name) { if (!value) throw new Exception("Asset validation failed: " + name); checks.Add(name); }
        [MenuItem("Vale das Relíquias/Validar regras e cenas")]
        public static void Validate()
        {
            checks.Clear();
            var goals = new CollectionGoals { coins = 2, diamonds = 1, teapots = 1 };
            Require(!goals.Complete, "Goals start incomplete");
            Require(goals.TryCollect(ItemKind.Coin) && goals.TryCollect(ItemKind.Coin) && !goals.TryCollect(ItemKind.Coin), "Coin quota cannot overflow");
            Require(!goals.Complete && goals.TryCollect(ItemKind.Diamond) && !goals.Complete, "All item types are required");
            Require(goals.TryCollect(ItemKind.Teapot) && goals.Complete, "Goal completion uses every quota");
            Require(!goals.TryCollect(ItemKind.Key) && !goals.TryCollect(ItemKind.Bomb), "Key and hazard do not count as relics");
            goals.Reset(); Require(goals.Coins == 0 && goals.Diamonds == 0 && goals.Teapots == 0 && !goals.Complete, "Reset clears all counters");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_Game/Animations/Player.controller");
            var parameters = controller.parameters.Select(p => p.name).ToHashSet();
            foreach (var state in controller.layers[0].stateMachine.states)
            {
                Require(state.state.motion != null, "Motion exists: " + state.state.name);
                foreach (var transition in state.state.transitions) foreach (var condition in transition.conditions) Require(parameters.Contains(condition.parameter), "Valid animation condition: " + condition.parameter);
            }
            Require(controller.layers[0].stateMachine.states.Select(s => s.state.name).Distinct().Count() == controller.layers[0].stateMachine.states.Length, "No duplicate animation states");
            for (int i = 0; i <= 3; i++)
            {
                EditorSceneManager.OpenScene("Assets/Scenes/" + (i == 0 ? "Menu" : "cena-" + i) + ".unity");
                var stage = UnityEngine.Object.FindFirstObjectByType<StageDirector>();
                Require(stage != null && stage.stageNumber == i, "Scene stage index " + i);
                var ui = UnityEngine.Object.FindFirstObjectByType<GameUI>();
                Require(ui != null && ui.font != null && ui.playButton != null && ui.healthBar != null && ui.result != null, "UI references " + i);
                Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1, "Single audio listener " + i);
                var terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
                Require(terrain != null && terrain.terrainData.terrainLayers.Length == 4 && terrain.GetComponent<TerrainCollider>() != null, "Terrain has four layers and collision " + i);
                Require(terrain.terrainData.GetAlphamaps(128, 46, 1, 1)[0, 0, 1] > 0.7f, "Gravel path paint is saved " + i);
                if (i == 0) continue;
                Require(stage.player != null && stage.player.GetComponent<Animator>().avatar.isValid, "Valid player and humanoid Avatar " + i);
                Require(stage.player.GetComponent<Animator>().runtimeAnimatorController == controller, "Player uses corrected animator " + i);
                if (i == 1 || i == 3)
                {
                    var items = UnityEngine.Object.FindObjectsByType<Collectible>(FindObjectsSortMode.None);
                    Require(items.Count(c => c.kind == ItemKind.Coin) == stage.goals.coins && items.Count(c => c.kind == ItemKind.Diamond) == stage.goals.diamonds && items.Count(c => c.kind == ItemKind.Teapot) == stage.goals.teapots, "Placed items match quotas " + i);
                }
                if (i == 2)
                {
                    var doors = UnityEngine.Object.FindObjectsByType<DoorInteraction>(FindObjectsSortMode.None);
                    Require(doors.Length == 2 && doors.Count(d => d.requiresKey) == 1, "Both door variants are present");
                    Require(UnityEngine.Object.FindObjectsByType<Collectible>(FindObjectsSortMode.None).Count(c => c.kind == ItemKind.Key) == 1, "Key is available in door stage");
                }
            }
            Require(EditorBuildSettings.scenes.Length == 4 && EditorBuildSettings.scenes.All(s => s.enabled), "Menu and three stages are included in build");
            Directory.CreateDirectory("../BuildLogs"); File.WriteAllLines("../BuildLogs/asset-validation.txt", checks);
            EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
            Debug.Log("VALE_ASSET_VALIDATION_OK checks=" + checks.Count);
        }
    }
}
