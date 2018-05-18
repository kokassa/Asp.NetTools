//  --------------------------------------------------------------------
//    description :
//
//    created by ALEE at 8/31/2017 7:55:35 PM
//    memeda
//  --------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.Entity;
using System.Linq.Expressions;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;
using CommonData.ServiceBase;

namespace CommonData.ServiceBase
{
    /// <summary>
    /// 实体业务抽象基类，封装基于某实体对象的基本操作方法，包括分面查询
    /// </summary>
    /// <typeparam name="TContainer"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TEntityID"></typeparam>
    public abstract class EntityBusinessService<TContainer, TEntity, TEntityID> : BusinessService<TContainer>
        where TContainer : DbContext, new()
        where TEntity : class
    {
        private ObjectQuery<TEntity> entityQuery = null;
        /// <summary>
        /// 获取实体查询对象
        /// </summary>
        protected ObjectQuery<TEntity> EntityQuery
        {
            get
            {
                if (this.entityQuery == null)
                {
                    this.entityQuery = this.ObjectContext.CreateQuery<TEntity>(typeof(TEntity).Name);
                }
                return this.entityQuery;
            }
        }

        /// <summary>
        /// 定义主键名称
        /// </summary>
        public virtual string IDPropertyName
        {
            get
            {
                return "ID";
            }
        }

        public EntityBusinessService()
        {

        }

        /// <summary>
        /// 创建业务对象
        /// </summary>
        /// <param name="container"></param>
        public EntityBusinessService(BusinessService<TContainer> businessServiceBase)
            : base(businessServiceBase)
        {
        }        

        /// <summary>
        /// 通过主键ID获取实体对象
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual TEntity GetById(TEntityID id, params string[] includes)
        {

            List<ObjectParameter> lst = new List<ObjectParameter>();
            lst.Add(new ObjectParameter("id", id));
            var tmpQuerry = Select(string.Format("it.{0}=@id", IDPropertyName), lst, includes);

            if (tmpQuerry.Count() <= 0) return null;
            else return tmpQuerry.First();
        }
       

        /// <summary>
        ///  查询第一个实体对象
        /// </summary>
        /// <param name="whereString"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        public virtual TEntity SelectFirst(string whereString, List<ObjectParameter> parms, params string[] includes)
        {
            List<TEntity> lst = Select(whereString, parms, includes).ToList();

            if ((lst != null) && (lst.Count() > 0)) return lst[0];
            else return null;
        }

        /// <summary>
        /// 查询实体对象集
        /// </summary>
        /// <param name="whereString">esql condition statement</param>
        /// <param name="parms">parameters</param>
        /// <returns></returns>
        public virtual ObjectQuery<TEntity> Select(string whereString, List<ObjectParameter> parms, params string[] includes)
        {
            ObjectQuery<TEntity> query = this.EntityQuery;
            if (string.IsNullOrEmpty(whereString)) whereString = "1=1";
            if (includes != null)
            {
                foreach (string include in includes)
                {
                    query = query.Include(include);
                }
            }

            ObjectQuery<TEntity> oq = query.Where(whereString);
            oq.Parameters.Clear();

            if (parms != null)
            {
                foreach (ObjectParameter op in parms)
                {
                    oq.Parameters.Add(op);
                }
            }
            return oq;
        }

        /// <summary>
        /// 返回所有实体对象集
        /// </summary>
        /// <returns></returns>
        public virtual ObjectQuery<TEntity> SelectAll(params string[] includes)
        {
            ObjectQuery<TEntity> query = this.EntityQuery;

            if (includes != null)
            {
                foreach (string include in includes)
                {
                    query = query.Include(include);
                }
            }
            return query;
        }

        /// <summary>
        /// 添加实体
        /// </summary>
        /// <param name="entity"></param>
        public virtual void Add(TEntity entity)
        {
            //this.container.AddObject(typeof(TEntity).Name, entity);
            this.container.Set<TEntity>().Add(entity);
        }

        /// <summary>
        /// 删除该实体
        /// </summary>
        /// <param name="entity"></param>
        public virtual void Delete(TEntity entity)
        {
            //this.container.DeleteObject(entity);
            this.container.Set<TEntity>().Remove(entity);
        }

        /// <summary>
        /// 删除该实体
        /// </summary>
        /// <param name="id"></param>
        public virtual void Delete(TEntityID id)
        {
            var obj = GetById(id);
            if (obj != null)
            {
                Delete(obj);
            }
        }

        /// <summary>
        /// 查询一页数据
        /// </summary>
        /// <param name="includePaths"></param>
        /// <param name="whereString"></param>
        /// <param name="sortExpress"></param>
        /// <param name="isSortAsc"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="totalCount"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public virtual IQueryable<TEntity> QueryPage(string includePaths, string whereString, string sortExpress, bool isSortAsc, int pageSize, int pageIndex, ref int totalCount, params ObjectParameter[] parameters)
        {
            return QueryPage(includePaths, whereString, null, sortExpress, isSortAsc, pageSize, pageIndex, ref  totalCount, parameters);
        }

        /// <summary>
        /// 查询一页数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="includePaths">需要include进行的对象，多个用";"号隔开,例"Bus_Community;XXX"</param>
        /// <param name="whereString">查询条件表达式，例:it.CustomerId=@customerid</param>
        /// <param name="funWheres">条件表达式</param>
        /// <param name="defaultSortExpress">默认排序属性名称</param>
        /// <param name="sortExpress">排序属性名称</param>
        /// <param name="isSortAsc">是否升序</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="pageIndex">当前页码,以0开始</param>
        /// <param name="totalCount">返回查询总数量</param>
        /// <param name="parameters">查询参数集合（可选）</param>
        /// <returns></returns>
        public virtual IQueryable<TEntity> QueryPage(string includePaths, string whereString, List<System.Linq.Expressions.Expression<Func<TEntity, bool>>> funWheres,
            string sortExpress, bool isSortAsc, int pageSize, int pageIndex, ref int totalCount, params ObjectParameter[] parameters)
        {
            totalCount = GetQueryCount(whereString, funWheres, parameters);
            string sqlStr = string.Format("select value c from {0}.[{1}] as c", this.ObjectContext.DefaultContainerName, typeof(TEntity).Name);

            ObjectQuery<TEntity> query = null;

            if (parameters != null && parameters.Count() > 0)
            {
                query = this.ObjectContext.CreateQuery<TEntity>(sqlStr, parameters);
            }
            else
            {
                query = this.ObjectContext.CreateQuery<TEntity>(sqlStr);
            }

            if (!string.IsNullOrEmpty(includePaths))
            {
                string[] tempArray = includePaths.Split(';');
                foreach (string paths in tempArray)
                {
                    query = query.Include(paths);
                }
            }

            if (!string.IsNullOrEmpty(whereString))
            {
                query = query.Where(whereString, parameters);
            }

            IQueryable<TEntity> queryReturn = query.AsQueryable();
            if (funWheres != null)
            {
                foreach (var funWhere in funWheres)
                {
                    queryReturn = queryReturn.Where(funWhere);
                }
            }

            queryReturn = queryReturn.OrderBy(sortExpress, isSortAsc);
            queryReturn = queryReturn.Skip(pageSize * pageIndex);
            queryReturn = queryReturn.Take(pageSize);

            return queryReturn;
        }

        /// <summary>
        /// 获取记录数
        /// </summary>
        /// <param name="objectContext"></param>
        /// <param name="whereString"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public virtual int GetQueryCount(string whereString, params ObjectParameter[] parameters)
        {
            return GetQueryCount(whereString, null, parameters);
        }

        /// <summary>
        /// 获取记录数
        /// </summary>
        /// <param name="objectContext"></param>
        /// <param name="whereString"></param>
        /// <param name="funWheres"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public virtual int GetQueryCount(string whereString, List<System.Linq.Expressions.Expression<Func<TEntity, bool>>> funWheres, params ObjectParameter[] parameters)
        {
            string sqlStr = string.Format("select value c from {0}.[{1}] as c", this.ObjectContext.DefaultContainerName, typeof(TEntity).Name);

            ObjectQuery<TEntity> query = null;

            if (parameters != null && parameters.Count() > 0)
            {
                query = this.ObjectContext.CreateQuery<TEntity>(sqlStr, parameters);
            }
            else
            {
                query = this.ObjectContext.CreateQuery<TEntity>(sqlStr);
            }

            if (!string.IsNullOrEmpty(whereString))
            {
                query = query.Where(whereString);
            }

            IQueryable<TEntity> queryReturn = query.AsQueryable();
            if (funWheres != null)
            {
                foreach (var funWhere in funWheres)
                {
                    queryReturn = queryReturn.Where(funWhere);
                }
            }

            return queryReturn.Count();
        }
    }
}
