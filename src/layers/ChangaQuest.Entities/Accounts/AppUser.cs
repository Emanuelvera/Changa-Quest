using Microsoft.AspNetCore.Identity;

namespace ChangaQuest.Entities.Accounts;

public sealed class AppUser : IdentityUser
{
    public bool DeclaredAdult { get; set; }
    public double? SearchLatitude { get; set; }
    public double? SearchLongitude { get; set; }
}
