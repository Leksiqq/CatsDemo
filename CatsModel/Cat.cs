namespace CatsModel;

public class Cat
{
    public Breed Breed { get; set; }
    public Cattery Cattery { get; set; }
    public Litter? Litter { get; set; }
    public Gender Gender { get; set; }
    public string Exterior { get; set; }
    public string NameEng { get; set; }
    public string NameNat { get; set; }
    public string OwnerInfo { get; set; }
    public string Title { get; set; }
}
