namespace cfm_frontend.Constants
{
    /// <summary>
    /// Centralized repository for all backend API endpoints.
    /// Organized hierarchically by domain/feature area following RESTful conventions.
    /// </summary>
    public static class ApiEndpoints
    {
        private const string Api = "/api";
        private const string Version = "/v1";
        private const string ApiBase = Api + Version;
        #region Authentication

        /// <summary>
        /// Authentication and user management endpoints
        /// </summary>
        public static class Auth
        {
            private const string Base = Api + "/Auth";

            /// <summary>
            /// POST: Authenticate user with credentials
            /// </summary>
            public const string Login = Base + "/Login";

            /// <summary>
            /// POST: Refresh access token using refresh token
            /// </summary>
            public const string RefreshToken = Base + "/refresh_token";

            /// <summary>
            /// POST: Request password reset email
            /// </summary>
            public const string ForgotPassword = Base + "/ForgotPassword";

            /// <summary>
            /// POST: Reset password with token
            /// </summary>
            public const string ResetPassword = Base + "/ResetPassword";

            /// <summary>
            /// POST: Validate license key for registration
            /// </summary>
            public const string ValidateLicense = Base + "/ValidateLicense";

            /// <summary>
            /// POST: Register new user account
            /// </summary>
            public const string Register = Base + "/Register";

            /// <summary>
            /// GET: Verify email address with token
            /// </summary>
            public const string VerifyEmail = Base + "/VerifyEmail";
        }

        #endregion

        /// <summary>
        /// User info endpoints
        /// </summary>
        #region SessionInfo
        public static class UserInfo
        {
            private const string Base = ApiBase + "/web-user";

            /// <summary>
            /// GET: Get user details
            /// </summary>
            public const string GetUserDetail = Base + "/info";

            /// <summary>
            /// GET: Get user privileges (uses bearer token for user identification)
            /// </summary>
            public const string GetUserPrivileges = Base + "/privileges";
        }
        #endregion

        #region Client Management

        /// <summary>
        /// Client management and switching endpoints
        /// </summary>
        public static class Client
        {
            private const string Base = ApiBase + "/client";

            /// <summary>
            /// POST: Get list of clients for authenticated user
            /// </summary>
            public const string List = Base + "/list";

            /// <summary>
            /// POST: Switch user's active client
            /// </summary>
            public const string Switch = Base + "/switch";
        }

        #endregion

        #region Work Request

        /// <summary>
        /// Work request management endpoints
        /// </summary>
        public static class WorkRequest
        {
            private const string Base = ApiBase + "/work-request";

            /// <summary>
            /// POST: Create new work request
            /// Endpoint: POST /api/v1/work-request
            /// </summary>
            public const string Create = Base;

            /// <summary>
            /// PUT: Update existing work request
            /// Endpoint: PUT /api/v1/work-request
            /// </summary>
            public const string Update = Base;

            /// <summary>
            /// POST: Get work request list with filters
            /// </summary>
            public const string List = Base + "/list";

            /// <summary>
            /// GET: Get all work request statuses
            /// </summary>
            public const string Statuses = Base + "/statuses";

            /// <summary>
            /// GET: Get all filter options for work requests
            /// Query params: idClient
            /// </summary>
            public const string GetFilterOptions = Base + "/list-filter";

            /// <summary>
            /// GET: Get work request by ID
            /// Path params: {id}
            /// Query params: cid (client ID)
            /// </summary>
            public static string GetById(int id) => $"{Base}/{id}";
        }

        #endregion

        #region Property Management

        /// <summary>
        /// Property management endpoints (includes locations, floors, and room zones)
        /// API Version: 1.0
        /// </summary>
        public static class Property
        {
            private const string Base = ApiBase + "/property";

            /// <summary>
            /// GET: Get properties (locations)
            /// Query params: idClient (required), idPropertyType (optional)
            /// API Version: 1.0
            /// </summary>
            public const string List = Base;

            /// <summary>
            /// GET: Get floors for a specific property
            /// Path params: {idProperty}
            /// API Version: 1.0
            /// </summary>
            /// <param name="idProperty">Property ID</param>
            /// <returns>Get floors endpoint URL</returns>
            public static string GetFloors(int idProperty) => $"{Base}/{idProperty}/floors";

