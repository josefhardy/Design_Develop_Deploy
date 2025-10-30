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
		User user = userRepo.GetUserByEmail(email);

        if (user == null)
        {
			Console.WriteLine("Email does not exist in this context");
			return null;
        }

		if (password == user.password)
		{
			Console.WriteLine("User login successful");
			return user;
		}
		else 
		{
			Console.WriteLine("Incorrect password...");
			return null;
		}
    }
}
