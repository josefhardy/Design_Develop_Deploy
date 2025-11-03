using Design.Develop.Deploy.Repos;
using Design_Develop_Deploy_Project.Repos;
using System;

namespace Design_Develop_Deploy_Project.Services;
public class SupervisorService
{
    private readonly StudentRepository _studentRepo;
    private readonly StatusRepository _statusRepo;
    private readonly MeetingRepository _meetingRepo;
    private readonly SupervisorRepository _supervisorRepo;
    private readonly Supervisor supervisor;
    public SupervisorService(string connectionString, Supervisor _supervisor)
	{
        _studentRepo = new StudentRepository(connectionString);
        _statusRepo = new StatusRepository(connectionString);
        _meetingRepo = new MeetingRepository(connectionString);
        _supervisorRepo = new SupervisorRepository(connectionString);
        supervisor = _supervisor;
    }

    public void ViewAllStudents()
    {
        Console.Clear();
        Console.WriteLine("=========== My Students ===========");
        Console.WriteLine($"Supervisor: {supervisor.first_name} {supervisor.last_name}");
        Console.WriteLine("===================================\n");

        var students = _studentRepo.GetAllStudentsUnderSpecificSupervisor(supervisor.supervisor_id);

        if (students == null || students.Count == 0)
        {
            Console.WriteLine("You currently have no assigned students.");
            Console.WriteLine("\nReturning to menu...");
            Thread.Sleep(1500);
            return;
        }

        // Sort students by wellbeing score (lowest first)
        students = students.OrderBy(s => s.wellbeing_score).ToList();

        Console.WriteLine("Students (sorted by wellbeing score):\n");
        foreach (var student in students)
        {
            Console.WriteLine($"ID: {student.student_id} | Name: {student.first_name} {student.last_name} | Wellbeing: {student.wellbeing_score}/10");
        }

        Console.WriteLine("\n===================================");
        Console.WriteLine("Press any key to return to the main menu...");
        Console.ReadKey();
    }


    public void ViewStudentDetails()
    {
        
    }

    public void BookMeeting() 
    {
        
    }
    
    public void ViewMeetings() 
    {

    }

    public void UpdateOfficeHours()
    {
        
    }

    public void ViewPerformanceMetrics()
    {
        
    }
}
