using System.ComponentModel.DataAnnotations;

namespace SmartHome.Model;

public class Home
{
    [Required]
    public int Id {set;get;}

    [Required]
    public string Name {set;get;}

    [MaxLength(255)]
    public string Address {set;get;}

    [Required]
    public int UserId { get; set; }
    [Required]
    public required User User { get; set; }

}