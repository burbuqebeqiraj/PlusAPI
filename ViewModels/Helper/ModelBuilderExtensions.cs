using System;
using Microsoft.EntityFrameworkCore;
using PlusApi.Models.User;

namespace PlusApi.ViewModels.Helper
{
    public static class ModelBuilderExtensions
    {
        public static void Seed(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserRole>(b =>
            {
                b.HasKey(e => e.UserRoleId);
                b.Property(b => b.UserRoleId).UseIdentityColumn(3,1);
                b.HasData(
                    new UserRole
                    {
                        UserRoleId = 1,
                        RoleName = "SuperAdmin",
                        DisplayName = "SuperAdmin",
                        RoleDesc = "Application SuperAdmin",
                        AddedBy = 1,
                        IsMigrationData = true
                    },
                    new UserRole
                    {
                        UserRoleId = 2,
                        RoleName = "Admin",
                        DisplayName = "Admin",
                        RoleDesc = "All Users",
                        AddedBy = 1,
                        IsMigrationData = true
                    }
                );
            });

            modelBuilder.Entity<Users>(b =>
            {
                b.HasKey(e => e.UserId);
                b.Property(b => b.UserId).UseIdentityColumn(3,1);
                b.HasData(
                    new Users
                    {
                        UserId = 1,
                        UserRoleId = 1,
                        FullName = "Super Admin",
                        Email = "superadmin@gmail.com",
                        Password = "super@admin@2025",
                        Mobile = "",
                        IsActive = true,
                        AddedBy = 1,
                        IsMigrationData = true
                    }
                );
            });
        }
    }
}
