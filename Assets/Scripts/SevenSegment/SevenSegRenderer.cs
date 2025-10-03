using UnityEngine;

public class SevenSegRenderer : MonoBehaviour
{
    [Header("Assign these in the Inspector")]
    [SerializeField]
    private GameObject segA;

    [SerializeField]
    private GameObject segB;

    [SerializeField]
    private GameObject segC;

    [SerializeField]
    private GameObject segD;

    [SerializeField]
    private GameObject segE;

    [SerializeField]
    private GameObject segF;

    [SerializeField]
    private GameObject segG;

    [HideInInspector]
    public EightBitMap map = new EightBitMap();

    public void SetDigit(int d)
    {
        map.SetDigit(d);
        Apply();
    }

    public void Apply()
    {
        if (segA)
        {
            segA.SetActive(map.IsOn(SevenSeg.A));
        }

        if (segB)
        {
            segB.SetActive(map.IsOn(SevenSeg.B));
        }

        if (segC)
        {
            segC.SetActive(map.IsOn(SevenSeg.C));
        }

        if (segD)
        {
            segD.SetActive(map.IsOn(SevenSeg.D));
        }

        if (segE)
        {
            segE.SetActive(map.IsOn(SevenSeg.E));
        }

        if (segF)
        {
            segF.SetActive(map.IsOn(SevenSeg.F));
        }

        if (segG)
        {
            segG.SetActive(map.IsOn(SevenSeg.G));
        }
    }
}
