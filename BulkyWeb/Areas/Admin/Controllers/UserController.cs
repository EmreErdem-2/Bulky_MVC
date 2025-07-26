using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _applicationDb;
        public UserController(ApplicationDbContext applicationDb)
        {
            _applicationDb = applicationDb;
        }
        public IActionResult Index()
        {
            return View();
        }

        #region API Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> objUserList = _applicationDb.ApplicationUser.Include(u=>u.Company).ToList();

            var userRoles = _applicationDb.UserRoles.ToList();
            var roles = _applicationDb.Roles.ToList(); 
            foreach (var user in objUserList)
            {
                var roleId = userRoles.FirstOrDefault(u => u.UserId == user.Id).RoleId;
                user.Role = roles.FirstOrDefault(u => u.Id == roleId).Name;
                if(user.Company==null)
                    user.Company = new Company { Name = "" };
            }

            return Json(new { data = objUserList });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody]string? id)
        {
            var objFromDb = _applicationDb.ApplicationUser.FirstOrDefault(u => u.Id == id);
            if(objFromDb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }
            if(objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                // User is currently locked, unlock them
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                // User is currently unlocked, lock them
                objFromDb.LockoutEnd = DateTime.Now.AddYears(1000); // Lock for a long time
            }
            _applicationDb.SaveChanges();
            return Json(new { success = true, message = "Operation successful" });
        }
        #endregion
    }
}
