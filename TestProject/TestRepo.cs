using System;
using System.Collections.Generic;
using Design_Develop_Deploy_Project.Objects;

namespace Tests
{
    public static class TestRepos
    {
        // -------------------------------
        // Test Users
        // -------------------------------
        public static List<User> TestUsers = new List<User>
        {
            new User { email = "student@example.com", password = "pass123", role = "Student" },
            new User { email = "supervisor@example.com", password = "superpass", role = "Supervisor" }
        };

        // -------------------------------
        // Test Supervisors
        // -------------------------------
        public static List<Supervisor> TestSupervisors = new List<Supervisor>
        {
            new Supervisor
            {
                supervisor_id = 1,
                user_id = 101,
                first_name = "Alice",
                last_name = "Smith",
                email = "alice@example.com",
                role = "Supervisor",
                office_hours = "Monday 09:00-11:00, Tuesday 14:00-16:00",
                meetings_booked_this_month = 0,
                wellbeing_checks_this_month = 0
            },
            new Supervisor
            {
                supervisor_id = 2,
                user_id = 102,
                first_name = "Bob",
                last_name = "Jones",
                email = "bob@example.com",
                role = "Supervisor",
                office_hours = "Wednesday 10:00-12:00",
                meetings_booked_this_month = 0,
                wellbeing_checks_this_month = 0
            }
        };

        // -------------------------------
        // Test Students
        // -------------------------------
        public static List<Student> TestStudents = new List<Student>
        {
            new Student
            {
                student_id = 201,
                supervisor_id = 1,
                first_name = "John",
                last_name = "Doe",
                email = "student@example.com",
                password = "pass123",
                role = "Student",
                wellbeing_score = 5,
                last_status_update = DateTime.Today.AddDays(-3)
            },
            new Student
            {
                student_id = 202,
                supervisor_id = 2,
                first_name = "Jane",
                last_name = "Smith",
                email = "jane@example.com",
                password = "pass456",
                role = "Student",
                wellbeing_score = 7,
                last_status_update = DateTime.Today.AddDays(-1)
            }
        };

        // -------------------------------
        // Test Meetings
        // -------------------------------
        public static List<Meeting> TestMeetings = new List<Meeting>
        {
            new Meeting
            {
                meeting_id = 1,
                student_id = 201,
                supervisor_id = 1,
                meeting_date = NextWeekday(DateTime.Today, DayOfWeek.Monday),
                start_time = new TimeSpan(9, 30, 0),
                end_time = new TimeSpan(10, 0, 0),
                notes = "Project discussion",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            },
            new Meeting
            {
                meeting_id = 2,
                student_id = 202,
                supervisor_id = 2,
                meeting_date = NextWeekday(DateTime.Today, DayOfWeek.Wednesday),
                start_time = new TimeSpan(10, 0, 0),
                end_time = new TimeSpan(10, 30, 0),
                notes = "Status update",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            }
        };

        // -------------------------------
        // Helper: find the next specific weekday
        // -------------------------------
        public static DateTime NextWeekday(DateTime start, DayOfWeek day)
        {
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            if (daysToAdd == 0) daysToAdd = 7; // always move to future
            return start.AddDays(daysToAdd).Date;
        }
    }
}
