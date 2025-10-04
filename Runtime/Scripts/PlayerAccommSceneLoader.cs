using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAccommSceneLoader : MonoBehaviour
{
    public void LoadSceneByName(string name) => PlayerAccommodationsManager.Instance.LoadSceneByName(name);

    public void LoadNextScene() => PlayerAccommodationsManager.Instance.LoadNextScene();

    public void LoadPrevScene() => PlayerAccommodationsManager.Instance.LoadPrevScene();

    public void LoadActiveGameScene() => PlayerAccommodationsManager.Instance.LoadActiveGameScene();

    public void LoadMainMenuScene() => PlayerAccommodationsManager.Instance.LoadMainMenuScene();

    public void LoadSceneByNameAndUnlock(string name) => PlayerAccommodationsManager.Instance.LoadSceneByNameAndUnlock(name);

    public void LoadNextSceneAndUnlock() => PlayerAccommodationsManager.Instance.LoadNextSceneAndUnlock();

    public void LoadPrevSceneAndUnlock() => PlayerAccommodationsManager.Instance.LoadPrevSceneAndUnlock();

    public void LoadSceneByIndex(int index) => PlayerAccommodationsManager.Instance.LoadSceneByIndex(index);
    
    public void LoadSceneByIndexAndUnlock(int index) => PlayerAccommodationsManager.Instance.LoadSceneByIndexAndUnlock(index);
}
