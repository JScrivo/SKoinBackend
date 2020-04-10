using System;
using System.Collections.Generic;
using System.IO;

namespace Sheridan.SKoin.API
{
    public static class Database
    {
        private const string Store = "./Store";
        private const string Users = Store + "/Users";
        private const string Transactions = Store + "/Transactions.csv";
        private const string Promotions = Store + "/Promotions";
        private const string Special = Store + "/Special";

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

                if (!Directory.Exists(Promotions))
                {
                    Directory.CreateDirectory(Promotions);
                }

                if (!Directory.Exists(Special))
                {
                    Directory.CreateDirectory(Special);
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

            if (File.Exists(location)) return false;

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

        public static bool TryGetHash(Guid user, out string hash)
        {
            if (TryGetUserInfo(user, out User info))
            {
                hash = info.Hash;
                return true;
            }
            else
            {
                hash = string.Empty;
                return false;
            }
        }

        public static bool TrySetHash(Guid user, string hash)
        {
            if (TryGetUserInfo(user, out User info))
            {
                info.Hash = hash;

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

        public static bool TryGetEnterprise(Guid user, out bool enterprise)
        {
            if (TryGetUserInfo(user, out User info))
            {
                enterprise = info.Enterprise;
                return true;
            }
            else
            {
                enterprise = false;
                return false;
            }
        }

        public static bool TrySetEnterprise(Guid user, bool enterprise)
        {
            if (TryGetUserInfo(user, out User info))
            {
                info.Enterprise = enterprise;
                return TrySetUserInfo(user, info);
            }
            else
            {
                return false;
            }
        }

        public static string[] GetPromotions(ulong maxResults)
        {
            var result = new List<string>();

            foreach (var promo in Directory.GetFiles(Promotions))
            {
                if (maxResults == 0) break;

                try
                {
                    result.Add(File.ReadAllText(promo));

                    maxResults--;
                }
                catch { }
            }

            return result.ToArray();
        }

        public static bool TryCreatePromotion(string name, string promotion)
        {
            var location = GetPromotionLocation(name);

            if (File.Exists(location)) return false;

            try
            {
                File.WriteAllText(location, promotion);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryRemovePromotion(string name)
        {
            var location = GetPromotionLocation(name);

            if (File.Exists(location))
            {
                try
                {
                    File.Delete(location);

                    return true;
                }
                catch { }
            }

            return false;
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

        public static bool TryGetSpecial<T>(string name, out T data)
        {
            data = default;

            var location = GetSpecialLocation(name);

            if (File.Exists(location))
            {
                var content = File.ReadAllText(location);

                if (Json.TryDeserialize(content, out T obj))
                {
                    data = obj;

                    return true;
                }
            }

            return false;
        }

        public static bool TrySetSpecial<T>(string name, T data)
        {
            var location = GetSpecialLocation(name);

            if (File.Exists(location))
            {
                if (Json.TrySerialize(data, out string json))
                {
                    try
                    {
                        File.WriteAllText(location, json);

                        return true;
                    }
                    catch { }
                }
            }

            return false;
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
            return $"{Users}/{user}.json";
        }

        private static string GetPromotionLocation(string promotion)
        {
            return $"{Promotions}/{promotion}.json";
        }

        private static string GetSpecialLocation(string special)
        {
            return $"{Special}/{special}.json";
        }

        private static bool TrySetUserInfo(Guid user, User info)
        {
            try
            {
                var location = GetUserLocation(user);

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
            public string Hash { get; set; }
            public bool Enterprise { get; set; }

            public User() { }

            public User(string hash)
            {
                Hash = hash;
                Balance = 0;
                Enterprise = false;
            }
        }
    }
}
