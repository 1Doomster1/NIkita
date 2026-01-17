using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NikitaMicrosoft
{
    public class NikitaDbContext : DbContext
    {
        public DbSet<Message> messages { get; set; }
        public DbSet<Chat> chats { get; set; }
        public DbSet<User> users { get; set; }

        public NikitaDbContext() 
        {
            //Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NikitaDB(L);Trusted_Connection=True;");
        }

    }
}
