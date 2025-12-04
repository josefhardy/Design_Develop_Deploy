using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Repos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Design_Develop_Deploy_Project.Utilities
{
    public class Validators
    {
        private readonly UserRepository userRepo;
        private readonly List<User> _testUsers;

        // Production constructor
        public Validators(UserRepository _userRepo)
        {
            userRepo = _userRepo;
        }

        // Test constructor
        public Validators(List<User> testUsers)
        {
            _testUsers = testUsers;
        }

        public User ValidateLogin(string email, string password)
        {
            User user;

            // Use test data if provided
            if (_testUsers != null)
            {
                user = _testUsers.FirstOrDefault(u => u.email.Trim().ToLower() == email.Trim().ToLower());
            }
            // Otherwise, use real repository
            else if (userRepo != null)
            {
                try
                {
                    user = userRepo.GetUserByEmail(email);
                }
                catch (Exception ex)
                {
                    ConsoleHelper.PrintSection("Database Error", ex.Message, "Red");
                    ConsoleHelper.Pause("Press any key to return to login...");
                    return null;
                }
            }
            else
            {
                throw new Exception("No data source provided for Validators.");
            }

            // Validation logic (same for test or production)
            if (user == null)
            {
                ConsoleHelper.PrintSection("Login Failed", "Email does not exist in this system.", "Red");
                Thread.Sleep(3000);
                return null;
            }

            if (password == user.password)
            {
                return user;
            }
            else
            {
                ConsoleHelper.PrintSection("Incorrect Password", "The password you entered is incorrect.", "Red");
                Thread.Sleep(3000);
                return null;
            }
        }
    }
}