            /// <summary>
            /// GET: Get room zones for a specific property and floor
            /// Path params: {idProperty}, {idFloor}
            /// API Version: 1.0
            /// </summary>
            /// <param name="idProperty">Property ID</param>
            /// <param name="idFloor">Floor ID</param>
            /// <returns>Get room zones endpoint URL</returns>
            public static string GetRoomZones(int idProperty, int idFloor) => $"{Base}/{idProperty}/floors/{idFloor}/roomzones";
        }

        /// <summary>
        /// Property group management endpoints
        /// </summary>
        public static class PropertyGroup
        {
            private const string Base = ApiBase + "/propertygroup";

            /// <summary>
            /// GET: Get property groups for client
            /// Query params: idClient
            /// </summary>
            public const string List = Base + "/list";
        }

        #endregion

        #region Employee & Person In Charge

        /// <summary>
        /// Employee and requestor search endpoints
        /// </summary>
        public static class Employee
        {
            private const string Base = ApiBase + "/employee";


            /// <summary>
            /// GET: Search employees/requestors by search term
            /// Query params: term, idCompany
            /// </summary>
            public const string SearchRequestors = Base + "/requestor";

            /// <summary>
            /// GET: Search workers from company
            /// Query params: idCompany, idProperty, prefiks
            /// </summary>
            public const string SearchWorkers = Base + "/worker";
        }

        public static class PersonInCharge
        {
            public const string Base = ApiBase + "/pic";
        }

        #endregion

        #region Service Provider

        /// <summary>
        /// Service provider management endpoints
        /// </summary>
        public static class ServiceProvider
        {
            private const string Base = ApiBase + "/service-provider";

            /// <summary>
            /// GET: Get service providers for client and company
            /// Query params: idClient, idCompany
            /// </summary>
            public const string List = Base;
        }

        #endregion

        #region Lookup Tables

        /// <summary>
        /// Lookup table endpoints for dynamic reference data
        /// </summary>
        public static class Lookup
        {
            private const string Base = ApiBase + "/lookup";

            /// <summary>
            /// GET: Get lookup items by type
            /// Query params: type, idClient (optional depending on type)
            /// Example types: workRequestMethod, workRequestStatus, workRequestAdditionalInformation, workRequestDocument
            /// </summary>
            public const string List = Base + "/list";

            /// <summary>
            /// POST: Create lookup item
            /// Query params: type
            /// </summary>
            public const string Create = Base + "/create";

            /// <summary>
            /// PUT: Update lookup item
            /// Query params: type
            /// </summary>
            public const string Update = Base + "/update";

            /// <summary>
            /// DELETE: Delete lookup item by ID
            /// Path params: {id}
            /// Query params: type
            /// </summary>
            /// <param name="id">Lookup item ID</param>
            /// <returns>Delete endpoint URL</returns>
            public static string Delete(int id) => $"{Base}/delete/{id}";

            /// <summary>
            /// PUT: Update order of lookup items
            /// Query params: type
            /// </summary>
            public const string UpdateOrder = Base + "/updateorder";

            /// <summary>
            /// Common lookup types for type-safe usage
            /// </summary>
            public static class Types
            {
                public const string WorkRequestMethod = "workRequestMethod";
                public const string WorkRequestStatus = "workRequestStatus";
                public const string WorkRequestPriorityLevel = "workRequestPriorityLevel";
                public const string WorkRequestAdditionalInformation = "workRequestAdditionalInformation";
                public const string WorkRequestDocument = "workRequestDocument";
                public const string Currency = "currency";
                public const string MeasurementUnit = "measurementUnit";
                public const string LaborMaterialLabel = "laborMaterialLabel";
            }
        }

        #endregion

        #region Priority Level (Form Detail)

        /// <summary>
        /// Priority Level endpoints for form detail (target date calculation)
        /// Target: GET /api/v1/priority-level?idClient={idClient}
        /// Returns all priority levels with full details for dropdown and target date calculation
        /// Note: This is separate from Settings.PriorityLevel which is for CRUD operations
        /// </summary>
        public static class PriorityLevelDetail
        {
            private const string Base = ApiBase + "/priority-level";

