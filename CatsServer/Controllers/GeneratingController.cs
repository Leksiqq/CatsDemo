using CatsModel;
using CatsUtil;
using Microsoft.AspNetCore.Mvc;
using Net.Leksi.KeyBox;

namespace CatsServer.Controllers;

public class GeneratingController : Controller
{
    [Route("/generate/colors")]
    public async Task GenerateColorsAsync(string? till)
    {
        Random random = new();
        string letters = "abcdefghjnopqrswxy";
        IKeyBox keyBox = HttpContext.RequestServices.GetRequiredService<IKeyBox>();

        HttpContext.Response.ContentType = "text/plain; charset=utf-8";

        await foreach (var it in HttpContext.RequestServices.GetRequiredService<Storage>().GetCatsAsync(null))
        {
            IKeyRing keyRing = keyBox.GetKeyRing(it);
            await HttpContext.Response.WriteAsync($"update Cats set Exterior='{letters[random.Next(0, letters.Length)]}' where IdCat={keyRing["IdCat"]} and IdCattery={keyRing["IdCattery"]};\n");
        }
    }

    [Route("/generate/titles")]
    public async Task GenerateTitlesAsync(string? till)
    {
        Random random = new();
        string[] titles = new[] { "WCH", "GEC", "EC", "GIC", "IC", "CH" };
        IKeyBox keyBox = HttpContext.RequestServices.GetRequiredService<IKeyBox>();

        HttpContext.Response.ContentType = "text/plain; charset=utf-8";

        await foreach (var it in HttpContext.RequestServices.GetRequiredService<Storage>().GetCatsAsync(null))
        {
            IKeyRing keyRing = keyBox.GetKeyRing(it);
            await HttpContext.Response.WriteAsync($"update Cats set title='{titles[random.Next(0, titles.Length)]}' where IdCat={keyRing["IdCat"]} and IdCattery={keyRing["IdCattery"]};\n");
        }
    }

