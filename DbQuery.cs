﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Collections;
using MSS = Microsoft.SqlServer.Server;
using System.Linq.Expressions;
using System.Reflection;

namespace Lab.DbQuery 
{
    public static class DbQueryExtensionMethods
    {
        public static T Get<T>(this DbDataReader reader, string colName)
        {
            T result;
            object value = reader[colName];
            if (value != DBNull.Value)
            {
                try
                {
                    TypeConverter tc = TypeDescriptor.GetConverter(typeof(T));
                    result = (T)(tc.ConvertFrom(value.ToString()));
                }
                catch
                {
                    result = default(T);
                }
            }
            else
            {
                result = default(T);
            }
            return result;
        }
    }

    #region
    internal class ReaderMappingCache<T>
    {
        private static readonly Dictionary<Identity, Func<DbDataReader, T>> _queryCache = new Dictionary<Identity, Func<DbDataReader, T>>();

        private static void SetQueryCache(Identity key, Func<DbDataReader, T> value)
        {
            lock (_queryCache) { _queryCache[key] = value; }
        }
        private static bool TryGetQueryCache(Identity key, out Func<DbDataReader, T> value)
        {
            lock (_queryCache) { return _queryCache.TryGetValue(key, out value); }
        }

        public static Func<DbDataReader, T> GetCacheInfo(Identity identity, Func<Func<DbDataReader, T>> setValue)
        {

            Func<DbDataReader, T> value;
            if (!TryGetQueryCache(identity, out value))
            {
                value = setValue();
                SetQueryCache(identity, value);
            }
            return value;

        }


    }
    internal class Identity : IEquatable<Identity>
    {
        public Identity(string[] readerFiled, Type type, IDictionary<string, object> customerMapping)
        {
            this.readerFiled = readerFiled;

            this.type = type;

            this.customerMapping = customerMapping;

            unchecked
            {
                hashCode = 17;

                if (readerFiled != null && readerFiled.Count() > 0)
                {
                    foreach (var t in readerFiled)
                    {
                        hashCode = hashCode * 23 + (t == null ? 0 : t.GetHashCode());
                    }
                }
                if (customerMapping != null && customerMapping.Count > 0)
                {
                    foreach (var item in customerMapping)
                    {
                        hashCode = hashCode * 23 + item.Key.GetHashCode() + item.Value.GetHashCode();
                    }
                }

                hashCode = hashCode * 23 + (type == null ? 0 : type.GetHashCode());
            }
        }


        public override bool Equals(object obj)
        {
            return Equals(obj as Identity);
        }
        private readonly string[] readerFiled;
        private readonly int hashCode;
        private readonly IDictionary<string, object> customerMapping;
        private readonly Type type;

        public override int GetHashCode()
        {
            return hashCode;
        }
        public bool Equals(Identity other)
        {

            return type == other.type
                && readerFiled.SequenceEqual(other.readerFiled)
                && customerMapping.SequenceEqual(other.customerMapping);
        }
    }
    #endregion

    public class ExecuteQueryOptional
    {
        public object CustomerMapping { get; set; }
        public object Parameters { get; set; }
    }
    public class ExecuteQueryOptional<T> : ExecuteQueryOptional
    {
        public Func<DbDataReader, T> ReaderFunc { get; set; }
    }
    public abstract class DbQuery
    {
        
        private bool IsSimpleType<T>()
        {
            return TypeDescriptor.GetConverter(typeof(T)).CanConvertFrom(typeof(string));
        }
        private bool IsSimpleType(object value)
        {
            return TypeDescriptor.GetConverter(value.GetType()).CanConvertFrom(typeof(string));
        }
        private IDictionary<string, object> AnonymousObjectToDictionary(object anonymousObject)
        {
            IDictionary<string, object> result = new Dictionary<string, object>(10);
            if (anonymousObject != null)
            {
                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(anonymousObject))
                {
                    result.Add(property.Name, property.GetValue(anonymousObject));
                }
            }
            return result;
        }

        private DbCommand SetupCommand(DbConnection connection, string commandText, object parameters)
        {
            var command = connection.CreateCommand();

            var paramsDictionary = AnonymousObjectToDictionary(parameters);

            var dbParameters = new List<DbParameter>(paramsDictionary.Count);

            foreach (var item in paramsDictionary)
            {
                if (item.Value == null)
                {
                    var dbParam = command.CreateParameter();
                    dbParam.Value = DBNull.Value;
                    dbParam.ParameterName = string.Format("@{0}", item.Key);
                    command.Parameters.Add(dbParam);
                }
                else if (item.Value is ICollection)
                {
                    int enumeratorCount = 0;
                    var listParams=new List<string>(10);

                    var enumerator = (item.Value as IEnumerable).GetEnumerator();
                    using (enumerator as IDisposable)
                    {
                        while (enumerator.MoveNext())
                        {
                            var listParam=string.Format("@{0}#{1}", item.Key, enumeratorCount);

                            var dbParam = command.CreateParameter();
                            dbParam.Value = enumerator.Current;
                            dbParam.ParameterName = listParam;
                            dbParam.DbType = MSS.SqlMetaData.InferFromValue(enumerator.Current, dbParam.ParameterName).DbType;
                            if (enumerator.Current is string && ((string)enumerator.Current).Length <= 4000)
                                dbParam.Size = 4000;
                            command.Parameters.Add(dbParam);

                            listParams.Add(listParam);

                            enumeratorCount++;
                        }
                    }
                    commandText = Regex.Replace(commandText, 
                                               string.Format("@{0}", item.Key), string.Join(",", listParams.ToArray()),
                                               RegexOptions.IgnoreCase);
                }
                else
                {
                    var dbParam = command.CreateParameter();
                    dbParam.Value = item.Value;
                    dbParam.ParameterName = string.Format("@{0}", item.Key);
                    dbParam.DbType = MSS.SqlMetaData.InferFromValue(item.Value, dbParam.ParameterName).DbType;
                    if (item.Value is string && ((string)item.Value).Length <= 4000)
                        dbParam.Size = 4000;
                    command.Parameters.Add(dbParam);
                }
            }

            command.CommandType = CommandType.Text;
            command.CommandText = commandText;
            return command;

        }

