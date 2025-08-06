using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    [SerializeField] private TMP_Text DamageTextGameObject;
    [SerializeField] private TMP_Text CountPunches;
    public List<TMP_Text> GeneratedText;
    private TMP_Text DeleteObj;
    private Camera MainCamera;
    private float Speed = 25f;
    private int punches;
    private void Awake()
    {
        MainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }
    public void GenerateDamageText(string TextDamage, Vector3 Pos)
    {
        TMP_Text Text = Instantiate(DamageTextGameObject, transform.position, transform.rotation, this.transform);
        Text.transform.position = MainCamera.WorldToScreenPoint(Pos);
        Text.text = TextDamage;
        Text.name = Random.Range(-Speed / 2f, Speed / 2f).ToString();
        GeneratedText.Add(Text);
        punches++;
        CountPunches.text = "Попаданий: " + punches;
    }
    
    private void Update()
    {
        if(GeneratedText != null)
        {
            foreach (TMP_Text TextDamage in GeneratedText)
            {
                float randomY = Speed;
                TextDamage.alpha -= 2f * Time.deltaTime;
                var NewVector = TextDamage.transform.position + new Vector3(float.Parse(TextDamage.name), randomY, 0);
                TextDamage.transform.position = Vector3.Lerp(TextDamage.transform.position, NewVector, 10f * Time.deltaTime);
                if(TextDamage.alpha <= 0)
                {
                    DeleteObj = TextDamage;
                }
            }
        }
        DestroyText();
    }
    
    private void DestroyText()
    {
        if (DeleteObj != null)
        {
            GeneratedText.RemoveAt(GeneratedText.IndexOf(DeleteObj));
            Destroy(DeleteObj.gameObject);
            DeleteObj = null;
        }
    }
}
