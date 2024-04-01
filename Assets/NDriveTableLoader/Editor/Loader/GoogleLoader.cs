using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace GoogleTableLoader
{
    public static class GoogleLoader
    {
        public const char HEADER_SEPARATOR = '/';
        
        private static GLoaderSettings _settings;

        public static GLoaderSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    var text = Resources.Load<TextAsset>("GoogleLoaderSettings");
                    _settings = JsonConvert.DeserializeObject<GLoaderSettings>(text.text);
                }
                return _settings;
            }
        }

        public static async Task<GoogleTable> Load(string id, bool includeData = false, Action<string, float> onProgress = null)
        {
            Debug.Log($"Start fetching {id}");
            var data = new GoogleTable();
            Debug.Log("Creating credentials");
            ICredential cred = null;
            try
            {
                cred = GetCredentials();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw ex;
            }
            Debug.Log("Creating Sheet service");
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = cred,
                ApplicationName = Settings.ApplicationName
            });
            Debug.Log("Creating request");
            var request = service.Spreadsheets.Get(id);
            request.IncludeGridData = includeData;
            Debug.Log("Fetching table data...");
            onProgress?.Invoke("start fetching", 0);
            var responce = await FetchSpreadsheet(service, id, includeData);
            var sheetCounter = 0;
            onProgress?.Invoke("start parsing", 0.3f);
            foreach (var sheet in responce.Sheets.Where(e=>!e.Properties.Title.StartsWith("#")))
            {
                var result = sheet;
                
                onProgress?.Invoke($"processing {sheet.Properties.Title}", sheetCounter/(float)responce.Sheets.Count * 0.7f + 0.3f);
                await Task.Yield();
                if (!includeData)
                {
                    for (int i = 0; i < Settings.Retries; i++)
                    {
                        result = await FetchTableSheet(service, id, sheet.Properties.Title);
                        if (result != null)
                        {
                            break;
                        }
                    }
                    if (result == null)
                    {
                        throw new Exception($"Failed to fetch {sheet.Properties.Title} content!");
                    }
                }
                data.AddSheet(result);
                sheetCounter++;
            }
            return data;
        }

        private static async Task<Spreadsheet> FetchSpreadsheet(SheetsService service, string id, bool includeData)
        {
            var request = service.Spreadsheets.Get(id);
            request.IncludeGridData = includeData;
            Debug.Log($"Fetching table data {id}...");
            Spreadsheet responce = null;
            try
            {
                responce = await request.ExecuteAsync();
            }
            catch (Exception ex)
            {
                if (includeData)
                {
                    return await FetchSpreadsheet(service, id, false);
                }
                else
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("Google loader error!", $"Failed to get sheet {id} from google", "OK");
                    throw ex;
                }
            }
            return responce;
        }

        private static async Task<Sheet> FetchTableSheet(SheetsService service, string id, string sheetTitle)
        {
            var req = service.Spreadsheets.Get(id);
            req.Ranges = new List<string>()
            {
                sheetTitle
            };
            req.IncludeGridData = true;
            await Task.Delay(Settings.RequestDelay);
            try
            {
                var resp = await req.ExecuteAsync();
                return resp.Sheets[0];
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }
        
        private static ICredential GetCredentials()
        {
            var credentials = GetServiceAccountCredentials();
            var initializer = new ServiceAccountCredential.Initializer(credentials.Id)
            {
                User = Settings.ServiceAccountEmail,
                Key = credentials.Key,
                Scopes = new[] {SheetsService.Scope.SpreadsheetsReadonly}
            };
            return new ServiceAccountCredential(initializer);
        }

        private static ServiceAccountCredential GetServiceAccountCredentials()
        {
            //#if !UNITY_EDITOR
            // return (ServiceAccountCredential) GoogleCredential.FromJson(Resources.Load<TextAsset>(Settings.CredentialsResource).text).UnderlyingCredential;
            // #else
            return (ServiceAccountCredential) GoogleCredential.FromFile(Settings.CredentialsPath).UnderlyingCredential;
            // #endif
        }
    }

    public class GoogleTable
    {
        public Dictionary<string, GoolgeSheet> Sheets = new Dictionary<string, GoolgeSheet>();

        public void AddSheet(Sheet sheetResponce)
        {
            Sheets[sheetResponce.Properties.Title] = new GoolgeSheet(sheetResponce);
        }
    }

    public class GoogleRow
    {
        private Dictionary<string, string> _data;

        public GoogleRow(string name, IList<string> headers, IList<string> values)
        {
            _data = new Dictionary<string, string>();
            for (int i = 0; i < headers.Count; i++)
            {
                if (string.IsNullOrEmpty(headers[i]))
                    continue;
                if (values.Count > i)
                {
                    try
                    {
                        _data[headers[i]] = values[i];
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(new Exception($"Error parsing {name}", ex));
                    }
                }
            }
        }

        public object Get(string key)
        {
            if (_data.TryGetValue(key, out var val))
                return val;
            return null;
        }

        public IReadOnlyDictionary<string, string> ToDictionary()
        {
            return _data.Where(e=>e.Value != null)
                .ToDictionary(e=>e.Key, e=>e.Value);
        }
    }

    public class GoolgeSheet
    {
        public string Name;
        public readonly IReadOnlyList<string> Headers;
        public readonly IReadOnlyList<GoogleRow> Rows;

        public GoolgeSheet(Sheet sheet)
        {
            Name = sheet.Properties.Title;
            var headersCount = sheet.Properties.GridProperties.FrozenRowCount.GetValueOrDefault(1);
            var data = sheet.Data[0];
            var lastHeaderIndex = 0;
            var counter = 0;
            var columnsCount = data.RowData[0].Values.Count;
            var partialHeaders = new string[columnsCount, headersCount];
            for (int row = 0; row < headersCount; row++)
            {
                var headerStack = string.Empty;
                for (int column = 0; column < columnsCount; column++)
                {
                    var value = data.RowData[row].Values[column].FormattedValue;
                    // if (!string.IsNullOrEmpty(value))
                    // {
                    //     if (row > 0)
                    //     {
                    //         headerStack = GoogleLoader.HEADER_SEPARATOR + value;
                    //     }
                    //     else
                    //     {
                    //         headerStack = value;
                    //     }
                    // }
                    //
                    // if (row > 0 && column > 0)
                    // {
                    //     var rData = data.RowData[row-1];
                    //     if (partialHeaders[column, row - 1] != partialHeaders[column, row])
                    //     {
                    //         headerStack = String.Empty;
                    //     }
                    // }
                    partialHeaders[column, row] = value;
                }
            }
            for (int i = 1; i < partialHeaders.GetLength(0); i++)
            {
                if (partialHeaders[i, 0] == null)
                {
                    partialHeaders[i, 0] = partialHeaders[i - 1, 0];
                }
            }
            var headers = new List<string>();
            for (int row = 1; row < partialHeaders.GetLength(1); row++)
            {
                for (int col = 1; col < partialHeaders.GetLength(0); col++)
                {
                    if (partialHeaders[col, row] == null)
                    {
                        if (partialHeaders[col, row - 1] == partialHeaders[col - 1, row - 1])
                        {
                            partialHeaders[col, row] = partialHeaders[col - 1, row];
                        }
                    }
                }
            }
            for (int column = 0; column < columnsCount; column++)
            {
                var header = string.Empty;
                for (int row = 0; row < headersCount; row++)
                {
                    var headerPart = partialHeaders[column, row];
                    if (string.IsNullOrEmpty(headerPart))
                        break;
                    header += $"/{headerPart}";
                }
                headers.Add(header.Trim('/'));
            }
            Headers = headers;
            var rows = new List<GoogleRow>();
            var skipHeader = headersCount;
            foreach (var row in data.RowData.Where(e=>e.Values != null && e.Values.Any(v=>!string.IsNullOrEmpty(v.FormattedValue))))
            {
                if (skipHeader>0)
                {
                    skipHeader--;
                    continue;
                }
                if (row.Values == null)
                    continue;
                try
                {
                    if (row.Values.All(e => GetValue(e) == null))
                    {
                        continue;
                    }

                    rows.Add(new GoogleRow(sheet.Properties.Title, headers, row.Values.Select(e=>e.FormattedValue).ToList()));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
            Rows = rows;
        }

        private object GetValue(CellData data)
        {
            if (data.EffectiveValue != null)
            {
                if (data.EffectiveValue.BoolValue.HasValue)
                    return data.EffectiveValue.BoolValue.Value;
                if (data.EffectiveValue.NumberValue.HasValue)
                    return data.EffectiveValue.NumberValue.Value;
                if (!string.IsNullOrEmpty(data.EffectiveValue.StringValue))
                {
                    return data.EffectiveValue.StringValue;
                }
            }
            return null;
        }
    }
}