using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Design_Develop_Deploy_Project;

public class StudentRepository
{
	private string _connectionString;

	public StudentRepository(string connectionString)
	{
		_connectionString = connectionString;
	}

	public Student GetStudentByEmail(string email)
	{
		if (string.IsNullOrWhiteSpace(email))
			return null;

		using (var conn = new SQLiteConnection(_connectionString))
		{
			conn.Open();

			string query = @"
			SELECT s.student_id, s.supervisor_id, s.wellbeing_score, s.last_status_update,
				   u.user_id, u.first_name, u.last_name, u.email, u.password, u.role
			FROM Students s
			JOIN Users u ON s.user_id = u.user_id
			WHERE LOWER(u.email) = @Email";


			using (var cmd = new SQLiteCommand(query, conn))
			{
				cmd.Parameters.AddWithValue("@Email", email.Trim().ToLower());

				using (var reader = cmd.ExecuteReader())
				{
					if (reader.Read())
					{
						return MapReaderToStudent(reader);
					}
					else
					{
						return null;
					}
				}
			}
		}

	}

	public Student GetStudentById(int studentId)
	{
		if (studentId <= 0)
		{
			return null;
		}
		using (var conn = new SQLiteConnection(_connectionString))
		{
			conn.Open();
			string query = @"
			SELECT s.student_id, s.supervisor_id, s.wellbeing_score, s.last_status_update,
				   u.user_id, u.first_name, u.last_name, u.email, u.password, u.role
			FROM Students s
			JOIN Users u ON s.user_id = u.user_id
			WHERE s.student_id = @StudentId";


			using (var cmd = new SQLiteCommand(query, conn))
			{
				cmd.Parameters.AddWithValue("@StudentId", studentId);

				using (var reader = cmd.ExecuteReader())
				{
					if (reader.Read())
					{
						return MapReaderToStudent(reader);

					}
					else
					{
						return null;
					}
				}
			}
		}
	}

	public List<Student> GetAllStudentsUnderSpecificSupervisor(int supervisorId)
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
				WHERE s.supervisor_id = @SupervisorId";
				using (var cmd = new SQLiteCommand(query, conn))
				{
					cmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
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

	public List<Student> GetAllStudents()
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
				JOIN Users u ON s.user_id = u.user_id";
				using (var cmd = new SQLiteCommand(query, conn))
				{
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


