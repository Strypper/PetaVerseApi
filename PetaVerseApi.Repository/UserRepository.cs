﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetaVerseApi.Contract;
using PetaVerseApi.Core.Database;
using PetaVerseApi.Core.Entities;
using PetaVerseApi.Repository.Extensions;
using System.Linq.Expressions;

namespace PetaVerseApi.Repository
{
    public class UserRepository : BaseRepository<User> , IUserRepository 
    {
        public UserRepository(ApplicationDbContext context) : base(context) { }

        public async Task<User?> FindByGuidAsync(string guid)
            => await _dbSet.Where(u => u.Guid == guid).FirstOrDefaultAsync();

        public async Task<User?> FindByNameAsync(string userName)
        {
            var user = await base.FindByNameAsync(userName);
            if (user is null || user.IsDeleted)
                return null;

            return user;
        }

        public override IQueryable<User> FindAll(Expression<Func<User, bool>>? predicate = null)
            => _dbSet
                .Where(u => !u.IsDeleted)
                .WhereIf(predicate != null, predicate!);
    }
}
