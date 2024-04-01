using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Google.Apis.Sheets.v4.Data;

namespace GoogleTableLoader
{
    public static class DataHelper
{
    private const char Separator = '/';
    public static void DataToDictionary(Dictionary<string, object> target, IReadOnlyDictionary<string, string> data)
    {
        object GetVal(string rawValue)
        {
            if (long.TryParse(rawValue, out var longVal))
                return longVal;
            try
            {
                var doubleVal = double.Parse(rawValue, CultureInfo.InvariantCulture);
                return doubleVal;
            }
            catch (Exception ex)
            {
            }
            
            if (bool.TryParse(rawValue, out var boolVal))
            {
                return boolVal;
            }
            return rawValue;
        }
            
        var groups = data.GroupBy(e => e.Key.Split(Separator)[0]);
        foreach (var group in groups)
        {
            var mainKey = group.First().Key.Split(Separator)[0];
            var groupData = group.ToArray();
            if (groupData.Length == 1 && !groupData[0].Key.Contains("["))
            {
                if (groupData[0].Key.StartsWith($"{mainKey}{Separator}"))
                {
                    target.Add(mainKey, new Dictionary<string, object>()
                    {
                        [groupData[0].Key.Replace($"{mainKey}{Separator}", "")] = GetVal(groupData[0].Value)
                    });
                }
                else
                {
                    target.Add(groupData[0].Key, GetVal(groupData[0].Value));
                }
            }
            else
            {
                if (groupData.First().Key.Split(Separator)[1].StartsWith("["))
                {
                    var arrayItems = groupData.GroupBy(e => e.Key.Split(Separator)[1]);
                    var indexes = groupData.Select(e => e.Key.Split(Separator)[1]).ToArray();
                    var hashset = new HashSet<string>(indexes);
                    var count = hashset.Count;
                    var listData = new List<object>();
                    for (int i = 0; i < count; i++)
                    {
                        var indexKey = $"[{i + 1}]";
                        var itemDict = groupData.Where(e => e.Key.Split(Separator)[1] == indexKey)
                            .Select(e =>
                                new KeyValuePair<string, string>(
                                    e.Key.Replace($"{mainKey}{Separator}{indexKey}", ""), e.Value))
                            .Select(e =>
                            {
                                if (e.Key.StartsWith(Separator.ToString()))
                                {
                                    return new KeyValuePair<string, string>(e.Key.Substring(1), e.Value);
                                }
                                return e;
                            })
                            .ToDictionary(e => e.Key, e => e.Value);
                        if (itemDict.Count == 1)
                        {
                            listData.Add(GetVal(itemDict.First().Value));
                        }
                        else
                        {
                            var dataDict = new Dictionary<string, object>();
                            DataToDictionary(dataDict, itemDict);
                            listData.Add(dataDict);
                        }
                    }
                    target[mainKey] = listData;
                }
                else
                {
                    var nestedData = groupData.Select(e =>
                            new KeyValuePair<string, string>(e.Key.Replace($"{mainKey}{Separator}",""), e.Value))
                        .ToDictionary(e=>e.Key,e=>e.Value);
                    var dict = new Dictionary<string, object>();
                    DataToDictionary(dict, nestedData);
                    target.Add(mainKey, dict);
                }
            }
        }
    }
    
    public static Dictionary<string, object>[] ConvertSheetToArray(GoolgeSheet sheet)
    {
        var result = new Dictionary<string, object>[sheet.Rows.Count];
        for (int i = 0; i < sheet.Rows.Count; i++)
        {
            var dict = new Dictionary<string, object>();
            DataHelper.DataToDictionary(dict, sheet.Rows[i].ToDictionary());
            result[i] = dict;
        }
        return result;
    }

    public static T[] ConvertSheetTo<T>(Spreadsheet sheet) where T : new()
    {
        var result = new List<T>();

        var headerSize = sheet.Sheets[0].Properties.GridProperties.FrozenRowCount.GetValueOrDefault(1);
        var id = sheet.Sheets[0].Merges.OrderBy(e => e.EndRowIndex).ThenBy(e => e.StartColumnIndex);
        
        return result.ToArray();
    }
}
}