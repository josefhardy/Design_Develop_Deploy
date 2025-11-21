
using Design_Develop_Deploy_Project.Services;
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Repos;
using Design_Develop_Deploy_Project.Utilities;
using System;

namespace Design_Develop_Deploy_Project.Services;
public class SupervisorService
{
    private readonly StudentRepository _studentRepo;
    private readonly StatusRepository _statusRepo;
    private readonly MeetingRepository _meetingRepo;
    private readonly SupervisorRepository _supervisorRepo;
    private readonly InteractionRepository _interactionRepo;
    private readonly Supervisor supervisor;
    public SupervisorService(string connectionString, Supervisor _supervisor)
	{
        _interactionRepo = new InteractionRepository(connectionString);
        _studentRepo = new StudentRepository(connectionString);
        _statusRepo = new StatusRepository(connectionString);
        _meetingRepo = new MeetingRepository(connectionString);
        _supervisorRepo = new SupervisorRepository(connectionString);
        supervisor = _supervisor;
    }

    public void ViewAllStudents()
    {
        Console.Clear();
        Console.WriteLine("=========== All Students ===========");
        Console.WriteLine($"Supervisor: {supervisor.first_name} {supervisor.last_name}");
        Console.WriteLine("====================================\n");


        var students = _studentRepo.GetAllStudentsUnderSpecificSupervisor(supervisor.supervisor_id)
            .OrderBy(s => s.wellbeing_score)
            .ToList();

        foreach (var s in students)
        {
            Console.WriteLine($"ID: {s.student_id}, Name: {s.first_name} {s.last_name}, Wellbeing: {s.wellbeing_score}/10");
        }

        ConsoleHelper.Pause("Press any key to return to the main menu...");
    }

    public void ViewStudentDetails()
    {
        Console.Clear();
        Console.WriteLine("=========== Student Details ===========");
        Console.Write("Enter Student ID: ");

        if (!int.TryParse(Console.ReadLine(), out int studentId))
        {
            Console.WriteLine("Invalid Student ID. Returning to menu...");
            Thread.Sleep(1500);
            return;
        }

        var student = _studentRepo.GetStudentById(studentId);
        if (student == null)
        {
            Console.WriteLine("Student not found. Returning to menu...");
            Thread.Sleep(1500);
            return;
        }

        Console.WriteLine($"\nName: {student.first_name} {student.last_name}");
        Console.WriteLine($"Wellbeing Score: {student.wellbeing_score}/10");
        Console.WriteLine($"Last Status Update: {(student.last_status_update.HasValue ? student.last_status_update.Value.ToString("dd MMM yyyy") : "N/A")}");
        Console.WriteLine("======================================");

        // ✅ NEW: Record that the supervisor viewed this student’s wellbeing
        _interactionRepo.RecordSupervisorInteraction(supervisor.supervisor_id, student.student_id, "wellbeing_check");

        bool answer = ConsoleHelper.GetYesOrNo("Would you like to book a meeting with this student?");
        if (answer)
        {
            BookMeeting(student);
        }
        else
        {
            Console.WriteLine("\nReturning to the main menu...");
            Thread.Sleep(1500);
        }
    }

