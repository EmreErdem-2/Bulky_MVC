using System.Security.Claims;
using System.Threading.Tasks;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        private readonly IOptions<IyzicoSettings> _iyzicoSettings;

        public OrderController(IUnitOfWork unitOfWork, IOptions<IyzicoSettings> iyzicoSettings)
        {
            _unitOfWork = unitOfWork;
            _iyzicoSettings = iyzicoSettings;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            OrderVM = new OrderVM()
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == id, includeProperties: "Product")
            };

            return View(OrderVM);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
            orderHeaderFromDb.OrderStatus = OrderVM.OrderHeader.OrderStatus;
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }

            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["success"] = "Order Details Updated Successfully";

            return RedirectToAction(nameof(Details), new {id=orderHeaderFromDb.Id});
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["success"] = "Order Status Updated Successfully";
            return RedirectToAction(nameof(Details), new {id=OrderVM.OrderHeader.Id});
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeaderFromDb.OrderStatus = SD.StatusShipped;
            orderHeaderFromDb.ShippingDate = DateTime.Now;
            if(orderHeaderFromDb.PaymentStatus== SD.PaymentStatusDelayedPayment)
            {
                orderHeaderFromDb.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();
            TempData["success"] = "Order Shipped Successfully";
            return RedirectToAction(nameof(Details), new { id = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public async Task<IActionResult> CancelOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                Iyzipay.Options options = IyzicoOptionsSet();
                var cancelRequest = new CreateCancelRequest
                {
                    ConversationId = orderHeader.ConversationId,
                    PaymentId = orderHeader.Id.ToString(),
                    Reason = "Cancelled by Admin",
                    Description = "Order cancelled by admin",
                    Ip = _iyzicoSettings.Value.BaseUrl,
                    Locale = Iyzipay.Model.Locale.TR.ToString()
                };

                Cancel cancel = await Cancel.Create(cancelRequest, options);
                if(cancel.Status == Status.SUCCESS.ToString())
                {
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id,SD.StatusCancelled,SD.StatusRefunded);
                }
                else
                {
                    TempData["error"] = "Error while cancelling the order: " + cancel.ErrorMessage;
                    return RedirectToAction(nameof(Details), new { id = OrderVM.OrderHeader.Id });
                }
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            }

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();
            TempData["success"] = "Order Cancelled Successfully";
            return RedirectToAction(nameof(Details), new { id = OrderVM.OrderHeader.Id });
        }


        private Iyzipay.Options IyzicoOptionsSet()
        {
            Iyzipay.Options options = new Iyzipay.Options();
            options.ApiKey = _iyzicoSettings.Value.ApiKey; //"sandbox-cbD9qj9t72r54rwH0qbF806hf8IcVwi0";
            options.SecretKey = _iyzicoSettings.Value.SecretKey; //"sandbox-AmLjKAT79fRq47avXlcphMt0byhSgvlk";
            options.BaseUrl = _iyzicoSettings.Value.BaseUrl; //"https://sandbox-api.iyzipay.com";

            return options;
        }

        #region API Calls
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeaders;

            if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
            }

                switch (status)
                {
                    case "pending":
                        objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.StatusPending);
                        break;
                    case "inprocess":
                        objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                        break;
                    case "completed":
                        objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                        break;
                    case "approved":
                        objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                        break;
                    default:
                        break;
                }

            return Json(new { data = objOrderHeaders });
        }
        #endregion

    }
}
