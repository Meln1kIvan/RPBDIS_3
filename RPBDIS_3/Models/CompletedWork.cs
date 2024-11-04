using System;
using System.Collections.Generic;

namespace RPBDIS_3.Models;

public partial class CompletedWork
{
    public int CompletedMaintenanceId { get; set; }

    public int? MaintenanceTypeId { get; set; }

    public int? EquipmentId { get; set; }

    public DateOnly? CompletionDate { get; set; }

    public int? ResponsibleEmployeeId { get; set; }

    public decimal? ActualCost { get; set; }

    public virtual Equipment? Equipment { get; set; }

    public virtual MaintenanceType? MaintenanceType { get; set; }

    public virtual Employee? ResponsibleEmployee { get; set; }
}
