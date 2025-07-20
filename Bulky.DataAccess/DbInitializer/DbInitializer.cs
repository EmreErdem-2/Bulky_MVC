using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.DataAccess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        // Constructor name changed to match the class initialization pattern
        public DbInitializer(UserManager<IdentityUser> userManager,
                             RoleManager<IdentityRole> roleManager,
                             ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        public void Initialize()
        {
            // migrations if they are not applied
            try
            {
                if(_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex) { }

            //create roles if they are not created
            if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();

                //if roles are not created, then we will create admin user as well
                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "admin@bulky.com",
                    Email = "admin@bulky.com",
                    Name = "Admin User",
                    PhoneNumber = "1234567890",
                    StreetAddress = "123 Admin St",
                    State = "IL",
                    PostalCode = "12345",
                    City = "Admin City"
                }, "Admin123!").GetAwaiter().GetResult();

                ApplicationUser adminUser = _db.ApplicationUser.FirstOrDefault(u => u.Email == "admin@bulky.com");
                _userManager.AddToRoleAsync(adminUser, SD.Role_Admin).GetAwaiter().GetResult();
            }

            return;
        }
    }
}
