using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NIkita
{
    public class NikitaDBContext : DbContext
    {
        public DbSet<Message> messages {  get; set; }
        public NikitaDBContext()
        { 
            Database.EnsureDeleted();
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NikitaDB;Trusted_Connection=True;");
        }

    }
}
