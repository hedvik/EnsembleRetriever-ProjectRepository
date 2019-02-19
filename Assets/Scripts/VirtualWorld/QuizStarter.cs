using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuizStarter : MonoBehaviour
{
    private CaveQuizManager _quizManager;
    private bool _quizStarted = false;

    private void Start()
    {
        _quizManager = GameObject.Find("CaveQuizManager").GetComponent<CaveQuizManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!_quizStarted && other.CompareTag("Player"))
        {
            _quizStarted = true;
            _quizManager.StartQuiz();
        }
    }
}
