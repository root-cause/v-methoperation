using System;

namespace MethOperation.Enums
{
    [Flags]
    public enum LabFlags
    {
        None = 0,
        IsOwned = 1,
        HasDoneSetup = 2,
        HasEquipmentUpgrade = 4,
        HasStaffUpgrade = 8,
        HasSecurityUpgrade = 16
    }
}
