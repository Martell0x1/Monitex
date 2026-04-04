using System.ComponentModel.DataAnnotations;

namespace SmartHome.Model;

public class Device
{
    [Required]
    public int Device_id { get; set; }

  [Required]
  public int User_id {get ; set;}

  [Required]

  public string Device_name {get ; set;}

  [Required]

  public string Device_status{get; set;}

  public DateTime LastSeen {get;set;}

}
