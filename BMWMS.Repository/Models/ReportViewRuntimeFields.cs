using System.ComponentModel.DataAnnotations.Schema;

namespace BMWMS.Repository.Models;

public partial class VwInboundReport
{
    [NotMapped]
    public string UnitCode { get; set; } = string.Empty;

}

public partial class VwOutboundReport
{
    [NotMapped]
    public string UnitCode { get; set; } = string.Empty;

}
