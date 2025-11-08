using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation;


public class pLab_ARPointOfInterestManager_Json : MonoBehaviour
{
    #region Inner Classes and Enums

    public class POITrackerData
    {
        private pLab_POIObject poi;
        private pLab_PointOfInterestCanvasBase_Json poiCanvas;
        private pLab_PointOfInterestObjectBase_Json poiObject;
        private Vector3 lastUpdateCameraPosition;
        private Vector3 lastUpdatePositionRelativeToCamera;

        public pLab_POIObject POI { get { return poi; } }
        public pLab_PointOfInterestCanvasBase_Json POICanvas { get { return poiCanvas; } }
        public pLab_PointOfInterestObjectBase_Json POIObject { get { return poiObject; } }

        public Vector3 LastUpdateCameraPosition { get { return lastUpdateCameraPosition; } set { lastUpdateCameraPosition = value; } }
        public Vector3 LastUpdatePositionRelativeToCamera { get { return lastUpdatePositionRelativeToCamera; } set { lastUpdatePositionRelativeToCamera = value; } }

        public POITrackerData(pLab_POIObject poi, pLab_PointOfInterestCanvasBase_Json poiCanvas, pLab_PointOfInterestObjectBase_Json poiObject)
        {
            this.poi = poi;
            this.poiCanvas = poiCanvas;
            this.poiObject = poiObject;
        }

        public POITrackerData(pLab_POIObject poi, pLab_PointOfInterestObjectBase_Json poiObject)
        {
            this.poi = poi;
            this.poiObject = poiObject;
        }

        public POITrackerData(pLab_POIObject poi, pLab_PointOfInterestCanvasBase_Json poiCanvas)
        {
            this.poi = poi;
            this.poiCanvas = poiCanvas;
        }


    }

    public enum PositioningMode
    {
        DistanceAndBearing = 0,
        UTM = 1
    }

    #endregion

    #region Variables

    /// <summary>
    /// Set of Point of Interests to track
    /// </summary>
    [SerializeField]
    [Tooltip("Set of point of interest to track")]
    private pLab_PointOfInterestJsonSet pointOfInterestSet;

    [SerializeField]
    private PositioningMode positioningMode = PositioningMode.DistanceAndBearing;

    [SerializeField]
    private pLab_ARTrueNorthFinder arTrueNorthFinder;

    [SerializeField]
    private Camera arCamera;

    [SerializeField]
    [FormerlySerializedAs("deviceHeightEstimater")]
    private pLab_ARDeviceElevationEstimater deviceElevationEstimater;

    [SerializeField]
    private pLab_LocationProvider locationProvider;

    [SerializeField]
    [Tooltip("[OPTIONAL] Object that created POI objects are parented to")]
    private Transform poiParentObject;

    private List<POITrackerData> poiTrackerDatas = new List<POITrackerData>();

    private Transform arCameraTransform;

    private double previousUpdateTimestamp = 0;

    private float previousUpdateAccuracy = 999;

    [SerializeField]
    private pLab_DataBaseManager_Json pLab_DataBaseManager_Json;

    #region Debug Variables
    [Header("Debug")]
    [SerializeField]
    private pLab_PointOfInterestDebug_Json pointOfInterestDebug;

    private float debugTimer = 0f;

    private float trackingDebugInterval = 1f;

    bool updateDebug = false;

  
    #endregion

    #endregion

    #region Properties

    public pLab_PointOfInterestJsonSet PointOfInterestSet
    {
        get { return pointOfInterestSet; }
        set
        {
            if (!pointOfInterestSet.Equals(value))
            {
                pointOfInterestSet = value;
                RefreshTrackers();
            }
        }
    }

    public PositioningMode PositioningModeProperty { get { return positioningMode; } set { positioningMode = value; } }



    private List<pLab_POIObject> PointOfInterests
    {
        get
        {
            return pointOfInterestSet != null && pointOfInterestSet.PointOfInterests != null ? pointOfInterestSet.PointOfInterests : new List<pLab_POIObject>();
        }
    }

    public List<POITrackerData> PoiTrackerDatas { get { return poiTrackerDatas; } }

    #endregion

    #region Inherited methods

