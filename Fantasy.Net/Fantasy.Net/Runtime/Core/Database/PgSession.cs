#if FANTASY_NET
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using Npgsql;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy.Database
{
    /// <summary>
    /// 选择操作SQL数据库时用哪个ORM。 Dapper略快但会牺牲高级特性。
    /// </summary>
    public enum PreferSqlMode
    {
        /// <summary>
        /// 更青睐用EFCore
        /// </summary>
        EFCore,
        /// <summary>
        /// 更青睐用Dapper
        /// </summary>
        Dapper
    }

    /// <summary>
    /// 【PgSession是 PgSql 数据库的操作会话, 继承自EFCore 的 DbContext。适用于PgSQL的 CRUD 操作。】
    /// </summary>
    /// 
    /// 【注意, 每个 PgSession 会交由EFCore构建一个“Entity - Table”模型】
    ///  模型初始化时自动构建, 构建后无法运行时变动! 无法HotUpdate！无法检测到动态新建的表、无法感知热更新建的字段！ 
    ///  因此，对于已上线的业务，如果需要无感更新业务数据, (典型的情况是中途给实体新增字段 )，
    ///  请务必考虑采用【蓝绿部署】结合"数据库迁移策略"；
    ///  另一种可能有效的运行时让服务无感交接的解决办法是：继承并实现额外的DbContext，比如实现一组临时的 TempSession 与 TempSessionUnPooled ，专门用于“Entity - Table”模型的热交接（但即便如此, 等到合适的时机依然需要重启服务器，构建新的模型）。
    ///  
    /// 【关于 PgSession 的池化机制】 由ServiceProvider依赖注入进行池化，
    /// 具体详情请见 PostgreSQL 初始化时调用的 ServiceCollection.AddDbContextPool() 方法，
    /// 微软在该方法中对 DbContext 的池化做了详细注释。
    /// 
    /// 【关于 ORM 性能】PgSession 基于 Dapper和EFCore 封装。
    /// 从性能来考虑, 4种 SQL 操作方式排名如下——
    /// 1. 原生SQL: 性能最优, 但没有 ORM 字段自动映射; 
    /// 2. Dapper: 相当于原生SQL + 自动字段映射, 性能次之;
    /// 3. EFCore: 如果使用 FromSqlRaw , 性能可能与 Dapper 相当;
    /// 4. EFCore一般情况: 适合多人合作开发, 提供高级特性, 如导航属性、LINQ 、Change Tracking 、事务管理。
    /// 当前封装，结合了1 2 3 4，以寻求均衡。
    /// 为什么当前不用号称速度更快的 ORM 框架，比如SQLSugar ？考虑到，现在AI时代, 复杂查询可以让AI生成最优的原生SQL执行, ORM 的性能不必须作为核心考虑项。  
    /// 而且许多数据库的高级用法，更常发生在响应非实时的后运营阶段、离线处理阶段，这时SQL执行效率并不追求极限，EFCore完全足以胜任绝大多数状况。
    ///
    public partial class PgSession : DbContext, IDbSession, IAssemblyLifecycle
    {
        /// <summary>
        /// Pg的实例引用
        /// </summary>
        private PostgreSQL pg;
        /// <summary>
        /// 设置Pg实例引用
        /// </summary>
        /// <param name="Pg"></param>
        public void SetPg(PostgreSQL Pg) {
            pg = Pg;
        }

        /// <summary>
        /// 构造函数, 就这样继承就可以了 
        /// </summary>
        /// <param name="options"></param>
        public PgSession(DbContextOptions options) : base(options)
        {

        }

        /// <summary>
        /// 非池中取得的PgSession, 劣势: 每次使用都会产生新实例, 相比池化, 会施加轻微GC压力。优势: 绝对的状态安全。
        /// 调用 PostgreSQL.Use() 时，传入参数设置未非池化， 即可获取 PgSessionUnPooled 实例。
        /// </summary>
        public class PgSessionUnPooled : PgSession
        {
            /// <summary>
            /// 构造方法
            /// </summary>
            /// <param name="options"></param>
            public PgSessionUnPooled(DbContextOptions<PgSessionUnPooled> options) : base(options)
            {
            }
        }

        /// <summary>
        ///  " Entity - Table" 映射模型构建阶段。
        ///  在 DbContext 首次实例化的时候会自动检查是否建构过模型，如果检测到从未建构过，OnModelCreating 就会生效。  
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Log.Info("\" Entity - Table\" Model is Creating...");

            DbAttrHelper.ScanFantasyDbSetTypes((type, tableName,attr) => {
                if (attr.IfSelectionContainsDbType(DatabaseType.PostgreSQL))
                {
                    Log.Debug($"Registering entity: {type.FullName} -> table {tableName}");
                    modelBuilder.Entity(type).ToTable(tableName);
                }
            });

            // 剔除属性导航
            //modelBuilder.Model.GetEntityTypes()
            //    .Where(t => !t.ClrType.GetCustomAttributes(typeof(TableAttribute), true).Any() &&
            //                !t.ClrType.GetCustomAttributes(typeof(FantasyTableAttribute), true).Any())
            //    .ToList()
            //    .ForEach(t => modelBuilder.Ignore(t.ClrType));

            //foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            //{
            //    Log.Warning($"EFCore 全部注册的实体: {entityType.ClrType.FullName}");
            //}

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assemblyManifest"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async FTask OnLoad(AssemblyManifest assemblyManifest)
        {
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assemblyManifest"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>

        public async FTask OnUnload(AssemblyManifest assemblyManifest)
        {
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 释放
        /// </summary>
        public override void Dispose()
        {
            Mode = PreferSqlMode.EFCore;
            pg = null;
            base.Dispose();
        }

        /// <summary>
        /// 异步释放
        /// </summary>
        public override async ValueTask DisposeAsync()
        {
            Mode = PreferSqlMode.EFCore;
            pg = null;
            await base.DisposeAsync();
        }

        /// <summary>
        /// Sql 操作倾向模式选择, 默认选择是 EFCore, 倾向于使用 EFCore。
        /// !! 需特别注意, 调用属于 IDbSession接口中的查询方法时， 使用Dapper模式, 会有一定的性能提升, 但这样就导致默认不会被 EFCore 框架自动跟踪属性变化了 !!
        /// </summary>
        public PreferSqlMode Mode { get; set; } = PreferSqlMode.EFCore;    
     
        #region Table

        /// <summary>
        /// PgSQL中表名即类型名, 如果通过 OnModelCreating 配置了指定表名, 才需用自定义名。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <returns>表名。</returns>
        private string GetTableName<T>(string table = null) where T : Entity
        {
            return table ?? typeof(T).Name;
        }

        /// <summary>
        /// 从表达式中获取列名。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="expression">字段表达式。</param>
        /// <returns>列名。</returns>
        private string GetColumnName<T>(Expression<Func<T, object>> expression)
        {
            // if (expression.Body is MemberExpression memberExpression)
            // {
            //     return memberExpression.Member.Name;
            // }
            // else if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression)
            // {
            //     return ((MemberExpression)unaryExpression.Operand).Member.Name;
            // }
            return string.Empty;
        }

        /// <summary>
        /// 将LINQ表达式转换为SQL WHERE子句。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="expression">LINQ表达式。</param>
        /// <returns>SQL WHERE子句。</returns>
        private string GetWhereClause<T>(Expression<Func<T, bool>> expression)
        {
            // 这里应该实现LINQ表达式到SQL的转换
            // 为简化，返回一个占位符
            return "1=1";
        }

        #endregion

        #region Count

        ///// <summary>
        ///// 统计指定表中的总行数。
        ///// </summary>
        ///// <typeparam name="T">实体类型。</typeparam>
        ///// <param name="table">表名称，可选。如果未指定，将使用实体类型的名称。</param>
        ///// <param name="dbContext">上下文，可选。</param>
        ///// <returns>总行数。</returns>
        //public async FTask<long> Count<T>(string table = null,DbContext dbContext = null) where T : Entity
        //{
        //    var tableName = GetTableName<T>(table);
        //    var connection = Handler.CreateConnection();
        //    try
        //    {
        //        await connection.OpenAsync(); 
        //        using (var cmd = connection.CreateCommand())
        //        {
        //            tableName = tableName.Replace("\"", "\"\"");
        //            cmd.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\"";
        //            var result = await cmd.ExecuteScalarAsync();
        //            return Convert.ToInt64(result);
        //        }
        //    }
        //    finally
        //    {
        //        await connection.CloseAsync();
        //    }
        //}

        /// <summary>
        /// 自行执行一句原生SQL语句
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        private async FTask<object?> ExecuteRawSqlOnceDirectly(string sql) {

            var connection = await GetOpenedConnection();//确保开启连接
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            return await cmd.ExecuteScalarAsync();
        }

        /// <summary>
        /// 获取数据库连接, 并确保连接已打开。
        /// </summary>
        /// <returns></returns>
        public async FTask<DbConnection> GetOpenedConnection() {
            var _connection = Database.GetDbConnection();
            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();
            return _connection;
        }

        /// <summary>
        /// 统计指定表中的总行数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="table">表名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>总行数。</returns>
        public async FTask<long> Count<T>(string table = null) where T : Entity
        {
            var tableName = GetTableName<T>(table);                     

            string sql = $"SELECT COUNT(*) FROM \"{tableName}\"";

            switch (Mode)
            {
                case PreferSqlMode.EFCore:
                    {
                        return await Database.SqlQueryRaw<long>(sql).FirstOrDefaultAsync();
                    }
                default:
                    {
                        var result = ExecuteRawSqlOnceDirectly(sql);
                        return Convert.ToInt64(result);
                    }
            }
        }

        /// <summary>
        /// 统计指定表中满足条件的行数量。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="filter">用于筛选行的条件。</param>
        /// <param name="table">表名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>满足条件的行数量。</returns>
        public async FTask<long> Count<T>(Expression<Func<T, bool>> filter, string table = null) where T : Entity
        {
            return await Set<T>().CountAsync(filter);
        }

        /// <summary>
        /// 快速估算表行数, 适合估算大表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <returns></returns>
        public async FTask<long> FastCount<T>(string table = null) where T : Entity
        {
            var tableName = GetTableName<T>(table); 
            var sql = $"SELECT reltuples::bigint FROM pg_class WHERE relname = '{tableName}'";

            switch (Mode)
            {
                case PreferSqlMode.EFCore:
                    {
                        var result = await Database.SqlQueryRaw<long>(sql).FirstOrDefaultAsync();
                        return result;
                    }
                default:
                    {
                        var result = await ExecuteRawSqlOnceDirectly(sql);
                        return Convert.ToInt64(result);
                    }
            }
        }


        #endregion

        #region Exist

        /// <summary>
        /// 判断指定表中是否存在行。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="table">表名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>如果存在行则返回 true，否则返回 false。</returns>
        public async FTask<bool> Exist<T>(string table = null) where T : Entity
        {
            // return await Count<T>(table) > 0;
            await FTask.CompletedTask;
            return false;
        }

        /// <summary>
        /// 判断指定表中是否存在满足条件的行。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="filter">用于筛选行的条件。</param>
        /// <param name="table">表名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>如果存在满足条件的行则返回 true，否则返回 false。</returns>
        public async FTask<bool> Exist<T>(Expression<Func<T, bool>> filter, string table = null) where T : Entity
        {
            // return await Count(filter, table) > 0;
            await FTask.CompletedTask;
            return false;
        }

        #endregion

        #region Query

        /// <summary>
        /// 在不加数据库锁定的情况下，查询指定 ID 的行。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="id">要查询的行 ID。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>查询到的行。</returns>
        public async FTask<T> QueryNotLock<T>(long id, bool isDeserialize = false, string table = null) where T : Entity
        {
            // var tableName = GetTableName<T>(table);
            // 
            // using (await _dataBaseLock.Wait(id))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE \"Id\" = @Id";
            //         cmd.Parameters.AddWithValue("Id", id);
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             if (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entity = _serializer.Deserialize<T>(bsonDocument);
            //                 
            //                 if (isDeserialize && entity != null)
            //                 {
            //                     entity.Deserialize(_scene);
            //                 }
            //                 
            //                 await _connection.CloseAsync();
            //                 return entity;
            //             }
            //         }
            //     }
            //     await _connection.CloseAsync();
            //     return null;
            // }
            await FTask.CompletedTask;
            return null;
        }

        /// <summary>
        /// 查询指定 ID 的行，并加数据库锁定以确保数据一致性。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="id">要查询的行 ID。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>查询到的行。</returns>
        public async FTask<T> Query<T>(long id, bool isDeserialize = false, string table = null) where T : Entity
        {
            // var tableName = GetTableName<T>(table);
            // 
            // using (await _dataBaseLock.Wait(id))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE \"Id\" = @Id";
            //         cmd.Parameters.AddWithValue("Id", id);
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             if (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entity = _serializer.Deserialize<T>(bsonDocument);
            //                 
            //                 if (isDeserialize && entity != null)
            //                 {
            //                     entity.Deserialize(_scene);
            //                 }
            //                 
            //                 await _connection.CloseAsync();
            //                 return entity;
            //             }
            //         }
            //     }
            //     await _connection.CloseAsync();
            //     return null;
            // }
            await FTask.CompletedTask;
            return null;
        }

        /// <summary>
        /// 通过分页查询并返回满足条件的行数量和日期列表（不加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">每页大小。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行数量和日期列表。</returns>
        public async FTask<(int count, List<T> dates)> QueryCountAndDatesByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, bool isDeserialize = false, string table = null) where T : Entity
        {
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     var count = await Count(filter);
            //     var dates = await QueryByPage(filter, pageIndex, pageSize, isDeserialize, table);
            //     return ((int)count, dates);
            // }
            await FTask.CompletedTask;
            return (0, new List<T>());
        }

        /// <summary>
        /// 通过分页查询并返回满足条件的行数量和日期列表（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">每页大小。</param>
        /// <param name="cols">要查询的列名称数组。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行数量和日期列表。</returns>
        public async FTask<(int count, List<T> dates)> QueryCountAndDatesByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, string[] cols, bool isDeserialize = false, string table = null) where T : Entity
        {
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     var count = await Count(filter);
            //     var dates = await QueryByPage(filter, pageIndex, pageSize, cols, isDeserialize, table);
            //     return ((int)count, dates);
            // }
            await FTask.CompletedTask;
            return (0, new List<T>());
        }

        /// <summary>
        /// 通过分页查询并返回满足条件的行列表（不加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">每页大小。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> QueryByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, bool isDeserialize = false, string table = null) where T : Entity
        {
            // var tableName = GetTableName<T>(table);
            // var whereClause = GetWhereClause(filter);
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE {whereClause} LIMIT {pageSize} OFFSET {(pageIndex - 1) * pageSize}";
            //         var list = new List<T>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entity = _serializer.Deserialize<T>(bsonDocument);
            //                 list.Add(entity);
            //             }
            //         }
            //         
            //         if (isDeserialize && list.Count > 0)
            //         {
            //             foreach (var entity in list)
            //             {
            //                 entity.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 通过分页查询并返回满足条件的行列表（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">每页大小。</param>
        /// <param name="cols">要查询的列名称数组。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> QueryByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, string[] cols, bool isDeserialize = false, string table = null) where T : Entity
        {
            // var tableName = GetTableName<T>(table);
            // var whereClause = GetWhereClause(filter);
            // var columns = cols != null && cols.Length > 0 ? string.Join(", ", cols.Select(c => $"\"{c}\"")) : "*";
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT {columns} FROM \"{tableName}\" WHERE {whereClause} LIMIT {pageSize} OFFSET {(pageIndex - 1) * pageSize}";
            //         var list = new List<T>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entity = _serializer.Deserialize<T>(bsonDocument);
            //                 list.Add(entity);
            //             }
            //         }
            //         
            //         if (isDeserialize && list.Count > 0)
            //         {
            //             foreach (var entity in list)
            //             {
            //                 entity.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 通过分页查询并返回满足条件的行列表，并按指定表达式进行排序（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">每页大小。</param>
        /// <param name="orderByExpression">排序表达式。</param>
        /// <param name="isAsc">是否升序排序。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> QueryByPageOrderBy<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, Expression<Func<T, object>> orderByExpression, bool isAsc = true, bool isDeserialize = false, string table = null) where T : Entity
        {
            // var tableName = GetTableName<T>(table);
            // var whereClause = GetWhereClause(filter);
            // var orderByColumn = GetColumnName(orderByExpression);
            // var orderDirection = isAsc ? "ASC" : "DESC";
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE {whereClause} ORDER BY \"{orderByColumn}\" {orderDirection} LIMIT {pageSize} OFFSET {(pageIndex - 1) * pageSize}";
            //         var list = new List<T>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entity = _serializer.Deserialize<T>(bsonDocument);
            //                 list.Add(entity);
            //             }
            //         }
            //         
            //         if (isDeserialize && list.Count > 0)
            //         {
            //             foreach (var entity in list)
            //             {
            //                 entity.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 通过指定过滤条件查询并返回满足条件的第一个行（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的第一个行，如果未找到则为 null。</returns>
        public async FTask<T?> First<T>(Expression<Func<T, bool>> filter, bool isDeserialize = false, string table = null) where T : Entity
        {
            // var tableName = GetTableName<T>(table);
            // var whereClause = GetWhereClause(filter);
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE {whereClause} LIMIT 1";
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             if (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entity = _serializer.Deserialize<T>(bsonDocument);
            //                 
            //                 if (isDeserialize && entity != null)
            //                 {
            //                     entity.Deserialize(_scene);
            //                 }
            //                 
            //                 await _connection.CloseAsync();
            //                 return entity;
            //             }
            //         }
            //     }
            //     await _connection.CloseAsync();
            //     return null;
            // }
            await FTask.CompletedTask;
            return null;
        }

        /// <summary>
        /// 通过指定 JSON 格式查询并返回满足条件的第一个行（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="json">JSON 查询条件。</param>
        /// <param name="cols">要查询的列名称数组。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的第一个行。</returns>
        public async FTask<T> First<T>(string json, string[] cols, bool isDeserialize = false, string table = null) where T : Entity
        {
            // var tableName = GetTableName<T>(table);
            // var columns = cols != null && cols.Length > 0 ? string.Join(", ", cols.Select(c => $"\"{c}\"")) : "*";
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         // 这里需要将JSON条件转换为SQL WHERE子句
            //         cmd.CommandText = $"SELECT {columns} FROM \"{tableName}\" WHERE {ConvertJsonToWhereClause(json)} LIMIT 1";
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             if (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entity = _serializer.Deserialize<T>(bsonDocument);
            //                 
            //                 if (isDeserialize && entity != null)
            //                 {
            //                     entity.Deserialize(_scene);
            //                 }
            //                 
            //                 await _connection.CloseAsync();
            //                 return entity;
            //             }
            //         }
            //     }
            //     await _connection.CloseAsync();
            //     return null;
            // }
            await FTask.CompletedTask;
            return null;
        }

        /// <summary>
        /// 通过指定过滤条件查询并返回满足条件的行列表，并按指定表达式进行排序（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="orderByExpression">排序表达式。</param>
        /// <param name="isAsc">是否升序排序。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> QueryOrderBy<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderByExpression, bool isAsc = true, bool isDeserialize = false, string table = null) where T : Entity
        {
            // var tableName = GetTableName<T>(table);
            // var whereClause = GetWhereClause(filter);
            // var orderByColumn = GetColumnName(orderByExpression);
            // var orderDirection = isAsc ? "ASC" : "DESC";
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE {whereClause} ORDER BY \"{orderByColumn}\" {orderDirection}";
            //         var list = new List<T>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entity = _serializer.Deserialize<T>(bsonDocument);
            //                 list.Add(entity);
            //             }
            //         }
            //         
            //         if (isDeserialize && list.Count > 0)
            //         {
            //             foreach (var entity in list)
            //             {
            //                 entity.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 通过指定过滤条件查询并返回满足条件的行列表（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> Query<T>(Expression<Func<T, bool>> filter, bool isDeserialize = false, string table = null) where T : Entity
        {
            // var tableName = GetTableName<T>(table);
            // var whereClause = GetWhereClause(filter);
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE {whereClause}";
            //         var list = new List<T>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entity = _serializer.Deserialize<T>(bsonDocument);
            //                 list.Add(entity);
            //             }
            //         }
            //         
            //         if (isDeserialize && list.Count > 0)
            //         {
            //             foreach (var entity in list)
            //             {
            //                 entity.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 根据指定 ID 加锁查询多个表中的行。
        /// </summary>
        /// <param name="id">行 ID。</param>
        /// <param name="tableNames">要查询的表名称列表。</param>
        /// <param name="result">查询结果存储列表。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        public async FTask Query(long id, List<string>? tableNames, List<Entity> result, bool isDeserialize = false)
        {
            // if (tableNames == null || tableNames.Count == 0)
            // {
            //     return;
            // }
            // 
            // using (await _dataBaseLock.Wait(id))
            // {
            //     await _connection.OpenAsync();
            //     foreach (var tableName in tableNames)
            //     {
            //         using (var cmd = _connection.CreateCommand())
            //         {
            //             cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE \"Id\" = @Id";
            //             cmd.Parameters.AddWithValue("Id", id);
            //             using (var reader = await cmd.ExecuteReaderAsync())
            //             {
            //                 if (await reader.ReadAsync())
            //                 {
            //                     var bsonDocument = GetBsonDocumentFromReader(reader);
            //                     var entityType = Type.GetType($"Fantasy.Entities.{tableName}, Fantasy");
            //                     if (entityType != null && typeof(Entity).IsAssignableFrom(entityType))
            //                     {
            //                         var entity = _serializer.Deserialize(entityType, bsonDocument) as Entity;
            //                         if (isDeserialize && entity != null)
            //                         {
            //                             entity.Deserialize(_scene);
            //                         }
            //                         result.Add(entity);
            //                     }
            //                 }
            //             }
            //         }
            //     }
            //     await _connection.CloseAsync();
            // }
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 根据指定的 JSON 查询条件查询并返回满足条件的行列表（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="json">JSON 查询条件。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> QueryJson<T>(string json, bool isDeserialize = false, string table = null) where T : Entity
        {
            // var tableName = GetTableName<T>(table);
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         // 这里需要将JSON条件转换为SQL WHERE子句
            //         cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE {ConvertJsonToWhereClause(json)}";
            //         var list = new List<T>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entity = _serializer.Deserialize<T>(bsonDocument);
            //                 list.Add(entity);
            //             }
            //         }
            //         
            //         if (isDeserialize && list.Count > 0)
            //         {
            //             foreach (var entity in list)
            //             {
            //                 entity.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 根据指定的 JSON 查询条件查询并返回满足条件的行列表，并选择指定的列（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="json">JSON 查询条件。</param>
        /// <param name="cols">要查询的列名称数组。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> QueryJson<T>(string json, string[] cols, bool isDeserialize = false, string table = null) where T : Entity
        {
            // var tableName = GetTableName<T>(table);
            // var columns = cols != null && cols.Length > 0 ? string.Join(", ", cols.Select(c => $"\"{c}\"")) : "*";
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         // 这里需要将JSON条件转换为SQL WHERE子句
            //         cmd.CommandText = $"SELECT {columns} FROM \"{tableName}\" WHERE {ConvertJsonToWhereClause(json)}";
            //         var list = new List<T>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entity = _serializer.Deserialize<T>(bsonDocument);
            //                 list.Add(entity);
            //             }
            //         }
            //         
            //         if (isDeserialize && list.Count > 0)
            //         {
            //             foreach (var entity in list)
            //             {
            //                 entity.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 根据指定的 JSON 查询条件和任务 ID 查询并返回满足条件的行列表（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="taskId">任务 ID。</param>
        /// <param name="json">JSON 查询条件。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> QueryJson<T>(long taskId, string json, bool isDeserialize = false, string table = null) where T : Entity
        {
            // var tableName = GetTableName<T>(table);
            // 
            // using (await _dataBaseLock.Wait(taskId))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         // 这里需要将JSON条件转换为SQL WHERE子句
            //         cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE {ConvertJsonToWhereClause(json)}";
            //         var list = new List<T>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entity = _serializer.Deserialize<T>(bsonDocument);
            //                 list.Add(entity);
            //             }
            //         }
            //         
            //         if (isDeserialize && list.Count > 0)
            //         {
            //             foreach (var entity in list)
            //             {
            //                 entity.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 根据指定过滤条件查询并返回满足条件的行列表，选择指定的列（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="cols">要查询的列名称数组。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> Query<T>(Expression<Func<T, bool>> filter, string[] cols, bool isDeserialize = false, string table = null) where T : Entity
        {
            // var tableName = GetTableName<T>(table);
            // var whereClause = GetWhereClause(filter);
            // var columns = cols != null && cols.Length > 0 ? string.Join(", ", cols.Select(c => $"\"{c}\"")) : "*";
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT {columns} FROM \"{tableName}\" WHERE {whereClause}";
            //         var list = new List<T>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entity = _serializer.Deserialize<T>(bsonDocument);
            //                 list.Add(entity);
            //             }
            //         }
            //         
            //         if (isDeserialize && list.Count > 0)
            //         {
            //             foreach (var entity in list)
            //             {
            //                 entity.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 根据指定过滤条件查询并返回满足条件的行列表，选择指定的列（加锁）。
        /// </summary>
        /// <param name="filter">文档实体类型。</param>
        /// <param name="cols">查询过滤条件。</param>
        /// <param name="isDeserialize">要查询的列名称数组。</param>
        /// <param name="table">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <typeparam name="T">表名称。</typeparam>
        /// <returns></returns>
        public async FTask<List<T>> Query<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>>[] cols, bool isDeserialize = false, string table = null) where T : Entity
        {
            // var tableName = GetTableName<T>(table);
            // var whereClause = GetWhereClause(filter);
            // var columns = new List<string> { "\"Id\"" }; // 确保包含Id列
            // 
            // foreach (var col in cols)
            // {
            //     if (col.Body is MemberExpression memberExpression)
            //     {
            //         columns.Add($"\"{memberExpression.Member.Name}\"");
            //     }
            //     else if (col.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression)
            //     {
            //         columns.Add($"\"{((MemberExpression)unaryExpression.Operand).Member.Name}\"");
            //     }
            // }
            // 
            // var columnsStr = string.Join(", ", columns);
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT {columnsStr} FROM \"{tableName}\" WHERE {whereClause}";
            //         var list = new List<T>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entity = _serializer.Deserialize<T>(bsonDocument);
            //                 list.Add(entity);
            //             }
            //         }
            //         
            //         if (isDeserialize && list.Count > 0)
            //         {
            //             foreach (var entity in list)
            //             {
            //                 entity.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        #endregion

        #region Save

        /// <summary>
        /// 保存实体对象到数据库（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="transactionSession">事务会话对象。</param>
        /// <param name="entity">要保存的实体对象。</param>
        /// <param name="table">表名称。</param>
        public async FTask Save<T>(object transactionSession, T? entity, string table = null) where T : Entity
        {
            // if (entity == null)
            // {
            //     Log.Error($"save entity is null: {typeof(T).Name}");
            //     return;
            // }
            // 
            // var clone = _serializer.Clone(entity);
            // var tableName = GetTableName<T>(table);
            // 
            // using (await _dataBaseLock.Wait(clone.Id))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.Transaction = (NpgsqlTransaction)transactionSession;
            //         var columns = GetColumnsForEntity(clone);
            //         var values = GetValuesForEntity(clone);
            //         
            //         // 检查记录是否存在
            //         cmd.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\" WHERE \"Id\" = @Id";
            //         cmd.Parameters.Clear();
            //         cmd.Parameters.AddWithValue("Id", clone.Id);
            //         var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            //         
            //         if (count > 0)
            //         {
            //             // 更新
            //             var setClause = string.Join(", ", columns.Select(c => $"\"{c}\" = @{c}"));
            //             cmd.CommandText = $"UPDATE \"{tableName}\" SET {setClause} WHERE \"Id\" = @Id";
            //             AddParametersForEntity(cmd, clone, columns);
            //         }
            //         else
            //         {
            //             // 插入
            //             var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
            //             var valueList = string.Join(", ", columns.Select(c => $"@{c}"));
            //             cmd.CommandText = $"INSERT INTO \"{tableName}\" ({columnList}) VALUES ({valueList})";
            //             AddParametersForEntity(cmd, clone, columns);
            //         }
            //         
            //         await cmd.ExecuteNonQueryAsync();
            //         await _connection.CloseAsync();
            //     }
            // }
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 保存实体对象到数据库（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="entity">要保存的实体对象。</param>
        /// <param name="table">表名称。</param>
        public async FTask Save<T>(T? entity, string table = null) where T : Entity, new()
        {
            if (entity == null)
            {
                Log.Error($"save entity is null: {typeof(T).Name}");
                return;
            }

            var tableName = GetTableName<T>(table);

            using (await pg.FlowLock.Wait(entity.Id))
            {
                switch (Mode)
                {
                    case PreferSqlMode.EFCore:
                        {
                            Entry(entity).State = EntityState.Modified;
                            await SaveChangesAsync();
                            break;
                        }
                    case PreferSqlMode.Dapper:
                        {
                            var _connection =  await GetOpenedConnection();
                            

                            break;
                        }
                }              
            }
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 保存实体对象到数据库（加锁）。
        /// </summary>
        /// <param name="filter">保存的条件表达式。</param>
        /// <param name="entity">实体类型。</param>
        /// <param name="table">表名称。</param>
        /// <typeparam name="T"></typeparam>
        public async FTask Save<T>(Expression<Func<T, bool>> filter, T? entity, string table = null) where T : Entity, new()
        {
            // if (entity == null)
            // {
            //     Log.Error($"save entity is null: {typeof(T).Name}");
            //     return;
            // }
            // 
            // var clone = _serializer.Clone(entity);
            // var tableName = GetTableName<T>(table);
            // var whereClause = GetWhereClause(filter);
            // 
            // using (await _dataBaseLock.Wait(clone.Id))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         var columns = GetColumnsForEntity(clone);
            //         var setClause = string.Join(", ", columns.Select(c => $"\"{c}\" = @{c}"));
            //         
            //         cmd.CommandText = $"UPDATE \"{tableName}\" SET {setClause} WHERE {whereClause}";
            //         AddParametersForEntity(cmd, clone, columns);
            //         
            //         await cmd.ExecuteNonQueryAsync();
            //         await _connection.CloseAsync();
            //     }
            // }
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 保存多个实体对象到数据库（加锁）。
        /// </summary>
        /// <param name="id">行 ID。</param>
        /// <param name="entities">要保存的实体对象列表。</param>
        public async FTask Save(long id, List<Entity>? entities)
        {
            // if (entities == null || entities.Count == 0)
            // {
            //     Log.Error("save entity is null");
            //     return;
            // }
            // 
            // using var listPool = ListPool<Entity>.Create();
            // 
            // foreach (var entity in entities)
            // {
            //     listPool.Add(_serializer.Clone(entity)); 
            // }
            // 
            // using (await _dataBaseLock.Wait(id))
            // {
            //     await _connection.OpenAsync();
            //     foreach (var clone in listPool)
            //     {
            //         try
            //         {
            //             var tableName = clone.GetType().Name;
            //             var columns = GetColumnsForEntity(clone);
            //             
            //             // 检查记录是否存在
            //             using (var cmd = _connection.CreateCommand())
            //             {
            //                 cmd.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\" WHERE \"Id\" = @Id";
            //                 cmd.Parameters.Clear();
            //                 cmd.Parameters.AddWithValue("Id", clone.Id);
            //                 var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            //                 
            //                 if (count > 0)
            //                 {
            //                     // 更新
            //                     var setClause = string.Join(", ", columns.Select(c => $"\"{c}\" = @{c}"));
            //                     cmd.CommandText = $"UPDATE \"{tableName}\" SET {setClause} WHERE \"Id\" = @Id";
            //                     AddParametersForEntity(cmd, clone, columns);
            //                 }
            //                 else
            //                 {
            //                     // 插入
            //                     var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
            //                     var valueList = string.Join(", ", columns.Select(c => $"@{c}"));
            //                     cmd.CommandText = $"INSERT INTO \"{tableName}\" ({columnList}) VALUES ({valueList})";
            //                     AddParametersForEntity(cmd, clone, columns);
            //                 }
            //                 
            //                 await cmd.ExecuteNonQueryAsync();
            //             }
            //         }
            //         catch (Exception e)
            //         {
            //             Log.Error($"Save List Entity Error: {clone.GetType().Name} {clone}\n{e}");
            //         }
            //     }
            //     await _connection.CloseAsync();
            // }
            await FTask.CompletedTask;
        }

        #endregion

        #region Insert

        /// <summary>
        /// 插入单个实体对象到数据库（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="entity">要插入的实体对象。</param>
        /// <param name="table">表名称。</param>
        public async FTask Insert<T>(T? entity, string table = null) where T : Entity, new()
        {
            // if (entity == null)
            // {
            //     Log.Error($"insert entity is null: {typeof(T).Name}");
            //     return;
            // }
            // 
            // var clone = _serializer.Clone(entity);
            // var tableName = GetTableName<T>(table);
            // 
            // using (await _dataBaseLock.Wait(entity.Id))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         var columns = GetColumnsForEntity(clone);
            //         var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
            //         var valueList = string.Join(", ", columns.Select(c => $"@{c}"));
            //         
            //         cmd.CommandText = $"INSERT INTO \"{tableName}\" ({columnList}) VALUES ({valueList})";
            //         AddParametersForEntity(cmd, clone, columns);
            //         
            //         await cmd.ExecuteNonQueryAsync();
            //         await _connection.CloseAsync();
            //     }
            // }
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 批量插入实体对象列表到数据库（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="list">要插入的实体对象列表。</param>
        /// <param name="table">表名称。</param>
        public async FTask InsertBatch<T>(IEnumerable<T> list, string table = null) where T : Entity, new()
        {
            // var tableName = GetTableName<T>(table);
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         foreach (var entity in list)
            //         {
            //             var clone = _serializer.Clone(entity);
            //             var columns = GetColumnsForEntity(clone);
            //             var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
            //             var valueList = string.Join(", ", columns.Select(c => $"@{c}"));
            //             
            //             cmd.CommandText = $"INSERT INTO \"{tableName}\" ({columnList}) VALUES ({valueList})";
            //             cmd.Parameters.Clear();
            //             AddParametersForEntity(cmd, clone, columns);
            //             
            //             await cmd.ExecuteNonQueryAsync();
            //         }
            //         await _connection.CloseAsync();
            //     }
            // }
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 批量插入实体对象列表到数据库（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="transactionSession">事务会话对象。</param>
        /// <param name="list">要插入的实体对象列表。</param>
        /// <param name="table">表名称。</param>
        public async FTask InsertBatch<T>(object transactionSession, IEnumerable<T> list, string table = null)
            where T : Entity, new()
        {
            // var tableName = GetTableName<T>(table);
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.Transaction = (NpgsqlTransaction)transactionSession;
            //         
            //         foreach (var entity in list)
            //         {
            //             var clone = _serializer.Clone(entity);
            //             var columns = GetColumnsForEntity(clone);
            //             var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
            //             var valueList = string.Join(", ", columns.Select(c => $"@{c}"));
            //             
            //             cmd.CommandText = $"INSERT INTO \"{tableName}\" ({columnList}) VALUES ({valueList})";
            //             cmd.Parameters.Clear();
            //             AddParametersForEntity(cmd, clone, columns);
            //             
            //             await cmd.ExecuteNonQueryAsync();
            //         }
            //         await _connection.CloseAsync();
            //     }
            // }
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 插入BsonDocument到数据库（加锁）。
        /// </summary>
        /// <param name="bsonDocument"></param>
        /// <param name="taskId"></param>
        /// <typeparam name="T"></typeparam>
        public async Task Insert<T>(BsonDocument bsonDocument, long taskId) where T : Entity
        {
            // var tableName = GetTableName<T>();
            // 
            // using (await _dataBaseLock.Wait(taskId))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         var columns = bsonDocument.Names;
            //         var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
            //         var valueList = string.Join(", ", columns.Select(c => $"@{c}"));
            //         
            //         cmd.CommandText = $"INSERT INTO \"{tableName}\" ({columnList}) VALUES ({valueList})";
            //         foreach (var name in columns)
            //         {
            //             cmd.Parameters.AddWithValue(name, bsonDocument[name].RawValue);
            //         }
            //         
            //         await cmd.ExecuteNonQueryAsync();
            //         await _connection.CloseAsync();
            //     }
            // }
            await FTask.CompletedTask;
        }

        #endregion

        #region Remove

        /// <summary>
        /// 根据ID删除单个实体对象（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="transactionSession">事务会话对象。</param>
        /// <param name="id">要删除的实体的ID。</param>
        /// <param name="table">表名称。</param>
        /// <returns>删除的实体数量。</returns>
        public async FTask<long> Remove<T>(object transactionSession, long id, string table = null)
            where T : Entity, new()
        {
            // var tableName = GetTableName<T>(table);
            // 
            // using (await _dataBaseLock.Wait(id))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.Transaction = (NpgsqlTransaction)transactionSession;
            //         cmd.CommandText = $"DELETE FROM \"{tableName}\" WHERE \"Id\" = @Id";
            //         cmd.Parameters.AddWithValue("Id", id);
            //         var result = await cmd.ExecuteNonQueryAsync();
            //         await _connection.CloseAsync();
            //         return result;
            //     }
            // }
            await FTask.CompletedTask;
            return 0;
        }

        /// <summary>
        /// 根据ID删除单个实体对象（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="id">要删除的实体的ID。</param>
        /// <param name="table">表名称。</param>
        /// <returns>删除的实体数量。</returns>
        public async FTask<long> Remove<T>(long id, string table = null) where T : Entity, new()
        {
            // var tableName = GetTableName<T>(table);
            // 
            // using (await _dataBaseLock.Wait(id))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"DELETE FROM \"{tableName}\" WHERE \"Id\" = @Id";
            //         cmd.Parameters.AddWithValue("Id", id);
            //         var result = await cmd.ExecuteNonQueryAsync();
            //         await _connection.CloseAsync();
            //         return result;
            //     }
            // }
            await FTask.CompletedTask;
            return 0;
        }

        /// <summary>
        /// 根据ID和筛选条件删除多个实体对象（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="coroutineLockQueueKey">异步锁Id。</param>
        /// <param name="transactionSession">事务会话对象。</param>
        /// <param name="filter">筛选条件。</param>
        /// <param name="table">表名称。</param>
        /// <returns>删除的实体数量。</returns>
        public async FTask<long> Remove<T>(long coroutineLockQueueKey, object transactionSession,
            Expression<Func<T, bool>> filter, string table = null) where T : Entity, new()
        {
            // var tableName = GetTableName<T>(table);
            // var whereClause = GetWhereClause(filter);
            // 
            // using (await _dataBaseLock.Wait(coroutineLockQueueKey))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.Transaction = (NpgsqlTransaction)transactionSession;
            //         cmd.CommandText = $"DELETE FROM \"{tableName}\" WHERE {whereClause}";
            //         var result = await cmd.ExecuteNonQueryAsync();
            //         await _connection.CloseAsync();
            //         return result;
            //     }
            // }
            await FTask.CompletedTask;
            return 0;
        }

        /// <summary>
        /// 根据ID和筛选条件删除多个实体对象（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="coroutineLockQueueKey">异步锁Id。</param>
        /// <param name="filter">筛选条件。</param>
        /// <param name="table">表名称。</param>
        /// <returns>删除的实体数量。</returns>
        public async FTask<long> Remove<T>(long coroutineLockQueueKey, Expression<Func<T, bool>> filter,
            string table = null) where T : Entity, new()
        {
            // var tableName = GetTableName<T>(table);
            // var whereClause = GetWhereClause(filter);
            // 
            // using (await _dataBaseLock.Wait(coroutineLockQueueKey))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"DELETE FROM \"{tableName}\" WHERE {whereClause}";
            //         var result = await cmd.ExecuteNonQueryAsync();
            //         await _connection.CloseAsync();
            //         return result;
            //     }
            // }
            await FTask.CompletedTask;
            return 0;
        }

        #endregion

        #region Utility

        /// <summary>
        /// ***************************** AI写的, 待人工检验 ********************************
        /// 对满足条件的行中的某个数值字段进行求和操作。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="filter">用于筛选行的条件。</param>
        /// <param name="sumExpression">要对其进行求和的字段表达式。</param>
        /// <param name="table">表名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>满足条件的行中指定字段的求和结果。</returns>
        public async FTask<long> Sum<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>> sumExpression, string? table = null) where T : Entity
        {
            //var tableName = table ?? typeof(T).Name.ToLowerInvariant();
            //var columnName = GetColumnName(sumExpression);

            //var (whereClause, parameters) = BuildSqlWhereClause(filter); //这里调用之后进行了复杂的构建

            //await using var conn = await Handler.OpenConnectionAsync();
            //await using var cmd = conn.CreateCommand();

            //// 使用参数化查询避免 SQL 注入
            //cmd.CommandText = $"SELECT SUM({columnName}) FROM \"{tableName}\" WHERE {whereClause}";
            //foreach (var param in parameters)
            //    cmd.Parameters.Add(param);

            //var result = await cmd.ExecuteScalarAsync();
            //return result != DBNull.Value ? Convert.ToInt64(result) : 0;
            await FTask.CompletedTask;
            return 1;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 从DataReader中获取BsonDocument
        /// </summary>
        private BsonDocument GetBsonDocumentFromReader(NpgsqlDataReader reader)
        {
            // var document = new BsonDocument();
            // for (int i = 0; i < reader.FieldCount; i++)
            // {
            //     var name = reader.GetName(i);
            //     var value = reader.GetValue(i);
            //     
            //     if (value == DBNull.Value)
            //     {
            //         document.Add(name, BsonNull.Value);
            //     }
            //     else
            //     {
            //         document.Add(name, new BsonValue(value));
            //     }
            // }
            // return document;
            return new BsonDocument();
        }

        /// <summary>
        /// 将JSON查询条件转换为SQL WHERE子句
        /// </summary>
        private string ConvertJsonToWhereClause(string json)
        {
            // 这里应该实现JSON到SQL WHERE子句的转换
            // 为简化，返回一个占位符
            return "1=1";
        }

        /// <summary>
        /// 获取实体的所有列名
        /// </summary>
        private List<string> GetColumnsForEntity<T>(T entity) where T : Entity
        {
            // var columns = new List<string>();
            // var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            // foreach (var property in properties)
            // {
            //     columns.Add(property.Name);
            // }
            // return columns;
            return new List<string>();
        }

        /// <summary>
        /// 获取实体的所有值
        /// </summary>
        private List<object> GetValuesForEntity<T>(T entity) where T : Entity
        {
            // var values = new List<object>();
            // var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            // foreach (var property in properties)
            // {
            //     values.Add(property.GetValue(entity));
            // }
            // return values;
            return new List<object>();
        }

        /// <summary>
        /// 为命令添加实体参数
        /// </summary>
        private void AddParametersForEntity<T>(NpgsqlCommand cmd, T entity, List<string> columns) where T : Entity
        {
            // foreach (var column in columns)
            // {
            //     var property = typeof(T).GetProperty(column);
            //     if (property != null)
            //     {
            //         var value = property.GetValue(entity);
            //         cmd.Parameters.AddWithValue(column, value ?? DBNull.Value);
            //     }
            // }
        }

        /// <summary>
        /// 从索引键定义获取列名
        /// </summary>
        private string GetIndexColumns(object key)
        {
            // 这里应该实现从索引键定义获取列名的逻辑
            // 为简化，返回一个占位符
            return "column";
        }

        /// <summary>
        /// 从索引键定义获取索引类型
        /// </summary>
        private string GetIndexType(object key)
        {
            // 这里应该实现从索引键定义获取索引类型的逻辑
            // 为简化，返回一个占位符
            return "USING btree";
        }
        #endregion

    }
}
#endif