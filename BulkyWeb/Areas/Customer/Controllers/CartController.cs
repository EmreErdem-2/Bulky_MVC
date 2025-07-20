using System.Security.Claims;
using Azure;
using Azure.Core;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOptions<IyzicoSettings> _iyzicoSettings;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public Guid conversationId { get; set; }

        public CartController(IUnitOfWork unitOfWork, IOptions<IyzicoSettings> iyzicoSettings)
        {
            _unitOfWork = unitOfWork;
            _iyzicoSettings = iyzicoSettings;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM shoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u=>u.ApplicationUserId==userId, includeProperties: "Product"),
                OrderHeader = new OrderHeader()
                {
                    ApplicationUserId = userId
                }
            };
            if(shoppingCartVM.ShoppingCartList.Count() > 0)
            {
                foreach(var cartItem in shoppingCartVM.ShoppingCartList)
                {
                    cartItem.Price = GetPriceBasedOnQuantity(cartItem);
                    shoppingCartVM.OrderHeader.OrderTotal = shoppingCartVM.OrderHeader.OrderTotal + cartItem.Count * cartItem.Price;
                }
            }
            else
            {
                shoppingCartVM.OrderHeader.OrderTotal = 0;
            }

            return View(shoppingCartVM);
        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM shoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new OrderHeader()
                {
                    ApplicationUserId = userId,
                    ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId)
                }
            };
            shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.ApplicationUser.Name;
            shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.ApplicationUser.City;
            shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.ApplicationUser.State;
            shoppingCartVM.OrderHeader.PostalCode = shoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            if (shoppingCartVM.ShoppingCartList.Count() > 0)
            {
                foreach (var cartItem in shoppingCartVM.ShoppingCartList)
                {
                    cartItem.Price = GetPriceBasedOnQuantity(cartItem);
                    shoppingCartVM.OrderHeader.OrderTotal = shoppingCartVM.OrderHeader.OrderTotal + cartItem.Count * cartItem.Price;
                }
            }
            else
            {
                shoppingCartVM.OrderHeader.OrderTotal = 0;
            }
            return View(shoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPOST() // Gets ShoppingCartVM from the Bind Property above
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");

            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;

            ShoppingCartVM.OrderHeader.ApplicationUserId = userId;
            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            if (ShoppingCartVM.ShoppingCartList.Count() > 0)
            {
                foreach (var cartItem in ShoppingCartVM.ShoppingCartList)
                {
                    cartItem.Price = GetPriceBasedOnQuantity(cartItem);
                    ShoppingCartVM.OrderHeader.OrderTotal = ShoppingCartVM.OrderHeader.OrderTotal + cartItem.Count * cartItem.Price;
                }
            }
            else
            {
                ShoppingCartVM.OrderHeader.OrderTotal = 0;
            }

            if(applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // Regular user. We need to capture payment
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                // Company user. Delayed payment
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }
            
            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();
            foreach(var cartItem in ShoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new OrderDetail()
                {
                    ProductId = cartItem.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = cartItem.Price,
                    Count = cartItem.Count
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // Regular user. We need to capture payment
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }

            conversationId = Guid.NewGuid();

            IyzicoCheckoutVM checkoutVM = new IyzicoCheckoutVM {
                Options = IyzicoOptionsSet(), 
                CheckoutFormRequest = IyzicoPaymentSet(ShoppingCartVM)
            };
            CheckoutFormInitialize checkoutFormResponse = 
                await CheckoutFormInitialize.Create(checkoutVM.CheckoutFormRequest, checkoutVM.Options);

            ShoppingCartVM.OrderHeader.ConversationId = checkoutVM.CheckoutFormRequest.ConversationId;
            ShoppingCartVM.OrderHeader.Token = checkoutFormResponse.Token;
            ShoppingCartVM.OrderHeader.TokenExpireTime = checkoutFormResponse.TokenExpireTime;
            _unitOfWork.OrderHeader.Update(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            return View("IyzicoPayment", checkoutFormResponse);
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult OrderConfirmation([FromRoute]string id, [FromForm]string token)
        {
            Iyzipay.Options options = IyzicoOptionsSet();
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id.ToString() == id);
            var storedToken = orderHeader.Token;

            RetrieveCheckoutFormRequest request = new RetrieveCheckoutFormRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = orderHeader.ConversationId,
                Token = storedToken
            };

            CheckoutForm checkoutForm = CheckoutForm.Retrieve(request, options).Result;

            if (checkoutForm.PaymentStatus == "SUCCESS" && token == storedToken)
            {
                orderHeader.PaymentId = checkoutForm.PaymentId;
                PaymentSuccessOrderHeaderStatusChange(orderHeader);
                RemoveShoppingCartAfterPayment(orderHeader.ApplicationUserId);

                TempData["success"] = "Payment successfull";
                HttpContext.Session.Clear();

                return View("OrderConfirmation", id);
            }
            else
            {
                TempData["error"] = "Payment failed. Please try again.";
                id = id + " ~ " + checkoutForm.ErrorMessage;

                return View("OrderPaymentFail", id);
            }
        }
        public IActionResult Plus(int? id)
        {
            ShoppingCart cartItem = _unitOfWork.ShoppingCart.Get(u=>u.Id == id);
            cartItem.Count++;
            _unitOfWork.ShoppingCart.Update(cartItem);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }
        public IActionResult Minus(int? id)
        {
            ShoppingCart cartItem = _unitOfWork.ShoppingCart.Get(u => u.Id == id);
            if(cartItem.Count == 1) {
                // If the count is 1, we should remove the item from the cart instead of decrementing
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cartItem.ApplicationUserId).Count() - 1);
                _unitOfWork.ShoppingCart.Remove(cartItem);
                _unitOfWork.Save();

                return RedirectToAction(nameof(Index));
            }
            else
            {
                cartItem.Count--;
                _unitOfWork.ShoppingCart.Update(cartItem);
                _unitOfWork.Save();

                return RedirectToAction(nameof(Index));
            }

        }
        public IActionResult Delete(int? id)
        {
            ShoppingCart? obj = _unitOfWork.ShoppingCart.Get(u => u.Id == id);
            if (obj == null)
            {
                return NotFound();
            }
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == obj.ApplicationUserId).Count());
            _unitOfWork.ShoppingCart.Remove(obj);
            _unitOfWork.Save();
            TempData["success"] = "Cart item deleted successfully";

            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Product == null)
                return 0;

            switch (shoppingCart.Count)
            {
                case int i when (i < 50):
                    return shoppingCart.Product.Price;
                case int i when (i > 50 && i < 100):
                    return shoppingCart.Product.Price50;
                case int i when (i >= 100):
                    return shoppingCart.Product.Price100;
            }

            return shoppingCart.Product.Price;
        }

        private Iyzipay.Options IyzicoOptionsSet()
        {
            Iyzipay.Options options = new Iyzipay.Options();
            options.ApiKey = _iyzicoSettings.Value.ApiKey; //"sandbox-cbD9qj9t72r54rwH0qbF806hf8IcVwi0";
            options.SecretKey = _iyzicoSettings.Value.SecretKey; //"sandbox-AmLjKAT79fRq47avXlcphMt0byhSgvlk";
            options.BaseUrl = _iyzicoSettings.Value.BaseUrl; //"https://sandbox-api.iyzipay.com";

            return options;
        }
        private CreateCheckoutFormInitializeRequest IyzicoPaymentSet(ShoppingCartVM shoppingCartVM)
        {
            CreateCheckoutFormInitializeRequest request = new CreateCheckoutFormInitializeRequest();
            request.Locale = Locale.TR.ToString();
            request.ConversationId = Guid.NewGuid().ToString();
            request.Price = shoppingCartVM.OrderHeader.OrderTotal.ToString();
            request.PaidPrice = shoppingCartVM.OrderHeader.OrderTotal.ToString();
            request.Currency = Currency.TRY.ToString();
            request.PaymentGroup = PaymentGroup.PRODUCT.ToString();
            request.CallbackUrl = Request.Scheme+"://"+Request.Host.Value+"/Customer/Cart/OrderConfirmation/"+shoppingCartVM.OrderHeader.Id+"/";

            request.Buyer = new Buyer();
            request.Buyer.Id = shoppingCartVM.OrderHeader.ApplicationUserId;
            request.Buyer.Name = shoppingCartVM.OrderHeader.Name.Split(' ')[0];
            request.Buyer.Surname = shoppingCartVM.OrderHeader.Name; //Surname şimdilik yok
            request.Buyer.Email = shoppingCartVM.OrderHeader.ApplicationUser.Email;
            request.Buyer.IdentityNumber = "1231231"; //identity number yok
            request.Buyer.RegistrationAddress = shoppingCartVM.OrderHeader.StreetAddress;
            request.Buyer.City = shoppingCartVM.OrderHeader.City;
            request.Buyer.Country = shoppingCartVM.OrderHeader.State;

            request.BillingAddress = new Address
            {
                ContactName = shoppingCartVM.OrderHeader.Name,
                City = shoppingCartVM.OrderHeader.City,
                Country = shoppingCartVM.OrderHeader.State,
                Description = shoppingCartVM.OrderHeader.StreetAddress
            };
            request.ShippingAddress = new Address
            {
                ContactName = shoppingCartVM.OrderHeader.Name,
                City = shoppingCartVM.OrderHeader.City,
                Country = shoppingCartVM.OrderHeader.State,
                Description = shoppingCartVM.OrderHeader.StreetAddress
            };

            request.BasketId = shoppingCartVM.OrderHeader.Id.ToString();
            request.BasketItems = new List<BasketItem>();
            foreach (var cartItem in shoppingCartVM.ShoppingCartList)
            {
                request.BasketItems.Add(new BasketItem
                {
                    Id = cartItem.ProductId.ToString(),
                    Name = cartItem.Product.Title,
                    Category1 = "Book",
                    ItemType = BasketItemType.PHYSICAL.ToString(),
                    Price = cartItem.Price * cartItem.Count + ".00", // Sonuna .00 eklemek şart. Miktar belirtilmediği için toplamla uyuşması açısından basketitem'ın sayısıyla çarpılır
                });
            };

            return request;
        }

        private void RemoveShoppingCartAfterPayment(string userId)
        {
            var shoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId);
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCartList);
            _unitOfWork.Save();
        }
        private void PaymentSuccessOrderHeaderStatusChange(OrderHeader orderHeader)
        {
                orderHeader.PaymentStatus = SD.PaymentStatusApproved;
                orderHeader.OrderStatus = SD.StatusApproved;
                orderHeader.PaymentDate = DateTime.Now;
                _unitOfWork.OrderHeader.Update(orderHeader);
                _unitOfWork.Save();
        }
        //TO DO: Arrange Payment and Order status information better via repository update methods
        //TO DO: Add a method to handle token expiration date check and populate status and errormessage in checkform
    }
}
