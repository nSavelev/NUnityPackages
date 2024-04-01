# NDriveTableLoader

## Dependencies:
- Unity Newtonsoft Json 3.0.2+ (com.unity.nuget.newtonsoft-json)

## Initial setup:
- You require key to load from google drive via GoogleSheetsAPI.
- Create by folowing this instructions: https://developers.google.com/workspace/guides/get-started
- Create table and add your service account e-mail as reader or editor.
- import package to Unity from git URL (https://github.com/nSavelev/NUnityPackages.git?path=Assets/NDriveTableLoader)

## GoogleTable formatting rules:
- Table locale should be EN-US
- Column name should be equal to data type field or property name
- Nested types and dictionaries are alowed by specifying field path (`SomeField/DataItem` or creating complex header)
- To create complex header merge top level cells !!WARNING!! when using complex headers, be sure that you pin all header rows
- Arrays are allowed by specifying index path in lua notation (starts with 1...) (`Array/[1]` `Array/[2]`)


## Unity Usage
When package was imported it creates a configuration file: `Assets/Editor/NDriveTableLoader/Resources/GoogleLoaderSettings.json`
You can edit this file in text editor or use editor window by calling Tools/NDriveTableLoader/Config
When all fields are specified you can create a data classes like described above:

```csharp
public struct Price
{
    public string Item;
    public int Amount;
}

public class SingleCfg
{
    public int Param1;
    public float Param2;
    public string Param3;
}

public class Iap
{
    public string Id;
    public string SKU;
}

public class NestedItem
{
    // specify to fetch data in nested item from certain page
    [NDriveItem("NestedData1")]
    public NestedData[] Data1;
    [NDriveItem("NestedData2")]
    public NestedData[] Data2;
}

public class NestedData
{
    public string Id;
    public string Value;
}

[NDriveTable("<your table id here>")]
public class TestConfig
{
    [NDriveItem("<page name, specify if ClassMember name doesn't match sheet page name>")]
    // [NDriveItem("Iap-List")] when fetching data from page "Iap-List"
    // [NDriveItem] when fetching data from page "Iaps"
    public Iap[] Iaps;
    
    // Build a dictionary from page "Items" with keys from "Id" column
    [NDriveDictionaryItem(name: "Items", key:"Id")]
    public Dictionary<string, Item> Items;
    
    // Non collection item, will use first row after header
    [NDriveSingleItem]
    public SingleCfg Single;
    
    // Contains agregated data classes with few pages data. Use markdown attributes in members
    [NDriveNested]
    public NestedItem Nested;
}
```

Loading data sample:
```csharp
    [MenuItem("Tools/Balance/LoadTestConfig")]
    public static void Load()
    {
        GoogleTableConverter.Load<TestConfig>()
            .ContinueWith(cfg =>
        {
            File.WriteAllText("Assets/TestConfig.json", JsonConvert.SerializeObject(cfg.Result, Formatting.Indented));
            AssetDatabase.Refresh();
        }).ConfigureAwait(true);
    }
```

## Some workflow details
This tool is building Dictionary from sheed data and then converting it to required type by Json Serialization and Deserialization. 
This way not suitable for runtime, but works fine in editor. Also it allows to use custom type converters (see NewtonsoftJson documentation) for strong typed values. 

