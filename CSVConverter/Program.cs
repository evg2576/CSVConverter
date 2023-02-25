using System.Collections;
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
            var _result = obj.GetType().GetProperties()
                .Where(x => !x.PropertyType.IsArray
                    || (x.PropertyType.IsArray
                        && (x.GetCustomAttribute<ColumnData>() is not null
                        || x.GetCustomAttribute<CellData>() is not null)))
                .Select(x =>
                {
                    if (!x.PropertyType.IsArray) return x.GetValue(obj)?.ToString().Aggregate((a, b) => a + "," + b);
                    if (x.PropertyType.IsArray)
                    {
                        if (x.GetCustomAttribute<ColumnData>() is not null)
                        {
                            return string.Join(";", (x.GetValue(obj) as IEnumerable<object>)./*Select(x => (x as IEnumerable<object>).*/Select(x => x.ToCsvLinq()));
                        }
                        //if (x.GetCustomAttribute<CellData>() is not null)
                    }
                    return null;
                });
            //Console.WriteLine();

            //var properties = obj.GetType().GetProperties();
            //var res = properties.Where(p => !p.PropertyType.IsArray).
            //    Select(p => p.GetValue(obj)?.ToString());
            //output += string.Join(';', res);
            //if (res is not null) output += ";";

            //var arrayProperties = properties.Where(p => p.PropertyType.IsArray);
            //var customAttributes = arrayProperties.Select(p => p.GetCustomAttributes(false));
            //var hasColumnData = customAttributes.Select(x => x.Select(a => a as ColumnData)).
            //    Select(x => x.Any(t => t is not null)).
            //    Any(x => x is true);
            //var columnDataProperties = properties.Where(p => p.PropertyType.IsArray && hasColumnData);
            //var columnDataRes = columnDataProperties.Select(p => p.GetValue(obj));
            //var columnRes = columnDataRes.Select(x => ((IEnumerable)x!).
            //Cast<object>().
            //ToList());
            //var stringColumnRes = columnRes.Select(x => x.Select(p => p.ToCsvLinq()));
            //output += string.Join(';', stringColumnRes.Select(x => string.Join(";", x)));

            //var hasCellData = customAttributes.Select(x => x.Select(a => a as CellData)).
            //    Select(x => x.Any(t => t is not null)).
            //    Any(x => x is true);
            //var cellDataProperties = properties.Where(p => p.PropertyType.IsArray && hasCellData);
            //var cellDataRes = cellDataProperties.Select(p => p.GetValue(obj));
            //var cellRes = cellDataRes.Select(x => ((IEnumerable)x!).
            //Cast<object>().
            //ToList());
            //var stringCellRes = cellRes.Select(x => x.Select(p => p.ToString()));
            //output += string.Join(';', stringCellRes.Select(x => string.Join(",", x)));
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
        var objs = objects as IList<T> ?? objects.ToList();
        if (objs.Any())
            using (var sw = new StreamWriter(destination))
                foreach (var o in objs)
                    sw.WriteLine(o?.ToCsvLinq());
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