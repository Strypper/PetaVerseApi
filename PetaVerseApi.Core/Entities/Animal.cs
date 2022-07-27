
namespace PetaVerseApi.Core.Entities
{
    public class Animal : BaseEntity
    {
<<<<<<< Updated upstream
=======
        public int          SpeciesId   { get; set; } //??
        public int          BreedId     { get; set; } //??
>>>>>>> Stashed changes
        public string       Name        { get; set; } = string.Empty;
        public string?      PetAvatar   { get; set; } = string.Empty;
        public string?      Bio         { get; set; } = string.Empty;
        public string?      PetColor    { get; set; } = string.Empty;
        public bool         Gender      { get; set; }
        public int?         Age         { get; set; }
<<<<<<< Updated upstream

        public Species?     Species     { get; set; }
=======
        public Species?     Species     { get; set; } // need remove
>>>>>>> Stashed changes
        public Breed?       Breed       { get; set; }


        public virtual ICollection<UserAnimal>           UserAnimals           { get; set; } = new HashSet<UserAnimal>();
        public virtual ICollection<AnimalPetaverseMedia> AnimalPetaverseMedias { get; set; } = new HashSet<AnimalPetaverseMedia>();
    }
}
