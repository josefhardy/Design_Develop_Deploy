using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Repos;
using System;

namespace Design_Develop_Deploy_Project.Utilities;

public class Validators
{
	private readonly UserRepository userRepo;
	public Validators(UserRepository _userRepo)
	{
		userRepo = _userRepo;
	}

    public User ValidateLogin(string email, string password)
    {

        try
        {
            User user = userRepo.GetUserByEmail(email);

            if (user == null)
            {
                ConsoleHelper.PrintSection("Login Failed", "Email does not exist in this system.");
                Thread.Sleep(2000);
                return null;
            }

            if (password == user.password)
            {
                return user;
            }
            else
            {
                ConsoleHelper.PrintSection("Incorrect Password", "The password you entered is incorrect.");
                Thread.Sleep(2000);
                return null;
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.PrintSection("Database Error", ex.Message);
            ConsoleHelper.Pause("Press any key to return to login...");
            return null;
        }
    }

}
