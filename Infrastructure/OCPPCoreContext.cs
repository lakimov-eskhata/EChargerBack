

#nullable disable

using Domain.Entities.Station;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public partial class OCPPCoreContext : DbContext
    {
        public OCPPCoreContext(DbContextOptions<OCPPCoreContext> options)
            : base(options)
        {
        }

        public virtual DbSet<ChargePointEntity> ChargePoints { get; set; }
        public virtual DbSet<ChargeTagEntity> ChargeTags { get; set; }
        public virtual DbSet<ConnectorStatusEntity> ConnectorStatuses { get; set; }
        public virtual DbSet<TransactionEntity> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChargePointEntity>(entity =>
            {
                entity.ToTable("ChargePoint");

                entity.HasKey(e => e.ChargePointId);
                
                entity.HasIndex(e => e.ChargePointId, "ChargePoint_Identifier")
                    .IsUnique();

                entity.Property(e => e.ChargePointId).HasMaxLength(100);

                entity.Property(e => e.Comment).HasMaxLength(200);

                entity.Property(e => e.Name).HasMaxLength(100);

                entity.Property(e => e.Username).HasMaxLength(50);

                entity.Property(e => e.Password).HasMaxLength(50);

                entity.Property(e => e.ClientCertThumb).HasMaxLength(100);
            });

            modelBuilder.Entity<ChargeTagEntity>(entity =>
            {
                entity.HasKey(e => e.TagId)
                    .HasName("PK_ChargeKeys");

                entity.Property(e => e.TagId).HasMaxLength(50);

                entity.Property(e => e.ParentTagId).HasMaxLength(50);

                entity.Property(e => e.TagName).HasMaxLength(200);
            });

            modelBuilder.Entity<ConnectorStatusEntity>(entity =>
            {
                entity.HasKey(e => new { e.ChargePointId, e.ConnectorId });

                entity.ToTable("ConnectorStatus");

                entity.Property(e => e.ChargePointId).HasMaxLength(100);

                entity.Property(e => e.ConnectorName).HasMaxLength(100);

                entity.Property(e => e.LastStatus).HasMaxLength(100);
            });

            modelBuilder.Entity<TransactionEntity>(entity =>
            {
                entity.HasKey(x => x.TransactionId);
                
                entity.Property(e => e.Uid).HasMaxLength(50);

                entity.Property(e => e.ChargePointId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.StartTagId).HasMaxLength(50);

                entity.Property(e => e.StartResult).HasMaxLength(100);

                entity.Property(e => e.StopTagId).HasMaxLength(50);

                entity.Property(e => e.StopReason).HasMaxLength(100);

                entity.HasOne(d => d.ChargePoint)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.ChargePointId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Transactions_ChargePoint");

                entity.HasIndex(e => new { e.ChargePointId, e.ConnectorId });
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