            /// <summary>
            /// GET: Get all priority levels with full details
            /// Query params: idClient
            /// Returns: List of PriorityLevelModel with target date configurations
            /// Used for: Dropdown population and target date calculation
            /// </summary>
            public const string List = Base;
        }

        #endregion

        #region Office Hour & Public Holiday

        /// <summary>
        /// Office Hour management endpoints
        /// Target: GET /api/v{version}/masters/office-hours?idClient={idClient}
        /// </summary>
        public static class OfficeHour
        {
            private const string Base = ApiBase + "/masters/office-hours";

            /// <summary>
            /// GET: Get office hours for client
            /// Query params: idClient
            /// </summary>
            public const string List = Base;
        }

        /// <summary>
        /// Public Holiday management endpoints
        /// Target: GET /api/v{version}/masters/public-holidays/{year}?idClient={idClient}
        /// </summary>
        public static class PublicHoliday
        {
            private const string Base = ApiBase + "/masters/public-holidays";

            /// <summary>
            /// GET: Get public holidays for client by year
            /// Path param: year
            /// Query params: idClient
            /// </summary>
            public static string GetByYear(int year) => $"{Base}/{year}";
        }

        #endregion

        #region Work Category

        /// <summary>
        /// Work category management endpoints (Settings)
        /// Base path: /api/v1/work-request/work-category
        /// </summary>
        public static class WorkCategory
        {
            private const string Base = ApiBase + "/work-request/work-category";

            /// <summary>
            /// GET: Get paginated work categories list
            /// Query params: cid (client id), keyword (search), page (pagination)
            /// Response: Paginated list of TypeFormDetailResponse
            /// </summary>
            public const string List = Base + "/list";

            /// <summary>
            /// GET: Get work category by ID
            /// Path params: {id}
            /// Query params: cid (client id)
            /// Response: Type_PayloadDto structure
            /// </summary>
            public static string GetById(int id) => $"{Base}/{id}";

            /// <summary>
            /// POST: Create new work category
            /// Body: WorkCategoryPayloadDto
            /// Response: int (new ID)
            /// </summary>
            public const string Create = Base;

            /// <summary>
            /// PUT: Update work category
            /// Body: WorkCategoryPayloadDto
            /// Response: int (updated ID)
            /// </summary>
            public const string Update = Base;

            /// <summary>
            /// DELETE: Delete work category by ID
            /// Path params: {id}
            /// Query params: cid (client id)
            /// </summary>
            public static string Delete(int id) => $"{Base}/{id}";
        }

        #endregion

        #region Other Category (V2 - Unified Endpoints)

        /// <summary>
        /// Other Category management endpoints (Settings) - New unified structure
        /// Base path: /api/v1/work-request/other-category
        /// </summary>
        public static class OtherCategoryV2
        {
            private const string Base = ApiBase + "/work-request/other-category";

            /// <summary>
            /// GET: Get paginated other categories list
            /// Query params: cid (client id), keyword (search), page (pagination), limit (page size)
            /// Response: Paginated list of TypeFormDetailResponse
            /// </summary>
            public const string List = Base + "/list";

            /// <summary>
            /// GET: Get other category by ID
            /// Path params: {id}
            /// Query params: cid (client id)
            /// Response: TypePayloadDto structure
            /// </summary>
            public static string GetById(int id) => $"{Base}/{id}";

            /// <summary>
            /// POST: Create new other category
            /// Body: TypePayloadDto
            /// Response: int (new ID)
            /// </summary>
            public const string Create = Base;

            /// <summary>
            /// PUT: Update other category
            /// Body: TypePayloadDto (includes IdType for identifying record)
            /// Response: int (updated ID)
            /// </summary>
            public const string Update = Base;

            /// <summary>
            /// DELETE: Delete other category by ID
            /// Path params: {id}
            /// Query params: cid (client id)
            /// </summary>
            public static string Delete(int id) => $"{Base}/{id}";
        }

        /// <summary>
        /// Other Category 2 management endpoints (Settings) - New unified structure
        /// Base path: /api/v1/work-request/other-category2
        /// </summary>
        public static class OtherCategory2V2
        {
            private const string Base = ApiBase + "/work-request/other-category2";

