using System;
using System.IO;

namespace Sheridan.SKoin.API
{
    public static class Database
    {
        private const string Store = "./Store";
        private const string Users = Store + "/Users";
        private const string Transactions = Store + "/Transactions.csv";

        public static bool TryInitialize()
        {
            try
            {
                if (!Directory.Exists(Store))
                {
                    Directory.CreateDirectory(Store);
                }

                if (!Directory.Exists(Users))
                {
                    Directory.CreateDirectory(Users);
                }

                if (!File.Exists(Transactions))
                {
                    File.Create(Transactions);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static bool TryCreateUser(string hash, out Guid user)
        {
            user = Guid.NewGuid();

            var location = GetUserLocation(user);

            var info = new User(hash);

            try
            {
                if (Json.TrySerialize(info, out string json))
                {
                    File.WriteAllText(location, json);
                    return true;
                }
            }
            catch { }

            return false;
        }

        public static bool TryGetPassword(Guid user, out string hash)
        {
            if (TryGetUserInfo(user, out User info))
            {
                hash = info.Password;
                return true;
            }
            else
            {
                hash = string.Empty;
                return false;
            }
        }

        public static bool TrySetPassword(Guid user, string hash)
        {
            if (TryGetUserInfo(user, out User info))
            {
                info.Password = hash;

                if (TrySetUserInfo(user, info))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetBalance(Guid user, out ulong balance)
        {
            if (TryGetUserInfo(user, out User info))
            {
                balance = info.Balance;
                return true;
            }
            else
            {
                balance = 0;
                return false;
            }
        }

        public static bool TryTransact(Guid fromUser, Guid toUser, ulong amount)
        {
            try
            {
                if (TryGetUserInfo(fromUser, out User fromUserInfo) && TryGetUserInfo(toUser, out User toUserInfo))
                {
                    if (fromUserInfo.Balance >= amount)
                    {
                        var originalFromBalance = fromUserInfo.Balance;
                        var originalToBalance = toUserInfo.Balance;

                        fromUserInfo.Balance -= amount;
                        toUserInfo.Balance += amount;

                        if (TrySetUserInfo(fromUser, fromUserInfo))
                        {
                            if (TrySetUserInfo(toUser, toUserInfo))
                            {
                                if (TryLogTransaction(new Transaction(fromUser, toUser, amount)))
                                {
                                    return true;
                                }
                                else
                                {
                                    fromUserInfo.Balance = originalFromBalance;
                                    toUserInfo.Balance = originalToBalance;
                                    TrySetUserInfo(fromUser, fromUserInfo);
                                    TrySetUserInfo(toUser, toUserInfo);
                                }
                            }
                            else
                            {
                                fromUserInfo.Balance = originalFromBalance;
                                TrySetUserInfo(fromUser, fromUserInfo);
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

        private static bool TryGetUserInfo(Guid user, out User info)
        {
            try
            {
                var location = GetUserLocation(user);

                if (File.Exists(location))
                {
                    return Json.TryDeserialize(File.ReadAllText(location), out info);
                }
            }
            catch { }

            info = default;
            return false;
        }

        private static string GetUserLocation(Guid user)
        {
            return $"{Users}/{user}";
        }

        private static bool TrySetUserInfo(Guid user, User info)
        {
            try
            {
                var location = $"{Users}/{user}";

                if (File.Exists(location) && Json.TrySerialize(info, out string json))
                {
                    File.WriteAllText(location, json);
                    return true;
                }
            }
            catch { }

            return false;
        }

        private static bool TryLogTransaction(Transaction transaction)
        {
            try
            {
                File.AppendAllLines(Transactions, new[] { transaction.ToString() });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private struct Transaction
        {
            public Guid From;
            public Guid To;
            public ulong Amount;

            public Transaction(Guid from, Guid to, ulong amount)
            {
                From = from;
                To = to;
                Amount = amount;
            }

            public override string ToString()
            {
                return $"{From},{To},{Amount}";
            }

            public static bool TryParse(string s, out Transaction result)
            {
                var parts = s.Split(',');

                if (parts.Length == 3 && Guid.TryParse(parts[0], out Guid from) && Guid.TryParse(parts[1], out Guid to) && ulong.TryParse(parts[2], out ulong amount))
                {
                    result = new Transaction(from, to, amount);
                    return true;
                }
                else
                {
                    result = default;
                    return false;
                }
            }
        }

        private class User
        {
            public ulong Balance { get; set; }
            public string Password { get; set; }

            public User() { }

            public User(string hash)
            {
                Password = hash;
                Balance = 0;
            }
        }
    }
}
