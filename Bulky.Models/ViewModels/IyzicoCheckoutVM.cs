using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iyzipay.Request;
using Iyzipay;

namespace BulkyBook.Models.ViewModels
{
    public class IyzicoCheckoutVM
    {
        public Options Options { get; set; }

        public CreateCheckoutFormInitializeRequest CheckoutFormRequest { get; set; }
    }
}
