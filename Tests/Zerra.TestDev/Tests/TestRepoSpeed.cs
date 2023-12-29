//// Copyright © KaKush LLC
//// Written By Steven Zawaski
//// Licensed to you under the MIT license

//using Dapper;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Data;
//using System.Data.SqlClient;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;
//using Zerra.Repository;

//namespace Zerra.TestDev
//{
//    public static class TestRepoSpeed
//    {
//        public static void Load()
//        {
//            for (var i = 10; i <= 1010; i++)
//            {
//                var model = new TestCustomerSqlModel()
//                {
//                    CustomerID = i,
//                    Name = "John Galt" + i,
//                    Credit = i
//                };
//                Repo.Persist(new Create<TestCustomerSqlModel>("UpdateCustomer", model));
//            }
//        }

//        public static void Test()
//        {
//            var connectionString = new TestSqlDataContext().ConnectionString;

//            int loopswarmup = 200;
//            int loops = 1000;

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loopswarmup; i++)
//                {
//                    var data = Repo.Query(new QueryMany<TestCustomerSqlModel>(x => x.CustomerID > 0));
//                }
//                timer.Stop();
//                Console.WriteLine("Warmup_Zerra_Sql: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loopswarmup; i++)
//                {
//                    using (var context = new TestEntityFrameworkContext())
//                    {
//                        var data = context.Set<CustomerEntity>().Where(x => x.CustomerID > 0).Select(x =>
//                            new TestCustomerEntityFrameworkModel()
//                            {
//                                CustomerID = x.CustomerID,
//                                Name = x.Name,
//                                Credit = x.Credit
//                            }).ToArray();
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Warmup_Raw__EF: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loopswarmup; i++)
//                {
//                    using (IDbConnection db = new SqlConnection(connectionString))
//                    {
//                        var data = db.Query<CustomerEntity>("SELECT CustomerID, Name, Credit FROM Customer WHERE CustomerID > 0").ToArray();
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Warmup_Dapper: {0}", timer.ElapsedMilliseconds);
//            }

//            //{
//            //    var timer = Stopwatch.StartNew();
//            //    for (var i = 0; i < loopswarmup; i++)
//            //    {
//            //        var data = Repo.Query(new QueryMany<TestCustomerEntityFrameworkModel>(x => x.CustomerID > 0));
//            //    }
//            //    timer.Stop();
//            //    Console.WriteLine("Warmup_Zerra_EF_: {0}", timer.ElapsedMilliseconds);
//            //}

//            Console.WriteLine();

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    var data = Repo.Query(new QueryMany<TestCustomerSqlModel>(x => x.CustomerID > 0));
//                }
//                timer.Stop();
//                Console.WriteLine("Many_Zerra_Sql: {0}", timer.ElapsedMilliseconds);
//            }

//            //{
//            //    var timer = new Stopwatch();
//            //    timer.Start();
//            //    for (var i = 0; i < loops; i++)
//            //    {
//            //        var data = Repo.Query(new QueryMany<TestCustomerEntityFrameworkModel>(x => x.CustomerID > 0));
//            //    }
//            //    timer.Stop();
//            //    Console.WriteLine("Many_Zerra_EF_: {0}", timer.ElapsedMilliseconds);
//            //}

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (var context = new TestEntityFrameworkContext())
//                    {
//                        var data = context.Set<CustomerEntity>().Where(x => x.CustomerID > 0).Select(x =>
//                            new TestCustomerEntityFrameworkModel()
//                            {
//                                CustomerID = x.CustomerID,
//                                Name = x.Name,
//                                Credit = x.Credit
//                            }).ToArray();
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Many_Raw__EF__: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (IDbConnection db = new SqlConnection(connectionString))
//                    {
//                        var data = db.Query<CustomerEntity>("SELECT CustomerID, Name, Credit FROM Customer WHERE CustomerID > 0").ToArray();
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Many_Dapper___: {0}", timer.ElapsedMilliseconds);
//            }

