namespace CatsModel;

public class Breed: IDisposable
{
    public static Breed? ins = null;

    public string Code { get; set; }
    public string Group { get; set; }
    public string NameEng { get; set; }
    public string NameNat { get; set; }

    public void Dispose()
    {
        Console.WriteLine($"Dispose {GetHashCode()}");
    }
}