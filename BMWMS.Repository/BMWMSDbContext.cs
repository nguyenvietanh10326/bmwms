using System;
using System.Collections.Generic;
using BMWMS.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Context;

public partial class BMWMSDbContext : DbContext
{
    public BMWMSDbContext(DbContextOptions<BMWMSDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0BF04C5CF1");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