    [Route("/generate/cats/{till=null}")]
    public async Task GenerateCatsAsync(string? till)
    {
        HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        DateOnly end = DateOnly.Parse(till is null ? DateTime.Today.ToString("yyyy-MM-dd") : till);
        int reprodFrom = 1;
        int reprodTo = 2;
        int monthsBetweenLitters = 11;
        int breedsCount = 3;
        int breedPos = 0;
        int firstMomsFreq = 365;
        IKeyBox keyBox = HttpContext.RequestServices.GetRequiredService<IKeyBox>();
        List<Breed> breeds = new();
        await foreach (var it in HttpContext.RequestServices.GetRequiredService<Storage>().GetBreedsAsync(null))
        {
            breeds.Add(it);
        }
        List<Cattery> catteries = new();
        await foreach (var it in HttpContext.RequestServices.GetRequiredService<Storage>().GetCatteriesAsync(null))
        {
            catteries.Add(it);
        }
        Dictionary<int, int> genIds = new();
        Dictionary<Cat, List<Litter>> litters = new();
        Dictionary<Cat, DateOnly> firstMomsBirthdays = new();
        List<Cat> cats = new();
        DateOnly today = DateOnly.Parse("2000-01-01");
        Random random = new();
        Dictionary<int, List<string>> namesMale = new();
        HashSet<int> usedCatteries = new();
        string[] lines = System.IO.File.ReadAllLines("Database/names_male.txt");
        for (int i = 0; i < lines.Length; i += 2)
        {
            namesMale[int.Parse(lines[i].Trim())] = new List<string>(lines[i + 1].Split(new[] { ',', '.' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        Dictionary<int, List<string>> namesFemale = new();
        lines = System.IO.File.ReadAllLines("Database/names_female.txt");
        for (int i = 0; i < lines.Length; i += 2)
        {
            namesFemale[int.Parse(lines[i].Trim())] = new List<string>(lines[i + 1].Split(new[] { ',', '.' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        while (today < end)
        {
            await HttpContext.Response.WriteAsync($"/* {today.ToString()} */\n");
            if (breedPos < breedsCount)
            {
                if (random.Next(firstMomsFreq) == 0)
                {
                    Breed breed = breeds[breedPos];
                    ++breedPos; 
                    Cat cat = cat = HttpContext.RequestServices.GetRequiredService<Cat>();
                    cat.Breed = breed;
                    cat.Gender = Gender.Female;
                    firstMomsBirthdays[cat] = today;
                    int firstLetter = random.Next(1, 10);
                    cat.NameNat = namesFemale[firstLetter][random.Next(0, namesFemale[firstLetter].Count)];
                    cat.NameEng = Util.Transliterate(cat.NameNat);
                    int idCattery = random.Next(1, catteries.Count + 1);
                    int idCat;
                    if (genIds.TryGetValue(idCattery, out idCat))
                    {
                        ++idCat;
                    }
                    else
                    {
                        idCat = 1;
                    }
                    genIds[idCattery] = idCat;
                    IKeyRing catKeyRing = keyBox.GetKeyRing(cat)!;
                    catKeyRing["IdCat"] = idCat;
                    catKeyRing["IdCattery"] = idCattery;
                    cats.Add(cat);
                    await HttpContext.Response.WriteAsync(GetCatInsertSql(keyBox, cat));
                }
            }
            else
            {
                usedCatteries.Clear();
                foreach (Cat femaleCat in cats.Where(c => c.Gender is Gender.Female
                    && (c.Litter is Litter lt && lt.Date <= today.AddYears(-reprodFrom)
                    && lt.Date > today.AddYears(-reprodTo)
                    || c.Litter is null && firstMomsBirthdays[c] <= today.AddYears(-reprodFrom) && firstMomsBirthdays[c] > today.AddYears(-reprodTo)))
                    .OrderBy(c =>
                    {
                        List<Litter>? litterList = null;
                        if (!litters.TryGetValue(c, out litterList))
                        {
                            return DateOnly.MinValue;
                        }
                        if (litterList.Last().Date <= today.AddMonths(-monthsBetweenLitters))
                        {
                            return litterList.Last().Date;
                        }
                        return DateOnly.MaxValue;
                    }))
                {
                    List<Litter>? litterList = null;
                    if (!litters.TryGetValue(femaleCat, out litterList) || litterList.Last().Date <= today.AddMonths(-monthsBetweenLitters))
                    {
                        IKeyRing femaleCatKeyRing = keyBox.GetKeyRing(femaleCat);
                        Cat[] allMaleCats = cats.Where(c => c.Gender is Gender.Male && c.Breed.Code == femaleCat.Breed.Code && c.Litter.Date <= today.AddYears(-reprodFrom)
                            && c.Litter.Date > today.AddYears(-reprodTo)).ToArray();
                        Cat[] maleCats = allMaleCats.Where(c => 
                        {
                            IKeyRing cKeyRing = keyBox.GetKeyRing(c);
                            return cKeyRing["IdCattery"] != femaleCatKeyRing["IdCattery"];
                        }).ToArray();
                        Cat ? maleCat = maleCats.Length > 0 ? maleCats[random.Next(maleCats.Length)] : (allMaleCats.Length > 0 ? allMaleCats[random.Next(allMaleCats.Length)] : null);
                        int idCattery = random.Next(2) == 0 ? (int)keyBox.GetKeyRing(femaleCat)!["IdCattery"]! 
                            : (maleCat is null ? random.Next(1, catteries.Count + 1) : (int)keyBox.GetKeyRing(maleCat)!["IdCattery"]!);
                        if (!usedCatteries.Contains(idCattery))
                        {
                            Litter litter = HttpContext.RequestServices.GetRequiredService<Litter>();
                            if (litterList is null)
                            {
                                litterList = new List<Litter>();
                                litters[femaleCat] = litterList;
                            }
                            litterList.Add(litter);
                            litter.Order = litterList.Count;
                            litter.Female = femaleCat;
                            litter.Male = maleCat;
                            litter.Date = today;
                            await HttpContext.Response.WriteAsync(GetLitterInsertSql(keyBox, litter));
                            int countNewBorn = random.Next(1, 6);
                            for (int i = 0; i < countNewBorn; ++i)
                            {
                                Cat cat = cat = HttpContext.RequestServices.GetRequiredService<Cat>();
                                cat.Breed = (maleCat is null || random.Next(2) == 0 ? femaleCat : maleCat).Breed;
                                cat.Gender = random.Next(2) == 0 ? Gender.Female : Gender.Male;
                                if (cat.Gender is Gender.Female)
                                {
                                    cat.NameNat = namesFemale[litter.Order][random.Next(0, namesFemale[litter.Order].Count)];
                                }
                                else
                                {
                                    cat.NameNat = namesMale[litter.Order][random.Next(0, namesMale[litter.Order].Count)];
                                }
                                cat.NameEng = Util.Transliterate(cat.NameNat);
                                cat.Litter = litter;
                                int idCat;
                                if (genIds.TryGetValue(idCattery, out idCat))
                                {
                                    ++idCat;
                                }
                                else
                                {
                                    idCat = 1;
                                }
                                genIds[idCattery] = idCat;
                                IKeyRing catKeyRing = keyBox.GetKeyRing(cat)!;
                                catKeyRing["IdCat"] = idCat;
                                catKeyRing["IdCattery"] = idCattery;
                                cats.Add(cat);
                                await HttpContext.Response.WriteAsync(GetCatInsertSql(keyBox, cat));
                            }
                            usedCatteries.Add(idCattery);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            today = today.AddDays(1);
        }
        await HttpContext.Response.WriteAsync($"/* total: {cats.Count} cats */");
    }

    private string GetLitterInsertSql(IKeyBox keyBox, Litter litter)
    {
        IKeyRing femaleKeyRing = keyBox.GetKeyRing(litter.Female)!;
        IKeyRing? maleKeyRing = litter.Male is null ? null : keyBox.GetKeyRing(litter.Male);
        return $"insert into Litters (IdLItter, IdFemale, IdFemaleCattery, IdMale, IdMaleCattery, Date) values ({litter.Order}, {femaleKeyRing["IdCat"]}, "
            + $"{femaleKeyRing["IdCattery"]}, {(maleKeyRing is null ? "NULL" : $"{maleKeyRing!["IdCat"]}")}, "
            + $"{(maleKeyRing is null ? "NULL" : $"{maleKeyRing!["IdCattery"]}")}, '{litter.Date.ToString("yyyy-MM-dd")}');\n";
    }

    private string GetCatInsertSql(IKeyBox keyBox, Cat cat)
    {
        IKeyRing catKeyRing = keyBox.GetKeyRing(cat)!;
        IKeyRing? litterKeyRing = cat.Litter is null ? null : keyBox.GetKeyRing(cat.Litter);
        return $"insert into Cats (IdCat, IdCattery, IdBreed, IdGroup, IdLitter, IdMother, IdMotherCattery, Gender, NameEng, NameNat, OwnerInfo, Exterior, Title) "
            + $"values ({catKeyRing["IdCat"]}, {catKeyRing["IdCattery"]}, '{cat.Breed.Code}', '{cat.Breed.Group}', "
            + $"{litterKeyRing?["IdLitter"]!.ToString() ?? "NULL"}, {(litterKeyRing is null ? "NULL" : $"{litterKeyRing!["IdFemale"]}")}, "
            + $"{(litterKeyRing is null ? "NULL" : $"{litterKeyRing!["IdFemaleCattery"]}")}, "
            + $"'{cat.Gender switch { Gender.Male => "M", _ => "F" }}', '{cat.NameEng}', '{cat.NameNat}', '{cat.OwnerInfo}', '{cat.Exterior}', '{cat.Title}');\n";
    }
}