    public void BookMeeting(Student student = null)
    {
        Console.Clear();

        // ✅ new: create scheduler service to handle logic
        var scheduler = new MeetingScheduler(_meetingRepo, _supervisorRepo);

        // -----------------------------
        // Step 1 – Choose student
        // -----------------------------
        if (student == null)
        {
            var students = _studentRepo.GetAllStudentsUnderSpecificSupervisor(supervisor.supervisor_id);
            List<string> studentDetails = students
                .Select(s => $"ID: {s.student_id} | Name: {s.first_name} {s.last_name}")
                .ToList();

            int choice = ConsoleHelper.PromptForChoice(studentDetails, "Select a student to book a meeting with:");
            student = _studentRepo.GetStudentById(students[choice - 1].student_id);
        }

        Console.WriteLine($"\nBooking meeting with {student.first_name} {student.last_name} (ID: {student.student_id})");
        Console.WriteLine("======================================");

        // -----------------------------
        // Step 2 – Get available slots
        // -----------------------------
        var availableSlots = new List<(DateTime start, DateTime end)>();

        for (int i = 0; i < 14; i++)
        {
            DateTime date = DateTime.Today.AddDays(i);
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                continue;

            // ✅ new: use scheduler to fetch available slots
            var slotsForThatDay = scheduler.FetchAvailableSlots(supervisor.supervisor_id, date);
            availableSlots.AddRange(slotsForThatDay);
        }

        availableSlots = availableSlots.OrderBy(s => s.start).ToList();

        if (availableSlots.Count == 0)
        {
            ConsoleHelper.PrintSection("No Available Slots", "No available meeting slots in the next 2 weeks.");
            ConsoleHelper.Pause();
            return;
        }

        // -----------------------------
        // Step 3 – Let supervisor choose a slot
        // -----------------------------
        var slotStrings = availableSlots
            .Select(s => $"{s.start:ddd dd MMM HH:mm} – {s.end:HH:mm}")
            .ToList();

        int slotChoice = ConsoleHelper.PromptForChoice(slotStrings, "Available Meeting Slots:");
        var selectedSlot = availableSlots[slotChoice - 1];

        string notes = ConsoleHelper.AskForInput("Add meeting notes (optional)");

        Console.Clear();
        ConsoleHelper.PrintSection("Meeting Confirmation",
            $"Date: {selectedSlot.start:dddd dd MMM}\n" +
            $"Time: {selectedSlot.start:HH:mm} – {selectedSlot.end:HH:mm}\n" +
            $"Student: {student.first_name} {student.last_name}\n" +
            $"Supervisor: {supervisor.first_name} {supervisor.last_name}");

        bool confirm = ConsoleHelper.GetYesOrNo("Confirm booking this meeting?");
        if (!confirm)
        {
            ConsoleHelper.PrintSection("Cancelled", "Meeting booking was cancelled.");
            ConsoleHelper.Pause();
            return;
        }

        // -----------------------------
        // Step 4 – Create meeting object
        // -----------------------------
        var meeting = new Meeting
        {
            student_id = student.student_id,
            supervisor_id = supervisor.supervisor_id,
            meeting_date = selectedSlot.start.Date,
            start_time = selectedSlot.start.TimeOfDay,
            end_time = selectedSlot.end.TimeOfDay,
            notes = notes
        };

        // -----------------------------
        // Step 5 – Validate before saving
        // -----------------------------
        if (!scheduler.ValidateMeeting(meeting, out string validationMessage))
        {
            ConsoleHelper.PrintSection("❌ Invalid Meeting", validationMessage);
            ConsoleHelper.Pause();
            return;
        }

        // -----------------------------
        // Step 6 – Save meeting
        // -----------------------------
        bool success = _meetingRepo.AddMeeting(meeting);

        if (success)
        {
            // Log interaction for metrics
            _interactionRepo.RecordSupervisorInteraction(supervisor.supervisor_id, student.student_id, "meeting");

            ConsoleHelper.PrintSection("✅ Success",
                $"Meeting booked for {selectedSlot.start:dddd dd MMM HH:mm} – {selectedSlot.end:HH:mm}");
        }
        else
        {
            ConsoleHelper.PrintSection("❌ Error",
                "Could not book the meeting. It may have been taken or an error occurred.");
        }

        ConsoleHelper.Pause();
    }
          
