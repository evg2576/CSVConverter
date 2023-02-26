using System.Reflection;

var cw = new CsvWriter<Library>();
cw.Write(GetLibraries(), "example.csv");

Library[] GetLibraries()
{
    Library[] libraries = new Library[10];
    Book[] books = new Book[10];
    string[] genres = new string[10];

    for (int i = 0; i < genres.Count(); i++)
    {
        genres[i] = "Genre " + i;
    }

    for (int i = 0; i < books.Count(); i++)
    {
        books[i] = new Book { Name = "Name " + i, Author = "Author " + i, Genres = genres };
    }

    for (int i = 0; i < libraries.Count(); i++)
    {
        libraries[i] = new Library();
        libraries[i].Name = "Name " + i;
        libraries[i].Adress = "Adress " + i;
        libraries[i].Rating = i * i;
        libraries[i].Books = books;
    }
    return libraries;
}

public static class Csv
{
    public static string ToCsvLinq(this object obj)
    {
        string output = "";
        try
        {
            output += obj
                .GetType()
                .GetProperties()
                .Where(x => !x.PropertyType.IsArray
                    || (x.PropertyType.IsArray
                        && (x.GetCustomAttribute<ColumnData>() is not null
                        || x.GetCustomAttribute<CellData>() is not null)))
                .Select(x =>
                {
                    return x.PropertyType.IsArray ?
                                x.GetCustomAttribute<ColumnData>() is not null ? 
                                    (x.GetValue(obj) as IEnumerable<object>)
                                    .Select(x => x.ToCsvLinq())
                                    .Aggregate((a, b) => a + ";" + b)
                                : x.GetCustomAttribute<CellData>() is not null ? 
                                        (x.GetValue(obj) as IEnumerable<object>)
                                        .Select(x => x.ToString())
                                        .Aggregate((a, b) => a + "," + b)
                                  : null
                           : x.GetValue(obj)?.ToString();
                })
                .Aggregate((a, b) => a + ";" + b);
        }
        catch (Exception)
        {
            throw;
        }
        return output;
    }

    public static string ToCsv(this object obj)
    {
        string output = "";
        var properties = obj.GetType().GetProperties();
        Array? array = null;
        for (var i = 0; i < properties.Length; i++)
            if (properties[i].PropertyType.IsArray)
            {
                object[] attributes = properties[i].GetCustomAttributes(false);
                foreach (Attribute attr in attributes)
                {
                    if (attr is ColumnData)
                    {
                        array = properties[i].GetValue(obj) as Array;
                        foreach (var item in array!)
                            output += item.ToCsv();
                    }
                    if (attr is CellData)
                    {
                        array = properties[i].GetValue(obj) as Array;
                        foreach (var item in array!)
                        {
                            output += item.ToString();
                            if (item != array.OfType<object>().Last())
                                output += ",";
                        }
                        output += ";";
                    }
                }
            }
            else
            {
                output += properties[i]?.GetValue(obj)?.ToString();
                if (i < properties.Length)
                    output += ";";
            }
        return output;
    }
}

public class CsvWriter<T>
{
    public void Write(IEnumerable<T> objects, string destination)
    {
        using (var sw = new StreamWriter(destination))
            sw.WriteLine(objects.Select(x => x?.ToCsv()).Aggregate((a, b) => a + "\n" + b));
    }
}

/// <summary>
/// Выводит массив поэлементно в ячейке
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
class CellData : Attribute { }

/// <summary>
/// Выводит массив поэлементно в ячейках
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
class ColumnData : Attribute { }

public class Library
{
    public string? Name { get; set; }
    public string? Adress { get; set; }
    public int Rating { get; set; }
    [ColumnData]
    public Book[]? Books { get; set; }
}

public class Book
{
    public string? Name { get; set; }
    public string? Author { get; set; }
    [CellData]
    public string[]? Genres { get; set; }
}