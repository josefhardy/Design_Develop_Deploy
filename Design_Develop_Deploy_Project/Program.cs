using Design_Develop_Deploy_Project.Repos;
using Design_Develop_Deploy_Project.Services;
using Design_Develop_Deploy_Project.UI;
using Design_Develop_Deploy_Project.Utilities;
using Design_Develop_Deploy_Project.Tables;
using System;
class Program 
{
    static void Main(string[] args) 
    {
        DatabaseInitializer.EnsureCreated();
        //DatabaseSeeder.WipeTable();
        //Thread.Sleep(2000);
        //DatabaseSeeder.Seed();
        //Thread.Sleep(3000);
        //Console.Clear();
        //DatabaseSeeder.PrintAllUsers();
        //Console.ReadKey();
        string dbpath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Tables", "Project_database.db")
        );

        string connectionString = $"Data Source={dbpath};Version=3;";


        var userRepo = new UserRepository(connectionString);
        var validators = new Validators(userRepo);
        var loginMenu = new LoginMenu(validators);

        while (true) 
        {
            Console.Clear();

            var user = loginMenu.ShowLoginScreen();

            if (user == null) { continue; }

            Console.Clear();
            Console.WriteLine($"Hi {user.first_name}, Welcome back!");
            Console.WriteLine();
            Console.WriteLine("Logging you in now...");
            Thread.Sleep(3000);

            switch (user.role.ToLower()) 
            {
                case "student":
                    var studentMenu = new StudentMenu(user, connectionString);
                    studentMenu.ShowStudentMenu();
                    break;
                case "supervisor":
                    var supervisorRepo = new SupervisorRepository(connectionString);
                    var interactionRepo = new InteractionRepository(connectionString);
                    var functionService = new SupervisorFunctionService(supervisorRepo, interactionRepo);
                    functionService.ResetMonthlyInteractionStatsIfNeeded();

                        var supervisorMenu = new SupervisorMenu(user, connectionString);
                        supervisorMenu.ShowSupervisorMenu();
                        break;
                    
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