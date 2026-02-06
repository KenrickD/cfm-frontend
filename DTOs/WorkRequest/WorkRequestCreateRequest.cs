namespace cfm_frontend.DTOs.WorkRequest
{
    public class WorkRequestCreateRequest
    {
        // Location Details
        public int Property_idProperty { get; set; } // location dropdown 
        public int? PropertyFloor_idPropertyFloor { get; set; } // floor dropdown 
        public int? RoomZone_idRoomZone { get; set; } //room zone

        // Requestor and Work Details
        public int requestor_Employee_idEmployee { get; set; }//requestor just take the employee id
        public string workTitle { get; set; }
        public string requestDetail { get; set; }
        public string solutionDetail { get; set; }//solution

        // Request Method and Status
        public int requestMethod_Enum_idEnum { get; set; }//request method radiobutton
        public int status_Enum_idEnum { get; set; }//status radio button

        // Work Categories
        public int? workCategory_Type_idType { get; set; } //work category  
        public int? customCategory_Type_idType { get; set; } // custom category
        public int? customCategory2_Type_idType { get; set; } // other category 2
        public bool IsPMFinding { get; set; }

        // Person in Charge and Worker
        public int pic_Employee_idEmployee { get; set; }//person in charge
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

        //labor material if using job code
        public List<MaterialJobCodeDto> Material_Jobcode { get; set; } = new List<MaterialJobCodeDto>();
        //labor material if adhoc
        public List<MaterialAdhocDto> Material_Adhoc { get; set; } = new List<MaterialAdhocDto>();
        // related asset 
        public List<AssetDto> Assets { get; set; } = new List<AssetDto>();
        //worker from company + worker from service provider
        public List<WorkerDto> Workers { get; set; } = new List<WorkerDto>();

        // Related Documents (file uploads as base64)
        public List<RelatedDocumentDto> RelatedDocuments { get; set; } = new List<RelatedDocumentDto>();

        //unclear and unused yet
        public int? TransactionChannel_idTransactionChannel { get; set; }
        public int? pmReferenceNumber_MaintenanceSchedule_idMaintenanceSchedule { get; set; }
        public int? inspectionRecordQuestionReferenceNumber_InspectionRecordQuestion_idInspectionRecordQuestion { get; set; }

    }
    public class MaterialJobCodeDto
    {
        public int idJobCode { get; set; }
        public string JobCode { get; set; }
        public float quantity { get; set; }
        public float unitPrice { get; set; }
    }

    public class MaterialAdhocDto
    {

        public string name { get; set; }
        public int label_Enum_idEnum { get; set; }
        public int unitPriceCurrency_Enum_idEnum { get; set; }
        public float unitPrice { get; set; }
        public float quantity { get; set; }
        public int measurementUnit_Enum_idEnum { get; set; }
    }

    public class AssetDto
    {
        public int idAsset { get; set; }
        public string asset { get; set; }
    }

    public class WorkerDto
    {
        public int Employee_idEmployee { get; set; }
        public int side_Enum_idEnum { get; set; }
        public bool isJoinToExternalChatRoom { get; set; }
        public bool HasAccess { get; set; }
    }

    public class AdditionalInformationDto
    {
        public int Type_idType { get; set; }
        public bool value { get; set; }
    }

    public class RelatedDocumentDto
    {
        public int idDocument { get; set; }
        public string documentName { get; set; }
        public string fileName { get; set; }
        public long fileSize { get; set; }
        public string extension { get; set; }
        public string documentUrl { get; set; }
        public string base64 { get; set; }
        public string documentType { get; set; }
    }
}
