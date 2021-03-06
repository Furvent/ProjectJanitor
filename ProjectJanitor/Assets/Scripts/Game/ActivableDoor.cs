using UnityEngine;
using System.Collections;
using System;
using MonoPersistency;
using GalacticJanitor.Engine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ActivableDoor : Interactable {

    [Header("Interaction")]
    public bool m_onlyOnce;
    public bool m_startOff;
    public GameObject m_object;
    public bool m_notify;
    public string m_noticeOn;
    public string m_noticeOff;
    public Color m_noticeColor = Color.white;
    public int m_noticeDuration = 1;
    public RedWire m_redWire;

    void Awake()
    {
        if (m_startOff)
            m_object.SetActive(false);

        UpdateWire();
    }

    public override void OnInteract()
    {
        m_object.SetActive(!m_object.activeInHierarchy);

        if (m_onlyOnce)
            CanInteract = false;

        if (m_notify)
            GameController.NotifyPlayer(m_object.activeInHierarchy ? m_noticeOn : m_noticeOff, m_noticeColor, m_noticeDuration);

        UpdateWire();
    }

    public override void CollectData(DataContainer container)
    {
        container.Addvalue("interact", CanInteract);
        container.Addvalue("active", m_object.activeInHierarchy);
    }

    public override void LoadData(DataContainer container)
    {
        CanInteract = container.GetValue<bool>("interact");
        m_object.SetActive(container.GetValue<bool>("active"));
        UpdateWire();
    }

    void UpdateWire()
    {
        if (m_redWire)
            m_redWire.SetSwitch(m_object.activeInHierarchy);
    }
}