            /// <summary>
            /// GET: Get paginated other categories 2 list
            /// Query params: cid (client id), keyword (search), page (pagination), limit (page size)
            /// Response: Paginated list of TypeFormDetailResponse
            /// </summary>
            public const string List = Base + "/list";

            /// <summary>
            /// GET: Get other category 2 by ID
            /// Path params: {id}
            /// Query params: cid (client id)
            /// Response: TypePayloadDto structure
            /// </summary>
            public static string GetById(int id) => $"{Base}/{id}";

            /// <summary>
            /// POST: Create new other category 2
            /// Body: TypePayloadDto
            /// Response: int (new ID)
            /// </summary>
            public const string Create = Base;

            /// <summary>
            /// PUT: Update other category 2
            /// Body: TypePayloadDto (includes IdType for identifying record)
            /// Response: int (updated ID)
            /// </summary>
            public const string Update = Base;

            /// <summary>
            /// DELETE: Delete other category 2 by ID
            /// Path params: {id}
            /// Query params: cid (client id)
            /// </summary>
            public static string Delete(int id) => $"{Base}/{id}";
        }

        /// <summary>
        /// Document Label management endpoints (Settings)
        /// Base path: /api/v1/work-request/document-label
        /// </summary>
        public static class DocumentLabelV2
        {
            private const string Base = ApiBase + "/work-request/document-label";

            /// <summary>
            /// GET: Get paginated document labels list
            /// Query params: cid (client id), keyword (search), page (pagination), limit (page size)
            /// Response: Paginated list of TypeFormDetailResponse
            /// </summary>
            public const string List = Base + "/list";

            /// <summary>
            /// GET: Get document label by ID
            /// Path params: {id}
            /// Query params: cid (client id)
            /// Response: TypePayloadDto structure
            /// </summary>
            public static string GetById(int id) => $"{Base}/{id}";

            /// <summary>
            /// POST: Create new document label
            /// Body: TypePayloadDto
            /// Response: int (new ID)
            /// </summary>
            public const string Create = Base;

            /// <summary>
            /// PUT: Update document label
            /// Body: TypePayloadDto (includes IdType for identifying record)
            /// Response: int (updated ID)
            /// </summary>
            public const string Update = Base;

            /// <summary>
            /// DELETE: Delete document label by ID
            /// Path params: {id}
            /// Query params: cid (client id)
            /// </summary>
            public static string Delete(int id) => $"{Base}/{id}";
        }

        /// <summary>
        /// Important Checklist management endpoints (Settings)
        /// Base path: /api/v1/work-request/important-checklist
        /// </summary>
        public static class ImportantChecklist
        {
            private const string Base = ApiBase + "/work-request/important-checklist";

            /// <summary>
            /// GET: Get important checklist items list
            /// Query params: cid (client id)
            /// Response: List of ImportantChecklistItemModel
            /// </summary>
            public const string List = Base + "/list";

            /// <summary>
            /// POST: Create new important checklist item
            /// Body: ImportantChecklistItemModel
            /// Response: int (new ID)
            /// </summary>
            public const string Create = Base;

            /// <summary>
            /// PUT: Update important checklist item
            /// Body: ImportantChecklistItemModel
            /// Response: int (updated ID)
            /// </summary>
            public const string Update = Base;

            /// <summary>
            /// DELETE: Delete important checklist item by ID
            /// Path params: {id}
            /// Query params: cid (client id)
            /// </summary>
            public static string Delete(int id) => $"{Base}/{id}";

            /// <summary>
            /// PUT: Update display order of important checklist items
            /// Body: ImportantChecklistUpdateOrderRequest
            /// </summary>
            public const string UpdateOrder = Base + "/update-order";
        }

        #endregion

        #region Other Category (Legacy)

        /// <summary>
        /// Other category management endpoints (Legacy - kept for backward compatibility)
        /// </summary>
        public static class OtherCategory
        {
            private const string Base = ApiBase + "/othercategory";

            /// <summary>
            /// GET: Get all other categories
            /// Query params: idClient (optional), categoryType (optional)
            /// </summary>
            public const string List = Base + "/list";

            /// <summary>
            /// POST: Create new other category
            /// </summary>
            public const string Create = Base + "/create";

