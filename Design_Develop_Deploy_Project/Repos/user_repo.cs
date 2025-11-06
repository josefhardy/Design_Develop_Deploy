using System;
using System.Data.SQLite;
using Design_Develop_Deploy_Project.Objects;

namespace Design_Develop_Deploy_Project.Repos
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public User GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            // Normalize input (avoid case sensitivity or whitespace issues)
            email = email.Trim().ToLower();

            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    const string query = "SELECT * FROM Users WHERE LOWER(email) = @Email";

                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new User
                                {
                                    user_id = Convert.ToInt32(reader["user_id"]),
                                    first_name = reader["first_name"].ToString(),
                                    last_name = reader["last_name"].ToString(),
                                    email = reader["email"].ToString(),
                                    password = reader["password"].ToString(),
                                    role = reader["role"].ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Rethrow so upper layers (services/UI) handle the message.
                throw new Exception("Error retrieving user by email.", ex);
            }

            return null;
        }
    }
}
