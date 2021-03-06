﻿namespace BreadShop
{
    public interface IOutboundEvents
    {
        void AccountCreatedSuccessfully(int accountId);

        void NewAccountBalance(int accountId, int newBalanceAmount);

        void AccountNotFound(int accountId);

        void OrderPlaced(int accountId, int amount);

        void OrderRejected(int accountId);

        void OrderCancelled(int accountId, int orderId);

        void OrderNotFound(int accountId, int orderId);

        // For Objective A
        void PlaceWholesaleOrder(int quantity);

        // For Objective B
        void OrderFilled(int accountId, int orderId, int quantity);
    }
}