using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.User;

public class PUT_User
{
    [Required(ErrorMessage = "UserName is required.")]
    [MinLength(3, ErrorMessage = "UserName must be at least 3 characters.")]
    public string? UserName { get; set; }

    [Required(ErrorMessage = "UserContact is required.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "UserContact must be a valid 10-digit phone number.")]
    public string? UserContact { get; set; }

    public string? TimeZoneId { get; set; }

    public IFormFile? AvatarImage { get; set; }
}
