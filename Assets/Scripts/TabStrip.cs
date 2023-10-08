using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class TabStrip : MonoBehaviour
{
    [SerializeField]
    private TabPair[] _tabCollection;
    [SerializeField]
    private Sprite _tabIconPicked;
    [SerializeField]
    private Sprite _tabIconDefault;
    [SerializeField]
    private Color _tabColorPicked;
    [SerializeField]
    private Color _tabColorDefault;
    [SerializeField]
    private Button _defaultTab;

    protected int CurrentTabIndex { get; set; }

    protected void Start()
    {
        for (int i = 0; i < _tabCollection.Length; i++)
        {
            // Avoid bug with variable used in closure below being updated by loop
            int index = i;
            _tabCollection[index].TabButton.onClick.AddListener(new UnityAction(() => PickTab(index)));
            _tabCollection[index].ButtonText = _tabCollection[index].TabButton.GetComponentInChildren<TMP_Text>();
        }

        EnableDefaultTab();
    }

    private void EnableDefaultTab()
    {
        //Initialize all tabs to an unpicked state
        for (int i = 0; i < _tabCollection.Length; i++)
        {
            SetTabState(i, false);
        }

        //Pick the default tab
        if (_tabCollection.Length > 0)
        {
            int? index = FindTabIndex(_defaultTab);
            //If tab is invalid, instead default to the first tab.
            if (index == null)
            {
                index = 0;
            }
            CurrentTabIndex = index.Value;
            SetTabState(CurrentTabIndex, true);
        }
    }

    public void PickTab(int index)
    {
        SetTabState(CurrentTabIndex, false);
        CurrentTabIndex = index;
        SetTabState(CurrentTabIndex, true);
    }

    private void SetTabState(int index, bool picked){
        TabPair affectedItem = _tabCollection[index];
        affectedItem.TabContent.interactable = picked;
        affectedItem.TabContent.blocksRaycasts = picked;
        affectedItem.TabContent.alpha = picked ? 1 : 0;
        affectedItem.TabButton.image.sprite = picked ? _tabIconPicked : _tabIconDefault;
        affectedItem.ButtonText.color = picked ? _tabColorPicked : _tabColorDefault;
    }

    private int? FindTabIndex(Button tabButton)
    {
        int? matchIndex = null;
        for(int i = 0; i < _tabCollection.Length; i++)
        {
            if(_tabCollection[i].TabButton == tabButton)
            {
                matchIndex = i;
                break;
            }
        }

        if (!matchIndex.HasValue)
        {
            Debug.LogWarning("The tab " + tabButton.gameObject.name + " does not belong to the tab strip " + name + ".");
        }
        return matchIndex;
    }
}