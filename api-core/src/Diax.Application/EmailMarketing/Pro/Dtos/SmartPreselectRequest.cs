namespace Diax.Application.EmailMarketing.Pro.Dtos;

public class SmartPreselectRequest
{
    public List<int> Segments { get; set; } = [1, 2]; // 1=Warm 2=Hot
    public int MinScore { get; set; } = 0;
    public int MaxPerProvider { get; set; } = 20;
    public int CooldownDays { get; set; } = 30;
}
