using System.ComponentModel.DataAnnotations;

namespace SmartHome.Model;
public class User
{   
    [Required]
    public int Id {get;set;}
    [Required , MaxLength(100)]
    public string Username {get;set;}
    [Required]
    [MaxLength(255)]
    [DataType(DataType.Password)]
    public string Password {get;set;}

    [Required]
    [MaxLength(100)]
    [DataType(DataType.EmailAddress)]
    [EmailAddress]
    public string Email {get;set;}

    [Required]
    public Home home {get;set;}
}