using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Product;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BMWMS.Business.Services
{
    public class UnitOfMeasureService : IUnitOfMeasureService
    {
        private readonly IUnitOfMeasureRepository _repository;

        public UnitOfMeasureService(IUnitOfMeasureRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<UnitOfMeasureDto>> GetAllAsync(string? keyword = null)
        {
            var entities = await _repository.GetAllAsync(keyword);
            return entities.Select(u => new UnitOfMeasureDto
            {
                UnitOfMeasureId = u.UnitOfMeasureId,
                UnitCode = u.UnitCode,
                UnitName = u.UnitName,
                QuantityScale = u.QuantityScale
            }).ToList();
        }

        public async Task<UnitOfMeasureDto?> GetByIdAsync(int id)
        {
            var u = await _repository.GetByIdAsync(id);
            if (u == null) return null;
            return new UnitOfMeasureDto
            {
                UnitOfMeasureId = u.UnitOfMeasureId,
                UnitCode = u.UnitCode,
                UnitName = u.UnitName,
                QuantityScale = u.QuantityScale
            };
        }

        public async Task<int> CreateAsync(CreateUnitOfMeasureDto dto)
        {
            if (await _repository.ExistsByCodeAsync(dto.UnitCode))
                throw new ApiException($"Mã ÐVT '{dto.UnitCode}' dã t?n t?i.", 400);

            var entity = new UnitsOfMeasure
            {
                UnitCode = dto.UnitCode,
                UnitName = dto.UnitName,
                QuantityScale = dto.QuantityScale,
                Status = dto.Status
            };

            await _repository.AddAsync(entity);
            return entity.UnitOfMeasureId;
        }

        public async Task UpdateAsync(int id, UpdateUnitOfMeasureDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) throw new ApiException("Không tìm th?y ÐVT", 404);

            if (await _repository.ExistsByCodeAsync(dto.UnitCode, id))
                throw new ApiException($"Mã ÐVT '{dto.UnitCode}' dã t?n t?i.", 400);

            entity.UnitCode = dto.UnitCode;
            entity.UnitName = dto.UnitName;
            entity.QuantityScale = dto.QuantityScale;
            entity.Status = dto.Status;

            await _repository.UpdateAsync(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) throw new ApiException("Không tìm th?y ÐVT", 404);

            if (await _repository.IsInUseAsync(id))
                throw new ApiException("ÐVT này dang du?c s? d?ng, không th? xóa", 400);

            await _repository.DeleteAsync(entity);
        }
    }
}
