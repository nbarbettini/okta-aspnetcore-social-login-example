using System.ComponentModel.DataAnnotations;

namespace SocialLoginExample.Models
{
    public class CompleteProfileViewModel
    {
        [Required]
        [Display(Name = "Rewards number")]
        public string RewardsNumber { get; set; }

        public string Error { get; set; }
    }
}
