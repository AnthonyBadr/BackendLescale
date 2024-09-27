namespace backend.Models
{
    public class Reports
    {
        string DateOfReport { get; set; }

        public int ReportNumber { get; set; }
        public string Item { get; set; }

        public int quantity { get; set; }
        string Status { get; set; }

        public double Percentage { get; set; }

    }
}
