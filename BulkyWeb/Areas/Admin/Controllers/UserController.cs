using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly IUnitOfWork _unitOfWork;
        public UserController(ApplicationDbContext applicationDb, IUnitOfWork unitOfWork)
        {
            _applicationDb = applicationDb;
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagement(string? userId)
        {
            if (userId == null || userId == "")
            {
                return NotFound();
            }
            var userFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == userId, includeProperties: "Company");
            if (userFromDb == null)
            {
                return NotFound();
            }
            userFromDb.Role = _applicationDb.UserRoles
                .Where(u => u.UserId == userFromDb.Id)
                .Select(u => _applicationDb.Roles.FirstOrDefault(r => r.Id == u.RoleId).Name)
                .FirstOrDefault() ?? string.Empty; // Default to empty if no role found
            RoleManagementVM roleManagementVM = new RoleManagementVM()
            {
                User = userFromDb,
                RoleList = _applicationDb.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Name
                }).ToList(),
                CompanyList = _applicationDb.Company.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }).ToList()
            };
            return View(roleManagementVM);
        }

        [HttpPost]
        public IActionResult RoleManagement(RoleManagementVM roleManagementVM)
        {
            var userFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == roleManagementVM.User.Id, includeProperties: "Company");
            var currentUserRoleId = _applicationDb.UserRoles.FirstOrDefault(u => u.UserId == userFromDb.Id).RoleId;
            var newRoleId = _applicationDb.Roles.FirstOrDefault(u => u.Name == roleManagementVM.User.Role).Id;

            if (roleManagementVM.User.Role != null && currentUserRoleId != newRoleId)
            {
                // Update UserRole table
                var userRoleFromDb = _applicationDb.UserRoles.FirstOrDefault(u => u.UserId == userFromDb.Id);
                if (userRoleFromDb != null)
                {
                    _applicationDb.UserRoles.Remove(userRoleFromDb);
                    _applicationDb.SaveChanges();

                    var newUserRole = new IdentityUserRole<string>
                    {
                        UserId = userFromDb.Id,
                        RoleId = newRoleId
                    };
                    _applicationDb.UserRoles.Add(newUserRole);
                    _applicationDb.SaveChanges();
                }
                else
                {
                    TempData["error"] = "No User found for Role";
                }
            }
            if (roleManagementVM.User.Company != null)
            {
                userFromDb.CompanyId = roleManagementVM.User.Company.Id;
                _unitOfWork.Save();
            }
            else
            {
                userFromDb.CompanyId = null;
                _unitOfWork.Save();
            }
            
            TempData["success"] = "User role updated!";
            
            return RedirectToAction("Index");
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
