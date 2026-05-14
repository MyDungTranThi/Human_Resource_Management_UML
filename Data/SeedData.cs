using HRM.Web.Models.Constants;
using HRM.Web.Models.Entities;
using HRM.Web.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace HRM.Web.Data;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HRMDbContext>();

        // 1. Seed Positions
        if (!await context.Positions.AnyAsync())
        {
            var positions = new List<Position>
            {
                new Position { PosName = AppRoles.Admin, BaseSalary = 50000000, Description = "System Administrator" },
                new Position { PosName = AppRoles.Manager, BaseSalary = 40000000, Description = "Project & Department Manager" },
                new Position { PosName = AppRoles.DeptHead, BaseSalary = 30000000, Description = "Department Head" },
                new Position { PosName = AppRoles.HR, BaseSalary = 20000000, Description = "Human Resources" },
                new Position { PosName = AppRoles.Accountant, BaseSalary = 20000000, Description = "Accountant" },
                new Position { PosName = AppRoles.Staff, BaseSalary = 10000000, Description = "Regular Staff" }
            };
            context.Positions.AddRange(positions);
            await context.SaveChangesAsync();
        }

        // 2. Seed Initial Departments (without managers first)
        if (!await context.Departments.AnyAsync())
        {
            context.Departments.AddRange(
                new Department { DeptName = "Phòng Giám đốc" },
                new Department { DeptName = "Phòng Nhân sự" },
                new Department { DeptName = "Phòng Kế toán" },
                new Department { DeptName = "Phòng Kỹ thuật" }
            );
            await context.SaveChangesAsync();
        }

        // 3. Seed Initial Employees
        if (!await context.Employees.AnyAsync())
        {
            var adminPos = await context.Positions.FirstAsync(p => p.PosName == AppRoles.Admin);
            var managerPos = await context.Positions.FirstAsync(p => p.PosName == AppRoles.Manager);
            var deptHeadPos = await context.Positions.FirstAsync(p => p.PosName == AppRoles.DeptHead);
            var staffPos = await context.Positions.FirstAsync(p => p.PosName == AppRoles.Staff);

            var deptDirector = await context.Departments.FirstAsync(d => d.DeptName == "Phòng Giám đốc");
            var deptHR = await context.Departments.FirstAsync(d => d.DeptName == "Phòng Nhân sự");

            // Manager (Top level)
            var managerEmp = new Employee
            {
                Name = "Big Boss Manager",
                Email = "manager@hrm.com",
                PhoneNumber = "0901234567",
                Address = "Hanoi",
                DeptId = deptDirector.DeptId,
                Status = "Working",
                DateOfBirth = new DateOnly(1985, 1, 1)
            };
            managerEmp.Pos.Add(managerPos);
            context.Employees.Add(managerEmp);
            await context.SaveChangesAsync();

            // Dept Head for HR
            var hrHead = new Employee
            {
                Name = "HR Head",
                Email = "hrhead@hrm.com",
                PhoneNumber = "0902234567",
                Address = "Hanoi",
                DeptId = deptHR.DeptId,
                Status = "Working",
                DateOfBirth = new DateOnly(1990, 5, 20)
            };
            hrHead.Pos.Add(deptHeadPos);
            context.Employees.Add(hrHead);
            await context.SaveChangesAsync();

            // Update Department with Manager
            deptHR.DeptManagerId = hrHead.EmpId;
            await context.SaveChangesAsync();

            // Staff for HR
            var hrStaff = new Employee
            {
                Name = "HR Staff 1",
                Email = "hrstaff1@hrm.com",
                PhoneNumber = "0903234567",
                Address = "Hanoi",
                DeptId = deptHR.DeptId,
                Status = "Working",
                DateOfBirth = new DateOnly(1995, 10, 15)
            };
            hrStaff.Pos.Add(staffPos);
            context.Employees.Add(hrStaff);
            await context.SaveChangesAsync();

            // 4. Seed Accounts
            context.Accounts.AddRange(
                new Account
                {
                    Username = "admin",
                    PasswordHash = PasswordHasher.Hash("123456"),
                    Role = AppRoles.Admin,
                    IsActive = true,
                    EmpId = managerEmp.EmpId // Link admin account to manager for simplicity
                },
                new Account
                {
                    Username = "manager",
                    PasswordHash = PasswordHasher.Hash("123456"),
                    Role = AppRoles.Manager,
                    IsActive = true,
                    EmpId = managerEmp.EmpId
                },
                new Account
                {
                    Username = "hrhead",
                    PasswordHash = PasswordHasher.Hash("123456"),
                    Role = AppRoles.DeptHead,
                    IsActive = true,
                    EmpId = hrHead.EmpId
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
