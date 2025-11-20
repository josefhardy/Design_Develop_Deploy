using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Design_Develop_Deploy_Project.Tables
{
    public static class DatabaseSeeder
    {
        public static void Seed()
        {
            string dbpath = "Project_database.db";
            string connString = $"Data Source={dbpath};Version=3;";

            using (var conn = new SQLiteConnection(connString))
            {
                conn.Open();

                using (var pragmaCommand = new SQLiteCommand("PRAGMA foreign_keys = ON;", conn))
                    pragmaCommand.ExecuteNonQuery();

                // Prevent duplicate seeding
                using (var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM Users;", conn))
                {
                    long count = (long)checkCmd.ExecuteScalar();
                    if (count > 0)
                    {
                        Console.WriteLine("Database already contains data — skipping seeding.");
                        return;
                    }
                }

                Console.WriteLine("🌱 Seeding database with realistic university data...");

                var random = new Random();

                // ---- Helper for unique passwords ----
                static string GeneratePassword(Random random, int length = 10)
                {
                    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                    return new string(Enumerable.Repeat(chars, length)
                        .Select(s => s[random.Next(s.Length)]).ToArray());
                }

                // ---- Data Sources ----
                string[] maleNames = {
            "James", "Liam", "Noah", "Ethan", "Oliver", "Lucas", "Henry", "Mason", "Jack", "William",
            "Alexander", "Benjamin", "Jacob", "Logan", "Elijah", "Daniel", "Matthew", "Aiden", "Samuel", "Owen",
            "Caleb", "Nathan", "Isaac", "Joseph", "Ryan", "Adam", "Andrew", "Aaron", "Connor", "Dylan"
        };

                string[] femaleNames = {
            "Emma", "Olivia", "Ava", "Sophia", "Isabella", "Mia", "Charlotte", "Amelia", "Harper", "Ella",
            "Grace", "Lily", "Abigail", "Emily", "Aria", "Chloe", "Scarlett", "Victoria", "Zoe", "Layla",
            "Hannah", "Nora", "Evelyn", "Lucy", "Sofia", "Leah", "Sarah", "Maya", "Hazel", "Claire"
        };

                string[] lastNames = {
            "Smith", "Johnson", "Brown", "Williams", "Jones", "Taylor", "Davis", "Clark", "Lee", "Harris",
            "Patel", "Nguyen", "Zhang", "Singh", "Wilson", "Anderson", "Thomas", "Moore", "Martin", "Thompson",
            "White", "Lopez", "King", "Scott", "Hill", "Green", "Adams", "Baker", "Wright", "Nelson",
            "Mitchell", "Campbell", "Carter", "Roberts", "Turner", "Phillips", "Evans", "Collins", "Stewart", "Edwards",
            "Morris", "Rogers", "Cook", "Murphy", "Bailey", "Cooper", "Reed", "Howard", "Ward", "Foster"
        };

                string[] officeHours = {
                "Monday 09:00-11:00, Wednesday 13:00-15:00",
                "Tuesday 10:00-12:00, Thursday 14:00-16:00",
                "Wednesday 09:00-11:00, Friday 10:00-12:00",
                "Monday 14:00-16:00, Thursday 09:00-11:00",
                "Tuesday 13:00-15:00, Friday 09:00-11:00"
                };

                // ---- 1. Senior Tutor ----
                long seniorTutorUserId;
                string tutorPass = GeneratePassword(random);
                using (var cmd = new SQLiteCommand(
                    "INSERT INTO Users (first_name, last_name, email, password, role) VALUES (@f, @l, @e, @p, 'senior_tutor'); SELECT last_insert_rowid();", conn))
                {
                    cmd.Parameters.AddWithValue("@f", "Sarah");
                    cmd.Parameters.AddWithValue("@l", "Thompson");
                    cmd.Parameters.AddWithValue("@e", "sarah.thompson@hull.ac.uk");
                    cmd.Parameters.AddWithValue("@p", tutorPass);
                    seniorTutorUserId = (long)cmd.ExecuteScalar();
                }

                Console.WriteLine($"👩‍🏫 Senior Tutor: Prof. Sarah Thompson created (Password: {tutorPass})");

                // ---- 2. Supervisors ----
                var supervisorList = new List<(long supervisorId, string fullName)>();
                var supNames = new (string, string)[] {
            ("Emily", "Zhang"), ("David", "Patel"), ("Rachel", "Hughes"), ("Michael", "Carter"), ("Anna", "Nguyen")
        };

                foreach (var (first, last) in supNames)
                {
                    string email = $"{first.ToLower()}.{last.ToLower()}@hull.ac.uk";
                    string office = officeHours[random.Next(officeHours.Length)];
                    string password = GeneratePassword(random);

                    long userId;
                    using (var userCmd = new SQLiteCommand(
                        "INSERT INTO Users (first_name, last_name, email, password, role) VALUES (@f, @l, @e, @p, 'supervisor'); SELECT last_insert_rowid();", conn))
                    {
                        userCmd.Parameters.AddWithValue("@f", first);
                        userCmd.Parameters.AddWithValue("@l", last);
                        userCmd.Parameters.AddWithValue("@e", email);
                        userCmd.Parameters.AddWithValue("@p", password);
                        userId = (long)userCmd.ExecuteScalar();
                    }

                    long supervisorId;
                    using (var supCmd = new SQLiteCommand(
                        @"INSERT INTO Supervisors (user_id, last_office_hours_update, last_wellbeing_check, office_hours) 
                  VALUES (@u, date('now'), date('now'), @o); 
                  SELECT last_insert_rowid();", conn))
                    {
                        supCmd.Parameters.AddWithValue("@u", userId);
                        supCmd.Parameters.AddWithValue("@o", office);
                        supervisorId = (long)supCmd.ExecuteScalar();
                    }

                    supervisorList.Add((supervisorId, $"{first} {last}"));
                    Console.WriteLine($"🧑‍🏫 Supervisor: Dr. {first} {last}, Office hours: {office}, Password: {password}");
                }

                // ---- 3. Students ----
                var studentList = new List<(long studentId, long supervisorId, string name)>();

                // Create mutable name pools for uniqueness
                var availableMaleNames = new List<string>(maleNames);
                var availableFemaleNames = new List<string>(femaleNames);

                foreach (var (supervisorId, supervisorName) in supervisorList)
                {
                    int numStudents = random.Next(5, 8);
                    for (int i = 0; i < numStudents; i++)
                    {
                        bool isFemale = random.NextDouble() < 0.5;
                        string first;

                        // Choose unique first name
                        if (isFemale && availableFemaleNames.Count > 0)
                        {
                            int index = random.Next(availableFemaleNames.Count);
                            first = availableFemaleNames[index];
                            availableFemaleNames.RemoveAt(index);
                        }
                        else if (!isFemale && availableMaleNames.Count > 0)
                        {
                            int index = random.Next(availableMaleNames.Count);
                            first = availableMaleNames[index];
                            availableMaleNames.RemoveAt(index);
                        }
                        else
                        {
                            // Fallback: if names run out, generate synthetic unique name
                            first = "Student" + random.Next(1000, 9999);
                        }

                        string last = lastNames[random.Next(lastNames.Length)];
                        string email = $"{first.ToLower()}.{last.ToLower()}{random.Next(1000, 9999)}@hull.ac.uk";
                        string password = GeneratePassword(random);

                        int score = random.Next(2, 10); // wellbeing_score from 2–9
                        string lastUpdate = DateTime.Now.AddDays(-random.Next(0, 30)).ToString("yyyy-MM-dd HH:mm:ss");

                        long userId;
                        using (var userCmd = new SQLiteCommand(
                            "INSERT INTO Users (first_name, last_name, email, password, role) VALUES (@f, @l, @e, @p, 'student'); SELECT last_insert_rowid();", conn))
                        {
                            userCmd.Parameters.AddWithValue("@f", first);
                            userCmd.Parameters.AddWithValue("@l", last);
                            userCmd.Parameters.AddWithValue("@e", email);
                            userCmd.Parameters.AddWithValue("@p", password);
                            userId = (long)userCmd.ExecuteScalar();
                        }

                        long studentId;
                        using (var studCmd = new SQLiteCommand(
                            @"INSERT INTO Students (user_id, supervisor_id, wellbeing_score, last_status_update)
                      VALUES (@u, @s, @w, @d); SELECT last_insert_rowid();", conn))
                        {
                            studCmd.Parameters.AddWithValue("@u", userId);
                            studCmd.Parameters.AddWithValue("@s", supervisorId);
                            studCmd.Parameters.AddWithValue("@w", score);
                            studCmd.Parameters.AddWithValue("@d", lastUpdate);
                            studentId = (long)studCmd.ExecuteScalar();
                        }

                        studentList.Add((studentId, supervisorId, $"{first} {last}"));
                        Console.WriteLine($"🎓 Student: {first} {last}, Email: {email}, Password: {password}, Wellbeing score: {score} (updated {lastUpdate}) under {supervisorName}");
                    }
                }

                Console.WriteLine($"✅ Created {studentList.Count} students total.");

                // ---- 4. Meetings (0–1 per student) ----
                foreach (var (studentId, supervisorId, name) in studentList)
                {
                    if (random.NextDouble() < 0.5)
                    {
                        DateTime date = DateTime.Now.AddDays(-random.Next(0, 30));
                        string start = "10:00";
                        string end = "11:00";
                        string note = "Progress check and wellbeing discussion.";

                        using (var meetCmd = new SQLiteCommand(
                            @"INSERT INTO Meetings (student_id, supervisor_id, meeting_date, start_time, end_time, notes)
                      VALUES (@stu, @sup, @d, @st, @et, @n);", conn))
                        {
                            meetCmd.Parameters.AddWithValue("@stu", studentId);
                            meetCmd.Parameters.AddWithValue("@sup", supervisorId);
                            meetCmd.Parameters.AddWithValue("@d", date.ToString("yyyy-MM-dd"));
                            meetCmd.Parameters.AddWithValue("@st", start);
                            meetCmd.Parameters.AddWithValue("@et", end);
                            meetCmd.Parameters.AddWithValue("@n", note);
                            meetCmd.ExecuteNonQuery();
                        }

                        Console.WriteLine($"🗓️ Meeting logged for {name} on {date:yyyy-MM-dd}");
                    }
                }

                Console.WriteLine("🎉 Realistic database seeding completed!");
            }
        }


        public static void WipeTable()
        {
            string dbpath = "Project_database.db";
            string connString = $"Data Source={dbpath};Version=3;";

            using (var conn = new SQLiteConnection(connString))
            {
                conn.Open();

                using (var pragmaCommand = new SQLiteCommand("PRAGMA foreign_keys = ON;", conn))
                    pragmaCommand.ExecuteNonQuery();

                using (var clearCmd = new SQLiteCommand(@"
            DELETE FROM Meetings;
            DELETE FROM Students;
            DELETE FROM Supervisors;
            DELETE FROM Users;
        ", conn))
                {
                    clearCmd.ExecuteNonQuery();
                    Console.WriteLine("🧹 Cleared all existing data from database tables.");
                }
            }
        }

        public static void PrintAllUsers()
        {
            string dbpath = "Project_database.db";
            string connString = $"Data Source={dbpath};Version=3;";

            using (var conn = new SQLiteConnection(connString))
            {
                conn.Open();

                using (var cmd = new SQLiteCommand("SELECT user_id, first_name, last_name, email, password, role FROM Users ORDER BY user_id;", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("==============================================");
                    Console.WriteLine("📋 USERS TABLE CONTENTS");
                    Console.WriteLine("==============================================");

                    if (!reader.HasRows)
                    {
                        Console.WriteLine("No users found in the database.");
                        return;
                    }

                    while (reader.Read())
                    {
                        int userId = reader.GetInt32(0);
                        string firstName = reader.GetString(1);
                        string lastName = reader.GetString(2);
                        string email = reader.GetString(3);
                        string password = reader.GetString(4);
                        string role = reader.GetString(5);

                        Console.WriteLine("----------------------------------------------");
                        Console.WriteLine($"🆔  User ID: {userId}");
                        Console.WriteLine($"👤 Name: {firstName} {lastName}");
                        Console.WriteLine($"📧 Email: {email}");
                        Console.WriteLine($"🔑 Password: {password}");
                        Console.WriteLine($"🎭 Role: {role}");
                    }

                    Console.WriteLine("----------------------------------------------");
                    Console.WriteLine("✅ All user records displayed successfully.");
                    Console.WriteLine("==============================================");
                }
            }
        }
    }
}
