using Microsoft.AspNetCore.Identity;

namespace Delivery_API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
    }
}
