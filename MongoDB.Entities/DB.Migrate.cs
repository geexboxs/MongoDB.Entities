using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Discover and run migrations from the same assembly as the specified type.
        /// </summary>
        /// <typeparam name="T">A type that is from the same assembly as the migrations you want to run</typeparam>
        public static async Task MigrateAsync<T>() where T : class
        {
            await MigrateAsync(typeof(T)).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes migration classes that implement the IMigration interface in the correct order to transform the database.
        /// <para>TIP: Write classes with names such as: _001_rename_a_field.cs, _002_delete_a_field.cs, etc. and implement IMigration interface on them. Call this method at the startup of the application in order to run the migrations.</para>
        /// </summary>
        public static async Task MigrateAsync()
        {
            await MigrateAsync(null).ConfigureAwait(false);
        }

        private static async Task MigrateAsync(Type targetType)
        {
            IEnumerable<Assembly> assemblies;

            if (targetType == null)
            {
                var excludes = new[]
                {
                    "Microsoft.",
                    "System.",
                    "MongoDB.",
                    "testhost.",
                    "netstandard",
                    "Newtonsoft.",
                    "mscorlib",
                    "NuGet."
                };

                assemblies = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(a =>
                          (!a.IsDynamic && !excludes.Any(n => a.FullName.StartsWith(n))) ||
                          a.FullName.StartsWith("MongoDB.Entities.Tests"));
            }
            else
            {
                assemblies = new[] { targetType.Assembly }.Concat(targetType.Assembly.GetReferencedAssemblies().Select(Assembly.Load));
            }
            using var dbContext = new DbContext(transactional: true);
            var types = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetInterfaces().Contains(typeof(IMigration)));

            //if (!types.Any())
            //    throw new InvalidOperationException("Didn't find any classes that implement IMigration interface.");

            var lastMigNum = (
                await dbContext.Find<Migration, long>()
                      .Sort(m => m.Number, Order.Descending)
                      .Limit(1)
                      .Project(m => m.Number)
                      .ExecuteAsync()
                      .ConfigureAwait(false))
                .SingleOrDefault();

            var migrations = new SortedDictionary<long, IMigration>();

            foreach (var t in types)
            {
                var success = long.TryParse(t.Name.Split('_')[1], out long migNum);

                if (!success)
                    throw new InvalidOperationException("Failed to parse migration number from the class name. Make sure to name the migration classes like: _20210623103815_some_migration_name.cs");

                if (migNum > lastMigNum)
                    migrations.Add(migNum, (IMigration)Activator.CreateInstance(t));
            }

            var sw = new Stopwatch();

            foreach (var migration in migrations)
            {
                sw.Start();
                await migration.Value.UpgradeAsync(dbContext).ConfigureAwait(false);
                var mig = new Migration
                {
                    Number = migration.Key,
                    Name = migration.Value.GetType().Name,
                    TimeTakenSeconds = sw.Elapsed.TotalSeconds
                };
                dbContext.AttachContextSession(mig);
                await mig.SaveAsync().ConfigureAwait(false);
                sw.Stop();
                sw.Reset();
            }

            if (migrations.Any())
            {
                await dbContext.CommitAsync().ConfigureAwait(false);
            }
        }
    }
}
