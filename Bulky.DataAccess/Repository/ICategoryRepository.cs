using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public interface ICategoryRepository : IRepository<Category> 
    {
        void Update(Category obj);
        void Save();

    }
}
