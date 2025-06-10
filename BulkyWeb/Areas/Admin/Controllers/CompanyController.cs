using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public CompanyController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Company> objCompanyList = _unitOfWork.Company.GetAll().ToList();

            return View(objCompanyList);
        }
        public IActionResult Upsert(int? id)
        {
            Company obj = new Company();

            if (id == null || id == 0)
            {
                //create (insert)
                return View(obj);
            }
            else
            {
                //update
                obj = _unitOfWork.Company.Get(u => u.Id == id);

                return View(obj);
            }
        }
        [HttpPost]
        public IActionResult Upsert(Company obj)
        {
            if (ModelState.IsValid)
            {
                string successNotification = "";
                if (obj.Id == 0)
                {
                    _unitOfWork.Company.Add(obj);
                    successNotification = "Company created successfully";
                }
                else
                {
                    _unitOfWork.Company.Update(obj);
                    successNotification = "Company updated successfully";
                }

                _unitOfWork.Save();
                TempData["success"] = successNotification;
                return RedirectToAction("Index");
            }
            else
            {
                return View(obj);
            }
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePOST(int? id)
        {
            Company? obj = _unitOfWork.Company.Get(u => u.Id == id);
            if (obj == null)
            {
                return NotFound();
            }
            _unitOfWork.Company.Remove(obj);
            _unitOfWork.Save();
            TempData["success"] = "Company deleted successfully";

            return RedirectToAction("Index");
        }

        #region API Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> objCompanyList = _unitOfWork.Company.GetAll().ToList();
            return Json(new { data = objCompanyList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Company companyToBeDeleted = _unitOfWork.Company.Get(u => u.Id == id);
            if (companyToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            _unitOfWork.Company.Remove(companyToBeDeleted);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete successful" });
        }
        #endregion
    }
}
