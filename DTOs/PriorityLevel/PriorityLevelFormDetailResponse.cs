namespace cfm_frontend.DTOs.PriorityLevel
{
    /// <summary>
    /// DTO matching the backend API response for priority level details.
    /// Duration fields are Int64 (TimeSpan ticks) that need to be converted to days/hours/minutes.
    /// </summary>
    public class PriorityLevelFormDetailResponse
    {
        public int idPriorityLevel { get; set; }
        public string name { get; set; }

        // Helpdesk Response Target
        public long? helpdeskResponseTarget { get; set; }
        public bool? helpdeskResponseTargetIsWithinOfficeHours { get; set; }
        public bool? helpdeskResponseTargetIsMandatory { get; set; }
        public long? helpdeskResponseComplienceDuration { get; set; }
        public bool isAcknowledgeRequestor_HelpdeskResponse { get; set; }
        public bool isAcknowledgeRequestor_HelpdeskResponseTargetChanges { get; set; }
        public long? helpdeskResponse_BeforeTargetReminder { get; set; }

        // Initial Follow Up Target
        public long? initialFollowUpTarget { get; set; }
        public int? initialFollowUpTargetCalculation_Enum_idEnum { get; set; }
        public string? initialFollowUpTargetCalculationText { get; set; }
        public bool? initialFollowUpTargetIsWithinOfficeHours { get; set; }
        public bool? initialFollowUpTargetIsMandatory { get; set; }
        public long? initialFollowUpComplienceDuration { get; set; }
        public bool isAcknowledgeRequestor_InitialFollowUp { get; set; }
        public bool isAcknowledgeRequestor_InitialFollowUpTargetChanges { get; set; }
        public long? initialFollowUp_BeforeTargetReminder { get; set; }

        // Quotation Submission Target
        public long? quotationSubmissionTarget { get; set; }
        public int? quotationSubmissionTargetCalculation_Enum_idEnum { get; set; }
        public string? quotationSubmissionTargetCalculationText { get; set; }
        public bool? quotationSubmissionTargetIsWithinOfficeHours { get; set; }
        public bool? quotationSubmissionTargetIsMandatory { get; set; }
        public long? quotationSubmissionComplienceDuration { get; set; }
        public bool isAcknowledgeRequestor_QuotationSubmission { get; set; }
        public bool isAcknowledgeRequestor_QuotationSubmissionTargetChanges { get; set; }
        public long? quotationSubmission_BeforeTargetReminder { get; set; }

        // Cost Approval Target
        public long? costApprovalTarget { get; set; }
        public int? costApprovalTargetCalculation_Enum_idEnum { get; set; }
        public string? costApprovalTargetCalculationText { get; set; }
        public bool? costApprovalTargetIsWithinOfficeHours { get; set; }
        public bool? costApprovalTargetIsMandatory { get; set; }
        public long? costApprovalComplienceDuration { get; set; }
        public bool isAcknowledgeRequestor_CostApproval { get; set; }
        public bool isAcknowledgeRequestor_CostApprovalTargetChanges { get; set; }
        public long? costApproval_BeforeTargetReminder { get; set; }

        // Work Completion Target
        public long? workCompletionTarget { get; set; }
        public int? workCompletionTargetCalculation_Enum_idEnum { get; set; }
        public string? workCompletionTargetCalculationText { get; set; }
        public bool? workCompletionTargetIsWithinOfficeHours { get; set; }
        public bool? workCompletionTargetIsMandatory { get; set; }
        public long? workCompletionComplienceDuration { get; set; }
        public bool isAcknowledgeRequestor_WorkCompletion { get; set; }
        public bool isAcknowledgeRequestor_WorkCompletionTargetChanges { get; set; }
        public long? workCompletion_BeforeTargetReminder { get; set; }

        // After Work Follow Up Target
        public long? afterWorkFollowUpTarget { get; set; }
        public int? afterWorkFollowUpTargetCalculation_Enum_idEnum { get; set; }
        public string? afterWorkFollowUpTargetCalculationText { get; set; }
        public bool? afterWorkFollowUpTargetIsWithinOfficeHours { get; set; }
        public bool? afterWorkFollowUpTargetIsMandatory { get; set; }
        public long? afterWorkFollowUpComplienceDuration { get; set; }
        public bool isAcknowledgeRequestor_AfterWorkFollowUp { get; set; }
        public bool isAcknowledgeRequestor_AfterWorkFollowUpTargetChanges { get; set; }
        public long? afterWorkFollowUp_BeforeTargetReminder { get; set; }
        public bool isAutoFill_AfterWorkFollowUp { get; set; }

        // Visual and display
        public int? visualColor_Enum_idEnum { get; set; }
        public int? displayOrder { get; set; }

        /// <summary>
        /// Converts this API response to the frontend PriorityLevelModel.
        /// TimeSpan ticks are converted to days/hours/minutes.
        /// </summary>
        public Models.PriorityLevelModel ToModel()
        {
            return new Models.PriorityLevelModel
            {
                Id = idPriorityLevel,
                Name = name ?? string.Empty,
                VisualColor = visualColor_Enum_idEnum?.ToString(),

                // Helpdesk Response Target
                HelpdeskResponseTargetDays = GetDays(helpdeskResponseTarget),
                HelpdeskResponseTargetHours = GetHours(helpdeskResponseTarget),
                HelpdeskResponseTargetMinutes = GetMinutes(helpdeskResponseTarget),
                HelpdeskResponseTargetWithinOfficeHours = helpdeskResponseTargetIsWithinOfficeHours ?? false,
                HelpdeskResponseTargetRequiredToFill = helpdeskResponseTargetIsMandatory ?? false,
                HelpdeskResponseTargetActivateCompliance = helpdeskResponseComplienceDuration.HasValue && helpdeskResponseComplienceDuration > 0,
                HelpdeskResponseTargetComplianceDurationDays = GetDays(helpdeskResponseComplienceDuration),
                HelpdeskResponseTargetComplianceDurationHours = GetHours(helpdeskResponseComplienceDuration),
                HelpdeskResponseTargetComplianceDurationMinutes = GetMinutes(helpdeskResponseComplienceDuration),
                HelpdeskResponseTargetAcknowledgeActual = isAcknowledgeRequestor_HelpdeskResponse,
                HelpdeskResponseTargetAcknowledgeTargetChanged = isAcknowledgeRequestor_HelpdeskResponseTargetChanges,
                HelpdeskResponseTargetReminderBeforeTarget = helpdeskResponse_BeforeTargetReminder.HasValue && helpdeskResponse_BeforeTargetReminder > 0,

                // Initial Follow Up Target
                InitialFollowUpTargetDays = GetDays(initialFollowUpTarget),
                InitialFollowUpTargetHours = GetHours(initialFollowUpTarget),
                InitialFollowUpTargetMinutes = GetMinutes(initialFollowUpTarget),
                InitialFollowUpTargetWithinOfficeHours = initialFollowUpTargetIsWithinOfficeHours ?? false,
                InitialFollowUpTargetReference = initialFollowUpTargetCalculationText ?? string.Empty,
                InitialFollowUpTargetRequiredToFill = initialFollowUpTargetIsMandatory ?? false,
                InitialFollowUpTargetActivateCompliance = initialFollowUpComplienceDuration.HasValue && initialFollowUpComplienceDuration > 0,
                InitialFollowUpTargetAcknowledgeActual = isAcknowledgeRequestor_InitialFollowUp,
                InitialFollowUpTargetAcknowledgeTargetChanged = isAcknowledgeRequestor_InitialFollowUpTargetChanges,
                InitialFollowUpTargetReminderBeforeTarget = initialFollowUp_BeforeTargetReminder.HasValue && initialFollowUp_BeforeTargetReminder > 0,

                // Quotation Submission Target
                QuotationSubmissionTargetDays = GetDays(quotationSubmissionTarget),
                QuotationSubmissionTargetHours = GetHours(quotationSubmissionTarget),
                QuotationSubmissionTargetMinutes = GetMinutes(quotationSubmissionTarget),
                QuotationSubmissionTargetWithinOfficeHours = quotationSubmissionTargetIsWithinOfficeHours ?? false,
                QuotationSubmissionTargetReference = quotationSubmissionTargetCalculationText ?? string.Empty,
                QuotationSubmissionTargetRequiredToFill = quotationSubmissionTargetIsMandatory ?? false,
                QuotationSubmissionTargetActivateCompliance = quotationSubmissionComplienceDuration.HasValue && quotationSubmissionComplienceDuration > 0,
                QuotationSubmissionTargetAcknowledgeActual = isAcknowledgeRequestor_QuotationSubmission,
                QuotationSubmissionTargetAcknowledgeTargetChanged = isAcknowledgeRequestor_QuotationSubmissionTargetChanges,
                QuotationSubmissionTargetReminderBeforeTarget = quotationSubmission_BeforeTargetReminder.HasValue && quotationSubmission_BeforeTargetReminder > 0,

                // Cost Approval Target
                CostApprovalTargetDays = GetDays(costApprovalTarget),
                CostApprovalTargetHours = GetHours(costApprovalTarget),
                CostApprovalTargetMinutes = GetMinutes(costApprovalTarget),
                CostApprovalTargetWithinOfficeHours = costApprovalTargetIsWithinOfficeHours ?? false,
                CostApprovalTargetReference = costApprovalTargetCalculationText ?? string.Empty,
                CostApprovalTargetRequiredToFill = costApprovalTargetIsMandatory ?? false,
                CostApprovalTargetActivateCompliance = costApprovalComplienceDuration.HasValue && costApprovalComplienceDuration > 0,
                CostApprovalTargetAcknowledgeActual = isAcknowledgeRequestor_CostApproval,
                CostApprovalTargetAcknowledgeTargetChanged = isAcknowledgeRequestor_CostApprovalTargetChanges,
                CostApprovalTargetReminderBeforeTarget = costApproval_BeforeTargetReminder.HasValue && costApproval_BeforeTargetReminder > 0,

                // Work Completion Target
                WorkCompletionTargetDays = GetDays(workCompletionTarget),
                WorkCompletionTargetHours = GetHours(workCompletionTarget),
                WorkCompletionTargetMinutes = GetMinutes(workCompletionTarget),
                WorkCompletionTargetWithinOfficeHours = workCompletionTargetIsWithinOfficeHours ?? false,
                WorkCompletionTargetReference = workCompletionTargetCalculationText ?? string.Empty,
                WorkCompletionTargetRequiredToFill = workCompletionTargetIsMandatory ?? false,
                WorkCompletionTargetActivateCompliance = workCompletionComplienceDuration.HasValue && workCompletionComplienceDuration > 0,
                WorkCompletionTargetAcknowledgeActual = isAcknowledgeRequestor_WorkCompletion,
                WorkCompletionTargetAcknowledgeTargetChanged = isAcknowledgeRequestor_WorkCompletionTargetChanges,
                WorkCompletionTargetReminderBeforeTarget = workCompletion_BeforeTargetReminder.HasValue && workCompletion_BeforeTargetReminder > 0,

                // After Work Follow Up Target
                AfterWorkFollowUpTargetDays = GetDays(afterWorkFollowUpTarget),
                AfterWorkFollowUpTargetHours = GetHours(afterWorkFollowUpTarget),
                AfterWorkFollowUpTargetMinutes = GetMinutes(afterWorkFollowUpTarget),
                AfterWorkFollowUpTargetWithinOfficeHours = afterWorkFollowUpTargetIsWithinOfficeHours ?? false,
                AfterWorkFollowUpTargetReference = afterWorkFollowUpTargetCalculationText ?? string.Empty,
                AfterWorkFollowUpTargetRequiredToFill = afterWorkFollowUpTargetIsMandatory ?? false,
                AfterWorkFollowUpTargetActivateCompliance = afterWorkFollowUpComplienceDuration.HasValue && afterWorkFollowUpComplienceDuration > 0,
                AfterWorkFollowUpTargetAcknowledgeActual = isAcknowledgeRequestor_AfterWorkFollowUp,
                AfterWorkFollowUpTargetAcknowledgeTargetChanged = isAcknowledgeRequestor_AfterWorkFollowUpTargetChanges,
                AfterWorkFollowUpTargetReminderBeforeTarget = afterWorkFollowUp_BeforeTargetReminder.HasValue && afterWorkFollowUp_BeforeTargetReminder > 0,
                AfterWorkFollowUpTargetActivateAutoFill = isAutoFill_AfterWorkFollowUp
            };
        }

        /// <summary>
        /// Extracts days from TimeSpan ticks
        /// </summary>
        private static int GetDays(long? ticks)
        {
            if (!ticks.HasValue || ticks.Value <= 0) return 0;
            var timeSpan = TimeSpan.FromTicks(ticks.Value);
            return timeSpan.Days;
        }

        /// <summary>
        /// Extracts hours (excluding full days) from TimeSpan ticks
        /// </summary>
        private static int GetHours(long? ticks)
        {
            if (!ticks.HasValue || ticks.Value <= 0) return 0;
            var timeSpan = TimeSpan.FromTicks(ticks.Value);
            return timeSpan.Hours;
        }

        /// <summary>
        /// Extracts minutes (excluding full hours) from TimeSpan ticks
        /// </summary>
        private static int GetMinutes(long? ticks)
        {
            if (!ticks.HasValue || ticks.Value <= 0) return 0;
            var timeSpan = TimeSpan.FromTicks(ticks.Value);
            return timeSpan.Minutes;
        }

    }
}
