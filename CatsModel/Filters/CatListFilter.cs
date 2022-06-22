﻿namespace CatsModel.Filters;

public class CatListFilter
{
    public Cattery? Cattery { get; set; }
    public DateOnly? BornAfter { get; set; }
    public DateOnly? BornBefore { get; set; }
    public string NameRegex { get; set; }
    public Cat? Ancestor { get; set; }
    public Gender? Gender { get; set; }
}
