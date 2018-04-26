using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WindowsGoodbye
{
    public class DatabaseContext: DbContext
    {
        public DbSet<DeviceInfo> Devices { get; set; }
        public DbSet<DeviceAuthRecord> AuthRecords { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlite("Data Source=devices.db");
        }
    }
}
