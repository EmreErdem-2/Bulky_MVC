using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BulkyBook.DataAccess.Data;
using Microsoft.AspNetCore.Identity;

namespace BulkyBook.DataAccess.DbInitializer
{
    public interface IDbInitializer
    {
        void Initialize();
    }
}
