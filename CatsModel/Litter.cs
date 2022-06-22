namespace CatsModel;

public class Litter
{
    public int Order { get; set; }
    public Cat Female { get; set; }
    public Cat? Male { get; set; }
    public DateOnly Date { get; set; }
}