            /// <summary>
            /// PUT: Update existing other category
            /// </summary>
            public const string Update = Base + "/update";

            /// <summary>
            /// DELETE: Delete other category by ID
            /// Path params: {id}
            /// </summary>
            /// <param name="id">Other category ID</param>
            /// <returns>Delete endpoint URL</returns>
            public static string Delete(int id) => $"{Base}/delete/{id}";
        }

        #endregion

        #region Generic Category Endpoints

        /// <summary>
        /// Generic category endpoint builder for reusable CRUD operations
        /// Supports: othercategory, othercategory2, jobcodegroup, materialtype
        /// </summary>
        public static class GenericCategory
        {
            /// <summary>
            /// GET: List items for generic category endpoint
            /// </summary>
            /// <param name="endpoint">Category endpoint name (e.g., "othercategory", "jobcodegroup")</param>
            /// <returns>List endpoint URL</returns>
            public static string List(string endpoint) => $"{ApiBase}/{endpoint}/list";

            /// <summary>
            /// POST: Create item for generic category endpoint
            /// </summary>
            /// <param name="endpoint">Category endpoint name</param>
            /// <returns>Create endpoint URL</returns>
            public static string Create(string endpoint) => $"{ApiBase}/{endpoint}/create";

            /// <summary>
            /// PUT: Update item for generic category endpoint
            /// </summary>
            /// <param name="endpoint">Category endpoint name</param>
            /// <returns>Update endpoint URL</returns>
            public static string Update(string endpoint) => $"{ApiBase}/{endpoint}/update";

            /// <summary>
            /// DELETE: Delete item for generic category endpoint
            /// </summary>
            /// <param name="endpoint">Category endpoint name</param>
            /// <param name="id">Item ID</param>
            /// <returns>Delete endpoint URL</returns>
            public static string Delete(string endpoint, int id) => $"{ApiBase}/{endpoint}/delete/{id}";
        }

        #endregion

        #region Settings - Person In Charge

        /// <summary>
        /// Settings management for persons in charge
        /// </summary>
        public static class Settings
        {
            private const string Base = ApiBase + "/settings";

            /// <summary>
            /// Person in charge settings endpoints
            /// Base path: /api/v1/work-request/pic
            /// </summary>
            public static class PersonInCharge
            {
                private const string PicBase = ApiBase + "/work-request/pic";

                /// <summary>
                /// GET: Get paginated PIC list
                /// Query params: cid (client id), page, limit, keyword
                /// </summary>
                public const string List = PicBase + "/list";

                /// <summary>
                /// GET: Get PIC details with property assignments
                /// Path params: {employeeId}
                /// Query params: cid (client id)
                /// </summary>
                public static string GetDetails(int employeeId) => $"{PicBase}/{employeeId}";

                /// <summary>
                /// POST: Create PIC property assignment
                /// Body: PicPropertyPayloadDto
                /// </summary>
                public const string Create = PicBase;

                /// <summary>
                /// PUT: Update PIC property assignment
                /// Body: PicPropertyPayloadDto
                /// </summary>
                public const string Update = PicBase;

                /// <summary>
                /// DELETE: Remove PIC
                /// Path params: {employeeId}
                /// Query params: cid (client id)
                /// </summary>
                public static string Delete(int employeeId) => $"{PicBase}/{employeeId}";
            }

            /// <summary>
            /// GET: Get properties for client
            /// Query params: idClient
            /// </summary>
            public const string Properties = Base + "/properties";

            /// <summary>
            /// Priority Level settings endpoints
            /// Uses new work-request API: /api/v1/work-request/priority-levels
            /// </summary>
            public static class PriorityLevel
            {
                private const string PriorityLevelBase = ApiBase + "/work-request/priority-levels";

                /// <summary>
                /// GET: Get paginated priority levels for client
                /// Query params: cid (clientId), keyword, page, limit
                /// </summary>
                public const string List = PriorityLevelBase;

                /// <summary>
                /// GET: Get priority level by ID
                /// Path params: {id}
                /// Query params: cid (clientId)
                /// </summary>
                /// <param name="id">Priority level ID</param>
                /// <returns>Get by ID endpoint URL</returns>
                public static string GetById(int id) => $"{PriorityLevelBase}/{id}";

