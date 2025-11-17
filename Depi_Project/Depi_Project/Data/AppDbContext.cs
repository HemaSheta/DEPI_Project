using Microsoft.EntityFrameworkCore;
using Depi_Project.Models;

namespace Depi_Project.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }


       
    }
}
