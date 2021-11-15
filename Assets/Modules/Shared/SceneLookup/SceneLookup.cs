using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Add this interface to any Monobehaviour to have it be auto-added to the lookup while the scene is loading
    /// </summary>
    public interface ISceneDependency { }

    /// <summary>
    /// Type-based lookup of scene objects
    /// Monobehaviors that implement the ISceneDependency inteface will automatically be added to the lookup
    /// after the scene is loaded, before Awake methods are called
    /// Obects can also add themselves at runtime
    /// Cleared when scenes are unloaded
    /// </summary>
    public static class SceneLookup
    {
        private static Dictionary<Type, object> lookup = new Dictionary<Type, object>();
        private static bool ranAutoInitializationForScene;

        /// <summary>
        /// Get an object from the lookup by type
        /// </summary>
        public static T Get<T>(bool warnIfNotFound = true) where T : class
        {
            AddSceneDependenciesIfNecessary();

            lookup.TryGetValue(typeof(T), out object instance);

            if (instance != null)
            {
                return instance as T;
            }

            if (warnIfNotFound)
            {
                Debug.LogWarning(typeof(SceneLookup).Name + " didn't find object of type " + typeof(T).Name);
            }

            return null;
        }

        /// <summary>
        /// Try to get an object from the lookup by type
        /// </summary>
        public static bool TryGet<T>(out T obj) where T : class
        {
            AddSceneDependenciesIfNecessary();

            lookup.TryGetValue(typeof(T), out object instance);
            obj = instance as T;

            return obj != null;
        }

        /// <summary>
        /// Add an object to the lookup
        /// </summary>
        /// <param name="obj">The object</param>
        public static void Add<T>(T obj)
        {
            AddInternal(obj.GetType(), obj);
        }

        static SceneLookup()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            // Clear the lookup on scene unloaded
            lookup.Clear();
            ranAutoInitializationForScene = false;
        }


        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            // In case nothing has run the add scene dependency logic by the time the scene has loaded, run it
            AddSceneDependenciesIfNecessary();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AddSceneDependenciesIfNecessary()
        {
            // Bail if already added dependencies for this scene
            if (ranAutoInitializationForScene)
            {
                return;
            }

            MonoBehaviour[] monoBehaviours = GameObject.FindObjectsOfType<MonoBehaviour>();
            foreach (MonoBehaviour monoBehaviour in monoBehaviours)
            {
                if (monoBehaviour is ISceneDependency)
                {
                    AddInternal(monoBehaviour.GetType(), monoBehaviour);
                }
            }
            ranAutoInitializationForScene = true;
        }

        private static void AddInternal(Type type, object obj)
        {
            Debug.Log("Add to lookup " + type + " -> " + obj);

            if (lookup.ContainsKey(type))
            {
                Debug.LogWarning("Updating existing lookup for type " + type.Name +
                    " from " + lookup[type] + " -> " + obj);
            }

            lookup[type] = obj;
        }
    }
}
