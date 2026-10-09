namespace VehicleService
{
    public enum  ServiceStatus
{
    Scheduled = 1,   // admin assigned to advisor  (= "under servicing")
    Completed = 2,   // advisor finished, waiting for payment
    Paid = 3,        // payment processed
    Dispatched = 4   // vehicle handed over
}
}
