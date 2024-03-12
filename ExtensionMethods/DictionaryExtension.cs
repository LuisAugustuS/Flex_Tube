using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

namespace Bundles.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods for the DBDictionary and DBObject types.
    /// </summary>
    internal static class DictionaryExtension
    {
        /// <summary>
        /// Gets or creates the extension dictionary.
        /// Code credits: Gilles Chanteau
        /// The original code is found <see href="https://www.theswamp.org/index.php?topic=59330.msg619726#msg619726">here</see>:
        /// </summary>
        /// <param name="dbObject">Instance to which the method applies.</param>
        /// <param name="tr">Transaction or OpenCloseTransaction tu use.</param>
        /// <param name="mode">Open mode to obtain in.</param>
        /// <returns>The extension dictionary.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name ="dbObject"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name ="tr"/> is null.</exception>
        public static DBDictionary GetOrCreateExtensionDictionary(this DBObject dbObject,
            Transaction tr, OpenMode mode = OpenMode.ForRead)
        {
            Assert.IsNotNull(dbObject, nameof(dbObject));
            Assert.IsNotNull(tr, nameof(tr));

            if (dbObject.ExtensionDictionary.IsNull)
            {
                dbObject.OpenForWrite(tr);
                dbObject.CreateExtensionDictionary();
            }
            return (DBDictionary)tr.GetObject(dbObject.ExtensionDictionary, mode);
        }

        /// <summary>
        /// Opens the object for write.
        /// </summary>
        /// <param name="dbObj">Instance to which the method applies.</param>
        /// <param name="tr">Transaction or OpenCloseTransaction tu use.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name ="dbObj"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name ="tr"/> is null.</exception>
        public static void OpenForWrite(this DBObject dbObj, Transaction tr)
        {
            Assert.IsNotNull(dbObj, nameof(dbObj));
            Assert.IsNotNull(tr, nameof(tr));

            if (!dbObj.IsWriteEnabled)
            {
                tr.GetObject(dbObj.ObjectId, OpenMode.ForWrite);
            }
        }

        /// <summary>
        /// Sets the xrecord data of the extension dictionary of the object.
        /// </summary>
        /// <param name="target">Instance to which the method applies.</param>
        /// <param name="tr">Transaction or OpenCloseTransaction tu use.</param>
        /// <param name="key">The xrecord key.</param>
        /// <param name="data">The new xrecord data.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name ="target"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name ="tr"/> is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name ="key"/> is null or empty.</exception>
        public static void SetXDictionaryXrecordData(this DBObject target, Transaction tr, string key, ResultBuffer data)
        {
            Assert.IsNotNull(target, nameof(target));
            Assert.IsNotNull(tr, nameof(tr));
            Assert.IsNotNullOrWhiteSpace(key, nameof(key));

            target.GetOrCreateExtensionDictionary(tr).SetXrecordData(tr, key, data);
        }

        /// <summary>
        /// Sets the xrecord data.
        /// </summary>
        /// <param name="dictionary">Instance to which the method applies.</param>
        /// <param name="tr">Transaction or OpenCloseTransaction tu use.</param>
        /// <param name="key">Key of the xrecord, the xrecord is created if it does not already exist.</param>
        /// <param name="data">Data</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name ="dictionary"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name ="tr"/> is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name ="key"/> is null or empty.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name ="data"/> is null.</exception>
        public static void SetXrecordData(this DBDictionary dictionary, Transaction tr, string key, ResultBuffer data)
        {
            Assert.IsNotNull(dictionary, nameof(dictionary));
            Assert.IsNotNull(tr, nameof(tr));
            Assert.IsNotNullOrWhiteSpace(key, nameof(key));
            Assert.IsNotNull(data, nameof(data));
            Xrecord xrecord;
            if (dictionary.Contains(key))
            {
                var id = dictionary.GetAt(key);
                if (!id.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(Xrecord))))
                    throw new System.ArgumentException("Not an Xrecord'", nameof(key));
                {
                    xrecord = (Xrecord)tr.GetObject(id, OpenMode.ForWrite);
                }
            }
            else
            {
                xrecord = new Xrecord();
                dictionary.OpenForWrite(tr);
                dictionary.SetAt(key, xrecord);
                tr.AddNewlyCreatedDBObject(xrecord, true);
            }
            xrecord.Data = data;
        }

        /// <summary>
        /// Tries to get the object extension dictionary.
        /// </summary>
        /// <param name="dbObject">Instance to which the method applies.</param>
        /// <param name="tr">Transaction or OpenCloseTransaction tu use.</param>
        /// <param name="dictionary">Output dictionary.</param>
        /// <param name="mode">Open mode to obtain in.</param>
        /// <param name="openErased">Value indicating whether to obtain erased objects.</param>
        /// <returns><c>true</c>, if the operation succeeded; <c>false</c>, otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name ="dbObject"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name ="tr"/> is null.</exception>
        public static bool TryGetExtensionDictionary(
            this DBObject dbObject,
            Transaction tr,
            out DBDictionary dictionary,
            OpenMode mode = OpenMode.ForRead,
            bool openErased = false)
        {
            Assert.IsNotNull(dbObject, nameof(dbObject));
            Assert.IsNotNull(tr, nameof(tr));

            dictionary = default;
            var id = dbObject.ExtensionDictionary;
            if (id.IsNull)
                return false;
            dictionary = (DBDictionary)tr.GetObject(id, mode, openErased);
            return true;
        }

        /// <summary>
        /// Tries to get the xrecord data of the extension dictionary of the object.
        /// </summary>
        /// <param name="source">Instance to which the method applies.</param>
        /// <param name="tr">Transaction or OpenCloseTransaction tu use.</param>
        /// <param name="key">Xrecord key.</param>
        /// <param name="data">Output data.</param>
        /// <returns>The xrecord data or null if the xrecord does not exists.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name ="source"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name ="tr"/> is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name ="key"/> is null or empty.</exception>
        public static bool TryGetXDictionaryXrecordData(this DBObject source, Transaction tr, string key, out ResultBuffer data)
        {
            Assert.IsNotNull(source, nameof(source));
            Assert.IsNotNull(tr, nameof(tr));
            Assert.IsNotNullOrWhiteSpace(key, nameof(key));

            data = default;
            return
                source.TryGetExtensionDictionary(tr, out DBDictionary xdict) &&
                xdict.TryGetXrecordData(tr, key, out data);
        }

        /// <summary>
        /// Tries to get the xrecord data.
        /// </summary>
        /// <param name="dictionary">Instance to which the method applies.</param>
        /// <param name="tr">Active transaction</param>
        /// <param name="key">Key of the xrecord.</param>
        /// <param name="data">Output data.</param>
        /// <returns><c>true</c>, if the operation succeeded; <c>false</c>, otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name ="dictionary"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name ="tr"/> is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name ="key"/> is null or empty.</exception>
        public static bool TryGetXrecordData(
            this DBDictionary dictionary,
            Transaction tr,
            string key,
            out ResultBuffer data)
        {
            Assert.IsNotNull(dictionary, nameof(dictionary));
            Assert.IsNotNull(tr, nameof(tr));
            Assert.IsNotNullOrWhiteSpace(key, nameof(key));

            data = default;
            if (dictionary.Contains(key))
            {
                var id = dictionary.GetAt(key);
                if (id.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(Xrecord))))
                {
                    var xrecord = (Xrecord)tr.GetObject(id, OpenMode.ForRead);
                    data = xrecord.Data;
                    return data != null;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Provides methods to throw an exception if an assertion is wrong.
    /// </summary>
    static class Assert
    {
        /// <summary>
        /// Throws ArgumentNullException if the object is null.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="obj">The instance to which the assertion applies.</param>
        /// <param name="paramName">Name of the parameter.</param>
        public static void IsNotNull<T>(T obj, string paramName) where T : class
        {
            if (obj == null)
            {
                throw new System.ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// Throws eNullObjectId if the <c>ObjectId</c> is null.
        /// </summary>
        /// <param name="id">The ObjectId to which the assertion applies.</param>
        /// <param name="paramName">Name of the parameter.</param>
        public static void IsNotObjectIdNull(ObjectId id, string paramName)
        {
            if (id.IsNull)
            {
                throw new Exception(ErrorStatus.NullObjectId, paramName);
            }
        }

        /// <summary>
        /// Throws ArgumentException if the string is null or empty.
        /// </summary>
        /// <param name="str">The string to which the assertion applies.</param>
        /// <param name="paramName">Name of the parameter.</param>
        public static void IsNotNullOrWhiteSpace(string str, string paramName)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                throw new System.ArgumentException("eNullOrWhiteSpace", paramName);
            }
        }
    }
}
