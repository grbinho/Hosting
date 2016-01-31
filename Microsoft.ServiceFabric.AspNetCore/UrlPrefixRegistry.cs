﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;

namespace Microsoft.ServiceFabric.AspNetCore
{
    public class UrlPrefixRegistry
    {
        private readonly object _lockObject = new object();
        private readonly SortedList<string, object> _entries = new SortedList<string, object>();

        public static readonly UrlPrefixRegistry Default = new UrlPrefixRegistry();

        public string Register(object instanceOrReplica)
        {
            if (instanceOrReplica == null)
            {
                throw new ArgumentNullException(nameof(instanceOrReplica));
            }

            if (!(instanceOrReplica is IStatelessServiceInstance) && !(instanceOrReplica is IStatefulServiceReplica))
            {
                throw new ArgumentException(null, nameof(instanceOrReplica));
            }

            lock (_lockObject)
            {
                if (_entries.ContainsValue(instanceOrReplica))
                {
                    throw new ArgumentException(null, nameof(instanceOrReplica));
                }

                string urlPrefix = null;

                if (instanceOrReplica is IStatelessServiceInstance)
                {
                    urlPrefix = "/";
                }

                if (instanceOrReplica is IStatefulServiceReplica)
                {
                    urlPrefix = $"/replica-{Guid.NewGuid()}";
                }

                _entries.Add(urlPrefix, instanceOrReplica);

                return urlPrefix;
            }
        }

        public bool Unregister(object instanceOrReplica)
        {
            if (instanceOrReplica == null)
            {
                throw new ArgumentNullException(nameof(instanceOrReplica));
            }

            lock (_lockObject)
            {
                int index = _entries.IndexOfValue(instanceOrReplica);

                if (index >= 0)
                {
                    _entries.RemoveAt(index);
                    return true;
                }

                return false;
            }
        }

        public bool StartWithUrlPrefix(PathString path, out PathString remainingPath, out object instanceOrReplica)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            remainingPath = null;
            instanceOrReplica = null;

            foreach (var entry in _entries.Reverse())
            {
                if (path.StartsWithSegments(entry.Key, out remainingPath))
                {
                    instanceOrReplica = entry.Value;
                    return true;
                }
            }

            return false;
        }
    }
}