                /// <summary>
                /// POST: Create priority level
                /// Body: PriorityLevelDetailsDto
                /// </summary>
                public const string Create = PriorityLevelBase;

                /// <summary>
                /// PUT: Update priority level
                /// Body: PriorityLevelDetailsDto
                /// </summary>
                public const string Update = PriorityLevelBase;

                /// <summary>
                /// DELETE: Delete priority level by ID
                /// Path params: {id}
                /// Query params: cid (clientId)
                /// </summary>
                /// <param name="id">Priority level ID</param>
                /// <returns>Delete endpoint URL</returns>
                public static string Delete(int id) => $"{PriorityLevelBase}/{id}";

                /// <summary>
                /// POST: Move priority level up in display order
                /// Path params: {id}
                /// Query params: cid (clientId)
                /// </summary>
                /// <param name="id">Priority level ID</param>
                /// <returns>Move up endpoint URL</returns>
                public static string MoveUp(int id) => $"{PriorityLevelBase}/{id}/move-up";

                /// <summary>
                /// POST: Move priority level down in display order
                /// Path params: {id}
                /// Query params: cid (clientId)
                /// </summary>
                /// <param name="id">Priority level ID</param>
                /// <returns>Move down endpoint URL</returns>
                public static string MoveDown(int id) => $"{PriorityLevelBase}/{id}/move-down";

                /// <summary>
                /// GET: Get dropdown options for priority level forms
                /// Query params: type (one of: priorityLevelInitialFollowUp, priorityLevelQuotationSubmission, etc.)
                /// </summary>
                public const string DropdownOptions = PriorityLevelBase + "/dropdown-options";
            }

            /// <summary>
            /// Cost Approver Group settings endpoints
            /// </summary>
            public static class CostApproverGroup
            {
                private const string CostApproverGroupBase = Base + "/cost-approver-group";

                /// <summary>
                /// GET: Get all cost approver groups for client
                /// Query params: idClient
                /// </summary>
                public const string List = CostApproverGroupBase;

                /// <summary>
                /// GET: Get cost approver group by ID
                /// Path params: {id}
                /// Query params: idClient
                /// </summary>
                /// <param name="id">Cost approver group ID</param>
                /// <returns>Get by ID endpoint URL</returns>
                public static string GetById(int id) => $"{CostApproverGroupBase}/{id}";

                /// <summary>
                /// POST: Create cost approver group
                /// </summary>
                public const string Create = CostApproverGroupBase;

                /// <summary>
                /// PUT: Update cost approver group
                /// </summary>
                public const string Update = CostApproverGroupBase;

                /// <summary>
                /// DELETE: Delete cost approver group by ID
                /// Path params: {id}
                /// </summary>
                /// <param name="id">Cost approver group ID</param>
                /// <returns>Delete endpoint URL</returns>
                public static string Delete(int id) => $"{CostApproverGroupBase}/{id}";
            }
        }

        #endregion

        #region Job Code

        /// <summary>
        /// Job code management endpoints
        /// </summary>
        public static class JobCode
        {
            public const string Base = ApiBase + "/jobcode";

            /// <summary>
            /// GET: Get all job codes
            /// Query params: idClient, isActiveData (optional), keyword (optional), group (optional), page (optional)
            /// </summary>
            public const string List = Base + "/list";

            /// <summary>
            /// GET: Search job codes for Labor/Material modal
            /// Query params: term, idClient
            /// Returns: List of JobCodeSearchResult (IdJobCode, Name, Description, MinimumStock, LatestStock, LaborMaterialMeasurementUnit)
            /// </summary>
            public const string Search = Base + "/search";

            /// <summary>
            /// GET: Get job code by ID
            /// Path params: {id}
            /// </summary>
            /// <param name="id">Job code ID</param>
            /// <returns>Get by ID endpoint URL</returns>
            public static string GetById(int id) => $"{Base}/{id}";

            /// <summary>
            /// POST: Create new job code
            /// </summary>
            public const string Create = Base + "/create";

            /// <summary>
            /// PUT: Update existing job code
            /// </summary>
            public const string Update = Base + "/update";

