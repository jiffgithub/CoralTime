﻿using CoralTime.Common.Exceptions;
using CoralTime.DAL.Cache;
using CoralTime.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using CoralTime.DAL.Interfaces;

namespace CoralTime.DAL.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        private readonly DbContext _db;
        private readonly DbSet<T> _dbSet;

        private readonly ICacheManager _cacheManager;

        private static readonly object LockCacheObject = new object();

        private readonly string _userId;

        protected BaseRepository(AppDbContext context, IMemoryCache memoryCache, string userId)
        {
            _db = context;
            _dbSet = _db.Set<T>();
            _cacheManager = CacheMemoryFactory.CreateCacheMemory(memoryCache);

            _userId = userId;
        }

        #region GetQuery.

        public virtual IQueryable<T> GetIncludes(IQueryable<T> query) => query;
        
        public virtual IQueryable<T> GetQueryWithIncludes() => GetIncludes(_dbSet);

        public virtual IQueryable<T> GetQueryWithoutIncludes() => _dbSet;

        public virtual IQueryable<T> GetQueryAsNoTraking() => _dbSet.AsNoTracking();

        public virtual IQueryable<T> GetQueryAsNoTrackingWithIncludes() => GetIncludes(GetQueryAsNoTraking());

        public virtual T GetQueryWithIncludesById(int id) => null;

        #endregion

        #region LinkedCache. 

        public virtual T LinkedCacheGetByName(string name) => null;

        public virtual T LinkedCacheGetById(int Id) => null;

        public virtual List<T> LinkedCacheGetList()
        {
            try
            {
                var key = GenerateCacheKey();
                var items = _cacheManager.CachedListGet<T>(key);
                if (items == null)
                {
                    lock (LockCacheObject)
                    {
                        items = _cacheManager.CachedListGet<T>(key);
                        if (items == null)
                        {
                            items = GetQueryAsNoTrackingWithIncludes().ToList();
                            _cacheManager.LinkedPutList(key, items);
                        }
                    }
                }

                return items;
            }
            catch (Exception seq)
            {
                throw new CoralTimeDangerException(seq.Message, seq);
            }
        }

        public virtual void LinkedCacheClear()
        {
            _cacheManager.LinkedCacheClear<T>();
        }

        #endregion

        #region Single Cache.

        protected int DefaultCacheTime { get; set; } = 800;

        //public virtual List<TEntity> GetCachedList(Func<List<TEntity>> getListFunc)
        //{
        //    string key = GenerateClientUniqueCacheKey();
        //    return CacheManager.Get(key, getListFunc);
        //}

        protected virtual string GenerateCacheKey()
        {
            var entityName = typeof(T).Name;
            var key = $"{entityName}_CacheKey";
            return key;
        }

        public string GenerateClientUniqueCacheKey(string userName)
        {
            var entityName = typeof(T).Name;
            var key = $"{userName}_{entityName}_CacheKey";
            return key;
        }

        public void ClearEntityCache()
        {
            var key = GenerateCacheKey();
            _cacheManager.Remove(key);
        }

        #endregion

        #region CRUD.
        
        public virtual T GetById(object id)
        {
            return _dbSet.Find(id);
        }

        public virtual void Insert(T entity)
        {
            if (entity is ILogChanges entityILogChange)
            {
                SetInfoAboutUserThatCratedEntity(entityILogChange);
                SetInfoAboutUserThatUpdatedEntity(entityILogChange);

                entity = (T)entityILogChange;
            }

            _dbSet.Add(entity);
        }

        public virtual void InsertRange(IEnumerable<T> entities)
        {
            if (entities is IEnumerable<ILogChanges> entitiesILogChange)
            {
                foreach (var entityILogChange in entitiesILogChange)
                {
                    SetInfoAboutUserThatCratedEntity(entityILogChange);
                    SetInfoAboutUserThatUpdatedEntity(entityILogChange);
                }

                entities = (IEnumerable<T>)entitiesILogChange;
            }

            _dbSet.AddRange(entities);
        }

        public virtual void Update(T entity)
        {
            if (entity is ILogChanges entityILogChange)
            {
                SetInfoAboutUserThatUpdatedEntity(entityILogChange);

                entity = (T)entityILogChange;
            }

            _dbSet.Update(entity);
        }

        public virtual void UpdateRange(IEnumerable<T> entities)
        {
            if (entities is IEnumerable<ILogChanges> entitiesILogChange)
            {
                foreach (var entityILogChange in entitiesILogChange)
                {
                    SetInfoAboutUserThatUpdatedEntity(entityILogChange);
                }

                entities = (IEnumerable<T>)entitiesILogChange;
            }

            _dbSet.UpdateRange(entities);
        }

        public virtual void Delete(object id)
        {
            var entityToDelete = _dbSet.Find(id);
            Delete(entityToDelete);
        }

        public virtual void Delete(T entityToDelete)
        {
            if (_db.Entry(entityToDelete).State == EntityState.Detached)
            {
                _dbSet.Attach(entityToDelete);
            }

            _dbSet.Remove(entityToDelete);
        }

        public virtual void DeleteRange(IEnumerable<T> entitiesToDelete)
        {
            foreach (var entityToDelete in entitiesToDelete)
            {
                if (_db.Entry(entityToDelete).State == EntityState.Detached)
                {
                    _dbSet.Attach(entityToDelete);
                }

                _dbSet.Remove(entityToDelete);
            }
        }

        public int ExecuteSqlCommand(string command, params object[] parameters)
        {
            return _db.Database.ExecuteSqlCommand(command, parameters);
        }

        private void SetInfoAboutUserThatCratedEntity(ILogChanges entityILogChange)
        {
            entityILogChange.CreatorId = _userId;
            entityILogChange.CreationDate = DateTime.Now;
        }

        private void SetInfoAboutUserThatUpdatedEntity(ILogChanges entityILogChange)
        {
            entityILogChange.LastUpdateDate = DateTime.Now;
            entityILogChange.LastEditorUserId = _userId;
        }

        #endregion
    }
}