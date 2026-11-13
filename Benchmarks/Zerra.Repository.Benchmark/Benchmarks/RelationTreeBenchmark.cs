// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Zerra.Repository.MsSql;
using Zerra.Repository.Test.MsSql;

namespace Zerra.Repository.Benchmark.Benchmarks
{
    //a tree of relations four levels deep with lookups at two levels, read by graph, shaped like an integration's sync history
    [MemoryDiagnoser]
    [MedianColumn, MinColumn, MaxColumn]
    [SimpleJob(warmupCount: 2, iterationCount: 5)]
    public class RelationTreeBenchmarks
    {
        private const string database = "ZerraRelationTreeBench";
        private const int executions = 10_000;
        private const int syncs = 50;
        private const int stepsPerSync = 8;
        private const int companies = 25;

        public static string ConnectionString => new SqlConnectionStringBuilder(MsSqlTestSqlDataContext.ConnectionString) { InitialCatalog = database }.ToString();

        private IRepo repo = null!;
        private IRepo layered = null!;
        private Guid executionId;
        private int companyId;

        [GlobalSetup]
        public async Task Setup()
        {
            var master = new SqlConnectionStringBuilder(MsSqlTestSqlDataContext.ConnectionString) { InitialCatalog = "master" }.ToString();
            using (var connection = new SqlConnection(master))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = $"IF DB_ID('{database}') IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}] END; CREATE DATABASE [{database}]";
                _ = command.ExecuteNonQuery();
            }

            var engine = new MsSqlEngine(ConnectionString);
            Type[] modelTypes = [typeof(TreeSync), typeof(TreeSyncExecution), typeof(TreeSyncExecutionMessage), typeof(TreeSyncStep), typeof(TreeSyncStepExecution), typeof(TreeSyncStepExecutionMessage), typeof(TreeSyncStepPerformance), typeof(TreeSyncStepExecutionParameter), typeof(TreeSyncStepExecutionDetail), typeof(TreeSyncStepExecutionDetailFile)];
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints, modelTypes);

            var repoSetup = Repo.New();
            repoSetup.AddProvider(new TransactStoreProvider<TreeSync>(engine));
            repoSetup.AddProvider(new TransactStoreProvider<TreeSyncExecution>(engine));
            repoSetup.AddProvider(new TransactStoreProvider<TreeSyncExecutionMessage>(engine));
            repoSetup.AddProvider(new TransactStoreProvider<TreeSyncStep>(engine));
            repoSetup.AddProvider(new TransactStoreProvider<TreeSyncStepExecution>(engine));
            repoSetup.AddProvider(new TransactStoreProvider<TreeSyncStepExecutionMessage>(engine));
            repoSetup.AddProvider(new TransactStoreProvider<TreeSyncStepPerformance>(engine));
            repoSetup.AddProvider(new TransactStoreProvider<TreeSyncStepExecutionParameter>(engine));
            repoSetup.AddProvider(new TransactStoreProvider<TreeSyncStepExecutionDetail>(engine));
            repoSetup.AddProvider(new TransactStoreProvider<TreeSyncStepExecutionDetailFile>(engine));
            repo = repoSetup;

