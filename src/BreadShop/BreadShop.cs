using System;

namespace BreadShop
{
    public class BreadShop
    {
        private const int PriceOfBread = 12;

        private readonly IOutboundEvents _events;
        private readonly AccountRepository _accountRepository = new AccountRepository();

        public BreadShop(IOutboundEvents events)
        {
            _events = events;
        }

        public void CreateAccount(int id)
        {
            var newAccount = new Account();
            _accountRepository.AddAccount(id, newAccount);
            _events.AccountCreatedSuccessfully(id);
        }

        public void Deposit(int accountId, int creditAmount)
        {
            var account = _accountRepository.GetAccount(accountId);
            if (account != null)
            {
                var newBalance = account.Deposit(creditAmount);
                _events.NewAccountBalance(accountId, newBalance);
            }
            else
            {
                _events.AccountNotFound(accountId);
            }
        }

        public void PlaceOrder(int accountId, int orderId, int amount)
        {
            var account = _accountRepository.GetAccount(accountId);
            if (account != null)
            {
                var cost = amount * PriceOfBread;
                if (account.GetBalance() >= cost)
                {
                    account.AddOrder(orderId, amount);
                    var newBalance = account.Deposit(-cost);
                    _events.OrderPlaced(accountId, amount);
                    _events.NewAccountBalance(accountId, newBalance);
                }
                else
                {
                    _events.OrderRejected(accountId);
                }
            }
            else
            {
                _events.AccountNotFound(accountId);
            }
        }

        public void CancelOrder(int accountId, int orderId)
        {
            var account = _accountRepository.GetAccount(accountId);
            if (account == null)
            {
                _events.AccountNotFound(accountId);
                return;
            }

            var cancelledQuantity = account.CancelOrder(orderId);
            if (cancelledQuantity == -1)
            {
                _events.OrderNotFound(accountId, orderId);
                return;
            }

            var newBalance = account.Deposit(cancelledQuantity * PriceOfBread);
            _events.OrderCancelled(accountId, orderId);
            _events.NewAccountBalance(accountId, newBalance);
        }

        public void PlaceWholesaleOrder()
        {
            throw new NotImplementedException("Implement me in Objective A");
        }

        public void OnWholesaleOrder(int quantity)
        {
            throw new NotImplementedException("Implement me in Objective B");
        }
    }
}