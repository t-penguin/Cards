using Photon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainUI : PunBehaviour
{
    [SerializeField] GameObject _mainPanel;
    [SerializeField] GameObject _multiplayerPanel;
    [SerializeField] GameObject _returnButton;

    private void Start()
    {
        OnReturnToMain();
    }

    #region PUN Callbacks

    public override void OnConnectedToMaster()
    {
        OnMultiplayerSelected();
    }

    #endregion

    public void OnMultiplayerSelected()
    {
        _mainPanel.SetActive(false);
        _multiplayerPanel.SetActive(true);
        _returnButton.SetActive(true);
    }

    public void OnReturnToMain()
    {
        _multiplayerPanel.SetActive(false);
        _returnButton.SetActive(false);
        _mainPanel.SetActive(true);
    }
}