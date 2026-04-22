using POS_system_cs.Domain.Enums;

namespace POS_system_cs.Domain.Entities;

public sealed class User : EntityBase
{
    public string UserName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Cashier;

    public bool IsActive { get; set; } = true;
}
