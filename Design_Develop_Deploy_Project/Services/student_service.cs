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

        Console.WriteLine("\nWellbeing status updated successfully!");
        Console.WriteLine("Returning to menu...");
        Thread.Sleep(1500);
    }

    public void BookMeeting()
    {
        Console.Clear();
        Console.WriteLine("=========== Book a Meeting ===========");
        Console.WriteLine($"Student: {student.first_name} {student.last_name}");
        Console.WriteLine($"Supervisor: {_supervisorRepo.GetSupervisorById(student.supervisor_id).first_name} {_supervisorRepo.GetSupervisorById(student.supervisor_id).last_name}");
        Console.WriteLine("======================================\n");

        int supervisorId = student.supervisor_id;
        var supervisor = _supervisorRepo.GetSupervisorById(supervisorId);

        var availableSlots = new List<(DateTime start, DateTime end)>();

        for (int i = 0; i < 14; i++)
        {
            DateTime date = DateTime.Today.AddDays(i);
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                continue;

            var slotsForThatDay = _meetingRepo.FetchAvailableSlots(supervisorId, date);
            availableSlots.AddRange(slotsForThatDay);
        }

        availableSlots = availableSlots.OrderBy(s => s.start).ToList();

        if (availableSlots.Count == 0)
        {
            Console.WriteLine("No available meeting slots with your supervisor in the next two weeks.");
            Console.WriteLine("Returning to menu...");
            Thread.Sleep(1500);
            return;
        }

        var slotStrings = new List<string>();
        foreach (var slot in availableSlots)
        {
            slotStrings.Add($"{slot.start:ddd dd MMM HH:mm} – {slot.end:HH:mm}");
        }

        int choice = ConsoleHelper.PromptForChoice(slotStrings, "Available Meeting Slots (Next 2 Weeks)");
        var selectedSlot = availableSlots[choice - 1];

        var existingMeeting = _meetingRepo.GetMeetingByVariable(
            student_id: student.student_id,
            meeting_date: selectedSlot.start.Date
        );

        if (existingMeeting != null &&
            ((selectedSlot.start.TimeOfDay < existingMeeting.end_time) &&
             (selectedSlot.end.TimeOfDay > existingMeeting.start_time)))
        {
            Console.WriteLine($"\nYou already have a meeting booked on {selectedSlot.start:dddd dd MMM} " +
                              $"from {existingMeeting.start_time:hh\\:mm} to {existingMeeting.end_time:hh\\:mm}.");
            Console.WriteLine("Please choose another slot.");
            Thread.Sleep(1500);
            return;
        }

        string note = ConsoleHelper.AskForInput("Add a note or reason for meeting (optional)");

        Console.Clear();
        Console.WriteLine("========== Meeting Confirmation ==========");
        Console.WriteLine($"Date: {selectedSlot.start:dddd dd MMM}");
        Console.WriteLine($"Time: {selectedSlot.start:HH:mm} – {selectedSlot.end:HH:mm}");
        Console.WriteLine($"Supervisor: {supervisor.first_name} {supervisor.last_name}");
        Console.WriteLine("==========================================\n");

        bool confirm = ConsoleHelper.GetYesOrNo("Do you want to confirm this booking?");
        if (!confirm)
        {
            Console.WriteLine("\nMeeting booking was cancelled.");
            Console.WriteLine("Returning to menu...");
            Thread.Sleep(1500);
            return;
        }

        var meeting = new Meeting
        {
            student_id = student.student_id,
            supervisor_id = supervisorId,
            meeting_date = selectedSlot.start.Date,
            start_time = selectedSlot.start.TimeOfDay,
            end_time = selectedSlot.end.TimeOfDay,
            notes = note
        };

        bool success = _meetingRepo.AddMeeting(meeting);

        if (success)
        {
            Console.WriteLine($"\nMeeting booked for {selectedSlot.start:dddd dd MMM HH:mm} – {selectedSlot.end:HH:mm}");
        }
        else
        {
            Console.WriteLine("\nCould not book the meeting. It may have been taken or an error occurred.");
        }

        Console.WriteLine("Returning to menu...");
        Thread.Sleep(1500);
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