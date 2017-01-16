using BreadShop;
using NSubstitute;
using NUnit.Framework;

namespace BreadShopTest
{
    public class BreadShopTest
    {
        private IOutboundEvents _events;
        private BreadShop.BreadShop _breadShop;

        private const int AccountIdOne = 1;
        private const int AccountIdTwo = 2;
        private const int OrderIdOne = 1;
        private const int OrderIdTwo = 2;

        [SetUp]
        public void Setup()
        {
            _events = Substitute.For<IOutboundEvents>();
            _breadShop = new BreadShop.BreadShop(_events);
        }

        [Test]
        public void create_an_account()
        {
            _breadShop.CreateAccount(AccountIdOne);
            ExpectAccountCreationSuccess(AccountIdOne);
        }

        [Test]
        public void deposit_some_money()
        {
            CreateAccount(AccountIdOne);

            const int depositAmount = 300;            
            _breadShop.Deposit(AccountIdOne, depositAmount);

            ExpectNewBalance(AccountIdOne, depositAmount);
        }

        [Test]
        public void reject_deposits_for_nonexistent_accounts()
        {
            const int nonExistentAccountId = -5;
            
            _breadShop.Deposit(nonExistentAccountId, 4000);
            ExpectAccountNotFound(nonExistentAccountId);
        }

        [Test]
        public void deposits_add_up()
        {
            CreateAccountWithBalance(AccountIdOne, 300);

            _breadShop.Deposit(AccountIdOne, 300);
            ExpectNewBalance(AccountIdOne, 600);
        }

        [Test]
        public void place_an_order_succeeds_if_there_is_enough_money()
        {
            CreateAccountWithBalance(AccountIdOne, 500);

            _breadShop.PlaceOrder(AccountIdOne, OrderIdOne, 40);

            ExpectOrderPlaced(AccountIdOne, 40);
            ExpectNewBalance(AccountIdOne, 500 - (Cost(40)));

        }

        [Test]
        public void cannot_place_order_for_nonexistent_account()
        {
            _breadShop.PlaceOrder(-5, OrderIdOne, 40);
            ExpectAccountNotFound(-5);
        }

        [Test]
        public void cannot_place_an_order_for_more_than_account_can_afford()
        {
            CreateAccountWithBalance(AccountIdOne, 500);
 
            _breadShop.PlaceOrder(AccountIdOne, OrderIdOne, 42);
            // 42 * 12 = 504
            ExpectOrderRejected(AccountIdOne);
        }

        [Test]
        public void cancel_an_order_by_id()
        {
            var balance = 500;
            CreateAccountWithBalance(AccountIdOne, balance);

            var amount = 40;
            PlaceOrder(AccountIdOne, OrderIdOne, amount, balance);

            ExpectNewBalance(AccountIdOne, balance);

            _breadShop.CancelOrder(AccountIdOne, OrderIdOne);
            
            ExpectOrderCancelled(AccountIdOne, OrderIdOne);
        }

        [Test]
        public void cannot_cancel_an_order_for_nonexistent_account()
        {
            _breadShop.CancelOrder(-5, OrderIdOne);
            ExpectAccountNotFound(-5);
        }

        [Test]
        public void cannot_cancel_a_nonexistent_order()
        {
            CreateAccount(AccountIdOne);

            _breadShop.CancelOrder(AccountIdOne, -5);

            ExpectOrderNotFound(-5);
        }

        [Test]
        public void cancelling_an_allows_balance_to_be_reused()
        {
            var balance = 500;
            CreateAccountWithBalance(AccountIdOne, balance);

            var amount = 40;
            PlaceOrder(AccountIdOne, OrderIdOne, amount, balance);
            CancelOrder(AccountIdOne, OrderIdOne, balance);

            // it's entirely possible that the balance in the resulting event doesn't match the internal
            // state of the system, so we ensure the balance has really been restored
            // by trying to place a new order with it.
            var amount2 = 40;
            _breadShop.PlaceOrder(AccountIdOne, OrderIdTwo, amount2);
            
            ExpectOrderPlaced(AccountIdOne, amount2);
            ExpectNewBalance(AccountIdOne, balance - (Cost(amount)));

        }

        [Test]
        [Ignore("Objective A")]
        public void an_empty_shop_places_an_empty_wholesale_order()
        {
            _breadShop.PlaceWholesaleOrder();
            ExpectWholesaleOrder(0);
        }