            /// <summary>
            /// DELETE: Delete job code by ID
            /// Path params: {id}
            /// </summary>
            /// <param name="id">Job code ID</param>
            /// <returns>Delete endpoint URL</returns>
            public static string Delete(int id) => $"{Base}/delete/{id}";

            /// <summary>
            /// GET: Get change history for job code
            /// Query params: id, pageReference, module
            /// </summary>
            public const string ChangeHistory = Base + "/change-history";
        }

        #endregion

        #region Job Code Group (Settings)

        /// <summary>
        /// Job Code Group management endpoints (Settings)
        /// Base path: /api/v1/job-code/group
        /// </summary>
        public static class JobCodeGroup
        {
            private const string Base = ApiBase + "/job-code/group";

            /// <summary>
            /// GET: Get paginated job code groups list
            /// Query params: cid (client id), keyword (search), page (pagination), limit (page size)
            /// Response: Paginated list of TypeFormDetailResponse
            /// </summary>
            public const string List = Base + "/list";

            /// <summary>
            /// GET: Get job code group by ID
            /// Path params: {id}
            /// Query params: cid (client id)
            /// Response: TypePayloadDto structure
            /// </summary>
            public static string GetById(int id) => $"{Base}/{id}";

            /// <summary>
            /// POST: Create new job code group
            /// Body: TypePayloadDto
            /// Response: int (new ID)
            /// </summary>
            public const string Create = Base;

            /// <summary>
            /// PUT: Update job code group
            /// Body: TypePayloadDto (includes IdType for identifying record)
            /// Response: int (updated ID)
            /// </summary>
            public const string Update = Base;

            /// <summary>
            /// DELETE: Delete job code group by ID
            /// Path params: {id}
            /// Query params: cid (client id)
            /// </summary>
            public static string Delete(int id) => $"{Base}/{id}";
        }

        #endregion

        #region Material Type (Settings)

        /// <summary>
        /// Material Type management endpoints (Settings)
        /// Base path: /api/v1/job-code/material-type
        /// </summary>
        public static class MaterialType
        {
            private const string Base = ApiBase + "/job-code/material-type";

            /// <summary>
            /// GET: Get paginated material types list
            /// Query params: cid (client id), keyword (search), page (pagination), limit (page size)
            /// Response: Paginated list of TypeFormDetailResponse
            /// </summary>
            public const string List = Base + "/list";

            /// <summary>
            /// GET: Get material type by ID
            /// Path params: {id}
            /// Query params: cid (client id)
            /// Response: TypePayloadDto structure
            /// </summary>
            public static string GetById(int id) => $"{Base}/{id}";

            /// <summary>
            /// POST: Create new material type
            /// Body: TypePayloadDto
            /// Response: int (new ID)
            /// </summary>
            public const string Create = Base;

            /// <summary>
            /// PUT: Update material type
            /// Body: TypePayloadDto (includes IdType for identifying record)
            /// Response: int (updated ID)
            /// </summary>
            public const string Update = Base;

            /// <summary>
            /// DELETE: Delete material type by ID
            /// Path params: {id}
            /// Query params: cid (client id)
            /// </summary>
            public static string Delete(int id) => $"{Base}/{id}";
        }

        #endregion

        #region Masters

        /// <summary>
        /// Masters API - Unified Types endpoint
        /// </summary>
        public static class Masters
        {
            private const string Base = ApiBase + "/masters";

            /// <summary>
            /// GET: Get types by category
            /// Path params: {category}
            /// Query params: idClient (required), parentTypeId (optional)
            /// </summary>
            /// <param name="category">Category type string</param>
            /// <returns>Get types endpoint URL</returns>
            public static string GetTypes(string category) => $"{Base}/types/{category}";

            /// <summary>
            /// GET: Get enums by category
            /// Path params: {category}
            /// </summary>
            /// <param name="category">Category type string</param>
            /// <returns>Get enums endpoint URL</returns>
            public static string GetEnums(string category) => $"{Base}/enums/{category}";

            /// <summary>
            /// GET: Search company contacts by name prefix
            /// Query params: cid (client id), prefix (search term)
            /// </summary>
            public const string CompanyContacts = Base + "/company-contacts";

