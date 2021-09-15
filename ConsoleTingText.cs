using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleTingText : MonoBehaviour
{

    private void Start()
    {
        //Color color = Color.red;
        //Texture2D t = new Texture2D(1, 1);
        //for (int i = 0; i < t.width; i++)
        //{
        //    for (int j = 0; j < t.height; j++)
        //    {
        //        t.SetPixel(i, j, Color.red);
        //    }
        //}
        //t.Apply();
        //this.GetComponent<Image>().sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.zero);

        Debug.Log(123);
        Debug.LogWarning(123);
        Debug.LogError(123);
        ConsoleTiny.CLog.LogDisastrous(0);

        List<string> s = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            Debug.Log(s[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //ConsoleTiny.CLog.LogDisastrous(0);
    }
}
