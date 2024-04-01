using System;

namespace NDriveTableLoader.Runtime.Attributes
{
    public class NDriveTableAttribute : Attribute
    {
        public readonly string Id;

        public NDriveTableAttribute(string id)
        {
            Id = id;
        }
    }

    public class NDriveItemAttribute : Attribute
    {
        public readonly string Name = null;

        public NDriveItemAttribute()
        {
        }
        public NDriveItemAttribute(string name)
        {
            Name = name;
        }
    }

    public class NDriveSingleItemAttribute : NDriveItemAttribute{}

    public class NDriveNestedAttribute : NDriveItemAttribute {}

    public class NDriveDictionaryItemAttribute : NDriveItemAttribute
    {
        public readonly string Key;
        public NDriveDictionaryItemAttribute(string key)
        {
            Key = key;
        }
        
        public NDriveDictionaryItemAttribute(string name, string key) : base(name)
        {
            Key = key;
        }
    }
}