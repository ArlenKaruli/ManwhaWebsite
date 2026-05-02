namespace ManwhaWebsite.Models
{
    public class ReadingListViewModel
    {
        public string DisplayName { get; set; } = "";
        // Desktop: filtered view
        public ReadingStatus ActiveFilter { get; set; } = ReadingStatus.Reading;
        public bool ShowAll { get; set; } = true;
        public List<UserReadingList> Entries { get; set; } = new();
        // Mobile: all entries grouped by status
        public Dictionary<ReadingStatus, List<UserReadingList>> GroupedEntries { get; set; } = new();
        // Counts
        public int ReadingCount { get; set; }
        public int CompletedCount { get; set; }
        public int PlanToReadCount { get; set; }
        public int DroppedCount { get; set; }
        public int TotalCount => ReadingCount + CompletedCount + PlanToReadCount + DroppedCount;
    }
}
