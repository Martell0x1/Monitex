using System.ComponentModel.DataAnnotations;

namespace SmartHome.DTO;
public class LoginDTO
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    [DataType(DataType.EmailAddress)]
    public string Email {set;get;}

    [Required]
    [MaxLength(255)]
    [DataType(DataType.Password)]
    [RegularExpression("^[A-Z](?=.*[0-9])(?=.*[^A-Za-z0-9]).{7,}$")]
    public string Password {set;get;}

}