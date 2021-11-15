using System.Collections.Generic;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Creation and management of a pool of reusable objects
    /// </summary>
    public class Pooler<T>
    {
        private List<T> pooledObjects = new List<T>();
        private List<T> borrowedObjects = new List<T>();

        private System.Func<T> createCallback;

        public int PooledCount
        {
            get { return pooledObjects.Count; }
        }

        public int BorrowedCount
        {
            get { return borrowedObjects.Count; }
        }

        public void Initialize(System.Func<T> createCallback, int count)
        {
            this.createCallback = createCallback;

            for (int i = 0; i < count; i++)
            {
                T instance = createCallback();
                pooledObjects.Add(instance);
            }
        }

        public T BorrowItem()
        {
            if (pooledObjects.Count > 0)
            {
                T borrowedObject = pooledObjects[0];
                pooledObjects.Remove(borrowedObject);
                borrowedObjects.Add(borrowedObject);
                return borrowedObject;
            }
            else
            {
                T newObject = createCallback();
                borrowedObjects.Add(newObject);
                return newObject;
            }
        }

        public void ReturnItem(T poolObject)
        {
            borrowedObjects.Remove(poolObject);
            pooledObjects.Add(poolObject);
        }

        public void ReturnAll(System.Action<T> returnCallback)
        {
            for (int i = borrowedObjects.Count - 1; i >= 0; i--)
            {
                T borrowedObject = borrowedObjects[i];

                returnCallback(borrowedObject);

                borrowedObjects.Remove(borrowedObject);
                pooledObjects.Add(borrowedObject);
            }
        }
    }
}
