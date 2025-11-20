using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Design_Develop_Deploy_Project.Objects;

namespace Design_Develop_Deploy_Project.Repos;
public class StatusRepository
{
	public string _connectionString { get; set; }

    public StatusRepository(string connectionString)
	{
		_connectionString = connectionString;
    }

    public bool UpdateStudentWellbeing(int studentId, int wellbeingScore, Student student)
    {
        try
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string query = "UPDATE Students SET wellbeing_score = @WellbeingScore, last_status_update = @LastStatusUpdate " +
                               "WHERE student_id = @StudentId";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@WellbeingScore", wellbeingScore);
                    cmd.Parameters.AddWithValue("@LastStatusUpdate", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@StudentId", studentId);

                    int rowsaffected = cmd.ExecuteNonQuery();
                    return rowsaffected > 0;
                }
            }

            student.wellbeing_score = wellbeingScore;
        }
        catch (Exception ex)
        {
            throw new Exception("Error updating wellbeing", ex);
        }
    }

    public List<Student> GetAllStudentsByWellBeingScore(int minScore, int maxScore)
    {
        var students = new List<Student>();
        try
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string query = @"
				SELECT s.student_id, s.supervisor_id, s.wellbeing_score, s.last_status_update,
					   u.user_id, u.first_name, u.last_name, u.email, u.password, u.role
				FROM Students s
				JOIN Users u ON s.user_id = u.user_id
				WHERE s.wellbeing_score BETWEEN @MinScore AND @MaxScore";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MinScore", minScore);
                    cmd.Parameters.AddWithValue("@MaxScore", maxScore);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var student = MapReaderToStudent(reader);
                            students.Add(student);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error retrieving all students from database.", ex);
        }

        return students;
    }

    private Student MapReaderToStudent(SQLiteDataReader reader)
    {
        return new Student
        {
            user_id = Convert.ToInt32(reader["user_id"]),
            student_id = Convert.ToInt32(reader["student_id"]),
            first_name = reader["first_name"].ToString(),
            last_name = reader["last_name"].ToString(),
            email = reader["email"].ToString(),
            password = reader["password"].ToString(),
            supervisor_id = Convert.ToInt32(reader["supervisor_id"]),
            wellbeing_score = Convert.ToInt32(reader["wellbeing_score"]),
            last_status_update = reader["last_status_update"] == DBNull.Value
                                 ? (DateTime?)null
                                 : Convert.ToDateTime(reader["last_status_update"]),
            role = reader["role"].ToString()
        };
    }
}
