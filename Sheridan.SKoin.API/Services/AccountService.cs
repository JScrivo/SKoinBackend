using System;
using System.Linq;

namespace Sheridan.SKoin.API.Services
{
    public class AccountService
    {
        [Documentation.Description("The JSON response that is returned if the request is unsuccessful.")]
        private static readonly string FailedRequest = "{\"Success\": false}";

        [Service("/api/account/register", ServiceType.Text, typeof(RegisterRequest), typeof(RegisterResponse))]
        [Documentation.Description("API for registering new accounts associated with a secure hash.")]
        public string RegisterAccount(string text)
        {
            if (Json.TryDeserialize(text, out RegisterRequest request) && request.IsValid())
            {
                var result = new RegisterResponse { Enterprise = request.Enterprise };

                if (Database.TryCreateUser(request.Hash, out Guid user) &&
                    Database.TrySetName(user, request.Name) &&
                    Database.TrySetPhone(user, request.Phone) &&
                    Database.TrySetEmail(user, request.Email) &&
                    Database.TrySetAddress(user, request.Address) &&
                    Database.TrySetEnterprise(user, request.Enterprise))
                {
                    result.Success = true;
                    result.Id = user.ToString();
                }

                if (Json.TrySerialize(result, out string json))
                {
                    return json;
                }
                else
                {
                    return FailedRequest;
                }
            }

            return null;
        }

        [Service("/api/account/login", ServiceType.Text, typeof(LoginRequest), typeof(RegisterResponse))]
        [Documentation.Description("API for retrieving the ID of a client that was already registered.")]
        public string Login(string text)
        {
            if (Json.TryDeserialize(text, out LoginRequest request) && request.IsValid())
            {
                var result = new RegisterResponse();

                if (Database.TryGetUser(request.Hash, out Guid user) &&
                    Database.TryGetEnterprise(user, out bool enterprise))
                {
                    result.Success = true;
                    result.Id = user.ToString();
                    result.Enterprise = enterprise;
                }

                if (Json.TrySerialize(result, out string json))
                {
                    return json;
                }
                else
                {
                    return FailedRequest;
                }
            }

            return null;
        }

        [Service("/api/account/info", ServiceType.Text, typeof(InfoRequest), typeof(InfoResponse))]
        [Documentation.Description("API for getting account info and balances.")]
        public string GetInfo(string text)
        {
            if (Json.TryDeserialize(text, out InfoRequest request) && request.IsValid() && IsValidIdAndHash(request.GetId(), request.Hash))
            {
                var result = new InfoResponse();

                if (Database.TryGetBalance(request.GetId(), out ulong balance) &&
                    Database.TryGetName(request.GetId(), out string name) &&
                    Database.TryGetEmail(request.GetId(), out string email) &&
                    Database.TryGetPhone(request.GetId(), out string phone) &&
                    Database.TryGetAddress(request.GetId(), out string address) &&
                    Database.TryGetEnterprise(request.GetId(), out bool enterprise) &&
                    Database.TryGetTransactions(request.GetId(), out Transaction[] transactions))
                {
                    result.Success = true;
                    result.Balance = balance;
                    result.Name = name;
                    result.Email = email;
                    result.Phone = phone;
                    result.Address = address;
                    result.Enterprise = enterprise;
                    result.Transactions = transactions.Select(transfer =>
                    {
                        if (transfer.From == request.GetId())
                        {
                            if (Database.TryGetName(transfer.To, out string name))
                            {
                                return new InfoTransaction
                                {
                                    Id = transfer.To,
                                    Name = name,
                                    Amount = transfer.Amount,
                                    Outbound = true
                                };
                            }
                        }
                        else if (transfer.To == request.GetId())
                        {
                            if (Database.TryGetName(transfer.From, out string name))
                            {
                                return new InfoTransaction
                                {
                                    Id = transfer.From,
                                    Name = name,
                                    Amount = transfer.Amount,
                                    Outbound = false
                                };
                            }
                        }

                        return null;
                    }).Where(transfer => !(transfer is null)).ToArray();
                }

                if (Json.TrySerialize(result, out string json))
                {
                    return json;
                }
                else
                {
                    return FailedRequest;
                }
            }

            return null;
        }

        [Service("/api/account/transfer", ServiceType.Text, typeof(TransferRequest), typeof(TransferResponse))]
        [Documentation.Description("API for transfering funds between accounts.")]
        public string Transfer(string text)
        {
            if (Json.TryDeserialize(text, out TransferRequest request) && request.IsValid() && IsValidIdAndHash(request.GetId(), request.Hash))
            {
                var result = new TransferResponse();

                if (Database.TryTransact(request.GetId(), request.GetRecipient(), request.Amount))
                {
                    result.Success = true;
                }

                if (Json.TrySerialize(result, out string json))
                {
                    return json;
                }
                else
                {
                    return FailedRequest;
                }
            }

            return null;
        }

