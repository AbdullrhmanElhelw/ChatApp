using ChatApp.Domain.Common;

namespace ChatApp.Domain.Entities;

public class CoupleSpace : BaseEntity
{
    public Guid Partner1Id { get; set; }
    public Guid? Partner2Id { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public DateTime? PairedAt { get; set; }

    public string? Partner1Mood { get; set; }
    public string? Partner2Mood { get; set; }

    public string? SpecialDatesJson { get; set; }
    public string? MemoriesJson { get; set; }
    public string? LoveNotesJson { get; set; }
    public string? DailyAnswersJson { get; set; }

    public AppUser? Partner1 { get; set; }
    public AppUser? Partner2 { get; set; }
}
