using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace backend.Models
{
    public class Payment
    {

        public int PaymentNumber {  get; set; }
        public string Type { get; set; }
        public double Amount { get; set; }
        public string Reason { get; set; }

        public string Created_by { get; set; }

        public string DateOfPayment {  get; set; }

        public int GrossNumber {  get; set; }



    }
}
