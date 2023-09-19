using Microsoft.EntityFrameworkCore;
using MinimalApiDemo.Models;

namespace MinimalApiDemo.Data
{
    public class MinimalApiContextDb : DbContext
    {
        public MinimalApiContextDb(DbContextOptions<MinimalApiContextDb> dbContextOptions) : base(dbContextOptions) { }

        public DbSet<Fornecedor> Fornecedor { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Fornecedor>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<Fornecedor>().
                Property(e => e.Nome).
                IsRequired().
                HasColumnType("VARCHAR(100)");

            modelBuilder.Entity<Fornecedor>()
                .Property(e => e.Documentos)
                .IsRequired()
                .HasColumnType("VARCHAR(14)");

            modelBuilder.Entity<Fornecedor>()
                .ToTable("Fornecedores");

          base.OnModelCreating(modelBuilder);
        }
    }
}
