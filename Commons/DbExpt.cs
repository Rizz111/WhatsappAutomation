using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

using WhatsappAutomation.DataContext;

public class Filter
{
    public int? NumberFrom { get; set; }
    public int? NumberTo { get; set; }
    public DateTime? DateTo { get; set; }
    public DateTime? DateFrom { get; set; }


}




public static class DbExt
{
    public static Dictionary<TKey1, Dictionary<TKey2, TValue>> Pivot3<TSource, TKey1, TKey2, TValue>(
    this IEnumerable<TSource> source
    , Func<TSource, TKey1> key1Selector
    , Func<TSource, TKey2> key2Selector
    , Func<IEnumerable<TSource>, TValue> aggregate)
    {
        return source.GroupBy(key1Selector).Select(
            x => new
            {
                X = x.Key,
                Y = source.GroupBy(key2Selector).Select(
                    z => new
                    {
                        Z = z.Key,
                        V = aggregate(from item in source
                                      where key1Selector(item).Equals(x.Key)
                                      && key2Selector(item).Equals(z.Key)
                                      select item
                        )

                    }
                ).ToDictionary(e => e.Z, o => o.V)
            }
        ).ToDictionary(e => e.X, o => o.Y);
    }

    public static List<T> GetList<T>(this DbContext db, string Query, IDbContextTransaction transaction = null) where T : class
    {
        try
        {
            if (Query != "")
            {
                var conn = db.Database.GetDbConnection();
                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();

                if (transaction != null)
                    return conn.Query<T>(Query, transaction).ToList();
                return conn.Query<T>(Query).ToList();
            }
            else
            {
                return new List<T>();
            }
        }
        catch { return new List<T>(); }
    }

    public static Task<IEnumerable<T>> GetListAsync<T>(this DbContext db, string Query, IDbContextTransaction transaction = null) where T : class
    {
        try
        {
            if (Query != "")
            {
                var conn = db.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed) conn.Open();

                return transaction != null ? conn.QueryAsync<T>(Query, transaction) : conn.QueryAsync<T>(Query);
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex) { throw new Exception("Error in Query", new Exception("Please Check the following method and Query " + $@"GetList()=> {Query}", ex)); }
    }

    public static T GetSingle<T>(this DbContext db, string Query, IDbContextTransaction transaction = null) where T : class
    {
        try
        {
            if (Query != "")
            {
                var conn = db.Database.GetDbConnection();
                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                if (transaction != null)
                    return conn.Query<T>(Query, transaction).FirstOrDefault();
                return conn.Query<T>(Query).FirstOrDefault();
            }
            else
            {
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    private static T SetNullNumericTo0<T>(T Item) where T : class
    {
        if (Item != null)
        {
            List<PropertyInfo> b = Item.GetType().GetProperties().Where(x => x.PropertyType == typeof(int?)).ToList();
            b.Where(s => s.GetValue(Item, null) == null).ToList().ForEach(s => s.SetValue(Item, 0, null));

            List<PropertyInfo> b1 = Item.GetType().GetProperties().Where(x => x.PropertyType == typeof(decimal?)).ToList();
            b1.Where(s => s.GetValue(Item, null) == null).ToList().ForEach(s => s.SetValue(Item, (decimal)0, null));

            List<PropertyInfo> b2 = Item.GetType().GetProperties().Where(x => x.PropertyType == typeof(double?)).ToList();
            b2.Where(s => s.GetValue(Item, null) == null).ToList().ForEach(s => s.SetValue(Item, (double)0, null));

            List<PropertyInfo> b3 = Item.GetType().GetProperties().Where(x => x.PropertyType == typeof(bool?)).ToList();
            b3.Where(s => s.GetValue(Item, null) == null).ToList().ForEach(s => s.SetValue(Item, false, null));

            List<PropertyInfo> b4 = Item.GetType().GetProperties().Where(x => x.PropertyType == typeof(string)).ToList();
            b4.Where(s => s.GetValue(Item, null) == null).ToList().ForEach(s => s.SetValue(Item, string.Empty, null));
        }
        return Item;
    }
    private static List<T> SetNullNumericTo0<T>(List<T> Item) where T : class
    {
        Item?.ForEach(rs =>
        {
            List<PropertyInfo> b = rs.GetType().GetProperties().Where(x => x.PropertyType == typeof(int?)).ToList();
            b.Where(s => s.GetValue(rs, null) == null).ToList().ForEach(s => s.SetValue(rs, 0, null));

            List<PropertyInfo> b1 = rs.GetType().GetProperties().Where(x => x.PropertyType == typeof(decimal?)).ToList();
            b1.Where(s => s.GetValue(rs, null) == null).ToList().ForEach(s => s.SetValue(rs, (decimal)0, null));

            List<PropertyInfo> b2 = rs.GetType().GetProperties().Where(x => x.PropertyType == typeof(double?)).ToList();
            b2.Where(s => s.GetValue(rs, null) == null).ToList().ForEach(s => s.SetValue(rs, (double)0, null));

            List<PropertyInfo> b3 = rs.GetType().GetProperties().Where(x => x.PropertyType == typeof(bool?)).ToList();
            b3.Where(s => s.GetValue(rs, null) == null).ToList().ForEach(s => s.SetValue(rs, false, null));

            List<PropertyInfo> b4 = rs.GetType().GetProperties().Where(x => x.PropertyType == typeof(string)).ToList();
            b4.Where(s => s.GetValue(rs, null) == null).ToList().ForEach(s => s.SetValue(rs, string.Empty, null));
        });
        return Item;
    }


    public static async Task<DataTable> SQLToDataTableAsync(this DbContext db, string Query)
    {
        try
        {
            var dt = new DataTable();
            if (Query != "")
            {
                var conn = db.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed) conn.Open();
                dt.Load(await conn.ExecuteReaderAsync(Query));
                return dt;
            }
            else
            {
                return dt;
            }
        }
        catch (Exception ex) { throw new Exception("Error in Query", new Exception("Please Check the following method and Query " + $@"SQLToDataTable()=> {Query}", ex)); }
    }

    public static DataTable SQLToDataTable(this DbContext db, string Query)
    {
        try
        {
            var dt = new DataTable();
            if (Query != "")
            {
                var conn = db.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed) conn.Open();
                dt.Load(conn.ExecuteReader(Query));
                return dt;
            }
            else
            {
                return dt;
            }
        }
        catch (Exception ex) { throw new Exception("Error in Query", new Exception("Please Check the following method and Query " + $@"SQLToDataTable()=> {Query}", ex)); }
    }


   
}