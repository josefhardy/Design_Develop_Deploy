
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
            Thread.Sleep(1500);
            return;
        }

        if (new_score_int < 0 || new_score_int > 10)
        {
            Console.WriteLine("\nInvalid score — please enter a number between 0 and 10.");
            Thread.Sleep(1500);
            return;
        }

        _statusRepo.UpdateStudentWellbeing(student.student_id, new_score_int, student);
        student.wellbeing_score = new_score_int;

        Console.WriteLine("\nWellbeing status updated successfully!");
        Thread.Sleep(1500);
    }

    public void BookMeeting()
    {
        Console.Clear();
        var scheduler = new MeetingScheduler(_meetingRepo, _supervisorRepo);

        Supervisor supervisor = _supervisorRepo.GetSupervisorById(student.supervisor_id);

        // 🔹 Get available slots via the new scheduler
        var availableSlots = new List<(DateTime start, DateTime end)>();
        for (int i = 0; i < 14; i++)
        {
            DateTime date = DateTime.Today.AddDays(i);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            var slotsForDay = scheduler.FetchAvailableSlots(supervisor.supervisor_id, date);
            availableSlots.AddRange(slotsForDay);
        }

        if (availableSlots.Count == 0)
        {
            ConsoleHelper.PrintSection("No Available Slots", "No open slots in the next 2 weeks.");
            return;
        }

        var slotOptions = availableSlots
            .Select(s => $"{s.start:ddd dd MMM HH:mm} – {s.end:HH:mm}")
            .ToList();

        int slotChoice = ConsoleHelper.PromptForChoice(slotOptions, "Choose a meeting slot:");
        var chosenSlot = availableSlots[slotChoice - 1];

        string notes = ConsoleHelper.AskForInput("Add meeting notes (optional)");

        var meeting = new Meeting
        {
            student_id = student.student_id,
            supervisor_id = supervisor.supervisor_id,
            meeting_date = chosenSlot.start.Date,
            start_time = chosenSlot.start.TimeOfDay,
            end_time = chosenSlot.end.TimeOfDay,
            notes = notes
        };

        // 🔹 Validate meeting before saving
        if (!scheduler.ValidateMeeting(meeting, out string message))
        {
            ConsoleHelper.PrintSection("Invalid Meeting", message);
            ConsoleHelper.Pause();
            return;
        }

        bool success = _meetingRepo.AddMeeting(meeting);
        if (success)
        {
            ConsoleHelper.PrintSection("Success", "Meeting booked successfully!");
        }
        else
        {
            ConsoleHelper.PrintSection("Error", "Failed to book the meeting.");
        }
    }

    public void ViewMeetings()
    {
        Console.Clear();
        Console.WriteLine("=========== View Meeting ===========");
        Console.WriteLine($"Student: {student.first_name} {student.last_name}");
        Console.WriteLine("=======================================\n");

        var meeting = _meetingRepo.GetMeetingByVariable(student_id: student.student_id);

        if (meeting == null)
        {
            Console.WriteLine("You currently have no meeting booked.\n");
            bool bookNow = ConsoleHelper.GetYesOrNo("Would you like to book a meeting now?");
            if (bookNow)
            {
                BookMeeting();
            }
            else
            {
                Console.WriteLine("\nReturning to menu...");
                Thread.Sleep(1500);
            }
            return;
        }

        var supervisor = _supervisorRepo.GetSupervisorById(meeting.supervisor_id);

        Console.WriteLine("=========== Current Meeting ===========");
        Console.WriteLine($"Date: {meeting.meeting_date:dddd dd MMM}");
        Console.WriteLine($"Time: {meeting.start_time:hh\\:mm} – {meeting.end_time:hh\\:mm}");
        Console.WriteLine($"Supervisor: {supervisor.first_name} {supervisor.last_name}");
        Console.WriteLine($"Notes: {(string.IsNullOrWhiteSpace(meeting.notes) ? "None" : meeting.notes)}");
        Console.WriteLine("=======================================\n");

        int choice = ConsoleHelper.PromptForChoice(
            new List<string> { "Reschedule meeting", "Cancel meeting", "Return to menu" },
            "What would you like to do?"
        );

        if (choice == 1)
        {
            Console.Clear();
            Console.WriteLine("=========== Reschedule Meeting ===========");
            Console.WriteLine($"Your current meeting is on {meeting.meeting_date:dddd dd MMM} " +
                              $"from {meeting.start_time:hh\\:mm} to {meeting.end_time:hh\\:mm}.");
            Console.WriteLine("==========================================\n");

            bool confirmReschedule = ConsoleHelper.GetYesOrNo("Would you like to find a new time?");
            if (confirmReschedule)
            {
                Console.WriteLine("\nLet's reschedule your meeting.");
                BookMeeting();

                // Delete the old meeting *after* successful booking
                _meetingRepo.DeleteMeeting(meeting.meeting_id);
            }
            else
            {
                Console.WriteLine("\nReschedule cancelled. Returning to menu...");
                Thread.Sleep(1500);
            }
        }
        else if (choice == 2)
        {
            bool confirm = ConsoleHelper.GetYesOrNo("Are you sure you want to cancel this meeting?");
            if (confirm)
            {
                _meetingRepo.DeleteMeeting(meeting.meeting_id);
                Console.WriteLine("\nMeeting cancelled successfully.");
            }
            else
            {
                Console.WriteLine("\nMeeting cancellation aborted.");
            }

            Console.WriteLine("Returning to menu...");
            Thread.Sleep(1500);
        }
        else
        {
            Console.WriteLine("\nReturning to menu...");
            Thread.Sleep(1500);
        }
    }

}