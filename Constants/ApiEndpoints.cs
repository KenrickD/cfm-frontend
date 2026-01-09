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

        }

        #endregion

        /// <summary>
        /// User info endpoints
        /// </summary>
        #region SessionInfo
        public static class UserInfo
        {
            private const string Base = Api + "/WebUser";

            /// <summary>
            /// GET: Get user details
            /// </summary>
            public const string GetUserDetail = Base + "/GetUserDetail";

            /// <summary>
            /// GET: Get user privileges (uses bearer token for user identification)
            /// </summary>
            public const string GetUserPrivileges = Base + "/GetUserPrivileges";
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
            /// </summary>
            public const string Create = Base + "/create";

            /// <summary>
            /// POST: Create new work request
            /// </summary>
            public const string List = Base + "/GetWorkRequestList";

            /// <summary>
            /// GET: Get all work request statuses
            /// </summary>
            public const string Statuses = Base + "/statuses";

            /// <summary>
            /// GET: Get all filter options for work requests
            /// Query params: idClient
            /// </summary>
            public const string GetFilterOptions = Base + "/GetFilterOptions";
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
            /// GET: Get persons in charge filtered by category and location
            /// Query params: idClient, idWorkCategory, idLocation
            /// </summary>
            public const string PersonsInCharge = Base + "/persons-in-charge";

            /// <summary>
            /// GET: Search employees/requestors by search term
            /// Query params: term, idCompany
            /// </summary>
            public const string SearchRequestors = Base + "/search-requestors";
        }

        #endregion

        #region Service Provider

        /// <summary>
        /// Service provider management endpoints
        /// </summary>
        public static class ServiceProvider
        {
            private const string Base = ApiBase + "/serviceprovider";

            /// <summary>
            /// GET: Get service providers for client and company
            /// Query params: idClient, idCompany
            /// </summary>
            public const string List = Base + "/list";
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
            }
        }

        #endregion

        #region Office Hour & Public Holiday

        /// <summary>
        /// Office Hour management endpoints
        /// </summary>
        public static class OfficeHour
        {
            private const string Base = ApiBase + "/officehour";

            /// <summary>
            /// GET: Get office hours for client
            /// Query params: idClient
            /// </summary>
            public const string List = Base + "/list";
        }

        /// <summary>
        /// Public Holiday management endpoints
        /// </summary>
        public static class PublicHoliday
        {
            private const string Base = ApiBase + "/publicholiday";

            /// <summary>
            /// GET: Get public holidays for client
            /// Query params: idClient, year (optional), isActiveData (optional)
            /// </summary>
            public const string List = Base + "/list";
        }

        #endregion

        #region Work Category

        /// <summary>
        /// Work category management endpoints
        /// </summary>
        public static class WorkCategory
        {
            private const string Base = ApiBase + "/workcategory";

            /// <summary>
            /// GET: Get all work categories
            /// Query params: idClient (optional), categoryType (optional)
            /// </summary>
            public const string List = Base + "/list";

            /// <summary>
            /// POST: Create new work category
            /// </summary>
            public const string Create = Base + "/create";

            /// <summary>
            /// PUT: Update existing work category
            /// </summary>
            public const string Update = Base + "/update";

            /// <summary>
            /// DELETE: Delete work category by ID
            /// Path params: {id}
            /// </summary>
            /// <param name="id">Work category ID</param>
            /// <returns>Delete endpoint URL</returns>
            public static string Delete(int id) => $"{Base}/delete/{id}";
        }

        #endregion

        #region Other Category

        /// <summary>
        /// Other category management endpoints
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
            /// </summary>
            public static class PersonInCharge
            {
                private const string PersonsBase = Base + "/persons-in-charge";

                /// <summary>
                /// GET: Get all persons in charge
                /// </summary>
                public const string List = PersonsBase;

                /// <summary>
                /// GET: Get person in charge by ID
                /// Path params: {id}
                /// </summary>
                /// <param name="id">Person in charge ID</param>
                /// <returns>Get by ID endpoint URL</returns>
                public static string GetById(int id) => $"{PersonsBase}/{id}";

                /// <summary>
                /// POST: Create person in charge
                /// </summary>
                public const string Create = PersonsBase;

                /// <summary>
                /// PUT: Update person in charge
                /// </summary>
                public const string Update = PersonsBase;

                /// <summary>
                /// DELETE: Delete person in charge by ID
                /// Path params: {id}
                /// </summary>
                /// <param name="id">Person in charge ID</param>
                /// <returns>Delete endpoint URL</returns>
                public static string Delete(int id) => $"{PersonsBase}/{id}";
            }

            /// <summary>
            /// GET: Get properties for client
            /// Query params: idClient
            /// </summary>
            public const string Properties = Base + "/properties";

            /// <summary>
            /// Priority Level settings endpoints
            /// </summary>
            public static class PriorityLevel
            {
                private const string PriorityLevelBase = Base + "/priority-level";

                /// <summary>
                /// GET: Get all priority levels for client
                /// Query params: idClient
                /// </summary>
                public const string List = PriorityLevelBase;

                /// <summary>
                /// GET: Get priority level by ID
                /// Path params: {id}
                /// Query params: idClient
                /// </summary>
                /// <param name="id">Priority level ID</param>
                /// <returns>Get by ID endpoint URL</returns>
                public static string GetById(int id) => $"{PriorityLevelBase}/{id}";

                /// <summary>
                /// POST: Create priority level
                /// </summary>
                public const string Create = PriorityLevelBase;

                /// <summary>
                /// PUT: Update priority level
                /// </summary>
                public const string Update = PriorityLevelBase;

                /// <summary>
                /// DELETE: Delete priority level by ID
                /// Path params: {id}
                /// </summary>
                /// <param name="id">Priority level ID</param>
                /// <returns>Delete endpoint URL</returns>
                public static string Delete(int id) => $"{PriorityLevelBase}/{id}";

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
            private const string Base = ApiBase + "/jobcode";

            /// <summary>
            /// GET: Get all job codes
            /// Query params: idClient, isActiveData (optional), keyword (optional), group (optional), page (optional)
            /// </summary>
            public const string List = Base + "/list";

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
            }
        }

        #endregion

        #region Email Distribution

        /// <summary>
        /// Email distribution list management endpoints
        /// </summary>
        public static class EmailDistribution
        {
            private const string Base = ApiBase + "/EmailDistribution";

            /// <summary>
            /// GET: Get all email distribution page references with status
            /// Returns list of distribution types with hasDistributionList flag
            /// </summary>
            public const string GetPageReferences = Base + "/GetPageReferences";

            /// <summary>
            /// GET: Get email distribution by ID
            /// Path params: {id}
            /// </summary>
            /// <param name="id">Email distribution list ID</param>
            /// <returns>Get by ID endpoint URL</returns>
            public static string GetById(int id) => $"{Base}/GetById/{id}";

            /// <summary>
            /// POST: Create new email distribution
            /// </summary>
            public const string Create = Base + "/Create";

            /// <summary>
            /// PUT: Update email distribution
            /// Path params: {id}
            /// </summary>
            /// <param name="id">Email distribution list ID</param>
            /// <returns>Update endpoint URL</returns>
            public static string Update(int id) => $"{Base}/Update/{id}";

            /// <summary>
            /// DELETE: Delete email distribution by ID
            /// Path params: {id}
            /// </summary>
            /// <param name="id">Email distribution list ID</param>
            /// <returns>Delete endpoint URL</returns>
            public static string Delete(int id) => $"{Base}/Delete/{id}";
        }

        #endregion
    }
}
