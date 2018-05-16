//  --------------------------------------------------------------------
//    description :
//
//    created by ALEE at 8/31/2017 7:55:35 PM
//    memeda
//  --------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace CommonData
{
    public static class MyExtensions
    {

        /// <summary>
        /// 判断是否有重复项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="keySelectors"></param>
        /// <returns></returns>
        public static bool ExistRepeat<T>(this IList<T> list, params Expression<Func<T, object>>[] keySelectors) where T : class
        {
            foreach (T item in list)
            {
                IEnumerable<T> query = list;
                foreach (Expression<Func<T, object>> keySelector in keySelectors)
                {
                    string name = "";
                    if (keySelector.Body is UnaryExpression)
                    {
                        name = GetExpressionText(keySelector.Body as UnaryExpression);
                    }
                    else if (keySelector.Body is MemberExpression)
                    {
                        name = (keySelector.Body as MemberExpression).Member.Name;
                    }
                    var expr = BuildExpression<T>(item, name);
                    query = query.Where(expr.Compile());
                }
                if (query.Count() > 1)
                {
                    return true;
                }
            }
            return false;
        }

        private static string GetExpressionText(UnaryExpression exp)
        {
            if (exp != null)
            {
                if (exp.Operand.NodeType == ExpressionType.MemberAccess)
                {
                    MemberExpression memExp = exp.Operand as MemberExpression;
                    if (memExp != null)
                    {
                        return memExp.Member.Name;
                    }
                }
            }
            return string.Empty;
        }

        private static Expression<Func<T, bool>> BuildExpression<T>(T item, string propertyName)
        {
            Expression<Func<T, bool>> expr;
            ParameterExpression paramExpr = Expression.Parameter(typeof(T), "l");

            PropertyInfo propInfo = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

            //l.propertyNameString
            MemberExpression memberExpr = Expression.Property(paramExpr, propInfo);

            ConstantExpression constExpr = Expression.Constant(item, typeof(T));
            //item.propertyNameString
            MemberExpression memberExprRight = Expression.Property(constExpr, propInfo);

            BinaryExpression equalExpr = Expression.Equal(memberExpr, memberExprRight);
            //l => l.aa == item.aa
            expr = Expression.Lambda<Func<T, bool>>(equalExpr, paramExpr);

            return expr;
        }

        public static IQueryable<T> OrderBy<T>(this IQueryable<T> queryable, string propertyName)
        {
            return OrderBy(queryable, propertyName, true);
        }

        public static IQueryable<T> OrderBy<T>(this IQueryable<T> queryable, string propertyName, bool isSortAsc)
        {
            Expression param = Expression.Parameter(typeof(T));
            var properties = propertyName.Split('.');
            var body = param;
            //支持"User.Age"这种参数  p=>p.User.Age
            foreach (var p in properties)
            {
                body = Expression.Property(body, p);
            }
            dynamic keySelector = Expression.Lambda(body, param as ParameterExpression);
            return isSortAsc ? Queryable.OrderBy(queryable, keySelector) : Queryable.OrderByDescending(queryable, keySelector);
        }

        public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> queryable, string propertyName)
        {
            return ThenBy(queryable, propertyName, true);
        }

        public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> queryable, string propertyName, bool isSortAsc)
        {
            Expression param = Expression.Parameter(typeof(T));
            var properties = propertyName.Split('.');
            var body = param;
            //支持"User.Age"这种参数  p=>p.User.Age
            foreach (var p in properties)
            {
                body = Expression.Property(body, p);
            }
            dynamic keySelector = Expression.Lambda(body, param as ParameterExpression);

            return isSortAsc ? Queryable.ThenBy(queryable, keySelector) : Queryable.ThenByDescending(queryable, keySelector);
        }

        /// <summary>
        /// 替换单引号
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ReplaceQuote(this string s)
        {
            string pattern = @"(?:\\)*(?=['])";
            return Regex.Replace(s, pattern, new MatchEvaluator(ReplaceText));
        }

        /// <summary>
        /// 匹配项
        /// </summary>
        /// <param name="s"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        private static string ReplaceText(Match m)
        {
            string str = m.Value;
            if (str.Length == 0)
            {
                // 只有单引号，则转义该单引号
                return str + @"\";
            }
            else if (str.Length == 1)
            {
                // 1个反斜杠加一个单引号，则不替换
                return string.Empty;
            }
            // 对于大于2个长度的反斜杠，则返回原来的2倍
            // 即 使把每个反斜杠都转义了。
            return new string('\\', 2 * str.Length);
        }

        /// <summary>
        /// 取下一个整十
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static int NextTen(this int i)
        {
            int temp = i / 10;
            return temp * 10 + 10;  
        }

        #region Json
        public static string ToJson(this object o)
        {
            JavaScriptSerializer jSer = new JavaScriptSerializer();
            return jSer.Serialize(o);
        }
        public static T JsonToObject<T>(this string json)
        {
            JavaScriptSerializer jSer = new JavaScriptSerializer();
            return jSer.Deserialize<T>(json);
        }
        public static object JsonToObject(this string json)
        {
            JavaScriptSerializer jSer = new JavaScriptSerializer();
            return jSer.DeserializeObject(json);
        }
        public static Dictionary<string, object> JsonToDict(this string json)
        {
            JavaScriptSerializer jSer = new JavaScriptSerializer();
            return jSer.DeserializeObject(json) as Dictionary<string, object>;
        }
        #endregion
    }
}
