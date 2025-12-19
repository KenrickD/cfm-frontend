namespace cfm_frontend.DTOs.WorkRequest
{
    public class WorkRequestCreateRequest
    {
        // Location Details
        public int IdLocation { get; set; }
        public int IdFloor { get; set; }
        public int IdRoom { get; set; }

        // Requestor and Work Details
        public int? IdRequestor { get; set; }
        public string RequestorName { get; set; }
        public string WorkTitle { get; set; }
        public string RequestDetail { get; set; }
        public string Solution { get; set; }

        // Request Method and Status
        public string RequestMethod { get; set; }
        public string Status { get; set; }

        // Work Categories
        public int IdWorkCategory { get; set; }
        public int? IdOtherCategory { get; set; }
        public int? IdOtherCategory2 { get; set; }
        public bool IsPMFinding { get; set; }

        // Person in Charge and Worker
        public int IdPersonInCharge { get; set; }
        public int? IdServiceProvider { get; set; }
        public int? IdWorker { get; set; }
        public string WorkerName { get; set; }

        // Important Checklist
        public bool IsPermitRequired { get; set; }
        public bool HasHazardousMaterial { get; set; }
        public bool RequiresIsolation { get; set; }
        public bool WorkAtHeight { get; set; }
        public bool RequiresLifting { get; set; }
        public bool RequiresSafeguarding { get; set; }
        public bool RequiresJSA { get; set; }

        // Labor/Material (as JSON string)
        public string LaborMaterialJson { get; set; }

        // Cost Estimation
        public decimal? CostEstimation { get; set; }

        // Timeline and Dates
        public string PriorityLevel { get; set; }
        public DateTime RequestDate { get; set; }

        // Helpdesk Response
        public DateTime? HelpdeskResponseDate { get; set; }
        public DateTime? HelpdeskResponseTarget { get; set; }
        public string HelpdeskResponseRemark { get; set; }

        // Initial Follow Up
        public DateTime? InitialFollowUpDate { get; set; }
        public DateTime? InitialFollowUpTarget { get; set; }
        public string InitialFollowUpRemark { get; set; }

        // Quotation Submission
        public DateTime? QuotationSubmissionDate { get; set; }
        public DateTime? QuotationSubmissionTarget { get; set; }
        public string QuotationSubmissionRemark { get; set; }

        // Cost Approval
        public DateTime? CostApprovalDate { get; set; }
        public DateTime? CostApprovalTarget { get; set; }
        public string CostApprovalRemark { get; set; }

        // Work Completion
        public DateTime? WorkCompletionDate { get; set; }
        public DateTime? WorkCompletionTarget { get; set; }
        public string WorkCompletionRemark { get; set; }

        // After Work Follow Up
        public DateTime? AfterWorkFollowUpDate { get; set; }
        public DateTime? AfterWorkFollowUpTarget { get; set; }
        public string AfterWorkFollowUpRemark { get; set; }

        // Summary and Feedback
        public string FollowUpSummary { get; set; }
        public string FeedbackStatus { get; set; }
        public string FeedbackSummary { get; set; }

        // System fields
        public int IdClient { get; set; }
        public int IdEmployee { get; set; }
        public bool IsDraft { get; set; }
    }
}