    private void Awake()
    {
        if (arCamera != null)
        {
            arCameraTransform = arCamera.gameObject.transform;
        }

        for (int i = 0; i < PointOfInterests.Count; i++)
        {
            PointOfInterests[i].trackingState = POITrackingState.NotTracking;
        }
    }

    private void OnEnable()
    {
        if (locationProvider != null)
        {
            locationProvider.OnLocationUpdated += OnLocationUpdated;
        }

        if (arTrueNorthFinder != null)
        {
            arTrueNorthFinder.OnHeadingUpdated += OnNorthHeadingUpdated;
        }

        ARSession.stateChanged += OnARSessionStateChange;
    }

    private void OnDisable()
    {
        if (locationProvider != null)
        {
            locationProvider.OnLocationUpdated -= OnLocationUpdated;
        }

        if (arTrueNorthFinder != null)
        {
            arTrueNorthFinder.OnHeadingUpdated -= OnNorthHeadingUpdated;
        }

        ARSession.stateChanged -= OnARSessionStateChange;
        StopTrackingPOIs();
    }


    private void Start()
    {
        //Debug
        if (pointOfInterestDebug != null)
        {
            pointOfInterestDebug.SetupPOIList(PointOfInterests);
        }
        //END Debug

        
       
        RefreshTrackers();




    }

    private void Update()
    {

        //Debug
        if (pointOfInterestDebug != null && !updateDebug)
        {
            debugTimer += Time.deltaTime;

            if (debugTimer >= trackingDebugInterval)
            {
                debugTimer = 0;
                updateDebug = true;
            }
        }
        //END Debug

        for (int i = 0; i < poiTrackerDatas.Count; i++)
        {
            pLab_PointOfInterestObjectBase_Json poiObject = poiTrackerDatas[i].POIObject;

            if (poiObject != null)
            {
                poiObject.SetVisibility(arCamera.farClipPlane > ((poiObject.transform.position - arCameraTransform.position).magnitude + 2f));
            }
        }
    }

#if UNITY_EDITOR

    private void Reset()
    {
        if (arTrueNorthFinder == null)
        {
            arTrueNorthFinder = FindAnyObjectByType<pLab_ARTrueNorthFinder>();
        }

        if (arCamera == null)
        {
            arCamera = this.GetComponentInChildren<Camera>();
        }

        if (deviceElevationEstimater == null)
        {
            deviceElevationEstimater = FindAnyObjectByType<pLab_ARDeviceElevationEstimater>();
        }

        if (locationProvider == null)
        {
            locationProvider = FindAnyObjectByType<pLab_LocationProvider>();
        }
    }

#endif

    #endregion

    #region Public methods

    /// <summary>
    /// Refresh trackers. Removes trackers for removed POIs. Should be called after MANUALLY modifying point of interests list
    /// </summary>
    public void RefreshTrackers()
    {
        List<pLab_POIObject> poisToRemove = new List<pLab_POIObject>();

        for (int i = 0; i < poiTrackerDatas.Count; i++)
        {
            if (!PointOfInterests.Contains(poiTrackerDatas[i].POI))
            {
                poisToRemove.Add(poiTrackerDatas[i].POI);
            }
        }

        for (int i = 0; i < poisToRemove.Count; i++)
        {
            StopTrackingPOI(poisToRemove[i]);
        }
        // Yeni POI'leri ekle
        for (int i = 0; i < PointOfInterests.Count; i++)
        {
            pLab_POIObject poi = PointOfInterests[i];

            // POI zaten takip ediliyorsa atla
            if (poiTrackerDatas.Exists(x => x.POI == poi)) continue;

            // Yeni POI için takip nesnesi oluþtur
            CreateObjectsForPOI(poi);
        }

     

        // POI takibini güncelle
        RecheckPOITrackings();

    }


    #endregion

    #region Event Handlers

    private void OnARSessionStateChange(ARSessionStateChangedEventArgs evt)
    {
        previousUpdateAccuracy = 9999f;
        // CheckDistances();
        if (evt.state == ARSessionState.SessionTracking)
        {
            RecheckPOITrackings();
        }
        else
        {
            StopTrackingPOIs();
        }
    }

