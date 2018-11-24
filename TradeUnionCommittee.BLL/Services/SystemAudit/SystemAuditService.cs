﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeUnionCommittee.BLL.DTO;
using TradeUnionCommittee.BLL.Extensions;
using TradeUnionCommittee.BLL.Interfaces.SystemAudit;
using TradeUnionCommittee.BLL.Utilities;
using TradeUnionCommittee.Common.ActualResults;
using TradeUnionCommittee.DAL.Entities;
using TradeUnionCommittee.DAL.Interfaces;

namespace TradeUnionCommittee.BLL.Services.SystemAudit
{
    public class SystemAuditService : ISystemAuditService
    {
        private readonly IUnitOfWork _database;
        private readonly IAutoMapperUtilities _mapperService;

        public SystemAuditService(IUnitOfWork database, IAutoMapperUtilities mapperService)
        {
            _database = database;
            _mapperService = mapperService;
        }

        public async Task AuditAsync(string email, Enums.Operations operation, Enums.Tables table)
        {
            await _database.SystemAuditRepository.AuditAsync(new Journal {EmailUser = email, Operation = (Operations)operation, Table = (Tables)table });
        }

        public async Task AuditAsync(string email, Enums.Operations operation, Enums.Tables[] tables)
        {
            foreach (var table in tables)
            {
                await AuditAsync(email, operation, table);
            }
        }

        public async Task AuditWithIpAsync(string email, string ipUser, Enums.Operations operation, Enums.Tables table)
        {
            await _database.SystemAuditRepository.AuditWithIpAsync(new Journal { EmailUser = email, IpUser = ipUser, Operation = (Operations)operation, Table = (Tables)table });
        }

        public async Task AuditWithIpAsync(string email, string ipUser, Enums.Operations operation, Enums.Tables[] tables)
        {
            foreach (var table in tables)
            {
                await AuditWithIpAsync(email, ipUser, operation, table);
            }
        }

        public async Task<ActualResult<IEnumerable<JournalDTO>>> FilterAsync(string email, DateTime startDate, DateTime endDate)
        {
            var existingPartitionInDb = await _database.SystemAuditRepository.GetExistingPartitionInDbAsync();
            var sequenceDate = startDate.Date.GetListPartitionings(endDate.Date);
            var resultPartition = sequenceDate.Intersect(existingPartitionInDb).ToList();

            if (resultPartition.Any())
            {
                var result = await _database.SystemAuditRepository.FilterAsync(resultPartition, email, startDate, endDate);
                return new ActualResult<IEnumerable<JournalDTO>> { Result = _mapperService.Mapper.Map<IEnumerable<JournalDTO>>(result) };
            }
            return new ActualResult<IEnumerable<JournalDTO>>();
        }
    }
}