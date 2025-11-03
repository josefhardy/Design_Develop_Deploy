using Design.Develop.Deploy.Repos;
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Repos;
using System;
using System.Collections.Generic;
using Design_Develop_Deploy_Project.Utilities;
using System.Data.SQLite;

namespace Design_Develop_Deploy_Project.Services;
public class StudentService
{
    private readonly StudentRepository _studentRepo;
    private readonly StatusRepository _statusRepo;
    private readonly MeetingRepository _meetingRepo;
    private readonly SupervisorRepository _supervisorRepo;
    private readonly Student student;

    public StudentService(string connectionString, Student _student)
    {
        student = _student;
        _studentRepo = new StudentRepository(connectionString);
        _statusRepo = new StatusRepository(connectionString);
        _meetingRepo = new MeetingRepository(connectionString);
        _supervisorRepo = new SupervisorRepository(connectionString);
    }

    public void ViewStatus()
    {
        Console.Clear();
        Console.WriteLine("======= Student Wellbeing Status =======");
        Console.WriteLine($"Student: {student.first_name} {student.last_name}");
        Console.WriteLine($"Current wellbeing score: {student.wellbeing_score}/10");
        Console.WriteLine($"Last updated: {(student.last_status_update?.ToString("dd MMM yyyy") ?? "No record")}");
        Console.WriteLine("========================================\n");

        bool choice = ConsoleHelper.GetYesOrNo("Would you like to update your wellbeing score now?");

        if (choice)
        {
            UpdateStatus();
        }
        else
        {
            Console.WriteLine("\nReturning to the main menu...");
            Thread.Sleep(1500);
        }
    }


    public void UpdateStatus()
    {
        Console.Clear();
        Console.WriteLine("======= Update Wellbeing Status =======");
        Console.WriteLine($"Student: {student.first_name} {student.last_name}");
        Console.WriteLine($"Current wellbeing score: {student.wellbeing_score}/10");
        Console.WriteLine("=======================================\n");

        string new_score = ConsoleHelper.AskForInput("Please enter your new wellbeing score (0–10)");

        int new_score_int;
        try
        {
            new_score_int = int.Parse(new_score);
        }
        catch (FormatException)
        {
            Console.WriteLine("\nInvalid input — score must be a number between 0 and 10.");
            Console.WriteLine("Returning to menu...");
            Thread.Sleep(1500);
            return;
        }

        if (new_score_int < 0 || new_score_int > 10)
        {
            Console.WriteLine("\nInvalid score — please enter a number between 0 and 10.");
            Console.WriteLine("Returning to menu...");
            Thread.Sleep(1500);
            return;
        }

        _studentRepo.UpdateStudentWellbeing(student.student_id, new_score_int);

        Console.WriteLine("\n✅ Wellbeing status updated successfully!");
        Console.WriteLine("Returning to menu...");
        Thread.Sleep(1500);
    }


    public void BookMeeting()
    {

    }

    public void ViewMeetings()
    {

    }
}