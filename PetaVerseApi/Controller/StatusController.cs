using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetaVerseApi.Contract;
using PetaVerseApi.Core.Entities;
using PetaVerseApi.DTOs;

namespace PetaVerseApi.Controller
{
    public class StatusController : BaseController
    {
        private readonly IUserRepository _userRepository;
        private readonly IStatusRepository _statusRepository;
        private readonly IMapper _mapper;

        public StatusController(IUserRepository userRepository, IStatusRepository statusRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _statusRepository = statusRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
        {
            var status = await _statusRepository.FindAll().ToListAsync(cancellationToken);
            return Ok(_mapper.Map<IEnumerable<StatusDTO>>(status));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StatusDTO dto, CancellationToken cancellationToken = default)
        {
            var status = _mapper.Map<Status>(dto);
            _statusRepository.Add(status);

            await _statusRepository.SaveChangesAsync(cancellationToken);
            return Ok(_mapper.Map<PetShortsDTO>(status));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromBody] StatusDTO dto, CancellationToken cancellationToken = default)
        {
            var status = await _statusRepository.FindByIdAsync(dto.Id, cancellationToken);
            if (status is null)
                return NotFound();

            _mapper.Map(dto, status);
            await _statusRepository.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            var status = await _statusRepository.FindByIdAsync(id, cancellationToken);
            if (status is null)
                return NotFound();

            _statusRepository.Delete(status);
            await _statusRepository.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
    }
}
