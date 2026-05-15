// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.EntityFrameworkCore;

namespace Zerra.Repository.Benchmark.EFData
{
    public sealed class EFDataContext : DbContext
    {
        private const string connectionString = "data source=.;initial catalog=ZerraSqlTest;integrated security=True;MultipleActiveResultSets=True;TrustServerCertificate=True;";

        public DbSet<EFTestTypesModel> TestTypes { get; set; }
        public DbSet<EFTestRelationsModel> TestRelations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EFTestRelationsModel>(entity =>
            {
                entity.ToTable("TestRelations");
                entity.HasKey(e => e.RelationAKey);
                entity.Property(e => e.RelationAKey).ValueGeneratedOnAdd();
                entity.Property(e => e.SomeValue).IsRequired(false);
            });

            modelBuilder.Entity<EFTestTypesModel>(entity =>
            {
                entity.ToTable("TestTypes");
                entity.HasKey(e => new { e.KeyA, e.KeyB });
                entity.Property(e => e.KeyA).ValueGeneratedNever();
                entity.Property(e => e.KeyB).ValueGeneratedNever();

                entity.Property(e => e.DateTimeThing).HasPrecision(6);
                entity.Property(e => e.DateTimeOffsetThing).HasPrecision(6);
                entity.Property(e => e.TimeSpanThing).HasPrecision(6);
                entity.Property(e => e.DateOnlyThing).HasPrecision(6);
                entity.Property(e => e.TimeOnlyThing).HasPrecision(6);

                entity.Property(e => e.DateTimeNullableThing).HasPrecision(6);
                entity.Property(e => e.DateTimeOffsetNullableThing).HasPrecision(6);
                entity.Property(e => e.TimeSpanNullableThing).HasPrecision(6);
                entity.Property(e => e.DateOnlyNullableThing).HasPrecision(6);
                entity.Property(e => e.TimeOnlyNullableThing).HasPrecision(6);

                entity.Property(e => e.DateTimeNullableThingNull).HasPrecision(6);
                entity.Property(e => e.DateTimeOffsetNullableThingNull).HasPrecision(6);
                entity.Property(e => e.TimeSpanNullableThingNull).HasPrecision(6);
                entity.Property(e => e.DateOnlyNullableThingNull).HasPrecision(6);
                entity.Property(e => e.TimeOnlyNullableThingNull).HasPrecision(6);

                entity.Property(e => e.StringThing).IsRequired(false);
                entity.Property(e => e.StringThingNull).IsRequired(false);
                entity.Property(e => e.BytesThing).IsRequired(false);
                entity.Property(e => e.BytesThingNull).IsRequired(false);

                entity.HasOne(e => e.RelationA)
                    .WithMany()
                    .HasForeignKey(e => e.RelationAKey)
                    .IsRequired(false);

                entity.HasMany(e => e.RelationB)
                    .WithOne()
                    .HasForeignKey(r => r.RelationBKey)
                    .HasPrincipalKey(e => e.KeyA)
                    .IsRequired(false);
            });
        }
    }
}
