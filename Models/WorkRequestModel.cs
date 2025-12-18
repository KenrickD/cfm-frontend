namespace cfm_frontend.Models
{
    public class WorkRequestBodyModel {
        public int idClient { get; set; }
        public int idActor { get; set; }
        public int idEmployee { get; set; }
        public int idStatus { get; set; }
        public string fromDate { get; set; } = string.Empty;
        public string toDate { get; set; } = string.Empty;
        public string filterWorkCategory { get; set; } = string.Empty;
        public string filterLocation { get; set; } = string.Empty;
        public int filterStatus { get; set; }
    }

    public class WorkRequestResponseModel {
        public int idWorkRequest { get; set; }
        public string workRequestCode { get; set; }
        public string requestDate { get; set; }
        public string workTitle { get; set; }
        public string status { get; set; }
        public string statusColor { get; set; }
        public string workCategory { get; set; }
        public string locationFinal { get; set; }
        public string assignedBy { get; set; }
        public string assignedDate { get; set; }
        public bool assignedStatus { get; set; }
        public string checkedInBy { get; set; }
        public string checkedInDate { get; set; }
        public bool checkedInStatus { get; set; }
        public string solution { get; set; }
        public bool isCompletedByTechnician { get; set; }
        public int isClientApproved { get; set; }
        public string workCompletion { get; set; }
        public string requestDetail { get; set; }
        public bool validPIC { get; set; }
        public int isJoinToExternalChat { get; set; }
        public string plm { get; set; }
        public string plmColor { get; set; }
    }
    public class WorkRequestListApiResponse
    {
        public List<WorkRequestResponseModel> data { get; set; }
        public int currentPage { get; set; }
        public int totalPages { get; set; }
        public int pageSize { get; set; }
        public int totalRecords { get; set; }
    }
}
