namespace cfm_frontend.DTOs.WorkRequest
{
    public class AdHocJobCodeRequest
    {
        public string name { get; set; }
        public int label_Enum_idEnum { get; set; }
        public int unitPriceCurrency_Enum_idEnum { get; set; }
        public float unitPrice { get; set; }
        public int measurementUnit_Enum_idEnum { get; set; }
        public int Client_idClient { get; set; }
    }

    public class AdHocJobCodeResponse
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public bool success { get; set; }
        public string message { get; set; }
    }
}
