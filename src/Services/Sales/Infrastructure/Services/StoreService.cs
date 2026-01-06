using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _360Retail.Services.Sales.Application.DTOs;
using _360Retail.Services.Sales.Application.Interfaces;
using _360Retail.Services.Sales.Domain.Entities;
using _360Retail.Services.Sales.Infrastructure.Persistence;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Sales.Infrastructure.Services
{
    public class StoreService : IStoreService
    {
        private readonly SalesDbContext _context;
        private readonly IMapper _mapper;

        public StoreService(SalesDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Guid> CreateAsync(CreateStoreDto request)
        {
          
            if (await _context.Stores.AnyAsync(s => s.StoreName == request.StoreName))
                throw new Exception("The store name already exists!");

            var store = _mapper.Map<Store>(request);
            store.Id = Guid.NewGuid();

         
            store.CreatedAt = DateTime.UtcNow;

            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            return store.Id;
        }

        public async Task<List<StoreDto>> GetAllAsync()
        {
            var list = await _context.Stores
                                     .OrderByDescending(s => s.CreatedAt)
                                     .ToListAsync();
            return _mapper.Map<List<StoreDto>>(list);
        }
    }
}