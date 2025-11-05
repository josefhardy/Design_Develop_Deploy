using Design_Develop_Deploy_Project.Repos;
using Design_Develop_Deploy_Project.Services;
using Design_Develop_Deploy_Project.UI;
using Design_Develop_Deploy_Project.Utilities;
using System;
class Program 
{
    static void Main(string[] args) 
    {
        string connectionString = "Data Source=Project_database.db;Version=3;";

        var userRepo = new UserRepository(connectionString);
        var validators = new Validators(userRepo);
        var loginMenu = new LoginMenu(validators);

        while (true) 
        {
            Console.Clear();

            var user = loginMenu.ShowLoginScreen();

            if (user == null) { continue; }

            switch (user.role.ToLower()) 
            {
                case "student":
                    var studentMenu = new StudentMenu(user, connectionString);
                    studentMenu.ShowStudentMenu();
                    break;
                case "supervisor":
                    {
                        var supervisorRepo = new SupervisorRepository(connectionString);
                        supervisorRepo.ResetMonthlyInteractionStats();
                        var supervisorMenu = new SupervisorMenu(user, connectionString);
                        supervisorMenu.ShowSupervisorMenu();
                        break;
                    }

                case "senior_tutor":
                    var seniortutorMenu = new SeniorTutorMenu(user, connectionString);
                    seniortutorMenu.ShowSeniorTutorMenu();
                    break;

                default:
                    Console.WriteLine("Unknown role, please double check login details!");
                    break;
            }

        }
    }
}