using Design_Develop_Deploy_Project.Repos;
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Design_Develop_Deploy_Project.UI
{
    public class LoginMenu
    {
        private readonly Validators validators;

        public LoginMenu(Validators _validators) 
        {
            validators = _validators;
        }

        public User ShowLoginScreen() 
        {
            Thread.Sleep(3000);
            Console.Clear();
            Console.WriteLine("========== LOGIN ==========");
            Console.WriteLine("Please enter email");
            string email = Console.ReadLine()?.Trim().ToLower();

            Console.Clear();
            Console.WriteLine("========== LOGIN ==========");
            Console.WriteLine("Please enter password");
            string password = Console.ReadLine()?.Trim();

            var User = validators.ValidateLogin(email, password);
            return User;

        }
    }
}
