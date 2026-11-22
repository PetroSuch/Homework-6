using System.ComponentModel.DataAnnotations;

namespace TaskAuthenticationAuthorization.Models
{
    public class EditUserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }

        [Required, RegularExpression("Admin|Buyer")]
        public string Role { get; set; }

        [Required, RegularExpression("none|regular|golden|wholesale")]
        [Display(Name = "Buyer type")]
        public string BuyerType { get; set; }

        [Display(Name = "Customer")]
        public int? CustomerId { get; set; }
    }
}
