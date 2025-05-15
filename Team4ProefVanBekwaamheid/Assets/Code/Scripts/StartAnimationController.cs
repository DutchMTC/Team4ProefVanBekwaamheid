using System.Collections.Generic;
using UnityEngine;

public class StartAnimationController : MonoBehaviour
{
    [SerializeField] private List<Animator> animators;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {

        }
    }

    public void StartGame()
    {
        foreach (var animator in animators)
        {
            animator.SetTrigger("Start");
        }
    }
}