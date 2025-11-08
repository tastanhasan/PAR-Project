using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "JSON Point of Interest Set", menuName = "Json Point of Interest Set", order = 2)]
public class pLab_PointOfInterestJsonSet : ScriptableObject
{
    #region Variables

    [SerializeField]
    private List<pLab_POIObject> pointOfInterests = new List<pLab_POIObject>();

    [Header("Shared Prefabs and Icons")]
    public Sprite sharedIcon;
    public GameObject sharedObjectPrefab;
    public GameObject sharedModelPrefab;
    public GameObject sharedCanvasPrefab;

    public int sharedTrackingRadius = 100;
    public int sharedTrackingExitMargin = 20;
    public int sharedClosedTrackingRadius = 20;
    public int sharedClosedTrackingExitMargin = 10;

    [Range(0, 360)]
    public float sharedFacingDirectionHeading = 90f;
    public float sharedRelativeHeight = 0f;

    [Header("Shared Tracking State")]
    public POITrackingState sharedTrackingState = POITrackingState.CloseTracking;
    #endregion

    #region Properties

    public List<pLab_POIObject> PointOfInterests { get { return pointOfInterests; } set { pointOfInterests = value; } }

    #endregion

    #region Public Methods

    public void AssignSharedAttributesToPOIs()
    {
        foreach (var poi in pointOfInterests)
        {
            if (poi.icon == null) poi.icon = sharedIcon;
            if (poi.objectPrefab == null) poi.objectPrefab = sharedObjectPrefab;
            if (poi.modelPrefab == null) poi.modelPrefab = sharedModelPrefab;
            if (poi.canvasPrefab == null) poi.canvasPrefab = sharedCanvasPrefab;

            poi.trackingRadius = Mathf.Max(poi.trackingRadius, sharedTrackingRadius);
            poi.trackingExitMargin = Mathf.Max(poi.trackingExitMargin, sharedTrackingExitMargin);
            poi.closeTrackingRadius = Mathf.Max(poi.closeTrackingRadius, sharedClosedTrackingRadius);
            poi.closeTrackingExitMargin = Mathf.Max(poi.closeTrackingExitMargin, sharedClosedTrackingExitMargin);
            poi.facingDirectionHeading = Mathf.Max(poi.facingDirectionHeading, sharedFacingDirectionHeading);
            poi.relativeHeight = Mathf.Max(poi.relativeHeight, sharedRelativeHeight);


            // TrackingState merkezi olarak atanýyor
            poi.trackingState = sharedTrackingState;

        }
    }


    #endregion
}
