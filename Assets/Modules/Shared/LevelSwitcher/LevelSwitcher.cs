using UnityEngine;
using UnityEngine.SceneManagement;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Utility for switching levels in the project
    /// </summary>
    public class LevelSwitcher : MonoBehaviour, ISceneDependency
    {
        public void ReturnToMap()
        {
            LoadLevel(Level.Map, fadeOutBeforeLoad: true);
        }

        public void LoadLevel(Level level, bool fadeOutBeforeLoad)
        {
            LoadScene(level.ToString(), fadeOutBeforeLoad);
        }

        public void ReloadCurrentLevel(bool fadeOutBeforeLoad)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            LoadScene(currentScene.name, fadeOutBeforeLoad);
        }

        private void LoadScene(string sceneName, bool fadeOutBeforeLoad)
        {
            if (fadeOutBeforeLoad && SceneLookup.TryGet<Fader>(out Fader fader))
            {
                fader.FadeSceneOut(onComplete: () => SceneManager.LoadScene(sceneName));
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}

