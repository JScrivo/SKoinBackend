using MySql.Data.MySqlClient;
using System;

namespace Sheridan.SKoin.API
{
    public static class Database
    {
        private static MySqlConnection Connection;

        public static bool TryConnect()
        {
            try
            {
                var builder = new MySqlConnectionStringBuilder
                {
                    Server = "172.24.0.2",
                    UserID = "skylar",
                    Password = "A1@yeetme",
                    Database = "SKoin"
                };

                Connection = new MySqlConnection(builder.ConnectionString);
                Connection.Open();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SQL Error: {ex.Message}");
                return false;
            }
        }

        public static bool TryCreateUser(string hash, out Guid user)
        {
            user = Guid.NewGuid();

            try
            {
                using (var command = new MySqlCommand($"INSERT INTO Users (GUID, Password) VALUES ('{user}', '{hash}');", Connection))
                {
                    if (command.ExecuteNonQuery() > 0)
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        public static bool TryGetPassword(Guid user, out string hash)
        {
            try
            {
                using (var command = new MySqlCommand($"SELECT (Password) FROM Users WHERE GUID='{user}';", Connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        hash = reader.GetString(0);
                        return true;
                    }
                }
            }
            catch
            {
                hash = string.Empty;
                return false;
            }
        }

        public static bool TrySetPassword(Guid user, string hash)
        {
            try
            {
                using (var command = new MySqlCommand($"UPDATE Users SET Password='{hash}' WHERE GUID='{user}';", Connection))
                {
                    if (command.ExecuteNonQuery() > 0)
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        public static bool TryGetBalance(Guid user, out ulong balance)
        {
            try
            {
                using (var command = new MySqlCommand($"SELECT (Balance) FROM Users WHERE GUID='{user}';", Connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        balance = reader.GetUInt64(0);
                        return true;
                    }
                }
            }
            catch
            {
                balance = 0;
                return false;
            }
        }

        public static bool TryTransact(Guid fromUser, Guid toUser, ulong amount)
        {
            try
            {
                if (TryGetBalance(fromUser, out ulong balance) && TryGetBalance(toUser, out _))
                {
                    if (balance >= amount)
                    {
                        using (var transferCommand = new MySqlCommand($"UPDATE Users SET Balance=Balance-{amount} WHERE GUID='{fromUser}'; UPDATE Users SET Balance=Balance+{amount} WHERE GUID='{toUser}';", Connection))
                        {
                            if (transferCommand.ExecuteNonQuery() > 0)
                            {
                                try
                                {
                                    using (var logCommand = new MySqlCommand($"INSERT INTO Transactions (From, To, Amount) VALUES ('{fromUser}', '{toUser}', {amount});"))
                                    {
                                        transferCommand.ExecuteNonQuery();
                                    }
                                }
                                catch { }

                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