    public void ViewMeetings()
    {
        Console.Clear();
        Console.WriteLine("=========== My Meetings ===========");
        Console.WriteLine($"Supervisor: {supervisor.first_name} {supervisor.last_name}");
        Console.WriteLine("===================================\n");

        var meetings = _meetingRepo.GetMeetingsBySupervisorId(supervisor.supervisor_id);

        if (meetings == null || meetings.Count == 0)
        {
            Console.WriteLine("You have no upcoming meetings.");
            Console.WriteLine("Returning to menu...");
            Thread.Sleep(1500);
            return;
        }

        List<string> meetings_details = meetings
            .Select(m => {
                var student = _studentRepo.GetStudentById(m.student_id);
                string studentName = student != null ? $"{student.first_name} {student.last_name}" : "Unknown";
                return $"Date: {m.meeting_date:ddd dd MMM} {m.start_time:hh\\:mm} – {m.end_time:hh\\:mm} | Student: {studentName} | Notes: {(string.IsNullOrWhiteSpace(m.notes) ? "None" : m.notes)}";
            }).ToList();

        for (int i = 0; i < meetings_details.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {meetings_details[i]}");
        }

        Console.WriteLine("\n===================================");

        int meeting_choice = ConsoleHelper.PromptForChoice(meetings_details, "Select a meeting to manage:");
        var selected_meeting = meetings[meeting_choice - 1];
        Console.Clear();

        var manageOptions = new List<string>
    {
        "Cancel Meeting",
        "Reschedule Meeting",
        "Return to Main Menu"
    };

        int manage_choice = ConsoleHelper.PromptForChoice(manageOptions, "Manage Meeting:");

        switch (manage_choice)
        {
            case 1:
                bool confirmCancel = ConsoleHelper.GetYesOrNo("Are you sure you want to cancel this meeting?");
                if (confirmCancel)
                {
                    _meetingRepo.DeleteMeeting(selected_meeting.meeting_id);
                    Console.WriteLine("Meeting cancelled successfully.");
                }
                else
                {
                    Console.WriteLine("Meeting cancellation aborted.");
                }
                ConsoleHelper.Pause();
                break;

            case 2:
                Console.Clear();
                Console.WriteLine("=========== Reschedule Meeting ===========");
                Console.WriteLine($"Your current meeting is on {selected_meeting.meeting_date:dddd dd MMM} " +
                                  $"from {selected_meeting.start_time:hh\\:mm} to {selected_meeting.end_time:hh\\:mm}.");
                Console.WriteLine("==========================================\n");

                bool confirmReschedule = ConsoleHelper.GetYesOrNo("Would you like to find a new time?");
                if (confirmReschedule)
                {
                    Console.WriteLine("\nLet's reschedule your meeting.");
                    BookMeeting();

                    // Delete the old meeting after successful booking
                    _meetingRepo.DeleteMeeting(selected_meeting.meeting_id);
                }
                else
                {
                    Console.WriteLine("\nReschedule cancelled. Returning to menu...");
                }
                ConsoleHelper.Pause();
                break;

            case 3:
                Console.WriteLine("Returning to main menu...");
                Thread.Sleep(1500);
                return;
        }

        Console.WriteLine("Returning to menu...");
        Thread.Sleep(1500);
    }

    public void UpdateOfficeHours()
    {
        Console.Clear();
        Console.WriteLine("=========== Update Office Hours ===========");
        Console.WriteLine($"Supervisor: {supervisor.first_name} {supervisor.last_name}");
        Console.WriteLine("===========================================\n");

        Console.WriteLine($"Current office hours: {supervisor.office_hours}");
        bool update = ConsoleHelper.GetYesOrNo("Would you like to update your office hours? Choose no to continue with the current hours");
        if (!update)
        {
            Console.WriteLine("Returning to menu...");
            Thread.Sleep(1500);
            return;
        }

        Console.Clear();
        Console.WriteLine("You need set up 2 office-hour sessions per week, each lasting exactly 2 hours.");
        Console.WriteLine("Please enter each one in this format: Monday 09:00-11:00");
        Console.WriteLine("Office hours must be within 08:00–18:00 and on weekdays (Monday–Friday).\n");

        var newOfficeHours = new List<string>();
        var chosenDays = new HashSet<string>();

        for (int i = 0; i < 2; i++)
        {
            bool valid = false;
            string input = "";

            while (!valid)
            {
                input = ConsoleHelper.AskForInput($"Enter office hour #{i + 1}:").Trim();

                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    Console.WriteLine("Invalid format. Use: Day HH:mm-HH:mm (e.g., Monday 09:00-11:00)");
                    continue;
                }

                string day = parts[0].Trim();
                string[] validDays = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };

                if (!validDays.Contains(day, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Invalid day. Please choose a weekday between Monday and Friday.");
                    continue;
                }

                if (chosenDays.Contains(day.ToLower()))
                {
                    Console.WriteLine($"You’ve already entered {day}. Please choose a different day.");
                    continue;
                }

                var times = parts[1].Split('-', StringSplitOptions.RemoveEmptyEntries);
                if (times.Length != 2)
                {
                    Console.WriteLine("Invalid time format. Use HH:mm-HH:mm (e.g., 09:00-11:00).");
                    continue;
                }

                if (!TimeSpan.TryParse(times[0], out TimeSpan start) || !TimeSpan.TryParse(times[1], out TimeSpan end))
                {
                    Console.WriteLine("Time must be in 24-hour format (e.g., 09:00 or 14:00).");
                    continue;
                }

                if (start >= end)
                {
                    Console.WriteLine("Start time must be before end time.");
                    continue;
                }

                if (end - start != TimeSpan.FromHours(2))
                {
                    Console.WriteLine("Each session must be exactly 2 hours long.");
                    continue;
                }

                if (start < new TimeSpan(8, 0, 0) || end > new TimeSpan(18, 0, 0))
                {
                    Console.WriteLine("Office hours must be between 08:00 and 18:00.");
                    continue;
                }

                chosenDays.Add(day.ToLower());
                newOfficeHours.Add($"{day} {start:hh\\:mm}-{end:hh\\:mm}");
                valid = true;
            }
        }

