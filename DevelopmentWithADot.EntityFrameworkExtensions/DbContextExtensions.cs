using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace DevelopmentWithADot.EntityFrameworkExtensions
{
	public static class DbContextExtensions
	{
		public static Object Load(this DbContext context, Type type, params Object[] keyValues)
		{
			return (context.Set(type).Find(keyValues));
		}

		public static T Load<T>(this DbContext context, params Object[] keyValues) where T : class
		{
			return (context.Set<T>().Find(keyValues));
		}

		public static IQueryable<T> LocalOrDatabase<T>(this DbContext context, Expression<Func<T, Boolean>> expression) where T : class
		{
			IEnumerable<T> localResults = context.Set<T>().Local.Where(expression.Compile());

			if (localResults.Any() == true)
			{
				return (localResults.AsQueryable());
			}

			IQueryable<T> databaseResults = context.Set<T>().Where(expression);

			return (databaseResults);
		}		
	}
}