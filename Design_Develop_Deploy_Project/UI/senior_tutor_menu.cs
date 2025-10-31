using Design.Develop.Deploy.Repos;
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Repos;
using Design_Develop_Deploy_Project.Services;
using Design_Develop_Deploy_Project.Utilities;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Design_Develop_Deploy_Project.UI;
public class SeniorTutorMenu
{
    private readonly MeetingRepository meetingRepo;
    private readonly StudentRepository studentRepo;
    private readonly StatusRepository statusRepo;
    private readonly SupervisorRepository supervisorRepo;
    private readonly User user;
    public SeniorTutorMenu(User user, string connectionString)
	{
        meetingRepo = new MeetingRepository(connectionString);
        studentRepo = new StudentRepository(connectionString);
        statusRepo = new StatusRepository(connectionString);
        supervisorRepo = new SupervisorRepository(connectionString);
        this.user = user;
    }

    public void ShowSeniorTutorMenu() 
    {
        bool exit = false;
        while (!exit) {
            var menuItems = new List<string>
            {
                "View all students",
                "View all supervisors",
                "View Supervisor performance",
                "View at risk students",
                "Logout"
            };

            Console.Clear();
            Console.WriteLine($"Welcome {user.first_name}!");
            int choice = ConsoleHelper.PromptForChoice(menuItems, "====== Senior Tutor Menu ======");

            switch (choice)
            {
                case 1:
                    tutorService.ViewAllStudents(user);
                    ConsoleHelper.Pause();
                    break;
                case 2:
                    tutorService.ViewAllSupervisors(user);
                    ConsoleHelper.Pause();
                    break;
                case 3:
                    tutorService.ViewSupervisorPerformance(user);
                    ConsoleHelper.Pause();
                    break;
                case 4:
                    tutorService.ViewAtRiskStudents(user);
                    ConsoleHelper.Pause();
                    break;
                case 5:
                    exit = true;
                    break;
                default:
                    Console.WriteLine("Invaliud choice, please try again.");
                    break;


            } 
        }
    }

}
