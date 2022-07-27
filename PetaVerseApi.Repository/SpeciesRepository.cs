using Microsoft.EntityFrameworkCore;
using PetaVerseApi.Contract;
using PetaVerseApi.Core.Database;
using PetaVerseApi.Core.Entities;
using PetaVerseApi.Repository.Extensions;
using System.Linq.Expressions;

namespace PetaVerseApi.Repository
{
    public class SpeciesRepository : BaseRepository<Species> , ISpeciesRepository
    {
        public SpeciesRepository(ApplicationDbContext context) : base(context) { }

        public override IQueryable<Species> FindAll(Expression<Func<Species, bool>>? predicate = null)
            => _dbSet.WhereIf(predicate != null, predicate!)
                     .Include(s => s.Breeds)
                     .ThenInclude(s => s.Animals);

        public override async Task<Species?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
            => await FindAll(b => b.Id == id)
                    .FirstOrDefaultAsync(cancellationToken);

    }
}
