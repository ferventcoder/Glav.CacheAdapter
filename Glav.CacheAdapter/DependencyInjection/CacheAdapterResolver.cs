﻿using Glav.CacheAdapter.Bootstrap;
using Glav.CacheAdapter.Core;
using Glav.CacheAdapter.Core.DependencyInjection;
using Glav.CacheAdapter.Core.Diagnostics;
using Glav.CacheAdapter.Distributed.Redis;
using Glav.CacheAdapter.Web;

namespace Glav.CacheAdapter.DependencyInjection
{
    public class CacheAdapterResolver : ICacheAdapterResolver
    {
        private readonly ILogging _logger;
        private readonly ICacheFactoryAssemblyResolver _cacheFactoryAssemblyResolver;

        public CacheAdapterResolver(ILogging logger, ICacheFactoryAssemblyResolver cacheFactoryAssemblyResolver)
        {
            _logger = logger;
            _cacheFactoryAssemblyResolver = cacheFactoryAssemblyResolver;
        }

        public ICacheProvider ResolveCacheFromConfig(CacheConfig config)
        {
            ICacheProvider provider;
            var cacheFactory = GetCacheConstructionFactoryUsingConfig(config);
            var cacheComponents = cacheFactory.CreateCacheComponents();
            if (config.IsCacheDependencyManagementEnabled)
            {
                provider = new CacheProvider(cacheComponents.Cache, _logger,config, cacheComponents.DependencyManager, cacheComponents.FeatureSupport);
            }
            else
            {
                provider = new CacheProvider(cacheComponents.Cache, _logger,config,null, cacheComponents.FeatureSupport);
            }
            _logger.WriteInfoMessage(string.Format("CacheProvider initialised with {0} cache engine", config.CacheToUse));
            return provider;
        }

        public ICacheConstructionFactory GetCacheConstructionFactoryUsingConfig(CacheConfig config)
        {
            return GetCacheConstructionFactoryUsingTypeValue(config.CacheToUse,config);
        }

        public ICacheConstructionFactory GetCacheConstructionFactoryUsingTypeValue(string cacheTypeValue, CacheConfig config)
        {
            ICacheConstructionFactory cacheFactory = null;
            var normalisedCacheToUse = !string.IsNullOrWhiteSpace(cacheTypeValue) ? cacheTypeValue.ToLowerInvariant() : string.Empty;
            switch (normalisedCacheToUse)
            {
                case CacheTypes.MemoryCache:
                    cacheFactory = new MemoryCacheFactory(_logger, config);
                    break;
                case CacheTypes.WebCache:
                    cacheFactory = new WebCacheFactory(_logger, config);
                    break;
                case CacheTypes.AppFabricCache:
                    cacheFactory = _cacheFactoryAssemblyResolver.ResolveCacheFactory(config);
                    break;
                case CacheTypes.memcached:
                    cacheFactory = _cacheFactoryAssemblyResolver.ResolveCacheFactory(config);
                    break;
                case CacheTypes.redis:
                    cacheFactory = new RedisCacheFactory(_logger, config);
                    break;
                case CacheTypes.hybrid:
                    throw new System.NotSupportedException("Hybrid configuration not supported at this time.");
                default:
                    cacheFactory = new MemoryCacheFactory(_logger, config);
                    break;
            }
            return cacheFactory;
        }
    }
}
