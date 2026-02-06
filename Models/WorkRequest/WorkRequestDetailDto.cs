using cfm_frontend.DTOs.ServiceProvider;

namespace cfm_frontend.Models.WorkRequest
{
    /// <summary>
    /// Main DTO for work request detail API response
    /// </summary>
    public class WorkRequestFormDetailDto
    {
        public int IdWorkRequest { get; set; }
        public string WorkTitle { get; set; } = string.Empty;
        public string WorkRequestCode { get; set; } = string.Empty;
        public double? CostEstimation { get; set; }
        public int? CostEstimationCurrency_idEnum { get; set; }
        public string? CostEstimationCurrency { get; set; }
        public LocationsDto? Location { get; set; }
        public WorkRequestRequestorDto? Requestor { get; set; }
        public WorkRequestPicDto? Pic { get; set; }
        public string? RequestDetail { get; set; }
        public string? Solution { get; set; }
        public DateTime RequestDate { get; set; }
        public WorkRequestPpmReference? ReferenceToPPM { get; set; }
        public WorkRequestPriorityLevel? PriorityLevel { get; set; }
        public WorkRequestStageSchedule? HelpdeskResponse { get; set; }
        public WorkRequestStageSchedule? InitialFollowUp { get; set; }
        public WorkRequestStageSchedule? QuotationSubmission { get; set; }
        public WorkRequestCostApproverGroup? OutstandingCostApproval { get; set; }
        public WorkRequestStageSchedule? CostApproval { get; set; }
        public WorkRequestStageSchedule? WorkCompletion { get; set; }
        public WorkRequestStageSchedule? AfterWorkFollowUp { get; set; }
        public EnumFormDetailResponse? RequestMethod { get; set; }
        public ServiceProviderFormDetailResponse? ServiceProvider { get; set; }
        public EnumFormDetailResponse? Status { get; set; }
        public EnumFormDetailResponse? Feedback { get; set; }
        public string? FeedbackSummary { get; set; }
        public TypeFormDetailResponse? WorkCategory { get; set; }
        public TypeFormDetailResponse? OtherCategory { get; set; }
        public TypeFormDetailResponse? OtherCategory2 { get; set; }
        public List<WorkRequestAdditionalInformation>? ImportantChecklists { get; set; }
        public List<WorkRequestWorkerDto>? WorkerFromCompany { get; set; }
        public List<WorkRequestWorkerDto>? WorkerFromServiceProvider { get; set; }
        public List<WorkRequestLaborMaterial>? LaborMaterials { get; set; }
        public List<WorkRequestRelatedAssetDto>? RelatesAssets { get; set; }
        public List<WorkRequestDocumentViewDto>? RelatedDocuments { get; set; }
        public List<WorkRequestWorkUpdateEmail>? WorkUpdateEmails { get; set; }
    }

    #region Location DTOs

    public class LocationsDto
    {
        public WorkRequestPropertyDto? Property { get; set; }
        public WorkRequestPropertyTypeDto? PropertyType { get; set; }
        public WorkRequestPropertyFloorDto? FloorDetail { get; set; }
        public WorkRequestRoomZoneDto? RoomZone { get; set; }
    }

    public class WorkRequestPropertyDto
    {
        public int IdProperty { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public int? IdCity { get; set; }
        public string? CityName { get; set; }
        public int? IdState { get; set; }
        public string? StateName { get; set; }
        public int? IdCountry { get; set; }
        public string? CountryName { get; set; }
        public int? IdPropertyType { get; set; }
    }

    public class WorkRequestPropertyTypeDto
    {
        public int IdPropertyType { get; set; }
        public string PropertyType { get; set; } = string.Empty;
    }

    public class WorkRequestPropertyFloorDto
    {
        public int IdPropertyFloor { get; set; }
        public string FloorUnitName { get; set; } = string.Empty;
    }

    public class WorkRequestRoomZoneDto
    {
        public int IdRoomZone { get; set; }
        public string RoomZoneName { get; set; } = string.Empty;
    }

    #endregion

    #region Person DTOs

    public class WorkRequestRequestorDto
    {
        public int IdEmployee { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string? Title { get; set; }
        public string? EmailAddress { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class WorkRequestPicDto
    {
        public int Employee_idEmployee { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string? Title { get; set; }
        public string? EmailAddress { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class WorkRequestWorkerDto
    {
        public int? Side_idEnum { get; set; }
        public string? WorkerSide { get; set; }
        public int IdEmployee { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string? Title { get; set; }
        public string? EmailAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsJoinToExternalChatRoom { get; set; }
    }

    #endregion

    #region Asset DTOs

    public class WorkRequestRelatedAssetDto
    {
        public int? IdRelatedAsset { get; set; }
        public int IdAsset { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? OtherCode { get; set; }
    }

    #endregion

    #region Work Request Related DTOs

    public class WorkRequestCostApproverGroup
    {
        public int? IdCostApproval { get; set; }
        public string? CostApprovalCode { get; set; }
        public int? IdCostApproverSubGroup { get; set; }
        public string? CostApproverSubGroupName { get; set; }
    }

    public class WorkRequestPpmReference
    {
        public int IdMaintenanceSchedule { get; set; }
        public int? IdMaintenanceActivity { get; set; }
        public string ReferenceCode { get; set; } = string.Empty;
        public string WorkTitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? Status_idEnum { get; set; }
        public DateTime CompletionDate { get; set; }
    }

    public class WorkRequestPriorityLevel
    {
        public int IdPrioriryLevel { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class WorkRequestAdditionalInformation
    {
        public int IdWorkRequestAdditionalInformation { get; set; }
        public int IdType { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsChecked { get; set; }
    }

    public class WorkRequestStageSchedule
    {
        public DateTime? ActualDate { get; set; }
        public DateTime? TargetDate { get; set; }
        public string? TargetChangeNote { get; set; }
        public string? Summary { get; set; }
    }

    public class WorkRequestLaborMaterial
    {
        public int IdWorkRequestLaborMaterial { get; set; }
        public bool IsAdhoc { get; set; }
        public int? IdJobCode { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? Label_idEnum { get; set; }
        public string? Label { get; set; }
        public int? PriceCurrency_idEnum { get; set; }
        public string? PriceCurrency { get; set; }
        public double Price { get; set; }
        public double Quantity { get; set; }
        public int? MeasurementUnit_idEnum { get; set; }
        public string? MeasurementUnit { get; set; }
    }

    public class WorkRequestDocumentViewDto
    {
        public int IdWorkRequest_Document { get; set; }
        public int IdWorkRequest { get; set; }
        public int IdDocument { get; set; }
        public string? DocumentName { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string DocumentUrl { get; set; } = string.Empty;
    }

    public class WorkRequestWorkUpdateEmail
    {
        public string EmailCode { get; set; } = string.Empty;
        public string EmailUrl { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime SentDate { get; set; }
    }

    #endregion
}
