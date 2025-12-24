namespace cfm_frontend.Models.WorkRequest
{
    public class WorkRequestBodyModel {
        public int Client_idClient { get; set; }
        public string searchTag { get; set; } = "";
        
        public int page { get; set; }
        public string keyWordSearch {  get; set; } = string.Empty;
        // for the filter function
        public int idPropertyType { get; set; } = -1;
        public int RoomZone_idRoomZone { get; set; } = -1;
        public bool showDeleted { get; set; } = false;
        public DateTime? requestDateFrom { get; set; }
        public DateTime? requestDateTo { get; set; }
        public DateTime? workCompletionFrom { get; set; }
        public DateTime? workCompletionTo { get; set; }
        public bool hasBeenCompletedByWorker { get; set; }
        public bool isSendEmail { get; set; }
    }

    public class WorkRequestResponseModel {
        public int idWorkRequest { get; set; }
        public string workRequestCode { get; set; } = "";
        public string workTitle { get; set; } = "";
        public string requestDetail { get; set; } = "";
        public string workRequestStatus { get; set; }
        public string propertyName { get; set; }
        public string floorProp { get; set; } = "";
        public string roomZoneName { get; set; } = "";
        public string workCategoryName { get; set; } = "";
        public string serviceProviderName { get; set; } = "";
        public int Requestor_Employee_idEmployee { get; set; }
        public string fullName { get; set; }
        public string requestorDeptName { get; set; } = "";
        public DateTime requestDate { get; set; }
        public int totalWorker { get; set; }
        public int totalWorkerCompleted { get; set; }
        public int totalHasSentEmail { get; set; }
        public int? costApprovalStatus { get; set; }
        public int? idCostApproval { get; set; }
    }
    public class WorkRequestListApiResponse
    {
        public List<WorkRequestResponseModel> data { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
