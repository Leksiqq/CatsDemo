using CatsModel;
using CatsModel.Filters;
using CatsUtil;
using Microsoft.Data.SqlClient;
using Net.Leksi.KeyBox;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;

namespace CatsServer;

public class Storage
{
    private readonly string _connectionString;
    private readonly IServiceProvider _serviceProvider;

    public Storage(IServiceProvider serviceProvider, string connectionString) =>
        (_connectionString, _serviceProvider) = (connectionString, serviceProvider);

    public async IAsyncEnumerable<Breed> GetBreedsAsync(BreedListFilter? filterObject)
    {
        Regex? SearchRegex = null;

        if (filterObject is { })
        {
            if (filterObject.SearchRegex is { })
            {
                SearchRegex = new(filterObject.SearchRegex, RegexOptions.IgnoreCase);
            }
        }

        using SqlConnection conn = new(_connectionString);
        await conn.OpenAsync();

        SqlCommand sqlCommand = new("select IdBreed, IdGroup, NameEng, NameNat from Breeds", conn);

        using DbDataReader dataReader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        while (await dataReader.ReadAsync())
        {
            if (
                SearchRegex is null
                || SearchRegex.IsMatch(dataReader["NameEng"].ToString()!.Trim()) || SearchRegex.IsMatch(dataReader["NameNat"].ToString()!.Trim())
                || SearchRegex.IsMatch(dataReader["IdBreed"].ToString()!.Trim()) || SearchRegex.IsMatch(dataReader["IdGroup"].ToString()!.Trim())
            )
            {
                Breed breed = _serviceProvider.GetRequiredService<Breed>();
                IKeyRing keyRing = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(breed)!;
                keyRing["IdBreed"] = dataReader["IdBreed"];
                keyRing["IdGroup"] = dataReader["IdGroup"];
                breed.NameEng = dataReader["NameEng"].ToString()!.Trim();
                breed.NameNat = dataReader["NameNat"].ToString()!.Trim();
                yield return breed;
            }
        }
    }

    internal async IAsyncEnumerable<Cattery> GetCatteriesAsync(CatteryListFilter? filterObject)
    {
        Regex? SearchRegex = null;

        if (filterObject is { })
        {
            if (filterObject.SearchRegex is { })
            {
                SearchRegex = new(filterObject.SearchRegex, RegexOptions.IgnoreCase);
            }
        }

        using SqlConnection conn = new(_connectionString);
        await conn.OpenAsync();

        SqlCommand sqlCommand = new("select IdCattery, NameEng, NameNat from Catteries", conn);

        using DbDataReader dataReader = await sqlCommand.ExecuteReaderAsync();
        while (await dataReader.ReadAsync())
        {
            if (
                SearchRegex is null
                || SearchRegex.IsMatch(dataReader["NameEng"].ToString()!.Trim()) || SearchRegex.IsMatch(dataReader["NameNat"].ToString()!.Trim())
            )
            {
                Cattery cattery = _serviceProvider.GetRequiredService<Cattery>();
                IKeyRing keyRing = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(cattery)!;
                keyRing["IdCattery"] = dataReader["IdCattery"].ToString()!.Trim();
                cattery.NameEng = dataReader["NameEng"].ToString()!.Trim();
                cattery.NameNat = dataReader["NameNat"].ToString()!.Trim();
                yield return cattery;
            }
        }
    }

