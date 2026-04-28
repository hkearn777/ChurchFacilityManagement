namespace ChurchFacilityManagement.Models
{
    public class DropdownValues
    {
        public List<string> Buildings { get; set; } = new();
        public List<string> Priorities { get; set; } = new();
        public List<string> Statuses { get; set; } = new();
        public List<string> RequestMethods { get; set; } = new();
        public Dictionary<string, string> StatusColors { get; set; } = new();
        public List<string> SelectedStatuses { get; set; } = new();
    }
}
