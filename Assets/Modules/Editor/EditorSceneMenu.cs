using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Editor toolbar for loading project scenes
    /// </summary>
    public class EditorSceneMenu : MonoBehaviour
    {
        [MenuItem("Scenes/BuildAShip")]
        static void OpenBuildAShip()
        {
            EditorSceneManager.OpenScene("Assets/Modules/BuildAShip/BuildAShip.unity");
        }

        [MenuItem("Scenes/Map")]
        static void OpenMap()
        {
            EditorSceneManager.OpenScene("Assets/Modules/Map/Map.unity");
        }

        [MenuItem("Scenes/SnowballFight")]
        static void OpenSnowballFight()
        {
            EditorSceneManager.OpenScene("Assets/Modules/SnowballFight/SnowballFight.unity");
        }

        [MenuItem("Scenes/SnowballToss")]
        static void OpenSnowballToss()
        {
            EditorSceneManager.OpenScene("Assets/Modules/SnowballToss/SnowballToss.unity");
        }

        [MenuItem("Scenes/Splash")]
        static void OpenSplash()
        {
            EditorSceneManager.OpenScene("Assets/Modules/Splash/Splash.unity");
        }

        [MenuItem("Scenes/Walkabout")]
        static void OpenWalkabout()
        {
            EditorSceneManager.OpenScene("Assets/Modules/Walkabout/Walkabout.unity");
        }

    }
}