    private void OnNorthHeadingUpdated(object sender, pLab_NorthHeadingUpdatedEventArgs e)
    {
        if (e.isPriority || TimeSpan.FromMilliseconds(e.timestamp - previousUpdateTimestamp).TotalSeconds > 10f) /// Orginially 5f
        {
            RotatePOIsRelativeToNorth(e.heading, e.isPriority);
        }
    }

    private void OnLocationUpdated(object sender, pLab_LocationUpdatedEventArgs e)
    {

        bool updatePositions = false;

        float accuracy = Mathf.Max(e.horizontalAccuracy, e.verticalAccuracy);

        float deltaAccuracy = accuracy - previousUpdateAccuracy;

        if (accuracy <= 8f || deltaAccuracy <= 5f)
        {
            updatePositions = true;
        }
        else
        {
            TimeSpan timeSinceLast = TimeSpan.FromMilliseconds(e.timestamp - previousUpdateTimestamp);
            updatePositions =
                (timeSinceLast.TotalSeconds > 10 && accuracy <= 8f)
                || (timeSinceLast.TotalSeconds > 20 && accuracy <= 15f)
                || (timeSinceLast.TotalSeconds > 30);
        }

        if (updatePositions)
        {
            previousUpdateAccuracy = accuracy;
            previousUpdateTimestamp = e.timestamp;
            // CheckDistances();
            RecheckPOITrackings();
        }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Move all tracked POI-objects
    /// </summary>
    private void RecheckPOITrackings()
    {
        // Assign shared attributes to POIs after refreshing trackers
        PointOfInterestSet.AssignSharedAttributesToPOIs();

        pLab_LatLon currentLocation = locationProvider.Location;
        if (currentLocation == null) return;

        Vector3 currentARCameraPosition = arCameraTransform.position;
        float devicePosY = currentARCameraPosition.y;
        currentARCameraPosition.y = 0;

        float groundLevel = deviceElevationEstimater != null ? deviceElevationEstimater.GroundLevelEstimate : 0f;
        if (PointOfInterests == null || PointOfInterests.Count == 0) return;

        for (int i = 0; i < PointOfInterests.Count; i++)
        {

            pLab_POIObject poi = PointOfInterests[i];

            //Calculate the distance between the points
            float distanceBetween = currentLocation.DistanceToPointPythagoras(poi.coordinates);

            bool onlyUpdateHeight = false;


            //Don't update the position if already so close
            //Maybe should actually search for a plane close to this and "anchor" it to that, or try to find nearest plane to get the height
            //If close tracking -> don't update the position to avoid jitter and unability to inspect object closely
            if (!poi.CloseTracking && distanceBetween <= poi.closeTrackingRadius)
            {
                if (!poi.FarTracking)
                {
                    CreateObjectsForPOI(poi);
                }

                poi.trackingState = POITrackingState.CloseTracking;
            }
            else if (!poi.Tracking && distanceBetween <= poi.trackingRadius)
            {
                poi.trackingState = POITrackingState.FarTracking;
                CreateObjectsForPOI(poi);
            }
            else if (poi.Tracking && distanceBetween >= poi.TrackingExitRadius)
            {
                StopTrackingPOI(poi);
            }
            else if (poi.CloseTracking)
            {
                if (distanceBetween >= poi.CloseTrackingExitRadius)
                {
                    poi.trackingState = POITrackingState.FarTracking;
                }
                else
                {
                    //Close Tracking is true AND poi is inside the closeTrackingRadius
                    onlyUpdateHeight = true;
                }
            }


            POITrackerData trackerData = poiTrackerDatas.Find(x => x.POI == poi);

            if (trackerData == null) continue;

            float trueNorthHeadingDifference = arTrueNorthFinder != null ? arTrueNorthFinder.Heading : 0;

            pLab_PointOfInterestCanvasBase_Json poiCanvas = trackerData.POICanvas;
            pLab_PointOfInterestObjectBase_Json poiObject = trackerData.POIObject;

            //If we only update the height, set the heights and continue to the next POI
            if (onlyUpdateHeight)
            {
                if (poiObject != null)
                {
                    poiObject.UpdatePositionY(groundLevel, devicePosY);
                }

                //Update poi's canvas position
                if (poiCanvas != null)
                {
                    poiCanvas.UpdatePositionY(groundLevel, devicePosY);
                }

                continue;
            }

            Vector3 newPos = Vector3.zero;

            float bearing = 0;

            switch (positioningMode)
            {
                case PositioningMode.DistanceAndBearing:
                    //Get the bearing relative to north (this is why it's important for the z-axis to face north)
                    bearing = pLab_GeoTools.BearingFromPointAToB(currentLocation, poi.coordinates);
                    /**
                    Usually it should be X = cos(bearing), Y = sin(bearing) but for some reason it has to flipped other way around
                    So X = sin(bearing) and Z = cos(bearing)
                    */
                    newPos = new Vector3(distanceBetween * Mathf.Sin(bearing), 0, distanceBetween * Mathf.Cos(bearing));

                    break;

                case PositioningMode.UTM:

                    Vector2 UTMDifference = pLab_GeoTools.UTMDifferenceBetweenPoints(currentLocation, poi.coordinates);

                    newPos = new Vector3(UTMDifference.x, 0, UTMDifference.y);

                    //For debugging
                    bearing = Vector3.SignedAngle(Vector3.forward, newPos.normalized, Vector3.up);
                    bearing = pLab_MathConversions.DegAngle0To360(bearing);
                    bearing = pLab_MathConversions.DegToRad(bearing);
                    //END debug
                    distanceBetween = newPos.magnitude;
                    break;
            }

            //Offset new position with current camera position
            trackerData.LastUpdateCameraPosition = currentARCameraPosition;
            trackerData.LastUpdatePositionRelativeToCamera = newPos;

            //Offset with true north difference so Z-axis == AR True-North-Axis
            newPos = currentARCameraPosition + (Quaternion.AngleAxis(trueNorthHeadingDifference, Vector3.up) * newPos);

            if (poiObject != null)
            {
                poiObject.UpdatePosition(newPos, groundLevel, devicePosY);
                poiObject.UpdateRotation(trueNorthHeadingDifference);
            }

            //Update poi's canvas position
            if (poiCanvas != null)
            {
                poiCanvas.UpdatePosition(newPos, groundLevel, devicePosY);
                poiCanvas.UpdateDistance(distanceBetween);
            }

            if (updateDebug)
            {
                pointOfInterestDebug.UpdateItem(poi, distanceBetween, bearing, newPos);
            }
        }

        updateDebug = false;

    }

    private void RotatePOIsRelativeToNorth(bool forceUpdate = false)
    {
        float trueNorthHeadingDifference = arTrueNorthFinder != null ? arTrueNorthFinder.Heading : 0;

        RotatePOIsRelativeToNorth(trueNorthHeadingDifference, forceUpdate);
    }

    /// <summary>
    /// Only rotates the POI relative to true north. Doesn't affect the positions.
    /// </summary>
    /// <param name="heading"></param>
    private void RotatePOIsRelativeToNorth(float heading, bool forceUpdate = false)
    {

        for (int i = 0; i < poiTrackerDatas.Count; i++)
        {
            POITrackerData trackerData = poiTrackerDatas[i];
            //Skip this one if the tracker data is null,
            //or the POI is null,
            //or if the POI is being close tracked (should remain in the current position and rotation)

            if (trackerData == null || trackerData.POI == null || (trackerData.POI.CloseTracking && !forceUpdate)) continue;

            Vector3 newPos = trackerData.LastUpdateCameraPosition + (Quaternion.AngleAxis(heading, Vector3.up) * trackerData.LastUpdatePositionRelativeToCamera);

            if (trackerData.POICanvas != null)
            {
                trackerData.POICanvas.UpdatePositionXZ(newPos);
            }

            if (trackerData.POIObject != null)
            {
                trackerData.POIObject.UpdatePositionXZ(newPos);
            }
        }
    }
    /// <summary>
    /// Enable tracking for Point of Interest. Create 3D-model and canvas objects.
    /// </summary>
    /// <param name="poi"></param>
    public void CreateObjectsForPOI(pLab_POIObject poi)
    {
        //Check if there is already objects for this tracker
        if (poiTrackerDatas != null && poiTrackerDatas.Count > 0)
        {
            POITrackerData poiTracker = poiTrackerDatas.Find((x) => x.POI == poi);
            if (poiTracker != null) return;
        }

        GameObject objectGo = null;
        GameObject modelGo = null;
        GameObject poiCanvasGo = null;

        pLab_PointOfInterestObjectBase_Json poiObject = null;
        pLab_PointOfInterestCanvasBase_Json poiCanvas = null;

        if (poi.modelPrefab != null)
        {
            if (poi.objectPrefab == null)
            {
                objectGo = new GameObject($"Object tracker for {poi.poiName}");
                objectGo.transform.SetParent(poiParentObject);
            }
            else
            {
                objectGo = Instantiate(poi.objectPrefab, Vector3.zero, Quaternion.identity, poiParentObject);
            }

            modelGo = Instantiate(poi.modelPrefab, Vector3.zero, Quaternion.identity, objectGo.transform);

            poiObject = objectGo.GetComponent<pLab_PointOfInterestObjectBase_Json>();

            if (poiObject == null)
            {
                poiObject = objectGo.AddComponent<pLab_PointOfInterestObject_Json>();
            }

            poiObject.Setup(poi, arCamera);
            poiObject.UpdateRotation(0);
        }


        if (poi.canvasPrefab != null)
        {
            poiCanvasGo = Instantiate(poi.canvasPrefab, Vector3.zero, Quaternion.identity, poiParentObject);

            poiCanvas = poiCanvasGo.GetComponent<pLab_PointOfInterestCanvasBase_Json>();

            if (poiCanvas == null)
            {
                poiCanvas = poiCanvasGo.AddComponent<pLab_PointOfInterestCanvas_Json>();
            }

            poiCanvas.Setup(poi, arCamera);
        }

        POITrackerData trackerData = new POITrackerData(poi, poiCanvas, poiObject);
        poiTrackerDatas.Add(trackerData);
    }

    /// <summary>
    /// Disable tracking of Point of Interest. Destroys 3D-model and canvas objects.
    /// </summary>
    /// <param name="poi"></param>
    private void StopTrackingPOI(pLab_POIObject poi)
    {
        poi.trackingState = POITrackingState.NotTracking;

        POITrackerData trackerData = poiTrackerDatas.Find((x) => x.POI == poi);

        if (trackerData == null) return;


        pLab_PointOfInterestObjectBase_Json poiObject = trackerData.POIObject;

        //Destroy and remove poi 3D-model
        if (poiObject != null)
        {
            Destroy(poiObject.gameObject);
        }

        //Destroy and remove poi canvas
        pLab_PointOfInterestCanvasBase_Json poiCanvas = trackerData.POICanvas;

        if (poiCanvas != null)
        {
            Destroy(poiCanvas.gameObject);
        }

        poiTrackerDatas.Remove(trackerData);
    }

    /// <summary>
    /// Stops tracking all the POIs, deleting all the canvases and gameobjects.
    /// </summary>
    private void StopTrackingPOIs()
    {
        if (poiTrackerDatas != null)
        {
            for (int i = 0; i < poiTrackerDatas.Count; i++)
            {
                POITrackerData trackerData = poiTrackerDatas[i];
                if (trackerData == null) continue;

                pLab_POIObject poi = trackerData.POI;

                if (poi != null)
                {
                    poi.trackingState = POITrackingState.NotTracking;
                }

                pLab_PointOfInterestObjectBase_Json poiObject = trackerData.POIObject;

                //Destroy and remove poi 3D-model
                if (poiObject != null)
                {
                    Destroy(poiObject.gameObject);
                }

                //Destroy and remove poi canvas
                pLab_PointOfInterestCanvasBase_Json poiCanvas = trackerData.POICanvas;

                if (poiCanvas != null)
                {
                    Destroy(poiCanvas.gameObject);
                }
            }

            //Make sure every poi is set to "NotTracking"-state
            for (int i = 0; i < PointOfInterests.Count; i++)
            {
                PointOfInterests[i].trackingState = POITrackingState.NotTracking;
            }

            poiTrackerDatas.Clear();

        }
    }

    #endregion

    #region IEnumerators/Coroutines

    #endregion

}
