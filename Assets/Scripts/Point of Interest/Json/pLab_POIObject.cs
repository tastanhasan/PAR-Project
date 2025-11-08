using System;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class pLab_POIObject
{
    #region Variables

    [Header("Information")]
    public string poiName;

    [SerializeField]
    [TextArea(2, 20)]
    public string description;

    // Artýk sprite ve prefab nesneleri burada saklanmýyor, bunlar set içinde atanacak

    [Header("Location")]
    public pLab_LatLon coordinates;

    [Header("Far Tracking Radiuses (meters)")]
    public int trackingRadius;
    public int trackingExitMargin;

    [Header("Close Tracking Radiuses (meters)")]
    public int closeTrackingRadius;
    public int closeTrackingExitMargin ;

    [Header("Positional Settings")]
    public POIPositionMode positionMode = POIPositionMode.AlignWithGround;
   
    public float facingDirectionHeading;
    public float relativeHeight;

    public POITrackingState trackingState;

    public Sprite icon; // Sprite burada saklanacak
    public GameObject objectPrefab; // GameObject burada saklanacak
    public GameObject modelPrefab; // GameObject burada saklanacak
    public GameObject canvasPrefab; // GameObject burada saklanacak

    #endregion

    #region Properties

    public bool Tracking => trackingState == POITrackingState.FarTracking || trackingState == POITrackingState.CloseTracking;
    public bool CloseTracking => trackingState == POITrackingState.CloseTracking;
    public bool FarTracking => trackingState == POITrackingState.FarTracking;

    public int TrackingExitRadius => trackingRadius + trackingExitMargin;
    public int CloseTrackingExitRadius => closeTrackingRadius + closeTrackingExitMargin;

    #endregion

    #region Public Methods

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public static pLab_POIObject FromJson(string json)
    {
        return JsonUtility.FromJson<pLab_POIObject>(json);
    }

    #endregion

}