        [Test]
        [Ignore("Objective A")]
        public void wholesale_orders_are_made_for_the_sum_of_the_quantities_of_outstanding_orders_in_one_account()
        {           
            var balance = Cost(40 + 55);
            CreateAccountWithBalance(AccountIdOne, balance);
            PlaceOrder(AccountIdOne, OrderIdOne, 40, balance);
            PlaceOrder(AccountIdOne, OrderIdTwo, 55, balance - Cost(40));

            _breadShop.PlaceWholesaleOrder();
            ExpectWholesaleOrder(40 + 55);
        }

        [Test]
        [Ignore("Objective A")]
        public void wholesale_orders_are_made_for_the_sum_of_the_quantities_of_outstanding_orders_across_accounts()
        {
            CreateAccountAndPlaceOrder(AccountIdOne, OrderIdOne, 40);
            CreateAccountAndPlaceOrder(AccountIdTwo, OrderIdTwo, 55);

            _breadShop.PlaceWholesaleOrder();
            ExpectWholesaleOrder(40 + 55);
        }

        [Test]
        [Ignore("Objective B")]
        public void arrival_of_wholesale_order_trigger_fills_of_a_single_outstanding_order()
        {
            var quantity = 40;
            CreateAccountAndPlaceOrder(AccountIdOne, OrderIdOne, quantity);

            _breadShop.OnWholesaleOrder(quantity);
            ExpectOrderFilled(AccountIdOne, OrderIdOne, quantity);
        }

        [Test]
        [Ignore("Objective B")]
        public void wholesale_order_quantities_might_only_fill_an_outstanding_order_partially()
        {
            var quantity = 40;
            CreateAccountAndPlaceOrder(AccountIdOne, OrderIdOne, quantity);

            var wholesaleOrderQuantity = quantity / 2;
            
            _breadShop.OnWholesaleOrder(wholesaleOrderQuantity);
            ExpectOrderFilled(AccountIdOne, OrderIdOne, wholesaleOrderQuantity);
        }

        [Test]
        [Ignore("Objective B")]
        public void an_order_can_be_filled_by_two_consecutive_wholesale_orders()
        {
            var quantity = 40;
            CreateAccountAndPlaceOrder(AccountIdOne, OrderIdOne, quantity);

            var firstWholeSaleQuantity = 25;
            _breadShop.OnWholesaleOrder(firstWholeSaleQuantity);
            ExpectOrderFilled(AccountIdOne, OrderIdOne, firstWholeSaleQuantity);

            var secondWholeSaleQuantity = quantity - firstWholeSaleQuantity;
            _breadShop.OnWholesaleOrder(secondWholeSaleQuantity);
            ExpectOrderFilled(AccountIdOne, OrderIdOne, secondWholeSaleQuantity);
        }

        [Test]
        [Ignore("Objective B")]
        public void orders_do_not_overfill()
        {
            var quantity = 40;
            var wholesaleOrderQuantity = 42;
            CreateAccountAndPlaceOrder(AccountIdOne, OrderIdOne, quantity);

            _breadShop.OnWholesaleOrder(wholesaleOrderQuantity);
            ExpectOrderFilled(AccountIdOne, OrderIdOne, quantity);
        }

        [Test]
        [Ignore("Objective B")]
        public void fully_filled_orders_are_removed_and_therefore_cannot_be_cancelled()
        {
            var quantity = 40;
            CreateAccountAndPlaceOrder(AccountIdOne, OrderIdOne, quantity);

            _breadShop.OnWholesaleOrder(quantity);
            ExpectOrderFilled(AccountIdOne, OrderIdOne, quantity);

            _breadShop.CancelOrder(AccountIdOne, OrderIdOne);
            ExpectOrderNotFound(OrderIdOne);
        }

        [Test]
        [Ignore("Objective B")]
        public void orders_do_not_overfill_across_two_wholesale_orders()
        {
            var quantity = 40;
            var wholesaleOrderQuantityOne = 21;
            CreateAccountAndPlaceOrder(AccountIdOne, OrderIdOne, quantity);

            _breadShop.OnWholesaleOrder(wholesaleOrderQuantityOne);
            ExpectOrderFilled(AccountIdOne, OrderIdOne, wholesaleOrderQuantityOne);

            var wholesaleOrderQuantityTwo = 33; // This will fill the remaining quantity
            _breadShop.OnWholesaleOrder(wholesaleOrderQuantityTwo);
            ExpectOrderFilled(AccountIdOne, OrderIdOne, quantity - wholesaleOrderQuantityOne);
        }

