using System;

namespace SteakawayRestaurant.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string OrderType { get; set; }  // DineIn | Online | Takeaway
        public string TableNumber { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }  // Pending|SentToKitchen|InPreparation|Ready|OutForDelivery|Delivered|BillRequested|Closed|Cancelled
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string SpecialNotes { get; set; }
        public int? WaiterId { get; set; }
        public int? RiderId { get; set; }
        public decimal Rating { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}