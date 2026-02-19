namespace cfm_frontend.DTOs.WorkRequest
{
    /// <summary>
    /// DTO for updating an existing work request
    /// Extends the create structure with idWorkRequest and isActiveData flags for collections
    /// </summary>
    public class WorkRequestUpdateRequest
    {
        // Work Request ID (required for update)
        public int idWorkRequest { get; set; }

        // Location Details
        public int Property_idProperty { get; set; }
        public int? PropertyFloor_idPropertyFloor { get; set; }
        public int? RoomZone_idRoomZone { get; set; }

        // Requestor and Work Details
        public int requestor_Employee_idEmployee { get; set; }
        public string workTitle { get; set; }
        public string requestDetail { get; set; }
        public string solutionDetail { get; set; }

        // Request Method and Status
        public int requestMethod_Enum_idEnum { get; set; }
        public int status_Enum_idEnum { get; set; }

        // Work Categories
        public int? workCategory_Type_idType { get; set; }
        public int? customCategory_Type_idType { get; set; }
        public int? customCategory2_Type_idType { get; set; }
        public bool IsPMFinding { get; set; }

        // Person in Charge and Worker
        public int pic_Employee_idEmployee { get; set; }
        public int? ServiceProvider_idServiceProvider { get; set; }

        // Important Checklist
        public List<AdditionalInformationDto> ImportantChecklist { get; set; } = new List<AdditionalInformationDto>();

        // Cost Estimation
        public int? costEstimationCurrency_Enum_idEnum { get; set; }
        public int? costEstimation_Approval_idApproval { get; set; }
        public float? costEstimation { get; set; }

        // Timeline and Dates
        public int PriorityLevel_idPriorityLevel { get; set; }
        public int TimeZone_idTimeZone { get; set; }
        public DateTime requestDate { get; set; }

        // Helpdesk Response
        public DateTime? helpdeskResponse { get; set; }
        public DateTime? helpdeskResponseTarget { get; set; }
        public string helpdeskResponseTargetChangeNote { get; set; }

        // Initial Follow Up
        public DateTime? onsiteResponse { get; set; }
        public DateTime? onsiteResponseTarget { get; set; }
        public string onsiteResponseTargetChangeNote { get; set; }

        // Quotation Submission
        public DateTime? quotationSubmission { get; set; }
        public DateTime? quotationSubmissionTarget { get; set; }
        public string quotationSubmissionTargetChangeNote { get; set; }

        // Cost Approval
        public DateTime? costApproval { get; set; }
        public DateTime? costApprovalTarget { get; set; }
        public string costApprovalTargetChangeNote { get; set; }

        // Work Completion
        public DateTime? workCompletion { get; set; }
        public DateTime? workCompletionTarget { get; set; }
        public string workCompletionTargetChangeNote { get; set; }

        // After Work Follow Up
        public DateTime? followUp { get; set; }
        public DateTime? followUpTarget { get; set; }
        public string followUpTargetChangeNote { get; set; }

        // Summary and Feedback
        public string followUpDetail { get; set; }
        public int? feedbackType_Enum_idEnum { get; set; }
        public string feedbackSummary { get; set; }

        // System fields
        public int Client_idClient { get; set; }
        public int IdEmployee { get; set; }

        // Labor/Material collections with isActiveData support
        public List<MaterialJobCodeUpdateDto> Material_Jobcode { get; set; } = new List<MaterialJobCodeUpdateDto>();
        public List<MaterialAdhocUpdateDto> Material_Adhoc { get; set; } = new List<MaterialAdhocUpdateDto>();

        // Related Asset with isActiveData support
        public List<AssetUpdateDto> Assets { get; set; } = new List<AssetUpdateDto>();

        // Workers with isActiveData support
        public List<WorkerUpdateDto> Workers { get; set; } = new List<WorkerUpdateDto>();

        // Related Documents with isActiveData support
        public List<RelatedDocumentUpdateDto> RelatedDocuments { get; set; } = new List<RelatedDocumentUpdateDto>();

        // Unused/future fields
        public int? TransactionChannel_idTransactionChannel { get; set; }
        public int? pmReferenceNumber_MaintenanceSchedule_idMaintenanceSchedule { get; set; }
        public int? inspectionRecordQuestionReferenceNumber_InspectionRecordQuestion_idInspectionRecordQuestion { get; set; }
    }

    /// <summary>
    /// Material from Job Code with inventory transaction date and active flag
    /// </summary>
    public class MaterialJobCodeUpdateDto
    {
        public int idJobCode { get; set; }
        public string JobCode { get; set; }
        public float quantity { get; set; }
        public float unitPrice { get; set; }
        public DateTime? inventoryTransactionDate { get; set; }
        public bool isActiveData { get; set; } = true;
    }

    /// <summary>
    /// Ad-hoc labor/material with ID for existing items and active flag
    /// idWorkRequest_AdHocLaborAndMaterial = 0 for new items, >0 for existing
    /// </summary>
    public class MaterialAdhocUpdateDto
    {
        public int idWorkRequest_AdHocLaborAndMaterial { get; set; }
        public string name { get; set; }
        public int label_Enum_idEnum { get; set; }
        public int unitPriceCurrency_Enum_idEnum { get; set; }
        public float unitPrice { get; set; }
        public float quantity { get; set; }
        public int measurementUnit_Enum_idEnum { get; set; }
        public bool isActiveData { get; set; } = true;
    }

    /// <summary>
    /// Asset with active flag for update operations
    /// </summary>
    public class AssetUpdateDto
    {
        public int idAsset { get; set; }
        public string asset { get; set; }
        public bool isActiveData { get; set; } = true;
    }

    /// <summary>
    /// Worker with active flag for update operations
    /// </summary>
    public class WorkerUpdateDto
    {
        public int Employee_idEmployee { get; set; }
        public int side_Enum_idEnum { get; set; }
        public bool isJoinToExternalChatRoom { get; set; }
        public bool HasAccess { get; set; }
        public bool isActiveData { get; set; } = true;
    }

    /// <summary>
    /// Document with ID for existing items and active flag
    /// idDocument = 0 for new documents (include base64), >0 for existing
    /// </summary>
    public class RelatedDocumentUpdateDto
    {
        public int idDocument { get; set; }
        public string documentName { get; set; }
        public string fileName { get; set; }
        public long fileSize { get; set; }
        public string extension { get; set; }
        public string documentUrl { get; set; }
        public string base64 { get; set; }
        public string documentType { get; set; }
        public bool isActiveData { get; set; } = true;
    }
}
