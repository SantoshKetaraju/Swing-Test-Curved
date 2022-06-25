using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Hooked : MonoBehaviour
{
    public TextMeshProUGUI instruction;

    public bool hooked = false, inRange = false;
    // Start is called before the first frame update

    private void OnEnable()
    {
        hooked = false;
    }

    private void OnDisable()
    {
        hooked = false;
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.tag == "Player")
        {
            inRange = true;
            if (PlayerPrefs.GetInt("Played", 0) == 0)
            {
                StartCoroutine(instruct());
            }
            else
            {
                instruction.text = "";
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        inRange = false;
    }

    public void ResetHooks()
    {
        hooked = false;
    }

    public void OnHooked()
    {
        hooked = true;
    }

    private IEnumerator instruct()
    {
#if UNITY_ANDROID
        instruction.text = "Tap on screen to start swing";
#elif UNITY_EDITOR
           instruction.text = "Press G on keyboard to start swing";
#else

           instruction.text = "Press G on keyboard to start swing";

#endif
        yield return new WaitForSeconds(1);

#if UNITY_ANDROID
        instruction.text = "Tap on screen again to stop swing";
#elif UNITY_EDITOR
           instruction.text = "Press G again to stop swing";
#else

           instruction.text = "Press G again to stop swing";

#endif
    }
}