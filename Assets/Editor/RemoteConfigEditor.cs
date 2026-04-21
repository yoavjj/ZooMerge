using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class RemoteConfigEditor : EditorWindow
{
    private MergeLevelData levelData;
    private Vector2 scrollPos;
    private Dictionary<object, bool> foldouts = new Dictionary<object, bool>();

    [MenuItem("Tools/Remote Config Manager")]
    public static void ShowWindow() => GetWindow<RemoteConfigEditor>("Remote Config");

    private void OnGUI()
    {
        GUILayout.Label("Noah's Ark - Level Balancer", EditorStyles.boldLabel);

        if (GUILayout.Button("Load Current Editor Data", GUILayout.Height(30)))
            levelData = FirebaseInitializer.MergeScoreData;

        if (levelData == null)
        {
            EditorGUILayout.HelpBox("Load data to start.", MessageType.Info);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < levelData.galaxies.Count; i++)
        {
            DrawGalaxy(levelData.galaxies[i], i);
        }

        if (GUILayout.Button("+ Add New Galaxy", GUILayout.Height(25)))
            levelData.galaxies.Add(new GalaxyData { name = "New Galaxy", galaxyId = levelData.galaxies.Count + 1 });

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("GENERATE & OPEN IN TEXT EDITOR", GUILayout.Height(40)))
        {
            ExportToJson();
        }
    }

    private void DrawGalaxy(GalaxyData galaxy, int index)
    {
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.BeginHorizontal();

        bool open = GetFoldout(galaxy);
        SetFoldout(galaxy, EditorGUILayout.Foldout(open, $"Galaxy {galaxy.galaxyId}: {galaxy.name}", true));

        GUILayout.FlexibleSpace();
        GUILayout.Label("ID:");
        galaxy.galaxyId = EditorGUILayout.IntField(galaxy.galaxyId, GUILayout.Width(60));
        galaxy.name = EditorGUILayout.TextField(galaxy.name, GUILayout.Width(150));

        if (GUILayout.Button("X", GUILayout.Width(25))) { levelData.galaxies.Remove(galaxy); return; }
        EditorGUILayout.EndHorizontal();

        if (GetFoldout(galaxy))
        {
            EditorGUI.indentLevel++;
            for (int j = 0; j < galaxy.levels.Count; j++)
            {
                DrawLevel(galaxy.levels[j], j, galaxy);
            }
            if (GUILayout.Button("+ Add Level"))
                galaxy.levels.Add(new MergeLevel { index = galaxy.levels.Count + 1 });
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawLevel(MergeLevel level, int index, GalaxyData galaxy)
    {
        EditorGUILayout.BeginVertical("box");

        // --- Level Header ---
        EditorGUILayout.BeginHorizontal();
        bool open = GetFoldout(level);
        SetFoldout(level, EditorGUILayout.Foldout(open, $"Level {level.index}", true));

        GUILayout.FlexibleSpace();
        GUILayout.Label("Index:", GUILayout.Width(45));
        level.index = EditorGUILayout.IntField(level.index, GUILayout.Width(40));
        GUILayout.Space(10);
        GUILayout.Label("Stage:", GUILayout.Width(45));
        level.stageId = EditorGUILayout.IntField(level.stageId, GUILayout.Width(40));

        if (GUILayout.Button("X", GUILayout.Width(25))) { galaxy.levels.Remove(level); return; }
        EditorGUILayout.EndHorizontal();

        if (GetFoldout(level))
        {
            EditorGUI.indentLevel++;

            // --- Enemies ---
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Enemy Data", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Add Enemy", GUILayout.Width(100)))
                level.enemy_data.Add(new EnemyData { id = 1, health = 2, coins = 5 });
            EditorGUILayout.EndHorizontal();

            if (level.enemy_data == null) level.enemy_data = new List<EnemyData>();
            for (int k = 0; k < level.enemy_data.Count; k++)
            {
                var enemy = level.enemy_data[k];
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("ID:", GUILayout.Width(25));
                enemy.id = EditorGUILayout.IntField(enemy.id, GUILayout.Width(50));
                GUILayout.Space(1);
                GUILayout.Label("HP:", GUILayout.Width(30));
                enemy.health = EditorGUILayout.IntField(enemy.health, GUILayout.Width(60));
                GUILayout.Space(1);
                GUILayout.Label("Coins:", GUILayout.Width(45));
                enemy.coins = EditorGUILayout.IntField(enemy.coins, GUILayout.Width(50));
                if (GUILayout.Button("-", GUILayout.Width(20))) { level.enemy_data.RemoveAt(k); break; }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            // --- Scores ---
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scores", EditorStyles.boldLabel);
            if (GUILayout.Button("Set Defaults", GUILayout.Width(100))) FillDefaultScores(level);
            EditorGUILayout.EndHorizontal();

            if (level.scores == null) level.scores = new List<MergeScoreEntry>();
            for (int s = 0; s < level.scores.Count; s++)
            {
                var score = level.scores[s];
                EditorGUILayout.BeginHorizontal();

                // Fixed width for "Ball Lvl:" label and field
                GUILayout.Label("Ball Lvl:", GUILayout.Width(60));
                score.level = EditorGUILayout.IntField(score.level, GUILayout.Width(60));

                GUILayout.Space(10);

                // Fixed width for "Score:" label and field
                GUILayout.Label("Score:", GUILayout.Width(45));
                score.score = EditorGUILayout.IntField(score.score, GUILayout.Width(60));

                if (GUILayout.Button("-", GUILayout.Width(20))) { level.scores.RemoveAt(s); break; }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Add Score Line")) level.scores.Add(new MergeScoreEntry());

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }

    private void FillDefaultScores(MergeLevel level)
    {
        level.scores = new List<MergeScoreEntry>
        {
            new MergeScoreEntry { level = 1, score = 2 },
            new MergeScoreEntry { level = 2, score = 4 },
            new MergeScoreEntry { level = 3, score = 6 },
            new MergeScoreEntry { level = 4, score = 8 },
            new MergeScoreEntry { level = 5, score = 10 }
        };
    }

    private void ExportToJson()
    {
        string json = JsonConvert.SerializeObject(levelData, Formatting.Indented);
        json = Regex.Replace(json, @"(?<=\[)\s+(?=\{)", " ");
        json = Regex.Replace(json, @"(?<=\})\s+(?=\])", " ");
        json = Regex.Replace(json, @"(?<=\})\s*,\s*(?=\{)", ", ");
        json = Regex.Replace(json, @"(?<=\{)\s+""(\w+)""\s*:\s*", "\"$1\":");

        string path = Path.Combine(Application.temporaryCachePath, "ark_levels.json");
        File.WriteAllText(path, json);
        EditorUtility.OpenWithDefaultApp(path);
    }

    private bool GetFoldout(object obj) => foldouts.ContainsKey(obj) ? foldouts[obj] : false;
    private void SetFoldout(object obj, bool state) => foldouts[obj] = state;
}