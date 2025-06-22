using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Converge.Data
{
    public class DesignTimeConvergeDbContextFactory : IDesignTimeDbContextFactory<ConvergeDbContext>
    {
        public ConvergeDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ConvergeDbContext>();
            optionsBuilder.UseSqlite("Data Source=converge.db");

            return new ConvergeDbContext(optionsBuilder.Options);
        }
    }
}
