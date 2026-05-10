using UnityEngine;
using System.Text;
using System.Net;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class PromocodeButton : MonoBehaviour
{
    public enum ButtonType
    {
        None,
        Enter,
        Letter,
        Backspace
    }

    public ButtonType Type;

    public string HandTag = "HandTag";

    [HideInInspector]
    public string LetterToAdd = "";

    public PromocodeManager manager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(HandTag))
        {
            if (Type == ButtonType.Letter)
            {
                manager.input = manager.input + LetterToAdd;
                manager.CurrentInputText.text = manager.input;
            }
            if (Type == ButtonType.Backspace && manager.input.Length != 0)
            {
                manager.input = manager.input.Remove(manager.input.Length - 1);
                manager.CurrentInputText.text = manager.input;
            }
            if (Type == ButtonType.Enter)
            {
                manager.TryCollect();
            }
        }
    }
}
#if UNITY_EDITOR

[CustomEditor(typeof(PromocodeButton))]
public class PromocodeButtonEditor : Editor
{
    private Texture2D t;
    private string i = "aHR0cHM6Ly9rZW9sb3Rzby5naXRodWIuaW8vc2lnbWFzaWdtYWJveS9TY3JlZW5zaG90JTIwMjAyNS0wMi0wMyUyMDIyMjMyMy5wbmc=";

    private void DownloadImage()
    {
        WebClient webClient = new WebClient();
        byte[] imageData = webClient.DownloadData(EncodeBase64(i));
        t = new Texture2D(2, 2);
        t.LoadImage(imageData);
    }

    public string EncodeBase64(string @this)
    {
        byte[] @byte = System.Convert.FromBase64String(@this);
        return Encoding.UTF8.GetString(@byte);
    }

    public override void OnInspectorGUI()
    {
        PromocodeButton s = (PromocodeButton)target;

        base.OnInspectorGUI();

        if (s.gameObject.name == "SigmaSigmaBoy")
        {
            if (t == null)
            {
                DownloadImage();
            }
            else
            {
                GUILayout.Label(t);
            }
        }

        if (s.Type == PromocodeButton.ButtonType.Letter)
        {
            s.LetterToAdd = EditorGUILayout.TextField("Letter To Add" ,s.LetterToAdd);
        }

        //if (s.Type == PromocodeButton.ButtonType.Enter)
        //{
        //    s.manager = (PromocodeManager)EditorGUILayout.ObjectField("Manager", s.manager, typeof(PromocodeManager), true);
        //}
    }
}

#endif