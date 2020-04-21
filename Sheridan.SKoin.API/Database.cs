using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

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

            if (TryGetUser(hash, out _)) return false;

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

        public static bool TryGetUser(string hash, out Guid user)
        {
            user = Guid.Empty;

            try
            {
                foreach (var userData in Directory.GetFiles(Users))
                {
                    if (Json.TryDeserialize(File.ReadAllText(userData), out User data))
                    {
                        if (data.Hash == hash)
                        {
                            var match = Regex.Match(userData, @"\\([\d\w-]+).\w+");

                            if (match.Success)
                            {
                                user = Guid.Parse(match.Groups[1].Value);
                                return true;
                            }
                        }
                    }
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

        public static bool TryGetName(Guid user, out string name)
        {
            if (TryGetUserInfo(user, out User info))
            {
                name = info.Name;
                return true;
            }
            else
            {
                name = null;
                return false;
            }
        }

        public static bool TryGetEmail(Guid user, out string email)
        {
            if (TryGetUserInfo(user, out User info))
            {
                email = info.Email;
                return true;
            }
            else
            {
                email = null;
                return false;
            }
        }

        public static bool TryGetPhone(Guid user, out string phone)
        {
            if (TryGetUserInfo(user, out User info))
            {
                phone = info.Phone;
                return true;
            }
            else
            {
                phone = null;
                return false;
            }
        }

        public static bool TryGetAddress(Guid user, out string address)
        {
            if (TryGetUserInfo(user, out User info))
            {
                address = info.Address;
                return true;
            }
            else
            {
                address = null;
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

        public static bool TryGetOneTimeSource(Guid user, out bool oneTime)
        {
            if (TryGetUserInfo(user, out User info))
            {
                oneTime = info.OneTimeSource;
                return true;
            }
            else
            {
                oneTime = false;
                return false;
            }
        }

        public static bool TryGetTransactions(Guid user, out Transaction[] transactions)
        {
            if (TryGetUserInfo(user, out User info))
            {
                transactions = info.Transactions.ToArray();
                return true;
            }
            else
            {
                transactions = null;
                return false;
            }
        }

        public static bool TrySetName(Guid user, string name)
        {
            if (TryGetUserInfo(user, out User info))
            {
                info.Name = name;

                return TrySetUserInfo(user, info);
            }
            else
            {
                return false;
            }
        }

        public static bool TrySetEmail(Guid user, string email)
        {
            if (TryGetUserInfo(user, out User info))
            {
                info.Email = email;

                return TrySetUserInfo(user, info);
            }
            else
            {
                return false;
            }
        }

        public static bool TrySetPhone(Guid user, string phone)
        {
            if (TryGetUserInfo(user, out User info))
            {
                info.Phone = phone;

                return TrySetUserInfo(user, info);
            }
            else
            {
                return false;
            }
        }

        public static bool TrySetAddress(Guid user, string address)
        {
            if (TryGetUserInfo(user, out User info))
            {
                info.Address = address;

                return TrySetUserInfo(user, info);
            }
            else
            {
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

        public static bool TrySetOneTimeSource(Guid user, bool oneTime)
        {
            if (TryGetUserInfo(user, out User info))
            {
                info.OneTimeSource = oneTime;
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

        public static bool TryCreatePromotion(string name, Promotion promotion, bool allowOverride = false)
        {
            var location = GetPromotionLocation(name);

            if (!allowOverride && File.Exists(location)) return false;

            try
            {
                if (Json.TrySerialize(promotion, out string json) && Guid.TryParse(promotion.Owner, out Guid id) && TryGetUserInfo(id, out User info))
                {
                    File.WriteAllText(location, json);

                    info.Promotions.Add(promotion);

                    return TrySetUserInfo(id, info);
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool TryGetPromotion(string name, out Promotion promotion)
        {
            var location = GetPromotionLocation(name);

            if (File.Exists(location))
            {
                try
                {
                    return Json.TryDeserialize(File.ReadAllText(location), out promotion);
                }
                catch { }
            }

            promotion = null;
            return false;
        }

        public static bool TryGetUserPromotions(Guid user, out Promotion[] promotions)
        {
            if (TryGetUserInfo(user, out User info))
            {
                promotions = info.Promotions.ToArray();
                return true;
            }
            else
            {
                promotions = null;
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
                return TryLogUserTransaction(transaction.From, transaction) &&
                    TryLogUserTransaction(transaction.To, transaction);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryLogUserTransaction(Guid user, Transaction transaction)
        {
            if (TryGetUserInfo(user, out User info))
            {
                info.Transactions.Add(transaction);

                return TrySetUserInfo(user, info);
            }
            else
            {
                return false;
            }
        }

        private class User
        {
            public ulong Balance { get; set; } = 0;
            public string Name { get; set; }
            public string Hash { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string Address { get; set; } = null;
            public bool Enterprise { get; set; } = false;
            public bool OneTimeSource { get; set; } = false;
            public List<Transaction> Transactions { get; set; } = new List<Transaction>();
            public List<Promotion> Promotions { get; set; } = new List<Promotion>();

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
