using System;

namespace Sheridan.SKoin.API.Services
{
    public class AccountService
    {
        [Documentation.Description("The JSON response that is returned if the request is unsuccessful.")]
        private static readonly string FailedRequest = $"{{\"{nameof(RegisterResponse.Success)}\": {false}}}";

        [Service("/api/account/register", ServiceType.Text, typeof(RegisterRequest), typeof(RegisterResponse))]
        [Documentation.Description("API for registering new accounts associated with a secure hash.")]
        public string RegisterAccount(string text)
        {
            if (Json.TryDeserialize(text, out RegisterRequest request) && request.IsValid())
            {
                var result = new RegisterResponse();

                if (Database.TryCreateUser(request.Hash, out Guid user))
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

        [Service("/api/login", ServiceType.Text, typeof(RegisterRequest), typeof(RegisterResponse))]
        [Documentation.Description("API for retrieving the ID of a client that was already registered.")]
        public string Login(string text)
        {
            if (Json.TryDeserialize(text, out RegisterRequest request) && request.IsValid())
            {
                var result = new RegisterResponse();

                if (Database.TryGetUser(request.Hash, out Guid user))
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

        [Service("/api/account/info", ServiceType.Text, typeof(InfoRequest), typeof(InfoResponse))]
        [Documentation.Description("API for getting account info and balances.")]
        public string GetInfo(string text)
        {
            if (Json.TryDeserialize(text, out InfoRequest request) && request.IsValid() && IsValidIdAndHash(request.GetId(), request.Hash))
            {
                var result = new InfoResponse();

                if (Database.TryGetBalance(request.GetId(), out ulong balance))
                {
                    result.Success = true;
                    result.Balance = balance;
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

        private static bool IsValidIdAndHash(Guid id, string hash)
        {
            return Database.TryGetHash(id, out string expected) && expected == hash;
        }

        private class RegisterRequest
        {
            [Documentation.Description("The secret hash for the client.")]
            public string Hash { get; set; }

            public virtual bool IsValid()
            {
                return true; //TODO: Remove. For development purposes only.

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
        }

        private class InfoRequest : RegisterRequest
        {
            [Documentation.Description("The user id that was registered with the client's secret hash.")]
            public string Id { get; set; }

            public override bool IsValid()
            {
                return base.IsValid() && Guid.TryParse(Id, out _);
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
    }
}
