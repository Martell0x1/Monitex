using System.ComponentModel.DataAnnotations;

namespace SmartHome.DTO;
public class RegisterDTO
{
    [Required , MaxLength(100)]
    public string Username {get;set;}
    [Required]
    [MaxLength(255)]
    [DataType(DataType.Password)]
    [RegularExpression("^[A-Z](?=.*[0-9])(?=.*[^A-Za-z0-9]).{7,}$")]
    public string Password {get;set;}

    [Required]
    [MaxLength(100)]
    [DataType(DataType.EmailAddress)]
    [EmailAddress]
    public string Email {get;set;}
}