namespace CRCardSwipe.Models.ViewModels;

public record VisitsByHostEntry(string Hostname, int VisitCount);
public record DailyVisitEntry(DateTime VisitDate, int VisitCount);