        [Test]
        [Ignore("Objective B")]
        public void orders_across_different_accounts_are_filled()
        {
            var quantityOne = 40;
            var quantityTwo = 55;
            CreateAccountAndPlaceOrder(AccountIdOne, OrderIdOne, quantityOne);
            CreateAccountAndPlaceOrder(AccountIdTwo, OrderIdTwo, quantityTwo);
         
            _breadShop.OnWholesaleOrder(quantityOne + quantityTwo);

            ExpectOrderFilled(AccountIdOne, OrderIdOne, quantityOne);
            ExpectOrderFilled(AccountIdTwo, OrderIdTwo, quantityTwo);
        }

        [Test]
        [Ignore("Objective B")]
        public void orders_fill_in_a_consistent_order_across_different_accounts()
        {
            var quantityOne = 40;
            var quantityTwo = 55;
            CreateAccountAndPlaceOrder(AccountIdOne, OrderIdOne, quantityOne);
            CreateAccountAndPlaceOrder(AccountIdTwo, OrderIdTwo, quantityTwo);

            var secondFillQuantity = 8;
            
            _breadShop.OnWholesaleOrder(quantityOne + secondFillQuantity);
            ExpectOrderFilled(AccountIdOne, OrderIdOne, quantityOne);
            ExpectOrderFilled(AccountIdTwo, OrderIdTwo, secondFillQuantity);

        }

        [Test]
        [Ignore("Objective B")]
        public void orders_fill_in_a_consistent_order_across_orders_in_the_same_account()
        {
            var quantityOne = 40;
            var quantityTwo = 50;
            var balance = Cost(quantityOne) + Cost(quantityTwo);
            CreateAccountWithBalance(AccountIdOne, balance);
            PlaceOrder(AccountIdOne, OrderIdOne, quantityOne, balance);
            PlaceOrder(AccountIdOne, OrderIdTwo, quantityTwo, balance - Cost(quantityOne));

            var secondFillQuantity = 8;
            _breadShop.OnWholesaleOrder(quantityOne + secondFillQuantity);
            ExpectOrderFilled(AccountIdOne, OrderIdOne, quantityOne);
            ExpectOrderFilled(AccountIdOne, OrderIdTwo, secondFillQuantity);

        }

        private int Cost(int quantityOne)
        {
            return quantityOne * 12;
        }

        private void ExpectOrderFilled(int accountId, int orderId, int quantity)
        {
            _events.Received(1).OrderFilled(accountId, orderId, quantity);
        }

        private void CancelOrder(int accountId, int orderId, int expectedBalanceAfterCancel)
        {
            _events.ClearReceivedCalls();
            _breadShop.CancelOrder(accountId, OrderIdOne);

            ExpectOrderCancelled(accountId, orderId);            
            ExpectNewBalance(accountId, expectedBalanceAfterCancel);
        }

        private void ExpectOrderNotFound(int orderId)
        {
            _events.Received(1).OrderNotFound(AccountIdOne, orderId);
        }


        private void ExpectOrderCancelled(int accountId, int orderId)
        {
            _events.Received(1).OrderCancelled(accountId, orderId);
        }

        private void PlaceOrder(int accountId, int orderId, int amount, int balanceBefore)
        {
            _breadShop.PlaceOrder(accountId, orderId, amount);
            ExpectOrderPlaced(accountId, amount);
            ExpectNewBalance(accountId, balanceBefore - (Cost(amount)));
        }

        private void ExpectOrderRejected(int accountId)
        {
            _events.Received(1).OrderRejected(accountId);
        }

        private void ExpectOrderPlaced(int accountId, int amount)
        {
            _events.Received(1).OrderPlaced(accountId, amount);
        }

        private void CreateAccountWithBalance(int accountId, int initialBalance)
        {
            CreateAccount(accountId);
            
            _breadShop.Deposit(accountId, initialBalance);
            ExpectNewBalance(accountId, initialBalance);
        }

        private void ExpectAccountNotFound(int accountId)
        {
            _events.Received(1).AccountNotFound(accountId);
        }

        private void CreateAccount(int accountId)
        {
            _breadShop.CreateAccount(accountId);
            ExpectAccountCreationSuccess(accountId);
        }

        private void ExpectNewBalance(int accountId, int newBalanceAmount)
        {
            _events.Received(1).NewAccountBalance(accountId, newBalanceAmount);
        }

        private void ExpectAccountCreationSuccess(int accountId)
        {
            _events.Received(1).AccountCreatedSuccessfully(accountId);
        }

        private void CreateAccountAndPlaceOrder(int accountId, int orderId, int amount)
        {
            var balance = Cost(amount);
            CreateAccountWithBalance(accountId, balance);
            PlaceOrder(accountId, orderId, amount, balance);
        }

        private void ExpectWholesaleOrder(int quantity)
        {
            _events.Received(1).PlaceWholesaleOrder(quantity);
        }
    }
}
