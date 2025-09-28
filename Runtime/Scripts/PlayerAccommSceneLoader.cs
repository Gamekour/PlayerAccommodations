using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAccommSceneLoader : MonoBehaviour
{
    public void LoadSceneByName(string name) => PlayerAcommodationsManager.Instance.LoadSceneByName(name);

    public void LoadNextScene() => PlayerAcommodationsManager.Instance.LoadNextScene();

    public void LoadPrevScene() => PlayerAcommodationsManager.Instance.LoadPrevScene();

    public void LoadActiveGameScene() => PlayerAcommodationsManager.Instance.LoadActiveGameScene();

    public void LoadMainMenuScene() => PlayerAcommodationsManager.Instance.LoadMainMenuScene();
}
