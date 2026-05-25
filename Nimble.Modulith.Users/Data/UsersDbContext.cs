using Microsoft.AspNetCore.Identity; using Microsoft.AspNetCore.Identity.EntityFrameworkCore; using Microsoft.EntityFrameworkCore;
namespace Nimble.Modulith.Users.Data; public class UsersDbContext(DbContextOptions<UsersDbContext> options):IdentityDbContext<IdentityUser,IdentityRole,string>(options) { }
