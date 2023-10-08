using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TabPair 
{
    [SerializeField]
    private Button _tabButton;
    public Button TabButton => _tabButton;
    [SerializeField]
    private CanvasGroup _tabContent;
    public CanvasGroup TabContent => _tabContent;
}
