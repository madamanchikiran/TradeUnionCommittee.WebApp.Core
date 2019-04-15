﻿using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeUnionCommittee.DAL.Audit.EF;
using TradeUnionCommittee.DAL.Audit.Entities;
using TradeUnionCommittee.DAL.Audit.Enums;
using TradeUnionCommittee.DAL.Audit.Extensions;

namespace TradeUnionCommittee.DAL.Audit.Repository
{
    public interface ISystemAuditRepository : IDisposable
    {
        Task AuditAsync(Journal journal);
        Task<IEnumerable<string>> GetExistingPartitionInDbAsync();
        Task<IEnumerable<Journal>> FilterAsync(IEnumerable<string> namesPartitions, string email, DateTime startDate, DateTime endDate);
    }

    public class SystemAuditRepository : ISystemAuditRepository
    {
        private readonly TradeUnionCommitteeAuditContext _dbContext;

        public SystemAuditRepository(TradeUnionCommitteeAuditContext db)
        {
            _dbContext = db;
        }

        public async Task AuditAsync(Journal journal)
        {
            const string sqlQuery = "INSERT INTO \"Journal\" (\"Guid\",\"Operation\",\"DateTime\",\"EmailUser\",\"Table\",\"IpUser\") VALUES (@1, CAST(@2 AS \"Operations\"), @3, @4, CAST(@5 AS \"Tables\"), CAST(@6 AS CIDR));";

            var guid = new NpgsqlParameter("@1", Guid.NewGuid().ToString());
            var operation = new NpgsqlParameter("@2", journal.Operation.ToString());
            var dateTime = new NpgsqlParameter("@3", DateTime.Now);
            var email = new NpgsqlParameter("@4", journal.EmailUser);
            var table = new NpgsqlParameter("@5", journal.Table.ToString());
            var ip = new NpgsqlParameter("@6", journal.IpUser);

            using (var dr = await _dbContext.Database.ExecuteSqlQueryAsync(sqlQuery, default(CancellationToken), guid, operation, dateTime, email, table, ip))
            {
                dr.DbDataReader.Close();
            }
        }

        public async Task<IEnumerable<string>> GetExistingPartitionInDbAsync()
        {
            const string sqlQuery = "SELECT tablename FROM pg_tables WHERE schemaname = \'public\' AND tablename != \'Journal\';";
            var result = new List<string>();
            using (var dr = await _dbContext.Database.ExecuteSqlQueryAsync(sqlQuery))
            {
                var reader = dr.DbDataReader;
                result.AddRange(from DbDataRecord dbDataRecord in reader select dbDataRecord["tablename"].ToString());
                dr.DbDataReader.Close();
            }
            return result;
        }

        public async Task<IEnumerable<Journal>> FilterAsync(IEnumerable<string> namesPartitions, string email, DateTime startDate, DateTime endDate)
        {
            var par1 = new NpgsqlParameter("@1", email);
            var par2 = new NpgsqlParameter("@2", startDate);
            var par3 = new NpgsqlParameter("@3", endDate);

            var result = new List<Journal>();
            using (var dr = await _dbContext.Database.ExecuteSqlQueryAsync(SqlGenerator(namesPartitions.ToList()), default(CancellationToken), par1, par2, par3))
            {
                var reader = dr.DbDataReader;
                if (reader.HasRows)
                {
                    result.AddRange(from DbDataRecord dbDataRecord in reader select new Journal
                    {
                        Guid = dbDataRecord["Guid"].ToString(),
                        Operation = (Operations)Enum.Parse(typeof(Operations), dbDataRecord["Operation"].ToString()),
                        DateTime = (DateTime)dbDataRecord["DateTime"],
                        EmailUser = dbDataRecord["EmailUser"].ToString(),
                        Table = (Tables)Enum.Parse(typeof(Tables), dbDataRecord["Table"].ToString())
                    });
                }
                dr.DbDataReader.Close();
            }
            return result;
        }

        private string SqlGenerator(IReadOnlyList<string> list)
        {
            if (list.Count > 1)
            {
                var result = string.Empty;

                for (var i = 0; i < list.Count; i++)
                {
                    if (i != list.Count - 1)
                    {
                        result += $"SELECT \"Guid\",\"Operation\",\"DateTime\",\"EmailUser\",\"Table\" FROM {list[i]} WHERE \"EmailUser\" = @1 AND \"DateTime\" BETWEEN @2 AND @3 UNION ALL ";
                    }
                    else
                    {
                        result += $"SELECT \"Guid\",\"Operation\",\"DateTime\",\"EmailUser\",\"Table\" FROM {list[i]} WHERE \"EmailUser\" = @1 AND \"DateTime\" BETWEEN @2 AND @3";
                    }
                }
                return result;
            }
            return $"SELECT \"Guid\",\"Operation\",\"DateTime\",\"EmailUser\",\"Table\" FROM {list[0]} WHERE \"EmailUser\" = @1 AND \"DateTime\" BETWEEN @2 AND @3";
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}