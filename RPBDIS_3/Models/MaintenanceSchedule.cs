using System;
using System.Collections.Generic;

namespace RPBDIS_3.Models;

public partial class MaintenanceSchedule
{
    public int ScheduleId { get; set; }

    public int? EquipmentId { get; set; }

    public int? MaintenanceTypeId { get; set; }

    public DateOnly? ScheduledDate { get; set; }

    public int? ResponsibleEmployeeId { get; set; }

    public decimal? EstimatedCost { get; set; }

    public virtual Equipment? Equipment { get; set; }

    public virtual MaintenanceType? MaintenanceType { get; set; }

    public virtual Employee? ResponsibleEmployee { get; set; }
}
