namespace cfm_frontend.Models.Asset
{
    /// <summary>
    /// Model for asset search result
    /// </summary>
    public class AssetSearchResult
    {
        public int IdAsset { get; set; }
        public string Label { get; set; }
        public string Name { get; set; }
        public string OtherCode { get; set; }
    }

    /// <summary>
    /// Model for asset group search result
    /// </summary>
    public class AssetGroupSearchResult
    {
        public string AssetGroupName { get; set; }
        public List<AssetSearchResult> Asset { get; set; } = new();
    }
}
