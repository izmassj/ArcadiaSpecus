using UnityEngine;
using DG.Tweening;

public class Animacion_intro : MonoBehaviour
{
    public Transform bomb;
    public Transform bombTarget;

    public GameObject normalBuildings;
    public GameObject destroyedBuildings;
    public GameObject explosionPNG;

    private int state = 0;
    private bool busy = false;

    void Start()
    {
        destroyedBuildings.SetActive(false);
        explosionPNG.SetActive(false);

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && !busy)
        {
            if (state == 0)
            {
                StartBombSequence();
            }
            else if (state == 1)
            {
                ShowDestroyedCity();
            }
        }
    }

    void StartBombSequence()
    {
        if (bombTarget == null) return;

        busy = true;

        Vector3 targetPos = new Vector3(
            bomb.position.x,
            bombTarget.position.y,
            bomb.position.z
        );

        bomb.DOMove(targetPos, 2f)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                bomb.gameObject.SetActive(false);
                normalBuildings.SetActive(false);

                
                explosionPNG.SetActive(true);

                Animator anim = explosionPNG.GetComponent<Animator>();

                if (anim != null)
                {
                    anim.Rebind();          // reinicia estado interno
                    anim.Update(0f);        // refresca inmediatamente
                    anim.Play(0, 0, 0f);    // reproduce desde el inicio
                }
                else
                {
                    Debug.LogWarning("Explosion PNG no tiene Animator");
                }

                state = 1;
                busy = false;
            });
    }

    void ShowDestroyedCity()
    {
        explosionPNG.SetActive(false);
        destroyedBuildings.SetActive(true);

        state = 2;
    }
}