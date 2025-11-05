using System;                               
using Design_Develop_Deploy_Project.Repos;
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Utilities;
using Design_Develop_Deploy_Project.Services;
using System.Runtime.CompilerServices;

namespace Design_Develop_Deploy_Project.UI;

public class SupervisorMenu
{
    private readonly string _connectionString;

    private readonly SupervisorRepository supervisorRepo;
	private readonly UserRepository userRepo;
	private readonly Supervisor supervisor;
	private readonly MeetingRepository meetingRepo;

	public SupervisorMenu(User user, string connectionstring)
	{
        _connectionString = connectionstring;
        supervisorRepo = new SupervisorRepository(connectionstring);
		userRepo = new UserRepository(connectionstring);
		meetingRepo = new MeetingRepository(connectionstring);
		supervisor = supervisorRepo.GetSupervisorByEmail(user.email);
	}

    public void ShowSupervisorMenu()
    {
        var supervisorService = new SupervisorService(_connectionString, supervisor);

        // 🔹 Reminder checks using repository helper functions
        bool updateOfficeHours = supervisorRepo.NeedsOfficeHourUpdate(supervisor.supervisor_id);
        bool updateWellbeingCheck = supervisorRepo.NeedsWellbeingCheckUpdate(supervisor.supervisor_id);

        var reminders = new List<string>();
        if (updateOfficeHours) reminders.Add("your office hours");
        if (updateWellbeingCheck) reminders.Add("your wellbeing check");

        // 🔹 Display reminders if needed
        if (reminders.Count > 0)
        {
            string joined = string.Join(" and ", reminders);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n⚠️  {supervisor.first_name}, it has been more than a week since your last {joined}.");
            Console.WriteLine("Please update them soon.\n");
            Console.ResetColor();
        }

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
            "View your meetings",
            "Update office hours",
            "View your performance metrics",
            "Logout"
        };

            int choice = ConsoleHelper.PromptForChoice(menuItems, "Select an option:");

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

            if (!exit)
                ConsoleHelper.Pause();
        }
    }

}