//            Console.WriteLine();

//            {
//                var timer = new Stopwatch();
//                timer.Start();
//                for (var i = 0; i < loops; i++)
//                {
//                    var data = Repo.Query(new QueryFirst<TestCustomerSqlModel>(x => x.CustomerID > 0));
//                }
//                timer.Stop();
//                Console.WriteLine("First_Zerra_Sql: {0}", timer.ElapsedMilliseconds);
//            }

//            //{
//            //    var timer = Stopwatch.StartNew();
//            //    for (var i = 0; i < loops; i++)
//            //    {
//            //        var data = Repo.Query(new QueryFirst<TestCustomerEntityFrameworkModel>(x => x.CustomerID > 0));
//            //    }
//            //    timer.Stop();
//            //    Console.WriteLine("First_Zerra_EF_: {0}", timer.ElapsedMilliseconds);
//            //}

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (var context = new TestEntityFrameworkContext())
//                    {
//                        var data = context.Set<CustomerEntity>().Where(x => x.CustomerID > 0).Select(x =>
//                            new TestCustomerEntityFrameworkModel()
//                            {
//                                CustomerID = x.CustomerID,
//                                Name = x.Name,
//                                Credit = x.Credit
//                            }).First();
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("First_Raw__EF__: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (IDbConnection db = new SqlConnection(connectionString))
//                    {
//                        var data = db.QueryFirst<CustomerEntity>("SELECT TOP(1) CustomerID, Name, Credit FROM Customer WHERE CustomerID > 0");
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("First_Dapper___: {0}", timer.ElapsedMilliseconds);
//            }

//            Console.WriteLine();

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    var data = Repo.Query(new QuerySingle<TestCustomerSqlModel>(x => x.CustomerID == 10));
//                }
//                timer.Stop();
//                Console.WriteLine("Single_Zerra_Sql: {0}", timer.ElapsedMilliseconds);
//            }

//            //{
//            //    var timer = Stopwatch.StartNew();
//            //    for (var i = 0; i < loops; i++)
//            //    {
//            //        var data = Repo.Query(new QuerySingle<TestCustomerEntityFrameworkModel>(x => x.CustomerID == 10));
//            //    }
//            //    timer.Stop();
//            //    Console.WriteLine("Single_Zerra_EF_: {0}", timer.ElapsedMilliseconds);
//            //}

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (var context = new TestEntityFrameworkContext())
//                    {
//                        var data = context.Set<CustomerEntity>().Where(x => x.CustomerID == 10).Select(x =>
//                            new TestCustomerEntityFrameworkModel()
//                            {
//                                CustomerID = x.CustomerID,
//                                Name = x.Name,
//                                Credit = x.Credit,
//                            }).Single();
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Single_Raw__EF__: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (IDbConnection db = new SqlConnection(connectionString))
//                    {
//                        var data = db.QuerySingle<CustomerEntity>("SELECT TOP(2) CustomerID, Name, Credit FROM Customer WHERE CustomerID = 10");
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Single_Dapper___: {0}", timer.ElapsedMilliseconds);
//            }

//            Console.WriteLine();

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    var data = Repo.Query(new QueryCount<TestCustomerSqlModel>(x => x.CustomerID > 0));
//                }
//                timer.Stop();
//                Console.WriteLine("Count_Zerra_Sql: {0}", timer.ElapsedMilliseconds);
//            }

//            //{
//            //    var timer = Stopwatch.StartNew();
//            //    for (var i = 0; i < loops; i++)
//            //    {
//            //        var data = Repo.Query(new QueryCount<TestCustomerEntityFrameworkModel>(x => x.CustomerID > 0));
//            //    }
//            //    timer.Stop();
//            //    Console.WriteLine("Count_Zerra_EF_: {0}", timer.ElapsedMilliseconds);
//            //}

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (var context = new TestEntityFrameworkContext())
//                    {
//                        var data = context.Set<CustomerEntity>().Count(x => x.CustomerID == 10);
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Count_Raw__EF__: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (IDbConnection db = new SqlConnection(connectionString))
//                    {
//                        var data = db.ExecuteScalar<int>("SELECT COUNT(1) FROM Customer WHERE CustomerID = 10");
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Count_Dapper___: {0}", timer.ElapsedMilliseconds);
//            }

//            Console.WriteLine();

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    var data = Repo.Query(new QueryAny<TestCustomerSqlModel>(x => x.CustomerID > 0));
//                }
//                timer.Stop();
//                Console.WriteLine("Any_Zerra_Sql: {0}", timer.ElapsedMilliseconds);
//            }

//            //{
//            //    var timer = Stopwatch.StartNew();
//            //    for (var i = 0; i < loops; i++)
//            //    {
//            //        var data = Repo.Query(new QueryAny<TestCustomerEntityFrameworkModel>(x => x.CustomerID > 0));
//            //    }
//            //    timer.Stop();
//            //    Console.WriteLine("Any_Zerra_EF_: {0}", timer.ElapsedMilliseconds);
//            //}

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (var context = new TestEntityFrameworkContext())
//                    {
//                        var data = context.Set<CustomerEntity>().Any(x => x.CustomerID == 10);
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Any_Raw__EF__: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (IDbConnection db = new SqlConnection(connectionString))
//                    {
//                        var data = db.Query("SELECT TOP(1) CustomerID FROM Customer WHERE CustomerID = 10").Count() > 0;
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Any_Dapper___: {0}", timer.ElapsedMilliseconds);
//            }

//            Console.WriteLine();
//        }

//        public static async Task TestAsync()
//        {
//            var connectionString = new TestSqlDataContext().ConnectionString;

//            int loopswarmup = 100;
//            int loops = 1000;

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loopswarmup; i++)
//                {
//                    var data = await Repo.QueryAsync(new QueryMany<TestCustomerSqlModel>(x => x.CustomerID > 0));
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Warmup_Zerra_Sql: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loopswarmup; i++)
//                {
//                    var data = await Repo.QueryAsync(new QueryMany<TestCustomerEntityFrameworkModel>(x => x.CustomerID > 0));
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Warmup_Zerra_EF_: {0}", timer.ElapsedMilliseconds);
//            }

