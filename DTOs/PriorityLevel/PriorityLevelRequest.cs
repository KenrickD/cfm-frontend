namespace cfm_frontend.DTOs.PriorityLevel
{
    public class PriorityLevelCreateRequest
    {
        public int IdClient { get; set; }
        public string Name { get; set; }
        public string VisualColor { get; set; }

        // Helpdesk Response Target
        public int HelpdeskResponseTargetDays { get; set; }
        public int HelpdeskResponseTargetHours { get; set; }
        public int HelpdeskResponseTargetMinutes { get; set; }
        public bool HelpdeskResponseTargetWithinOfficeHours { get; set; }
        public string HelpdeskResponseTargetReference { get; set; }
        public bool HelpdeskResponseTargetRequiredToFill { get; set; }
        public bool HelpdeskResponseTargetActivateCompliance { get; set; }
        public int HelpdeskResponseTargetComplianceDurationDays { get; set; }
        public int HelpdeskResponseTargetComplianceDurationHours { get; set; }
        public int HelpdeskResponseTargetComplianceDurationMinutes { get; set; }
        public bool HelpdeskResponseTargetAcknowledgeActual { get; set; }
        public bool HelpdeskResponseTargetAcknowledgeTargetChanged { get; set; }
        public bool HelpdeskResponseTargetReminderBeforeTarget { get; set; }

        // Initial Follow Up Target
        public int InitialFollowUpTargetDays { get; set; }
        public int InitialFollowUpTargetHours { get; set; }
        public int InitialFollowUpTargetMinutes { get; set; }
        public bool InitialFollowUpTargetWithinOfficeHours { get; set; }
        public string InitialFollowUpTargetReference { get; set; }
        public bool InitialFollowUpTargetRequiredToFill { get; set; }
        public bool InitialFollowUpTargetActivateCompliance { get; set; }
        public bool InitialFollowUpTargetAcknowledgeActual { get; set; }
        public bool InitialFollowUpTargetAcknowledgeTargetChanged { get; set; }
        public bool InitialFollowUpTargetReminderBeforeTarget { get; set; }

        // Quotation Submission Target
        public int QuotationSubmissionTargetDays { get; set; }
        public int QuotationSubmissionTargetHours { get; set; }
        public int QuotationSubmissionTargetMinutes { get; set; }
        public bool QuotationSubmissionTargetWithinOfficeHours { get; set; }
        public string QuotationSubmissionTargetReference { get; set; }
        public bool QuotationSubmissionTargetRequiredToFill { get; set; }
        public bool QuotationSubmissionTargetActivateCompliance { get; set; }
        public bool QuotationSubmissionTargetAcknowledgeActual { get; set; }
        public bool QuotationSubmissionTargetAcknowledgeTargetChanged { get; set; }
        public bool QuotationSubmissionTargetReminderBeforeTarget { get; set; }

        // Cost Approval Target
        public int CostApprovalTargetDays { get; set; }
        public int CostApprovalTargetHours { get; set; }
        public int CostApprovalTargetMinutes { get; set; }
        public bool CostApprovalTargetWithinOfficeHours { get; set; }
        public string CostApprovalTargetReference { get; set; }
        public bool CostApprovalTargetRequiredToFill { get; set; }
        public bool CostApprovalTargetActivateCompliance { get; set; }
        public bool CostApprovalTargetAcknowledgeActual { get; set; }
        public bool CostApprovalTargetAcknowledgeTargetChanged { get; set; }
        public bool CostApprovalTargetReminderBeforeTarget { get; set; }

        // Work Completion Target
        public int WorkCompletionTargetDays { get; set; }
        public int WorkCompletionTargetHours { get; set; }
        public int WorkCompletionTargetMinutes { get; set; }
        public bool WorkCompletionTargetWithinOfficeHours { get; set; }
        public string WorkCompletionTargetReference { get; set; }
        public bool WorkCompletionTargetRequiredToFill { get; set; }
        public bool WorkCompletionTargetActivateCompliance { get; set; }
        public bool WorkCompletionTargetAcknowledgeActual { get; set; }
        public bool WorkCompletionTargetAcknowledgeTargetChanged { get; set; }
        public bool WorkCompletionTargetReminderBeforeTarget { get; set; }

        // After Work Follow Up Target
        public int AfterWorkFollowUpTargetDays { get; set; }
        public int AfterWorkFollowUpTargetHours { get; set; }
        public int AfterWorkFollowUpTargetMinutes { get; set; }
        public bool AfterWorkFollowUpTargetWithinOfficeHours { get; set; }
        public string AfterWorkFollowUpTargetReference { get; set; }
        public bool AfterWorkFollowUpTargetRequiredToFill { get; set; }
        public bool AfterWorkFollowUpTargetActivateCompliance { get; set; }
        public bool AfterWorkFollowUpTargetAcknowledgeActual { get; set; }
        public bool AfterWorkFollowUpTargetAcknowledgeTargetChanged { get; set; }
        public bool AfterWorkFollowUpTargetReminderBeforeTarget { get; set; }
        public bool AfterWorkFollowUpTargetActivateAutoFill { get; set; }
    }

    public class PriorityLevelUpdateRequest : PriorityLevelCreateRequest
    {
        public int Id { get; set; }
    }
}
