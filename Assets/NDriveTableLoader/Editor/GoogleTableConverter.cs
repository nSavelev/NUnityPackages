using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GoogleTableLoader;
using NDriveTableLoader.Runtime.Attributes;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace NDriveTableLoader.Editor
{
    public static class GoogleTableConverter
    {
        public static async Task<TType> Load<TType>()
        {
            var type = typeof(TType);
            var tableAttribute = type.GetCustomAttribute<NDriveTableAttribute>();
            if (tableAttribute == null)
            {
                throw new Exception($"{type.Name} should be marked with NDriveTableAttribute!");
            }
            var id = tableAttribute.Id;
            try
            {
                var tableData = await GoogleLoader.Load(id, onProgress: TryReportProgress);
                var data = new Dictionary<string, object>();
                FillType(type, data, tableData, (s, f) => TryReportProgress(s, f));
                var json = JsonConvert.SerializeObject(data);
                var result = JsonConvert.DeserializeObject<TType>(json);
                EditorUtility.ClearProgressBar();
                return result;
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogException(ex);
                throw ex;
            }
        }

        public static async Task LoadTo<TType>(TType target, Action<string, float> progress = null)
        {
            var type = typeof(TType);
            var tableAttribute = type.GetCustomAttribute<NDriveTableAttribute>();
            if (tableAttribute == null)
            {
                throw new Exception($"{type.Name} should be marked with NDriveTableAttribute!");
            }
            var id = tableAttribute.Id;
            try
            {
                var tableData = await GoogleLoader.Load(id, onProgress: progress);
                var data = new Dictionary<string, object>();
                FillType(type, data, tableData, (s, f) => TryReportProgress(s, f));
                var json = JsonConvert.SerializeObject(data);
                JsonConvert.PopulateObject(json, target, new JsonSerializerSettings(){NullValueHandling = NullValueHandling.Ignore});
                EditorUtility.ClearProgressBar();
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogException(ex);
                throw ex;
            }
        }

        private static void FillType(Type type, Dictionary<string, object> dataTarget, GoogleTable table, Action<string, float> onProgress = null)
        {
            var items = type.GetMembers().Where(e => e.GetCustomAttribute<NDriveItemAttribute>() != null).ToList();
            for (int i = 0; i < items.Count; i++)
            {
                if (onProgress != null)
                {
                    onProgress(items[i].Name, (float)i / items.Count);
                }
                FillData(items[i], dataTarget, table);
            }
        }

        private static void FillData(
            MemberInfo memberInfo, 
            Dictionary<string, object> dataTarget,
            GoogleTable dataSource)
        {
            var tableConfig = memberInfo.GetCustomAttribute<NDriveItemAttribute>();
            if (tableConfig == null)
                return;
            switch (tableConfig)
            {
                case NDriveDictionaryItemAttribute dict:
                    var rawDict = GetRawData(memberInfo, tableConfig, dataSource);
                    dataTarget[memberInfo.Name] = rawDict.ToDictionary(e => e[dict.Key]);
                    break;
                case NDriveSingleItemAttribute single:
                    var rawSingle = GetRawData(memberInfo, tableConfig, dataSource);
                    dataTarget[memberInfo.Name] = rawSingle.FirstOrDefault();
                    break;
                case NDriveNestedAttribute nested:
                    Type nestedType;
                    if (memberInfo is PropertyInfo prop)
                        nestedType = prop.PropertyType;
                    else if (memberInfo is FieldInfo field)
                        nestedType = field.FieldType;
                    else
                        return;
                    var data = new Dictionary<string, object>();
                    FillType(nestedType, data, dataSource);
                    dataTarget[memberInfo.Name] = data;
                    break;
                default:
                    var rawDefault = GetRawData(memberInfo, tableConfig, dataSource);
                    dataTarget[memberInfo.Name] = rawDefault;
                    break;
            }
        }

        private static Dictionary<string, object>[] GetRawData(MemberInfo memberInfo, NDriveItemAttribute cfg,
            GoogleTable dataSource)
        {
            var name = string.IsNullOrEmpty(cfg.Name) ? memberInfo.Name : cfg.Name;
            return DataHelper.ConvertSheetToArray(dataSource.Sheets[name]);
        }

        private static void TryReportProgress(string message, float progress)
        {
            EditorUtility.DisplayProgressBar("NDriveGoogleLoader", message, progress);
        }
    }
}