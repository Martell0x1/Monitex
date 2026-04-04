using System.ComponentModel.DataAnnotations;

namespace SmartHome.DTO;

public class CreateDeviceDTO
{

  [Required]
  public string Device_name {set;get;}
}
