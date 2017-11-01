using System;
using System.Threading.Tasks;
using DBreeze.Transactions;

namespace LiGet.Cache.DBreeze
{
    public class DBreezeCacheTransaction : INupkgCacheTransaction
    {
        private readonly Transaction tx;
        public DBreezeCacheTransaction(Transaction tx)
        {
            this.tx = tx;
        }

        public void Dispose()
        {
            if(tx != null)
                tx.Dispose();
        }

        public byte[] TryGet(string table, string key) {
            var row = this.tx.Select<string,byte[]>(table, key);
            if(row.Exists) {
                return row.Value;                
            }
            return null;
        }

        public void Insert(string table, string key, byte[] value) {
            // we do not lock, if there are 2 or more uncessary writes, it is still OK because content is the same
            this.tx.Insert<string, byte[]>(table, key, value);
            this.tx.Commit();
        }
    }
}