using Microsoft.EntityFrameworkCore;
using TestDostavka.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Person> Persons { get; set; }
    public DbSet<Request> Requests { get; set; }
    public DbSet<RequestComment> RequestComments { get; set; }
    public DbSet<Payment> Payments { get; set; }
}
