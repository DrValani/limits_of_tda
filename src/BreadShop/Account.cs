using System.Collections.Generic;

namespace BreadShop
{
    public class Account
    {
        private int? _balance = 0;
        private Dictionary<int, int> _orders = new Dictionary<int, int>();

        public int? GetBalance()
        {
            return _balance;
        }

        public int? Deposit(int? creditAmount)
        {
            _balance += creditAmount;
            return _balance;
        }

        public void AddOrder(int orderId, int amount)
        {
            _orders.Add(orderId, amount);
        }

        public int? CancelOrder(int orderId)
        {
            int value = 0;
            if (_orders.TryGetValue(orderId, out value))
            {
                _orders.Remove(orderId);
                return value;
            }
            return null;
        }
    }

}
