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
                if(Breed.ins is null)
                {
                    Breed.ins = breed;
                    Console.WriteLine($"Fix {breed.GetHashCode()}");
                }
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

        ObjectCache cache = _serviceProvider.GetRequiredService<ObjectCache>();

        using DbDataReader dataReader = await sqlCommand.ExecuteReaderAsync();

        while (await dataReader.ReadAsync())
        {
            if(NameRegex is null || NameRegex.IsMatch(dataReader["NameEng"].ToString()!.Trim()) || NameRegex.IsMatch(dataReader["NameNat"].ToString()!.Trim()))
            {
                Cat cat = _serviceProvider.GetRequiredService<Cat>();
                IKeyRing keyRingCat = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(cat)!;
                keyRingCat["IdCat"] = dataReader["IdCat"];
                keyRingCat["IdCattery"] = dataReader["IdCattery"];
                if (cache.TryGet(keyRingCat, out Cat? cat1))
                {
                    cat = cat1!;
                }
                else
                {
                    cache.Add(keyRingCat, cat);
                }

                cat.NameEng = dataReader["NameEng"].ToString()!.Trim();
                cat.NameNat = dataReader["NameNat"].ToString()!.Trim();
                cat.Gender = dataReader["Gender"].ToString()!.Trim() switch { "M" => Gender.Male, _ => Gender.Female };
                cat.OwnerInfo = dataReader["OwnerInfo"].ToString()!.Trim();
                cat.Exterior = dataReader["Exterior"].ToString()!.Trim();
                cat.Title = dataReader["Title"].ToString()!.Trim();

                cat.Breed = _serviceProvider.GetRequiredService<Breed>();
                if (_serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(cat.Breed) is IKeyRing keyRingBreed)
                {
                    keyRingBreed["IdBreed"] = dataReader["IdBreed"];
                    keyRingBreed["IdGroup"] = dataReader["IdGroup"];
                    if (cache.TryGet(keyRingBreed, out Breed? breed1))
                    {
                        cat.Breed = breed1!;
                    }
                    else
                    {
                        cat.Breed.NameEng = dataReader["BreedNameEng"].ToString()!.Trim();
                        cat.Breed.NameNat = dataReader["BreedNameNat"].ToString()!.Trim();
                        cache.Add(keyRingBreed, cat.Breed);
                    }
                }

                if (_serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(cat.Cattery) is IKeyRing keyRingCattery)
                {
                    keyRingCattery["IdCattery"] = dataReader["IdCattery"].ToString()!.Trim();
                    if (cache.TryGet(keyRingCattery, out Cattery? cattery1))
                    {
                        cat.Cattery = cattery1!;
                    }
                    else
                    {
                        cat.Cattery.NameEng = dataReader["CatteryNameEng"].ToString()!.Trim();
                        cat.Cattery.NameNat = dataReader["CatteryNameNat"].ToString()!.Trim();
                        cache.Add(keyRingCattery, cat.Cattery);
                    }
                }

                if (dataReader["IdLitter"] != DBNull.Value)
                {
                    cat.Litter = _serviceProvider.GetRequiredService<Litter>();
                    if (_serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(cat.Litter) is IKeyRing keyRingLitter)
                    {
                        keyRingLitter["IdLitter"] = dataReader["IdLitter"];
                        keyRingLitter["IdFemale"] = dataReader["IdMother"];
                        keyRingLitter["IdFemaleCattery"] = dataReader["IdMotherCattery"];
                        if (cache.TryGet(keyRingLitter, out Litter? litter1))
                        {
                            cat.Litter = litter1;
                        }
                        else
                        {
                            cat.Litter.Date = DateOnly.Parse(DateTime.Parse(dataReader["Date"].ToString()!.Trim()).ToString("yyyy-MM-dd"));
                            cache.Add(keyRingLitter, cat.Litter);
                        }
                    }
                }
                yield return cat;
            }
        }
    }

    private void ApplyCatListFilter(CatListFilter filterObject, SqlCommand sqlCommand, StringBuilder sb)
    {
        StringBuilder sbWhere = new();
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
            sqlCommand.Parameters.AddWithValue("Gender", filterObject.Gender switch { Gender.Male => "M", _ => "F" });
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
        if (sbWhere.Length > 0)
        {
            sb.Append(" WHERE ").Append(sbWhere);
        }

    }
}