//            Console.WriteLine();

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    var data = await Repo.QueryAsync(new QueryMany<TestCustomerSqlModel>(x => x.CustomerID > 0));
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Many_Zerra_Sql: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = new Stopwatch();
//                timer.Start();
//                for (var i = 0; i < loops; i++)
//                {
//                    var data = await Repo.QueryAsync(new QueryMany<TestCustomerEntityFrameworkModel>(x => x.CustomerID > 0));
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Many_Zerra_EF_: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (var context = new TestEntityFrameworkContext())
//                    {
//                        var data = await context.Set<CustomerEntity>().Where(x => x.CustomerID > 0).Select(x =>
//                            new TestCustomerEntityFrameworkModel()
//                            {
//                                CustomerID = x.CustomerID,
//                                Name = x.Name,
//                                Credit = x.Credit
//                            }).ToArrayAsync();
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Many_Raw__EF__: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (IDbConnection db = new SqlConnection(connectionString))
//                    {
//                        var data = (await db.QueryAsync<CustomerEntity>("SELECT CustomerID, Name, Credit FROM Customer WHERE CustomerID > 0")).ToArray();
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Many_Dapper___: {0}", timer.ElapsedMilliseconds);
//            }

//            Console.WriteLine();

//            {
//                var timer = new Stopwatch();
//                timer.Start();
//                for (var i = 0; i < loops; i++)
//                {
//                    var data = await Repo.QueryAsync(new QueryFirst<TestCustomerSqlModel>(x => x.CustomerID > 0));
//                }
//                timer.Stop();
//                Console.WriteLine("Async_First_Zerra_Sql: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    var data = await Repo.QueryAsync(new QueryFirst<TestCustomerEntityFrameworkModel>(x => x.CustomerID > 0));
//                }
//                timer.Stop();
//                Console.WriteLine("Async_First_Zerra_EF_: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (var context = new TestEntityFrameworkContext())
//                    {
//                        var data = await context.Set<CustomerEntity>().Where(x => x.CustomerID > 0).Select(x =>
//                            new TestCustomerEntityFrameworkModel()
//                            {
//                                CustomerID = x.CustomerID,
//                                Name = x.Name,
//                                Credit = x.Credit
//                            }).FirstAsync();
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Async_First_Raw__EF__: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (IDbConnection db = new SqlConnection(connectionString))
//                    {
//                        var data = db.QueryFirstAsync<CustomerEntity>("SELECT Top(1) CustomerID, Name, Credit FROM Customer WHERE CustomerID > 0");
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Async_First_Dapper___: {0}", timer.ElapsedMilliseconds);
//            }

