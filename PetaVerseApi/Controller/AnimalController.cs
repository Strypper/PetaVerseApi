using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetaVerseApi.Contract;
using PetaVerseApi.Core.Database;
using PetaVerseApi.Core.Entities;
using PetaVerseApi.DTOs;
using PetaVerseApi.Interfaces;
using MediaType = PetaVerseApi.Core.Entities.MediaType;

namespace PetaVerseApi.Controller
{
    public class AnimalController : BaseController
    {
        private readonly IMapper                         _mapper;
        private readonly IMediaService                   _mediaService;
        private readonly IUserRepository                 _userRepository;
        private readonly IBreedRepository                _breedRepository;
        private readonly IAnimalRepository               _animalRepository;
        private readonly ISpeciesRepository              _speciesRepository;
        private readonly ApplicationDbContext            _petaverseDbContext;
        private readonly IUserAnimalRepository           _userAnimalRepository;
        private readonly IPetaverseMediaRepository       _petaverseMediaRepository;
        private readonly IAnimalPetaverseMediaRepository _animalPetaverseMediaRepository;

        public AnimalController(IMapper mapper,
                                IMediaService mediaService, 
                                IUserRepository userRepository,
                                IBreedRepository breedRepository,
                                IAnimalRepository animalRepository,
                                ISpeciesRepository speciesRepository,
                                ApplicationDbContext petaverseDpContext,
                                IUserAnimalRepository userAnimalRepository,
                                IPetaverseMediaRepository petaverseMediaRepository,
                                IAnimalPetaverseMediaRepository animalPetaverseMediaRepository)
        {
            _mapper                         = mapper;
            _mediaService                   = mediaService;
            _userRepository                 = userRepository;
            _breedRepository                = breedRepository;
            _animalRepository               = animalRepository;
            _speciesRepository              = speciesRepository;
            _petaverseDbContext             = petaverseDpContext;
            _userAnimalRepository           = userAnimalRepository;   
            _petaverseMediaRepository       = petaverseMediaRepository;
            _animalPetaverseMediaRepository = animalPetaverseMediaRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
        {
            var animals = await _animalRepository.FindAll().ToListAsync(cancellationToken);
            return Ok(_mapper.Map<IEnumerable<AnimalDTO>>(animals));
        }


        [HttpGet("{animalId}")]
        public async Task<IActionResult> GetById(int animalId, CancellationToken cancellationToken = default)
        {
            var animal = await _animalRepository.FindByIdAsync(animalId, cancellationToken);
            return animal != null ? Ok(_mapper.Map<AnimalDTO>(animal)) : NotFound("Unable to find the requested animal"); 
        }


        [HttpGet("{userGuid}")]
        public async Task<IActionResult> GetAllByUserGuid(string userGuid, CancellationToken cancellationToken = default)
        {
            //Check if user exist
            var user = await _userRepository.FindByGuidAsync(userGuid, cancellationToken);
            if(user != null){
                //Get AnimalID
                var animalIds = await _userAnimalRepository.FindAll(ua => ua.UserId == user.Id).Select(ua => ua.AnimalId).ToListAsync();
                var animals = new List<AnimalDetailDTO>();
                foreach (var id in animalIds)
                {
                    var animal = await _animalRepository.FindAnimalWithFullInfo(id, cancellationToken);
                    if (animal != null)
                    {
                        var petPhotoIds = _animalPetaverseMediaRepository.FindAll(apm => apm.AnimalId == id).Select(apm => apm.PetaverMediaId).ToList();
                        var petPhotos = new List<PetaverseMediaDTO>();
                        petPhotoIds.ForEach(id =>
                        {
                            var photo = _petaverseMediaRepository.FindAll(media => media.Id == id).FirstOrDefault();
                            if (photo != null)
                                petPhotos.Add(_mapper.Map<PetaverseMediaDTO>(photo));
                        });
                        var animalDTO = _mapper.Map<AnimalDetailDTO>(animal);
                        animalDTO.PetPhotos = petPhotos;
                        animals.Add(animalDTO);
                    }
                }
                return Ok(animals);
            }
            else return NotFound("User invalid");
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PetDTO dto, CancellationToken cancellationToken = default)
        {
            var listOfOwnersGuid = dto.OwnerGuids.Split(',').ToList();
            var listOfOwner = new List<User>();
            foreach (var ownerGuid in listOfOwnersGuid)
            {
                var user = await _userRepository.FindByGuidAsync(ownerGuid, cancellationToken);
                if (user == null) return NotFound($"Cant find user with Guid: {ownerGuid}");
                else listOfOwner.Add(user);
            }
            using var petaverseTransaction = await _petaverseDbContext.Database.BeginTransactionAsync();
            var animal = _mapper.Map<Animal>(dto);

            animal.SixDigitCode = await _animalRepository.Generate6DigitCodeAsync();
            animal.Breed = await _breedRepository.FindByIdAsync(dto.BreedId, cancellationToken);
            animal.Species = await _speciesRepository.FindByIdAsync(dto.SpeciesId, cancellationToken);

            _animalRepository.Add(animal);
            await _animalRepository.SaveChangesAsync(cancellationToken);

            if(listOfOwner.Count > 0)
            {
                listOfOwner.ForEach(owner => _userAnimalRepository.Add(new UserAnimal() { UserId = owner.Id, AnimalId = animal.Id}));
                await _userAnimalRepository.SaveChangesAsync(cancellationToken);
            }

            await petaverseTransaction.CommitAsync(cancellationToken);
            return Ok(_mapper.Map<AnimalDTO>(animal));
        }

        [HttpPost]
        public async Task<IActionResult> UploadPetAvatar(IFormFile avatar)
        {
            if (_mediaService.IsImage(avatar))
            {
                using(Stream stream = avatar.OpenReadStream())
                {
                    Tuple<bool, string> result = await _mediaService.UploadAvatarToStorage(stream, avatar.FileName);
                    var isUploaded = result.Item1;
                    var stringUrl = result.Item2;
                    if (isUploaded && !String.IsNullOrEmpty(stringUrl))
                    {
                        return Ok(stringUrl);
                    }
                    else return BadRequest("Look like the image couldnt upload to the storage");
                }
            }
            else return new UnsupportedMediaTypeResult();
        }

        [HttpPost("{petId}"), DisableRequestSizeLimit]
        public async Task<IActionResult> UploadAnimalMedias(int petId, List<IFormFile> medias, CancellationToken cancellationToken)
        {
            var pet = await _animalRepository.FindByIdAsync(petId, cancellationToken);
            if(pet == null)
                return NotFound("Not Found This Pet");
            else
            {
                var uploadedPetPhotos = new List<PetaverseMediaDTO>();
                try
                {
                    if (medias.Count == 0)
                        return BadRequest("No medias received from the upload");

                    foreach (var formFile in medias)
                    {
                        if (_mediaService.IsImage(formFile) && formFile.Length > 0)
                        {
                            using (Stream stream = formFile.OpenReadStream())
                            {
                                Tuple<bool, string> result = await _mediaService.UploadFileToStorage(stream, formFile.FileName);
                                var isUploaded = result.Item1;
                                var stringUrl = result.Item2;

                                if (isUploaded && !String.IsNullOrEmpty(stringUrl))
                                {
                                    var petaverseMedia = new PetaverseMedia()
                                    {
                                        MediaUrl = stringUrl,
                                        TimeUpload = DateTime.Now,
                                        Type = MediaType.Photo
                                    };

                                    _petaverseMediaRepository.Add(petaverseMedia);
                                    await _petaverseMediaRepository.SaveChangesAsync(cancellationToken);

                                    _animalPetaverseMediaRepository.Add(new AnimalPetaverseMedia()
                                    {
                                        AnimalId = pet.Id,
                                        PetaverMediaId = petaverseMedia.Id
                                    });
                                    await _animalPetaverseMediaRepository.SaveChangesAsync(cancellationToken);

                                    uploadedPetPhotos.Add(new PetaverseMediaDTO() 
                                    {
                                        Id = petaverseMedia.Id,
                                        MediaUrl = stringUrl,
                                        Type = DTOs.MediaType.Photo
                                    });
                                } else return BadRequest("Look like the image couldnt upload to the storage");
                            }
                        }
                        else
                        {
                            return new UnsupportedMediaTypeResult();
                        }
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
                return Ok(uploadedPetPhotos);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromBody] BreedDTO dto, int id, CancellationToken cancellationToken = default)
        {
            var breed = await _animalRepository.FindByIdAsync(id, cancellationToken);
            if (breed is null)
                return NotFound();

            _mapper.Map(dto, breed);
            await _animalRepository.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            var animal = await _animalRepository.FindByIdAsync(id, cancellationToken);
            if (animal is null)
                return NotFound();

            _animalRepository.Delete(animal);
            var userAnimals = _userAnimalRepository.FindAll(ua => ua.AnimalId == animal.Id).ToList();
            userAnimals.ForEach(ua => _userAnimalRepository.Delete(ua));
            await _animalRepository.SaveChangesAsync(cancellationToken);
            await _userAnimalRepository.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
    }
}
