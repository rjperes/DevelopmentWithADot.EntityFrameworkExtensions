using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Metadata.Edm;
using System.Data.Objects;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace DevelopmentWithADot.EntityFrameworkExtensions
{
	public static class DbContextExtensions
	{
		public static void BulkInsert<T>(this DbContext ctx, IEnumerable<T> items)
		{
			using (SqlBulkCopy bcp = new SqlBulkCopy(ctx.Database.Connection as SqlConnection))
			{
				DataTable table = new DataTable();
				IEnumerable<PropertyDescriptor> columns = TypeDescriptor.GetProperties(typeof(T))
					.OfType<PropertyDescriptor>()
					.Where(x => (typeof(IEnumerable).IsAssignableFrom(x.PropertyType) == false) || x.PropertyType == typeof(String))
					.Where(x => x.IsReadOnly == false);
				ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;

				foreach (PropertyDescriptor column in columns)
				{
					table.Columns.Add(column.Name, column.PropertyType);
				}

				table.BeginLoadData();

				EntitySetBase entitySet = octx.MetadataWorkspace.GetItemCollection(DataSpace.SSpace)
				.GetItems<EntityContainer>()
				.Single()
				.BaseEntitySets
				.Where(x => x.Name == typeof(T).Name)
				.Single();

				bcp.DestinationTableName = entitySet.MetadataProperties["Table"].Value.ToString(); 

				foreach (T item in items)
				{
					DataRow row = table.NewRow();
					table.Rows.Add(row);

					foreach (PropertyDescriptor column in columns)
					{
						row[column.Name] = column.GetValue(item);
					}
				}

				table.EndLoadData();

				bcp.WriteToServer(table);
			}
		}

		public static IEnumerable<T> ExecuteQuery<T>(this DbContext ctx, String entitySql, params Object [] parameters)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;

			entitySql = Regex.Replace(entitySql, "{(?<x>\\d+)}", "@p${x}");

			return (octx.CreateQuery<T>(entitySql, parameters.Select((element, index) => new ObjectParameter(String.Format("p{0}", index), element)).ToArray()).ToList());
		}

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