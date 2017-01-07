using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace KhepriRevit
{
    public class RMIfy
    {
        //Reflection machinery
        static MethodInfo GetMethod(Type t, String name)
        {
            MethodInfo m = t.GetMethod(name);
            Debug.Assert(m != null, "There is no method named " + name);
            return m;
        }

        static String MethodNameFromType(Type t) => t.Name.Replace("[]", "Array");

        static MethodCallExpression DeserializeParameter(ParameterExpression c, ParameterInfo p) =>
            Expression.Call(c, GetMethod(c.Type, "r" + MethodNameFromType(p.ParameterType)));

        static Expression SerializeReturn(ParameterExpression c, ParameterInfo p, Expression e)
        {
            var writer = GetMethod(c.Type, "w" + MethodNameFromType(p.ParameterType));
            if (p.ParameterType == typeof(void))
                return Expression.Block(e, Expression.Call(c, writer));
            else
                return Expression.Call(c, writer, e);
        }

        static Expression SerializeErrors(ParameterExpression c, ParameterInfo p, Expression e)
        {
            var reporter = GetMethod(c.Type, "e" + MethodNameFromType(p.ParameterType));
            var ex = Expression.Parameter(typeof(Exception), "ex");
            return Expression.TryCatch(e,
                Expression.Catch(ex,
                    Expression.Block(
                        Expression.Call(c, reporter, ex))));
        }

        static Action<T, P> GenerateRMIFor<T, P>(T channel, P primitives, MethodInfo f)
        {
            ParameterExpression c = Expression.Parameter(typeof(T), "channel");
            ParameterExpression pr = Expression.Parameter(typeof(P), "primitives");
            BlockExpression block = Expression.Block(
                SerializeErrors(
                    c,
                    f.ReturnParameter,
                    SerializeReturn(
                        c,
                        f.ReturnParameter,
                        Expression.Call(
                            pr,
                            f,
                            f.GetParameters().Select(p => DeserializeParameter(c, p))))));
            return Expression.Lambda<Action<T, P>>(block, new ParameterExpression[] { c, pr }).Compile();
        }

        public static Action<T, P> RMIFor<T, P>(T channel, P primitives, String name)
        {
            MethodInfo f = GetMethod(typeof(P), name);
            return (f == null) ? null : GenerateRMIFor(channel, primitives, f);
        }
    }
}