        Console.Clear();
        Console.WriteLine("=========== New Office Hours ===========");
        foreach (var slot in newOfficeHours)
            Console.WriteLine(slot);
        Console.WriteLine("========================================\n");

        bool confirm = ConsoleHelper.GetYesOrNo("Save these office hours?");
        if (confirm)
        {
            string formattedHours = string.Join(",", newOfficeHours);
            _supervisorRepo.UpdateOfficeHours(supervisor.supervisor_id, formattedHours);
            Console.WriteLine("\n✅ Office hours updated successfully!");
        }
        else
        {
            Console.WriteLine("\nChanges discarded.");
        }

        ConsoleHelper.Pause("Press any key to return to the main menu...");
    }

    public void ViewPerformanceMetrics()
    {
        Console.Clear();

        // ✅ Use InteractionRepository instead of SupervisorRepo
        var (meetingsBooked, wellbeingChecks) = _interactionRepo.GetSupervisorActivity(supervisor.supervisor_id);

        Console.WriteLine("=========== Performance Metrics ===========");
        Console.WriteLine($"Supervisor: {supervisor.first_name} {supervisor.last_name}");
        Console.WriteLine("==========================================\n");
        Console.WriteLine($"Meetings Booked This Month: {meetingsBooked}");
        Console.WriteLine($"Wellbeing Checks Conducted This Month: {wellbeingChecks}");
        Console.WriteLine("==========================================");

        ConsoleHelper.Pause("Press any key to return to the main menu...");
    }
    public void ViewInactiveStudents()
    {
        Console.Clear();
        Console.WriteLine("=========== Inactive Students ===========\n");

        var students = _studentRepo.GetAllStudentsUnderSpecificSupervisor(supervisor.supervisor_id);
        if (students == null || students.Count == 0)
        {
            Console.WriteLine("No students found under your supervision.");
            ConsoleHelper.Pause();
            return;
        }

        // Define inactivity threshold
        DateTime thresholdDate = DateTime.Today.AddDays(-14);

        var inactiveStudents = students
            .Where(s => !s.last_status_update.HasValue || s.last_status_update.Value < thresholdDate)
            .OrderBy(s => s.last_status_update ?? DateTime.MinValue)
            .ToList();

        if (inactiveStudents.Count == 0)
        {
            Console.WriteLine("✅ All your students have updated their wellbeing recently!");
            ConsoleHelper.Pause();
            return;
        }

        // Display inactive students
        foreach (var student in inactiveStudents)
        {
            string lastUpdate = student.last_status_update.HasValue
                ? student.last_status_update.Value.ToString("dd MMM yyyy")
                : "No record";

            Console.WriteLine($"ID: {student.student_id}");
            Console.WriteLine($"Name: {student.first_name} {student.last_name}");
            Console.WriteLine($"Wellbeing: {student.wellbeing_score}/10");
            Console.WriteLine($"Last Update: {lastUpdate}");
            Console.WriteLine("-----------------------------------------");
        }

        Console.WriteLine($"Total inactive students: {inactiveStudents.Count}");
        Console.WriteLine();

        // Optional: prompt to take action
        bool book = ConsoleHelper.GetYesOrNo("Would you like to book a meeting with one of these students?");
        if (book)
        {
            int choice = ConsoleHelper.PromptForChoice(
                inactiveStudents.Select(s => $"{s.first_name} {s.last_name} (Last update: {(s.last_status_update.HasValue ? s.last_status_update.Value.ToString("dd MMM") : "No record")})").ToList(),
                "Select a student to book a meeting with:");

            var selectedStudent = inactiveStudents[choice - 1];
            BookMeeting(selectedStudent);
        }

        ConsoleHelper.Pause("Press any key to return to the menu...");
    }

}
