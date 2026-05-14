using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HRM.Web.Models.Entities;

public partial class HRMDbContext : DbContext
{
    public HRMDbContext()
    {
    }

    public HRMDbContext(DbContextOptions<HRMDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }
    public virtual DbSet<Assign> Assigns { get; set; }
    public virtual DbSet<Department> Departments { get; set; }
    public virtual DbSet<Employee> Employees { get; set; }
    public virtual DbSet<EmploymentContract> EmploymentContracts { get; set; }
    public virtual DbSet<Position> Positions { get; set; }
    public virtual DbSet<Project> Projects { get; set; }
    public virtual DbSet<Reward> Rewards { get; set; }
    public virtual DbSet<Salary> Salaries { get; set; }
    public virtual DbSet<Schedule> Schedules { get; set; }
    public virtual DbSet<Timekeeping> Timekeepings { get; set; }
    public virtual DbSet<Absence> Absences { get; set; }
    public virtual DbSet<VwEmployeeOverview> VwEmployeeOverviews { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__ACCOUNT__349DA5864CC5D606");
            entity.ToTable("ACCOUNT");
            entity.HasIndex(e => e.Username, "UQ__ACCOUNT__536C85E407DE919A").IsUnique();
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.EmpId).HasColumnName("EmpID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(100);
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.Username).HasMaxLength(50);
            entity.HasOne(d => d.Emp).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.EmpId)
                .HasConstraintName("FK_ACCOUNT_EMPLOYEE");
        });

        modelBuilder.Entity<Assign>(entity =>
        {
            entity.HasKey(e => new { e.EmpId, e.ProjId }).HasName("PK__ASSIGN__7E4FA8D67C44030C");
            entity.ToTable("ASSIGN", tb => tb.HasTrigger("trg_InsertRewardOnGoodEvaluate"));
            entity.Property(e => e.EmpId).HasColumnName("EmpID");
            entity.Property(e => e.ProjId).HasColumnName("ProjID");
            entity.HasOne(d => d.Emp).WithMany(p => p.Assigns)
                .HasForeignKey(d => d.EmpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ASSIGN__EmpID__7E37BEF6");
            entity.HasOne(d => d.Proj).WithMany(p => p.Assigns)
                .HasForeignKey(d => d.ProjId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ASSIGN__ProjID__7F2BE32F");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DeptId).HasName("PK__DEPARTME__0148818E0C36F373");
            entity.ToTable("DEPARTMENT");
            entity.HasIndex(e => e.DeptName, "UQ__DEPARTME__5E508265D8E3D572").IsUnique();
            entity.Property(e => e.DeptId).HasColumnName("DeptID");
            entity.Property(e => e.DeptManagerId).HasColumnName("DeptManagerID");
            entity.Property(e => e.DeptName).HasMaxLength(50);
            entity.HasOne(d => d.DeptManager).WithMany(p => p.Departments)
                .HasForeignKey(d => d.DeptManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Department_Manager");
            entity.HasMany(d => d.Projs).WithMany(p => p.Depts)
                .UsingEntity<Dictionary<string, object>>(
                    "DepartmentProject",
                    r => r.HasOne<Project>().WithMany()
                        .HasForeignKey("ProjId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__DEPARTMEN__ProjI__0B91BA14"),
                    l => l.HasOne<Department>().WithMany()
                        .HasForeignKey("DeptId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__DEPARTMEN__DeptI__0A9D95DB"),
                    j =>
                    {
                        j.HasKey("DeptId", "ProjId").HasName("PK__DEPARTME__D02A9321AB9A3811");
                        j.ToTable("DEPARTMENT_PROJECT");
                        j.IndexerProperty<int>("DeptId").HasColumnName("DeptID");
                        j.IndexerProperty<int>("ProjId").HasColumnName("ProjID");
                    });
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmpId).HasName("PK__EMPLOYEE__AF2DBA79558517D5");
            entity.ToTable("EMPLOYEE");
            entity.HasIndex(e => e.PhoneNumber, "UQ__EMPLOYEE__85FB4E384BE85A5A").IsUnique();
            entity.HasIndex(e => e.Email, "UQ__EMPLOYEE__A9D10534A9F1E3E9").IsUnique();
            entity.Property(e => e.EmpId).HasColumnName("EmpID");
            entity.Property(e => e.Address).HasMaxLength(100);
            entity.Property(e => e.AnnualLeaveDays).HasColumnType("decimal(5, 1)");
            entity.Property(e => e.DeptId).HasColumnName("DeptID");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.ImagePath).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.PhoneNumber).HasMaxLength(15);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.HasOne(d => d.Dept).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DeptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employee_Dept");
            entity.HasMany(d => d.Pos).WithMany(p => p.Emps)
                .UsingEntity<Dictionary<string, object>>(
                    "EmployeePosition",
                    r => r.HasOne<Position>().WithMany()
                        .HasForeignKey("PosId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__EMPLOYEE___PosID__03F0984C"),
                    l => l.HasOne<Employee>().WithMany()
                        .HasForeignKey("EmpId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__EMPLOYEE___EmpID__02FC7413"),
                    j =>
                    {
                        j.HasKey("EmpId", "PosId").HasName("PK__EMPLOYEE__8958C8C2C8F4F15E");
                        j.ToTable("EMPLOYEE_POSITION");
                        j.IndexerProperty<int>("EmpId").HasColumnName("EmpID");
                        j.IndexerProperty<int>("PosId").HasColumnName("PosID");
                    });
        });

        modelBuilder.Entity<EmploymentContract>(entity =>
        {
            entity.HasKey(e => e.ContId).HasName("PK__EMPLOYME__F03BCFB9ADD1C06D");
            entity.ToTable("EMPLOYMENT_CONTRACT");
            entity.Property(e => e.ContId).HasColumnName("ContID");
            entity.Property(e => e.ContractType).HasMaxLength(50);
            entity.Property(e => e.EmpId).HasColumnName("EmpID");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.HasOne(d => d.Emp).WithMany(p => p.EmploymentContracts)
                .HasForeignKey(d => d.EmpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EMPLOYMEN__EmpID__75A278F5");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.PosId).HasName("PK__POSITION__67572BB397E44412");
            entity.ToTable("POSITION");
            entity.Property(e => e.PosId).HasColumnName("PosID");
            entity.Property(e => e.BaseSalary).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.PosName).HasMaxLength(50);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjId).HasName("PK__PROJECT__16212AFCAB8DCF36");
            entity.ToTable("PROJECT");
            entity.Property(e => e.ProjId).HasColumnName("ProjID");
            entity.Property(e => e.ProjName).HasMaxLength(100);
            entity.Property(e => e.ProjStatus).HasMaxLength(50);
            entity.Property(e => e.AssignedByManagerId).HasColumnName("AssignedByManagerID");
        });

        modelBuilder.Entity<Reward>(entity =>
        {
            entity.HasKey(e => e.RewardId).HasName("PK__REWARD__82501599724EDC10");
            entity.ToTable("REWARD");
            entity.Property(e => e.RewardId).HasColumnName("RewardID");
            entity.Property(e => e.EmpId).HasColumnName("EmpID");
            entity.Property(e => e.ProjId).HasColumnName("ProjID");
            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.Property(e => e.RewardType).HasMaxLength(100);
            entity.Property(e => e.Value).HasColumnType("decimal(15, 2)");
            entity.HasOne(d => d.Emp).WithMany(p => p.Rewards)
                .HasForeignKey(d => d.EmpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__REWARD__EmpID__6E01572D");
            entity.HasOne(d => d.Proj).WithMany(p => p.Rewards)
                .HasForeignKey(d => d.ProjId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__REWARD__ProjID__6EF57B66");
        });

        modelBuilder.Entity<Salary>(entity =>
        {
            entity.HasKey(e => new { e.EmpId, e.PayrollMonth, e.PayrollYear }).HasName("PK__SALARY__DAD748D51A3D2ED3");
            entity.ToTable("SALARY");
            entity.Property(e => e.EmpId).HasColumnName("EmpID");
            entity.Property(e => e.BaseSalary).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.Bonus).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.Deduction).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.LeaveDaysInMonth).HasColumnType("decimal(5, 1)");
            entity.Property(e => e.NetSalary).HasColumnType("decimal(15, 2)");
            entity.HasOne(d => d.Emp).WithMany(p => p.Salaries)
                .HasForeignKey(d => d.EmpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SALARY__EmpID__6B24EA82");
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__SCHEDULE__9C8A5B698E76E439");
            entity.ToTable("SCHEDULE");
            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
            entity.Property(e => e.EmpId).HasColumnName("EmpID");
            entity.Property(e => e.Shift).HasMaxLength(50);
            entity.HasOne(d => d.Emp).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.EmpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SCHEDULE__EmpID__797309D9");
        });

        modelBuilder.Entity<Timekeeping>(entity =>
        {
            entity.HasKey(e => new { e.EmpId, e.WorkDate }).HasName("PK__TIMEKEEP__ED608B30536067D6");
            entity.ToTable("TIMEKEEPING", tb => tb.HasTrigger("trg_CalculateWorkingHours"));
            entity.Property(e => e.EmpId).HasColumnName("EmpID");
            entity.HasOne(d => d.Emp).WithMany(p => p.Timekeepings)
                .HasForeignKey(d => d.EmpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TIMEKEEPI__EmpID__5EBF139D");
        });

        modelBuilder.Entity<Absence>(entity =>
        {
            entity.HasKey(e => e.AbsenceId).HasName("PK_Absence");
            entity.ToTable("ABSENCE");
            entity.Property(e => e.AbsenceId).HasColumnName("AbsenceID");
            entity.Property(e => e.EmpId).HasColumnName("EmpID");
            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.HasOne(d => d.Emp).WithMany(p => p.Absences)
                .HasForeignKey(d => d.EmpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Absence_Employee");
        });

        modelBuilder.Entity<VwEmployeeOverview>(entity =>
        {
            entity.HasNoKey().ToView("vw_EmployeeOverview");
            entity.Property(e => e.Department).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.EmpId).HasColumnName("EmpID");
            entity.Property(e => e.EmployeeName).HasMaxLength(50);
            entity.Property(e => e.Gender).HasMaxLength(3);
            entity.Property(e => e.PhoneNumber).HasMaxLength(15);
            entity.Property(e => e.Position).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
