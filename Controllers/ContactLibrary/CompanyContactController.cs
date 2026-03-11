using cfm_frontend.Controllers;
using cfm_frontend.DTOs;
using cfm_frontend.DTOs.CompanyContact;
using cfm_frontend.Extensions;
using cfm_frontend.Models;
using cfm_frontend.Services;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace cfm_frontend.Controllers.ContactLibrary
{
    /// <summary>
    /// Company Contact management controller
    /// Handles CRUD operations for company contacts with phone and email management
    /// </summary>
    public class CompanyContactController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CompanyContactController> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public CompanyContactController(
            IPrivilegeService privilegeService,
            ILogger<BaseController> baseLogger,
            IHttpClientFactory httpClientFactory,
            ILogger<CompanyContactController> logger)
            : base(privilegeService, baseLogger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// GET: /CompanyContact/Index
        /// Display paginated list of company contacts with filtering
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? departments,
            bool showDeleted = false,
            int page = 1)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // TODO: Add privilege check
            // if (!CheckViewAccess("ContactLibrary", "CompanyContact"))
            // {
            //     return RedirectToAction("AccessDenied", "Error");
            // }

            ViewBag.Title = "List of Company Contact";
            ViewBag.pTitle = "Contact Library";

            // Parse selected departments
            List<int>? selectedDepartments = null;
            if (!string.IsNullOrEmpty(departments))
            {
                selectedDepartments = departments.Split(',')
                    .Where(d => int.TryParse(d, out _))
                    .Select(int.Parse)
                    .ToList();
            }

            // TODO: When backend API is ready, fetch data from API
            // For now, return empty/mock data
            var viewModel = new CompanyContactViewModel
            {
                Contacts = new List<CompanyContactListDto>(),
                Paging = new PagingInfo
                {
                    CurrentPage = page,
                    PageSize = 20,
                    TotalCount = 0,
                    TotalPages = 0
                },
                SearchKeyword = search,
                SelectedDepartments = selectedDepartments,
                ShowDeleted = showDeleted,
                FilterOptions = new CompanyContactFilterDto
                {
                    Departments = new List<DepartmentFilterItem>()
                },
                IdClient = userInfo.PreferredClientId
            };

            return View("~/Views/ContactLibrary/CompanyContact/Index.cshtml", viewModel);
        }

        /// <summary>
        /// GET: /CompanyContact/Add
        /// Display add new company contact form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // TODO: Add privilege check
            // if (!CheckAddAccess("ContactLibrary", "CompanyContact"))
            // {
            //     return RedirectToAction("AccessDenied", "Error");
            // }

            ViewBag.Title = "Add New Contact";
            ViewBag.pTitle = "Contact Library";
            ViewBag.pTitleUrl = "/CompanyContact/Index";

            // TODO: Load dropdown data from backend API when ready
            var viewModel = new CompanyContactAddViewModel
            {
                TitlePrefixes = GetTitlePrefixList(),
                Departments = new List<SelectListItem>(),
                PhoneTypes = GetPhoneTypeList(),
                IdClient = userInfo.PreferredClientId
            };

            return View("~/Views/ContactLibrary/CompanyContact/Add.cshtml", viewModel);
        }

        /// <summary>
        /// GET: /CompanyContact/Edit/{id}
        /// Display edit company contact form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int? cid)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // TODO: Add privilege check
            // if (!CheckEditAccess("ContactLibrary", "CompanyContact"))
            // {
            //     return RedirectToAction("AccessDenied", "Error");
            // }

            // Client context validation (multi-tab session safety)
            int clientId = cid ?? userInfo.PreferredClientId;
            if (clientId != userInfo.PreferredClientId)
            {
                TempData["ErrorMessage"] = "Client context mismatch. Please refresh the page.";
                return RedirectToAction("Index");
            }

            ViewBag.Title = "Edit This Contact";
            ViewBag.pTitle = "Contact Library";
            ViewBag.pTitleUrl = "/CompanyContact/Index";

            // TODO: Fetch contact data from backend API
            var viewModel = new CompanyContactEditViewModel
            {
                Contact = new CompanyContactDto
                {
                    IdContact = id,
                    IdClient = clientId,
                    Phones = new List<PhoneDto>(),
                    Emails = new List<EmailDto>()
                },
                TitlePrefixes = GetTitlePrefixList(),
                Departments = new List<SelectListItem>(),
                PhoneTypes = GetPhoneTypeList(),
                IdClient = clientId
            };

            return View("~/Views/ContactLibrary/CompanyContact/Edit.cshtml", viewModel);
        }

        /// <summary>
        /// GET: /CompanyContact/Detail/{id}
        /// Display contact details (read-only)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Detail(int id, int? cid)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // TODO: Add privilege check
            // if (!CheckViewAccess("ContactLibrary", "CompanyContact"))
            // {
            //     return RedirectToAction("AccessDenied", "Error");
            // }

            // Client context validation
            int clientId = cid ?? userInfo.PreferredClientId;
            if (clientId != userInfo.PreferredClientId)
            {
                TempData["ErrorMessage"] = "Client context mismatch. Please refresh the page.";
                return RedirectToAction("Index");
            }

            ViewBag.Title = "Contact Detail";
            ViewBag.pTitle = "Contact Library";
            ViewBag.pTitleUrl = "/CompanyContact/Index";

            // TODO: Fetch contact data from backend API
            var viewModel = new CompanyContactDetailViewModel
            {
                Contact = new CompanyContactDto
                {
                    IdContact = id,
                    IdClient = clientId,
                    Phones = new List<PhoneDto>(),
                    Emails = new List<EmailDto>()
                },
                IdClient = clientId
            };

            return View("~/Views/ContactLibrary/CompanyContact/Detail.cshtml", viewModel);
        }

        #region API Actions for AJAX Calls

        /// <summary>
        /// GET: /CompanyContact/GetList
        /// API endpoint for fetching paginated contact list
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetList(
            int? cid,
            string? keyword,
            string? departments,
            bool showDeleted = false,
            int page = 1,
            int limit = 20)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return Unauthorized();
            }

            int clientId = cid ?? userInfo.PreferredClientId;

            // TODO: Call backend API when ready
            // For now, return mock response
            var response = new ApiResponseDto<object>
            {
                Success = true,
                Message = "Backend API not implemented yet",
                Data = new
                {
                    items = new List<CompanyContactListDto>(),
                    totalCount = 0,
                    page,
                    pageSize = limit
                }
            };

            return Json(response);
        }

        /// <summary>
        /// GET: /CompanyContact/GetById
        /// API endpoint for fetching single contact by ID
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetById(int id, int? cid)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return Unauthorized();
            }

            int clientId = cid ?? userInfo.PreferredClientId;

            // TODO: Call backend API when ready
            var response = new ApiResponseDto<CompanyContactDto>
            {
                Success = true,
                Message = "Backend API not implemented yet",
                Data = new CompanyContactDto { IdContact = id, IdClient = clientId }
            };

            return Json(response);
        }

        /// <summary>
        /// POST: /CompanyContact/Create
        /// API endpoint for creating new contact
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CompanyContactDto contact)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return Unauthorized();
            }

            // TODO: Call backend API when ready
            var response = new ApiResponseDto<int>
            {
                Success = true,
                Message = "Backend API not implemented yet. Contact would be created.",
                Data = 1 // Mock ID
            };

            return Json(response);
        }

        /// <summary>
        /// PUT: /CompanyContact/Update
        /// API endpoint for updating existing contact
        /// </summary>
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromBody] CompanyContactDto contact)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return Unauthorized();
            }

            // TODO: Call backend API when ready
            var response = new ApiResponseDto<int>
            {
                Success = true,
                Message = "Backend API not implemented yet. Contact would be updated.",
                Data = contact.IdContact
            };

            return Json(response);
        }

        /// <summary>
        /// DELETE: /CompanyContact/Delete
        /// API endpoint for deleting contact
        /// </summary>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int? cid)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return Unauthorized();
            }

            // TODO: Call backend API when ready
            var response = new ApiResponseDto<object>
            {
                Success = true,
                Message = "Backend API not implemented yet. Contact would be deleted."
            };

            return Json(response);
        }

        /// <summary>
        /// GET: /CompanyContact/GetFilterOptions
        /// API endpoint for fetching filter options (departments)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFilterOptions(int? cid)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return Unauthorized();
            }

            int clientId = cid ?? userInfo.PreferredClientId;

            // TODO: Call backend API when ready
            var response = new ApiResponseDto<CompanyContactFilterDto>
            {
                Success = true,
                Message = "Backend API not implemented yet",
                Data = new CompanyContactFilterDto
                {
                    Departments = new List<DepartmentFilterItem>()
                }
            };

            return Json(response);
        }

        /// <summary>
        /// GET: /CompanyContact/GetDepartments
        /// API endpoint for fetching departments dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDepartments(int? cid)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return Unauthorized();
            }

            // TODO: Load from backend API
            var departments = new List<SelectListItem>();

            return Json(new { success = true, data = departments });
        }

        /// <summary>
        /// GET: /CompanyContact/GetPhoneTypes
        /// API endpoint for fetching phone types dropdown
        /// </summary>
        [HttpGet]
        public IActionResult GetPhoneTypes()
        {
            var phoneTypes = GetPhoneTypeList();
            return Json(new { success = true, data = phoneTypes });
        }

        /// <summary>
        /// GET: /CompanyContact/GetTitlePrefixes
        /// API endpoint for fetching title prefixes
        /// </summary>
        [HttpGet]
        public IActionResult GetTitlePrefixes()
        {
            var titles = GetTitlePrefixList();
            return Json(new { success = true, data = titles });
        }

        #endregion

        #region Phone Management Actions

        /// <summary>
        /// POST: /CompanyContact/AddPhone
        /// API endpoint for adding phone to contact
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPhone([FromBody] PhoneDto phone)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return Unauthorized();
            }

            // TODO: Call backend API when ready
            var response = new ApiResponseDto<int>
            {
                Success = true,
                Message = "Backend API not implemented yet. Phone would be added.",
                Data = 1 // Mock phone ID
            };

            return Json(response);
        }

        /// <summary>
        /// PUT: /CompanyContact/UpdatePhone
        /// API endpoint for updating phone
        /// </summary>
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePhone([FromBody] PhoneDto phone)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return Unauthorized();
            }

            // TODO: Call backend API when ready
            var response = new ApiResponseDto<int>
            {
                Success = true,
                Message = "Backend API not implemented yet. Phone would be updated.",
                Data = phone.IdPhone
            };

            return Json(response);
        }

        /// <summary>
        /// DELETE: /CompanyContact/DeletePhone
        /// API endpoint for deleting phone
        /// </summary>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhone(int idContact, int idPhone)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return Unauthorized();
            }

            // TODO: Call backend API when ready
            var response = new ApiResponseDto<object>
            {
                Success = true,
                Message = "Backend API not implemented yet. Phone would be deleted."
            };

            return Json(response);
        }

        #endregion

        #region Email Management Actions

        /// <summary>
        /// POST: /CompanyContact/AddEmail
        /// API endpoint for adding email to contact
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmail([FromBody] EmailDto email)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return Unauthorized();
            }

            // TODO: Call backend API when ready
            var response = new ApiResponseDto<int>
            {
                Success = true,
                Message = "Backend API not implemented yet. Email would be added.",
                Data = 1 // Mock email ID
            };

            return Json(response);
        }

        /// <summary>
        /// PUT: /CompanyContact/UpdateEmail
        /// API endpoint for updating email
        /// </summary>
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmail([FromBody] EmailDto email)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return Unauthorized();
            }

            // TODO: Call backend API when ready
            var response = new ApiResponseDto<int>
            {
                Success = true,
                Message = "Backend API not implemented yet. Email would be updated.",
                Data = email.IdEmail
            };

            return Json(response);
        }

        /// <summary>
        /// DELETE: /CompanyContact/DeleteEmail
        /// API endpoint for deleting email
        /// </summary>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmail(int idContact, int idEmail)
        {
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return Unauthorized();
            }

            // TODO: Call backend API when ready
            var response = new ApiResponseDto<object>
            {
                Success = true,
                Message = "Backend API not implemented yet. Email would be deleted."
            };

            return Json(response);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get title prefix list (Mr, Ms, Mrs, Dr, etc.)
        /// </summary>
        private List<SelectListItem> GetTitlePrefixList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Mr", Text = "Mr" },
                new SelectListItem { Value = "Ms", Text = "Ms" },
                new SelectListItem { Value = "Mrs", Text = "Mrs" },
                new SelectListItem { Value = "Dr", Text = "Dr" },
                new SelectListItem { Value = "Prof", Text = "Prof" }
            };
        }

        /// <summary>
        /// Get phone type list (Mobile, Office, Home, etc.)
        /// </summary>
        private List<SelectListItem> GetPhoneTypeList()
        {
            // TODO: Load from backend API (enum: PhoneType)
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Mobile" },
                new SelectListItem { Value = "2", Text = "Office" },
                new SelectListItem { Value = "3", Text = "Home" },
                new SelectListItem { Value = "4", Text = "Fax" }
            };
        }

        #endregion
    }
}
