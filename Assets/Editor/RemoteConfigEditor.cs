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

    // This is the "ball" / toggle for cascading scores
    private bool autoCascadeScores = true;

    [MenuItem("Tools/Remote Config Manager")]
    public static void ShowWindow() => GetWindow<RemoteConfigEditor>("Remote Config");

    private string GetFilePath()
    {
        // This automatically maps to /Users/user/UnityProjects/ZooMerge/Assets/Editor/level.json
        return Path.Combine(Application.dataPath, "Editor", "level.json");
    }

    private void OnGUI()
    {
        GUILayout.Label("Noah's Ark - Level Balancer", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load level.json", GUILayout.Height(30))) LoadFromFile();
        if (GUILayout.Button("Save level.json", GUILayout.Height(30))) SaveToFile();
        EditorGUILayout.EndHorizontal();

        if (levelData == null || levelData.galaxies == null)
        {
            EditorGUILayout.HelpBox("Click 'Load level.json' to open the file from your Editor folder.", MessageType.Info);
            return;
        }

        GUILayout.Space(10);

        // --- THE CASCADE TOGGLE UI ---
        GUI.backgroundColor = autoCascadeScores ? Color.green : Color.white;
        if (GUILayout.Button(autoCascadeScores ? "🟢 AUTO-CASCADE SCORES: ON" : "⚪ AUTO-CASCADE SCORES: OFF", GUILayout.Height(25)))
        {
            autoCascadeScores = !autoCascadeScores;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.HelpBox(autoCascadeScores ? "When you edit a score, the difference will automatically be added to ALL subsequent scores in the game." : "Score edits will only affect the specific field you change.", MessageType.Info);

        GUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < levelData.galaxies.Count; i++)
        {
            DrawGalaxy(levelData.galaxies[i], i);
        }

        if (GUILayout.Button("+ Add New Galaxy", GUILayout.Height(25)))
            levelData.galaxies.Add(new GalaxyData { name = "New Galaxy", galaxyId = levelData.galaxies.Count + 1 });

        EditorGUILayout.EndScrollView();
    }

    private void DrawGalaxy(GalaxyData galaxy, int gIndex)
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
                DrawLevel(galaxy.levels[j], j, gIndex, galaxy);
            }
            if (GUILayout.Button("+ Add Level"))
                galaxy.levels.Add(new MergeLevel { index = galaxy.levels.Count + 1 });
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawLevel(MergeLevel level, int lIndex, int gIndex, GalaxyData galaxy)
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

                GUILayout.Label("Ball Lvl:", GUILayout.Width(60));
                score.level = EditorGUILayout.IntField(score.level, GUILayout.Width(60));

                GUILayout.Space(10);

                GUILayout.Label("Score:", GUILayout.Width(45));

                // --- DETECT SCORE CHANGES FOR CASCADING ---
                int oldScore = score.score;
                score.score = EditorGUILayout.IntField(score.score, GUILayout.Width(60));

                // If the score was changed, and cascade is turned on, run the logic
                if (oldScore != score.score && autoCascadeScores)
                {
                    int difference = score.score - oldScore;
                    ApplyCascade(gIndex, lIndex, s, difference);
                }

                if (GUILayout.Button("-", GUILayout.Width(20))) { level.scores.RemoveAt(s); break; }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Add Score Line")) level.scores.Add(new MergeScoreEntry());

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }

    // --- CASCADING LOGIC ---
    // --- CASCADING LOGIC ---
    private void ApplyCascade(int startG, int startL, int targetScoreIndex, int delta)
    {
        for (int g = startG; g < levelData.galaxies.Count; g++)
        {
            var gal = levelData.galaxies[g];

            // If we are in the edited galaxy, start cascading from the NEXT level (startL + 1).
            // If we are in a future galaxy, start from the first level (0).
            int lStart = (g == startG) ? startL + 1 : 0;

            for (int l = lStart; l < gal.levels.Count; l++)
            {
                var lvl = gal.levels[l];

                // Ensure this level actually has this Ball Lvl to prevent errors
                if (lvl.scores != null && targetScoreIndex < lvl.scores.Count)
                {
                    // Only update the exact same Ball Lvl index!
                    lvl.scores[targetScoreIndex].score += delta;
                }
            }
        }
        // Force the editor to repaint immediately so you see the numbers change
        Repaint();
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

    private void LoadFromFile()
    {
        string path = GetFilePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            levelData = JsonConvert.DeserializeObject<MergeLevelData>(json);
            Debug.Log($"Loaded level data directly from: {path}");
        }
        else
        {
            Debug.LogWarning($"File not found at {path}. Creating a new blank layout.");
            levelData = new MergeLevelData { galaxies = new List<GalaxyData>() };
        }
    }

    private void SaveToFile()
    {
        if (levelData == null) return;
        string path = GetFilePath();

        // Let Newtonsoft handle the standard pretty-print formatting automatically
        string json = JsonConvert.SerializeObject(levelData, Formatting.Indented);

        File.WriteAllText(path, json);

        // Forces Unity to refresh and notice the file changed
        AssetDatabase.Refresh();
        Debug.Log($"Saved cleanly to: {path}");
    }

    private void ExportToJson()
    {
        // Standard pretty-print formatting here as well
        string json = JsonConvert.SerializeObject(levelData, Formatting.Indented);

        string path = Path.Combine(Application.temporaryCachePath, "ark_levels.json");
        File.WriteAllText(path, json);
        EditorUtility.OpenWithDefaultApp(path);
    }

    private bool GetFoldout(object obj) => foldouts.ContainsKey(obj) ? foldouts[obj] : false;
    private void SetFoldout(object obj, bool state) => foldouts[obj] = state;
}