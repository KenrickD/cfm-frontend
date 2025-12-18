using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mvc.Controllers
{
    public class HelpdeskController : Controller
    {
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
        public IActionResult Index()
        {
            //API call to backend for the data

            //attach response payload to viewmodel
            var viewmodel = new WorkRequestViewModel();

            //populate the paging

            //return the view
            return View(viewmodel);
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