//            Console.WriteLine();

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    var data = await Repo.QueryAsync(new QuerySingle<TestCustomerSqlModel>(x => x.CustomerID == 10));
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Single_Zerra_Sql: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    var data = await Repo.QueryAsync(new QuerySingle<TestCustomerEntityFrameworkModel>(x => x.CustomerID == 10));
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Single_Zerra_EF_: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (var context = new TestEntityFrameworkContext())
//                    {
//                        var data = await context.Set<CustomerEntity>().Where(x => x.CustomerID == 10).Select(x =>
//                            new TestCustomerEntityFrameworkModel()
//                            {
//                                CustomerID = x.CustomerID,
//                                Name = x.Name,
//                                Credit = x.Credit,
//                            }).SingleAsync();
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Single_Raw__EF__: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (IDbConnection db = new SqlConnection(connectionString))
//                    {
//                        var data = await db.QuerySingleAsync<CustomerEntity>("SELECT TOP(2) CustomerID, Name, Credit FROM Customer WHERE CustomerID = 10");
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Single_Dapper___: {0}", timer.ElapsedMilliseconds);
//            }

//            Console.WriteLine();

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    var data = await Repo.QueryAsync(new QueryCount<TestCustomerSqlModel>(x => x.CustomerID > 0));
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Count_Zerra_Sql: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    var data = await Repo.QueryAsync(new QueryCount<TestCustomerEntityFrameworkModel>(x => x.CustomerID > 0));
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Count_Zerra_EF_: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (var context = new TestEntityFrameworkContext())
//                    {
//                        var data = await context.Set<CustomerEntity>().CountAsync(x => x.CustomerID == 10);
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Count_Raw__EF__: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (IDbConnection db = new SqlConnection(connectionString))
//                    {
//                        var data = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Customer WHERE CustomerID = 10");
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Count_Dapper___: {0}", timer.ElapsedMilliseconds);
//            }

//            Console.WriteLine();

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    var data = await Repo.QueryAsync(new QueryAny<TestCustomerSqlModel>(x => x.CustomerID > 0));
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Any_Zerra_Sql: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    var data = await Repo.QueryAsync(new QueryAny<TestCustomerEntityFrameworkModel>(x => x.CustomerID > 0));
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Any_Zerra_EF_: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (var context = new TestEntityFrameworkContext())
//                    {
//                        var data = await context.Set<CustomerEntity>().AnyAsync(x => x.CustomerID == 10);
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Any_Raw__EF__: {0}", timer.ElapsedMilliseconds);
//            }

//            {
//                var timer = Stopwatch.StartNew();
//                for (var i = 0; i < loops; i++)
//                {
//                    using (IDbConnection db = new SqlConnection(connectionString))
//                    {
//                        var data = (await db.QueryAsync("SELECT TOP(1) CustomerID FROM Customer WHERE CustomerID = 10")).Count() > 0;
//                    }
//                }
//                timer.Stop();
//                Console.WriteLine("Async_Any_Dapper___: {0}", timer.ElapsedMilliseconds);
//            }

//            Console.WriteLine();
//        }
//    }
//}
