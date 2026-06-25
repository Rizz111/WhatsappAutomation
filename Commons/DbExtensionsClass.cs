using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace WhatsappAutomation.Context;

/// <summary>
///
/// </summary>
public static class DbExtensionsClass
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="db"></param>
    /// <param name="Query"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
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

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="db"></param>
    /// <param name="Query"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static IEnumerable<T?> GetList<T>(this DbContext db, string Query, IDbContextTransaction transaction = null) where T : class
    {
        try
        {
            if (Query != "")
            {
                var conn = db.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed) conn.Open();

                return transaction != null ? conn.Query<T?>(Query, transaction) : conn.Query<T?>(Query);
            }
            else
            {
                return new List<T>();
            }
        }
        catch (Exception ex) { throw new Exception("Error in Query", new Exception("Please Check the following method and Query " + $@"GetList()=> {Query}", ex)); }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="db"></param>
    /// <param name="Query"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
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

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="db"></param>
    /// <param name="Query"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static T? GetSingle<T>(this DbContext db, string Query, IDbContextTransaction transaction = null) where T : class
    {
        try
        {
            if (Query != "")
            {
                var conn = db.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed) conn.Open();
                if (transaction != null)
                {
                    return conn.Query<T>(Query, transaction).FirstOrDefault();
                }
                else
                {
                    return conn.Query<T>(Query).FirstOrDefault();
                }
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

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="db"></param>
    /// <param name="Query"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static Task<T?> GetSingleAsync<T>(this DbContext db, string Query, IDbContextTransaction transaction = null) where T : class
    {
        try
        {
            if (Query != "")
            {
                var conn = db.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed) conn.Open();
                if (transaction != null)
                {
                    return conn.QueryFirstAsync<T?>(Query, transaction);
                }
                else
                {
                    return conn.QueryFirstAsync<T?>(Query);
                }
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
}