using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.User
{
    public class ChangePasswordDTO
    {
        [Required(ErrorMessage = "OldPassword is required.")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "NewPassword is required.")]
        [MinLength(6, ErrorMessage = "NewPassword must be at least 6 characters.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "ConfirmNewPassword is required.")]
        [Compare("NewPassword", ErrorMessage = "ConfirmNewPassword does not match NewPassword.")]
        public string ConfirmNewPassword { get; set; }
    }
}
