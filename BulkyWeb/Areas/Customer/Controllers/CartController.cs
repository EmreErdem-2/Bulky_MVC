using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
        public IActionResult SummaryPOST() // Gets ShoppingCartVM from the Bind Property above
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


            return RedirectToAction(nameof(OrderConfirmation), new {id=ShoppingCartVM.OrderHeader.Id} );
        }

        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
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
    }
}
