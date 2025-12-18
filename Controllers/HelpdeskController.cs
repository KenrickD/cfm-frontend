using cfm_frontend.Controllers;
using cfm_frontend.Models;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
namespace Mvc.Controllers
{
    public class HelpdeskController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        public HelpdeskController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<LoginController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }
        // GET: Helpdesk
        public IActionResult CourseCourseAdd()
        {
            return View();
        }
        public IActionResult CourseCourseView()
        {
            return View();
        }
        public IActionResult CourseDashboard()
        {
            return View();
        }
        public IActionResult CoursePricing()
        {
            return View();
        }
        public IActionResult CourseSettingNotifications()
        {
            return View();
        }
        public IActionResult CourseSettingPayment()
        {
            return View();
        }
        public IActionResult CourseSettingPricing()
        {
            return View();
        }
        public IActionResult CourseSite()
        {
            return View();
        }
        public IActionResult CourseStudentAdd()
        {
            return View();
        }
        public IActionResult CourseStudentApply()
        {
            return View();
        }
        public IActionResult CourseStudentList()
        {
            return View();
        }
        public IActionResult CourseTeacherAdd()
        {
            return View();
        }
        public IActionResult CourseTeacherApply()
        {
            return View();
        }
        public IActionResult CourseTeacherList()
        {
            return View();
        }
        public IActionResult HelpdeskCreateTicket()
        {
            return View();
        }
        public IActionResult HelpdeskCustomer()
        {
            return View();
        }
        public IActionResult WorkRequestDashboard()
        {
            return View();
        }
        public IActionResult HelpdeskTicketDetails()
        {
            return View();
        }
        public IActionResult HelpdeskTicket()
        {
            return View();
        }
        //Work Request Management List page
        public async Task<IActionResult> Index(int page = 1,int pagesize = 20)
        {
            var viewModel = new WorkRequestViewModel
            {
                CurrentPage = page,
                PageSize = pagesize,
                WorkRequest = new List<WorkRequestResponseModel>(),
                PropertyGroups = new List<PropertyGroupModel>(),
                Status = new List<WRStatusModel>()
            };

            //var baseUrl = _configuration["BackendBaseUrl"];
            //var client = _httpClientFactory.CreateClient();

            //// 1. Fetch Dropdown Data (PropertyGroups & Status) - assuming separate endpoints or previously cached
            //// await LoadFilters(client, baseUrl, viewModel); 

            //// 2. Fetch Paged Data
            //// We pass ?page=x&pageSize=y to the backend
            //var response = await client.GetAsync($"{baseUrl}/api/helpdesk/workrequests?page={page}&pageSize={pagesize}");

            //if (response.IsSuccessStatusCode)
            //{
            //    var jsonString = await response.Content.ReadAsStringAsync();

            //    // Assuming Backend returns a structure like: { "data": [...], "totalCount": 100 }
            //    // We deserialized into a temporary helper class or dynamic object
            //    var apiResult = JsonConvert.DeserializeObject<PagedResult<WorkRequestResponseModel>>(jsonString);

            //    if (apiResult != null)
            //    {
            //        viewModel.WorkRequest = apiResult.Data;
            //        viewModel.TotalItems = apiResult.TotalCount; // Essential for calculating 'TotalPages'
            //    }
            //}

            return View(viewModel);
        }

        public class PagedResult<T>
        {
            public List<T> Data { get; set; }
            public int TotalCount { get; set; }
        }
        public IActionResult InvoiceCreate()
        {
            return View();
        }
        public IActionResult InvoiceDashboard()
        {
            return View();
        }
        public IActionResult InvoiceEdit()
        {
            return View();
        }
        public IActionResult InvoiceList()
        {
            return View();
        }
        public IActionResult InvoiceView()
        {
            return View();
        }
        public IActionResult MembershipDashboard()
        {
            return View();
        }
        public IActionResult MembershipList()
        {
            return View();
        }
        public IActionResult MembershipPricing()
        {
            return View();
        }
        public IActionResult MembershipSetting()
        {
            return View();
        }
    }
}