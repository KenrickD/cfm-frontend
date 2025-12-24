namespace cfm_frontend.DTOs.Client
{
    public class ClientListResponse
    {
        public List<ClientItem> Clients { get; set; } = new List<ClientItem>();
    }

    public class ClientItem
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
    }
}