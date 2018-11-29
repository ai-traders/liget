namespace LiGet.Configuration
{
    public class StorageOptions
    {
        public StorageType Type { get; set; }
    }

    public enum StorageType
    {
        FileSystem = 0
    }
}
