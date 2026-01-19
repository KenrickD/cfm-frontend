namespace cfm_frontend.Models.Asset
{
    /// <summary>
    /// Model for asset search result
    /// </summary>
    public class AssetSearchResult
    {
        public int IdAsset { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Model for asset group search result
    /// </summary>
    public class AssetGroupSearchResult
    {
        public int IdAssetGroup { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
