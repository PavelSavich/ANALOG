using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatrixController : MonoBehaviour
{
    [SerializeField]
    private float minGreen = 0.25f;

    [SerializeField]
    private float maxGreen = 1.0f;

    [SerializeField]
    private float minSpeed = 10f;

    [SerializeField]
    private float maxSpeed = 30f;

    [SerializeField]
    private int minGap = 90;

    [SerializeField]
    private int maxGap = 210;


    [SerializeField]
    private List<LEDDisplay> displays;

    [SerializeField]
    private List<LEDTextRenderer> textRenderers;

    private Coroutine _changeLettersCorutine;

    void Start()
    {
        foreach (LEDDisplay display in displays) 
        {
            float brightness = Random.Range(minGreen, maxGreen);
            display.OnColor = new Color(0f, brightness, 0f);
        }

        foreach (LEDTextRenderer renderer in textRenderers)
        {
            char randomChar = GetRandomLetter();
            renderer.SetText(randomChar.ToString());

            float speed = Random.Range(minSpeed, maxSpeed);
            renderer.SetScrollSpeed(speed);

            int gap = Random.Range(minGap, maxGap);
            renderer.SetLoopGap(gap);

            renderer.StartAnimation();
        }

        _changeLettersCorutine = StartCoroutine(ChangeLetters());
    }

    private char GetRandomLetter()
    {
        bool upper = Random.value > 0.5f;
        int ascii = upper ? Random.Range(65, 91) : Random.Range(97, 123);
        return (char)ascii;
    }

    private IEnumerator ChangeLetters() 
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);

            int randomIndex = Random.Range(0, textRenderers.Count);
            LEDTextRenderer renderer = textRenderers[randomIndex];

            char randomChar = GetRandomLetter();
            renderer.SetText(randomChar.ToString());
        }
    }
}
