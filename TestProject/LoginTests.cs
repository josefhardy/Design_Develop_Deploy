using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Utilities; // For Validators
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Tests
{
    public class LoginTests
    {
        private readonly Validators _validators;

        public LoginTests()
        {
            // Inject test users into the real Validators class
            _validators = new Validators(TestRepos.TestUsers);
        }

        [Fact]
        public void ValidateLogin_ValidStudent_ReturnsUser()
        {
            var user = _validators.ValidateLogin("student@example.com", "pass123");
            Assert.NotNull(user);
            Assert.Equal("student@example.com", user.email);
        }

        [Fact]
        public void ValidateLogin_ValidSupervisor_ReturnsUser()
        {
            var user = _validators.ValidateLogin("supervisor@example.com", "superpass");
            Assert.NotNull(user);
            Assert.Equal("supervisor@example.com", user.email);
        }

        [Fact]
        public void ValidateLogin_InvalidPassword_ReturnsNull()
        {
            var user = _validators.ValidateLogin("student@example.com", "wrongpass");
            Assert.Null(user);
        }

        [Fact]
        public void ValidateLogin_NonExistentUser_ReturnsNull()
        {
            var user = _validators.ValidateLogin("nonexistent@example.com", "pass123");
            Assert.Null(user);
        }
    }
}
