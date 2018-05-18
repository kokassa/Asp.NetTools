//  --------------------------------------------------------------------
//    description :
//
//    created by ALEE at 8/31/2017 7:55:35 PM
//    waitsea@qq.com
//  --------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Data;
using System.Linq.Expressions;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Core.EntityClient;


namespace CommonData.ServiceBase
{
    /// <summary>
    /// 业务抽象基类，封装Entity Framework 数据访问对象及通用查询方法
    /// </summary>
    /// <typeparam name="TContainer"></typeparam>
    public abstract class BusinessService<TContainer> where TContainer : DbContext, new()
    {
        #region ContainerMgnt
        private TContainer _container = null;

        public BusinessService()
        {

        }

        /// <summary>
        /// used in the condition of transaction needed
        /// </summary>
        /// <param name="container"></param>
        public BusinessService(BusinessService<TContainer> businessServiceBase)
        {
            this.container = businessServiceBase.container;
        }

        public void Dispose()
        {
            _container.Dispose();
        }

        /// <summary>
        /// 获取或设置Entity Framework数据访问对象
        /// </summary>
        protected TContainer container
        {
            get
            {
                if (_container == null)
                {
                    _container = new TContainer();
                }
                return _container;
            }
            set
            {
                if (value != null)
                {
                    _container = value;
                }
            }
        }

        //void _container_SavingChanges(object sender, EventArgs e)
        //{
        //    string message = _container.ToTraceString();
        //}

        /// <summary>
        /// 向DB提交数据
        /// </summary>
        /// <returns></returns>
        public virtual int SaveChanges()
        {
            this.ObjectContext.CommandTimeout = 120;
            return this.container.SaveChanges();
        }

        /// <summary>
        /// 获取当前Entity Framework数据容器对象名称
        /// </summary>
        public string DefaultContainerName
        {
            get
            {
                return this.ObjectContext.DefaultContainerName;
            }
        }

        protected ObjectContext ObjectContext
        {
            get
            {
                return (this.container as IObjectContextAdapter).ObjectContext;
            }
        }

        /// <summary>
        /// 跟踪数据的变化（批量插入时关闭此项，会提高性能）
        /// </summary>
        public bool AutoDetectChangesEnabled
        {
            get
            {
                return this.container.Configuration.AutoDetectChangesEnabled;
            }
            set
            {
                this.container.Configuration.AutoDetectChangesEnabled = value;
            }
        }

        /// <summary>
        /// 保存时验证数据有效性
        /// </summary>
        public bool ValidateOnSaveEnabled
        {
            get
            {
                return this.container.Configuration.ValidateOnSaveEnabled;
            }
            set
            {
                this.container.Configuration.ValidateOnSaveEnabled = value;
            }
        }

        #endregion

        /// <summary>
        /// 查询一页数据
        /// </summary>
        /// <typeparam name="T">返回集合中数据对象类型,可以实体或自定义对象</typeparam>
        /// <param name="query">基本查询，可理解为数据库视图或一段关联查询的SQL脚本</param>
        /// <param name="funWheres">查询过滤条件</param>
        /// <param name="sortExpress">排序字段名</param>
        /// <param name="isSortAsc">是否按升序排</param>
        /// <param name="pageSize">每页数据条数</param>
        /// <param name="pageIndex">查询页索引</param>
        /// <param name="totalCount">返回总记录数</param>
        /// <returns></returns>
        public virtual IQueryable<T> QueryPage<T>(IQueryable<T> query, List<System.Linq.Expressions.Expression<Func<T, bool>>> funWheres,
         string sortExpress, bool isSortAsc, int pageSize, int pageIndex, ref int totalCount)
        {

            var sortExpresses = new List<SortExpress>();
            if (!String.IsNullOrEmpty(sortExpress))
                sortExpresses.Add(new SortExpress { SortPropertyName = sortExpress, IsSortAsc = isSortAsc });

            return QueryPage<T>(query, funWheres, sortExpresses, pageSize, pageIndex, ref totalCount);
        }

        /// <summary>
        /// 查询一页数据
        /// </summary>
        /// <typeparam name="T">返回集合中数据对象类型,可以实体或自定义对象</typeparam>
        /// <param name="query">基本查询，可理解为数据库视图或一段关联查询的SQL脚本</param>
        /// <param name="funWheres">查询过滤条件</param>
        /// <param name="sortExpresses">包含排序信息集合，支持再排序</param>
        /// <param name="pageSize">每页数据条数</param>
        /// <param name="pageIndex">查询页索引</param>
        /// <param name="totalCount">返回总记录数</param>
        /// <returns></returns>
        public virtual IQueryable<T> QueryPage<T>(IQueryable<T> query, List<System.Linq.Expressions.Expression<Func<T, bool>>> funWheres,
         List<SortExpress> sortExpresses, int pageSize, int pageIndex, ref int totalCount)
        {
            totalCount = GetQueryCount(query, funWheres);
            IQueryable<T> queryReturn = query.AsQueryable();
            if (funWheres != null)
            {
                foreach (var funWhere in funWheres)
                {
                    queryReturn = queryReturn.Where(funWhere);
                }
            }

            if (sortExpresses != null)
            {
                for (int i = 0; i < sortExpresses.Count; i++)
                {
                    SortExpress sortExpress = sortExpresses[i];
                    if (i == 0)
                    {
                        queryReturn = queryReturn.OrderBy(sortExpress.SortPropertyName, sortExpress.IsSortAsc);

                    }
                    else
                    {
                        queryReturn = (queryReturn as IOrderedQueryable<T>).ThenBy(sortExpress.SortPropertyName, sortExpress.IsSortAsc);
                    }
                }
            }
            queryReturn = queryReturn.Skip(pageSize * pageIndex);
            queryReturn = queryReturn.Take(pageSize);

            return queryReturn;
        }

