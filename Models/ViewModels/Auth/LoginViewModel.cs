using System.ComponentModel.DataAnnotations;

namespace HRM.Web.Models.ViewModels.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui long nhap ten dang nhap.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap mat khau.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
