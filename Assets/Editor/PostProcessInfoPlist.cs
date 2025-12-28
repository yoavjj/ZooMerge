#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class PostProcessInfoPlist
{
    [PostProcessBuild]
    public static void ChangePlist(BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget != BuildTarget.iOS)
        {
            return;
        }

        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict rootDict = plist.root;

        // Set the tracking usage description
        const string key = "NSUserTrackingUsageDescription";
        const string message = "This identifier will be used to deliver personalized ads and measure ad performance.";
        rootDict.SetString(key, message);

        // Write back to file
        File.WriteAllText(plistPath, plist.WriteToString());
    }
}
#endif