            //every model behind a rule that changes nothing on reads, as a layer for writes only is
            var layeredSetup = Repo.New();
            layeredSetup.AddProvider<TreeSync>(new PassRules<TreeSync>(new TransactStoreProvider<TreeSync>(engine)));
            layeredSetup.AddProvider<TreeSyncExecution>(new PassRules<TreeSyncExecution>(new TransactStoreProvider<TreeSyncExecution>(engine)));
            layeredSetup.AddProvider<TreeSyncExecutionMessage>(new PassRules<TreeSyncExecutionMessage>(new TransactStoreProvider<TreeSyncExecutionMessage>(engine)));
            layeredSetup.AddProvider<TreeSyncStep>(new PassRules<TreeSyncStep>(new TransactStoreProvider<TreeSyncStep>(engine)));
            layeredSetup.AddProvider<TreeSyncStepExecution>(new PassRules<TreeSyncStepExecution>(new TransactStoreProvider<TreeSyncStepExecution>(engine)));
            layeredSetup.AddProvider<TreeSyncStepExecutionMessage>(new PassRules<TreeSyncStepExecutionMessage>(new TransactStoreProvider<TreeSyncStepExecutionMessage>(engine)));
            layeredSetup.AddProvider<TreeSyncStepPerformance>(new PassRules<TreeSyncStepPerformance>(new TransactStoreProvider<TreeSyncStepPerformance>(engine)));
            layeredSetup.AddProvider<TreeSyncStepExecutionParameter>(new PassRules<TreeSyncStepExecutionParameter>(new TransactStoreProvider<TreeSyncStepExecutionParameter>(engine)));
            layeredSetup.AddProvider<TreeSyncStepExecutionDetail>(new PassRules<TreeSyncStepExecutionDetail>(new TransactStoreProvider<TreeSyncStepExecutionDetail>(engine)));
            layeredSetup.AddProvider<TreeSyncStepExecutionDetailFile>(new PassRules<TreeSyncStepExecutionDetailFile>(new TransactStoreProvider<TreeSyncStepExecutionDetailFile>(engine)));
            layered = layeredSetup;

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandTimeout = 600;
                command.CommandText = Seed;
                _ = command.ExecuteNonQuery();
                command.CommandText = $"SELECT[AlternateId] FROM[Integration].[SyncExecution] WHERE[Id]={executions / 2}";
                executionId = (Guid)command.ExecuteScalar()!;
            }
            companyId = 7;

            //both sides read the same rows
            var zerra = (await ExecutionTree_Zerra())!;
            var ef = (await ExecutionTree_EF())!;
            var zerraCount = $"{zerra.Sync.Id} {zerra.SyncExecutionMessages.Length} {zerra.SyncStepExecutions.Length} {zerra.SyncStepExecutions.Sum(x => x.SyncStepExecutionMessages.Length + x.SyncStepPerformances.Length + x.SyncStepExecutionParameters.Length + x.SyncStepExecutionDetails.Sum(d => d.SyncStepExecutionDetailFiles.Length))} {zerra.SyncStepExecutions.Sum(x => x.SyncStep.Id)}";
            var efCount = $"{ef.Sync.Id} {ef.SyncExecutionMessages.Count} {ef.SyncStepExecutions.Count} {ef.SyncStepExecutions.Sum(x => x.SyncStepExecutionMessages.Count + x.SyncStepPerformances.Count + x.SyncStepExecutionParameters.Count + x.SyncStepExecutionDetails.Sum(d => d.SyncStepExecutionDetailFiles.Count))} {ef.SyncStepExecutions.Sum(x => x.SyncStep.Id)}";
            var zerraCompany = await HistoryForCompany_Zerra();
            var efCompany = await HistoryForCompany_EF();
            zerraCount += $" {zerraCompany.Length} {String.Join(",", zerraCompany.Take(20).Select(x => x.Id))} {zerraCompany.Sum(x => x.SyncExecutionMessages.Length + x.Sync.SyncSteps.Length + x.SyncStepExecutions.Sum(s => s.SyncStepExecutionDetails.Length))}";
            efCount += $" {efCompany.Length} {String.Join(",", efCompany.Take(20).Select(x => x.Id))} {efCompany.Sum(x => x.SyncExecutionMessages.Count + x.Sync.SyncSteps.Count + x.SyncStepExecutions.Sum(s => s.SyncStepExecutionDetails.Count))}";
            if (zerraCount != efCount)
                throw new Exception($"Zerra read {zerraCount}, EF read {efCount}");
            var rules = (await ExecutionTreeRules_Zerra())!;
            var rulesCompany = await HistoryForCompanyRules_Zerra();
            var rulesCount = $"{rules.Sync.Id} {rules.SyncExecutionMessages.Length} {rules.SyncStepExecutions.Length} {rules.SyncStepExecutions.Sum(x => x.SyncStepExecutionMessages.Length + x.SyncStepPerformances.Length + x.SyncStepExecutionParameters.Length + x.SyncStepExecutionDetails.Sum(d => d.SyncStepExecutionDetailFiles.Length))} {rules.SyncStepExecutions.Sum(x => x.SyncStep.Id)}"
                + $" {rulesCompany.Length} {String.Join(",", rulesCompany.Take(20).Select(x => x.Id))} {rulesCompany.Sum(x => x.SyncExecutionMessages.Length + x.Sync.SyncSteps.Length + x.SyncStepExecutions.Sum(s => s.SyncStepExecutionDetails.Length))}";
            if (rulesCount != zerraCount)
                throw new Exception($"Zerra with rules read {rulesCount}, without read {zerraCount}");
        }

        private sealed class PassRules<TModel>(ITransactStoreProvider<TModel> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<TModel>, TModel>(next) where TModel : class, new() { }

        [GlobalCleanup]
        public void Cleanup()
        {
            SqlConnection.ClearAllPools();
            var master = new SqlConnectionStringBuilder(MsSqlTestSqlDataContext.ConnectionString) { InitialCatalog = "master" }.ToString();
            using var connection = new SqlConnection(master);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]";
            _ = command.ExecuteNonQuery();
        }

        private static string Numbers(int count) => $"WITH N AS(SELECT TOP({count}) ROW_NUMBER()OVER(ORDER BY(SELECT NULL)) AS I FROM sys.all_objects a CROSS JOIN sys.all_objects b CROSS JOIN sys.all_objects c)";
        private static readonly string Seed = $"""
            {Numbers(syncs)} INSERT INTO[Integration].[Sync]([Id],[Name],[IsEnabled]) SELECT I,CONCAT('Sync ',I),1 FROM N;
            {Numbers(syncs * stepsPerSync)} INSERT INTO[Integration].[SyncStep]([Id],[SyncId],[Name],[StepOrder]) SELECT I,(I-1)/{stepsPerSync}+1,CONCAT('Step ',I),(I-1)%{stepsPerSync}+1 FROM N;
            {Numbers(executions)} INSERT INTO[Integration].[SyncExecution]([Id],[AlternateId],[SyncId],[CompanyId],[StartedOn],[LastEventOn],[Status]) SELECT I,NEWID(),CASE WHEN I%97=0 THEN NULL ELSE I%{syncs}+1 END,I%{companies}+1,CASE WHEN I%10=0 THEN NULL ELSE DATEADD(MINUTE,-8*I,SYSUTCDATETIME()) END,DATEADD(MINUTE,5-8*I,SYSUTCDATETIME()),'Complete' FROM N;
            {Numbers(executions * 3)} INSERT INTO[Integration].[SyncExecutionMessage]([Id],[SyncExecutionId],[Message],[CreatedOn]) SELECT I,(I-1)/3+1,CONCAT('Execution message ',I),DATEADD(SECOND,I,'2025-01-01') FROM N;
            {Numbers(executions * stepsPerSync)} INSERT INTO[Integration].[SyncStepExecution]([Id],[SyncExecutionId],[SyncStepId],[StartedOn],[Status]) SELECT I,(I-1)/{stepsPerSync}+1,(((I-1)/{stepsPerSync}+1)%{syncs})*{stepsPerSync}+(I-1)%{stepsPerSync}+1,DATEADD(SECOND,I,'2025-01-01'),'Complete' FROM N;
            {Numbers(executions * stepsPerSync * 2)} INSERT INTO[Integration].[SyncStepExecutionMessage]([Id],[SyncStepExecutionId],[Message]) SELECT I,(I-1)/2+1,CONCAT('Step message ',I) FROM N;
            {Numbers(executions * stepsPerSync)} INSERT INTO[Integration].[SyncStepPerformance]([Id],[SyncStepExecutionId],[Milliseconds],[Records]) SELECT I,I,I%5000,I%300 FROM N;
            {Numbers(executions * stepsPerSync * 3)} INSERT INTO[Integration].[SyncStepExecutionParameter]([Id],[SyncStepExecutionId],[Name],[Value]) SELECT I,(I-1)/3+1,CONCAT('Parameter',I%3),CONCAT('Value ',I) FROM N;
            {Numbers(executions * stepsPerSync * 2)} INSERT INTO[Integration].[SyncStepExecutionDetail]([Id],[SyncStepExecutionId],[Description]) SELECT I,(I-1)/2+1,CONCAT('Detail ',I) FROM N;
            {Numbers(executions * stepsPerSync * 2)} INSERT INTO[Integration].[SyncStepExecutionDetailFile]([Id],[SyncStepExecutionDetailId],[UrlToFile],[ShowToCustomer],[IsOutput],[AlternateId]) SELECT I,I,CONCAT('https://files.example.com/',NEWID()),I%2,I%3,NEWID() FROM N;
            """;

        private static Graph<TreeSyncExecution> TreeGraph()
        {
            var details = new Graph<TreeSyncStepExecutionDetail>(true, x => x.SyncStepExecutionDetailFiles);
            var steps = new Graph<TreeSyncStepExecution>(true, x => x.SyncStep, x => x.SyncStepExecutionMessages, x => x.SyncStepPerformances, x => x.SyncStepExecutionParameters);
            steps.AddChildGraph(nameof(TreeSyncStepExecution.SyncStepExecutionDetails), details);
            var graph = new Graph<TreeSyncExecution>(true, x => x.Sync, x => x.SyncExecutionMessages);
            graph.AddChildGraph(nameof(TreeSyncExecution.SyncStepExecutions), steps);
            return graph;
        }

        private static Graph<TreeSyncExecution> CompanyGraph()
        {
            var steps = new Graph<TreeSyncStepExecution>(true, x => x.SyncStepExecutionDetails);
            var graph = new Graph<TreeSyncExecution>(true, x => x.SyncExecutionMessages);
            graph.AddChildGraph(nameof(TreeSyncExecution.Sync), new Graph<TreeSync>(true, x => x.SyncSteps));
            graph.AddChildGraph(nameof(TreeSyncExecution.SyncStepExecutions), steps);
            return graph;
        }

        [Benchmark]
        public Task<TreeSyncExecution?> ExecutionTree_Zerra() => repo.Query<TreeSyncExecution>(TreeGraph()).Where(x => x.AlternateId == executionId).FirstOrDefaultAsync();

        [Benchmark]
        public Task<TreeSyncExecution?> ExecutionTreeRules_Zerra() => layered.Query<TreeSyncExecution>(TreeGraph()).Where(x => x.AlternateId == executionId).FirstOrDefaultAsync();

        [Benchmark]
        public async Task<EFSyncExecution?> ExecutionTree_EF()
        {
            using var context = new TreeDataContext();
            return await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.SyncExecutions.AsNoTracking().AsSplitQuery()
                .Include(x => x.Sync)
                .Include(x => x.SyncExecutionMessages)
                .Include(x => x.SyncStepExecutions).ThenInclude(x => x.SyncStep)
                .Include(x => x.SyncStepExecutions).ThenInclude(x => x.SyncStepExecutionMessages)
                .Include(x => x.SyncStepExecutions).ThenInclude(x => x.SyncStepPerformances)
                .Include(x => x.SyncStepExecutions).ThenInclude(x => x.SyncStepExecutionParameters)
                .Include(x => x.SyncStepExecutions).ThenInclude(x => x.SyncStepExecutionDetails).ThenInclude(x => x.SyncStepExecutionDetailFiles),
                x => x.AlternateId == executionId);
        }

        [Benchmark]
        public Task<TreeSyncExecution[]> HistoryForCompany_Zerra()
        {
            var enabledOnly = false;
            return repo.Query<TreeSyncExecution>(CompanyGraph())
                .Where(x => x.CompanyId == companyId && x.SyncId != null && (x.Sync.IsEnabled || !enabledOnly) && (x.StartedOn == null || x.StartedOn >= DateTime.UtcNow.AddDays(-45)))
                .OrderByDescending(x => x.StartedOn ?? x.LastEventOn)
                .ToArrayAsync();
        }

        [Benchmark]
        public Task<TreeSyncExecution[]> HistoryForCompanyRules_Zerra()
        {
            var enabledOnly = false;
            return layered.Query<TreeSyncExecution>(CompanyGraph())
                .Where(x => x.CompanyId == companyId && x.SyncId != null && (x.Sync.IsEnabled || !enabledOnly) && (x.StartedOn == null || x.StartedOn >= DateTime.UtcNow.AddDays(-45)))
                .OrderByDescending(x => x.StartedOn ?? x.LastEventOn)
                .ToArrayAsync();
        }

        [Benchmark]
        public async Task<EFSyncExecution[]> HistoryForCompany_EF()
        {
            var enabledOnly = false;
            using var context = new TreeDataContext();
            return await EntityFrameworkQueryableExtensions.ToArrayAsync(context.SyncExecutions.AsNoTracking().AsSplitQuery()
                .Include(x => x.SyncExecutionMessages)
                .Include(x => x.Sync).ThenInclude(x => x.SyncSteps)
                .Include(x => x.SyncStepExecutions).ThenInclude(x => x.SyncStepExecutionDetails)
                .Where(x => x.CompanyId == companyId && x.Sync != null && (x.Sync.IsEnabled || !enabledOnly) && (x.StartedOn == null || x.StartedOn >= DateTime.UtcNow.AddDays(-45)))
                .OrderByDescending(x => x.StartedOn ?? x.LastEventOn));
        }
    }

    [Entity("Integration", "Sync")]
    public sealed class TreeSync
    {
        [Identity]
        public int Id { get; set; }
        [StoreProperties(true, 100)]
        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        [Relation(nameof(TreeSyncStep.SyncId))]
        public TreeSyncStep[] SyncSteps { get; set; }
    }

    [Entity("Integration", "SyncExecution")]
    public sealed class TreeSyncExecution
    {
        [Identity]
        public int Id { get; set; }
        [StoreIndex(Unique = true)]
        public Guid AlternateId { get; set; }
        [StoreIndex]
        public int? SyncId { get; set; }
        [StoreIndex]
        public int CompanyId { get; set; }
        public DateTime? StartedOn { get; set; }
        public DateTime? LastEventOn { get; set; }
        [StoreProperties(true, 50)]
        public string Status { get; set; }
        [Relation(nameof(SyncId))]
        public TreeSync Sync { get; set; }
        [Relation(nameof(TreeSyncExecutionMessage.SyncExecutionId))]
        public TreeSyncExecutionMessage[] SyncExecutionMessages { get; set; }
        [Relation(nameof(TreeSyncStepExecution.SyncExecutionId))]
        public TreeSyncStepExecution[] SyncStepExecutions { get; set; }
    }

    [Entity("Integration", "SyncExecutionMessage")]
    public sealed class TreeSyncExecutionMessage
    {
        [Identity]
        public int Id { get; set; }
        [StoreIndex]
        public int SyncExecutionId { get; set; }
        public string Message { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    [Entity("Integration", "SyncStep")]
    public sealed class TreeSyncStep
    {
        [Identity]
        public int Id { get; set; }
        [StoreIndex]
        public int SyncId { get; set; }
        [StoreProperties(true, 100)]
        public string Name { get; set; }
        public int StepOrder { get; set; }
    }

    [Entity("Integration", "SyncStepExecution")]
    public sealed class TreeSyncStepExecution
    {
        [Identity]
        public int Id { get; set; }
        [StoreIndex]
        public int SyncExecutionId { get; set; }
        [StoreIndex]
        public int SyncStepId { get; set; }
        public DateTime StartedOn { get; set; }
        [StoreProperties(true, 50)]
        public string Status { get; set; }
        [Relation(nameof(SyncStepId))]
        public TreeSyncStep SyncStep { get; set; }
        [Relation(nameof(TreeSyncStepExecutionMessage.SyncStepExecutionId))]
        public TreeSyncStepExecutionMessage[] SyncStepExecutionMessages { get; set; }
        [Relation(nameof(TreeSyncStepPerformance.SyncStepExecutionId))]
        public TreeSyncStepPerformance[] SyncStepPerformances { get; set; }
        [Relation(nameof(TreeSyncStepExecutionParameter.SyncStepExecutionId))]
        public TreeSyncStepExecutionParameter[] SyncStepExecutionParameters { get; set; }
        [Relation(nameof(TreeSyncStepExecutionDetail.SyncStepExecutionId))]
        public TreeSyncStepExecutionDetail[] SyncStepExecutionDetails { get; set; }
    }

    [Entity("Integration", "SyncStepExecutionMessage")]
    public sealed class TreeSyncStepExecutionMessage
    {
        [Identity]
        public int Id { get; set; }
        [StoreIndex]
        public int SyncStepExecutionId { get; set; }
        public string Message { get; set; }
    }

    [Entity("Integration", "SyncStepPerformance")]
    public sealed class TreeSyncStepPerformance
    {
        [Identity]
        public int Id { get; set; }
        [StoreIndex]
        public int SyncStepExecutionId { get; set; }
        public long Milliseconds { get; set; }
        public int Records { get; set; }
    }

    [Entity("Integration", "SyncStepExecutionParameter")]
    public sealed class TreeSyncStepExecutionParameter
    {
        [Identity]
        public int Id { get; set; }
        [StoreIndex]
        public int SyncStepExecutionId { get; set; }
        [StoreProperties(true, 100)]
        public string Name { get; set; }
        public string Value { get; set; }
    }

    [Entity("Integration", "SyncStepExecutionDetail")]
    public sealed class TreeSyncStepExecutionDetail
    {
        [Identity]
        public int Id { get; set; }
        [StoreIndex]
        public int SyncStepExecutionId { get; set; }
        public string Description { get; set; }
        [Relation(nameof(TreeSyncStepExecutionDetailFile.SyncStepExecutionDetailId))]
        public TreeSyncStepExecutionDetailFile[] SyncStepExecutionDetailFiles { get; set; }
    }

    [Entity("Integration", "SyncStepExecutionDetailFile")]
    public sealed class TreeSyncStepExecutionDetailFile
    {
        [Identity]
        public int Id { get; set; }
        [StoreIndex]
        public int SyncStepExecutionDetailId { get; set; }
        public string UrlToFile { get; set; }
        public bool ShowToCustomer { get; set; }
        public bool IsOutput { get; set; }
        public Guid AlternateId { get; set; }
    }

    public sealed class EFSync
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public List<EFSyncStep> SyncSteps { get; set; }
    }
    public sealed class EFSyncExecution
    {
        public int Id { get; set; }
        public Guid AlternateId { get; set; }
        public int? SyncId { get; set; }
        public int CompanyId { get; set; }
        public DateTime? StartedOn { get; set; }
        public DateTime? LastEventOn { get; set; }
        public string Status { get; set; }
        public EFSync Sync { get; set; }
        public List<EFSyncExecutionMessage> SyncExecutionMessages { get; set; }
        public List<EFSyncStepExecution> SyncStepExecutions { get; set; }
    }
    public sealed class EFSyncExecutionMessage
    {
        public int Id { get; set; }
        public int SyncExecutionId { get; set; }
        public string Message { get; set; }
        public DateTime CreatedOn { get; set; }
    }
    public sealed class EFSyncStep
    {
        public int Id { get; set; }
        public int SyncId { get; set; }
        public string Name { get; set; }
        public int StepOrder { get; set; }
    }
    public sealed class EFSyncStepExecution
    {
        public int Id { get; set; }
        public int SyncExecutionId { get; set; }
        public int SyncStepId { get; set; }
        public DateTime StartedOn { get; set; }
        public string Status { get; set; }
        public EFSyncStep SyncStep { get; set; }
        public List<EFSyncStepExecutionMessage> SyncStepExecutionMessages { get; set; }
        public List<EFSyncStepPerformance> SyncStepPerformances { get; set; }
        public List<EFSyncStepExecutionParameter> SyncStepExecutionParameters { get; set; }
        public List<EFSyncStepExecutionDetail> SyncStepExecutionDetails { get; set; }
    }
    public sealed class EFSyncStepExecutionMessage
    {
        public int Id { get; set; }
        public int SyncStepExecutionId { get; set; }
        public string Message { get; set; }
    }
    public sealed class EFSyncStepPerformance
    {
        public int Id { get; set; }
        public int SyncStepExecutionId { get; set; }
        public long Milliseconds { get; set; }
        public int Records { get; set; }
    }
    public sealed class EFSyncStepExecutionParameter
    {
        public int Id { get; set; }
        public int SyncStepExecutionId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
    public sealed class EFSyncStepExecutionDetail
    {
        public int Id { get; set; }
        public int SyncStepExecutionId { get; set; }
        public string Description { get; set; }
        public List<EFSyncStepExecutionDetailFile> SyncStepExecutionDetailFiles { get; set; }
    }
    public sealed class EFSyncStepExecutionDetailFile
    {
        public int Id { get; set; }
        public int SyncStepExecutionDetailId { get; set; }
        public string UrlToFile { get; set; }
        public bool ShowToCustomer { get; set; }
        public bool IsOutput { get; set; }
        public Guid AlternateId { get; set; }
    }

    public sealed class TreeDataContext : DbContext
    {
        public DbSet<EFSyncExecution> SyncExecutions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlServer(RelationTreeBenchmarks.ConnectionString);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EFSync>(e => { e.ToTable("Sync", "Integration"); e.Property(x => x.Id).ValueGeneratedNever(); e.HasMany(x => x.SyncSteps).WithOne().HasForeignKey(x => x.SyncId); });
            modelBuilder.Entity<EFSyncStep>(e => { e.ToTable("SyncStep", "Integration"); e.Property(x => x.Id).ValueGeneratedNever(); });
            modelBuilder.Entity<EFSyncExecutionMessage>(e => { e.ToTable("SyncExecutionMessage", "Integration"); e.Property(x => x.Id).ValueGeneratedNever(); });
            modelBuilder.Entity<EFSyncStepExecutionMessage>(e => { e.ToTable("SyncStepExecutionMessage", "Integration"); e.Property(x => x.Id).ValueGeneratedNever(); });
            modelBuilder.Entity<EFSyncStepPerformance>(e => { e.ToTable("SyncStepPerformance", "Integration"); e.Property(x => x.Id).ValueGeneratedNever(); });
            modelBuilder.Entity<EFSyncStepExecutionParameter>(e => { e.ToTable("SyncStepExecutionParameter", "Integration"); e.Property(x => x.Id).ValueGeneratedNever(); });
            modelBuilder.Entity<EFSyncStepExecutionDetailFile>(e => { e.ToTable("SyncStepExecutionDetailFile", "Integration"); e.Property(x => x.Id).ValueGeneratedNever(); });
            modelBuilder.Entity<EFSyncStepExecutionDetail>(e =>
            {
                e.ToTable("SyncStepExecutionDetail", "Integration");
                e.Property(x => x.Id).ValueGeneratedNever();
                e.HasMany(x => x.SyncStepExecutionDetailFiles).WithOne().HasForeignKey(x => x.SyncStepExecutionDetailId);
            });
            modelBuilder.Entity<EFSyncStepExecution>(e =>
            {
                e.ToTable("SyncStepExecution", "Integration");
                e.Property(x => x.Id).ValueGeneratedNever();
                e.HasOne(x => x.SyncStep).WithMany().HasForeignKey(x => x.SyncStepId);
                e.HasMany(x => x.SyncStepExecutionMessages).WithOne().HasForeignKey(x => x.SyncStepExecutionId);
                e.HasMany(x => x.SyncStepPerformances).WithOne().HasForeignKey(x => x.SyncStepExecutionId);
                e.HasMany(x => x.SyncStepExecutionParameters).WithOne().HasForeignKey(x => x.SyncStepExecutionId);
                e.HasMany(x => x.SyncStepExecutionDetails).WithOne().HasForeignKey(x => x.SyncStepExecutionId);
            });
            modelBuilder.Entity<EFSyncExecution>(e =>
            {
                e.ToTable("SyncExecution", "Integration");
                e.Property(x => x.Id).ValueGeneratedNever();
                e.HasOne(x => x.Sync).WithMany().HasForeignKey(x => x.SyncId);
                e.HasMany(x => x.SyncExecutionMessages).WithOne().HasForeignKey(x => x.SyncExecutionId);
                e.HasMany(x => x.SyncStepExecutions).WithOne().HasForeignKey(x => x.SyncExecutionId);
            });
        }
    }
}
