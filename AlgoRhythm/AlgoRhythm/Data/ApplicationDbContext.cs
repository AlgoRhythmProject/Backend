using AlgoRhythm.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace AlgoRhythm.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : base(opts) { }

    public DbSet<User> Users { get; set; } = null!;
}