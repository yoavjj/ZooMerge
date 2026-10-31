using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(BallUnlockCatalogSO))]
public class BallUnlockCatalogSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(
            15f
        );

        if (GUILayout.Button(
                "Save Remote Config JSON",
                GUILayout.Height(32f)))
        {
            ExportRemoteConfigJson();
        }
    }

    private void ExportRemoteConfigJson()
    {
        BallUnlockCatalogSO catalog =
            (BallUnlockCatalogSO)target;

        BallUnlockRemoteConfigData data =
            new BallUnlockRemoteConfigData();

        foreach (
            BallUnlockCatalogSO.UnlockDefinition definition
            in catalog.Definitions)
        {
            if (definition == null)
                continue;

            BallUnlockRemoteDefinition remote =
                new BallUnlockRemoteDefinition
                {
                    type =
                        definition.type.ToString(),

                    coinCost =
                        definition.coinCost,

                    mergeRequirements =
                        new List<
                            BallUnlockRemoteMergeRequirement
                        >()
                };

            if (definition.mergeRequirements != null)
            {
                foreach (
                    BallUnlockCatalogSO.MergeRequirement requirement
                    in definition.mergeRequirements)
                {
                    if (requirement == null)
                        continue;

                    remote.mergeRequirements.Add(
                        new BallUnlockRemoteMergeRequirement
                        {
                            type =
                                requirement.type.ToString(),

                            requiredAmount =
                                requirement.requiredAmount
                        }
                    );
                }
            }

            data.definitions.Add(
                remote
            );
        }

        string json =
            JsonConvert.SerializeObject(
                data,
                Formatting.Indented
            );

        string catalogPath =
            AssetDatabase.GetAssetPath(
                catalog
            );

        string directory =
            Path.GetDirectoryName(
                catalogPath
            );

        string filePath =
            Path.Combine(
                directory,
                "ball_unlock_catalog.json"
            );

        // Creates the file if missing.
        // Overwrites it if it already exists.
        File.WriteAllText(
            filePath,
            json
        );

        AssetDatabase.Refresh();

        string fullPath =
            Path.GetFullPath(
                filePath
            );

        Debug.Log(
            $"[BallUnlockCatalogSO] " +
            $"Saved Remote Config JSON:\n" +
            $"{fullPath}"
        );

        InternalEditorUtility
            .OpenFileAtLineExternal(
                fullPath,
                1
            );
    }
}