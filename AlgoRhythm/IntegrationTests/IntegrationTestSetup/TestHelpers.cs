using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Users;
using AlgoRhythm.Shared.Models.Users;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationTests.IntegrationTestSetup
{
    public static class TestHelpers
    {
        public static async Task<string> SetupAuthenticatedUser(
            string email, 
            string password, 
            RoleManager<Role> roleManager,
            UserManager<User> userManager,
            IAuthService authService)
        {
            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new Role
                {
                    Name = "User",
                    NormalizedName = "USER",
                    Description = "Default user role"
                });
            }

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new Role
                {
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    Description = "Administrator with full access"
                });
            }

            var user = new User
            {
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "User",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await userManager.AddToRoleAsync(user, "User");

            var loginRequest = new LoginRequest(email, password);
            var authResponse = await authService.LoginAsync(loginRequest);

            return authResponse.Token;
        }
    }
}
