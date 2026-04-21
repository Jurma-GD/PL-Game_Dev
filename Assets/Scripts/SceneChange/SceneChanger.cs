using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class NewMonoBehaviourScript : MonoBehaviour
{
    public string sceneToLoad;
    public Animator fadeAnim;
    public float fadeTime = .5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            fadeAnim.Play("FadeToWhite");
            StartCoroutine(DelayFade());

        }

        IEnumerator DelayFade()
        {
            yield return new WaitForSeconds(fadeTime);
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