        [Service("/api/account/redeem", ServiceType.Text, typeof(RedeemRequest), typeof(TransferResponse))]
        [Documentation.Description("API for redeeming one-time promo codes.")]
        public string Redeem(string text)
        {
            if (Json.TryDeserialize(text, out RedeemRequest request) && request.IsValid() && IsValidIdAndHash(request.GetId(), request.Hash))
            {
                var result = new TransferResponse();

                if (Database.TryGetOneTimeSource(request.GetCode(), out bool oneTime) && oneTime &&
                    Database.TryGetBalance(request.GetCode(), out ulong balance) && balance > 0 &&
                    Database.TryTransact(request.GetCode(), request.GetId(), balance))
                {
                    result.Success = true;
                }

                if (Json.TrySerialize(result, out string json))
                {
                    return json;
                }
                else
                {
                    return FailedRequest;
                }
            }

            return null;
        }

        private static bool IsValidIdAndHash(Guid id, string hash)
        {
            return Database.TryGetHash(id, out string expected) && expected == hash;
        }

        private class RegisterRequest
        {
            [Documentation.Description("The secret hash for the client.")]
            public string Hash { get; set; }
            [Documentation.Description("The account name.")]
            public string Name { get; set; }
            [Documentation.Description("The account email.")]
            public string Email { get; set; }
            [Documentation.Description("The account phone number.")]
            public string Phone { get; set; }
            [Documentation.Description("The account address.")]
            public string Address { get; set; } = null;
            [Documentation.Description("Whether the account is enterprise.")]
            public bool Enterprise { get; set; } = false;

            public virtual bool IsValid()
            {
                return !(Hash is null || Name is null || Email is null || Phone is null); //TODO: Remove. For development purposes only.

                try
                {
                    return Convert.FromBase64String(Hash).Length == 32;
                }
                catch (FormatException)
                {
                    return false;
                }
                catch (ArgumentNullException)
                {
                    return false;
                }
            }
        }

        private class RegisterResponse
        {
            [Documentation.Description("Whether or not the request was successful.")]
            public bool Success { get; set; } = false;
            [Documentation.Description("The user id that was registered with the client's secret hash.")]
            public string Id { get; set; } = Guid.Empty.ToString();
            [Documentation.Description("Whether the account is an enterprise account.")]
            public bool Enterprise { get; set; }
        }

        private class LoginRequest
        {
            [Documentation.Description("The secret hash for the client.")]
            public string Hash { get; set; }

            public virtual bool IsValid()
            {
                return !(Hash is null); //TODO: Remove. For development purposes only.

                try
                {
                    return Convert.FromBase64String(Hash).Length == 32;
                }
                catch (FormatException)
                {
                    return false;
                }
                catch (ArgumentNullException)
                {
                    return false;
                }
            }
        }

        private class InfoRequest
        {
            [Documentation.Description("The user id that was registered with the client's secret hash.")]
            public string Id { get; set; }
            [Documentation.Description("The secret hash for the client.")]
            public string Hash { get; set; }

            public virtual bool IsValid()
            {
                return Guid.TryParse(Id, out _) && !(Hash is null); //TODO: Remove. For development purposes only.

                try
                {
                    return Guid.TryParse(Id, out _) && Convert.FromBase64String(Hash).Length == 32;
                }
                catch (FormatException)
                {
                    return false;
                }
                catch (ArgumentNullException)
                {
                    return false;
                }
            }

            public Guid GetId()
            {
                return Guid.Parse(Id);
            }
        }

        private class InfoResponse
        {
            [Documentation.Description("Whether or not the request was successful.")]
            public bool Success { get; set; } = false;
            [Documentation.Description("The current balance of the account.")]
            public ulong Balance { get; set; } = 0;
            [Documentation.Description("The account name.")]
            public string Name { get; set; }
            [Documentation.Description("The account email.")]
            public string Email { get; set; } = null;
            [Documentation.Description("The account phone number.")]
            public string Phone { get; set; } = null;
            [Documentation.Description("The account address.")]
            public string Address { get; set; } = null;
            [Documentation.Description("Whether the account is an enterprise account.")]
            public bool Enterprise { get; set; } = false;
            [Documentation.Children]
            [Documentation.Description("The transactions made by the account.")]
            public InfoTransaction[] Transactions { get; set; } = new InfoTransaction[0];
        }

        private class InfoTransaction
        {
            [Documentation.Description("The user id of the other transactor.")]
            public Guid Id { get; set; } = Guid.Empty;
            [Documentation.Description("The name of the other transactor.")]
            public string Name { get; set; } = string.Empty;
            [Documentation.Description("The amount of the transaction.")]
            public ulong Amount { get; set; } = 0;
            [Documentation.Description("Whether the transaction is going out of the user's account.")]
            public bool Outbound { get; set; } = false;
        }

        private class TransferRequest : InfoRequest
        {
            [Documentation.Description("The user id of the recipient.")]
            public string Recipient { get; set; }
            [Documentation.Description("The amount to send to the recipient.")]
            public ulong Amount { get; set; }

            public override bool IsValid()
            {
                return base.IsValid() && Guid.TryParse(Recipient, out _);
            }

            public Guid GetRecipient()
            {
                return Guid.Parse(Recipient);
            }
        }

        private class TransferResponse
        {
            [Documentation.Description("Whether or not the request was successful.")]
            public bool Success { get; set; } = false;
        }

        private class RedeemRequest : InfoRequest
        {
            public string Code { get; set; }

            public override bool IsValid()
            {
                return base.IsValid() && Guid.TryParse(Code, out _);
            }

            public Guid GetCode()
            {
                return Guid.Parse(Code);
            }
        }
    }
}