        private Func<DbDataReader, T> DynamicMappingInternal<T>(string[] fileName, IDictionary<string, object> customerMapping)
        {

            ParameterExpression r = Expression.Parameter(typeof(DbDataReader), "dr");
            Expression<Func<DbDataReader, T>> lambda;
            if (IsSimpleType<T>())//IsSimpleType
            {

                Expression body = Expression.Call(typeof(DbQueryExtensionMethods).GetMethod("Get").MakeGenericMethod(typeof(T)),
                                                  new Expression[] { r, Expression.Constant(fileName.First())});
                lambda = Expression.Lambda<Func<DbDataReader, T>>(body, r);
                return lambda.Compile();
            }

            List<MemberBinding> bindings = new List<MemberBinding>();
            foreach (PropertyInfo property in (typeof(T).GetProperties()))
            {
                string mappedPropertyName;
                customerMapping.ToDictionary(c=>c.Key,c=>c.Value.ToString())
                               .TryGetValue(property.Name.ToLower(), out mappedPropertyName);
                if (string.IsNullOrEmpty(mappedPropertyName))
                {
                    mappedPropertyName = property.Name.ToLower();
                }
                if (fileName.Contains(mappedPropertyName))
                {
                    MethodCallExpression propertyValue = Expression.Call(
                        typeof(DbQueryExtensionMethods).GetMethod("Get").MakeGenericMethod(property.PropertyType),
                        r, Expression.Constant(mappedPropertyName));
                    MemberBinding binding = Expression.Bind(property, propertyValue);
                    bindings.Add(binding);
                }
            }
            Expression initializer = Expression.MemberInit(Expression.New(typeof(T)), bindings);
            lambda = Expression.Lambda<Func<DbDataReader, T>>(initializer, r);
            return lambda.Compile();
        }

        private Func<DbDataReader, T> DynamicMapping<T>(DbDataReader reader, object customerMapping)
        {
            var customerMappingDic = AnonymousObjectToDictionary(customerMapping);
            string[] readerFiled = Enumerable.Range(0, reader.FieldCount)
                                 .Select(f => reader.GetName(f).ToLower()).ToArray();
            return ReaderMappingCache<T>.GetCacheInfo(new Identity(readerFiled, typeof(T), customerMappingDic),
                                                                   () => DynamicMappingInternal<T>(readerFiled, customerMappingDic));
                                                   
        }
        public int ExecuteCommand(string connectionString, string command)
        {
            return ExecuteCommand(connectionString, command, null);
        }
        public int ExecuteCommand(string connectionString, string command, object parameters)
        {
            using (var conn = GetDbConnection(connectionString))
            {
                var comm = SetupCommand(conn, command, parameters);
                return comm.ExecuteNonQuery();
            }
        }

      
        public IEnumerable<T> ExecuteQuery<T>(string connectionString, string command, ExecuteQueryOptional optional)
        {
         
            using (var reader = GetDbDataReader(connectionString, command, optional.Parameters))
            {
                Func<DbDataReader, T> readerFunc = null;
                if (optional is ExecuteQueryOptional<T>)
                {
                    readerFunc = ((ExecuteQueryOptional<T>)optional).ReaderFunc;
                }
                readerFunc = readerFunc ?? DynamicMapping<T>(reader, optional.CustomerMapping);

                while (reader.Read())
                {
                    yield return readerFunc(reader);
                }
            }
        }

        public DbDataReader GetDbDataReader(string connectionString, string command, object parameters = null)
        {
            var conn = GetDbConnection(connectionString);
            var comm = SetupCommand(conn, command, parameters);
            return comm.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public abstract  DbConnection GetDbConnection(string connectionString);
    }
    public sealed class SQlDbQuery : DbQuery
    {
        public static DbQuery Current
        {
            get { return new SQlDbQuery(); }
        }

        public override DbConnection GetDbConnection(string connectionString)
        {
            var conn = new SqlConnection(connectionString);
            conn.Open();
            return conn;
        }
    }

   


   

}
