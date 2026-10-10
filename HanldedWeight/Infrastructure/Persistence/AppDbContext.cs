using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
namespace HandledWeight.Infrastructure.Persistence{
    public class AppDbContext : DbContext{

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){ }
            //mapeamos el modelo para posteriormente puedamos crear la tabla en el servidor de base de datos
            public DbSet<Entities.Users> Users { get; set; }
            public DbSet<Entities.Roles> Roles { get; set; }
            public DbSet<Entities.BussinesPartners> BussinesPartners { get; set; }
            public DbSet<Entities.Products> Products { get; set; }
            public DbSet<Entities.Vehicle> Vehicle{ get; set; }
            public DbSet<Entities.Devices> Devices { get; set; }
            public DbSet<Entities.Drivers> Drivers { get; set; }
            public DbSet<Entities.Receipts> Receipts { get; set; }
            public DbSet<Entities.Factory> Factory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                // Opcional: Fluent API para reglas específicas (similar a las columnas en Laravel)
                modelBuilder.Entity<Entities.Users>(entity =>
                {
                    entity.Property(p => p.Name)
                          .HasMaxLength(150)
                          .IsRequired();
                });

            modelBuilder.Entity<Entities.Users>().HasIndex(u => u.Email).IsUnique();

        }

    }
}
