using System;

namespace Sheridan.SKoin.API.Services
{
    public class AccountService
    {
        private static readonly string FailedRequest = $"{{\"{nameof(RegisterResponse.Success)}\": {false}}}";

        [Service("/api/account/register", ServiceType.Text)]
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

        [Service("/api/account/info", ServiceType.Text)]
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

        [Service("/api/account/transfer", ServiceType.Text)]
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
            public string Hash { get; set; }

            public virtual bool IsValid()
            {
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
            public bool Success { get; set; } = false;
            public string Id { get; set; } = Guid.Empty.ToString();
        }

        private class InfoRequest : RegisterRequest
        {
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
            public bool Success { get; set; } = false;
            public ulong Balance { get; set; } = 0;
        }

        private class TransferRequest : InfoRequest
        {
            public string Recipient { get; set; }
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
            public bool Success { get; set; } = false;
        }
    }
}
