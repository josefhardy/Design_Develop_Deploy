using System.Collections.Generic;
using Design_Develop_Deploy_Project.Objects;

namespace Tests
{
    public static class TestRepos
    {
        public static List<User> TestUsers = new List<User>
        {
            new User { email = "student@example.com", password = "pass123", role = "Student" },
            new User { email = "supervisor@example.com", password = "superpass", role = "Supervisor" }
        };
    }
}