    internal async IAsyncEnumerable<Cat> GetAncestorsAsync(CatListFilter filterObject)
    {
        Queue<Cat> queue = new();
        CatsUtil.ObjectCache cache = _serviceProvider.GetRequiredService<CatsUtil.ObjectCache>();
        CatListFilter filter = new();
        filter.Self = filterObject.Descendant;
        if(filter.Self is { })
        {
            await foreach (Cat cat in GetCatsAsync(filter))
            {
                queue.Enqueue(cat);
                break;
            }
            filter.Self = null;
            while (queue.Count > 0)
            {
                Cat currentDescendant = queue.Dequeue();
                for(int step = 0; step < 2; ++step)
                {
                    if(step == 0)
                    {
                        filter.Self = currentDescendant.Litter?.Female ?? null;
                    } else
                    {
                        filter.Self = currentDescendant.Litter?.Male ?? null;
                    }
                    if(filter.Self is { })
                    {
                        await foreach (Cat cat in GetCatsAsync(filter))
                        {
                            IKeyRing keyRingCat = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(cat)!;
                            if (!cache.TryGet(keyRingCat, out Cat? _))
                            {
                                queue.Enqueue(cat);
                                cache.Add<Cat>(keyRingCat, cat);
                                if (TestFilter(filterObject, cat))
                                {
                                    yield return cat;
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }
    }

    internal async IAsyncEnumerable<Cat> GetDescendantsAsync(CatListFilter filterObject)
    {
        Queue<Cat> queue = new();
        CatsUtil.ObjectCache cache = _serviceProvider.GetRequiredService<CatsUtil.ObjectCache>();
        CatListFilter filter = new();
        filter.Self = filterObject.Ancestor;
        if(filter.Self is { })
        {
            await foreach (Cat cat in GetCatsAsync(filter))
            {
                queue.Enqueue(cat);
                break;
            }
            filter.Self = null;
            while (queue.Count > 0)
            {
                Cat currentAncestor = queue.Dequeue();
                for (int step = 0; step < 2; ++step)
                {
                    if (step == 0 && (currentAncestor!.Gender is Gender.Female || currentAncestor.Gender is Gender.FemaleCastrate))
                    {
                        filter.Mother = currentAncestor;
                        filter.Father = null;
                    }
                    else if (step == 1 && (currentAncestor!.Gender is Gender.Male || currentAncestor.Gender is Gender.MaleCastrate))
                    {
                        filter.Father = currentAncestor;
                        filter.Mother = null;
                    }
                    if (filter.Mother is { } || filter.Father is { })
                    {
                        await foreach (Cat cat in GetCatsAsync(filter))
                        {
                            IKeyRing keyRingCat = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(cat)!;
                            if (!cache.TryGet(keyRingCat, out Cat? _))
                            {
                                queue.Enqueue(cat);
                                cache.Add<Cat>(keyRingCat, cat);
                                if (TestFilter(filterObject, cat))
                                {
                                    yield return cat;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private bool TestFilter(CatListFilter filterObject, Cat cat)
    {
        IKeyRing? keyRingCat = null;
        if (filterObject.Father is { })
        {
            if (keyRingCat is null)
            {
                keyRingCat = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(cat)!;
            }
            IKeyRing keyRingFather = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(filterObject.Father)!;
            if (!keyRingCat.Equals(keyRingFather))
            {
                return false;
            }
        }
        if (filterObject.Mother is { })
        {
            if (keyRingCat is null)
            {
                keyRingCat = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(cat)!;
            }
            IKeyRing keyRingMother = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(filterObject.Mother)!;
            if (!keyRingCat.Equals(keyRingMother))
            {
                return false;
            }
        }
        if (filterObject.Cattery is { })
        {
            IKeyRing keyRingFilterCattery = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(filterObject.Cattery)!;
            IKeyRing keyRingCatCattery = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(cat.Cattery)!;
            if (!keyRingCatCattery.Equals(keyRingFilterCattery))
            {
                return false;
            }
        }
        if (filterObject.Breed is { })
        {
            IKeyRing keyRingFilterBreed = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(filterObject.Breed)!;
            IKeyRing keyRingCatBreed = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(cat.Breed)!;
            if (!keyRingCatBreed.Equals(keyRingFilterBreed))
            {
                return false;
            }
        }
        if (filterObject.BornAfter is { })
        {
            if (cat.Litter is { } && cat.Litter.Date < filterObject.BornAfter)
            {
                return false;
            }
        }
        if (filterObject.BornBefore is { })
        {
            if (cat.Litter is { } && cat.Litter.Date > filterObject.BornBefore)
            {
                return false;
            }
        }
        if (filterObject.Gender is { })
        {
            if (cat.Gender != filterObject.Gender)
            {
                return false;
            }
        }
        if (filterObject.NameRegex is { })
        {
            Regex regex = new Regex(filterObject.NameRegex);
            if (!regex.IsMatch(cat.NameNat) && !regex.IsMatch(cat.NameEng))
            {
                return false;
            }
        }
        return true;
    }

    internal async IAsyncEnumerable<Cat> GetCatsAsync(CatListFilter? filterObject)
    {
        Regex? NameRegex = null;

        using SqlConnection conn = new(_connectionString);
        await conn.OpenAsync();

        SqlCommand sqlCommand = new();

        StringBuilder sb = new(@"SELECT IdCat, Cats.IdCattery, Cats.IdBreed, Cats.IdGroup, Cats.IdLitter, IdMother, IdMotherCattery, Litters.Date,
            Gender, Cats.NameEng, Cats.NameNat, 
	        OwnerInfo, Exterior, Title,
            Catteries.NameEng CatteryNameEng, Catteries.NameNat CatteryNameNat,
            Breeds.NameEng BreedNameEng, Breeds.NameNat BreedNameNat
            FROM (Cats left join Litters on Cats.IdLitter=Litters.IdLitter and Cats.IdMother=Litters.IdFemale and Cats.IdMotherCattery=Litters.IdFemaleCattery) 
                join Catteries on Catteries.IdCattery=Cats.IdCattery join Breeds on Breeds.IdBreed=Cats.IdBreed and Breeds.IdGroup=Cats.IdGroup");


        if (filterObject is { })
        {
            if (filterObject.NameRegex is { })
            {
                NameRegex = new(filterObject.NameRegex, RegexOptions.IgnoreCase);
            }
            ApplyCatListFilter(filterObject, sqlCommand, sb);
        }

        sqlCommand.CommandText = sb.ToString();
        sqlCommand.Connection = conn;

        CatsUtil.ObjectCache cache = _serviceProvider.GetRequiredService<CatsUtil.ObjectCache>();

        using DbDataReader dataReader = await sqlCommand.ExecuteReaderAsync();

        while (await dataReader.ReadAsync())
        {
            if (NameRegex is null || NameRegex.IsMatch(dataReader["NameEng"].ToString()!.Trim()) || NameRegex.IsMatch(dataReader["NameNat"].ToString()!.Trim()))
            {
                Cat cat;

                IKeyRing keyRingCat = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing<Cat>()!;
                keyRingCat["IdCat"] = dataReader["IdCat"];
                keyRingCat["IdCattery"] = dataReader["IdCattery"];

                if (!cache.TryGet(keyRingCat, out cat!))
                {
                    cat = (Cat)keyRingCat.InstantiateSource();
                    cache.Add(keyRingCat, cat);
                }

                LoadSingleCat(cache, dataReader, cat);
                yield return cat;
            }
        }
    }

    private void LoadSingleCat(CatsUtil.ObjectCache cache, DbDataReader dataReader, Cat cat)
    {
        cat.NameEng = dataReader["NameEng"].ToString()!.Trim();
        cat.NameNat = dataReader["NameNat"].ToString()!.Trim();
        cat.Gender = dataReader["Gender"].ToString()!.Trim() switch { "M" => Gender.Male, "F" => Gender.Female, "MC" => Gender.MaleCastrate, "FC" => Gender.FemaleCastrate, _ => Gender.Female };
        cat.OwnerInfo = dataReader["OwnerInfo"].ToString()!.Trim();
        cat.Exterior = dataReader["Exterior"].ToString()!.Trim();
        cat.Title = dataReader["Title"].ToString()!.Trim();

        IKeyRing keyRingBreed = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing<Breed>()!;
        keyRingBreed["IdBreed"] = dataReader["IdBreed"];
        keyRingBreed["IdGroup"] = dataReader["IdGroup"];

        if (cache.TryGet(keyRingBreed, out Breed? breed1))
        {
            cat.Breed = breed1!;
        }
        else
        {
            cat.Breed = (Breed)keyRingBreed.InstantiateSource();
            cat.Breed.NameEng = dataReader["BreedNameEng"].ToString()!.Trim();
            cat.Breed.NameNat = dataReader["BreedNameNat"].ToString()!.Trim();
            cache.Add(keyRingBreed, cat.Breed);
        }

        IKeyRing keyRingCattery = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing<Cattery>()!;
        keyRingCattery["IdCattery"] = dataReader["IdCattery"];

        if (cache.TryGet(keyRingCattery, out Cattery? cattery1))
        {
            cat.Cattery = cattery1!;
        }
        else
        {
            cat.Cattery = (Cattery)keyRingCattery.InstantiateSource();
            cat.Cattery.NameEng = dataReader["CatteryNameEng"].ToString()!.Trim();
            cat.Cattery.NameNat = dataReader["CatteryNameNat"].ToString()!.Trim();
            cache.Add(keyRingCattery, cat.Cattery);
        }

        if (dataReader["IdLitter"] != DBNull.Value)
        {
            IKeyRing keyRingLitter = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing<Litter>()!;
            keyRingLitter["IdLitter"] = dataReader["IdLitter"];
            keyRingLitter["IdFemale"] = dataReader["IdMother"];
            keyRingLitter["IdFemaleCattery"] = dataReader["IdMotherCattery"];

            if (cache.TryGet(keyRingLitter, out Litter? litter1))
            {
                cat.Litter = litter1;
            }
            else
            {
                cat.Litter = (Litter)keyRingLitter.InstantiateSource();
                cat.Litter.Date = DateOnly.Parse(DateTime.Parse(dataReader["Date"].ToString()!.Trim()).ToString("yyyy-MM-dd"));
                cache.Add(keyRingLitter, cat.Litter);
            }
        }
    }

    private void ApplyCatListFilter(CatListFilter filterObject, SqlCommand sqlCommand, StringBuilder sb)
    {
        StringBuilder sbWhere = new();
        if (filterObject.Breed is { })
        {
            sbWhere.Append("Cats.IdBreed=@IdBreed AND Cats.IdGroup=@IdGroup");
            IKeyRing keyRingBreed = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(filterObject.Breed)!;
            sqlCommand.Parameters.AddWithValue("IdBreed", keyRingBreed["IdBreed"]!);
            sqlCommand.Parameters.AddWithValue("IdGroup", keyRingBreed["IdGroup"]!);
        }
        if (filterObject.Cattery is { })
        {
            sbWhere.Append("Cats.IdCattery=@IDCattery");
            IKeyRing keyRingCattery = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(filterObject.Cattery)!;
            sqlCommand.Parameters.AddWithValue("IDCattery", keyRingCattery["IDCattery"]!);
        }
        if (filterObject.BornAfter is { })
        {
            if (sbWhere.Length > 0)
            {
                sbWhere.Append(" AND ");
            }
            sbWhere.Append("Litters.Date>=@BornAfter");
            sqlCommand.Parameters.AddWithValue("BornAfter", filterObject.BornAfter?.ToString("yyyy-MM-dd"));
        }
        if (filterObject.BornBefore is { })
        {
            if (sbWhere.Length > 0)
            {
                sbWhere.Append(" AND ");
            }
            sbWhere.Append("Litters.Date<=@BornBefore");
            sqlCommand.Parameters.AddWithValue("BornBefore", filterObject.BornBefore?.ToString("yyyy-MM-dd"));
        }
        if (filterObject.Gender is { })
        {
            if (sbWhere.Length > 0)
            {
                sbWhere.Append(" AND ");
            }
            sbWhere.Append("Gender=@Gender");
            sqlCommand.Parameters.AddWithValue("Gender", filterObject.Gender switch { Gender.Male => "M", Gender.Female => "F", _ => "C" });
        }
        if (filterObject.Self is { })
        {
            if (sbWhere.Length > 0)
            {
                sbWhere.Append(" AND ");
            }
            sbWhere.Append("IdCat=@IdCat AND Cats.IdCattery=@IdCattery");
            IKeyRing keyRingSelf = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(filterObject.Self)!;
            sqlCommand.Parameters.AddWithValue("IdCat", keyRingSelf["IdCat"]);
            sqlCommand.Parameters.AddWithValue("IdCattery", keyRingSelf["IdCattery"]);
        }
        if (filterObject.Mother is { })
        {
            if (sbWhere.Length > 0)
            {
                sbWhere.Append(" AND ");
            }
            sbWhere.Append("IdMother=@IdMother AND IdMotherCattery=@IdMotherCattery");
            IKeyRing keyRingMother = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(filterObject.Mother)!;
            sqlCommand.Parameters.AddWithValue("IdMother", keyRingMother["IdCat"]);
            sqlCommand.Parameters.AddWithValue("IdMotherCattery", keyRingMother["IdCattery"]);
        }
        if (filterObject.Father is { })
        {
            if (sbWhere.Length > 0)
            {
                sbWhere.Append(" AND ");
            }
            sbWhere.Append("Cats.IdLitter is not null AND Litters.IdMale=@IdFather AND  Litters.IdMaleCattery=@IdFatherCattery");
            IKeyRing keyRingFather = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(filterObject.Father)!;
            sqlCommand.Parameters.AddWithValue("IdFather", keyRingFather["IdCat"]);
            sqlCommand.Parameters.AddWithValue("IdFatherCattery", keyRingFather["IdCattery"]);
        }
        if (filterObject.Litter is { })
        {
            if (sbWhere.Length > 0)
            {
                sbWhere.Append(" AND ");
            }
            sbWhere.Append("Cats.IdLitter is not null AND Litters.IdLitter=@IdLitter AND Litters.IdFemale=@IdFemale AND  Litters.IdFemaleCattery=@IdFemaleCattery");
            IKeyRing keyRingLitter = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(filterObject.Litter)!;
            sqlCommand.Parameters.AddWithValue("IdLitter", keyRingLitter["IdLitter"]);
            sqlCommand.Parameters.AddWithValue("IdFemale", keyRingLitter["IdFemale"]);
            sqlCommand.Parameters.AddWithValue("IdFemaleCattery", keyRingLitter["IdFemaleCattery"]);
        }
        if (sbWhere.Length > 0)
        {
            sb.Append(" WHERE ").Append(sbWhere);
        }

    }

}
