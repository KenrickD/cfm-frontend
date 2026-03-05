using System.ComponentModel.DataAnnotations;

namespace cfm_frontend.DTOs.WorkRequest
{
    /// <summary>
    /// DTO for sending simplified work request (lite version)
    /// Maps to backend SendWorkRequestDetailParam
    /// </summary>
    public class SendWorkRequestPayloadDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Client_idClient couldn't = 0")]
        public int Client_idClient { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Property_idProperty couldn't = 0")]
        public int Property_idProperty { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "PropertyFloor_idPropertyFloor couldn't = 0")]
        public int? PropertyFloor_idPropertyFloor { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "RoomZone_idRoomZone couldn't = 0")]
        public int? RoomZone_idRoomZone { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "requestor_Employee_idEmployee couldn't = 0")]
        public int requestor_Employee_idEmployee { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "workCategory_Type_idType couldn't = 0")]
        public int? workCategory_Type_idType { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TimeZone_idTimeZone couldn't = 0")]
        public int TimeZone_idTimeZone { get; set; }

        [Required(ErrorMessage = "Request is Mandatory")]
        public string requestDetail { get; set; }

        [MaxLength(10, ErrorMessage = "Maximum of 10 documents are allowed")]
        public List<RelatedDocumentDto>? RelatedDocuments { get; set; }
    }

    /// <summary>
    /// DTO for work request list item in SendNewWorkRequest page
    /// Maps to backend SendWorkRequestListResponse
    /// </summary>
    public class SendWorkRequestListItemDto
    {
        public int IdWorkRequest { get; set; }
        public string WorkRequestCode { get; set; } = string.Empty;
        public string WorkTitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int IdRequestor_Employee { get; set; }
        public string RequestorName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string Floor { get; set; } = string.Empty;
        public string RoomZone { get; set; } = string.Empty;
        public string WorkCategory { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
    }
}