        /// <summary>
        /// 获取记录数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="funWheres"></param>
        /// <returns></returns>
        public virtual int GetQueryCount<T>(IQueryable<T> query, List<System.Linq.Expressions.Expression<Func<T, bool>>> funWheres)
        {
            IQueryable<T> queryReturn = query.AsQueryable();
            if (funWheres != null)
            {
                foreach (var funWhere in funWheres)
                {
                    queryReturn = queryReturn.Where(funWhere);
                }
            }

            return queryReturn.Count();
        }

        /// <summary>
        /// 执行存储过程查询
        /// </summary>
        /// <param name="storedProcedureName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public DataSet ExecuteStoredProcedureQuery(string storedProcedureName, params SqlParameter[] parameters)
        {
            var connectionString = ((EntityConnection)this.ObjectContext.Connection).StoreConnection.ConnectionString;
            var ds = new DataSet();

            using (var conn = new SqlConnection(connectionString))
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = storedProcedureName;
                    cmd.CommandType = CommandType.StoredProcedure;
                    foreach (var parameter in parameters)
                    {
                        cmd.Parameters.Add(parameter);
                    }

                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(ds);
                    }
                }
            }

            return ds;
        }

        /// <summary>
        /// 执行Sql查询
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public DataSet ExecuteSQLQuery(string sql, params SqlParameter[] parameters)
        {
           var connectionString = ((EntityConnection)this.ObjectContext.Connection).StoreConnection.ConnectionString;
            //var connectionString = ConfigurationManager.ConnectionStrings["FootingBirdModelContainer"].ConnectionString;
            var ds = new DataSet();

            using (var conn = new SqlConnection(connectionString))
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    foreach (var parameter in parameters)
                    {
                        cmd.Parameters.Add(parameter);
                    }

                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(ds);
                    }
                }
            }

            return ds;
        }
    }

    public class SortExpress
    {
        public string SortPropertyName { get; set; }
        public bool IsSortAsc { get; set; }
    }

    public static class ObjectSetExtention
    {
        /// <summary>
        /// 查找（包含刚添加，状态为added）对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="set"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IEnumerable<T> WhereIncludeAdded<T>(this ObjectSet<T> set, Expression<Func<T, bool>> predicate) where T : class
        {
            var dbResult = set.Where(predicate);
            var offlineResult = set.Context.ObjectStateManager.GetObjectStateEntries(EntityState.Added).Select(entry => entry.Entity).OfType<T>().Where(predicate.Compile());
            return offlineResult.Union(dbResult);
        }

        /// <summary>
        /// 查找（包含刚添加，状态为added）对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="set"></param>
        /// <param name="dbContext">当前DbContext对象</param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IEnumerable<T> WhereIncludeAdded<T>(this DbSet<T> set, DbContext dbContext, Expression<Func<T, bool>> predicate) where T : class
        {
            var dbResult = set.Where(predicate);

            var context = (dbContext as IObjectContextAdapter).ObjectContext;
            context.ExecuteStoreCommand("", null);
            context.CreateQuery<T>("");

            var offlineResult = context.ObjectStateManager.GetObjectStateEntries(EntityState.Added).Select(entry => entry.Entity).OfType<T>().Where(predicate.Compile());
            return offlineResult.Union(dbResult);
        }

        public static T AddObject<T>(this DbSet<T> set, T entity) where T : class
        {
            return set.Add(entity);

        }

        public static T DeleteObject<T>(this DbSet<T> set, T entity) where T : class
        {
            return set.Remove(entity);
        }

        public static T DeleteObject<T>(this DbContext dbContext, T entity) where T : class
        {
            return dbContext.Set<T>().Remove(entity);
        }

        public static ObjectQuery<T> CreateQuery<T>(this DbContext dbContext, string queryString, params ObjectParameter[] parameters)
        {
            var context = (dbContext as IObjectContextAdapter).ObjectContext;
            return context.CreateQuery<T>(queryString, parameters);
        }

        public static int ExecuteStoreCommand(this DbContext dbContext, string commandText, params object[] parameters)
        {
            var context = (dbContext as IObjectContextAdapter).ObjectContext;
            return context.ExecuteStoreCommand(commandText, parameters);
        }

        public static ObjectResult<TElement> ExecuteStoreQuery<TElement>(this DbContext dbContext, string commandText, params object[] parameters)
        {
            var context = (dbContext as IObjectContextAdapter).ObjectContext;
            return context.ExecuteStoreQuery<TElement>(commandText, parameters);
        }

        public static ObjectResult<TEntity> ExecuteStoreQuery<TEntity>(this DbContext dbContext, string commandText, string entitySetName, MergeOption mergeOption, params object[] parameters)
        {
            var context = (dbContext as IObjectContextAdapter).ObjectContext;
            return context.ExecuteStoreQuery<TEntity>(commandText, entitySetName, mergeOption, parameters);
        }
    }
}
