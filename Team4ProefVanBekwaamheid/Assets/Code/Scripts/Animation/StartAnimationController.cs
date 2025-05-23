using System.Collections.Generic;
using UnityEngine;

public class StartAnimationController : MonoBehaviour
{
    [SerializeField] private List<Animator> _animators;
    public void StartGame()
    {
        foreach (var animator in _animators)
        {
            animator.SetTrigger("Start");
        }
    }
}