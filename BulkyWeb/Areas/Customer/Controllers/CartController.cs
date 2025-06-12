using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

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
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u=>u.ApplicationUserId==userId, includeProperties: "Product")
            };
            if(shoppingCartVM.ShoppingCartList.Count() > 0)
            {
                foreach(var cartItem in shoppingCartVM.ShoppingCartList)
                {
                    shoppingCartVM.OrderTotal = shoppingCartVM.OrderTotal + cartItem.Count * GetPriceBasedOnQuantity(cartItem);
                }
            }
            else
            {
                shoppingCartVM.OrderTotal = 0;
            }

            return View(shoppingCartVM);
        }

        public IActionResult Summary()
        {
            return View();
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
