using System.Collections.Generic;

namespace BreadShop
{
    public class AccountRepository
    {
        private readonly Dictionary<int, Account> _accounts = new Dictionary<int, Account>();

        public void AddAccount(int id, Account newAccount)
        {
            _accounts.Add(id, newAccount);
        }

        public Account GetAccount(int accountId)
        {
            Account account;
            _accounts.TryGetValue(accountId, out account);
            return account;
        }
    }
}
