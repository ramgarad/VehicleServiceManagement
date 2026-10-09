using VehicleService.Entities;
using VehicleService.Models.Dtos;

namespace VehicleService.Services
{
    public class BillCalculator
    {
        // Builds Bill-of-Material lines. Prices ALWAYS come from the WorkItem master (advisor cannot change them).
        public static (List<BillItem> Items, decimal Total) Build(IEnumerable<BillLine> lines, IReadOnlyDictionary<int, WorkItem> catalog)
        {
            var items = new List<BillItem>();
            foreach (var g in lines.GroupBy(l => l.WorkItemId))
            {
                if (!catalog.TryGetValue(g.Key, out var wi))
                    throw new ArgumentException($"Work item {g.Key} was not found.");
                var qty = g.Sum(x => x.Quantity);
                items.Add(new BillItem { WorkItemId = wi.Id, Quantity = qty, UnitPrice = wi.UnitPrice, LineTotal = wi.UnitPrice * qty });
            }
            return (items, items.Sum(i => i.LineTotal));
        }

        // Sunday of the current week (a Sunday returns itself)
        public static DateTime WeekEnd(DateTime today) => today.Date.AddDays((7 - (int)today.DayOfWeek) % 7);

    }
}
