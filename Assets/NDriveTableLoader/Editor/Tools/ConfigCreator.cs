using System.IO;
using GoogleTableLoader;
using Newtonsoft.Json;
using UnityEditor;

public static class ConfigCreator
{
    public const string CfgPath = "Assets/Editor/NDriveTableLoader/Resources/";
    public const string CfgFile = "Assets/Editor/NDriveTableLoader/Resources/GoogleLoaderSettings.json";
    
    [InitializeOnLoadMethod]
    public static void EnsureConfig()
    {
        if (!File.Exists(CfgFile))
        {
            if (!Directory.Exists(CfgPath))
            {
                Directory.CreateDirectory(CfgPath);
            }
            File.WriteAllText(CfgFile, JsonConvert.SerializeObject(new GoogleLoaderSettings(), Formatting.Indented));
        }
    }
}
