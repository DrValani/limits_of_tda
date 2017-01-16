using System.Collections.Generic;

namespace BreadShop
{
    public class AccountRepository
    {
        private Dictionary<int, Account> accounts = new Dictionary<int, Account>();

        public AccountRepository()
        {
        }

        public void addAccount(int id, Account newAccount)
        {
            accounts.Add(id, newAccount);
        }

        public Account getAccount(int accountId)
        {
            Account account = null;
            if (accounts.TryGetValue(accountId, out account))
            {
                return account;
            };
            return null;
        }
    }
}
