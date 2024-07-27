using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IDCardVisual : MonoBehaviour
{
    [SerializeField] Image _image;
    [SerializeField] TextMeshProUGUI _name;

    public void SetIDCard(string name, Sprite image)
    {
        _name.text = name;
        _image.sprite = image;
    }
}