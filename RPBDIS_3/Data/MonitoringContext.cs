using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RPBDIS_3.Models;

namespace RPBDIS_3.Data;

public partial class MonitoringContext : DbContext
{
    public MonitoringContext()
    {
    }

    public MonitoringContext(DbContextOptions<MonitoringContext> options)
        : base(options)
    {
    }


    public virtual DbSet<CompletedWork> CompletedWorks { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Equipment> Equipments { get; set; }

    public virtual DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; }

    public virtual DbSet<MaintenanceType> MaintenanceTypes { get; set; }

    public virtual DbSet<ViewMaintenanceCostAnalysis> ViewMaintenanceCostAnalyses { get; set; }

    public virtual DbSet<ViewUnscheduledMaintenanceFrequency> ViewUnscheduledMaintenanceFrequencies { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Создаем новый ConfigurationBuilder
        ConfigurationBuilder builder = (ConfigurationBuilder)new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())  // Устанавливаем путь к текущему каталогу
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);  // Добавляем JSON файл конфигурации

        // Создаем конфигурацию
        IConfigurationRoot configuration = builder.Build();

        // Получаем строку подключения из appsettings.json
        string connectionString = configuration.GetConnectionString("DefaultConnection");

        // Настраиваем подключение к базе данных
        optionsBuilder.UseSqlServer(connectionString);

        // Логируем SQL-запросы в отладочный вывод
        optionsBuilder.LogTo(message => System.Diagnostics.Debug.WriteLine(message));


    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompletedWork>(entity =>
        {
            entity.HasKey(e => e.CompletedMaintenanceId).HasName("PK__Complete__F4D87C7A0F5AC2B4");

            entity.Property(e => e.CompletedMaintenanceId).HasColumnName("CompletedMaintenanceID");
            entity.Property(e => e.ActualCost).HasColumnType("money");
            entity.Property(e => e.EquipmentId).HasColumnName("EquipmentID");
            entity.Property(e => e.MaintenanceTypeId).HasColumnName("MaintenanceTypeID");
            entity.Property(e => e.ResponsibleEmployeeId).HasColumnName("ResponsibleEmployeeID");

            entity.HasOne(d => d.Equipment).WithMany(p => p.CompletedWorks)
                .HasForeignKey(d => d.EquipmentId)
                .HasConstraintName("FK_CompletedWorks_Equipment");

            entity.HasOne(d => d.MaintenanceType).WithMany(p => p.CompletedWorks)
                .HasForeignKey(d => d.MaintenanceTypeId)
                .HasConstraintName("FK_CompletedWorks_MaintenanceTypes");

            entity.HasOne(d => d.ResponsibleEmployee).WithMany(p => p.CompletedWorks)
                .HasForeignKey(d => d.ResponsibleEmployeeId)
                .HasConstraintName("FK_CompletedWorks_Employees");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04FF154B889B2");

            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Position).HasMaxLength(50);
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.EquipmentId).HasName("PK__Equipmen__3447459912FB6037");

            entity.Property(e => e.EquipmentId).HasColumnName("EquipmentID");
            entity.Property(e => e.InventoryNumber).HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<MaintenanceSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Maintena__9C8A5B695AFF160E");

            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
            entity.Property(e => e.EquipmentId).HasColumnName("EquipmentID");
            entity.Property(e => e.EstimatedCost).HasColumnType("money");
            entity.Property(e => e.MaintenanceTypeId).HasColumnName("MaintenanceTypeID");
            entity.Property(e => e.ResponsibleEmployeeId).HasColumnName("ResponsibleEmployeeID");

            entity.HasOne(d => d.Equipment).WithMany(p => p.MaintenanceSchedules)
                .HasForeignKey(d => d.EquipmentId)
                .HasConstraintName("FK_Schedules_Equipment");

            entity.HasOne(d => d.MaintenanceType).WithMany(p => p.MaintenanceSchedules)
                .HasForeignKey(d => d.MaintenanceTypeId)
                .HasConstraintName("FK_Schedules_MaintenanceTypes");

            entity.HasOne(d => d.ResponsibleEmployee).WithMany(p => p.MaintenanceSchedules)
                .HasForeignKey(d => d.ResponsibleEmployeeId)
                .HasConstraintName("FK_Schedules_Employees");
        });

        modelBuilder.Entity<MaintenanceType>(entity =>
        {
            entity.HasKey(e => e.MaintenanceTypeId).HasName("PK__Maintena__C1017E6B5119736D");

            entity.Property(e => e.MaintenanceTypeId).HasColumnName("MaintenanceTypeID");
            entity.Property(e => e.Description).HasMaxLength(100);
        });

        modelBuilder.Entity<ViewMaintenanceCostAnalysis>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("View_MaintenanceCostAnalysis");

            entity.Property(e => e.EquipmentName).HasMaxLength(100);
            entity.Property(e => e.TotalMaintenanceCost).HasColumnType("money");
        });

        modelBuilder.Entity<ViewUnscheduledMaintenanceFrequency>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("View_UnscheduledMaintenanceFrequency");

            entity.Property(e => e.EquipmentName).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
