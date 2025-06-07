using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Please enter the Title.")]
        public string Title { get; set; }
        public string Description { get; set; }
        [Required(ErrorMessage = "Please enter the ISBN.")]
        public string ISBN { get; set; }
        [Required(ErrorMessage = "Please enter the Author.")]
        public string Author { get; set; }
        [Required]
        [Display(Name = "List Price")]
        [Range(1, 1000, ErrorMessage = "List Price must be between 1 and 1000.")]
        public double ListPrice { get; set; }
        [Required]
        [Display(Name = "List Price")]
        [Range(1, 1000)]
        public double Price { get; set; }
        [Required]
        [Display(Name = "Price for 50+")]
        [Range(1, 1000)]
        public double Price50 { get; set; }
        [Required]
        [Display(Name = "Price for 100+")]
        [Range(1, 1000)]
        public double Price100 { get; set; }
    }
}
