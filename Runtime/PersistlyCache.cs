using System;
using System.Collections.Generic;

namespace Persistly.Unity
{
    public interface IPersistlySaveCache
    {
        bool TryGet(string saveId, out PersistlySave save);

        void Store(PersistlySave save);

        void Clear(string saveId);
    }

    public sealed class InMemoryPersistlySaveCache : IPersistlySaveCache
    {
        private readonly Dictionary<string, PersistlySave> _saves = new Dictionary<string, PersistlySave>(StringComparer.Ordinal);
        private readonly object _gate = new object();

        public bool TryGet(string saveId, out PersistlySave save)
        {
            lock (_gate)
            {
                return _saves.TryGetValue(saveId, out save);
            }
        }

        public void Store(PersistlySave save)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            lock (_gate)
            {
                _saves[save.SaveId] = save;
            }
        }

        public void Clear(string saveId)
        {
            lock (_gate)
            {
                _saves.Remove(saveId);
            }
        }
    }
}