            /// <summary>
            /// Category type constants for Masters API
            /// </summary>
            public static class CategoryTypes
            {
                public const string WorkCategory = "workCategory";
                public const string WorkRequestCustomCategory = "workRequestCustomCategory";
                public const string WorkRequestCustomCategory2 = "workRequestCustomCategory2";
                public const string WorkRequestAdditionalInformation = "workRequestAdditionalInformation";
                public const string WorkRequestMethod = "workRequestMethod";
                public const string WorkRequestStatus = "workRequestStatus";
                public const string WorkRequestFeedbackType = "workRequestFeedbackType";
                public const string Currency = "currency";
                public const string MeasurementUnit = "measurementUnit";
                public const string MaterialLabel = "materialLabel";
                public const string WorkRequestDocument = "workRequestDocument";

                // Priority Level reference types
                public const string PriorityLevelInitialFollowUp = "priorityLevelInitialFollowUp";
                public const string PriorityLevelQuotationSubmission = "priorityLevelQuotationSubmission";
                public const string PriorityLevelCostApproval = "priorityLevelCostApproval";
                public const string PriorityLevelWorkCompletion = "priorityLevelWorkCompletion";
                public const string PriorityLevelAfterWorkFollowUp = "priorityLevelAfterWorkFollowUp";
                public const string VisualColor = "visualColor";
            }
        }

        #endregion

        #region Email Distribution

        /// <summary>
        /// Email distribution list management endpoints.
        /// Backend: /api/v1/work-request/email-distributions
        /// </summary>
        public static class EmailDistribution
        {
            private const string Base = ApiBase + "/work-request/email-distributions";

            /// <summary>
            /// GET: Paged list of email distribution types with setup status.
            /// Query params: cid, page, limit, keyword
            /// </summary>
            public const string List = Base;

            /// <summary>
            /// GET: Get email distribution detail by enum ID.
            /// Query params: cid
            /// </summary>
            public static string GetById(int idEnum) => $"{Base}/{idEnum}";

            /// <summary>
            /// PUT: Save (upsert) email distribution configuration.
            /// Handles both setup (new) and edit (existing) based on idEnum + idClient.
            /// </summary>
            public const string Save = Base;
        }

        #endregion

        #region Inventory Management

        /// <summary>
        /// Inventory transaction management endpoints
        /// </summary>
        public static class Inventory
        {
            private const string Base = ApiBase + "/inventory";

            /// <summary>
            /// POST: Get inventory transactions list with filtering
            /// Request body: InventoryFilterModel
            /// </summary>
            public const string List = Base + "/list";

            /// <summary>
            /// GET: Get filter options for inventory transactions
            /// Query params: idClient
            /// </summary>
            public const string GetFilterOptions = Base + "/filter-options";

            /// <summary>
            /// POST: Create new inventory transaction
            /// </summary>
            public const string Create = Base + "/create";

            /// <summary>
            /// PUT: Update inventory transaction
            /// Path params: {id}
            /// </summary>
            /// <param name="id">Transaction ID</param>
            /// <returns>Update endpoint URL</returns>
            public static string Update(int id) => $"{Base}/update/{id}";

            /// <summary>
            /// DELETE: Delete inventory transaction by ID
            /// Path params: {id}
            /// </summary>
            /// <param name="id">Transaction ID</param>
            /// <returns>Delete endpoint URL</returns>
            public static string Delete(int id) => $"{Base}/delete/{id}";

            /// <summary>
            /// GET: Get inventory transaction by ID
            /// Path params: {id}
            /// </summary>
            /// <param name="id">Transaction ID</param>
            /// <returns>Get by ID endpoint URL</returns>
            public static string GetById(int id) => $"{Base}/{id}";

            /// <summary>
            /// GET: Search materials for autocomplete
            /// Query params: idClient, term
            /// </summary>
            public const string SearchMaterials = Base + "/search-materials";
        }

        #endregion

        #region Asset

        /// <summary>
        /// Asset search endpoints
        /// </summary>
        public static class Asset
        {
            private const string Base = ApiBase + "/asset";

            public static string GetAsset(int idProperty) => $"{Base}/{idProperty}";

            public static string GetAssetByGroup(int idProperty) => $"{Base}/asset-group/{idProperty}";
        }


        #endregion
    }
}
