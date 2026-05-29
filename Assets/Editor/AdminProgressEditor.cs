using Firebase.Auth;
using Firebase.Functions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;

public class AdminProgressEditor : EditorWindow
{
    string targetUid = "";
    int galaxy = 0;
    int level = 0;
    string status = "Ready";
    int addCoinsAmount = 10;

    [MenuItem("Tools/ZooMerge/Admin Tools")]
    static void Open() => GetWindow<AdminProgressEditor>("Admin Tools");

    void OnGUI()
    {
        GUILayout.Label("Target Player", EditorStyles.boldLabel);
        targetUid = EditorGUILayout.TextField("User UID", targetUid);

        GUILayout.Space(8);

        GUILayout.Label("Progress", EditorStyles.boldLabel);
        galaxy = EditorGUILayout.IntField("last_played_galaxy", galaxy);
        level = EditorGUILayout.IntField("last_played_level", level);

        GUILayout.Space(10);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(targetUid)))
        {
            if (GUILayout.Button("Apply Progress (Cloud)"))
                _ = ApplyProgress();

            if (GUILayout.Button("Reset Inventory (Cloud)"))
                _ = ResetInventoryCloud();
        }

        GUILayout.Space(8);
        GUILayout.Label("Economy", EditorStyles.boldLabel);
        addCoinsAmount = EditorGUILayout.IntField("Add Coins", addCoinsAmount);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(targetUid)))
        {
            if (GUILayout.Button("Add Coins (Cloud)"))
                _ = AddCoinsCloud();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(status, MessageType.Info);
    }

    static async Task<FirebaseFunctions> GetFunctionsReady(string statusPrefix, System.Action<string> setStatus, System.Action repaint)
    {
        var auth = FirebaseAuth.DefaultInstance;
        var user = auth.CurrentUser;

        if (user == null)
        {
            setStatus("❌ No Firebase user. Run Play Mode and sign in first.");
            repaint();
            return null;
        }

        // Refresh token so admin claim is included
        setStatus($"{statusPrefix}\nRefreshing token...");
        repaint();
        await user.TokenAsync(true);

        // Correct region (your functions are in us-central1)
        return FirebaseFunctions.GetInstance(Firebase.FirebaseApp.DefaultInstance, "us-central1");
    }

    async Task ApplyProgress()
    {
        try
        {
            var functions = await GetFunctionsReady("Setting progress...", s => status = s, Repaint);
            if (functions == null) return;

            var fn = functions.GetHttpsCallable("adminSetPlayerProgress");

            var data = new Dictionary<string, object>
            {
                { "uid", targetUid.Trim() },
                { "galaxy", galaxy },
                { "level", level },
            };

            status = "Sending progress update...";
            Repaint();

            await fn.CallAsync(data);

            status = $"✅ Updated {targetUid} -> galaxy {galaxy}, level {level}";
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            status = "❌ ERROR:\n" + ex.ToString();
        }

        Repaint();
    }

    async Task ResetInventoryCloud()
    {
        try
        {
            var functions = await GetFunctionsReady("Resetting inventory...", s => status = s, Repaint);
            if (functions == null) return;

            var fn = functions.GetHttpsCallable("adminResetInventory");

            var data = new Dictionary<string, object>
            {
                { "uid", targetUid.Trim() }
            };

            status = "Sending inventory reset...";
            Repaint();

            await fn.CallAsync(data);

            status = $"✅ Inventory reset for {targetUid} (economy fields cleared)";
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            status = "❌ ERROR:\n" + ex.ToString();
        }

        Repaint();
    }

    async Task AddCoinsCloud()
    {
        try
        {
            var functions = await GetFunctionsReady("Adding coins...", s => status = s, Repaint);
            if (functions == null) return;

            var fn = functions.GetHttpsCallable("adminAddCoins");

            var data = new Dictionary<string, object>
        {
            { "uid", targetUid.Trim() },
            { "amount", addCoinsAmount }
        };

            status = "Sending coin update...";
            Repaint();

            await fn.CallAsync(data);

            status = $"✅ Added {addCoinsAmount} coins to {targetUid}";
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            status = "❌ ERROR:\n" + ex.ToString();
        }

        Repaint();
    }
}