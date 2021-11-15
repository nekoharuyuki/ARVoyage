using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Niantic.ARVoyage
{
    // Wrappers around UnityEvent to allow for generic instantiation
    // Because these events are instantiated, they can be invoked without checking whether they are null.

    // When defining an AppEvent in your class, follow this pattern for instance/static:

    // public void AppEvent MyEvent =  new AppEvent();
    // public static void AppEvent MyEvent =  new AppEvent();

    // Because the Inspector doesn't support generics only the non-parameterized version allows 
    // for drag-and-drop support in the inspector.
    // If you want to have an inspector-compatible AppEvent with parameters, use this pattern:

    // Define a non-generic class that extends the event's parameter types
    // [Serializable] public class MyParamAppEvent : AppEvent<string, int, float> { }
    // Define a serialized field to be populated in the inspector
    // Doesn't need to be instantiated, because the inspector serialiation will instantiate it
    // [SerializeField] public MyParamAppEvent myParamAppEvent;

    [Serializable]
    public class AppEvent : UnityEvent { }

    [Serializable]
    public class AppEvent<T> : UnityEvent<T> { }

    [Serializable]
    public class AppEvent<T, U> : UnityEvent<T, U> { }

    [Serializable]
    public class AppEvent<T, U, V> : UnityEvent<T, U, V> { }

    [Serializable]
    public class AppEvent<T, U, V, W> : UnityEvent<T, U, V, W> { }
}
