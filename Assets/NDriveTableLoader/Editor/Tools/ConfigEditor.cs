using System;
using System.IO;
using GoogleTableLoader;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace NDriveTableLoader.Editor.Tools
{
    public class ConfigEditor : EditorWindow
    {
        private GoogleLoaderSettings _config;
        private string _path;

        [MenuItem("Tools/NDriveTableLoader/Config")]
        public static void ShowConfig()
        {
            var wnd = EditorWindow.GetWindow<ConfigEditor>();
            wnd.LoadConfig(ConfigCreator.CfgFile);
        }

        private void LoadConfig(string cfgFile)
        {
            ConfigCreator.EnsureConfig();
            _path = cfgFile;
            _config = JsonConvert.DeserializeObject<GoogleLoaderSettings>(File.ReadAllText(cfgFile));
        }

        private void OnGUI()
        {
            if (_config == null)
            {
                LoadConfig(ConfigCreator.CfgFile);
            }
            var changed = false;
            GUILayout.BeginHorizontal();
            changed = changed || DrawStringField("Credentials", 
                () => _config.CredentialsPath,
                v => _config.CredentialsPath = v);
            if (GUILayout.Button("SelectFile", GUILayout.Width(150)))
            {
                var oldPath = string.IsNullOrEmpty(_config.CredentialsPath)
                    ? ""
                    : _config.CredentialsPath.Replace(Path.GetFileName(_config.CredentialsPath), "");
                var path = EditorUtility.OpenFilePanel("GoogleDriveCredentials", oldPath, "json");
                path = Path.GetRelativePath(Directory.GetCurrentDirectory(), path);
                if (path != _config.CredentialsPath)
                {
                    _config.CredentialsPath = path;
                    changed = true;
                }
            }
            GUILayout.EndHorizontal();
            changed = changed || DrawStringField("Service Account e-mail", () => _config.ServiceAccountEmail,
                v => _config.ServiceAccountEmail = v);
            changed = changed || DrawStringField("App Name", () => _config.ApplicationName,
                v => _config.ApplicationName = v);
            changed = changed ||
                      DrawIntField("Request delay", () => _config.RequestDelay, v => _config.RequestDelay = v);
            changed = changed ||
                      DrawIntField("Retries", () => _config.Retries, v => _config.Retries = v);
            if (changed)
            {
                File.WriteAllText(_path, JsonConvert.SerializeObject(_config, Formatting.Indented));
            }
        }

        private bool DrawIntField(string name, Func<int> getValue, Action<int> setValue)
        {
            var oldVal = getValue();
            var newVal = EditorGUILayout.IntField(name, oldVal);
            if (newVal != oldVal)
            {
                setValue(newVal);
                return true;
            }
            return false;
        }
        
        private bool DrawStringField(string name, Func<string> getValue, Action<string> setValue)
        {
            var oldVal = getValue();
            var newVal = EditorGUILayout.TextField(name, oldVal);
            if (newVal != oldVal)
            {
                setValue(newVal);
                return true;
            }
            return false;
        }
    }
}