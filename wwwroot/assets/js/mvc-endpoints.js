/**
 * MVC Endpoints - Centralized repository for all MVC controller endpoint URLs
 *
 * This file provides a single source of truth for all MVC controller action URLs
 * used throughout the frontend JavaScript modules. It mirrors the structure of
 * the C# Constants/ApiEndpoints.cs file used in the backend.
 *
 * Organization: Hierarchical by controller and feature area
 * Usage: MvcEndpoints.Helpdesk.Location.GetByClient
 *
 * Benefits:
 * - Single source of truth for endpoint URLs
 * - Easy to maintain and update
 * - Reduces hardcoded URLs across JavaScript files
 * - IntelliSense support in modern IDEs
 * - Type-safe refactoring
 */

(function(window) {
    'use strict';

    const MvcEndpoints = {
        /**
         * Account Controller
         * Authentication, session, and user account management
         */
        Account: {
            RefreshPrivileges: '/Account/RefreshPrivileges'
        },

        /**
         * Helpdesk Controller
         * Work request management and related functionality
         */
        Helpdesk: {
            /**
             * Work Request CRUD Operations
             * Create and Update work requests
             */
            WorkRequestCrud: {
                Create: '/Helpdesk/WorkRequestAdd',
                Update: '/Helpdesk/WorkRequestUpdate'
            },

            /**
             * Location Cascade
             * Property hierarchy navigation (Location -> Floor -> Room)
             */
            Location: {
                GetByClient: '/Helpdesk/GetLocationsByClient',
                GetFloorsByLocation: '/Helpdesk/GetFloorsByLocation',
                GetRoomsByFloor: '/Helpdesk/GetRoomsByFloor'
            },

            /**
             * Work Request Dropdowns and Reference Data
             * Static and dynamic dropdown data for work request forms
             */
            WorkRequest: {
                GetWorkCategoriesByTypes: '/Helpdesk/GetWorkCategoriesByTypes',
                GetOtherCategoriesByTypes: '/Helpdesk/GetOtherCategoriesByTypes',
                GetImportantChecklistByTypes: '/Helpdesk/GetImportantChecklistByTypes',
                GetWorkRequestMethodsByEnums: '/Helpdesk/GetWorkRequestMethodsByEnums',
                GetWorkRequestStatusesByEnums: '/Helpdesk/GetWorkRequestStatusesByEnums',
                GetFeedbackTypesByEnums: '/Helpdesk/GetFeedbackTypesByEnums',
                GetServiceProvidersByClient: '/Helpdesk/GetServiceProvidersByClient',
                GetPriorityLevels: '/Helpdesk/GetPriorityLevels',
                GetPriorityLevelById: '/Helpdesk/GetPriorityLevelById',
                GetPersonsInChargeByFilters: '/Helpdesk/GetPersonsInChargeByFilters'
            },

            /**
             * Search & Autocomplete Endpoints
             * Dynamic search functionality for various entities
             */
            Search: {
                Requestors: '/Helpdesk/SearchRequestors',
                CompanyContacts: '/Helpdesk/SearchCompanyContacts',
                WorkersByCompany: '/Helpdesk/SearchWorkersByCompany',
                WorkersByServiceProvider: '/Helpdesk/SearchWorkersByServiceProvider',
                JobCode: '/Helpdesk/SearchJobCode',
                Asset: '/Helpdesk/SearchAsset',
                AssetGroup: '/Helpdesk/SearchAssetGroup',
                Employees: '/Helpdesk/SearchEmployees'
            },

            /**
             * Extended Data
             * Supporting data for labor/material and other extended functionality
             */
            Extended: {
                GetCurrencies: '/Helpdesk/GetCurrencies',
                GetMeasurementUnits: '/Helpdesk/GetMeasurementUnits',
                GetLaborMaterialLabels: '/Helpdesk/GetLaborMaterialLabels',
                GetDocumentLabels: '/Helpdesk/GetDocumentLabels',
                GetOfficeHours: '/Helpdesk/GetOfficeHours',
                GetPublicHolidays: '/Helpdesk/GetPublicHolidays'
            },

            /**
             * Priority Level Management (non-settings)
             * Note: Get endpoint is under WorkRequest.GetPriorityLevels
             */
            DeletePriorityLevel: '/Helpdesk/DeletePriorityLevel',

            /**
             * Generic Enums Endpoint
             * For loading dropdown options by category
             */
            GetEnumsByCategory: '/Helpdesk/GetEnumsByCategory',

            /**
             * Settings Namespace
             * Configuration and master data management
             */
            Settings: {
                /**
                 * Work Category CRUD
                 */
                WorkCategory: {
                    List: '/Helpdesk/GetWorkCategories',
                    Create: '/Helpdesk/CreateWorkCategory',
                    Update: '/Helpdesk/UpdateWorkCategory',
                    Delete: '/Helpdesk/DeleteWorkCategory'
                },

                /**
                 * Other Category CRUD (V2 - with pagination)
                 */
                OtherCategory: {
                    List: '/Helpdesk/GetOtherCategoriesPaged',
                    Create: '/Helpdesk/CreateOtherCategoryV2',
                    Update: '/Helpdesk/UpdateOtherCategoryV2',
                    Delete: '/Helpdesk/DeleteOtherCategoryV2'
                },

                /**
                 * Other Category 2 CRUD (V2 - with pagination)
                 */
                OtherCategory2: {
                    List: '/Helpdesk/GetOtherCategories2Paged',
                    Create: '/Helpdesk/CreateOtherCategory2V2',
                    Update: '/Helpdesk/UpdateOtherCategory2V2',
                    Delete: '/Helpdesk/DeleteOtherCategory2V2'
                },

                /**
                 * Important Checklist CRUD (with pagination)
                 */
                ImportantChecklist: {
                    List: '/Helpdesk/GetImportantChecklistsPaged',
                    Create: '/Helpdesk/CreateImportantChecklist',
                    Update: '/Helpdesk/UpdateImportantChecklist',
                    Delete: '/Helpdesk/DeleteImportantChecklist',
                    UpdateOrder: '/Helpdesk/UpdateImportantChecklistOrder'
                },

                /**
                 * Document Label (Related Document) CRUD (with pagination)
                 */
                DocumentLabel: {
                    List: '/Helpdesk/GetDocumentLabelsPaged',
                    Create: '/Helpdesk/CreateDocumentLabelV2',
                    Update: '/Helpdesk/UpdateDocumentLabelV2',
                    Delete: '/Helpdesk/DeleteDocumentLabelV2'
                },

                /**
                 * Job Code Group CRUD (V2 - with pagination)
                 */
                JobCodeGroup: {
                    List: '/Helpdesk/GetJobCodeGroupsPaged',
                    Create: '/Helpdesk/CreateJobCodeGroupV2',
                    Update: '/Helpdesk/UpdateJobCodeGroupV2',
                    Delete: '/Helpdesk/DeleteJobCodeGroupV2'
                },

                /**
                 * Material Type CRUD (V2 - with pagination)
                 */
                MaterialType: {
                    List: '/Helpdesk/GetMaterialTypesPaged',
                    Create: '/Helpdesk/CreateMaterialTypeV2',
                    Update: '/Helpdesk/UpdateMaterialTypeV2',
                    Delete: '/Helpdesk/DeleteMaterialTypeV2'
                },

                /**
                 * Person in Charge CRUD
                 */
                PersonInCharge: {
                    List: '/Helpdesk/GetPersonsInChargePaged',
                    GetDetails: '/Helpdesk/GetPicDetails',
                    Create: '/Helpdesk/CreatePersonInCharge',
                    Update: '/Helpdesk/UpdatePersonInCharge',
                    Delete: '/Helpdesk/DeletePersonInCharge'
                },

                /**
                 * Supporting Data for Settings
                 */
                GetProperties: '/Helpdesk/GetProperties',

                /**
                 * Priority Level CRUD (Settings context)
                 */
                PriorityLevel: {
                    List: '/Helpdesk/PriorityLevel',
                    GetById: '/Helpdesk/GetPriorityLevelById',
                    Create: '/Helpdesk/CreatePriorityLevel',
                    Update: '/Helpdesk/UpdatePriorityLevel',
                    Delete: '/Helpdesk/DeletePriorityLevel',
                    MoveUp: '/Helpdesk/MovePriorityLevelUp',
                    MoveDown: '/Helpdesk/MovePriorityLevelDown',
                    DropdownOptions: '/Helpdesk/GetPriorityLevelDropdownOptions'
                },

                /**
                 * Cost Approver Group CRUD
                 */
                CostApproverGroup: {
                    List: '/Helpdesk/GetCostApproverGroups',
                    GetById: '/Helpdesk/GetCostApproverGroupById',
                    Create: '/Helpdesk/CreateCostApproverGroup',
                    Update: '/Helpdesk/UpdateCostApproverGroup',
                    Delete: '/Helpdesk/DeleteCostApproverGroup'
                },

                /**
                 * Email Distribution List Management
                 */
                EmailDistribution: {
                    List: '/Helpdesk/GetEmailDistributionList',
                    GetById: '/Helpdesk/GetEmailDistributionById',
                    Setup: '/Helpdesk/EmailDistributionListSetup',
                    Edit: '/Helpdesk/EmailDistributionListEdit',
                    Save: '/Helpdesk/SaveEmailDistribution'
                }
            }
        }
    };

    /**
     * Helper Functions
     * Utility functions for building dynamic URLs
     */
    MvcEndpoints.Helpers = {
        /**
         * Build URL with query parameters
         * @param {string} baseUrl - Base endpoint URL
         * @param {object} params - Query parameters object
         * @returns {string} URL with query string
         *
         * @example
         * buildUrl('/Helpdesk/GetLocations', { idClient: 123, userId: 456 })
         * // Returns: '/Helpdesk/GetLocations?idClient=123&userId=456'
         */
        buildUrl: function(baseUrl, params) {
            if (!params || Object.keys(params).length === 0) {
                return baseUrl;
            }

            const queryString = Object.keys(params)
                .filter(key => params[key] !== null && params[key] !== undefined)
                .map(key => `${encodeURIComponent(key)}=${encodeURIComponent(params[key])}`)
                .join('&');

            return queryString ? `${baseUrl}?${queryString}` : baseUrl;
        },

        /**
         * Build URL with path parameter
         * @param {string} baseUrl - Base endpoint URL
         * @param {string|number} id - ID to append to URL
         * @returns {string} URL with ID parameter
         *
         * @example
         * buildUrlWithId('/Helpdesk/Settings/GetPersonInChargeById', 123)
         * // Returns: '/Helpdesk/Settings/GetPersonInChargeById?id=123'
         */
        buildUrlWithId: function(baseUrl, id) {
            return `${baseUrl}?id=${id}`;
        },

        /**
         * Build URL with multiple IDs as parameters
         * @param {string} baseUrl - Base endpoint URL
         * @param {object} idParams - Object with ID parameters
         * @returns {string} URL with ID parameters
         *
         * @example
         * buildUrlWithIds('/Helpdesk/GetRoomsByFloor', { locationId: 1, floorId: 2 })
         * // Returns: '/Helpdesk/GetRoomsByFloor?locationId=1&floorId=2'
         */
        buildUrlWithIds: function(baseUrl, idParams) {
            return this.buildUrl(baseUrl, idParams);
        }
    };

    /**
     * Make globally available
     * Attach to window object for access across all scripts
     */
    window.MvcEndpoints = Object.freeze(MvcEndpoints);

    /**
     * Log initialization in development
     * Helps verify that the endpoints are loaded correctly
     */
    if (console && console.log) {
        console.log('%c[MvcEndpoints] %cInitialized - Available at window.MvcEndpoints',
            'color: #0066cc; font-weight: bold',
            'color: #666');
    }

})(window);
