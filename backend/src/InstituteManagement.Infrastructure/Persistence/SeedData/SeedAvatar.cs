namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class SeedAvatar
{
    public static string Create(string name, string color)
    {
        var initials = string.Join("", name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => char.ToUpperInvariant(x[0])));
        return $"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='400' height='600' viewBox='0 0 400 600'%3E%3Crect width='400' height='600' fill='%23{color}'/%3E%3Ccircle cx='200' cy='210' r='96' fill='%23ffffff' fill-opacity='.22'/%3E%3Ctext x='200' y='245' text-anchor='middle' font-family='Arial' font-size='82' font-weight='700' fill='white'%3E{initials}%3C/text%3E%3Cpath d='M55 600c15-135 75-210 145-210s130 75 145 210' fill='%23ffffff' fill-opacity='.18'/%3E%3C/svg%3E";
    }
}
