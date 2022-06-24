namespace CatsModel.Filters;

public class CatListFilter
{
    public Breed? Breed { get; set; }
    public Cattery? Cattery { get; set; }
    public DateOnly? BornAfter { get; set; }
    public DateOnly? BornBefore { get; set; }
    public string NameRegex { get; set; }
    public Gender? Gender { get; set; }
    public Cat? Self { get; set; }
    public Cat? Mother { get; set; }
    public Cat? Father { get; set; }
    public Cat? Ancestor { get; set; }
    public Litter? Litter { get; set; }
}
