using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetaVerseApi.Contract;
using PetaVerseApi.Core.Entities;
using PetaVerseApi.DTOs;

namespace PetaVerseApi.Controller
{
    public class TemperamentController : ControllerBase
    {
        private readonly ITemperamentRepository _temperamentRepository;
        private readonly IMapper _mapper;

        public TemperamentController(IMapper mapper, ITemperamentRepository temperamentRepository)
        {
            _mapper = mapper;
            _temperamentRepository = temperamentRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
        {
            var breed = await _temperamentRepository.FindAll().ToListAsync(cancellationToken);
            return Ok(_mapper.Map<IEnumerable<TemperamentDTO>>(breed));
        }

        [HttpPost]
        public async Task<IActionResult> Create(TemperamentDTO dto, CancellationToken cancellationToken = default)
        {
            var temperament = _mapper.Map<Temperament>(dto);
            _temperamentRepository.Add(temperament);

            await _temperamentRepository.SaveChangesAsync(cancellationToken);
            return Ok(_mapper.Map<TemperamentDTO>(temperament));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromBody] TemperamentDTO dto, CancellationToken cancellationToken = default)
        {
            var temperament = await _temperamentRepository.FindByIdAsync(dto.Id, cancellationToken);
            if (temperament is null)
                return NotFound();

            _mapper.Map(dto, temperament);
            await _temperamentRepository.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            var temperament = await _temperamentRepository.FindByIdAsync(id, cancellationToken);
            if (temperament is null)
                return NotFound();

            _temperamentRepository.Delete(temperament);
            await _temperamentRepository.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
    }
}
