using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(RetryRefillPricingSO))]
public class RetryRefillPricingSOEditor : Editor
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
            ExportJson();
        }
    }

    private void ExportJson()
    {
        RetryRefillPricingSO pricing =
            (RetryRefillPricingSO)target;

        RetryRefillPricingRemoteConfigData data =
            new RetryRefillPricingRemoteConfigData
            {
                costs =
                    pricing.costs,

                overflowStep =
                    pricing.overflowStep
            };

        string json =
            JsonConvert.SerializeObject(
                data,
                Formatting.Indented
            );

        string assetPath =
            AssetDatabase.GetAssetPath(
                pricing
            );

        string directory =
            Path.GetDirectoryName(
                assetPath
            );

        string filePath =
            Path.Combine(
                directory,
                "retry_refill_pricing.json"
            );

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
            $"[RetryRefillPricingSO] " +
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