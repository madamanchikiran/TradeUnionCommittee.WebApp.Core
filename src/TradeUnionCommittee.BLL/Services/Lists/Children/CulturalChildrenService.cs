﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeUnionCommittee.BLL.ActualResults;
using TradeUnionCommittee.BLL.Configurations;
using TradeUnionCommittee.BLL.DTO.Children;
using TradeUnionCommittee.BLL.Enums;
using TradeUnionCommittee.BLL.Helpers;
using TradeUnionCommittee.BLL.Interfaces.Lists.Children;
using TradeUnionCommittee.DAL.EF;
using TradeUnionCommittee.DAL.Entities;

namespace TradeUnionCommittee.BLL.Services.Lists.Children
{
    internal class CulturalChildrenService : ICulturalChildrenService
    {
        private readonly TradeUnionCommitteeContext _context;
        private readonly AutoMapperConfiguration _mapperService;
        private readonly HashIdConfiguration _hashIdUtilities;

        public CulturalChildrenService(TradeUnionCommitteeContext context, AutoMapperConfiguration mapperService, HashIdConfiguration hashIdUtilities)
        {
            _context = context;
            _mapperService = mapperService;
            _hashIdUtilities = hashIdUtilities;
        }

        public async Task<ActualResult<IEnumerable<CulturalChildrenDTO>>> GetAllAsync(string hashIdChildren)
        {
            try
            {
                var id = _hashIdUtilities.DecryptLong(hashIdChildren);
                var cultural = await _context.CulturalChildrens
                    .Include(x => x.IdCulturalNavigation)
                    .Where(x => x.IdChildren == id)
                    .OrderByDescending(x => x.DateVisit)
                    .ToListAsync();
                var result = _mapperService.Mapper.Map<IEnumerable<CulturalChildrenDTO>>(cultural);
                return new ActualResult<IEnumerable<CulturalChildrenDTO>> { Result = result };
            }
            catch (Exception exception)
            {
                return new ActualResult<IEnumerable<CulturalChildrenDTO>>(DescriptionExceptionHelper.GetDescriptionError(exception));
            }
        }

        public async Task<ActualResult<CulturalChildrenDTO>> GetAsync(string hashId)
        {
            try
            {
                var id = _hashIdUtilities.DecryptLong(hashId);
                var cultural = await _context.CulturalChildrens
                    .Include(x => x.IdCulturalNavigation)
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (cultural == null)
                {
                    return new ActualResult<CulturalChildrenDTO>(Errors.TupleDeleted);
                }
                var result = _mapperService.Mapper.Map<CulturalChildrenDTO>(cultural);
                return new ActualResult<CulturalChildrenDTO> { Result = result };
            }
            catch (Exception exception)
            {
                return new ActualResult<CulturalChildrenDTO>(DescriptionExceptionHelper.GetDescriptionError(exception));
            }
        }

        public async Task<ActualResult> CreateAsync(CulturalChildrenDTO item)
        {
            try
            {
                await _context.CulturalChildrens.AddAsync(_mapperService.Mapper.Map<CulturalChildrens>(item));
                await _context.SaveChangesAsync();
                return new ActualResult();
            }
            catch (Exception exception)
            {
                return new ActualResult(DescriptionExceptionHelper.GetDescriptionError(exception));
            }
        }

        public async Task<ActualResult> UpdateAsync(CulturalChildrenDTO item)
        {
            try
            {
                _context.Entry(_mapperService.Mapper.Map<CulturalChildrens>(item)).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return new ActualResult();
            }
            catch (Exception exception)
            {
                return new ActualResult(DescriptionExceptionHelper.GetDescriptionError(exception));
            }
        }

        public async Task<ActualResult> DeleteAsync(string hashId)
        {
            try
            {
                var id = _hashIdUtilities.DecryptLong(hashId);
                var result = await _context.CulturalChildrens.FindAsync(id);
                if (result != null)
                {
                    _context.CulturalChildrens.Remove(result);
                    await _context.SaveChangesAsync();
                }
                return new ActualResult();
            }
            catch (Exception exception)
            {
                return new ActualResult(DescriptionExceptionHelper.GetDescriptionError(exception));
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}