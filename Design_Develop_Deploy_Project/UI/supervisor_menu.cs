using System;
using Design_Develop_Deploy_Project.Repos;
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Utilities;
using Design_Develop_Deploy_Project.Services;

namespace Design_Develop_Deploy_Project.UI;

public class SupervisorMenu
{
    private readonly string _connectionString;
    private readonly InteractionRepository interactionRepo;
    private readonly SupervisorRepository supervisorRepo;
    private readonly Supervisor supervisor;

    public SupervisorMenu(User user, string connectionString)
    {
        _connectionString = connectionString;
        supervisorRepo = new SupervisorRepository(connectionString);
        interactionRepo = new InteractionRepository(connectionString);
        supervisor = supervisorRepo.GetSupervisorByEmail(user.email);
    }

    public void ShowSupervisorMenu()
    {
        var supervisorService = new SupervisorService(_connectionString, supervisor);
        var functionService = new SupervisorFunctionService(supervisorRepo, interactionRepo);

        ShowReminders(functionService);

        bool exit = false;
        while (!exit)
        {
            Console.Clear();
            Console.WriteLine($"Welcome, {supervisor.first_name} {supervisor.last_name}!");
            Console.WriteLine("=========== Supervisor Menu ===========\n");

            var menuItems = new List<string>
            {
                "View all your students",
                "View details of a specific student",
                "View inactive students",
                "Book a meeting with a student",
                "Manage your meetings",
                "Update office hours",
                "View your performance metrics",
                "Logout"
            };

            int choice = ConsoleHelper.PromptForChoice(menuItems, "Select an option:");

            try
            {
                switch (choice)
                {
                    case 1:
                        supervisorService.ViewAllStudents();
                        break;
                    case 2:
                        supervisorService.ViewStudentDetails();
                        break;
                    case 3:
                        supervisorService.ViewInactiveStudents();
                        break;
                    case 4:
                        supervisorService.BookMeeting();
                        break;
                    case 5:
                        supervisorService.ViewMeetings();
                        break;
                    case 6:
                        supervisorService.UpdateOfficeHours();
                        break;
                    case 7:
                        supervisorService.ViewPerformanceMetrics();
                        break;
                    case 8:
                        Console.WriteLine("\nLogging out...");
                        Thread.Sleep(1000);
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("\nInvalid choice, please try again.");
                        Thread.Sleep(1000);
                        break;
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.PrintSection("Error", ex.Message, "Red");
            }

            if (!exit)
                ConsoleHelper.Pause();
        }
    }

    private void ShowReminders(SupervisorFunctionService functionService)
    {
        bool updateOfficeHours = functionService.NeedsOfficeHourUpdate(supervisor.supervisor_id);
        bool updateWellbeingCheck = functionService.NeedsWellbeingCheckUpdate(supervisor.supervisor_id);

        var reminders = new List<string>();
        if (updateOfficeHours) reminders.Add("your office hours");
        if (updateWellbeingCheck) reminders.Add("your wellbeing check");

        if (reminders.Count > 0)
        {
            Console.Clear();
            string joined = string.Join(" and ", reminders);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n{supervisor.first_name}, it has been more than a week since your last {joined}.");
            Console.WriteLine("Please update them soon.\n");
            Thread.Sleep(5000);
            Console.ResetColor();
        }
    }
}
