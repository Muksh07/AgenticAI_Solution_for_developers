using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Database
{
    public class DatabaseContext : DbContext
    {
         protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data Source =; Initial Catalog = ToDOList; Integrated Security = True; Encrypt = False");
        }
        public DbSet<ProjectFeedback> ProjectFeedbacks { get; set; }
        public DbSet<ProjectLifecycle> ProjectLifecycles  { get; set;}    
        
    }
}