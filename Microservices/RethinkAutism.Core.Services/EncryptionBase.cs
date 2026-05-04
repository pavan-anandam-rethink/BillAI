using System;
using System.Collections.Generic;

namespace RethinkAutism.Core.Services
{
    public class EncryptionBase
    {
        private const string EncryptedFieldPrefix = "e";

        private Dictionary<string, string> EncryptionCache { get;set;}
        internal bool hasBeenDecrypted = false;
        public bool iseDirty = false;
        public bool ImmediateEncryption = false;
        public void Encrypt()
        {
            Encryptthis();
        }
        protected internal string DecryptProp(string Property)
        {
            Decryptthis();
            return GetCache(Property);
        }
        protected internal void EncryptProp(string Property, string value)
        {
            iseDirty = true;
            SetCache(Property, value);
            if (ImmediateEncryption)
            {
                Encryptthis();
            }
        }
        private void Decryptthis()
        {
            CheckCache();
            if (hasBeenDecrypted)
            {
                return;
            }
            SetDecryptedValues(
                Encryption.DecryptList(
                    GetEncryptedValues()
                    )
                );
            hasBeenDecrypted = true;
        }
        
        private void Encryptthis()
        {
            iseDirty = false;
            CheckCache();
            SetEncryptedValues(
                Encryption.EncryptList(
                    GetDecryptedValues()
                    )
                );

        }
        protected internal void SetEncryptedValues(IList<byte[]> EncryptedValues, IEnumerable<String> PropertyMap = null)
        {
            if (PropertyMap == null)
            {
                PropertyMap = Encryption.GetPropertyMap(GetType());
            }
            var Accessor = FastMember.ObjectAccessor.Create(this);
            var y = 0;
            foreach (var p in PropertyMap)
            {
                Accessor[EncryptedFieldPrefix + p] = EncryptedValues[y];
                y++;
            }
        }
        internal void SetDecryptedValues(IList<string> DecryptedValues, IEnumerable<String> PropertyMap = null)
        {
        
            if (PropertyMap == null)
            {
                PropertyMap = Encryption.GetPropertyMap(GetType());
            }
            var y = 0;
            foreach (var p in PropertyMap)
            {
                SetCache(p, DecryptedValues[y]);
                y++;
            }
            
        }
        internal List<EncryptedBinaryRow> GetEncryptedValues(IEnumerable<String> PropertyMap = null)
        {
            var List = new List<EncryptedBinaryRow>();
            if (PropertyMap == null)
            {
                PropertyMap = Encryption.GetPropertyMap(GetType());
            }
            var Accessor = FastMember.ObjectAccessor.Create(this);
            var dGuid = Guid.NewGuid(); // decpreciated
            foreach (var p in PropertyMap)
            {
                List.Add(new EncryptedBinaryRow()
                    {
                        Guid = dGuid,
                        Binary = (byte[]) Accessor[EncryptedFieldPrefix + p]
                    });
            }
            return List;
            
        }
        internal List<DecryptedStringRow> GetDecryptedValues(IEnumerable<String> PropertyMap = null)
        {
            var List = new List<DecryptedStringRow>();
            if (PropertyMap == null)
            {
                PropertyMap = Encryption.GetPropertyMap(GetType());
            }
            var dGuid = Guid.NewGuid(); // decpreciated
            foreach (var p in PropertyMap)
            {
                List.Add(new DecryptedStringRow()
                {
                    Guid = dGuid,
                    String = GetCache(p)
                });
            }
            return List;            
        }
        private void SetCache(string Property, string value)
        {
            CheckCache();
            if(!EncryptionCache.ContainsKey(Property))
            {
                EncryptionCache.Add(Property, value);
                return;
            }
            EncryptionCache[Property] = value;
        }
        private string GetCache(string Property)
        {
            CheckCache();
            if (EncryptionCache.ContainsKey(Property))
            {
                return EncryptionCache[Property];
            }
            return "";
        }

        private void CheckCache()
        {
            if (EncryptionCache == null)
            {
                EncryptionCache = new Dictionary<string, string>();
            }
        }
    }
}
